using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Caso404.EditorTools
{
    /// <summary>
    /// Bordes redondeados en los muebles y objetos hechos con cubos. Un cubo perfecto tiene aristas de cuchillo que no
    /// atrapan la luz y delatan la primitiva; con un borde de 0.5 a 3 cm la arista brilla como en un mueble real.
    /// Solo cambia la malla visible: el BoxCollider sigue igual, así que no cambia nada de la jugabilidad.
    /// No toca paredes, pisos ni techos (sus piezas se tocan y el borde dejaría rendijas), vidrios, piezas muy delgadas
    /// (calcomanías, papeles) ni cubos con una imagen propia (letreros, fotos), que dependen de las UV del cubo original.
    /// </summary>
    public static class BevelBoxes
    {
        /// <summary>Claves de material que forman la "caja" del cuarto: se dejan con aristas rectas.</summary>
        static readonly string[] SkipMaterialPrefixes =
        {
            "mat_wall", "mat_floor", "mat_glass", "mat_vidrio", "mat_concrete", "mat_rodapie", "mat_techo", "mat_ceiling",
            "mat_fachada", "mat_anden", "mat_asfalto", "mat_linea_calle", "mat_persiana", "mat_dust", "mat_mancha", "mat_mud",
        };

        static readonly string[] SkipNameParts = { "Pared", "Piso", "Techo", "Muro", "Losa", "Columna", "Viga", "Escal", "Huella" };

        const string MeshFolder = "Assets/_Project/Art/Meshes/Generadas";

        /// <summary>
        /// Redondea los cubos de la escena abierta. Devuelve cuántos cambió.
        /// Las mallas se guardan en un archivo aparte por escena (<see cref="MeshFolder"/>) y no dentro de la escena:
        /// dentro de la escena la hacían pesar más de 70 MB (GitHub rechaza archivos de más de 100 MB).
        /// </summary>
        public static int Apply(string sceneName)
        {
            var cube = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            var cache = new Dictionary<(Vector3Int size, int radius, bool meters), Mesh>();
            int count = 0;
            foreach (var mf in Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var mesh = mf.sharedMesh;
                if (mesh == null) continue;
                bool isCube = mesh == cube || mesh.name.StartsWith("BoxUV") || mesh.name.StartsWith("Bevel");
                if (!isCube) continue;
                var mr = mf.GetComponent<MeshRenderer>();
                if (mr == null || mr.sharedMaterial == null) continue;
                var mat = mr.sharedMaterial;
                if (Skip(mf.gameObject, mat)) continue;

                var s = mf.transform.lossyScale;
                s = new Vector3(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));
                float min = Mathf.Min(s.x, Mathf.Min(s.y, s.z));
                float max = Mathf.Max(s.x, Mathf.Max(s.y, s.z));
                if (min < 0.012f || max > 3.2f) continue;

                bool meters = ProceduralMaterials.IsTextured(mat);
                // Sin textura de receta: solo si el material no tiene imagen propia (los colores planos no usan UV).
                if (!meters && mat.HasProperty("_BaseMap") && mat.GetTexture("_BaseMap") != null) continue;

                float radius = Mathf.Clamp(min * 0.18f, 0.004f, 0.03f);
                var key = (Vector3Int.RoundToInt(s * 1000f), Mathf.RoundToInt(radius * 10000f), meters);
                if (!cache.TryGetValue(key, out var bevel)) cache[key] = bevel = Build(s, radius, meters);
                mf.sharedMesh = bevel;
                count++;
            }
            Save(cache.Values, sceneName);
            return count;
        }

        /// <summary>
        /// Guarda las mallas en Art/Meshes/Generadas/&lt;escena&gt;_bordes.asset. Se borra el archivo anterior solo después
        /// de asignar las mallas nuevas (las viejas ya no las usa nadie), así el paso se puede repetir.
        /// </summary>
        static void Save(IEnumerable<Mesh> meshes, string scene)
        {
            // El nombre llega de VisualPass: los builders crean la escena sin guardarla y GetActiveScene().name está vacío.
            if (string.IsNullOrEmpty(scene)) return;
            SharedAssetsBuilder.EnsureFolder(MeshFolder);
            var path = $"{MeshFolder}/{scene}_bordes.asset";
            AssetDatabase.DeleteAsset(path);
            Mesh container = null;
            foreach (var mesh in meshes)
            {
                if (container == null) { AssetDatabase.CreateAsset(mesh, path); container = mesh; }
                else AssetDatabase.AddObjectToAsset(mesh, container);
            }
            AssetDatabase.SaveAssets();
        }

        static bool Skip(GameObject go, Material mat)
        {
            if (go.GetComponentInParent<Canvas>() != null) return true;
            if (mat.HasProperty("_Surface") && mat.GetFloat("_Surface") > 0.5f) return true; // transparente
            foreach (var p in SkipMaterialPrefixes) if (mat.name.StartsWith(p)) return true;
            foreach (var p in SkipNameParts) if (go.name.Contains(p)) return true;
            return false;
        }

        /// <summary>
        /// Cubo de tamaño <paramref name="size"/> (en metros) con aristas redondeadas de radio <paramref name="r"/>.
        /// Se construye en metros y se divide por el tamaño, porque el transform vuelve a escalarlo.
        /// Cada cara es una cuadrícula con más divisiones cerca de los bordes; cada punto se proyecta sobre la caja
        /// redondeada (punto interior más cercano + r en la dirección hacia afuera), lo que da normales suaves.
        /// </summary>
        static Mesh Build(Vector3 size, float r, bool meters)
        {
            var h = size * 0.5f;
            var verts = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();

            for (int axis = 0; axis < 3; axis++)
            for (int sign = -1; sign <= 1; sign += 2)
            {
                int ua = (axis + 1) % 3, va = (axis + 2) % 3;
                var uc = Coords(h[ua], r);
                var vc = Coords(h[va], r);
                int start = verts.Count;
                foreach (float v in vc)
                foreach (float u in uc)
                {
                    var p = Vector3.zero;
                    p[axis] = sign * h[axis];
                    p[ua] = u;
                    p[va] = v;
                    var inner = new Vector3(
                        Mathf.Clamp(p.x, -h.x + r, h.x - r),
                        Mathf.Clamp(p.y, -h.y + r, h.y - r),
                        Mathf.Clamp(p.z, -h.z + r, h.z - r));
                    var d = p - inner;
                    var faceN = Vector3.zero;
                    faceN[axis] = sign;
                    var n = d.sqrMagnitude > 1e-12f ? d.normalized : faceN;
                    var pos = inner + n * r;

                    verts.Add(new Vector3(pos.x / size.x, pos.y / size.y, pos.z / size.z));
                    // Normal en el espacio del objeto: el transform la divide por la escala, así que se multiplica antes.
                    normals.Add(Vector3.Scale(n, size).normalized);
                    uvs.Add(meters ? MeterUV(p, faceN) : FaceUV(p, h, axis, sign, ua, va));
                }

                int cols = uc.Length;
                for (int j = 0; j < vc.Length - 1; j++)
                for (int i = 0; i < cols - 1; i++)
                {
                    int a = start + j * cols + i, b = a + 1, c = a + cols, e = c + 1;
                    // Orden de los vértices: la cara debe mirar hacia afuera (Unity usa sentido horario).
                    var pa = verts[a]; var pb = verts[b]; var pc = verts[c];
                    var faceN = Vector3.zero;
                    faceN[axis] = sign;
                    bool outward = Vector3.Dot(Vector3.Cross(pb - pa, pc - pa), faceN) < 0f;
                    if (outward) { tris.Add(a); tris.Add(c); tris.Add(b); tris.Add(b); tris.Add(c); tris.Add(e); }
                    else { tris.Add(a); tris.Add(b); tris.Add(c); tris.Add(b); tris.Add(e); tris.Add(c); }
                }
            }

            var mesh = new Mesh { name = $"Bevel {size.x:F2}x{size.y:F2}x{size.z:F2} r{r * 100f:F1}cm" };
            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            // Tangentes solo con textura (mapa de normales); en los colores lisos sobran y pesan.
            if (meters) mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            Unwrapping.GenerateSecondaryUVSet(mesh); // UV2 para el horneado de luz
            return mesh;
        }

        /// <summary>Posiciones de la cuadrícula de una cara: más densas en la franja del borde para que la curva se vea suave.</summary>
        static float[] Coords(float half, float r)
        {
            if (half - r <= r * 0.5f) return new[] { -half, -half + r * 0.35f, 0f, half - r * 0.35f, half };
            // Tres puntos por curva bastan para un radio de 3 cm como máximo (antes eran cuatro: 64 vértices por cara).
            return new[] { -half, -half + r * 0.45f, -half + r, half - r, half - r * 0.45f, half };
        }

        /// <summary>UV en metros (igual que ProceduralMaterials.BoxMesh): la textura no se estira en piezas largas.</summary>
        static Vector2 MeterUV(Vector3 p, Vector3 n)
        {
            if (Mathf.Abs(n.x) > 0.5f) return new Vector2(p.z * Mathf.Sign(n.x), p.y);
            if (Mathf.Abs(n.y) > 0.5f) return new Vector2(p.x, p.z * Mathf.Sign(n.y));
            return new Vector2(-p.x * Mathf.Sign(n.z), p.y);
        }

        /// <summary>UV de 0 a 1 por cara (para materiales de color plano, donde la UV no se ve).</summary>
        static Vector2 FaceUV(Vector3 p, Vector3 h, int axis, int sign, int ua, int va)
        {
            float u = (p[ua] / h[ua] + 1f) * 0.5f, v = (p[va] / h[va] + 1f) * 0.5f;
            return sign > 0 ? new Vector2(u, v) : new Vector2(1f - u, v);
        }
    }
}
