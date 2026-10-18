using System.Linq;
using System.Threading;
using Caso404.AI;
using Caso404.Data;
using Caso404.Dialogue;
using NUnit.Framework;

namespace Caso404.Tests
{
    /// <summary>Reglas deterministas del diálogo (sin IA): mentiras, secretos y evidencias entregadas.</summary>
    public class DialogueTests
    {
        static DialogueService Make(Case1World w) => new(w.State, w.Evidence, new OfflineAiClient(), w.Case);
        static CharacterProfile Who(Case1World w, string id) => w.Case.FindSuspect(id);
        static PresentableItem Item(DialogueService d, string evidenceId) => d.GetPresentableItems().First(i => i.Evidence != null && i.Evidence.Id == evidenceId);

        [Test]
        public void Laura_GivesNotebookOnlyAfterHourLieExposedAndCalm()
        {
            var w = new Case1World();
            var d = Make(w);
            var laura = Who(w, "laura");
            d.Begin(laura);
            Assert.IsFalse(d.CanUseCalmApproach(laura));

            w.CollectAndAnalyze("ev_c1_telefono");
            var line = d.Present(laura, Item(d, "ev_c1_telefono"));
            Assert.IsTrue(line.ExposedLie);
            Assert.IsTrue(w.State.IsLieExposed("lie_c1_laura_hora"));
            Assert.IsTrue(w.State.KnowsFact("f_c1_laura_golpe"));

            SecretEntry revealed = null;
            d.SecretRevealed += (_, s) => revealed = s;
            Assert.IsTrue(d.CanUseCalmApproach(laura));
            d.AskAsync(laura, DialogueTopic.Calm, null, CancellationToken.None).Wait();
            Assert.IsNotNull(revealed);
            Assert.AreEqual("dec_c1_cuaderno", revealed.TriggersDecision.Id);
            Assert.IsTrue(w.State.KnowsFact("f_c1_laura_cuaderno"));
        }

        [Test]
        public void Laura_PressingBlocksCalmInThatConversation()
        {
            var w = new Case1World();
            var d = Make(w);
            var laura = Who(w, "laura");
            w.CollectAndAnalyze("ev_c1_telefono");
            d.Begin(laura);
            d.Present(laura, Item(d, "ev_c1_telefono"));
            d.AskAsync(laura, DialogueTopic.Press, null, CancellationToken.None).Wait();
            Assert.IsFalse(d.CanUseCalmApproach(laura));
            d.Begin(laura); // nueva conversación
            Assert.IsTrue(d.CanUseCalmApproach(laura));
        }

        [Test]
        public void Andres_HandsPhoneWhenRefutedWithSolidEvidence()
        {
            var w = new Case1World();
            var d = Make(w);
            var andres = Who(w, "andres");
            w.CollectAndAnalyze("ev_c1_llavero");
            w.State.SetCurrentScene("c1", "c1_s3");
            d.Begin(andres);
            Assert.IsTrue(d.Present(andres, Item(d, "ev_c1_llavero")).ExposedLie);
            Assert.IsFalse(w.Evidence.IsFound(w.Ev("ev_c1_celular_andres")), "En el sótano todavía no entrega el celular (GUION: se obtiene en la Comisaría)");
            w.State.SetCurrentScene("c1", "c1_s4");
            d.Begin(andres);
            d.Present(andres, Item(d, "ev_c1_llavero"));
            Assert.IsTrue(w.Evidence.IsFound(w.Ev("ev_c1_celular_andres")), "En la Comisaría lo entrega aunque la mentira ya estuviera descubierta");
        }

        [Test]
        public void Hernan_TestimonyAboutAndresAlsoRefutesAndresButGivesNoPhone()
        {
            var w = new Case1World();
            var d = Make(w);
            w.CollectAndAnalyze("ev_c1_recibo_tienda");
            d.Begin(Who(w, "hernan"));
            d.Present(Who(w, "hernan"), Item(d, "ev_c1_recibo_tienda"));
            Assert.IsTrue(w.State.KnowsFact("f_c1_hernan_andres"));

            var andres = Who(w, "andres");
            d.Begin(andres);
            var testimony = d.GetPresentableItems().First(i => i.FactId == "f_c1_hernan_andres");
            Assert.IsTrue(d.Present(andres, testimony).ExposedLie);
            Assert.IsFalse(w.Evidence.IsFound(w.Ev("ev_c1_celular_andres")), "Un testimonio no es prueba sólida");
        }

        [Test]
        public void OfflineAnswer_OtherPersonRevealsLauraFacts()
        {
            var w = new Case1World();
            var d = Make(w);
            var laura = Who(w, "laura");
            d.Begin(laura);
            var line = d.AskAsync(laura, DialogueTopic.OtherPerson, null, CancellationToken.None).Result;
            StringAssert.Contains("Carlos vino esa noche", line.Text);
            Assert.IsTrue(w.State.KnowsFact("f_c1_laura_gritos"));
        }

        [Test]
        public void Validator_RejectsUnknownIdsAndLockedSecrets()
        {
            var w = new Case1World();
            var laura = Who(w, "laura");
            Assert.IsTrue(AiReplyValidator.IsValid(new AiReply { reply = "Hola", emotion = "neutral" }, laura, w.State));
            Assert.IsFalse(AiReplyValidator.IsValid(new AiReply { reply = "Hola", emotion = "feliz" }, laura, w.State));
            var badFact = new AiReply { reply = "x", emotion = "neutral" };
            badFact.usedFactIds.Add("f_c1_carlos_cafe");
            Assert.IsFalse(AiReplyValidator.IsValid(badFact, laura, w.State), "Laura no conoce ese hecho");
            var secret = new AiReply { reply = "x", emotion = "triste" };
            secret.revealedSecretIds.Add("f_c1_laura_cuaderno");
            Assert.IsFalse(AiReplyValidator.IsValid(secret, laura, w.State), "El secreto aún está bloqueado");
        }
    }
}
