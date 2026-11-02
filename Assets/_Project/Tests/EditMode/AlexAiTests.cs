using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Caso404.AI;
using Caso404.Evidence;
using NUnit.Framework;

namespace Caso404.Tests
{
    /// <summary>
    /// Preguntas libres a ALEX: con IA usa la respuesta solo si es válida; sin IA (o si falla) usa el guion.
    /// En ningún caso la respuesta cambia la confianza ni el estado del caso. Usa un cliente de IA falso (sin red).
    /// </summary>
    public class AlexAiTests
    {
        /// <summary>Cliente falso: devuelve una respuesta fija y guarda el último pedido.</summary>
        class FakeAi : IAiClient
        {
            public AiReply Reply;
            public AiRequest LastRequest;
            public bool IsOffline => false;
            public Task<AiReply> AskAsync(AiRequest request, CancellationToken ct)
            {
                LastRequest = request;
                return Task.FromResult(Reply);
            }
        }

        static (Case1World w, AlexService alex, FakeAi ai) Make(AiReply reply)
        {
            var w = new Case1World();
            var ai = new FakeAi { Reply = reply };
            var alex = new AlexService(w.State, w.Case, w.Db.Alex, w.Evidence, ai);
            return (w, alex, ai);
        }

        static AiReply Reply(string text, params string[] lieIds) =>
            new() { reply = text, emotion = "confiado", usedLieIds = new List<string>(lieIds) };

        [Test]
        public void Offline_UsesScriptLines_AndNeverChangesTrust()
        {
            var w = new Case1World();
            int trust = w.Alex.Trust;
            var answer = w.Alex.AskAsync("¿Quién crees que fue?", CancellationToken.None).Result;
            Assert.AreEqual(w.Case.AlexLines.Crime.Text, answer);
            var press = w.Alex.AskAsync("¿Por qué tu archivo dio error?", CancellationToken.None).Result;
            Assert.AreEqual(w.Case.AlexLines.Press.Text, press);
            Assert.AreEqual(trust, w.Alex.Trust, "Preguntarle a ALEX no cambia la confianza");
            Assert.AreEqual("", w.Alex.AskAsync("   ", CancellationToken.None).Result, "Pregunta vacía: sin respuesta");
        }

        [Test]
        public void AiReply_IsUsed_AndRequestHasOnlyAlexContext()
        {
            var (w, alex, ai) = Make(Reply("Yo revisaría primero el balcón, detective."));
            w.CollectAndAnalyze("ev_c1_huella"); // ALEX lanza su afirmación sobre la ventana
            int trust = alex.Trust;
            var answer = alex.AskAsync("¿Qué opinas de la huella?", CancellationToken.None).Result;

            Assert.AreEqual("Yo revisaría primero el balcón, detective.", answer);
            Assert.AreEqual(trust, alex.Trust);
            var req = ai.LastRequest;
            Assert.IsTrue(req.IsAlex);
            Assert.AreEqual(w.Case.AlexTarget.DisplayName, req.AlexTarget, "El objetivo de ALEX sale de los datos del caso");
            Assert.IsTrue(req.AlexClaims.Any(c => c.StartsWith("lie_c1_alex_ventana") && c.EndsWith("[pendiente]")));
            Assert.IsTrue(req.AnalyzedEvidence.Any(e => e.StartsWith("ev_c1_huella")));
            Assert.AreEqual(w.Case.AllCharacters.Count(), req.Suspects.Count);
            Assert.AreEqual("Warm", req.AlexTone);
        }

        [Test]
        public void AiReply_ThatReusesARefutedClaim_IsReplacedByTheScript()
        {
            var (w, alex, _) = Make(Reply("Insisto: entraron por la ventana.", "lie_c1_alex_ventana"));
            w.CollectAndAnalyze("ev_c1_huella", "ev_c1_ventana");
            var claim = w.Lie("lie_c1_alex_ventana");
            Assert.IsTrue(alex.IsValidReply(Reply("x", "lie_c1_alex_ventana")), "Antes de refutarla puede usarla");
            Assert.IsTrue(alex.Question(claim, w.Ev("ev_c1_ventana")).Success);

            Assert.IsFalse(alex.IsValidReply(Reply("x", "lie_c1_alex_ventana")), "Refutada: no la puede sostener");
            Assert.IsFalse(alex.IsValidReply(Reply("x", "lie_inventada")), "Solo afirmaciones de ALEX ya hechas");
            Assert.IsFalse(alex.IsValidReply(Reply("")), "Respuesta vacía");
            var answer = alex.AskAsync("¿Entonces cómo entraron?", CancellationToken.None).Result;
            Assert.AreNotEqual("Insisto: entraron por la ventana.", answer);
        }

        [Test]
        public void NoAiReply_FallsBackToScript()
        {
            var (w, alex, _) = Make(null); // proxy apagado o error de red
            var answer = alex.AskAsync("¿Y esta evidencia?", CancellationToken.None).Result;
            Assert.AreEqual(w.Case.AlexLines.Evidence.Text, answer);
        }
    }
}
