using Caso404.AI;
using Caso404.Core.Events;
using Caso404.UI;
using TMPro;
using UnityEditor;
using UnityEngine.TextCore.LowLevel;
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
            // Fuentes libres (skill diseno-ui): Special Elite para títulos y papel, IBM Plex Sans para la interfaz e
            // IBM Plex Mono para ALEX. Si faltan los .ttf, se usa LiberationSans (la de TextMeshPro).
            var title = FontAsset("SpecialElite-Regular", font);
            var body = FontAsset("IBMPlexSans-Regular", font);
            var mono = FontAsset("IBMPlexMono-Regular", font);
            SetWeights(body, FontAsset("IBMPlexSans-SemiBold", font), FontAsset("IBMPlexSans-Italic", font));
            SetWeights(mono, FontAsset("IBMPlexMono-Medium", font), null);
            theme.TitleFont = title != null ? title : font;
            theme.BodyFont = body != null ? body : font;
            theme.MonoFont = mono != null ? mono : font;
            theme.PaperSprite = UiSprite("ui_papel", 512, 640, PaperPixel);
            theme.ScreenSprite = UiSprite("ui_pantalla", 256, 512, ScreenPixel);
            theme.RoundSprite = UiSprite("ui_redondeado", 64, 64, (x, y, w, h) => RoundPixel(x, y, w, h, false), 20);
            theme.FrameSprite = UiSprite("ui_marco", 64, 64, (x, y, w, h) => RoundPixel(x, y, w, h, true), 20);
            theme.GradientSprite = UiSprite("ui_degradado", 8, 256, GradientPixel);
            theme.VignetteSprite = UiSprite("ui_vineta", 256, 256, VignettePixel);
            theme.CheckSprite = UiSprite("ui_check", 64, 64, CheckPixel);
            EditorUtility.SetDirty(theme);

            var ai = GetOrCreate<AiSettings>(AiSettingsPath);
            // IA activada: si el proxy (Server/proxy.mjs) no está corriendo, el juego lo detecta en 2 s y usa las
            // frases del guion. Para jugar siempre sin IA, marca Use Offline en el Inspector.
            ai.UseOffline = false;
            ai.ProxyUrl = "http://127.0.0.1:8790/dialogue"; // 8787 lo usa wrangler (Cloudflare) en este equipo
            EditorUtility.SetDirty(ai);

            GetOrCreate<BoolEventChannel>(ModalChannelPath);
            GetOrCreate<StringEventChannel>(EvidenceChannelPath);
            GetOrCreate<StringEventChannel>(DecisionChannelPath);

            AssetDatabase.SaveAssets();
            Debug.Log("[Caso404] Assets compartidos listos (UITheme, AiSettings, canales de eventos).");
        }

        // ================= Texturas de la UI (generadas: no hay archivos de arte para la interfaz) =================

        const string UiTexFolder = "Assets/_Project/UI/Textures";

        const string FontFolder = "Assets/_Project/UI/Fonts";

        /// <summary>Caracteres que se pre-cargan en el atlas (español, comillas tipográficas, rayas y flechas).</summary>
        static readonly string Charset = BuildCharset();

        static string BuildCharset()
        {
            var sb = new System.Text.StringBuilder();
            for (char c = (char)32; c < 127; c++) sb.Append(c);
            for (char c = (char)160; c < 256; c++) sb.Append(c);
            sb.Append("\u2013\u2014\u2018\u2019\u201C\u201D\u2026\u2022\u20AC\u2190\u2192\u2191\u2193\u25B2\u25BC\u25A0\u25A1\u25CF\u25CB");
            return sb.ToString();
        }

        /// <summary>
        /// Crea (si falta) el TMP_FontAsset de un .ttf de UI/Fonts: atlas dinámico con el juego de caracteres ya cargado,
        /// así el texto se ve igual en el Editor y en el ejecutable. Los símbolos que falten se buscan en LiberationSans.
        /// </summary>
        static TMP_FontAsset FontAsset(string ttfName, TMP_FontAsset fallback)
        {
            var path = $"{FontFolder}/{ttfName} SDF.asset";
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (existing != null) return existing;
            var source = AssetDatabase.LoadAssetAtPath<Font>($"{FontFolder}/{ttfName}.ttf");
            if (source == null) { Debug.LogWarning($"[Caso404] Falta la fuente {ttfName}.ttf; se usa LiberationSans."); return null; }

            var fa = TMP_FontAsset.CreateFontAsset(source, 64, 6, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            fa.name = $"{ttfName} SDF";
            AssetDatabase.CreateAsset(fa, path);
            fa.atlasTextures[0].name = $"{ttfName} Atlas";
            AssetDatabase.AddObjectToAsset(fa.atlasTextures[0], fa);
            fa.material.name = $"{ttfName} Material";
            AssetDatabase.AddObjectToAsset(fa.material, fa);
            fa.TryAddCharacters(Charset, out _);
            if (fallback != null) fa.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset> { fallback };
            EditorUtility.SetDirty(fa);
            AssetDatabase.SaveAssets();
            return fa;
        }

        /// <summary>Usa la variante real de la fuente para &lt;b&gt; (seminegrita) y &lt;i&gt; en lugar de imitarlas.</summary>
        static void SetWeights(TMP_FontAsset regular, TMP_FontAsset bold, TMP_FontAsset italic)
        {
            if (regular == null) return;
            var table = regular.fontWeightTable;
            if (bold != null) { table[6].regularTypeface = bold; table[7].regularTypeface = bold; }
            if (italic != null) table[4].italicTypeface = italic;
            EditorUtility.SetDirty(regular);
        }

        /// <summary>Rectángulo blanco de esquinas redondeadas (radio 16 px) con borde suavizado; con ring=true, solo el contorno de 2 px.</summary>
        static Color RoundPixel(int x, int y, int w, int h, bool ring)
        {
            const float r = 16f;
            float px = x + 0.5f, py = y + 0.5f;
            float dx = Mathf.Max(Mathf.Max(r - px, px - (w - r)), 0f);
            float dy = Mathf.Max(Mathf.Max(r - py, py - (h - r)), 0f);
            float dist = Mathf.Sqrt(dx * dx + dy * dy) - r; // < 0 adentro
            if (dx == 0f && dy == 0f) dist = -Mathf.Min(Mathf.Min(px, w - px), Mathf.Min(py, h - py));
            float a = Mathf.Clamp01(0.5f - dist);
            if (ring) a *= Mathf.Clamp01(dist + 2.5f);
            return new Color(1f, 1f, 1f, a);
        }

        /// <summary>Degradado vertical: opaco abajo, transparente arriba (curva suave).</summary>
        static Color GradientPixel(int x, int y, int w, int h)
        {
            float v = 1f - y / (float)(h - 1);
            return new Color(1f, 1f, 1f, v * v * (3f - 2f * v));
        }

        /// <summary>Viñeta: transparente al centro y oscura en los bordes.</summary>
        static Color VignettePixel(int x, int y, int w, int h)
        {
            float u = x / (float)(w - 1) * 2f - 1f, v = y / (float)(h - 1) * 2f - 1f;
            float d = Mathf.Clamp01((Mathf.Sqrt(u * u + v * v) - 0.45f) / 0.95f);
            return new Color(1f, 1f, 1f, d * d);
        }

        /// <summary>Marca de verificación blanca, trazo de 7 px.</summary>
        static Color CheckPixel(int x, int y, int w, int h)
        {
            var p = new Vector2(x + 0.5f, y + 0.5f);
            float d = Mathf.Min(SegmentDistance(p, new Vector2(12, 33), new Vector2(26, 18)), SegmentDistance(p, new Vector2(26, 18), new Vector2(52, 48)));
            return new Color(1f, 1f, 1f, Mathf.Clamp01(4f - d));
        }

        static float SegmentDistance(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return Vector2.Distance(p, a + ab * t);
        }

        /// <summary>Genera (si falta) una textura de UI y la importa como Sprite (con borde 9-slice si border &gt; 0).</summary>
        static Sprite UiSprite(string name, int w, int h, System.Func<int, int, int, int, Color> pixel, int border = 0)
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
            }
            // Se revisa siempre: un sprite único (no hoja "Multiple"), sin mipmaps y con su borde 9-slice.
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            var wanted = new Vector4(border, border, border, border);
            if (imp.textureType != TextureImporterType.Sprite || imp.spriteImportMode != SpriteImportMode.Single || imp.spriteBorder != wanted)
            {
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.mipmapEnabled = false;
                imp.alphaIsTransparency = true;
                imp.wrapMode = TextureWrapMode.Clamp;
                imp.spriteBorder = wanted;
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
