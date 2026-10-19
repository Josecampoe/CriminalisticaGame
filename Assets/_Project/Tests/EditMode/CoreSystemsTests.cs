using System.IO;
using System.Linq;
using Caso404.AI;
using Caso404.Core;
using Caso404.Save;
using NUnit.Framework;
using UnityEngine;

namespace Caso404.Tests
{
    /// <summary>Pruebas de ALEX (confianza y contradicciones), hipótesis, decisiones y guardado.</summary>
    public class CoreSystemsTests
    {
        [Test]
        public void Alex_AcceptRaisesTrust_QuestionLowersIt()
        {
            var w = new Case1World();
            Assert.AreEqual(60, w.Alex.Trust, "alexTrust empieza en 60");
            Assert.AreEqual(AlexTone.Warm, w.Alex.Tone);
            w.CollectAndAnalyze("ev_c1_huella");
            var claim = w.Lie("lie_c1_alex_ventana");
            w.Alex.Accept(claim);
            w.Alex.Accept(claim); // aceptar dos veces no suma dos veces
            Assert.AreEqual(65, w.Alex.Trust);

            Assert.IsFalse(w.Alex.Question(claim, w.Ev("ev_c1_huella")).Success, "La huella no refuta esa afirmación");
            Assert.AreEqual(0, w.Alex.ContradictionCount);

            w.CollectAndAnalyze("ev_c1_ventana");
            var ok = w.Alex.Question(claim, w.Ev("ev_c1_ventana"));
            Assert.IsTrue(ok.Success);
            Assert.AreEqual("Tienes razón. Mi análisis fue apresurado. Lo registro.", ok.Response);
            Assert.AreEqual(55, w.Alex.Trust);
            Assert.AreEqual(AlexTone.Formal, w.Alex.Tone);
            Assert.AreEqual(1, w.Alex.ContradictionCount);
            w.Alex.Question(claim, w.Ev("ev_c1_ventana"));
            Assert.AreEqual(1, w.Alex.ContradictionCount, "La misma contradicción no cuenta dos veces");
        }

        [Test]
        public void Alex_ColdToneBelow30()
        {
            var w = new Case1World();
            w.State.ChangeTrust(-35);
            Assert.AreEqual(AlexTone.Cold, w.Alex.Tone);
            w.State.ChangeTrust(-500);
            Assert.AreEqual(0, w.Alex.Trust, "La confianza no baja de 0");
        }

        [Test]
        public void Hypothesis_KeyOptionLockedUntilWindowAndDoorAnalyzed()
        {
            var w = new Case1World();
            var hyp = w.Case.Hypotheses.First(h => h.Id == "hyp_c1_s1");
            var q1 = hyp.Questions[0];
            var key = q1.Options.First(o => o.Id == "llave");
            Assert.IsFalse(w.Hypotheses.IsUnlocked(key));
            StringAssert.Contains("Ventana", w.Hypotheses.LockReason(key));
            Assert.IsFalse(w.Hypotheses.Submit(q1, new[] { "llave" }), "Una opción bloqueada no cuenta");

            w.CollectAndAnalyze("ev_c1_ventana", "ev_c1_puerta");
            Assert.IsTrue(w.Hypotheses.IsUnlocked(key));
            Assert.IsFalse(w.Hypotheses.Submit(q1, new[] { "ventana" }));
            Assert.IsFalse(w.State.HasFlag("flag_c1_h_llave"));
            Assert.IsTrue(w.Hypotheses.Submit(q1, new[] { "llave" }));
            Assert.IsTrue(w.State.HasFlag("flag_c1_h_llave"));
        }

        [Test]
        public void Hypothesis_MultiSelectNeedsExactSet()
        {
            var w = new Case1World();
            var q2 = w.Case.Hypotheses.First(h => h.Id == "hyp_c1_s1").Questions[1];
            w.CollectAndAnalyze("ev_c1_huella", "ev_c1_telefono");
            Assert.IsFalse(w.Hypotheses.Submit(q2, new[] { "carlos", "desconocido" }));
            Assert.IsTrue(w.Hypotheses.Submit(q2, new[] { "carlos", "laura", "desconocido" }));
            Assert.IsFalse(w.Hypotheses.IsComplete(w.Case.Hypotheses.First(h => h.Id == "hyp_c1_s1")), "Falta la pregunta 1");
        }

        [Test]
        public void Decision_OnlyOnce()
        {
            var w = new Case1World();
            w.Decide("dec_c1_s1", "B");
            var d = w.Case.FindDecision("dec_c1_s1");
            Assert.IsFalse(w.Decisions.Choose(d, d.FindOption("A")).Applied);
            Assert.AreEqual("B", w.State.GetDecisionOption("dec_c1_s1"));
            Assert.IsFalse(w.State.HasFlag("flag_c1_carlos_detenido"));
        }

        [Test]
        public void Notebook_DecisionChangesTrustAndFlags()
        {
            var w = new Case1World();
            w.Decide("dec_c1_cuaderno", "B");
            Assert.IsTrue(w.State.HasFlag("flag_c1_cuaderno"));
            Assert.AreEqual(55, w.State.AlexTrust);
        }

        [Test]
        public void Phone_DeepAnalysisSetsNxFlag()
        {
            var w = new Case1World();
            var phone = w.Ev("ev_c1_celular_andres");
            Assert.IsTrue(w.Evidence.Grant(phone));
            Assert.IsFalse(w.Evidence.RunExtraAnalysis(phone).Success, "Primero el análisis normal");
            w.CollectAndAnalyze("ev_c1_celular_andres");
            Assert.IsTrue(w.Evidence.RunExtraAnalysis(phone).Success);
            Assert.IsTrue(w.State.HasFlag("flag_c1_pista_nx"));
        }

        [Test]
        public void Save_RoundTrip()
        {
            var path = Path.Combine(Application.temporaryCachePath, "caso404_test_save.json");
            var saver = new SaveService(path);
            saver.Delete();
            var w = new Case1World();
            w.CollectAndAnalyze("ev_c1_ventana");
            w.Decide("dec_c1_s1", "A");
            w.State.AddContradiction("c1", "lie_c1_alex_ventana");
            Assert.IsTrue(saver.Save(w.State.State));

            Assert.IsTrue(saver.TryLoad(out var loaded));
            var restored = new GameStateService(loaded);
            Assert.IsTrue(restored.IsEvidenceAnalyzed("ev_c1_ventana"));
            Assert.IsTrue(restored.HasFlag("flag_c1_carlos_detenido"));
            Assert.AreEqual(70, restored.AlexTrust);
            Assert.AreEqual(1, restored.GetContradictionCount("c1"));
            Assert.AreEqual("A", restored.GetDecisionOption("dec_c1_s1"));
            saver.Delete();
            Assert.IsFalse(saver.TryLoad(out _));
        }
    }
}
