using System.Linq;
using Caso404.Evidence;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Caso404.Tests
{
    /// <summary>
    /// El paso visual no debe romper la jugabilidad: en cada escena jugable hay post-procesado global, la cámara del
    /// jugador lo tiene activado, nada de lo agregado tiene collider y la utilería no tapa ninguna evidencia.
    /// </summary>
    public class VisualPassTests
    {
        string _previousScene;

        [SetUp]
        public void ProtectOpenScene()
        {
            var active = EditorSceneManager.GetActiveScene();
            if (active.isDirty) Assert.Ignore("Guarda la escena abierta antes de correr esta prueba.");
            _previousScene = active.path;
        }

        [TearDown]
        public void RestoreOpenScene()
        {
            if (!string.IsNullOrEmpty(_previousScene)) EditorSceneManager.OpenScene(_previousScene, OpenSceneMode.Single);
        }

        [Test]
        public void EveryGameplayScene_HasPostProcessingAndNoBlockingDecor()
        {
            foreach (var s in EditorBuildSettings.scenes.Where(s => !s.path.EndsWith("MainMenu.unity")))
            {
                EditorSceneManager.OpenScene(s.path, OpenSceneMode.Single);
                var name = System.IO.Path.GetFileNameWithoutExtension(s.path);

                var volumes = Object.FindObjectsByType<Volume>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                Assert.IsTrue(volumes.Any(v => v.isGlobal && v.sharedProfile != null && v.weight > 0f), $"{name}: falta el post-procesado global");

                var cam = GameObject.FindGameObjectWithTag("MainCamera");
                Assert.IsNotNull(cam, $"{name}: falta la cámara del jugador");
                var data = cam.GetComponent<UniversalAdditionalCameraData>();
                Assert.IsTrue(data != null && data.renderPostProcessing, $"{name}: la cámara del jugador no tiene post-procesado");

                var decor = GameObject.Find("Ambiente visual");
                Assert.IsNotNull(decor, $"{name}: falta el paso visual");
                Assert.IsEmpty(decor.GetComponentsInChildren<Collider>(true), $"{name}: la decoración no debe tener colliders");

                var props = decor.transform.Find("Utilería");
                if (props == null) continue;
                var propBounds = props.GetComponentsInChildren<Renderer>(true).Select(r => r.bounds).ToList();
                foreach (var pickup in Object.FindObjectsByType<EvidencePickup>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    var col = pickup.GetComponent<Collider>();
                    if (col == null || !pickup.gameObject.activeInHierarchy) continue;
                    var b = col.bounds;
                    Assert.IsFalse(propBounds.Any(p => p.Intersects(b)), $"{name}: la utilería tapa la evidencia {pickup.Evidence.Id}");
                }
            }
        }
    }
}
