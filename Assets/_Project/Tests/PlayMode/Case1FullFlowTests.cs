using System.Collections;
using System.Collections.Generic;
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
    /// Recorre el caso 1 completo como lo haría el jugador, desde el menú principal hasta cada final:
    /// inspeccionar y guardar (modo inspección), analizar, cuestionar a ALEX, hablar, responder hipótesis, decidir,
    /// cambiar de escena (con fundido) y acusar. Falla si cualquier script registra un error (LogAssert).
    /// Necesita gráficos (no corre con -nographics) por la RenderTexture del modo inspección.
    /// </summary>
    public class Case1FullFlowTests
    {
        const float SceneTimeout = 20f;
        string _savePath;
        string _backup;

        GameContext Game => Object.FindAnyObjectByType<GameContext>();
        UIManager Ui => Object.FindAnyObjectByType<UIManager>();
        GameUIController Controller => Object.FindAnyObjectByType<GameUIController>();

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
        public IEnumerator TrueEnding_InvestigatingEverything()
        {
            yield return NewGame();
            yield return PlayApartment(refuteAlex: true, decision: "C");
            yield return WaitScene("c1_s3");
            yield return PlayPorteria(refuteAlex: true, expectOverol: true);
            Assert.IsTrue(Choose("Café La Esquina"));
            yield return WaitScene("c1_s2");
            yield return PlayCafe(expectCarlos: true, refuteAlex: true);
            Assert.IsTrue(Choose("Comisaría"));
            yield return WaitScene("c1_s4");
            yield return GetAndresPhone();
            Assert.IsTrue(Game.State.HasFlag("flag_c1_pista_nx"), "El análisis profundo del celular activa flag_c1_pista_nx");
            yield return Accuse("andres", "end_c1_verdadero", "ev_c1_llavero", "ev_c1_overol", "ev_c1_celular_andres");
            Assert.IsTrue(Game.State.KnowsFact("f_c1_andres_pago"), "En el final verdadero Andrés confiesa el pago de Soporte NX");
            yield return ContinueToCase2();
        }

        [UnityTest]
        public IEnumerator TrueEnding_EvenIfCarlosWasDetained()
        {
            yield return NewGame();
            yield return PlayApartment(refuteAlex: true, decision: "A");
            Assert.IsTrue(Choose("Café La Esquina"), "Tras detener a Carlos se elige entre el café y la portería");
            yield return WaitScene("c1_s2");
            yield return PlayCafe(expectCarlos: false, refuteAlex: false);
            Assert.IsTrue(Choose("Portería y sótano del Edificio Mirador"));
            yield return WaitScene("c1_s3");
            yield return PlayPorteria(refuteAlex: true, expectOverol: false);
            Assert.IsTrue(Choose("Comisaría"));
            yield return WaitScene("c1_s4");
            yield return Accuse("andres", "end_c1_verdadero", "ev_c1_llavero", "ev_c1_camara_original", "ev_c1_ventana");
            yield return BackToMenu();
        }

        [UnityTest]
        public IEnumerator IncompleteEnding_AlexNeverQuestioned()
        {
            yield return NewGame();
            yield return PlayApartment(refuteAlex: false, decision: "B");
            yield return WaitScene("c1_s2");
            yield return PlayCafe(expectCarlos: true, refuteAlex: false);
            Assert.IsTrue(Choose("Portería y sótano del Edificio Mirador"));
            yield return WaitScene("c1_s3");
            yield return PlayPorteria(refuteAlex: false, expectOverol: true);
            Assert.IsTrue(Choose("Comisaría"));
            yield return WaitScene("c1_s4");
            yield return Accuse("andres", "end_c1_incompleto", "ev_c1_llavero", "ev_c1_camara_original", "ev_c1_ventana");
            Assert.AreEqual("andres_sin_alex", Game.State.State.accusations.Single().variantId);
            yield return BackToMenu();
        }

        [UnityTest]
        public IEnumerator DeceptionEnding_FollowingAlex()
        {
            yield return NewGame();
            yield return PlayApartment(refuteAlex: false, decision: "A", acceptAlex: true);
            Assert.IsTrue(Choose("Portería y sótano del Edificio Mirador"));
            yield return WaitScene("c1_s3");
            yield return PlayPorteria(refuteAlex: false, expectOverol: false);
            Assert.IsTrue(Choose("Café La Esquina"));
            yield return WaitScene("c1_s2");
            yield return PlayCafe(expectCarlos: false, refuteAlex: false);
            Assert.IsTrue(Choose("Comisaría"));
            yield return WaitScene("c1_s4");
            yield return Accuse("carlos", "end_c1_engano", "ev_c1_huella", "ev_c1_colilla", "ev_c1_registro_porteria");
            yield return BackToMenu();
        }

        // ================= Caso 2 =================

        [UnityTest]
        public IEnumerator Case2_SecretEnding_WithTraceFromCase1()
        {
            yield return ContinueAtCase2(new[] { "end_c1_verdadero", "flag_c1_pista_nx" }, trust: 40);
            yield return PlayServidores(decision: "C");
            yield return WaitScene("c2_s3");
            yield return PlayCaseta(protectInes: true, expectBackup: true);
            Assert.IsTrue(Choose("Piso ejecutivo — Torre Nexo"));
            yield return WaitScene("c2_s2");
            yield return PlayPisoEjecutivo(expectSebastian: true);
            Assert.IsTrue(Choose("Comisaría"));
            yield return WaitScene("c2_s4");
            Assert.IsTrue(Object.FindObjectsByType<CharacterNpc>(FindObjectsSortMode.None).All(n => n.Profile.CaseId == "c2"),
                "En la Comisaría del caso 2 solo aparece el elenco del caso 2");
            yield return Talk("elena", d => Assert.IsNotEmpty(d.Greeting(Profile("elena"))));
            yield return Accuse("valeria", "end_c2_secreto", "ev_c2_tarjeta_valeria", "ev_c2_backup", "ev_c2_consola_valeria");
            // Tras el interrogatorio a ALEX: decisión final, resultado y créditos.
            Object.FindAnyObjectByType<EndingView>().Finish();
            yield return null;
            Assert.IsInstanceOf<DecisionView>(Ui.Top, "El final secreto abre dec_c2_alex");
            yield return Decide(Game.Case.FindDecision("dec_c2_alex"), "B");
            Assert.IsTrue(Choose("Continuar"));
            yield return null;
            var ending = Object.FindAnyObjectByType<EndingView>();
            Assert.AreSame(ending, Ui.Top, "Créditos: ERROR 404: CONFIANZA NO ENCONTRADA.");
            ending.Finish();
            yield return null;
            Assert.IsTrue(Choose("Menú principal"));
        }

        [UnityTest]
        public IEnumerator Case2_DeceptionEnding_TimeLost()
        {
            yield return ContinueAtCase2(new[] { "end_c1_engano" }, trust: 75);
            yield return PlayServidores(decision: "A");
            Assert.IsTrue(Choose("Caseta de seguridad y parqueadero"));
            yield return WaitScene("c2_s3");
            yield return PlayCaseta(protectInes: true, expectBackup: false);
            Assert.IsTrue(Choose("Piso ejecutivo — Torre Nexo"));
            yield return WaitScene("c2_s2");
            yield return PlayPisoEjecutivo(expectSebastian: false);
            Assert.IsTrue(Choose("Comisaría"));
            yield return WaitScene("c2_s4");
            yield return Accuse("sebastian", "end_c2_engano", "ev_c2_nota", "ev_c2_tarjeta_valeria", "ev_c2_sensor_humo");
            yield return BackToMenu();
        }

        /// <summary>Simula una partida guardada al terminar el caso 1 y la continúa desde el menú.</summary>
        IEnumerator ContinueAtCase2(string[] flags, int trust)
        {
            var state = new GameState { currentCaseId = "c2", currentSceneId = "c2_s1", alexTrust = trust };
            state.flags.AddRange(flags);
            new Save.SaveService().Save(state);
            yield return SceneManager.LoadSceneAsync(GameUIController.MainMenuScene);
            yield return null;
            Object.FindAnyObjectByType<MainMenuView>().ContinueGame();
            yield return WaitScene("c2_s1");
            Assert.IsTrue(((MessageView)Ui.Top).Choose("Empezar"));
        }

        IEnumerator PlayServidores(string decision)
        {
            yield return CollectAndAnalyzeAll(5);
            var g = Game;
            Assert.IsTrue(g.Alex.Question(g.Case.FindLie("lie_c2_alex_falla"), g.Case.FindEvidence("ev_c2_panel_gas")).Success);
            Assert.IsTrue(g.Alex.Question(g.Case.FindLie("lie_c2_alex_nota"), g.Case.FindEvidence("ev_c2_nota")).Success);
            AnswerHypothesis(new Dictionary<string, string[]> { ["hyp_c2_s1_q1"] = new[] { "distancia" } });
            yield return Decide(g.Scene.ClosingDecision, decision);
            if (decision == "A") Assert.IsTrue(((MessageView)Ui.Top).Choose("Continuar"));
        }

        IEnumerator PlayCaseta(bool protectInes, bool expectBackup)
        {
            yield return CollectAndAnalyzeAll(3);
            var g = Game;
            Assert.IsTrue(g.Evidence.RunExtraAnalysis(g.Case.FindEvidence("ev_c2_camara_parqueadero")).Success, "Laboratorio externo");
            Assert.IsTrue(g.Alex.Question(g.Case.FindLie("lie_c2_alex_placas"), g.Case.FindEvidence("ev_c2_camara_parqueadero")).Success);
            yield return Talk("ines", d => Assert.IsTrue(d.Present(Profile("ines"), Item(d, "ev_c2_libro_rondas")).ExposedLie));
            Assert.IsInstanceOf<DecisionView>(Ui.Top, "Al refutar a Inés se abre dec_c2_ines");
            yield return Decide(g.Case.FindDecision("dec_c2_ines"), protectInes ? "A" : "B");
            Assert.IsTrue(((MessageView)Ui.Top).Choose("Continuar"));
            var backup = g.Case.FindEvidence("ev_c2_backup");
            Assert.AreEqual(expectBackup, g.Evidence.IsFound(backup), expectBackup ? "Inés entrega la copia espejo" : "Con tiempo perdido la copia ya fue borrada");
            if (expectBackup) Assert.IsTrue(g.Evidence.Analyze(backup).Success);
            AnswerHypothesis(new Dictionary<string, string[]> { ["hyp_c2_s3_q1"] = new[] { "apagada" } });
            Controller.FinishScene();
            yield return null;
        }

        IEnumerator PlayPisoEjecutivo(bool expectSebastian)
        {
            bool sebHere = Object.FindObjectsByType<CharacterNpc>(FindObjectsSortMode.None).Any(n => n.Profile.Id == "sebastian");
            Assert.AreEqual(expectSebastian, sebHere, "Sebastián detenido no está en su escritorio");
            yield return CollectAndAnalyzeAll(6);
            var g = Game;
            Assert.IsTrue(g.Evidence.RunExtraAnalysis(g.Case.FindEvidence("ev_c2_portatil_tomas"), "LUCERO").Success);
            Assert.IsTrue(g.State.HasFlag("flag_c2_directiva"));
            if (expectSebastian)
                yield return Talk("sebastian", d => StringAssert.Contains("El Muelle", d.Present(Profile("sebastian"), Item(d, "ev_c2_chat_kairos")).Text));
            AnswerHypothesis(new Dictionary<string, string[]> { ["hyp_c2_s2_q1"] = new[] { "valeria" } });
            Controller.FinishScene();
            yield return null;
        }

        // ================= Escenas =================

        IEnumerator NewGame()
        {
            yield return SceneManager.LoadSceneAsync(GameUIController.MainMenuScene);
            yield return null;
            Object.FindAnyObjectByType<MainMenuView>().StartNewGame();
            Assert.IsNotNull(CaseLetterView.Current, "Antes del caso 1 se muestra la carta de la Comisaria Lozano");
            CaseLetterView.Current.Begin();
            yield return WaitScene("c1_s1");
            Assert.IsInstanceOf<MessageView>(Ui.Top, "La partida nueva abre con la intro del caso");
            Assert.IsTrue(((MessageView)Ui.Top).Choose("Empezar"));
        }

        IEnumerator PlayApartment(bool refuteAlex, string decision, bool acceptAlex = false)
        {
            yield return CollectAndAnalyzeAll(7);
            var g = Game;
            Assert.IsTrue(g.State.IsAlexClaimRaised("lie_c1_alex_ventana"));
            Assert.IsTrue(g.State.IsAlexClaimRaised("lie_c1_alex_archivo"));
            if (acceptAlex) g.Alex.Accept(g.Case.FindLie("lie_c1_alex_ventana"));
            if (refuteAlex) Assert.IsTrue(g.Alex.Question(g.Case.FindLie("lie_c1_alex_ventana"), g.Case.FindEvidence("ev_c1_puerta")).Success);

            // Laura: el teléfono refuta su hora → hablar con calma → cuaderno → decisión al cerrar el diálogo.
            yield return Talk("laura", d =>
            {
                Assert.IsTrue(d.Present(Profile("laura"), Item(d, "ev_c1_telefono")).ExposedLie);
                var t = d.AskAsync(Profile("laura"), AI.DialogueTopic.Calm, null, default);
                t.Wait();
            });
            Assert.IsInstanceOf<DecisionView>(Ui.Top, "Se abre dec_c1_cuaderno");
            yield return Decide(g.Case.FindDecision("dec_c1_cuaderno"), "B");
            Assert.IsTrue(((MessageView)Ui.Top).Choose("Continuar"));

            AnswerHypothesis(new Dictionary<string, string[]>
            {
                ["hyp_c1_s1_q1"] = new[] { "llave" },
                ["hyp_c1_s1_q2"] = new[] { "carlos", "laura", "desconocido" },
            });
            Assert.IsTrue(g.State.HasFlag("flag_c1_h_llave"));
            yield return Decide(g.Scene.ClosingDecision, decision);
            if (decision == "A") Assert.IsTrue(((MessageView)Ui.Top).Choose("Continuar"));
        }

        IEnumerator PlayCafe(bool expectCarlos, bool refuteAlex)
        {
            bool carlosHere = Object.FindObjectsByType<CharacterNpc>(FindObjectsSortMode.None).Any(n => n.Profile.Id == "carlos");
            Assert.AreEqual(expectCarlos, carlosHere, expectCarlos ? "Carlos debe estar en el café" : "Carlos detenido no aparece en el café");
            yield return CollectAndAnalyzeAll(3);
            var g = Game;
            Assert.IsTrue(g.State.IsAlexClaimRaised("lie_c1_alex_horacafe"));
            if (refuteAlex) Assert.IsTrue(g.Alex.Question(g.Case.FindLie("lie_c1_alex_horacafe"), g.Case.FindEvidence("ev_c1_recibo_cafe")).Success);
            if (expectCarlos)
                yield return Talk("carlos", d => Assert.IsTrue(d.Present(Profile("carlos"), Item(d, "ev_c1_huella")).ExposedLie));
            AnswerHypothesis(new Dictionary<string, string[]> { ["hyp_c1_s2_q1"] = new[] { "no" } });
            Controller.FinishScene();
            yield return null;
        }

        IEnumerator PlayPorteria(bool refuteAlex, bool expectOverol)
        {
            var pickups = Object.FindObjectsByType<EvidencePickup>(FindObjectsSortMode.None);
            Assert.AreEqual(expectOverol, pickups.Any(p => p.Evidence.Id == "ev_c1_overol"), "El overol solo aparece si Carlos no fue detenido");
            Assert.AreEqual(!expectOverol, pickups.Any(p => p.Evidence.Id == "ev_c1_cenizas"), "Las cenizas solo aparecen si Carlos fue detenido");
            yield return CollectAndAnalyzeAll(6); // 7 objetos (la grabación original no tiene objeto) y solo uno de overol/cenizas está activo
            var g = Game;
            Assert.IsTrue(g.State.IsEvidenceAnalyzed("ev_c1_camara_original"), "El DVR desbloquea la grabación original");
            if (refuteAlex)
            {
                Assert.IsTrue(g.Alex.Question(g.Case.FindLie("lie_c1_alex_archivo"), g.Case.FindEvidence("ev_c1_camara_original")).Success);
                if (expectOverol) Assert.IsTrue(g.Alex.Question(g.Case.FindLie("lie_c1_alex_overol"), g.Case.FindEvidence("ev_c1_overol")).Success);
            }
            yield return Talk("hernan", d =>
            {
                Assert.IsTrue(d.AvailableExtraTopics(Profile("hernan")).Any(), "Con flag_c1_h_llave aparece la llave maestra");
                Assert.IsTrue(d.Present(Profile("hernan"), Item(d, "ev_c1_recibo_tienda")).ExposedLie);
            });
            Assert.IsTrue(g.State.KnowsFact("f_c1_hernan_andres"));
            AnswerHypothesis(new Dictionary<string, string[]>
            {
                ["hyp_c1_s3_q1"] = new[] { "andres" },
                ["hyp_c1_s3_q2"] = new[] { "simular" },
            });
            Controller.FinishScene();
            yield return null;
        }

        IEnumerator GetAndresPhone()
        {
            yield return Talk("andres", d => d.Present(Profile("andres"), Item(d, "ev_c1_llavero")));
            var g = Game;
            var phone = g.Case.FindEvidence("ev_c1_celular_andres");
            Assert.IsTrue(g.Evidence.IsFound(phone), "Andrés entrega su celular en la Comisaría");
            Assert.IsTrue(g.Evidence.Analyze(phone).Success);
            Assert.IsTrue(g.Evidence.RunExtraAnalysis(phone).Success);
        }

        IEnumerator Accuse(string suspect, string expectedEnding, params string[] proofs)
        {
            var accusation = Object.FindAnyObjectByType<AccusationView>();
            Object.FindAnyObjectByType<PlayerContext>().RequestAccusation();
            yield return null;
            Assert.AreSame(accusation, Ui.Top, "El expediente para la Fiscalía abre la acusación");
            var result = accusation.Accuse(suspect, proofs);
            Assert.IsNotNull(result, "Acusación inválida");
            yield return null;
            var ending = Object.FindAnyObjectByType<EndingView>();
            Assert.AreSame(ending, Ui.Top);
            Assert.AreEqual(expectedEnding, ending.ShownEndingId);
        }

        /// <summary>Tras el final del caso 1 empieza el caso 2 (c2_s1) conservando flags y alexTrust.</summary>
        IEnumerator ContinueToCase2()
        {
            int trust = Game.State.AlexTrust;
            Object.FindAnyObjectByType<EndingView>().Finish();
            yield return null;
            Assert.IsTrue(Choose("Comenzar «Protocolo 404»"), "Tras el caso 1 se ofrece empezar el caso 2");
            Assert.IsNotNull(CaseLetterView.Current, "Antes del caso 2 se muestra su carta");
            CaseLetterView.Current.Begin();
            yield return WaitScene("c2_s1");
            var g = Game;
            Assert.AreEqual("c2", g.Case.Id);
            Assert.IsTrue(g.State.HasFlag("end_c1_verdadero"), "El final del caso 1 pasa al caso 2");
            Assert.IsTrue(g.State.HasFlag("flag_c1_pista_nx"));
            Assert.AreEqual(trust, g.State.AlexTrust, "alexTrust pasa del caso 1 al caso 2");
            Assert.IsTrue(((MessageView)Ui.Top).Choose("Empezar"), "Intro del caso 2");
            yield return CollectAndAnalyzeAll(5);
            Assert.IsTrue(g.State.IsAlexClaimRaised("lie_c2_alex_falla"));
            Assert.IsTrue(g.State.IsAlexClaimRaised("lie_c2_alex_nota"));
            Assert.IsTrue(g.Alex.Question(g.Case.FindLie("lie_c2_alex_falla"), g.Case.FindEvidence("ev_c2_sensor_humo")).Success);
            Assert.IsTrue(g.Alex.Question(g.Case.FindLie("lie_c2_alex_nota"), g.Case.FindEvidence("ev_c2_nota")).Success);
            AnswerHypothesis(new Dictionary<string, string[]> { ["hyp_c2_s1_q1"] = new[] { "distancia" } });
            yield return Decide(g.Scene.ClosingDecision, "B");
            Assert.IsTrue(g.State.IsSceneCompleted("c2_s1"));
            yield return WaitScene("c2_s2"); // dec_c2_s1 B lleva a la oficina de Tomás
        }

        IEnumerator BackToMenu()
        {
            Object.FindAnyObjectByType<EndingView>().Finish(); // tras "Tres meses después..."
            yield return null;
            Assert.IsTrue(Choose("Menú principal"));
            float t = 0;
            while (SceneManager.GetActiveScene().name != GameUIController.MainMenuScene && t < SceneTimeout) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.AreEqual(GameUIController.MainMenuScene, SceneManager.GetActiveScene().name);
        }

        // ================= Acciones del jugador =================

        IEnumerator CollectAndAnalyzeAll(int expectedPickups)
        {
            var player = Object.FindAnyObjectByType<PlayerContext>();
            var inspection = Object.FindAnyObjectByType<InspectionController>();
            var g = Game;
            var pickups = Object.FindObjectsByType<EvidencePickup>(FindObjectsSortMode.None);
            Assert.AreEqual(expectedPickups, pickups.Length, $"Evidencias en {g.Scene.Id}");
            foreach (var p in pickups)
            {
                player.RequestInspect(p.Evidence, p.gameObject);
                yield return null;
                Assert.IsInstanceOf<InspectionView>(Ui.Top);
                if (p.Evidence.HasHiddenDetail) g.Evidence.MarkHiddenDetailSeen(p.Evidence); // el jugador gira el objeto
                Assert.IsTrue(inspection.Keep(), $"No se guardó {p.Evidence.Id}");
                yield return null;
            }
            var journal = Object.FindAnyObjectByType<JournalView>();
            journal.Open();
            yield return null;
            // Se repite porque analizar el DVR agrega una evidencia nueva (la grabación original).
            EvidenceData next;
            while ((next = g.Evidence.FoundEvidence.FirstOrDefault(e => g.Evidence.CanAnalyze(e, out _))) != null)
                Assert.IsTrue(g.Evidence.Analyze(next).Success, next.Id);
            yield return null;
            journal.Close();
        }

        IEnumerator Talk(string characterId, System.Action<DialogueService> actions)
        {
            var npc = Object.FindObjectsByType<CharacterNpc>(FindObjectsSortMode.None).First(n => n.Profile.Id == characterId);
            Object.FindAnyObjectByType<PlayerContext>().RequestTalk(npc.Profile, npc.Head);
            yield return null;
            Assert.IsInstanceOf<DialogueView>(Ui.Top);
            actions(Game.Dialogue);
            yield return null;
            Ui.Top.Close();
            yield return null;
        }

        void AnswerHypothesis(Dictionary<string, string[]> answers)
        {
            var g = Game;
            foreach (var q in g.Scene.Hypothesis.Questions)
                Assert.IsTrue(g.Hypotheses.Submit(q, answers[q.Id]), $"Respuesta incorrecta en {q.Id}");
            Assert.IsTrue(g.Hypotheses.IsComplete(g.Scene.Hypothesis));
        }

        IEnumerator Decide(DecisionData decision, string optionId)
        {
            var view = Object.FindAnyObjectByType<DecisionView>();
            if (Ui.Top != view) view.Show(decision);
            yield return null;
            var apply = typeof(DecisionView).GetMethod("Apply", BindingFlags.Instance | BindingFlags.NonPublic);
            apply.Invoke(view, new object[] { decision.FindOption(optionId) });
            yield return null;
            Assert.AreEqual(optionId, Game.State.GetDecisionOption(decision.Id));
        }

        bool Choose(string label) => Ui.Top is MessageView m && m.Choose(label);

        IEnumerator WaitScene(string sceneId)
        {
            float t = 0;
            while (t < SceneTimeout)
            {
                var g = Game;
                if (g != null && g.Scene != null && g.Scene.Id == sceneId && g.State != null && Ui != null) break;
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            Assert.AreEqual(sceneId, Game?.Scene?.Id, $"No se cargó la escena {sceneId}");
            yield return null;
            yield return null;
        }

        CharacterProfile Profile(string id) => Game.Case.FindCharacter(id);

        static PresentableItem Item(DialogueService d, string evidenceId) =>
            d.GetPresentableItems().First(i => i.Evidence != null && i.Evidence.Id == evidenceId);
    }
}
