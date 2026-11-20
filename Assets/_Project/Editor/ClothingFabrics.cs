using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Caso404.EditorTools
{
    /// <summary>Tipo de tela de una prenda: define la textura, el relieve y el brillo.</summary>
    public enum Fabric { None, Cotton, Denim, Wool, Leather }

    /// <summary>
    /// Telas para la ropa de los personajes. Las piezas del paquete de Quaternius traen las UV colapsadas en un solo
    /// punto (cada material es un color plano), así que una textura no se vería. Aquí se:
    /// 1) generan cuatro telas que se repiten (algodón, mezclilla, paño de traje y cuero) con su mapa de normales;
    /// 2) crean copias de las mallas con UV proyectadas en metros (por la cara dominante de cada vértice);
    /// 3) arman los materiales: color de la prenda × textura casi blanca, relieve suave y brillo según la tela.
    /// Las copias se comparten entre personajes (una por pieza) y se guardan en Art/Characters/Mallas.
    /// </summary>
    public static class ClothingFabrics
    {
        const int Size = 256;
        const string TexFolder = "Assets/_Project/Art/Characters/Telas";
        const string MeshFolder = "Assets/_Project/Art/Characters/Mallas";

        static readonly Dictionary<Mesh, Mesh> Copies = new();

        /// <summary>Tamaño en metros de una repetición de la textura y brillo de cada tela.</summary>
        static (float meters, float smooth, float bump) Params(Fabric f) => f switch
        {
            Fabric.Denim => (0.09f, 0.2f, 0.65f),
            Fabric.Wool => (0.1f, 0.26f, 0.6f),
            Fabric.Leather => (0.12f, 0.55f, 0.5f),
            _ => (0.08f, 0.14f, 0.45f),
        };

        /// <summary>Borra las copias de mallas anteriores (se regeneran en cada construcción de personajes).</summary>
        public static void Reset()
        {
            Copies.Clear();
            if (AssetDatabase.IsValidFolder(MeshFolder)) AssetDatabase.DeleteAsset(MeshFolder);
            SharedAssetsBuilder.EnsureFolder(MeshFolder);
            SharedAssetsBuilder.EnsureFolder(TexFolder);
        }

        // ================= Mallas con UV =================

        /// <summary>Copia de la malla con UV en metros (proyección por eje dominante) y tangentes para el relieve.</summary>
        public static Mesh WithUVs(Mesh source)
        {
            if (source == null) return null;
            if (Copies.TryGetValue(source, out var cached)) return cached;
            var mesh = Object.Instantiate(source);
            mesh.name = source.name;
            var verts = mesh.vertices;
            var normals = mesh.normals;
            // Algunos FBX vienen en centímetros: si la pieza mide más de 10 unidades, se pasa a metros.
            float unit = mesh.bounds.size.magnitude > 10f ? 0.01f : 1f;
            var uv = new Vector2[verts.Length];
            for (int i = 0; i < verts.Length; i++)
            {
                var p = verts[i] * unit;
                var n = normals.Length == verts.Length ? normals[i] : Vector3.up;
                float ax = Mathf.Abs(n.x), ay = Mathf.Abs(n.y), az = Mathf.Abs(n.z);
                uv[i] = ax >= ay && ax >= az ? new Vector2(p.z, p.y) : ay >= az ? new Vector2(p.x, p.z) : new Vector2(p.x, p.y);
            }
            mesh.uv = uv;
            mesh.RecalculateTangents();
            // El nombre incluye la carpeta del modelo: Men/Suit y Women/Suit tienen mallas que se llaman igual
            // ("Suit_Body") y una copia sobrescribía a la otra (malla de mujer sobre esqueleto de hombre).
            var folder = Path.GetFileName(Path.GetDirectoryName(AssetDatabase.GetAssetPath(source)) ?? "");
            AssetDatabase.CreateAsset(mesh, $"{MeshFolder}/{Safe(folder)}_{Safe(source.name)}.asset");
            Copies[source] = mesh;
            return mesh;
        }

        static string Safe(string s) => string.Join("_", s.Split(Path.GetInvalidFileNameChars()));

        // ================= Materiales =================

        /// <summary>Aplica la tela al material: textura casi blanca (el color sale de _BaseColor), relieve y brillo.</summary>
        public static void Apply(Material mat, Fabric fabric)
        {
            if (fabric == Fabric.None) return;
            var (meters, smooth, bump) = Params(fabric);
            var (albedo, normal) = Textures(fabric);
            mat.SetTexture("_BaseMap", albedo);
            mat.SetTextureScale("_BaseMap", Vector2.one / meters);
            mat.SetTexture("_BumpMap", normal);
            mat.SetFloat("_BumpScale", bump);
            mat.EnableKeyword("_NORMALMAP");
            mat.SetFloat("_Smoothness", smooth);
        }

        /// <summary>Genera (si faltan) e importa la textura y el mapa de normales de una tela.</summary>
        static (Texture2D albedo, Texture2D normal) Textures(Fabric fabric)
        {
            string a = $"{TexFolder}/T_tela_{fabric}_A.png", n = $"{TexFolder}/T_tela_{fabric}_N.png";
            if (!File.Exists(a) || !File.Exists(n))
            {
                var h = new float[Size * Size];
                var col = new Color[Size * Size];
                for (int y = 0; y < Size; y++)
                    for (int x = 0; x < Size; x++)
                    {
                        float u = (x + 0.5f) / Size, v = (y + 0.5f) / Size;
                        float height = Height(fabric, u, v, x, y, out float tint);
                        h[y * Size + x] = height;
                        col[y * Size + x] = new Color(tint, tint, tint, 1f);
                    }
                Save(a, col, false);
                Save(n, Normals(h, fabric == Fabric.Leather ? 3f : 2.2f), true);
            }
            Import(a, false);
            Import(n, true);
            return (AssetDatabase.LoadAssetAtPath<Texture2D>(a), AssetDatabase.LoadAssetAtPath<Texture2D>(n));
        }

        /// <summary>
        /// Altura (0..1) y tono (casi blanco) del tejido en (u, v). Los períodos son enteros para que se repita sin cortes.
        /// </summary>
        static float Height(Fabric fabric, float u, float v, int x, int y, out float tint)
        {
            const float Tau = Mathf.PI * 2f;
            float grain = Hash(x, y) - 0.5f;
            switch (fabric)
            {
                case Fabric.Denim:
                {
                    // Sarga: diagonales finas; los hilos claros (trama) asoman en franjas irregulares.
                    float twill = Mathf.Sin(Tau * (u * 32f + v * 32f)) * 0.5f + 0.5f;
                    float slub = Tileable(u, v, 4, 24, 3.1f);
                    float h = twill * 0.8f + grain * 0.1f;
                    tint = 0.82f + 0.12f * twill + 0.05f * (slub - 0.5f) + grain * 0.04f;
                    return h;
                }
                case Fabric.Wool:
                {
                    // Espiga muy suave: diagonales que cambian de sentido cada franja.
                    float band = Mathf.Floor(u * 8f) % 2f == 0f ? 1f : -1f;
                    float herring = Mathf.Sin(Tau * (band * u * 48f + v * 48f)) * 0.5f + 0.5f;
                    float h = herring * 0.6f + grain * 0.15f;
                    tint = 0.88f + 0.07f * herring + grain * 0.03f;
                    return h;
                }
                case Fabric.Leather:
                {
                    // Poros y arrugas: ruido de varias escalas, brillo parejo.
                    float cells = Tileable(u, v, 24, 24, 7.7f);
                    float wrinkles = Tileable(u, v, 6, 6, 1.3f);
                    float h = cells * 0.6f + wrinkles * 0.4f;
                    tint = 0.85f + 0.1f * wrinkles + grain * 0.03f;
                    return h;
                }
                default:
                {
                    // Algodón: tejido plano (cuadrícula de hilos) con algo de irregularidad.
                    float warp = Mathf.Sin(Tau * u * 40f) * 0.5f + 0.5f;
                    float weft = Mathf.Sin(Tau * v * 40f) * 0.5f + 0.5f;
                    float h = (warp * weft) * 0.88f + Tileable(u, v, 8, 8, 5.5f) * 0.12f; // poca variación grande: se veía como manchas
                    tint = 0.86f + 0.08f * h + grain * 0.05f;
                    return h;
                }
            }
        }

        // ================= Utilidades de textura =================

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

        static Color[] Normals(float[] h, float strength)
        {
            var n = new Color[h.Length];
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    float l = h[y * Size + (x + Size - 1) % Size], r = h[y * Size + (x + 1) % Size];
                    float d = h[((y + Size - 1) % Size) * Size + x], up = h[((y + 1) % Size) * Size + x];
                    var v = new Vector3((l - r) * strength, (d - up) * strength, 1f).normalized;
                    n[y * Size + x] = new Color(v.x * 0.5f + 0.5f, v.y * 0.5f + 0.5f, v.z * 0.5f + 0.5f, 1f);
                }
            return n;
        }

        static void Save(string path, Color[] pixels, bool linear)
        {
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false, linear);
            tex.SetPixels(pixels);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
        }

        static void Import(string path, bool normal)
        {
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            if (imp == null) return;
            var type = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            if (imp.textureType == type && imp.wrapMode == TextureWrapMode.Repeat && imp.anisoLevel == 4) return;
            imp.textureType = type;
            imp.wrapMode = TextureWrapMode.Repeat;
            imp.anisoLevel = 4;
            imp.SaveAndReimport();
        }
    }
}
