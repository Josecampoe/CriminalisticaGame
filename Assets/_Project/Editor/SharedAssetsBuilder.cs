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
            theme.PaperSprite = UiSprite("ui_papel", 512, 640, PaperPixel);
            theme.ScreenSprite = UiSprite("ui_pantalla", 256, 512, ScreenPixel);
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

        // ================= Texturas de la UI (generadas: no hay archivos de arte para la interfaz) =================

        const string UiTexFolder = "Assets/_Project/UI/Textures";

        /// <summary>Genera (si falta) una textura de UI y la importa como Sprite.</summary>
        static Sprite UiSprite(string name, int w, int h, System.Func<int, int, int, int, Color> pixel)
        {
            EnsureFolder(UiTexFolder);
            var path = $"{UiTexFolder}/{name}.png";
            if (!System.IO.File.Exists(path))
            {
                var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                        tex.SetPixel(x, y, pixel(x, y, w, h));
                tex.Apply();
                System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path);
                var imp = (TextureImporter)AssetImporter.GetAtPath(path);
                imp.textureType = TextureImporterType.Sprite;
                imp.mipmapEnabled = false;
                imp.alphaIsTransparency = true;
                imp.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        /// <summary>Papel de expediente: fibras, bordes amarillentos, una marca de taza y un doblez horizontal.</summary>
        static Color PaperPixel(int x, int y, int w, int h)
        {
            float u = x / (float)w, v = y / (float)h;
            var c = new Color(0.914f, 0.886f, 0.816f);
            float fibers = Mathf.PerlinNoise(x * 0.9f, y * 0.04f) * 0.5f + Mathf.PerlinNoise(x * 0.05f + 9f, y * 0.05f) * 0.5f;
            float edge = Mathf.Min(Mathf.Min(u, 1f - u), Mathf.Min(v, 1f - v));
            float aged = Mathf.Clamp01(1f - edge * 9f);
            c *= 0.95f + 0.07f * fibers;
            c = Color.Lerp(c, new Color(0.78f, 0.68f, 0.5f), aged * 0.45f);
            float ring = Mathf.Abs(Vector2.Distance(new Vector2(u * w, v * h), new Vector2(w * 0.78f, h * 0.2f)) - 52f);
            if (ring < 5f) c = Color.Lerp(c, new Color(0.7f, 0.58f, 0.42f), (1f - ring / 5f) * 0.35f * Mathf.PerlinNoise(x * 0.1f, y * 0.1f));
            float fold = Mathf.Abs(v - 0.5f) * h;
            if (fold < 2f) c *= 0.93f + fold * 0.03f;
            c.a = 1f;
            return c;
        }

        /// <summary>Pantalla oscura: líneas de barrido cada 3 px y un brillo leve hacia el centro.</summary>
        static Color ScreenPixel(int x, int y, int w, int h)
        {
            float u = x / (float)w - 0.5f, v = y / (float)h - 0.5f;
            float glow = Mathf.Clamp01(1f - Mathf.Sqrt(u * u * 2f + v * v) * 1.3f);
            float scan = y % 3 == 0 ? 0.7f : 1f;
            var c = new Color(0.04f, 0.07f, 0.08f) + new Color(0.02f, 0.08f, 0.08f) * glow;
            c *= scan;
            c.a = 1f;
            return c;
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
