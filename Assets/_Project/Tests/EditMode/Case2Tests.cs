using System.Linq;
using Caso404.AI;
using Caso404.Data;
using Caso404.Dialogue;
using NUnit.Framework;

namespace Caso404.Tests
{
    /// <summary>Caso 2 "Protocolo 404": finales (GUION §4.6), requisitos de refutación y reglas de diálogo.</summary>
    public class Case2Tests
    {
        static Case1World World() => new("c2");

        /// <summary>Investigación base: gas a distancia, nota, portátil con contraseña, tarjeta y placas legibles.</summary>
        static void Investigate(Case1World w, bool refuteAlex)
        {
            w.CollectAndAnalyze("ev_c2_puerta_log", "ev_c2_sensor_humo", "ev_c2_panel_gas", "ev_c2_nota", "ev_c2_usb_elena",
                "ev_c2_boceto", "ev_c2_postit", "ev_c2_portatil_tomas", "ev_c2_tarjeta_valeria", "ev_c2_consola_valeria", "ev_c2_camara_parqueadero");
            Assert.IsTrue(w.Evidence.RunExtraAnalysis(w.Ev("ev_c2_portatil_tomas"), "lucero").Success, "La contraseña es LUCERO (sin importar mayúsculas)");
            Assert.IsTrue(w.State.HasFlag("flag_c2_directiva"));
            Assert.IsTrue(w.Evidence.RunExtraAnalysis(w.Ev("ev_c2_camara_parqueadero")).Success);
            Assert.IsTrue(w.State.HasFlag("flag_c2_placas_legibles"));
            if (!refuteAlex) return;
            w.Refute("lie_c2_alex_falla", "ev_c2_sensor_humo");
            w.Refute("lie_c2_alex_nota", "ev_c2_nota");
            w.Refute("lie_c2_alex_portatil", "ev_c2_portatil_tomas");
        }

        [Test]
        public void SecretEnding_NeedsTheTraceFromCase1()
        {
            var w = World();
            w.State.AddFlag("flag_c1_pista_nx"); // viene del caso 1
            Investigate(w, refuteAlex: true);
            var r = w.Accuse("valeria", "ev_c2_tarjeta_valeria", "ev_c2_camara_parqueadero", "ev_c2_consola_valeria");
            Assert.AreEqual("end_c2_secreto", r.Ending.Id);
            Assert.AreEqual("dec_c2_alex", r.Ending.FollowUpDecision.Id);
        }

        [Test]
        public void TrueEnding_WithoutTraceFromCase1()
        {
            var w = World();
            Investigate(w, refuteAlex: true);
            var r = w.Accuse("valeria", "ev_c2_tarjeta_valeria", "ev_c2_camara_parqueadero", "ev_c2_consola_valeria");
            Assert.AreEqual("end_c2_verdadero", r.Ending.Id, "Sin flag_c1_pista_nx ni flag_c1_cuaderno no hay final secreto");
        }

        [Test]
        public void TrueEnding_EvenIfTimeWasLost()
        {
            var w = World();
            w.Decide("dec_c2_s1", "A");
            Assert.IsFalse(w.Evidence.IsAvailable(w.Ev("ev_c2_backup")), "Con tiempo perdido la copia espejo se borra");
            w.CollectAndAnalyze("ev_c2_tarjeta_valeria", "ev_c2_camara_parqueadero", "ev_c2_sensor_humo", "ev_c2_puerta_log");
            w.Evidence.RunExtraAnalysis(w.Ev("ev_c2_camara_parqueadero"));
            w.Refute("lie_c2_alex_placas", "ev_c2_camara_parqueadero");
            w.Refute("lie_c2_alex_falla", "ev_c2_sensor_humo");
            w.Decide("dec_c2_ines", "A");
            Assert.IsFalse(w.Evidence.IsFound(w.Ev("ev_c2_backup")), "Inés declara, pero ya no hay copia espejo");
            var r = w.Accuse("valeria", "ev_c2_tarjeta_valeria", "ev_c2_camara_parqueadero", "ev_c2_sensor_humo");
            Assert.AreEqual("end_c2_verdadero", r.Ending.Id);
        }

        [Test]
        public void Refutations_RequirePasswordAndExternalLab()
        {
            var w = World();
            w.CollectAndAnalyze("ev_c2_portatil_tomas", "ev_c2_camara_parqueadero");
            Assert.IsFalse(w.Alex.Question(w.Lie("lie_c2_alex_portatil"), w.Ev("ev_c2_portatil_tomas")).Success, "Sin la contraseña el portátil no refuta");
            Assert.IsFalse(w.Alex.Question(w.Lie("lie_c2_alex_placas"), w.Ev("ev_c2_camara_parqueadero")).Success, "Sin laboratorio externo no hay placas");
            Assert.IsFalse(w.Evidence.RunExtraAnalysis(w.Ev("ev_c2_portatil_tomas"), "ALEX").Success, "Contraseña incorrecta");
            w.Evidence.RunExtraAnalysis(w.Ev("ev_c2_portatil_tomas"), "LUCERO");
            w.Evidence.RunExtraAnalysis(w.Ev("ev_c2_camara_parqueadero"));
            w.Refute("lie_c2_alex_portatil", "ev_c2_portatil_tomas");
            w.Refute("lie_c2_alex_placas", "ev_c2_camara_parqueadero");
        }

        [Test]
        public void CameraOnlyCountsAsSolidProofAfterExternalLab_AndInesTestimonyCounts()
        {
            var w = World();
            w.CollectAndAnalyze("ev_c2_tarjeta_valeria", "ev_c2_camara_parqueadero", "ev_c2_sensor_humo");
            var proofs = new[] { "ev_c2_tarjeta_valeria", "ev_c2_camara_parqueadero", "ev_c2_sensor_humo" };
            Assert.AreEqual(1, Decisions.EndingEvaluator.CountSolidProofs(w.Case, proofs, w.State));
            w.Evidence.RunExtraAnalysis(w.Ev("ev_c2_camara_parqueadero"));
            Assert.AreEqual(2, Decisions.EndingEvaluator.CountSolidProofs(w.Case, proofs, w.State));
            w.Decide("dec_c2_ines", "A");
            Assert.AreEqual(3, Decisions.EndingEvaluator.CountSolidProofs(w.Case, proofs, w.State), "flag_c2_ines_testigo cuenta como prueba");
        }

        [Test]
        public void IncompleteEnding_Variants()
        {
            var w = World();
            Investigate(w, refuteAlex: false);
            Assert.AreEqual("valeria_sin_alex", w.Accuse("valeria", "ev_c2_tarjeta_valeria", "ev_c2_camara_parqueadero", "ev_c2_consola_valeria").Variant.Id);
            var w2 = World();
            w2.CollectAndAnalyze("ev_c2_libro_rondas", "ev_c2_sensor_humo", "ev_c2_panel_gas");
            var r = w2.Accuse("ines", "ev_c2_libro_rondas", "ev_c2_sensor_humo", "ev_c2_panel_gas");
            Assert.AreEqual("end_c2_incompleto", r.Ending.Id);
            Assert.AreEqual("ines", r.Variant.Id);
        }

        [Test]
        public void DeceptionEnding_AccuseSebastian()
        {
            var w = World();
            w.CollectAndAnalyze("ev_c2_nota", "ev_c2_camara_parqueadero", "ev_c2_sensor_humo");
            Assert.AreEqual("end_c2_engano", w.Accuse("sebastian", "ev_c2_nota", "ev_c2_camara_parqueadero", "ev_c2_sensor_humo").Ending.Id);
        }

        [Test]
        public void Ines_ExposedLieOpensHerDecision_AndProtectionGivesBackup()
        {
            var w = World();
            var d = new DialogueService(w.State, w.Evidence, new OfflineAiClient(), w.Case);
            DecisionData triggered = null;
            d.DecisionTriggered += x => triggered = x;
            w.CollectAndAnalyze("ev_c2_libro_rondas");
            var ines = w.Case.FindSuspect("ines");
            d.Begin(ines);
            var item = d.GetPresentableItems().First(i => i.Evidence != null && i.Evidence.Id == "ev_c2_libro_rondas");
            Assert.IsTrue(d.Present(ines, item).ExposedLie);
            Assert.AreEqual("dec_c2_ines", triggered?.Id);
            Assert.IsTrue(w.State.KnowsFact("f_c2_ines_vio"));
            // La opción A entrega la copia espejo (lo hace GameContext al aplicar la decisión).
            var option = triggered.FindOption("A");
            w.Decisions.Choose(triggered, option);
            foreach (var ev in option.GrantsEvidence) w.Evidence.Grant(ev);
            Assert.IsTrue(w.Evidence.IsFound(w.Ev("ev_c2_backup")));
        }

        [Test]
        public void Sebastian_ConfessesKairosWhenShownTheChat()
        {
            var w = World();
            var d = new DialogueService(w.State, w.Evidence, new OfflineAiClient(), w.Case);
            w.CollectAndAnalyze("ev_c2_chat_kairos");
            var seb = w.Case.FindSuspect("sebastian");
            d.Begin(seb);
            var line = d.Present(seb, d.GetPresentableItems().First(i => i.Evidence != null && i.Evidence.Id == "ev_c2_chat_kairos"));
            StringAssert.Contains("El Muelle", line.Text);
            Assert.IsTrue(w.State.KnowsFact("f_c2_seb_kairos"));
            Assert.IsTrue(w.State.KnowsFact("f_c2_seb_bar"));
        }

        [Test]
        public void Elena_AfterDeceptionEnding_CooperatesOnlyAfterSeeingHerUsb()
        {
            var w = World();
            w.State.AddFlag("end_c1_engano");
            var d = new DialogueService(w.State, w.Evidence, new OfflineAiClient(), w.Case);
            var elena = w.Case.FindCharacter("elena");
            d.Begin(elena);
            Assert.IsTrue(d.IsRefusing(elena));
            Assert.IsEmpty(d.AvailableExtraTopics(elena), "Mientras no coopera no ofrece sus temas");
            StringAssert.Contains("Carlos", d.AskAsync(elena, DialogueTopic.Crime, null, default).Result.Text);
            w.CollectAndAnalyze("ev_c2_usb_elena");
            d.Present(elena, d.GetPresentableItems().First(i => i.Evidence != null && i.Evidence.Id == "ev_c2_usb_elena"));
            Assert.IsFalse(d.IsRefusing(elena));
            Assert.IsNotEmpty(d.AvailableExtraTopics(elena));
        }

        [Test]
        public void Elena_NotebookGreetingLowersTrustOnce()
        {
            var w = World();
            w.State.AddFlag("flag_c1_cuaderno");
            var d = new DialogueService(w.State, w.Evidence, new OfflineAiClient(), w.Case);
            var elena = w.Case.FindCharacter("elena");
            StringAssert.Contains("cuaderno", d.Greeting(elena));
            d.Greeting(elena);
            Assert.AreEqual(55, w.State.AlexTrust, "−5 una sola vez");
        }

        [Test]
        public void Data_IsFairAndMatchesTheScript()
        {
            var w = World();
            Assert.AreEqual(15, w.Case.Evidence.Count);
            Assert.AreEqual(5, w.Case.AlexClaims.Count());
            Assert.AreEqual("valeria", w.Case.Culprit.Id);
            Assert.AreEqual("sebastian", w.Case.AlexTarget.Id);
            Assert.IsTrue(w.Case.Evidence.All(e => e.Id.StartsWith("ev_c2_")));
            Assert.IsTrue(w.Case.Lies.All(l => l.Id.StartsWith("lie_c2_")));
            Assert.IsNotNull(w.Case.FindCharacter("elena"), "Elena es aliada");
            Assert.IsNull(w.Case.FindSuspect("elena"), "Elena no se puede acusar");
            foreach (var lie in w.Case.Lies)
                Assert.IsTrue(lie.RefutedByEvidence.Count > 0 || lie.RefutedByFactIds.Count > 0, $"{lie.Id} no tiene refutación");
        }
    }
}
