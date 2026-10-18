using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using Caso404.Core;
using Caso404.Data;
using Caso404.Dialogue;
using Caso404.Evidence;
using Caso404.Interaction;
using Caso404.Player;
using Caso404.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Caso404.Tests
{
    /// <summary>
    /// Prueba de humo de c1_s1: carga la escena, recoge y analiza las 7 evidencias con el modo inspección,
    /// abre cada pantalla, descubre la mentira de Laura, refuta a ALEX y llega hasta dec_c1_s1.
    /// Si cualquier script registra un error, la prueba falla (LogAssert).
    /// </summary>
    public class Apartment404SmokeTest
    {
        string _savePath;
        string _backup;

        [SetUp]
        public void BackupSave()
        {
            _savePath = Path.Combine(Application.persistentDataPath, "save.json");
            _backup = File.Exists(_savePath) ? File.ReadAllText(_savePath) : null;
        }

        [TearDown]
        public void RestoreSave()
        {
            if (_backup != null) File.WriteAllText(_savePath, _backup);
            else if (File.Exists(_savePath)) File.Delete(_savePath);
        }

        [UnityTest]
        public IEnumerator SceneIsPlayableUntilClosingDecision()
        {
            yield return SceneManager.LoadSceneAsync("Apartamento404");
            yield return null;
            yield return null;

            var game = Object.FindAnyObjectByType<GameContext>();
            var ui = Object.FindAnyObjectByType<UIManager>();
            var inspection = Object.FindAnyObjectByType<InspectionController>();
            var player = Object.FindAnyObjectByType<PlayerContext>();
            Assert.IsNotNull(game?.State, "GameContext sin servicios");
            Assert.IsInstanceOf<MessageView>(ui.Top, "Debe abrir con la intro del caso");
            ui.CloseTop();
            ui.Remove(ui.Top);

            // Recoger las 7 evidencias con el modo inspección.
            var pickups = Object.FindObjectsByType<EvidencePickup>(FindObjectsSortMode.None);
            Assert.AreEqual(7, pickups.Length, "c1_s1 tiene 7 evidencias");
            foreach (var p in pickups)
            {
                player.RequestInspect(p.Evidence, p.gameObject);
                yield return null;
                Assert.IsInstanceOf<InspectionView>(ui.Top);
                Assert.IsTrue(inspection.Keep(), $"No se guardó {p.Evidence.Id}");
                yield return null;
            }
            Assert.AreEqual(7, game.State.State.foundEvidenceIds.Count);

            // Analizar con el expediente abierto (ejercita su refresco).
            var journal = Object.FindAnyObjectByType<JournalView>();
            journal.Open();
            yield return null;
            foreach (var p in pickups) Assert.IsTrue(game.Evidence.Analyze(p.Evidence).Success, p.Evidence.Id);
            yield return null;
            journal.Close();
            Assert.AreEqual(0, game.Evidence.UnanalyzedCount);

            // ALEX: lanzó sus dos afirmaciones de la escena; se refuta la de la ventana.
            Assert.IsTrue(game.State.IsAlexClaimRaised("lie_c1_alex_ventana"));
            Assert.IsTrue(game.State.IsAlexClaimRaised("lie_c1_alex_archivo"));
            var alexPanel = Object.FindAnyObjectByType<AlexPanelView>();
            alexPanel.Open();
            yield return null;
            var r = game.Alex.Question(game.Case.FindLie("lie_c1_alex_ventana"), game.Case.FindEvidence("ev_c1_ventana"));
            Assert.IsTrue(r.Success);
            yield return null;
            alexPanel.Close();

            // Laura: mentira de la hora → hablar con calma → cuaderno → decisión al cerrar el diálogo.
            var laura = Object.FindAnyObjectByType<CharacterNpc>();
            player.RequestTalk(laura.Profile, laura.Head);
            yield return null;
            Assert.IsInstanceOf<DialogueView>(ui.Top);
            var phone = game.Dialogue.GetPresentableItems().First(i => i.Evidence != null && i.Evidence.Id == "ev_c1_telefono");
            Assert.IsTrue(game.Dialogue.Present(laura.Profile, phone).ExposedLie);
            var calm = game.Dialogue.AskAsync(laura.Profile, AI.DialogueTopic.Calm, null, default);
            yield return new WaitUntil(() => calm.IsCompleted);
            Assert.IsTrue(game.State.KnowsFact("f_c1_laura_cuaderno"));
            ui.Top.Close();
            yield return null;
            Assert.IsInstanceOf<DecisionView>(ui.Top, "Al cerrar el diálogo se abre dec_c1_cuaderno");
            InvokeApply((DecisionView)ui.Top, game.Case.FindDecision("dec_c1_cuaderno").FindOption("B"));
            yield return null;
            Assert.IsTrue(game.State.HasFlag("flag_c1_cuaderno"));
            ui.Top.Close(); // texto de la nota de Elena
            yield return null;

            // Hipótesis y decisión de cierre.
            var hyp = Object.FindAnyObjectByType<HypothesisView>();
            hyp.Open();
            yield return null;
            var h = game.Scene.Hypothesis;
            Assert.IsTrue(game.Hypotheses.Submit(h.Questions[0], new[] { "llave" }));
            Assert.IsTrue(game.Hypotheses.Submit(h.Questions[1], new[] { "carlos", "laura", "desconocido" }));
            hyp.Close();
            var decisionView = Object.FindAnyObjectByType<DecisionView>();
            decisionView.Show(game.Scene.ClosingDecision);
            yield return null;
            InvokeApply(decisionView, game.Scene.ClosingDecision.FindOption("B"));
            yield return null;

            Assert.AreEqual("B", game.State.GetDecisionOption("dec_c1_s1"));
            Assert.IsTrue(game.State.IsSceneCompleted("c1_s1"));
            Assert.IsInstanceOf<MessageView>(ui.Top, "Sin c1_s2 construida, se muestra el fin de la demo");
            Assert.IsTrue(File.Exists(_savePath), "Se guarda al decidir y al cambiar de escena");
        }

        static void InvokeApply(DecisionView view, DecisionOption option)
        {
            var apply = typeof(DecisionView).GetMethod("Apply", BindingFlags.Instance | BindingFlags.NonPublic);
            apply.Invoke(view, new object[] { option });
        }
    }
}
