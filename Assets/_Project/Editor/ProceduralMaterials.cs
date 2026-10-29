using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Caso404.EditorTools
{
    /// <summary>
    /// Materiales "realistas" sin descargar texturas: genera por código albedo, normal map y mapa de metal/brillo
    /// (ruido, vetas de madera, tablas, baldosas, ladrillo, concreto manchado, tela, metal cepillado y pintura gastada)
    /// y los asigna a los materiales URP Lit compartidos (mat_floor, mat_wall…). Como los materiales son compartidos,
    /// todas las escenas cambian a la vez.
    /// Las texturas son repetibles (sin costuras). Su tamaño real en metros va en la escala del material, y
    /// <see cref="ApplyBoxUVs"/> pone las UV de los cubos en metros para que una pared de 10 m no estire la textura.
    /// </summary>
    public static class ProceduralMaterials
    {
        const string TexFolder = "Assets/_Project/Art/Textures/Procedural";
        const string MatFolder = "Assets/_Project/Art/Materials";
        const int Size = 512;

        enum Pattern { Planks, Tiles, Bricks, Plaster, Concrete, Grain, Weave, Brushed, Painted, Speckle }

        readonly struct Recipe
        {
            public readonly string Key; public readonly Pattern Pattern; public readonly Color Color;
            public readonly float Meters, Smooth, Metal, Bump; public readonly int A, B; public readonly bool Worn;
            public Recipe(string key, Pattern p, Color c, float meters, float smooth, float metal = 0f, float bump = 1f, int a = 0, int b = 0, bool worn = false)
            { Key = key; Pattern = p; Color = c; Meters = meters; Smooth = smooth; Metal = metal; Bump = bump; A = a; B = b; Worn = worn; }
        }

        static readonly Recipe[] Recipes =
        {
            new("floor", Pattern.Planks, new Color(0.46f, 0.33f, 0.23f), 2f, 0.5f, a: 11, b: 3),
            new("floor_hall", Pattern.Tiles, new Color(0.42f, 0.42f, 0.4f), 2f, 0.55f, a: 4, worn: true),
            new("floor_balcony", Pattern.Tiles, new Color(0.5f, 0.49f, 0.46f), 2f, 0.22f, a: 5, worn: true),
            new("floor_tile", Pattern.Tiles, new Color(0.64f, 0.6f, 0.52f), 1.8f, 0.65f, a: 6),
            new("concrete", Pattern.Concrete, new Color(0.46f, 0.46f, 0.45f), 3f, 0.18f, bump: 1.2f, worn: true),
            new("wall", Pattern.Plaster, new Color(0.73f, 0.71f, 0.67f), 2f, 0.08f, bump: 0.5f),
            new("wall_hall", Pattern.Plaster, new Color(0.6f, 0.62f, 0.6f), 2f, 0.1f, bump: 0.5f, worn: true),
            new("wall_cafe", Pattern.Bricks, new Color(0.55f, 0.34f, 0.25f), 2f, 0.12f, bump: 1.4f, a: 26, b: 9),
            new("wall_office", Pattern.Plaster, new Color(0.63f, 0.65f, 0.67f), 2f, 0.1f, bump: 0.4f),
            new("wood", Pattern.Grain, new Color(0.4f, 0.26f, 0.16f), 1f, 0.42f, bump: 0.6f),
            new("fabric", Pattern.Weave, new Color(0.27f, 0.3f, 0.36f), 0.5f, 0.05f, bump: 0.8f, a: 48),
            new("metal", Pattern.Brushed, new Color(0.62f, 0.63f, 0.66f), 1f, 0.62f, metal: 0.9f, bump: 0.3f),
            new("locker", Pattern.Painted, new Color(0.33f, 0.41f, 0.37f), 1f, 0.45f, metal: 0.4f, bump: 0.6f, worn: true),
            new("dark", Pattern.Plaster, new Color(0.1f, 0.1f, 0.11f), 1f, 0.32f, bump: 0.3f),
            new("cork", Pattern.Speckle, new Color(0.6f, 0.45f, 0.28f), 0.5f, 0.05f, bump: 1f),
            new("container", Pattern.Painted, new Color(0.18f, 0.34f, 0.22f), 2f, 0.35f, metal: 0.4f, bump: 0.8f, worn: true),
            new("paper", Pattern.Plaster, new Color(0.9f, 0.88f, 0.82f), 0.5f, 0.1f, bump: 0.2f),
            new("overall", Pattern.Weave, new Color(0.45f, 0.47f, 0.5f), 0.4f, 0.08f, bump: 0.6f, a: 40, worn: true),
        };

        static readonly HashSet<string> TexturedKeys = new();

        /// <summary>Genera las texturas (solo si faltan) y configura los materiales. Devuelve las claves texturizadas.</summary>
        [MenuItem("Caso404/Arte/Generar materiales procedurales")]
        public static void Build() => Build(false);

        public static void Build(bool regenerate)
        {
            SharedAssetsBuilder.EnsureFolder(TexFolder);
            TexturedKeys.Clear();
            foreach (var r in Recipes)
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>($"{MatFolder}/mat_{r.Key}.mat");
                if (mat == null) mat = SceneKit.Mat(r.Key, r.Color);
                string albedo = $"{TexFolder}/T_{r.Key}_A.png", normal = $"{TexFolder}/T_{r.Key}_N.png", mask = $"{TexFolder}/T_{r.Key}_M.png";
                if (regenerate || !File.Exists(albedo)) Generate(r, albedo, normal, mask);
                Import(albedo, false, true);
                Import(normal, true, false);
                Import(mask, false, false);

                mat.SetColor("_BaseColor", Color.white);
                mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(albedo));
                mat.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(normal));
                mat.SetFloat("_BumpScale", 1f);
                mat.EnableKeyword("_NORMALMAP");
                mat.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>(mask));
                mat.EnableKeyword("_METALLICSPECGLOSSMAP");
                mat.SetFloat("_Smoothness", 1f); // con mapa, el brillo sale del alfa del mapa
                mat.SetFloat("_Metallic", 1f);
                mat.SetTextureScale("_BaseMap", Vector2.one / r.Meters);
                mat.enableInstancing = true;
                EditorUtility.SetDirty(mat);
                TexturedKeys.Add(r.Key);
            }
            AssetDatabase.SaveAssets();
        }

        /// <summary>Claves de materiales con textura (para saber a qué cubos poner UV en metros).</summary>
        public static bool IsTextured(Material m)
        {
            if (m == null) return false;
            if (TexturedKeys.Count == 0) foreach (var r in Recipes) TexturedKeys.Add(r.Key);
            return m.name.StartsWith("mat_") && TexturedKeys.Contains(m.name.Substring(4));
        }

        static void Import(string path, bool normal, bool srgb)
        {
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            if (imp == null) { AssetDatabase.ImportAsset(path); imp = (TextureImporter)AssetImporter.GetAtPath(path); }
            bool changed = false;
            var type = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            if (imp.textureType != type) { imp.textureType = type; changed = true; }
            if (imp.sRGBTexture != srgb && !normal) { imp.sRGBTexture = srgb; changed = true; }
            if (imp.maxTextureSize != Size) { imp.maxTextureSize = Size; changed = true; }
            if (imp.anisoLevel != 4) { imp.anisoLevel = 4; changed = true; }
            if (imp.wrapMode != TextureWrapMode.Repeat) { imp.wrapMode = TextureWrapMode.Repeat; changed = true; }
            if (changed) imp.SaveAndReimport();
        }

        // ================= Generación =================

        static void Generate(Recipe r, string albedoPath, string normalPath, string maskPath)
        {
            var height = new float[Size * Size];
            var albedo = new Color[Size * Size];
            var mask = new Color[Size * Size];
            float seed = r.Key.GetHashCode() % 1000 * 0.137f;
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    float u = (x + 0.5f) / Size, v = (y + 0.5f) / Size;
                    Sample(r, u, v, seed, out float h, out float tint, out float smooth, out Color shift);
                    if (r.Worn)
                    {
                        // Suciedad y desgaste: manchas oscuras grandes y rayones finos.
                        float dirt = Mathf.Clamp01((Fbm(u, v, 3, 4, seed + 50f) - 0.55f) * 3f);
                        tint *= 1f - 0.35f * dirt;
                        smooth *= 1f - 0.5f * dirt;
                        shift = Color.Lerp(shift, new Color(0.85f, 0.78f, 0.68f), dirt * 0.5f);
                    }
                    int i = y * Size + x;
                    height[i] = h;
                    var c = r.Color * shift * tint;
                    albedo[i] = new Color(Mathf.Clamp01(c.r), Mathf.Clamp01(c.g), Mathf.Clamp01(c.b), 1f);
                    mask[i] = new Color(r.Metal, 0f, 0f, Mathf.Clamp01(smooth * r.Smooth));
                }
            Save(albedoPath, albedo);
            Save(maskPath, mask);
            Save(normalPath, NormalFromHeight(height, 2.2f * r.Bump));
        }

        static void Sample(Recipe r, float u, float v, float seed, out float h, out float tint, out float smooth, out Color shift)
        {
            shift = Color.white;
            float n = Fbm(u, v, 16, 4, seed);
            switch (r.Pattern)
            {
                case Pattern.Planks:
                {
                    int rows = r.A, segs = r.B;
                    float fv = v * rows; int row = Mathf.FloorToInt(fv);
                    float off = Hash(row, 7) ;
                    float fu = u * segs + off; int seg = Mathf.FloorToInt(fu);
                    float id = Hash(row, seg % segs + 31);
                    float edgeV = Mathf.Min(fv - row, row + 1 - fv) * (2f / rows) * 100f; // distancia al borde en cm de textura
                    float edgeU = Mathf.Min(fu - seg, seg + 1 - fu) * (2f / segs) * 100f;
                    float groove = Mathf.Clamp01(Mathf.Min(edgeV, edgeU) / 0.35f);
                    float grain = Tileable(u, v, 3, rows * 8, seed + id * 10f);
                    float rings = Mathf.Sin((v * rows * 6f + grain * 5f + id * 20f) * Mathf.PI) * 0.5f + 0.5f;
                    tint = (0.78f + 0.4f * id) * (0.88f + 0.18f * rings) * Mathf.Lerp(0.55f, 1f, groove);
                    h = 0.6f * groove + 0.08f * rings;
                    smooth = Mathf.Lerp(0.3f, 1f, groove) * (0.8f + 0.3f * n);
                    break;
                }
                case Pattern.Tiles:
                case Pattern.Bricks:
                {
                    bool brick = r.Pattern == Pattern.Bricks;
                    int rows = brick ? r.A : r.A, cols = brick ? r.B : r.A;
                    float fv = v * rows; int row = Mathf.FloorToInt(fv);
                    float fu = u * cols + (brick && row % 2 == 1 ? 0.5f : 0f); int col = Mathf.FloorToInt(fu);
                    float id = Hash(row, col % cols + 3);
                    float grout = brick ? 0.09f : 0.035f;
                    float eu = Mathf.Min(fu - col, col + 1 - fu), ev = Mathf.Min(fv - row, row + 1 - fv);
                    float edge = Mathf.Min(eu * (brick ? 1f : 1f), ev * (brick ? (float)cols / rows * 2.5f : 1f));
                    float inTile = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((edge - grout * 0.5f) / (grout * 0.5f)));
                    float speck = brick ? 0f : (Hash(Mathf.FloorToInt(u * 700), Mathf.FloorToInt(v * 700)) > 0.93f ? 1f : 0f);
                    tint = Mathf.Lerp(brick ? 0.85f : 0.62f, (brick ? 0.75f + 0.45f * id : 0.92f + 0.14f * id) * (0.93f + 0.12f * n) * (1f - 0.25f * speck), inTile);
                    if (!brick && inTile < 0.5f) shift = new Color(0.9f, 0.88f, 0.85f);
                    if (brick && inTile < 0.5f) shift = new Color(1.2f, 1.25f, 1.35f);
                    h = inTile * (0.8f + 0.2f * n);
                    smooth = Mathf.Lerp(0.25f, 1f, inTile) * (0.85f + 0.25f * n);
                    break;
                }
                case Pattern.Plaster:
                {
                    float big = Fbm(u, v, 2, 3, seed + 9f);
                    float fine = Fbm(u, v, 48, 3, seed + 3f);
                    tint = 0.9f + 0.14f * big + 0.06f * fine;
                    h = fine * 0.6f + big * 0.2f;
                    smooth = 0.7f + 0.6f * fine;
                    break;
                }
                case Pattern.Concrete:
                {
                    float big = Fbm(u, v, 3, 4, seed + 9f);
                    float stain = Mathf.Clamp01((Fbm(u, v, 5, 3, seed + 21f) - 0.5f) * 4f);
                    float pits = Hash(Mathf.FloorToInt(u * 700), Mathf.FloorToInt(v * 700)) > 0.992f ? 0.5f : 0f; // poros escasos y suaves
                    float crack = Mathf.Clamp01(1f - Mathf.Abs(Tileable(u, v, 4, 4, seed + 77f) - 0.5f) * 90f)
                                  * Mathf.Clamp01((Fbm(u, v, 2, 2, seed + 91f) - 0.62f) * 6f) * 0.6f; // grietas finas y escasas
                    tint = (0.82f + 0.3f * big) * (1f - 0.3f * stain) * (1f - 0.35f * pits) * (1f - 0.4f * crack);
                    h = 0.5f + 0.4f * n - 0.4f * pits - 0.5f * crack;
                    smooth = (0.7f + 0.6f * stain) * (1f - 0.5f * pits);
                    break;
                }
                case Pattern.Grain:
                {
                    float warp = Tileable(u, v, 2, 6, seed + 5f);
                    float rings = Mathf.Sin((v * 24f + warp * 6f) * Mathf.PI) * 0.5f + 0.5f;
                    float fibers = Tileable(u, v, 4, 90, seed + 2f);
                    tint = (0.82f + 0.2f * rings) * (0.9f + 0.18f * fibers);
                    h = 0.4f * rings + 0.3f * fibers;
                    smooth = 0.8f + 0.3f * fibers;
                    break;
                }
                case Pattern.Weave:
                {
                    float N = r.A;
                    float a = Mathf.Sin(u * N * Mathf.PI * 2f), b = Mathf.Sin(v * N * Mathf.PI * 2f);
                    float over = (Mathf.Repeat(Mathf.Floor(u * N) + Mathf.Floor(v * N), 2f) < 1f) ? Mathf.Abs(a) : Mathf.Abs(b);
                    tint = (0.85f + 0.2f * over) * (0.92f + 0.14f * n);
                    h = over;
                    smooth = 0.6f + 0.4f * over;
                    break;
                }
                case Pattern.Brushed:
                {
                    float streak = Tileable(u, v, 2, 160, seed + 4f);
                    float smudge = Fbm(u, v, 3, 3, seed + 12f);
                    tint = 0.88f + 0.16f * streak - 0.08f * smudge;
                    h = streak * 0.5f;
                    smooth = 0.75f + 0.25f * streak - 0.3f * smudge;
                    break;
                }
                case Pattern.Painted:
                {
                    float chip = Mathf.Clamp01((Fbm(u, v, 6, 4, seed + 15f) - 0.62f) * 8f);
                    float scratch = Mathf.Clamp01(1f - Mathf.Abs(Tileable(u, v, 30, 2, seed + 40f) - 0.5f) * 40f) * (Hash(Mathf.FloorToInt(v * 12), 2) > 0.6f ? 1f : 0f);
                    tint = (0.92f + 0.12f * n) * (1f - 0.15f * scratch);
                    shift = Color.Lerp(Color.white, new Color(1.5f, 0.85f, 0.55f), chip); // óxido debajo de la pintura
                    tint *= 1f - 0.3f * chip;
                    h = 0.7f - 0.5f * chip - 0.3f * scratch;
                    smooth = (1f - 0.7f * chip) * (0.85f + 0.2f * n);
                    break;
                }
                default: // Speckle (corcho)
                {
                    float s = Hash(Mathf.FloorToInt(u * 260), Mathf.FloorToInt(v * 260));
                    tint = 0.75f + 0.45f * s * (0.7f + 0.3f * n);
                    h = s;
                    smooth = 0.5f + 0.5f * s;
                    break;
                }
            }
        }

        // ================= Ruido repetible =================

        static float Hash(int x, int y)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263);
                h = (h ^ (h >> 13)) * 1274126177u;
                return ((h ^ (h >> 16)) & 0xFFFFFF) / (float)0xFFFFFF;
            }
        }

        /// <summary>Perlin que se repite en [0,1] (mezcla las cuatro esquinas del período).</summary>
        static float Tileable(float u, float v, float fx, float fy, float seed)
        {
            float x = u * fx, y = v * fy;
            float a = Mathf.PerlinNoise(x + seed, y + seed);
            float b = Mathf.PerlinNoise(x - fx + seed, y + seed);
            float c = Mathf.PerlinNoise(x + seed, y - fy + seed);
            float d = Mathf.PerlinNoise(x - fx + seed, y - fy + seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, u), Mathf.Lerp(c, d, u), v);
        }

        static float Fbm(float u, float v, int freq, int octaves, float seed)
        {
            float sum = 0f, amp = 0.5f, norm = 0f;
            for (int o = 0; o < octaves; o++)
            {
                sum += Tileable(u, v, freq, freq, seed + o * 17.3f) * amp;
                norm += amp;
                amp *= 0.5f;
                freq *= 2;
            }
            return sum / norm;
        }

        static Color[] NormalFromHeight(float[] h, float strength)
        {
            var n = new Color[h.Length];
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    float l = h[y * Size + (x + Size - 1) % Size], r = h[y * Size + (x + 1) % Size];
                    float d = h[((y + Size - 1) % Size) * Size + x], u = h[((y + 1) % Size) * Size + x];
                    var v = new Vector3((l - r) * strength, (d - u) * strength, 1f).normalized;
                    n[y * Size + x] = new Color(v.x * 0.5f + 0.5f, v.y * 0.5f + 0.5f, v.z * 0.5f + 0.5f, 1f);
                }
            return n;
        }

        static void Save(string path, Color[] pixels)
        {
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false, true);
            tex.SetPixels(pixels);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
        }

        // ================= UV en metros para los cubos =================

        /// <summary>
        /// Cambia la malla de cada cubo con material texturizado por una copia con UV en metros (proyección por cara)
        /// y UV2 para el horneado. Las mallas se comparten entre cubos del mismo tamaño y se guardan dentro de la escena.
        /// </summary>
        public static int ApplyBoxUVs()
        {
            var cube = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            var cache = new Dictionary<Vector3Int, Mesh>();
            int count = 0;
            foreach (var mf in Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var mesh = mf.sharedMesh;
                if (mesh == null || (mesh != cube && !mesh.name.StartsWith("BoxUV"))) continue;
                var mr = mf.GetComponent<MeshRenderer>();
                if (mr == null || !IsTextured(mr.sharedMaterial)) continue;
                var s = mf.transform.lossyScale;
                var key = new Vector3Int(Mathf.RoundToInt(Mathf.Abs(s.x) * 100), Mathf.RoundToInt(Mathf.Abs(s.y) * 100), Mathf.RoundToInt(Mathf.Abs(s.z) * 100));
                if (!cache.TryGetValue(key, out var boxMesh)) cache[key] = boxMesh = BoxMesh(cube, (Vector3)key / 100f);
                mf.sharedMesh = boxMesh;
                count++;
            }
            return count;
        }

        static Mesh BoxMesh(Mesh cube, Vector3 size)
        {
            var verts = cube.vertices;
            var normals = cube.normals;
            var uv = new Vector2[verts.Length];
            for (int i = 0; i < verts.Length; i++)
            {
                var p = Vector3.Scale(verts[i], size);
                var n = normals[i];
                if (Mathf.Abs(n.x) > 0.5f) uv[i] = new Vector2(p.z * Mathf.Sign(n.x), p.y);
                else if (Mathf.Abs(n.y) > 0.5f) uv[i] = new Vector2(p.x, p.z * Mathf.Sign(n.y));
                else uv[i] = new Vector2(-p.x * Mathf.Sign(n.z), p.y);
            }
            var mesh = new Mesh { name = $"BoxUV {size.x:F2}x{size.y:F2}x{size.z:F2}" };
            mesh.vertices = verts;
            mesh.normals = normals;
            mesh.uv = uv;
            mesh.triangles = cube.triangles;
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            Unwrapping.GenerateSecondaryUVSet(mesh); // UV2 para el horneado de luz
            return mesh;
        }
    }
}
