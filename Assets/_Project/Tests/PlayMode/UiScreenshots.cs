using System.Collections;
using System.IO;
using System.Linq;
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
    /// Herramienta (no es una prueba normal): guarda capturas de cada pantalla de UI en Logs/ui_*.png para revisarlas.
    /// Correr con: -runTests -testPlatform PlayMode -testFilter Caso404.Tests.UiScreenshots (con gráficos).
    /// </summary>
    [Explicit("Herramienta de capturas; se corre a mano")]
    public class UiScreenshots
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
        public IEnumerator CaptureAllScreens()
        {
            yield return SceneManager.LoadSceneAsync("MainMenu");
            yield return new WaitForSecondsRealtime(1.5f);
            yield return Shot("00_menu");
            var options = Object.FindAnyObjectByType<MainMenuView>().transform.Find("Options");
            options.gameObject.SetActive(true);
            yield return Shot("00b_opciones");
            options.gameObject.SetActive(false);

            Object.FindAnyObjectByType<MainMenuView>().StartNewGame();
            yield return new WaitForSecondsRealtime(2f);
            yield return Shot("00c_carta_c1_escribiendo");
            CaseLetterView.Current.Complete();
            yield return null;
            yield return Shot("00d_carta_c1");
            CaseLetterView.Current.Begin();
            yield return WaitScene("c1_s1");
            yield return new WaitForSecondsRealtime(1f);
            yield return Shot("01_intro");
            var ui = Object.FindAnyObjectByType<UIManager>();
            ((MessageView)ui.Top).Choose("Empezar");
            yield return new WaitForSecondsRealtime(0.5f);
            yield return Shot("02_hud");

            var game = Object.FindAnyObjectByType<GameContext>();
            var player = Object.FindAnyObjectByType<PlayerContext>();
            var pickups = Object.FindObjectsByType<EvidencePickup>(FindObjectsSortMode.None);
            var huella = pickups.First(p => p.Evidence.Id == "ev_c1_huella");
            player.RequestInspect(huella.Evidence, huella.gameObject);
            yield return new WaitForSecondsRealtime(0.4f);
            yield return Shot("03_inspeccion");
            Object.FindAnyObjectByType<InspectionController>().Keep();
            foreach (var p in pickups) game.Evidence.TryCollect(p.Evidence);
            game.Evidence.Analyze(game.Case.FindEvidence("ev_c1_huella"));
            game.Evidence.Analyze(game.Case.FindEvidence("ev_c1_camara_pasillo"));
            yield return new WaitForSecondsRealtime(0.3f);

            var journal = Object.FindAnyObjectByType<JournalView>();
            journal.Open();
            yield return new WaitForSecondsRealtime(0.4f);
            yield return Shot("04_expediente");
            journal.Close();

            var alex = Object.FindAnyObjectByType<AlexPanelView>();
            alex.Open();
            yield return new WaitForSecondsRealtime(0.4f);
            yield return Shot("05_alex");
            alex.Close();

            var laura = Object.FindAnyObjectByType<CharacterNpc>();
            player.RequestTalk(laura.Profile, laura.Head);
            yield return new WaitForSecondsRealtime(0.3f);
            game.Evidence.Analyze(game.Case.FindEvidence("ev_c1_telefono"));
            var line = game.Dialogue.Present(laura.Profile, game.Dialogue.GetPresentableItems().First(i => i.Evidence != null && i.Evidence.Id == "ev_c1_telefono"));
            yield return new WaitForSecondsRealtime(3f);
            yield return Shot("06_dialogo");
            ui.Top.Close();
            yield return new WaitForSecondsRealtime(0.3f);

            var hyp = Object.FindAnyObjectByType<HypothesisView>();
            hyp.Open();
            yield return new WaitForSecondsRealtime(0.4f);
            yield return Shot("07_hipotesis");
            hyp.Close();

            var decision = Object.FindAnyObjectByType<DecisionView>();
            decision.Show(game.Scene.ClosingDecision);
            yield return new WaitForSecondsRealtime(0.4f);
            yield return Shot("08_decision");
            decision.Close();

            var accusation = Object.FindAnyObjectByType<AccusationView>();
            accusation.Open(); // en c1_s1 no se acusa, pero sirve para ver el diseño
            yield return new WaitForSecondsRealtime(0.4f);
            yield return Shot("09_acusacion");
            accusation.Close();

            var result = Decisions.EndingEvaluator.Evaluate(game.Case, "carlos", new[] { "ev_c1_huella" }, 0, game.State);
            var ending = Object.FindAnyObjectByType<EndingView>();
            ending.Show(result, null);
            yield return new WaitForSecondsRealtime(0.4f);
            yield return Shot("10_final");
            ending.Close();
        }

        IEnumerator WaitScene(string sceneId)
        {
            for (float t = 0; t < 20f; t += Time.unscaledDeltaTime)
            {
                var g = Object.FindAnyObjectByType<GameContext>();
                if (g != null && g.Scene != null && g.Scene.Id == sceneId) yield break;
                yield return null;
            }
        }

        /// <summary>Pasa los Canvas a modo cámara, renderiza a una textura y guarda el PNG.</summary>
        static IEnumerator Shot(string name)
        {
            var camGo = new GameObject("ShotCamera");
            var cam = camGo.AddComponent<Camera>();
            var main = Camera.main;
            if (main != null) cam.CopyFrom(main);
            cam.depth = 100;
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c => c.isRootCanvas).ToList();
            var modes = canvases.Select(c => c.renderMode).ToList();
            foreach (var c in canvases.OrderBy(c => c.sortingOrder))
            {
                c.renderMode = RenderMode.ScreenSpaceCamera;
                c.worldCamera = cam;
                c.planeDistance = 0.3f + 0.01f * (100 - c.sortingOrder) / 100f;
            }
            var rt = new RenderTexture(1920, 1080, 24);
            cam.targetTexture = rt;
            yield return null;
            Canvas.ForceUpdateCanvases();
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            File.WriteAllBytes($"Logs/ui_{name}.png", tex.EncodeToPNG());
            for (int i = 0; i < canvases.Count; i++) { canvases[i].renderMode = modes[i]; canvases[i].worldCamera = null; }
            cam.targetTexture = null;
            Object.Destroy(tex);
            rt.Release();
            Object.Destroy(camGo);
        }
    }
}
