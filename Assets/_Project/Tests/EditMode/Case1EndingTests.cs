using NUnit.Framework;

namespace Caso404.Tests
{
    /// <summary>Recorre los 3 finales del caso 1 (GUION §3.6 y caminos mínimos del §5).</summary>
    public class Case1EndingTests
    {
        [Test]
        public void TrueEnding_EvenIfCarlosWasDetained()
        {
            var w = new Case1World();
            w.CollectAndAnalyze("ev_c1_ventana", "ev_c1_puerta", "ev_c1_huella", "ev_c1_camara_pasillo");
            Assert.IsTrue(w.State.IsAlexClaimRaised("lie_c1_alex_ventana"), "Al analizar la huella ALEX debe culpar a Carlos");
            Assert.IsTrue(w.State.IsAlexClaimRaised("lie_c1_alex_archivo"), "Al analizar la cámara ALEX debe dar ERROR 404");

            w.Decide("dec_c1_s1", "A");
            Assert.IsTrue(w.State.HasFlag("flag_c1_carlos_detenido"));
            Assert.AreEqual(70, w.State.AlexTrust, "Seguir la recomendación de ALEX: +10");

            // Consecuencia: Andrés quema el overol.
            Assert.IsFalse(w.Evidence.IsAvailable(w.Ev("ev_c1_overol")), "Con Carlos detenido no debe aparecer el overol");
            Assert.IsTrue(w.Evidence.IsAvailable(w.Ev("ev_c1_cenizas")), "Con Carlos detenido deben aparecer las cenizas");

            // El DVR desbloquea la grabación original.
            w.CollectAndAnalyze("ev_c1_dvr");
            Assert.IsTrue(w.Evidence.IsFound(w.Ev("ev_c1_camara_original")), "El DVR debe desbloquear la grabación original");
            w.CollectAndAnalyze("ev_c1_camara_original", "ev_c1_llavero");

            w.Refute("lie_c1_alex_ventana", "ev_c1_ventana");
            w.Refute("lie_c1_alex_archivo", "ev_c1_camara_original");
            Assert.AreEqual(2, w.Alex.ContradictionCount);

            var result = w.Accuse("andres", "ev_c1_llavero", "ev_c1_camara_original", "ev_c1_ventana");
            Assert.AreEqual("end_c1_verdadero", result.Ending.Id);
            Assert.AreEqual(2, result.SolidProofs);
            Assert.IsTrue(w.State.HasFlag("end_c1_verdadero"), "El final debe quedar como flag para el caso 2");
        }

        [Test]
        public void TrueEnding_WithOverolPath()
        {
            var w = new Case1World();
            w.CollectAndAnalyze("ev_c1_ventana", "ev_c1_puerta", "ev_c1_huella");
            w.Decide("dec_c1_s1", "C");
            w.CollectAndAnalyze("ev_c1_llavero");
            var overol = w.Ev("ev_c1_overol");
            Assert.IsTrue(w.Evidence.TryCollect(overol));
            Assert.IsTrue(w.State.IsAlexClaimRaised("lie_c1_alex_overol"), "Al encontrar el overol ALEX lo minimiza");
            Assert.IsFalse(w.Evidence.CanAnalyze(overol, out _), "Sin girarlo no se ve el vidrio");
            w.CollectAndAnalyze("ev_c1_overol");
            w.Refute("lie_c1_alex_overol", "ev_c1_overol");
            w.Refute("lie_c1_alex_ventana", "ev_c1_puerta");

            var result = w.Accuse("andres", "ev_c1_llavero", "ev_c1_overol", "ev_c1_puerta");
            Assert.AreEqual("end_c1_verdadero", result.Ending.Id);
        }

        [Test]
        public void IncompleteEnding_AndresWithFewProofs()
        {
            var w = new Case1World();
            w.CollectAndAnalyze("ev_c1_ventana", "ev_c1_puerta", "ev_c1_huella", "ev_c1_llavero");
            w.Refute("lie_c1_alex_ventana", "ev_c1_ventana");
            var result = w.Accuse("andres", "ev_c1_llavero", "ev_c1_ventana", "ev_c1_puerta");
            Assert.AreEqual("end_c1_incompleto", result.Ending.Id);
            Assert.AreEqual("andres_pocas_pruebas", result.Variant.Id);
        }

        [Test]
        public void IncompleteEnding_AndresWithProofsButAlexUnnoticed()
        {
            var w = new Case1World();
            w.CollectAndAnalyze("ev_c1_ventana", "ev_c1_dvr", "ev_c1_camara_original", "ev_c1_llavero");
            var result = w.Accuse("andres", "ev_c1_llavero", "ev_c1_camara_original", "ev_c1_ventana");
            Assert.AreEqual("end_c1_incompleto", result.Ending.Id);
            Assert.AreEqual("andres_sin_alex", result.Variant.Id);
        }

        [TestCase("laura")]
        [TestCase("hernan")]
        public void IncompleteEnding_InnocentAccused(string suspect)
        {
            var w = new Case1World();
            w.CollectAndAnalyze("ev_c1_ventana", "ev_c1_puerta", "ev_c1_telefono");
            var result = w.Accuse(suspect, "ev_c1_ventana", "ev_c1_puerta", "ev_c1_telefono");
            Assert.AreEqual("end_c1_incompleto", result.Ending.Id);
            Assert.AreEqual("inocente", result.Variant.Id);
        }

        [Test]
        public void DeceptionEnding_AccuseCarlos()
        {
            var w = new Case1World();
            w.CollectAndAnalyze("ev_c1_huella", "ev_c1_colilla", "ev_c1_telefono");
            w.Decide("dec_c1_s1", "A");
            var result = w.Accuse("carlos", "ev_c1_huella", "ev_c1_colilla", "ev_c1_telefono");
            Assert.AreEqual("end_c1_engano", result.Ending.Id);
        }

        [Test]
        public void Accusation_RejectsUnanalyzedOrRepeatedProofs()
        {
            var w = new Case1World();
            w.CollectAndAnalyze("ev_c1_ventana", "ev_c1_puerta");
            Assert.IsNull(w.Accusation.Accuse("andres", new[] { "ev_c1_ventana", "ev_c1_ventana", "ev_c1_puerta" }));
            Assert.IsNull(w.Accusation.Accuse("andres", new[] { "ev_c1_ventana", "ev_c1_puerta", "ev_c1_llavero" }));
        }
    }
}
