using Caso404.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static Caso404.EditorTools.SceneKit;

namespace Caso404.EditorTools
{
    /// <summary>Escena del menú principal (Nueva partida, Continuar, Salir) y el botón "Construir todo".</summary>
    public static class MainMenuSceneBuilder
    {
        public const string ScenePath = ScenesFolder + "/MainMenu.unity";

        [MenuItem("Caso404/Escenas/Menú principal")]
        public static void Build()
        {
            var scene = BeginEmpty();
            var cam = new GameObject("MenuCamera") { tag = "MainCamera" }.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Theme.BgDark;
            cam.gameObject.AddComponent<AudioListener>();

            var canvas = CreateCanvas("UI");
            var faderCanvas = CreateCanvas("Fader", 100);
            var fader = UiChild<ScreenFader>(faderCanvas.transform, "ScreenFader", false);
            Set(fader, "_theme", Theme);
            var menu = UiChild<MainMenuView>(canvas.transform, "MainMenu", false);
            Set(menu, "_theme", Theme);
            Set(menu, "_database", Db);
            Set(menu, "_fader", fader);

            EditorSceneManager.SaveScene(scene, ScenePath);
            RefreshBuildSettings();
            Debug.Log($"[Caso404] Escena {ScenePath} construida.");
        }

        /// <summary>Regenera datos, assets compartidos y todas las escenas del caso 1.</summary>
        [MenuItem("Caso404/Construir todo (datos y escenas)")]
        public static void BuildAll()
        {
            Case1DataBuilder.Build();
            Case2DataBuilder.Build();
            SharedAssetsBuilder.Build();
            Build();
            Apartment404SceneBuilder.Build();
            CafeSceneBuilder.Build();
            PorteriaSceneBuilder.Build();
            ComisariaSceneBuilder.Build();
            ServidoresSceneBuilder.Build();
            PisoEjecutivoSceneBuilder.Build();
            CasetaSceneBuilder.Build();
            RefreshBuildSettings();
            Debug.Log("[Caso404] Todo construido.");
        }
    }
}
