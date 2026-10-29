using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Caso404.EditorTools
{
    /// <summary>
    /// Utilería para que los espacios no se vean vacíos: rodapiés, enchufes e interruptores en las paredes, objetos sobre
    /// mesas y mostradores, plantas en esquinas libres, manchas en los pisos de concreto y el desorden de la escena del crimen.
    /// Nada tiene collider (no estorba ni bloquea el rayo de interacción), y antes de poner cada cosa se comprueba con
    /// la física que no choque con muebles y que quede lejos de evidencias, personajes y objetos interactivos.
    /// </summary>
    public static class SceneDressing
    {
        const string TexFolder = "Assets/_Project/Art/Textures/Procedural";

        static List<Bounds> _keepClear;
        static System.Random _rnd;
        static int _count;

        /// <summary>Agrega la utilería bajo <paramref name="root"/>. Devuelve cuántas piezas puso.</summary>
        public static int Apply(Transform root, string sceneName, VisualPass.Ambience ambience)
        {
            _rnd = new System.Random(sceneName.GetHashCode());
            _count = 0;
            Physics.SyncTransforms();
            int interactable = LayerMask.NameToLayer(ProjectSetup.InteractableLayer);
            _keepClear = Object.FindObjectsByType<Collider>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(c => c.gameObject.layer == interactable)
                .Select(c => { var b = c.bounds; b.Expand(0.35f); return b; }).ToList();
            var player = Object.FindAnyObjectByType<Caso404.Player.FirstPersonController>();
            if (player != null) _keepClear.Add(new Bounds(player.transform.position + Vector3.up, new Vector3(1.5f, 2f, 1.5f)));

            var group = new GameObject("Utilería").transform;
            group.SetParent(root, false);
            var renderers = Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)
                .Where(r => !r.transform.IsChildOf(root) && r.GetComponent<Collider>() != null).ToList();

            Walls(group, renderers);
            Surfaces(group, renderers);
            if (ambience != VisualPass.Ambience.Night || sceneName == "PisoEjecutivo") Plants(group, renderers);
            Stains(group, renderers, sceneName);
            if (sceneName == "Apartamento404") CrimeSceneMess(group);
            Pipes(group, renderers);
            Stools(group, renderers);
            return _count;
        }

        // ================= Paredes =================

        /// <summary>Rodapié a lo largo de cada pared y, en las paredes largas, un enchufe y un interruptor.</summary>
        static void Walls(Transform g, List<MeshRenderer> renderers)
        {
            var baseboard = SceneKit.Mat("rodapie", new Color(0.2f, 0.15f, 0.11f), 0.35f);
            var plastic = SceneKit.Mat("plastico", new Color(0.86f, 0.85f, 0.8f), 0.45f);
            foreach (var r in renderers)
            {
                if (r.sharedMaterial == null || !r.sharedMaterial.name.StartsWith("mat_wall")) continue;
                var b = r.bounds;
                bool alongX = b.size.x > b.size.z;
                float thin = alongX ? b.size.z : b.size.x, length = alongX ? b.size.x : b.size.z;
                if (b.min.y > 0.05f || b.size.y < 1.5f || thin > 0.3f || length < 0.4f) continue;
                var size = alongX ? new Vector3(length, 0.09f, thin + 0.025f) : new Vector3(thin + 0.025f, 0.09f, length);
                Piece(g, "Rodapié", PrimitiveType.Cube, new Vector3(b.center.x, b.min.y + 0.045f, b.center.z), size, baseboard, shadows: false);
                if (length < 2.2f) continue;
                var axis = alongX ? Vector3.right : Vector3.forward;
                var normal = alongX ? Vector3.forward : Vector3.right;
                var start = b.center - axis * (length / 2f);
                foreach (var side in new[] { 1f, -1f })
                {
                    var face = normal * side * (thin / 2f + 0.006f);
                    var plate = alongX ? new Vector3(0.08f, 0.12f, 0.012f) : new Vector3(0.012f, 0.12f, 0.08f);
                    TryWallPiece(g, "Enchufe", start + axis * 0.45f + Vector3.up * 0.3f + face, plate, normal * side, plastic);
                    TryWallPiece(g, "Interruptor", start + axis * (length - 0.35f) + Vector3.up * 1.2f + face, plate, normal * side, plastic);
                }
            }
        }

        static void TryWallPiece(Transform g, string name, Vector3 pos, Vector3 size, Vector3 outward, Material mat)
        {
            var probe = new Bounds(pos + outward * 0.07f, Vector3.one * 0.08f);
            if (Physics.CheckBox(probe.center, probe.extents) || Blocked(new Bounds(pos, Vector3.one * 0.1f))) return;
            Piece(g, name, PrimitiveType.Cube, new Vector3(pos.x, pos.y, pos.z), size, mat, shadows: false);
        }

        // ================= Mesas y mostradores =================

        /// <summary>Tazas, libros y papeles sobre mesas, escritorios, barras y encimeras (de 1 a 3 por superficie).</summary>
        static void Surfaces(Transform g, List<MeshRenderer> renderers)
        {
            var paper = SceneKit.Mat("paper");
            var mug = SceneKit.Mat("ceramica", new Color(0.82f, 0.8f, 0.76f), 0.7f);
            var coffee = SceneKit.Mat("cafe_liquido", new Color(0.12f, 0.07f, 0.04f), 0.85f);
            Material[] covers =
            {
                SceneKit.Mat("libro_rojo", new Color(0.42f, 0.1f, 0.08f), 0.3f),
                SceneKit.Mat("libro_azul", new Color(0.1f, 0.16f, 0.3f), 0.3f),
                SceneKit.Mat("libro_verde", new Color(0.14f, 0.24f, 0.16f), 0.3f),
            };
            foreach (var r in renderers)
            {
                var m = r.sharedMaterial;
                if (m == null || !(m.name == "mat_wood" || m.name == "mat_dark" || m.name == "mat_metal")) continue;
                var b = r.bounds;
                float top = b.max.y;
                if (top < 0.68f || top > 1.15f || b.size.x < 0.45f || b.size.z < 0.4f || b.size.x * b.size.z > 6f || b.size.y > 1.2f) continue;
                if (Blocked(b)) continue;
                int items = Mathf.Clamp(Mathf.RoundToInt(b.size.x * b.size.z * 2f), 1, 3);
                for (int i = 0; i < items; i++)
                {
                    var p = new Vector3(Mathf.Lerp(b.min.x + 0.15f, b.max.x - 0.15f, (float)_rnd.NextDouble()), top,
                                        Mathf.Lerp(b.min.z + 0.15f, b.max.z - 0.15f, (float)_rnd.NextDouble()));
                    if (Physics.CheckBox(p + Vector3.up * 0.12f, new Vector3(0.12f, 0.08f, 0.12f))) continue; // algo encima (monitor, caja)
                    float yaw = (float)_rnd.NextDouble() * 360f;
                    switch (_rnd.Next(3))
                    {
                        case 0:
                            Piece(g, "Taza", PrimitiveType.Cylinder, p + Vector3.up * 0.05f, new Vector3(0.08f, 0.05f, 0.08f), mug);
                            Piece(g, "Café", PrimitiveType.Cylinder, p + Vector3.up * 0.096f, new Vector3(0.068f, 0.003f, 0.068f), coffee, shadows: false);
                            break;
                        case 1:
                            for (int k = 0; k < 3; k++)
                                Piece(g, "Papeles", PrimitiveType.Cube, p + Vector3.up * (0.002f + k * 0.003f), new Vector3(0.21f, 0.002f, 0.29f), paper, yaw + k * 9f, shadows: false);
                            break;
                        default:
                            int n = 1 + _rnd.Next(3);
                            for (int k = 0; k < n; k++)
                                Piece(g, "Libro", PrimitiveType.Cube, p + Vector3.up * (0.017f + k * 0.034f), new Vector3(0.16f, 0.032f, 0.23f), covers[_rnd.Next(covers.Length)], yaw + k * 14f);
                            break;
                    }
                }
            }
        }

        // ================= Plantas =================

        /// <summary>Una planta en una esquina libre de cada cuarto grande (matera, tierra y hojas).</summary>
        static void Plants(Transform g, List<MeshRenderer> renderers)
        {
            var pot = SceneKit.Mat("matera", new Color(0.42f, 0.22f, 0.14f), 0.25f);
            var soil = SceneKit.Mat("tierra", new Color(0.12f, 0.09f, 0.07f));
            var leaves = SceneKit.Mat("hojas", new Color(0.16f, 0.27f, 0.14f), 0.35f);
            foreach (var r in renderers.Where(r => r.name.EndsWith(" piso")))
            {
                var b = r.bounds;
                if (b.size.x * b.size.z < 10f) continue;
                var corners = new[] { new Vector3(b.min.x + 0.45f, 0, b.min.z + 0.45f), new Vector3(b.max.x - 0.45f, 0, b.min.z + 0.45f),
                                      new Vector3(b.min.x + 0.45f, 0, b.max.z - 0.45f), new Vector3(b.max.x - 0.45f, 0, b.max.z - 0.45f) };
                int first = _rnd.Next(4);
                for (int i = 0; i < 4; i++)
                {
                    var c = corners[(first + i) % 4];
                    c.y = b.max.y;
                    var area = new Bounds(c + Vector3.up * 0.75f, new Vector3(0.6f, 1.3f, 0.6f));
                    if (Physics.CheckBox(area.center, area.extents) || Blocked(area)) continue;
                    Piece(g, "Matera", PrimitiveType.Cylinder, c + Vector3.up * 0.2f, new Vector3(0.36f, 0.2f, 0.36f), pot);
                    Piece(g, "Tierra", PrimitiveType.Cylinder, c + Vector3.up * 0.39f, new Vector3(0.32f, 0.01f, 0.32f), soil, shadows: false);
                    for (int k = 0; k < 6; k++)
                    {
                        float a = k * 60f + (float)_rnd.NextDouble() * 30f, h = 0.6f + (float)_rnd.NextDouble() * 0.45f;
                        var dir = Quaternion.Euler(0, a, 0) * Vector3.forward;
                        var leaf = Piece(g, "Hoja", PrimitiveType.Sphere, c + Vector3.up * h + dir * 0.13f, new Vector3(0.12f, 0.42f, 0.05f), leaves);
                        leaf.transform.rotation = Quaternion.LookRotation(dir) * Quaternion.Euler(-25f - (float)_rnd.NextDouble() * 20f, 0, 0);
                    }
                    break;
                }
            }
        }

        // ================= Manchas =================

        /// <summary>
        /// Manchas (aceite, humedad, suciedad) como planos transparentes sobre los pisos de concreto: sótano, parqueaderos
        /// y caseta. En la noche las manchas son más brillantes (piso mojado).
        /// </summary>
        static void Stains(Transform g, List<MeshRenderer> renderers, string sceneName)
        {
            var mat = StainMaterial();
            var floors = renderers.Where(r => r.sharedMaterial != null && (r.sharedMaterial.name == "mat_concrete" || r.sharedMaterial.name == "mat_floor_hall")
                                              && r.bounds.size.y < 0.3f && r.bounds.max.y < 0.2f && r.bounds.max.y > -0.2f).ToList();
            foreach (var f in floors)
            {
                var b = f.bounds;
                int n = Mathf.Clamp(Mathf.RoundToInt(b.size.x * b.size.z / 10f), 1, 8);
                for (int i = 0; i < n; i++)
                {
                    var p = new Vector3(Mathf.Lerp(b.min.x + 0.5f, b.max.x - 0.5f, (float)_rnd.NextDouble()), b.max.y + 0.003f + i * 0.0004f,
                                        Mathf.Lerp(b.min.z + 0.5f, b.max.z - 0.5f, (float)_rnd.NextDouble()));
                    float s = 0.5f + (float)_rnd.NextDouble() * 1.4f;
                    if (Blocked(new Bounds(p, new Vector3(s, 0.3f, s)))) continue;
                    var q = Piece(g, "Mancha", PrimitiveType.Quad, p, new Vector3(s, s * (0.6f + (float)_rnd.NextDouble() * 0.6f), 1f), mat, shadows: false);
                    q.transform.rotation = Quaternion.Euler(90f, (float)_rnd.NextDouble() * 360f, 0f);
                }
            }
        }

        static Material StainMaterial()
        {
            SharedAssetsBuilder.EnsureFolder(TexFolder);
            var texPath = $"{TexFolder}/T_mancha.png";
            if (!File.Exists(texPath))
            {
                const int size = 256;
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        float u = x / (float)size - 0.5f, v = y / (float)size - 0.5f;
                        float d = Mathf.Sqrt(u * u + v * v) * 2f;
                        float n = Mathf.PerlinNoise(x * 0.03f + 3f, y * 0.03f + 7f) * 0.7f + Mathf.PerlinNoise(x * 0.12f, y * 0.12f) * 0.3f;
                        float a = Mathf.Clamp01((1f - d) * 1.6f + (n - 0.55f) * 1.8f) * Mathf.Clamp01((1f - d) * 3f);
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * 0.75f));
                    }
                tex.Apply();
                File.WriteAllBytes(texPath, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(texPath);
                var imp = (TextureImporter)AssetImporter.GetAtPath(texPath);
                imp.alphaIsTransparency = true;
                imp.wrapMode = TextureWrapMode.Clamp;
                imp.SaveAndReimport();
            }
            var mat = SceneKit.Mat("mancha", new Color(0.09f, 0.08f, 0.07f), 0.35f);
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetFloat("_ZWrite", 0f);
            mat.SetFloat("_BlendModePreserveSpecular", 0f); // sin esto, el brillo se ve en todo el cuadro aunque el alfa sea 0
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)RenderQueue.Transparent - 50;
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texPath));
            mat.SetColor("_BaseColor", new Color(0.09f, 0.08f, 0.07f, 1f));
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // ================= Sótanos y café =================

        /// <summary>Tuberías y una bandeja de cables bajo el techo de los cuartos de concreto (sótano, mantenimiento, parqueaderos).</summary>
        static void Pipes(Transform g, List<MeshRenderer> renderers)
        {
            var pipe = SceneKit.Mat("tuberia", new Color(0.32f, 0.3f, 0.27f), 0.45f, 0.6f);
            var redPipe = SceneKit.Mat("tuberia_incendio", new Color(0.5f, 0.08f, 0.06f), 0.4f, 0.3f);
            foreach (var f in renderers.Where(r => r.name.EndsWith(" piso") && r.sharedMaterial != null && r.sharedMaterial.name == "mat_concrete"))
            {
                var b = f.bounds;
                bool alongX = b.size.x >= b.size.z;
                float len = (alongX ? b.size.x : b.size.z) - 0.3f, across = alongX ? b.size.z : b.size.x;
                if (len < 2f || across < 1.2f) continue;
                var acrossDir = alongX ? Vector3.forward : Vector3.right;
                float side = 1f;
                var probe = new Bounds(b.center + acrossDir * (across / 2f - 0.4f) + Vector3.up * (SceneKit.WallHeight - 0.2f),
                                       alongX ? new Vector3(len, 0.3f, 0.6f) : new Vector3(0.6f, 0.3f, len));
                if (Blocked(probe))
                {
                    side = -1f; // del otro lado del cuarto
                    probe.center = b.center - acrossDir * (across / 2f - 0.4f) + Vector3.up * (SceneKit.WallHeight - 0.2f);
                    if (Blocked(probe)) continue;
                }
                for (int k = 0; k < 2; k++)
                {
                    float off = (across / 2f - 0.3f - k * 0.22f) * side;
                    var c = b.center + acrossDir * off;
                    c.y = SceneKit.WallHeight - 0.18f - k * 0.06f;
                    var size = new Vector3(0.09f - k * 0.02f, len / 2f, 0.09f - k * 0.02f);
                    var p = Piece(g, k == 0 ? "Tubería" : "Tubería contra incendios", PrimitiveType.Cylinder, c, size, k == 0 ? pipe : redPipe, shadows: false);
                    p.transform.rotation = alongX ? Quaternion.Euler(0, 0, 90) : Quaternion.Euler(90, 0, 0);
                }
            }
        }

        /// <summary>Asiento tapizado sobre cada butaco del café.</summary>
        static void Stools(Transform g, List<MeshRenderer> renderers)
        {
            var leather = SceneKit.Mat("cuero", new Color(0.3f, 0.08f, 0.06f), 0.55f);
            foreach (var r in renderers.Where(r => r.name.StartsWith("Butaco")))
            {
                var b = r.bounds;
                Piece(g, "Asiento del butaco", PrimitiveType.Cylinder, new Vector3(b.center.x, b.max.y + 0.03f, b.center.z), new Vector3(b.size.x * 1.15f, 0.03f, b.size.z * 1.15f), leather);
            }
        }

        // ================= Escena del crimen =================

        /// <summary>Desorden creíble en el apartamento (forcejeo y búsqueda): libros y cojines en el piso, cuadro caído, cajón abierto.</summary>
        static void CrimeSceneMess(Transform g)
        {
            var fabric = SceneKit.Mat("fabric");
            var paper = SceneKit.Mat("paper");
            var wood = SceneKit.Mat("wood");
            var frame = SceneKit.Mat("dark");
            var glass = SceneKit.Mat("glass");
            Material[] covers = { SceneKit.Mat("libro_rojo"), SceneKit.Mat("libro_azul"), SceneKit.Mat("libro_verde") };
            // Libros caídos del estante de la sala
            for (int i = 0; i < 7; i++)
                TryFloor(g, "Libro caído", PrimitiveType.Cube, new Vector3(-5.35f + (i % 3) * 0.28f, 0.018f + (i / 3) * 0.033f, 6.55f - i * 0.12f),
                    new Vector3(0.16f, 0.032f, 0.23f), covers[i % 3], i * 37f, i % 2 == 0 ? 8f : 0f);
            // Cojines del sofá en el piso
            TryFloor(g, "Cojín", PrimitiveType.Cube, new Vector3(-4.55f, 0.07f, 5.6f), new Vector3(0.45f, 0.14f, 0.45f), fabric, 25f, 6f);
            TryFloor(g, "Cojín", PrimitiveType.Cube, new Vector3(-4.2f, 0.07f, 3.3f), new Vector3(0.45f, 0.14f, 0.45f), fabric, -40f, 10f);
            // Cuadro caído con el vidrio roto, bajo el corcho del comedor
            TryFloor(g, "Cuadro caído", PrimitiveType.Cube, new Vector3(-5.6f, 0.03f, 2.2f), new Vector3(0.6f, 0.03f, 0.45f), frame, 12f, 4f);
            for (int i = 0; i < 6; i++)
                TryFloor(g, "Vidrio del cuadro", PrimitiveType.Cube, new Vector3(-5.3f + i * 0.09f, 0.004f, 2.0f + (i % 2) * 0.15f), new Vector3(0.06f, 0.003f, 0.04f), glass, i * 53f);
            // Más papeles revueltos hacia el estudio y el pasillo interno
            for (int i = 0; i < 10; i++)
                TryFloor(g, "Papel revuelto", PrimitiveType.Cube, new Vector3(-1.2f + i * 0.3f, 0.004f, 2.3f + (i % 4) * 0.4f), new Vector3(0.21f, 0.003f, 0.29f), paper, i * 47f);
            // Cajón abierto del archivador del estudio, con carpetas
            TryFloor(g, "Cajón abierto", PrimitiveType.Cube, new Vector3(2.72f, 0.85f, 4.0f), new Vector3(0.4f, 0.22f, 0.5f), SceneKit.Mat("metal"), 0f);
            for (int i = 0; i < 3; i++)
                TryFloor(g, "Carpeta en el piso", PrimitiveType.Cube, new Vector3(2.75f + i * 0.25f, 0.006f, 4.7f + i * 0.1f), new Vector3(0.24f, 0.01f, 0.32f), SceneKit.Mat("cork"), i * 31f);
            // Silla corrida y taza rota junto a la barra
            TryFloor(g, "Taza rota", PrimitiveType.Cylinder, new Vector3(2.6f, 0.04f, 1.6f), new Vector3(0.08f, 0.04f, 0.08f), SceneKit.Mat("ceramica"), 0f, 90f);
            TryFloor(g, "Mancha de café", PrimitiveType.Quad, new Vector3(2.75f, 0.003f, 1.75f), new Vector3(0.5f, 0.35f, 1f), StainMaterial(), 0f, 90f);
            _ = wood;
        }

        static void TryFloor(Transform g, string name, PrimitiveType type, Vector3 pos, Vector3 scale, Material mat, float yaw, float tilt = 0f)
        {
            if (Blocked(new Bounds(pos, scale + Vector3.one * 0.05f))) return;
            var go = Piece(g, name, type, pos, scale, mat);
            go.transform.rotation = Quaternion.Euler(tilt, yaw, 0f);
        }

        // ================= Común =================

        static bool Blocked(Bounds b) => _keepClear.Any(k => k.Intersects(b));

        static GameObject Piece(Transform parent, string name, PrimitiveType type, Vector3 pos, Vector3 scale, Material mat, float yaw = 0f, bool shadows = true)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0));
            go.transform.localScale = scale;
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
            _count++;
            return go;
        }
    }
}
