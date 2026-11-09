using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Caso404.Core;
using Caso404.Interaction;
using Caso404.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Caso404.Tests
{
    /// <summary>
    /// Pruebas de controles reales: Escape abre la pausa (con un teclado simulado del Input System) y cada objeto
    /// interactivo de cada escena se puede apuntar tanto de cerca como a un par de metros.
    /// </summary>
    public class GameplayInputTests
    {
        const float SceneTimeout = 20f;
        string _savePath;
        string _backup;

        UIManager Ui => Object.FindAnyObjectByType<UIManager>();

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
        public IEnumerator Escape_OpensAndClosesPause()
        {
            yield return Enter("c1", "c1_s1");
            // En modo batch no hay ventana con foco: sin esto el Input System descarta el teclado simulado.
            var settings = InputSystem.settings;
            var oldBackground = settings.backgroundBehavior;
            var oldEditor = settings.editorInputBehaviorInPlayMode;
            settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var kb = InputSystem.AddDevice<Keyboard>();
            try
            {
                Assert.IsNull(Ui.Top, "Tras la intro no queda ningún panel abierto");
                int cancels = 0;
                Object.FindAnyObjectByType<Caso404.Player.InputReader>().CancelPressed += () => cancels++;
                yield return Press(kb, Key.Escape);
                Assert.AreEqual(1, cancels, "La tecla Escape llega al InputReader una vez");
                var pause = Ui.Top as MessageView;
                Assert.IsNotNull(pause, "Escape abre el menú de pausa");
                CollectionAssert.Contains(pause.ButtonLabels.ToList(), "Guardar y salir al menú");
                yield return Press(kb, Key.Escape);
                yield return null;
                Assert.IsNull(Ui.Top, "Escape otra vez cierra la pausa");
                yield return Press(kb, Key.Escape);
                Assert.IsInstanceOf<MessageView>(Ui.Top, "Y se puede volver a abrir");
            }
            finally
            {
                InputSystem.RemoveDevice(kb);
                settings.backgroundBehavior = oldBackground;
                settings.editorInputBehaviorInPlayMode = oldEditor;
            }
        }

        [UnityTest]
        public IEnumerator Interactables_ReachableNearAndFar(
            [Values("c1:c1_s1", "c1:c1_s2", "c1:c1_s3", "c1:c1_s4", "c2:c2_s1", "c2:c2_s2", "c2:c2_s3")] string where)
        {
            var parts = where.Split(':');
            yield return Enter(parts[0], parts[1]);
            var interactor = Object.FindAnyObjectByType<PlayerInteractor>();
            var cam = Object.FindAnyObjectByType<Caso404.Player.PlayerContext>().Camera;
            interactor.enabled = false;
            Object.FindAnyObjectByType<Caso404.Player.FirstPersonController>().enabled = false;
            int layer = LayerMask.NameToLayer("Interactable");
            var targets = Object.FindObjectsByType<Collider>(FindObjectsSortMode.None)
                .Where(c => c.gameObject.layer == layer && c.enabled && c.gameObject.activeInHierarchy && c.GetComponentInParent<IInteractable>() != null)
                .GroupBy(c => (Component)c.GetComponentInParent<IInteractable>()).Select(g => g.First()).ToList();
            Assert.IsNotEmpty(targets);
            var failures = new List<string>();
            int closeChecks = 0;
            foreach (var target in targets)
            {
                foreach (float distance in new[] { 0.75f, 1.6f, 2.3f })
                {
                    var result = TryFrom(interactor, cam, target, distance);
                    if (result == false) failures.Add($"{target.name} a {distance} m");
                    if (result == true && distance < 1f) closeChecks++;
                }
            }
            Assert.IsEmpty(failures, "No se pudo apuntar a: " + string.Join(", ", failures));
            Assert.GreaterOrEqual(closeChecks, targets.Count / 2, "La mayoría de los objetos se prueban también de cerca");
        }

        /// <summary>
        /// Prueba 16 direcciones alrededor del objeto a esa distancia horizontal. null = no hay sitio donde pararse
        /// (pared, mueble); true = desde algún sitio válido se apunta; false = hay sitio pero no se apunta desde ninguno.
        /// </summary>
        static bool? TryFrom(PlayerInteractor interactor, Camera cam, Collider target, float distance)
        {
            var center = target.bounds.center;
            float floor = FloorBelow(center);
            bool anySpot = false;
            for (int i = 0; i < 16; i++)
            {
                float a = i * Mathf.PI / 8f;
                var feet = new Vector3(center.x + Mathf.Cos(a) * distance, floor, center.z + Mathf.Sin(a) * distance);
                var eye = feet + Vector3.up * 1.6f;
                if (!CanStand(feet, target)) continue;
                // Solo cuentan sitios con línea de vista sin paredes (desde ahí el jugador realmente lo ve).
                if (WallBetween(eye, center)) continue;
                anySpot = true;
                cam.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(center - eye));
                var hit = interactor.FindTarget();
                if (hit != null && hit.GetComponentInParent<IInteractable>() == target.GetComponentInParent<IInteractable>()) return true;
            }
            return anySpot ? false : (bool?)null;
        }

        static float FloorBelow(Vector3 p)
        {
            return Physics.Raycast(p + Vector3.up * 0.05f, Vector3.down, out var hit, 5f, 1, QueryTriggerInteraction.Ignore) &&
                   hit.collider.bounds.size.y < 0.3f ? hit.point.y : 0f;
        }

        /// <summary>El jugador cabe ahí: cápsula de 0.3 m sin chocar con muebles ni paredes, con piso debajo.</summary>
        static bool CanStand(Vector3 feet, Collider target)
        {
            if (!Physics.Raycast(feet + Vector3.up * 0.2f, Vector3.down, 0.5f, 1, QueryTriggerInteraction.Ignore)) return false;
            var cols = Physics.OverlapCapsule(feet + Vector3.up * 0.65f, feet + Vector3.up * 1.45f, 0.3f, ~0, QueryTriggerInteraction.Ignore);
            return cols.All(c => c == target || c.GetComponentInParent<Caso404.Player.PlayerContext>() != null);
        }

        static bool WallBetween(Vector3 eye, Vector3 center)
        {
            var dir = center - eye;
            var hits = Physics.RaycastAll(eye, dir.normalized, dir.magnitude - 0.15f, 1, QueryTriggerInteraction.Ignore);
            return hits.Any(h => h.collider.bounds.size.y >= PlayerInteractor.BlockerMinHeight &&
                                 h.collider.GetComponentInParent<Caso404.Player.PlayerContext>() == null);
        }

        IEnumerator Enter(string caseId, string sceneId)
        {
            new Save.SaveService().Save(new GameState { currentCaseId = caseId, currentSceneId = sceneId, alexTrust = 50 });
            yield return SceneManager.LoadSceneAsync(GameUIController.MainMenuScene);
            yield return null;
            Object.FindAnyObjectByType<MainMenuView>().ContinueGame();
            float t = 0;
            while (t < SceneTimeout)
            {
                var g = Object.FindAnyObjectByType<GameContext>();
                if (g != null && g.Scene != null && g.Scene.Id == sceneId && Ui != null) break;
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            yield return null;
            yield return null;
            if (Ui.Top is MessageView m) m.Choose("Empezar");
            yield return null;
        }

        static IEnumerator Press(Keyboard kb, Key key)
        {
            InputSystem.QueueStateEvent(kb, new KeyboardState(key));
            yield return null;
            InputSystem.QueueStateEvent(kb, new KeyboardState());
            yield return null;
        }
    }
}
