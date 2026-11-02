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
            Cars(group, renderers);
            Books(group);
            SofaCushions(group);
            if (sceneName == "Apartamento404") HomeDetails(group);
            Shelves(group);
            Lockers(group);
            ParkingDetails(group);
            if (sceneName == "CasetaSeguridad") BoothFront(group);
            ExitSigns(group);
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
            // Sin luz puntual: el letrero emisivo y el bloom bastan (las luces puntuales eran lo más caro del café).
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

        /// <summary>Los carros de los parqueaderos (cajas) ganan cabina con vidrios oscuros, llantas, faros y luces traseras.</summary>
        static void Cars(Transform g, List<MeshRenderer> renderers)
        {
            var glass = SceneKit.Mat("vidrio_carro", new Color(0.08f, 0.1f, 0.13f), 0.9f);
            var tire = SceneKit.Mat("llanta", new Color(0.05f, 0.05f, 0.05f));
            var body = SceneKit.Mat("car");
            var tail = SceneKit.Mat("luz_trasera", new Color(0.6f, 0.05f, 0.04f), 0.8f);
            tail.EnableKeyword("_EMISSION");
            tail.SetColor("_EmissionColor", new Color(1f, 0.05f, 0.03f) * 0.6f);
            EditorUtility.SetDirty(tail);
            var head = SceneKit.Mat("faro", new Color(0.85f, 0.85f, 0.8f), 0.9f, 0.3f);
            foreach (var r in renderers.Where(r => r.sharedMaterial != null && r.sharedMaterial.name == "mat_car"))
            {
                var b = r.bounds;
                var around = b; around.Expand(new Vector3(0.2f, 0.8f, 0.2f));
                if (Blocked(around)) continue; // hay una evidencia en el carro o junto a él
                bool alongZ = b.size.z >= b.size.x;
                var fwd = alongZ ? Vector3.forward : Vector3.right;
                var side = alongZ ? Vector3.right : Vector3.forward;
                float len = alongZ ? b.size.z : b.size.x, wid = alongZ ? b.size.x : b.size.z;
                var cabin = new Bounds(b.center + Vector3.up * (b.extents.y + 0.25f) - fwd * 0.2f, Vector3.zero);
                var rot = Quaternion.LookRotation(fwd);
                var c1 = Piece(g, "Cabina", PrimitiveType.Cube, cabin.center, new Vector3(wid * 0.86f, 0.5f, len * 0.5f), glass);
                c1.transform.rotation = rot;
                var roof = Piece(g, "Techo", PrimitiveType.Cube, cabin.center + Vector3.up * 0.26f, new Vector3(wid * 0.84f, 0.04f, len * 0.42f), body);
                roof.transform.rotation = rot;
                foreach (var sx in new[] { -1f, 1f })
                    foreach (var sz in new[] { -1f, 1f })
                    {
                        var w = Piece(g, "Llanta", PrimitiveType.Cylinder, b.center + side * sx * (wid / 2f - 0.12f) + fwd * sz * (len / 2f - 0.75f) + Vector3.up * (0.33f - b.center.y + b.min.y - 0.15f),
                            new Vector3(0.64f, 0.12f, 0.64f), tire);
                        w.transform.rotation = Quaternion.LookRotation(fwd) * Quaternion.Euler(0, 0, 90);
                    }
                foreach (var sx in new[] { -1f, 1f })
                {
                    Piece(g, "Faro", PrimitiveType.Cube, b.center + side * sx * (wid / 2f - 0.3f) + fwd * (len / 2f + 0.005f) + Vector3.up * 0.15f, alongZ ? new Vector3(0.3f, 0.12f, 0.02f) : new Vector3(0.02f, 0.12f, 0.3f), head, shadows: false);
                    Piece(g, "Luz trasera", PrimitiveType.Cube, b.center + side * sx * (wid / 2f - 0.25f) - fwd * (len / 2f + 0.005f) + Vector3.up * 0.15f, alongZ ? new Vector3(0.35f, 0.1f, 0.02f) : new Vector3(0.02f, 0.1f, 0.35f), tail, shadows: false);
                }
            }
        }

        /// <summary>
        /// Los bloques de color que hacían de libros ("Libros i", "Archivos i") se reemplazan por lomos individuales de
        /// alturas, grosores y colores distintos (el bloque original se oculta, no se borra).
        /// </summary>
        static void Books(Transform g)
        {
            Material[] covers =
            {
                SceneKit.Mat("libro_rojo"), SceneKit.Mat("libro_azul"), SceneKit.Mat("libro_verde"),
                SceneKit.Mat("libro_crema", new Color(0.75f, 0.7f, 0.58f), 0.3f), SceneKit.Mat("libro_negro", new Color(0.08f, 0.08f, 0.09f), 0.35f),
            };
            foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r => r.name.StartsWith("Libros ") || r.name.StartsWith("Archivos ")))
            {
                var b = r.bounds;
                bool alongZ = b.size.z >= b.size.x;
                float len = alongZ ? b.size.z : b.size.x, depth = alongZ ? b.size.x : b.size.z;
                if (len < 0.3f) continue;
                r.enabled = false;
                var axis = alongZ ? Vector3.forward : Vector3.right;
                float t = -len / 2f + 0.01f;
                while (t < len / 2f - 0.03f)
                {
                    float thick = 0.02f + (float)_rnd.NextDouble() * 0.035f;
                    float hgt = b.size.y * (0.7f + (float)_rnd.NextDouble() * 0.3f);
                    if (_rnd.NextDouble() < 0.06) { t += 0.08f; continue; } // hueco
                    var pos = b.center + axis * (t + thick / 2f) + Vector3.up * (hgt / 2f - b.size.y / 2f);
                    var size = alongZ ? new Vector3(depth * 0.9f, hgt, thick) : new Vector3(thick, hgt, depth * 0.9f);
                    var book = Piece(g, "Libro", PrimitiveType.Cube, pos, size, covers[_rnd.Next(covers.Length)], shadows: false);
                    if (_rnd.NextDouble() < 0.08) book.transform.rotation = Quaternion.AngleAxis(alongZ ? 12f : -12f, alongZ ? Vector3.right : Vector3.forward);
                    t += thick + 0.002f;
                }
            }
        }

        /// <summary>Cojines del asiento y del respaldo sobre los sofás (con una separación entre ellos).</summary>
        static void SofaCushions(Transform g)
        {
            var fabric = SceneKit.Mat("fabric");
            foreach (var seat in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r => r.name == "Sofá base"))
            {
                var b = seat.bounds;
                bool alongZ = b.size.z >= b.size.x;
                var axis = alongZ ? Vector3.forward : Vector3.right;
                float len = alongZ ? b.size.z : b.size.x, depth = alongZ ? b.size.x : b.size.z;
                for (int k = 0; k < 2; k++)
                {
                    var c = b.center + axis * ((k - 0.5f) * len / 2f) + Vector3.up * (b.extents.y + 0.06f);
                    var size = alongZ ? new Vector3(depth * 0.92f, 0.12f, len / 2f - 0.03f) : new Vector3(len / 2f - 0.03f, 0.12f, depth * 0.92f);
                    Piece(g, "Cojín del asiento", PrimitiveType.Cube, c, size, fabric);
                }
            }
        }

        /// <summary>Habitación y cocina del apartamento: cobija arrugada, lámpara de noche, manijas de la nevera y microondas.</summary>
        static void HomeDetails(Transform g)
        {
            MeshRenderer Find(string n) => Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).FirstOrDefault(r => r.name == n);
            var blanket = SceneKit.Mat("cobija", new Color(0.32f, 0.12f, 0.1f), 0.08f);
            var metal = SceneKit.Mat("metal");
            var dark = SceneKit.Mat("dark");
            var bed = Find("Cama");
            if (bed != null)
            {
                var b = bed.bounds;
                var c = Piece(g, "Cobija", PrimitiveType.Cube, new Vector3(b.center.x, b.max.y + 0.03f, b.center.z - 0.25f), new Vector3(b.size.x + 0.06f, 0.06f, b.size.z * 0.6f), blanket);
                c.transform.rotation = Quaternion.Euler(2f, 4f, 0f);
                var fold = Piece(g, "Cobija doblada", PrimitiveType.Cube, new Vector3(b.center.x + 0.2f, b.max.y + 0.08f, b.center.z + 0.15f), new Vector3(b.size.x * 0.7f, 0.08f, 0.3f), blanket);
                fold.transform.rotation = Quaternion.Euler(0f, -8f, 3f);
            }
            var night = Find("Mesa de noche");
            if (night != null)
            {
                var b = night.bounds;
                var top = new Vector3(b.center.x, b.max.y, b.center.z);
                Piece(g, "Base de la lámpara de noche", PrimitiveType.Cylinder, top + Vector3.up * 0.1f, new Vector3(0.08f, 0.1f, 0.08f), dark);
                var shade = SceneKit.Mat("pantalla_tela");
                Piece(g, "Pantalla de la lámpara de noche", PrimitiveType.Cylinder, top + Vector3.up * 0.27f, new Vector3(0.2f, 0.08f, 0.2f), shade);
            }
            var fridge = Find("Nevera");
            if (fridge != null)
            {
                var b = fridge.bounds;
                var front = new Vector3(b.min.x - 0.02f, 0f, b.center.z + b.size.z * 0.35f);
                Piece(g, "Manija de la nevera", PrimitiveType.Cube, front + Vector3.up * 1.35f, new Vector3(0.03f, 0.4f, 0.03f), metal);
                Piece(g, "Manija del congelador", PrimitiveType.Cube, front + Vector3.up * 0.65f, new Vector3(0.03f, 0.3f, 0.03f), metal);
                Piece(g, "Junta de la puerta", PrimitiveType.Cube, new Vector3(b.min.x - 0.003f, 1.05f, b.center.z), new Vector3(0.006f, 0.015f, b.size.z * 0.95f), dark, shadows: false);
            }
            var counter = Find("Cubierta");
            if (counter != null)
            {
                var b = counter.bounds;
                var p = new Vector3(b.min.x + 0.35f, b.max.y, b.center.z);
                if (!Blocked(new Bounds(p, Vector3.one * 0.5f)))
                {
                    Piece(g, "Microondas", PrimitiveType.Cube, p + Vector3.up * 0.15f, new Vector3(0.5f, 0.3f, 0.36f), SceneKit.Mat("plastico"));
                    Piece(g, "Puerta del microondas", PrimitiveType.Cube, p + new Vector3(-0.04f, 0.15f, 0.181f), new Vector3(0.34f, 0.22f, 0.005f), dark, shadows: false);
                }
            }
        }

        /// <summary>
        /// Las estanterías (bloques de metal) pasan a ser estanterías de verdad: cuatro postes, repisas y cajas de cartón.
        /// El bloque original conserva su collider y solo se oculta. Se omiten las que tienen una evidencia cerca.
        /// </summary>
        static void Shelves(Transform g)
        {
            var metal = SceneKit.Mat("metal");
            var cardboard = SceneKit.Mat("carton", new Color(0.5f, 0.38f, 0.24f), 0.1f);
            var binder = SceneKit.Mat("libro_negro");
            foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r => r.name == "Estantería"))
            {
                var b = r.bounds;
                if (Blocked(b)) continue;
                r.enabled = false;
                bool alongX = b.size.x >= b.size.z;
                float len = alongX ? b.size.x : b.size.z, depth = alongX ? b.size.z : b.size.x;
                var axis = alongX ? Vector3.right : Vector3.forward;
                var across = alongX ? Vector3.forward : Vector3.right;
                foreach (var sa in new[] { -1f, 1f })
                    foreach (var sb in new[] { -1f, 1f })
                        Piece(g, "Poste", PrimitiveType.Cube, new Vector3(b.center.x, b.center.y, b.center.z) + axis * sa * (len / 2f - 0.02f) + across * sb * (depth / 2f - 0.02f),
                            new Vector3(0.035f, b.size.y, 0.035f), metal);
                int levels = Mathf.Max(3, Mathf.RoundToInt(b.size.y / 0.45f));
                for (int k = 0; k < levels; k++)
                {
                    float y = b.min.y + 0.08f + k * (b.size.y - 0.12f) / (levels - 1);
                    var shelfPos = new Vector3(b.center.x, y, b.center.z);
                    Piece(g, "Repisa", PrimitiveType.Cube, shelfPos, alongX ? new Vector3(len, 0.02f, depth) : new Vector3(depth, 0.02f, len), metal);
                    if (k == levels - 1) continue;
                    float t = -len / 2f + 0.08f;
                    while (t < len / 2f - 0.2f)
                    {
                        float w = 0.18f + (float)_rnd.NextDouble() * 0.2f;
                        float h = 0.15f + (float)_rnd.NextDouble() * 0.17f;
                        if (_rnd.NextDouble() < 0.25) { t += w; continue; }
                        bool isBinder = _rnd.NextDouble() < 0.3;
                        var size = isBinder ? new Vector3(0.06f, 0.3f, depth * 0.8f) : new Vector3(w, h, depth * 0.8f);
                        if (!alongX) size = new Vector3(size.z, size.y, size.x);
                        Piece(g, isBinder ? "Carpeta" : "Caja", PrimitiveType.Cube, shelfPos + axis * (t + (isBinder ? 0.03f : w / 2f)) + Vector3.up * (size.y / 2f + 0.01f), size, isBinder ? binder : cardboard);
                        t += (isBinder ? 0.065f : w) + 0.02f;
                    }
                }
            }
        }

        /// <summary>Casilleros metálicos con rejillas de ventilación, manija y número.</summary>
        static void Lockers(Transform g)
        {
            var dark = SceneKit.Mat("dark");
            var metal = SceneKit.Mat("metal");
            int n = 1;
            foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r => r.name.StartsWith("Casillero ") && !r.name.Contains("correo")).OrderBy(r => r.name))
            {
                var b = r.bounds;
                if (b.size.y < 1.5f || Mathf.Min(b.size.x, b.size.z) < 0.3f) continue;
                if (Blocked(b)) { n++; continue; }
                bool frontZ = b.size.x >= b.size.z; // el frente mira al lado largo (hacia el pasillo)
                var front = frontZ ? new Vector3(b.center.x, 0f, b.max.z + 0.003f) : new Vector3(b.max.x + 0.003f, 0f, b.center.z);
                float w = frontZ ? b.size.x : b.size.z;
                for (int k = 0; k < 3; k++)
                    Piece(g, "Rejilla", PrimitiveType.Cube, front + Vector3.up * (b.max.y - 0.15f - k * 0.05f), frontZ ? new Vector3(w * 0.6f, 0.012f, 0.004f) : new Vector3(0.004f, 0.012f, w * 0.6f), dark, shadows: false);
                Piece(g, "Manija", PrimitiveType.Cube, front + Vector3.up * 1.0f + (frontZ ? Vector3.right : Vector3.forward) * (w * 0.32f), frontZ ? new Vector3(0.03f, 0.12f, 0.02f) : new Vector3(0.02f, 0.12f, 0.03f), metal);
                Piece(g, "Placa", PrimitiveType.Cube, front + Vector3.up * (b.max.y - 0.35f), frontZ ? new Vector3(0.08f, 0.05f, 0.004f) : new Vector3(0.004f, 0.05f, 0.08f), SceneKit.Mat("paper"), shadows: false);
                n++;
            }
        }

        /// <summary>
        /// Parqueaderos: franjas amarillas y negras de seguridad al pie de las columnas, topes de llanta, números de los
        /// puestos pintados en el piso, conos y un letrero de nivel en la pared.
        /// </summary>
        static void ParkingDetails(Transform g)
        {
            var floors = Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r => r.name == "Parqueadero piso").ToList();
            if (floors.Count == 0) return;
            var yellow = SceneKit.Mat("yellow");
            var black = SceneKit.Mat("libro_negro");
            var concrete = SceneKit.Mat("concrete");
            var orange = SceneKit.Mat("cono", new Color(0.95f, 0.35f, 0.05f), 0.4f);
            var white = SceneKit.Mat("plastico");
            foreach (var col in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r => r.name.StartsWith("Columna ")))
            {
                var b = col.bounds;
                if (!floors.Any(f => f.bounds.Contains(new Vector3(b.center.x, f.bounds.center.y, b.center.z)))) continue;
                for (int k = 0; k < 4; k++)
                    Piece(g, k % 2 == 0 ? "Franja amarilla" : "Franja negra", PrimitiveType.Cube, new Vector3(b.center.x, 0.06f + k * 0.12f, b.center.z),
                        new Vector3(b.size.x + 0.01f, 0.12f, b.size.z + 0.01f), k % 2 == 0 ? yellow : black, shadows: false);
            }
            foreach (var f in floors)
            {
                var b = f.bounds;
                int n = 1;
                // Puestos a lo largo del borde sur, cada 3 m
                for (float x = b.min.x + 3.5f; x < b.max.x - 1.5f; x += 3f)
                {
                    var stop = new Vector3(x, 0.06f, b.min.z + 0.55f);
                    if (!Blocked(new Bounds(stop, new Vector3(1.8f, 0.4f, 0.5f))) && !Physics.CheckBox(stop + Vector3.up * 0.1f, new Vector3(0.8f, 0.05f, 0.15f)))
                        Piece(g, "Tope de llanta", PrimitiveType.Cube, stop, new Vector3(1.5f, 0.12f, 0.2f), concrete);
                    var label = new GameObject($"Número de puesto {n}").AddComponent<TMPro.TextMeshPro>();
                    label.transform.SetParent(g, false);
                    label.transform.position = new Vector3(x, 0.006f, b.min.z + 1.4f);
                    label.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                    label.text = $"P-{n:00}";
                    label.fontSize = 6f;
                    label.alignment = TMPro.TextAlignmentOptions.Center;
                    label.color = new Color(0.85f, 0.75f, 0.3f, 0.8f);
                    label.rectTransform.sizeDelta = new Vector2(2f, 1f);
                    n++;
                }
                // Conos en una esquina libre
                var corner = new Vector3(b.max.x - 1f, 0f, b.max.z - 1f);
                for (int k = 0; k < 3; k++)
                {
                    var c = corner + new Vector3(-k * 0.5f, 0f, -(k % 2) * 0.4f);
                    if (Blocked(new Bounds(c + Vector3.up * 0.3f, new Vector3(0.4f, 0.6f, 0.4f))) || Physics.CheckBox(c + Vector3.up * 0.35f, new Vector3(0.15f, 0.25f, 0.15f))) continue;
                    Piece(g, "Base del cono", PrimitiveType.Cube, c + Vector3.up * 0.015f, new Vector3(0.34f, 0.03f, 0.34f), orange);
                    Piece(g, "Cono", PrimitiveType.Cylinder, c + Vector3.up * 0.3f, new Vector3(0.18f, 0.27f, 0.18f), orange);
                    Piece(g, "Banda del cono", PrimitiveType.Cylinder, c + Vector3.up * 0.38f, new Vector3(0.185f, 0.04f, 0.185f), white, shadows: false);
                }
                // Letrero de nivel en la pared norte (cara interior)
                var sign = new Vector3(b.center.x, 1.9f, b.max.z - 0.09f);
                if (!Blocked(new Bounds(sign, new Vector3(1.6f, 0.6f, 0.4f))))
                {
                    Piece(g, "Letrero de nivel", PrimitiveType.Cube, sign, new Vector3(1.4f, 0.45f, 0.03f), yellow, shadows: false);
                    var t = new GameObject("Texto del letrero").AddComponent<TMPro.TextMeshPro>();
                    t.transform.SetParent(g, false);
                    t.transform.position = sign + Vector3.back * 0.02f;
                    t.text = "SÓTANO  -1";
                    t.fontSize = 3f;
                    t.fontStyle = TMPro.FontStyles.Bold;
                    t.alignment = TMPro.TextAlignmentOptions.Center;
                    t.color = new Color(0.08f, 0.08f, 0.08f);
                    t.rectTransform.sizeDelta = new Vector2(1.4f, 0.45f);
                }
            }
        }

        /// <summary>Fachada de la caseta (pared este, x = 0): letrero "VIGILANCIA" sobre la ventanilla y aplique cálido sobre la puerta.</summary>
        static void BoothFront(Transform g)
        {
            var plate = SceneKit.Mat("libro_negro");
            var lampMat = SceneKit.Mat("emit_aplique", new Color(1f, 0.8f, 0.5f), 0.5f);
            lampMat.EnableKeyword("_EMISSION");
            lampMat.SetColor("_EmissionColor", new Color(1f, 0.78f, 0.5f) * 4f);
            EditorUtility.SetDirty(lampMat);
            var signPos = new Vector3(0.1f, 2.3f, 2.9f);
            if (!Blocked(new Bounds(signPos, new Vector3(0.3f, 0.4f, 1.6f))))
            {
                Piece(g, "Letrero VIGILANCIA", PrimitiveType.Cube, signPos, new Vector3(0.03f, 0.3f, 1.4f), plate, shadows: false);
                var t = new GameObject("Texto VIGILANCIA").AddComponent<TextMeshPro>();
                t.transform.SetParent(g, false);
                t.transform.position = signPos + Vector3.right * 0.02f;
                t.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
                t.text = "VIGILANCIA";
                t.fontSize = 2.2f;
                t.fontStyle = FontStyles.Bold;
                t.characterSpacing = 8f;
                t.alignment = TextAlignmentOptions.Center;
                t.color = new Color(0.95f, 0.8f, 0.2f);
                t.rectTransform.sizeDelta = new Vector2(1.4f, 0.3f);
            }
            var lampPos = new Vector3(0.12f, 2.35f, 1f);
            if (!Blocked(new Bounds(lampPos, Vector3.one * 0.4f)))
            {
                Piece(g, "Aplique", PrimitiveType.Cube, lampPos, new Vector3(0.14f, 0.12f, 0.22f), lampMat, shadows: false);
                var l = new GameObject("Luz del aplique").AddComponent<Light>();
                l.transform.SetParent(g, false);
                l.transform.position = lampPos + Vector3.right * 0.3f + Vector3.down * 0.1f;
                l.type = LightType.Point;
                l.color = new Color(1f, 0.78f, 0.5f);
                l.intensity = 1.2f;
                l.range = 3.5f;
                l.shadows = LightShadows.None;
            }
        }

        /// <summary>La señal de escaleras del pasillo se vuelve un aviso verde iluminado con su texto.</summary>
        static void ExitSigns(Transform g)
        {
            var green = SceneKit.Mat("aviso_salida", new Color(0.1f, 0.6f, 0.25f), 0.5f);
            green.EnableKeyword("_EMISSION");
            green.SetColor("_EmissionColor", new Color(0.15f, 1f, 0.4f) * 2.5f);
            EditorUtility.SetDirty(green);
            foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r => r.name == "Señal de escaleras"))
            {
                var b = r.bounds;
                r.enabled = false;
                Piece(g, "Aviso de escaleras", PrimitiveType.Cube, b.center, b.size, green, shadows: false);
                var t = new GameObject("Texto ESCALERAS").AddComponent<TextMeshPro>();
                t.transform.SetParent(g, false);
                t.transform.position = b.center + Vector3.forward * (b.extents.z + 0.003f);
                t.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                t.text = "ESCALERAS";
                t.fontSize = 1.1f;
                t.fontStyle = FontStyles.Bold;
                t.alignment = TextAlignmentOptions.Center;
                t.color = Color.white;
                t.rectTransform.sizeDelta = new Vector2(b.size.x, b.size.y);
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
