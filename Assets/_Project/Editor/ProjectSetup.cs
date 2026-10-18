using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Caso404.EditorTools
{
    /// <summary>
    /// Ajustes de proyecto que conviene hacer por código (reproducibles en modo batch).
    /// Uso: menú Caso404/Preparar proyecto, o -executeMethod Caso404.EditorTools.ProjectSetup.Run
    /// </summary>
    public static class ProjectSetup
    {
        public const string InteractableLayer = "Interactable";
        public const string InspectionLayer = "Inspection";
        const string PcRpAssetPath = "Assets/Settings/PC_RPAsset.asset";
        const string TmpEssentials = "Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage";

        [MenuItem("Caso404/Preparar proyecto")]
        public static void Run()
        {
            SetDefaultRenderPipeline();
            EnsureLayer(InteractableLayer);
            EnsureLayer(InspectionLayer);
            ImportTmpEssentials();
            AssetDatabase.SaveAssets();
            Debug.Log("[Caso404] Preparación de proyecto terminada.");
        }

        static void SetDefaultRenderPipeline()
        {
            var rp = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(PcRpAssetPath);
            if (rp == null) { Debug.LogError($"[Caso404] No se encontró {PcRpAssetPath}"); return; }
            GraphicsSettings.defaultRenderPipeline = rp;
            Debug.Log("[Caso404] Default Render Pipeline = PC_RPAsset");
        }

        /// <summary>Agrega una capa de usuario si no existe (en el primer hueco libre desde la 8).</summary>
        public static void EnsureLayer(string layerName)
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tagManager.FindProperty("layers");
            for (int i = 0; i < layers.arraySize; i++)
                if (layers.GetArrayElementAtIndex(i).stringValue == layerName) return;
            for (int i = 8; i < layers.arraySize; i++)
            {
                var p = layers.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(p.stringValue))
                {
                    p.stringValue = layerName;
                    tagManager.ApplyModifiedProperties();
                    Debug.Log($"[Caso404] Capa '{layerName}' creada en el índice {i}");
                    return;
                }
            }
            Debug.LogError($"[Caso404] No hay huecos libres para la capa {layerName}");
        }

        static void ImportTmpEssentials()
        {
            if (Directory.Exists("Assets/TextMesh Pro")) return;
            AssetDatabase.ImportPackage(TmpEssentials, false);
            Debug.Log("[Caso404] TMP Essential Resources importados.");
        }
    }
}
