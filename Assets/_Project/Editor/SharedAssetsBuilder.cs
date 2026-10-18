using Caso404.AI;
using Caso404.Core.Events;
using Caso404.UI;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Caso404.EditorTools
{
    /// <summary>
    /// Crea los assets compartidos: UITheme, AiSettings (sin conexión) y los canales de eventos.
    /// Uso: menú Caso404/Crear assets compartidos, o -executeMethod Caso404.EditorTools.SharedAssetsBuilder.Build
    /// </summary>
    public static class SharedAssetsBuilder
    {
        public const string ThemePath = "Assets/_Project/UI/UITheme.asset";
        public const string AiSettingsPath = "Assets/_Project/Data/AiSettings.asset";
        public const string ModalChannelPath = "Assets/_Project/Data/Events/evt_ui_modal.asset";
        public const string EvidenceChannelPath = "Assets/_Project/Data/Events/evt_evidence_collected.asset";
        public const string DecisionChannelPath = "Assets/_Project/Data/Events/evt_decision_made.asset";
        const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

        [MenuItem("Caso404/Crear assets compartidos")]
        public static void Build()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (font == null) Debug.LogError($"[Caso404] No se encontró la fuente {FontPath}");

            var theme = GetOrCreate<UITheme>(ThemePath);
            theme.TitleFont = font;
            theme.BodyFont = font;
            theme.MonoFont = font;
            EditorUtility.SetDirty(theme);

            var ai = GetOrCreate<AiSettings>(AiSettingsPath);
            ai.UseOffline = true; // en esta versión no hay servidor proxy ni clave de API
            EditorUtility.SetDirty(ai);

            GetOrCreate<BoolEventChannel>(ModalChannelPath);
            GetOrCreate<StringEventChannel>(EvidenceChannelPath);
            GetOrCreate<StringEventChannel>(DecisionChannelPath);

            AssetDatabase.SaveAssets();
            Debug.Log("[Caso404] Assets compartidos listos (UITheme, AiSettings, canales de eventos).");
        }

        /// <summary>Carga un asset o lo crea (con sus carpetas) si no existe.</summary>
        public static T GetOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            EnsureFolder(System.IO.Path.GetDirectoryName(path).Replace('\\', '/'));
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        public static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            var parent = System.IO.Path.GetDirectoryName(folder).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(folder));
        }
    }
}
