using System.Linq;
using Caso404.Core;
using Caso404.Data;
using Caso404.Dialogue;
using Caso404.Evidence;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Caso404.Tests
{
    /// <summary>
    /// Integridad entre datos y escenas: cada evidencia "de escena" tiene su objeto, cada personaje listado su NPC,
    /// y cada escena está en Build Settings. Detecta escenas editadas a mano que perdieron algo.
    /// </summary>
    public class SceneIntegrityTests
    {
        string _previousScene;

        [SetUp]
        public void ProtectOpenScene()
        {
            // Abrir escenas descartaría cambios sin guardar: en ese caso la prueba no corre.
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
        public void EveryScene_HasItsEvidenceAndCharacters()
        {
            var db = AssetDatabase.LoadAssetAtPath<GameDatabase>(Case1World.DatabasePath);
            var buildScenes = EditorBuildSettings.scenes.Select(s => System.IO.Path.GetFileNameWithoutExtension(s.path)).ToList();
            var checkedUnityScenes = new System.Collections.Generic.HashSet<string>();
            foreach (var c in db.Cases)
            foreach (var scene in c.Scenes)
            {
                Assert.Contains(scene.UnitySceneName, buildScenes, $"{scene.Id}: la escena {scene.UnitySceneName} no está en Build Settings");
                var path = EditorBuildSettings.scenes.First(s => s.path.EndsWith($"/{scene.UnitySceneName}.unity")).path;
                EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

                var pickups = Object.FindObjectsByType<EvidencePickup>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var ev in scene.Evidence.Where(e => e.Source == EvidenceSource.Scene))
                    Assert.IsTrue(pickups.Any(p => p.Evidence == ev), $"{scene.Id}: falta el objeto de {ev.Id}");

                var npcs = Object.FindObjectsByType<CharacterNpc>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var presence in scene.Characters)
                    Assert.IsTrue(npcs.Any(n => n.Profile == presence.Character), $"{scene.Id}: falta el NPC de {presence.Character.Id}");

                var game = Object.FindAnyObjectByType<GameContext>();
                Assert.IsNotNull(game, $"{scene.Id}: falta GameContext");
                if (checkedUnityScenes.Add(scene.UnitySceneName))
                    foreach (var p in pickups)
                        Assert.IsNotNull(p.Evidence, $"{scene.UnitySceneName}: un EvidencePickup no tiene evidencia asignada");
            }
        }
    }
}
