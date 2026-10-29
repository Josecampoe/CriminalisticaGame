using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using TMPro;
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
            if (sceneName == "Apartamento404") EvidenceMarkers(group);
            if (sceneName is "Comisaria" or "PorteriaSotano" or "Apartamento404") Blinds(group, renderers);
            if (sceneName == "CafeLaEsquina") CafeSign(group, renderers);
            Pictures(group, renderers, sceneName);
            if (sceneName is "Comisaria" or "PisoEjecutivo" or "PorteriaSotano") OfficeFurniture(group, renderers);
            if (sceneName == "ServidoresB") CableTrays(group, renderers);
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

        // ================= Detalles noir =================

        /// <summary>
        /// Marcadores amarillos numerados junto a cada evidencia del apartamento, como en una escena del crimen ya
        /// acordonada. Quedan a 50 cm del objeto (fuera de su zona libre) y sin collider.
        /// </summary>
        static void EvidenceMarkers(Transform g)
        {
            var yellow = SceneKit.Mat("marcador", new Color(0.95f, 0.75f, 0.1f), 0.4f);
            int n = 1;
            foreach (var p in Object.FindObjectsByType<Caso404.Evidence.EvidencePickup>(FindObjectsInactive.Exclude, FindObjectsSortMode.InstanceID)
                         .OrderBy(e => e.Evidence != null ? e.Evidence.Id : e.name))
            {
                var c = p.GetComponent<Collider>();
                if (c == null || c.bounds.min.y > 1.2f) continue; // la cámara del techo no lleva marcador
                var floorY = c.bounds.min.y < 0.5f ? Mathf.Max(0f, c.bounds.min.y - 0.04f) : 0f;
                Vector3? spot = null;
                foreach (var dir in new[] { Vector3.back, Vector3.left, Vector3.right, Vector3.forward })
                {
                    var q = new Vector3(c.bounds.center.x, floorY, c.bounds.center.z) + dir * (Mathf.Max(c.bounds.extents.x, c.bounds.extents.z) + 0.5f);
                    if (!Physics.CheckBox(q + Vector3.up * 0.12f, new Vector3(0.08f, 0.1f, 0.08f))) { spot = q; break; }
                }
                if (spot == null) continue;
                var tent = Piece(g, $"Marcador {n}", PrimitiveType.Cube, spot.Value + Vector3.up * 0.06f, new Vector3(0.11f, 0.12f, 0.09f), yellow, 20f * n);
                var label = new GameObject("Número").AddComponent<TextMeshPro>();
                label.transform.SetParent(tent.transform, false);
                label.transform.localPosition = new Vector3(0f, 0f, -0.52f);
                label.transform.localScale = new Vector3(1f / 0.11f, 1f / 0.12f, 1f) * 0.1f;
                label.text = n.ToString();
                label.fontSize = 1.1f;
                label.fontStyle = FontStyles.Bold;
                label.alignment = TextAlignmentOptions.Center;
                label.color = Color.black;
                label.rectTransform.sizeDelta = new Vector2(1f, 1f);
                n++;
            }
        }

        /// <summary>
        /// Persianas en las ventanas grandes: láminas inclinadas que proyectan las franjas de luz y sombra del cine negro.
        /// Cubren solo la parte de arriba de la ventana, así la ciudad se sigue viendo.
        /// </summary>
        static void Blinds(Transform g, List<MeshRenderer> renderers)
        {
            var slat = SceneKit.Mat("persiana", new Color(0.78f, 0.75f, 0.68f), 0.35f, 0.2f);
            foreach (var r in renderers.Concat(Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(m => m.GetComponent<Collider>() == null)))
            {
                if (r.sharedMaterial == null || r.sharedMaterial.name != "mat_glass" || r.transform.parent == null) continue;
                var b = r.bounds;
                bool alongX = b.size.x > b.size.z;
                float width = alongX ? b.size.x : b.size.z;
                if (b.size.y < 0.9f || width < 1.1f || width > 4.5f || Mathf.Min(b.size.x, b.size.z) > 0.1f) continue;
                if (r.transform.parent.GetComponent<Caso404.Evidence.EvidencePickup>() != null) continue;
                if (Blocked(b)) continue;
                var n = alongX ? Vector3.forward : Vector3.right;
                // del lado del cuarto: el que no choca con nada a 10 cm
                var side = Physics.CheckBox(b.center + n * 0.12f, new Vector3(0.03f, 0.03f, 0.03f)) ? -1f : 1f;
                float top = b.max.y - 0.02f, bottom = b.max.y - b.size.y * 0.6f;
                for (float y = top; y > bottom; y -= 0.075f)
                {
                    var s2 = Piece(g, "Lámina de persiana", PrimitiveType.Cube, b.center + n * (side * 0.09f) + Vector3.up * (y - b.center.y),
                        alongX ? new Vector3(width, 0.004f, 0.06f) : new Vector3(0.06f, 0.004f, width), slat);
                    s2.transform.rotation = Quaternion.AngleAxis(35f, alongX ? Vector3.right : Vector3.forward);
                }
                Piece(g, "Riel de persiana", PrimitiveType.Cube, b.center + n * (side * 0.09f) + Vector3.up * (top + 0.04f - b.center.y),
                    alongX ? new Vector3(width + 0.04f, 0.05f, 0.07f) : new Vector3(0.07f, 0.05f, width + 0.04f), slat);
            }
        }

        /// <summary>El letrero de la fachada del café se vuelve neón: tubo ámbar encendido y las letras "CAFÉ LA ESQUINA".</summary>
        static void CafeSign(Transform g, List<MeshRenderer> renderers)
        {
            var sign = Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).FirstOrDefault(r => r.name.StartsWith("Letrero"));
            if (sign == null) return;
            var neon = SceneKit.Mat("neon_ambar", new Color(1f, 0.6f, 0.15f), 0.5f);
            neon.EnableKeyword("_EMISSION");
            neon.SetColor("_EmissionColor", new Color(1f, 0.55f, 0.12f) * 5f);
            neon.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            EditorUtility.SetDirty(neon);
            var b = sign.bounds;
            foreach (var face in new[] { -1f, 1f })
            {
                var text = new GameObject("Neón CAFÉ LA ESQUINA").AddComponent<TextMeshPro>();
                text.transform.SetParent(g, false);
                text.transform.position = new Vector3(b.center.x, b.center.y, face < 0 ? b.min.z - 0.01f : b.max.z + 0.01f);
                text.transform.rotation = face < 0 ? Quaternion.identity : Quaternion.Euler(0, 180, 0);
                text.text = "CAFÉ LA ESQUINA";
                text.fontSize = 2.4f;
                text.fontStyle = FontStyles.Bold;
                text.characterSpacing = 6f;
                text.alignment = TextAlignmentOptions.Center;
                text.color = new Color(0.12f, 0.06f, 0.02f);
                text.rectTransform.sizeDelta = new Vector2(b.size.x, b.size.y);
            }
            var tube = Piece(g, "Tubo de neón", PrimitiveType.Cube, new Vector3(b.center.x, b.max.y + 0.03f, b.min.z - 0.02f), new Vector3(b.size.x + 0.1f, 0.025f, 0.025f), neon, shadows: false);
            var tube2 = Piece(g, "Tubo de neón", PrimitiveType.Cube, new Vector3(b.center.x, b.min.y - 0.03f, b.min.z - 0.02f), new Vector3(b.size.x + 0.1f, 0.025f, 0.025f), neon, shadows: false);
            var glow = new GameObject("Brillo del neón").AddComponent<Light>();
            glow.transform.SetParent(g, false);
            glow.transform.position = new Vector3(b.center.x, b.center.y - 0.3f, b.min.z - 0.6f);
            glow.type = LightType.Point;
            glow.color = new Color(1f, 0.6f, 0.2f);
            glow.intensity = 1.6f;
            glow.range = 4f;
            glow.shadows = LightShadows.None;
            _ = tube; _ = tube2;
        }

        /// <summary>Cuadros enmarcados (imagen generada, oscura y desenfocada) en algunas paredes largas libres.</summary>
        static void Pictures(Transform g, List<MeshRenderer> renderers, string sceneName)
        {
            if (sceneName is "ServidoresB" or "CasetaSeguridad") return;
            var frame = SceneKit.Mat("marco", new Color(0.12f, 0.08f, 0.05f), 0.5f);
            var art = PictureMaterial();
            int placed = 0;
            foreach (var r in renderers.Where(r => r.sharedMaterial != null && r.sharedMaterial.name.StartsWith("mat_wall")).OrderBy(r => r.name))
            {
                if (placed >= 6) break;
                var b = r.bounds;
                bool alongX = b.size.x > b.size.z;
                float thin = alongX ? b.size.z : b.size.x, length = alongX ? b.size.x : b.size.z;
                if (length < 3f || thin > 0.3f || b.size.y < 2f || _rnd.NextDouble() < 0.4) continue;
                var normal = alongX ? Vector3.forward : Vector3.right;
                foreach (var side in new[] { 1f, -1f })
                {
                    var pos = new Vector3(b.center.x, 1.55f, b.center.z) + normal * side * (thin / 2f + 0.02f);
                    var probe = pos + normal * side * 0.25f;
                    if (Physics.CheckBox(probe, new Vector3(0.4f, 0.5f, 0.4f)) || Blocked(new Bounds(pos, Vector3.one * 1.2f))) continue;
                    var size = alongX ? new Vector3(0.75f, 0.55f, 0.03f) : new Vector3(0.03f, 0.55f, 0.75f);
                    Piece(g, "Marco", PrimitiveType.Cube, pos, size, frame);
                    var q = Piece(g, "Cuadro", PrimitiveType.Quad, pos + normal * side * 0.018f, new Vector3(0.65f, 0.45f, 1f), art, shadows: false);
                    q.transform.rotation = Quaternion.LookRotation(-normal * side);
                    placed++;
                    break;
                }
            }
        }

        static Material PictureMaterial()
        {
            var texPath = $"{TexFolder}/T_cuadro.png";
            if (!File.Exists(texPath))
            {
                const int w = 256, h = 180;
                var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        float u = x / (float)w, v = y / (float)h;
                        // Paisaje de montañas al atardecer, en tonos apagados (como una foto vieja)
                        float ridge = 0.45f + 0.12f * Mathf.PerlinNoise(u * 4f, 1.3f) + 0.06f * Mathf.PerlinNoise(u * 13f, 4f);
                        var sky = Color.Lerp(new Color(0.55f, 0.42f, 0.32f), new Color(0.25f, 0.28f, 0.33f), v);
                        var land = Color.Lerp(new Color(0.12f, 0.13f, 0.11f), new Color(0.22f, 0.22f, 0.18f), v / ridge);
                        var c = v < ridge ? land : sky;
                        c *= 0.85f + 0.15f * Mathf.PerlinNoise(x * 0.2f, y * 0.2f);
                        tex.SetPixel(x, y, c);
                    }
                tex.Apply();
                File.WriteAllBytes(texPath, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(texPath);
            }
            var mat = SceneKit.Mat("cuadro", Color.white, 0.6f);
            mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texPath));
            EditorUtility.SetDirty(mat);
            return mat;
        }

        /// <summary>
        /// Archivadores metálicos y un dispensador de agua contra paredes libres de las oficinas. Solo se ponen del lado
        /// donde hay piso (dentro del edificio), donde no chocan con nada y lejos de lo interactivo. Sin collider.
        /// </summary>
        static void OfficeFurniture(Transform g, List<MeshRenderer> renderers)
        {
            var cabinet = SceneKit.Mat("locker");
            var handle = SceneKit.Mat("metal");
            var water = SceneKit.Mat("bidon", new Color(0.45f, 0.65f, 0.85f, 1f), 0.9f);
            var white = SceneKit.Mat("plastico");
            int placed = 0;
            foreach (var r in renderers.Where(r => r.sharedMaterial != null && r.sharedMaterial.name.StartsWith("mat_wall")).OrderBy(r => r.name))
            {
                if (placed >= 5) break;
                var b = r.bounds;
                bool alongX = b.size.x > b.size.z;
                float thin = alongX ? b.size.z : b.size.x, length = alongX ? b.size.x : b.size.z;
                if (length < 2.2f || thin > 0.3f || b.size.y < 2f || b.min.y > 0.05f) continue;
                var axis = alongX ? Vector3.right : Vector3.forward;
                var normal = alongX ? Vector3.forward : Vector3.right;
                foreach (var side in new[] { 1f, -1f })
                {
                    var basePos = new Vector3(b.center.x, 0f, b.center.z) + normal * side * (thin / 2f + 0.33f) - axis * (length / 2f - 0.55f);
                    if (!Physics.Raycast(basePos + Vector3.up * 1f, Vector3.down, out var hit, 1.3f) || Mathf.Abs(hit.point.y) > 0.1f) continue;
                    var area = new Bounds(basePos + Vector3.up * 0.75f, alongX ? new Vector3(0.6f, 1.3f, 0.55f) : new Vector3(0.55f, 1.3f, 0.6f));
                    if (Physics.CheckBox(area.center, area.extents) || Blocked(area)) continue;
                    bool cooler = placed % 3 == 2;
                    if (cooler)
                    {
                        Piece(g, "Dispensador de agua", PrimitiveType.Cube, basePos + Vector3.up * 0.5f, new Vector3(0.32f, 1f, 0.32f), white);
                        Piece(g, "Botellón", PrimitiveType.Cylinder, basePos + Vector3.up * 1.2f, new Vector3(0.27f, 0.2f, 0.27f), water);
                    }
                    else
                    {
                        var size = alongX ? new Vector3(0.48f, 1.32f, 0.6f) : new Vector3(0.6f, 1.32f, 0.48f);
                        Piece(g, "Archivador", PrimitiveType.Cube, basePos + Vector3.up * 0.66f, size, cabinet);
                        for (int d = 0; d < 4; d++)
                        {
                            var front = basePos + Vector3.up * (0.2f + d * 0.32f) + normal * side * 0.305f;
                            Piece(g, "Manija", PrimitiveType.Cube, front, alongX ? new Vector3(0.14f, 0.025f, 0.02f) : new Vector3(0.02f, 0.025f, 0.14f), handle, shadows: false);
                        }
                    }
                    placed++;
                    break;
                }
            }
        }

        /// <summary>Bandejas de cables sobre cada fila de racks, con cables de colores que bajan a los equipos.</summary>
        static void CableTrays(Transform g, List<MeshRenderer> renderers)
        {
            var tray = SceneKit.Mat("metal");
            Material[] cables = { SceneKit.Mat("cable_azul", new Color(0.1f, 0.25f, 0.6f), 0.4f), SceneKit.Mat("cable_amarillo", new Color(0.75f, 0.6f, 0.1f), 0.4f), SceneKit.Mat("cable") };
            foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(m => m.name.StartsWith("Fila de racks")))
            {
                var b = r.bounds;
                var top = new Vector3(b.center.x, b.max.y + 0.32f, b.center.z);
                Piece(g, "Bandeja de cables", PrimitiveType.Cube, top, new Vector3(0.45f, 0.05f, b.size.z), tray);
                for (int k = 0; k < 6; k++)
                {
                    float z = b.min.z + 0.4f + k * (b.size.z - 0.8f) / 5f;
                    Piece(g, "Cable", PrimitiveType.Cube, new Vector3(b.center.x + (k % 2 == 0 ? 0.1f : -0.1f), b.max.y + 0.15f, z), new Vector3(0.025f, 0.32f, 0.025f), cables[k % cables.Length], shadows: false);
                }
                Piece(g, "Mazo de cables", PrimitiveType.Cube, top + Vector3.up * 0.04f, new Vector3(0.3f, 0.05f, b.size.z - 0.1f), cables[0], shadows: false);
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
