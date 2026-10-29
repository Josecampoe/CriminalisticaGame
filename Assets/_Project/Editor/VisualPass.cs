using System.Collections.Generic;
using System.IO;
using System.Linq;
using Caso404.Environment;
using Caso404.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Caso404.EditorTools
{
    /// <summary>
    /// Paso visual "noir": se aplica sobre una escena ya construida (al final de cada builder, o a mano sobre las escenas
    /// existentes con Caso404 → Arte → Aplicar paso visual). Es idempotente: borra lo que creó la vez anterior y lo vuelve
    /// a crear. No mueve ni quita evidencias, personajes ni colliders: solo agrega objetos sin collider y ajusta luces,
    /// materiales, cámara y post-procesado.
    /// </summary>
    public static class VisualPass
    {
        public const string Folder = "Assets/_Project/Art/PostProcess";
        const string FxMatFolder = "Assets/_Project/Art/Materials/Fx";
        const string RootName = "Ambiente visual";

        /// <summary>Los tres ambientes de luz del juego.</summary>
        public enum Ambience { Overcast, Fluorescent, Night }

        /// <summary>Ambiente de cada escena: día nublado (apartamento), fluorescente (sótano, Comisaría), noche (café y caso 2).</summary>
        public static Ambience For(string sceneName) => sceneName switch
        {
            "Apartamento404" => Ambience.Overcast,
            "CafeLaEsquina" => Ambience.Night, // GUION §3.2: "café pequeño de barrio, de noche" (10:20 PM)
            "PorteriaSotano" or "Comisaria" => Ambience.Fluorescent,
            _ => Ambience.Night,
        };

        [MenuItem("Caso404/Arte/Aplicar paso visual (todas las escenas)")]
        public static void ApplyAll()
        {
            SharedAssetsBuilder.Build(); // texturas de papel y pantalla de la UI
            ProceduralMaterials.Build(false);
            AmbientAudioBuilder.Build(false);
            CharacterArtBuilder.BuildSurfaceMasks(); // piel, ropa y zapatos con brillo distinto
            foreach (var path in SceneKit.BuildOrder)
            {
                if (!File.Exists(path)) continue;
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                Apply(scene.name);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Caso404] Paso visual aplicado a todas las escenas.");
        }

        /// <summary>Línea de comandos: aplica el paso visual y toma las capturas "despues" (sin hornear).</summary>
        public static void ApplyAndAudit()
        {
            ApplyAll();
            VisualAudit.Run("despues");
        }

        /// <summary>
        /// Línea de comandos: reconstruye las escenas con sus builders (cada uno aplica el paso visual al final; la ciudad
        /// incluye semáforos, cables y avisos), hornea la luz y toma las capturas. No toca datos ni personajes.
        /// </summary>
        /// <summary>Como <see cref="RebuildBakeAndAudit"/>, pero también regenera los personajes (texturas y prefabs).</summary>
        public static void RebuildWithCharactersBakeAndAudit()
        {
            CharacterArtBuilder.Build();
            RebuildBakeAndAudit();
        }

        public static void RebuildBakeAndAudit()
        {
            SharedAssetsBuilder.Build();
            AmbientAudioBuilder.Build(false);
            CharacterArtBuilder.BuildSurfaceMasks();
            MainMenuSceneBuilder.Build();
            Apartment404SceneBuilder.Build();
            CafeSceneBuilder.Build();
            PorteriaSceneBuilder.Build();
            ComisariaSceneBuilder.Build();
            ServidoresSceneBuilder.Build();
            PisoEjecutivoSceneBuilder.Build();
            CasetaSceneBuilder.Build();
            SceneKit.RefreshBuildSettings();
            AssetDatabase.SaveAssets();
            LightingBaker.BakeAll();
            VisualAudit.Run("despues");
        }

        /// <summary>Línea de comandos: aplica el paso visual, hornea la luz de todas las escenas y toma las capturas.</summary>
        public static void ApplyBakeAndAudit()
        {
            ApplyAll();
            LightingBaker.BakeAll();
            VisualAudit.Run("despues");
        }

        /// <summary>Aplica el paso visual a la escena abierta.</summary>
        public static void Apply(string sceneName)
        {
            var old = GameObject.Find(RootName);
            if (old != null) Object.DestroyImmediate(old);
            var root = new GameObject(RootName).transform;

            bool menu = sceneName == "MainMenu";
            var ambience = menu ? Ambience.Night : For(sceneName);
            var volume = new GameObject("Post-procesado global").AddComponent<Volume>();
            volume.transform.SetParent(root, false);
            volume.isGlobal = true;
            volume.sharedProfile = Profile(ambience);

            foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                SetupCamera(cam, menu);
            if (menu) return;

            // SceneKit.BeginEmpty vuelve a poner colores planos en los materiales compartidos: se reaplican las texturas.
            ProceduralMaterials.Build(false);

            SetupDialogueFocus(root);
            Glass();
            Footprint();
            int boxes = ProceduralMaterials.ApplyBoxUVs();
            SetAmbient(ambience);
            var lights = RoomLights();
            Fixtures(root, lights, ambience);
            ExtraTubes(root);
            if (sceneName == "CafeLaEsquina") CafeLamps(root);
            Accents(root);
            Shadows(lights);
            ReflectionProbes(root);
            Dust(root, lights, ambience);
            CharacterFill(ambience);
            int props = SceneDressing.Apply(root, sceneName, ambience);
            ServerRacks(root);
            Windows(root, ambience);
            if (ambience == Ambience.Night) WetStreets();
            AmbientAudioBuilder.AddTo(root, sceneName);

            // La Comisaría es de noche en el caso 2: CaseAtmosphere cambia también el perfil.
            var atmosphere = Object.FindAnyObjectByType<CaseAtmosphere>();
            if (atmosphere != null)
            {
                SceneKit.Set(atmosphere, "_volume", volume);
                SceneKit.Set(atmosphere, "_nightProfile", Profile(Ambience.Night));
            }
            Debug.Log($"[Caso404] Paso visual en {sceneName}: {lights.Count} luces con lámpara visible, {boxes} cubos con UV en metros, {props} piezas de utilería.");
        }

        // ================= Cámara =================

        /// <summary>Cámara con post-procesado, HDR y antialiasing SMAA (más barato que TAA y sin estelas al girar).</summary>
        static void SetupCamera(Camera cam, bool menu)
        {
            var data = cam.GetComponent<UniversalAdditionalCameraData>();
            if (data == null) data = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            data.antialiasingQuality = AntialiasingQuality.Medium;
            data.dithering = true; // evita bandas en los degradados oscuros
            cam.allowHDR = true;
            if (!menu) cam.farClipPlane = 900f;
        }

        /// <summary>Al hablar, la cámara se acerca y desenfoca el fondo (CameraFocusFx + Volume de profundidad de campo).</summary>
        static void SetupDialogueFocus(Transform root)
        {
            var fps = Object.FindAnyObjectByType<FirstPersonController>();
            if (fps == null) return;
            var cam = fps.GetComponentInChildren<Camera>(true);
            var focusVolume = new GameObject("Profundidad de campo (diálogo)").AddComponent<Volume>();
            focusVolume.transform.SetParent(root, false);
            focusVolume.isGlobal = true;
            focusVolume.priority = 10;
            focusVolume.weight = 0f;
            focusVolume.sharedProfile = FocusProfile();
            var fx = cam.GetComponent<CameraFocusFx>();
            if (fx == null) fx = cam.gameObject.AddComponent<CameraFocusFx>();
            SceneKit.Set(fx, "_controller", fps);
            SceneKit.Set(fx, "_camera", cam);
            SceneKit.Set(fx, "_focusVolume", focusVolume);
            SceneKit.Set(fx, "_bobTarget", cam.transform.parent);
        }

        // ================= Materiales especiales =================

        /// <summary>Vidrio: casi transparente, liso y con un poco de metal para que refleje las probes (no "lava" la imagen).</summary>
        static void Glass()
        {
            var glass = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Materials/mat_glass.mat");
            if (glass == null) return;
            glass.SetColor("_BaseColor", new Color(0.62f, 0.72f, 0.78f, 0.1f));
            glass.SetFloat("_Smoothness", 0.97f);
            glass.SetFloat("_Metallic", 0.25f);
            glass.SetFloat("_EnvironmentReflections", 1f);
            glass.SetFloat("_SpecularHighlights", 1f);
            EditorUtility.SetDirty(glass);
        }

        /// <summary>
        /// La huella del balcón (evidencia clave) deja de ser una tabla marrón: textura de suela con tacón, antepié y
        /// dibujo de la banda de rodadura, recortada con alfa para que alrededor se vea el piso.
        /// </summary>
        static void Footprint()
        {
            const string texPath = "Assets/_Project/Art/Textures/Procedural/T_huella.png";
            if (!File.Exists(texPath))
            {
                const int w = 128, h = 320;
                var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        float u = (x + 0.5f) / w, v = (y + 0.5f) / h;
                        float fore = Sq((u - 0.5f) / 0.44f) + Sq((v - 0.68f) / 0.3f);
                        float heel = Sq((u - 0.5f) / 0.36f) + Sq((v - 0.19f) / 0.17f);
                        float arch = Sq((u - 0.56f) / 0.26f) + Sq((v - 0.42f) / 0.22f);
                        float edge = Mathf.PerlinNoise(x * 0.25f, y * 0.25f) * 0.25f;
                        bool inside = fore < 1f - edge || heel < 1f - edge || arch < 0.8f - edge;
                        bool groove = Mathf.Abs(Mathf.Repeat(v * 26f + Mathf.Abs(u - 0.5f) * 3f, 1f) - 0.5f) < 0.16f; // zigzag
                        float dirt = 0.75f + 0.25f * Mathf.PerlinNoise(x * 0.12f + 3f, y * 0.12f);
                        var c = new Color(0.27f, 0.19f, 0.11f) * (groove ? 0.55f : 1f) * dirt;
                        float a = inside ? (groove ? 0.75f : 1f) * Mathf.Clamp01(dirt + 0.1f) : 0f;
                        tex.SetPixel(x, y, new Color(c.r, c.g, c.b, a));
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
            var mud = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Materials/mat_mud.mat");
            if (mud == null) return;
            mud.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texPath));
            mud.SetColor("_BaseColor", Color.white);
            mud.SetFloat("_AlphaClip", 1f);
            mud.SetFloat("_Cutoff", 0.45f);
            mud.EnableKeyword("_ALPHATEST_ON");
            mud.SetFloat("_Smoothness", 0.35f);
            mud.SetOverrideTag("RenderType", "TransparentCutout");
            mud.renderQueue = (int)RenderQueue.AlphaTest;
            EditorUtility.SetDirty(mud);
        }

        static float Sq(float x) => x * x;

        static Material Emissive(string name, Color color, float hdr)
        {
            SharedAssetsBuilder.EnsureFolder(FxMatFolder);
            var path = $"{FxMatFolder}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat, path); }
            mat.SetColor("_BaseColor", color * 0.9f);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * hdr);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None; // la luz real ya la aporta el Light
            mat.SetFloat("_Smoothness", 0.6f);
            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // ================= Luces =================

        /// <summary>Luz ambiente más baja que antes: más contraste entre lo iluminado por las lámparas y las sombras.</summary>
        static void SetAmbient(Ambience a)
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = a switch
            {
                Ambience.Overcast => new Color(0.33f, 0.34f, 0.36f),
                Ambience.Fluorescent => new Color(0.19f, 0.21f, 0.21f),
                _ => new Color(0.1f, 0.11f, 0.15f),
            };
        }

        /// <summary>Luces puntuales de los cuartos (las del builder, en la raíz de la escena; no las de la ciudad).</summary>
        static List<Light> RoomLights() =>
            Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Where(l => l.type == LightType.Point && l.transform.parent == null)
                .OrderBy(l => l.name).ToList();

        static bool IsTube(Light l) => l.name.Contains("Fluorescente") || l.name.Contains("Parqueadero") || l.name.Contains("racks")
                                       || l.name.StartsWith("Pasillo") || l.name.Contains("pasillo exterior");
        static bool IsGlow(Light l) => l.name.Contains("Brillo") || l.name.Contains("emergencia");

        /// <summary>
        /// Cada luz tiene su fuente visible: tubo fluorescente con su caja, o lámpara colgante con pantalla y bombillo.
        /// No proyectan sombra ni tienen collider (no tapan la luz ni estorban al jugador).
        /// </summary>
        static void Fixtures(Transform root, List<Light> lights, Ambience ambience)
        {
            var group = new GameObject("Lámparas").transform;
            group.SetParent(root, false);
            var housing = SceneKit.Mat("dark");
            var shade = SceneKit.Mat("lamp_shade", new Color(0.12f, 0.12f, 0.11f), 0.45f, 0.6f);
            int tubeIndex = 0;
            foreach (var l in lights)
            {
                if (IsGlow(l)) continue;
                float ceiling = SceneKit.WallHeight;
                var p = l.transform.position;
                if (IsTube(l))
                {
                    var tubeMat = Emissive("emit_tubo", new Color(0.86f, 0.94f, 1f), 3.2f);
                    var t = Part(group, $"Fluorescente ({l.name})", PrimitiveType.Cube, new Vector3(p.x, ceiling - 0.05f, p.z), new Vector3(1.25f, 0.05f, 0.18f), housing);
                    var tube = Part(group, "Tubo", PrimitiveType.Cube, new Vector3(p.x, ceiling - 0.085f, p.z), new Vector3(1.18f, 0.03f, 0.1f), tubeMat);
                    tube.transform.SetParent(t.transform, true);
                    l.transform.position = new Vector3(p.x, ceiling - 0.25f, p.z);
                    if (l.name.Contains("Parqueadero"))
                    {
                        // Los parqueaderos son grandes: más luz por tubo (valores fijos para poder repetir el paso).
                        l.intensity = Mathf.Max(l.intensity, 1.6f);
                        l.range = Mathf.Max(l.range, 9f);
                    }
                    // Algunos tubos viejos parpadean (sótano, parqueaderos y servidores).
                    if (ambience != Ambience.Overcast && tubeIndex++ % 3 == 1)
                    {
                        var flicker = l.gameObject.GetComponent<LightFlicker>();
                        if (flicker == null) flicker = l.gameObject.AddComponent<LightFlicker>();
                        SceneKit.Set(flicker, "_light", l);
                        SceneKit.Set(flicker, "_tube", tube.GetComponent<Renderer>());
                        var so = new SerializedObject(flicker);
                        so.FindProperty("_tubeEmission").colorValue = new Color(0.86f, 0.94f, 1f) * 3.2f;
                        so.ApplyModifiedPropertiesWithoutUndo();
                    }
                }
                else
                {
                    bool warm = l.color.r > l.color.b + 0.05f;
                    var bulbMat = Emissive(warm ? "emit_bombillo_calido" : "emit_bombillo_frio", warm ? new Color(1f, 0.78f, 0.5f) : new Color(0.9f, 0.95f, 1f), 6f);
                    float bulbY = ceiling - 0.45f; // fijo: así el paso se puede repetir sin bajar la lámpara cada vez
                    var lamp = Part(group, $"Lámpara ({l.name})", PrimitiveType.Cylinder, new Vector3(p.x, (ceiling + bulbY) / 2f + 0.1f, p.z), new Vector3(0.012f, (ceiling - bulbY) / 2f - 0.1f, 0.012f), housing);
                    var cone = Part(group, "Pantalla", PrimitiveType.Cylinder, new Vector3(p.x, bulbY + 0.07f, p.z), new Vector3(0.42f, 0.07f, 0.42f), shade);
                    cone.transform.SetParent(lamp.transform, true);
                    var bulb = Part(group, "Bombillo", PrimitiveType.Sphere, new Vector3(p.x, bulbY - 0.01f, p.z), Vector3.one * 0.11f, bulbMat);
                    bulb.transform.SetParent(lamp.transform, true);
                    l.transform.position = new Vector3(p.x, bulbY - 0.12f, p.z);
                }
            }
        }

        /// <summary>
        /// Acentos de luz cálida a la altura de la mesa, típicos del cine negro: lámpara de banquero verde en cada
        /// escritorio de la Comisaría y la lámpara de pie de la sala del apartamento encendida.
        /// </summary>
        static void Accents(Transform root)
        {
            var group = new GameObject("Acentos de luz").transform;
            group.SetParent(root, false);
            var brass = SceneKit.Mat("laton", new Color(0.55f, 0.42f, 0.18f), 0.75f, 0.9f);
            var green = Emissive("emit_pantalla_banquero", new Color(0.1f, 0.45f, 0.2f), 0.8f);
            var warm = Emissive("emit_bombillo_calido", new Color(1f, 0.78f, 0.5f), 6f);
            var shade = SceneKit.Mat("pantalla_tela", new Color(0.75f, 0.68f, 0.55f), 0.1f);
            var evidence = Object.FindObjectsByType<Caso404.Evidence.EvidencePickup>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Select(e => e.GetComponent<Collider>()).Where(c => c != null).Select(c => c.bounds).ToList();
            bool NearEvidence(Vector3 p) => evidence.Any(b => b.SqrDistance(p) < 0.5f * 0.5f);

            foreach (var desk in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r => r.name == "Escritorio"))
            {
                var b = desk.bounds;
                var p = new Vector3(b.max.x - 0.22f, b.max.y, b.max.z - 0.18f);
                if (NearEvidence(p)) continue;
                Part(group, "Base de lámpara", PrimitiveType.Cylinder, p + Vector3.up * 0.01f, new Vector3(0.14f, 0.01f, 0.14f), brass);
                Part(group, "Brazo de lámpara", PrimitiveType.Cylinder, p + Vector3.up * 0.17f, new Vector3(0.018f, 0.16f, 0.018f), brass);
                var cap = Part(group, "Pantalla verde", PrimitiveType.Cylinder, p + Vector3.up * 0.34f, new Vector3(0.11f, 0.15f, 0.11f), green);
                cap.transform.rotation = Quaternion.Euler(0, 0, 90);
                var l = new GameObject("Luz de la lámpara de banquero").AddComponent<Light>();
                l.transform.SetParent(group, false);
                l.transform.position = p + Vector3.up * 0.27f;
                l.type = LightType.Point;
                l.color = new Color(1f, 0.82f, 0.55f);
                l.intensity = 0.9f;
                l.range = 2.2f;
                l.shadows = LightShadows.None;
            }

            foreach (var stand in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r => r.name == "Lámpara de pie"))
            {
                var b = stand.bounds;
                var top = new Vector3(b.center.x, b.max.y, b.center.z);
                Part(group, "Pantalla de la lámpara de pie", PrimitiveType.Cylinder, top + Vector3.up * 0.12f, new Vector3(0.38f, 0.14f, 0.38f), shade);
                Part(group, "Bombillo", PrimitiveType.Sphere, top + Vector3.up * 0.05f, Vector3.one * 0.08f, warm);
                var l = new GameObject("Luz de la lámpara de pie").AddComponent<Light>();
                l.transform.SetParent(group, false);
                l.transform.position = top - Vector3.up * 0.05f + Vector3.right * 0.2f;
                l.type = LightType.Point;
                l.color = new Color(1f, 0.75f, 0.45f);
                l.intensity = 1.1f;
                l.range = 3.5f;
                l.shadows = LightShadows.None;
            }
        }

        /// <summary>Tres lámparas colgantes cálidas sobre el centro del salón del café, que quedaba casi a oscuras.</summary>
        static void CafeLamps(Transform root)
        {
            var group = new GameObject("Lámparas del salón").transform;
            group.SetParent(root, false);
            var housing = SceneKit.Mat("dark");
            var shade = SceneKit.Mat("lamp_shade");
            var bulbMat = Emissive("emit_bombillo_calido", new Color(1f, 0.78f, 0.5f), 6f);
            float ceiling = SceneKit.WallHeight, bulbY = ceiling - 0.6f;
            foreach (var p in new[] { new Vector3(0f, 0f, 2.2f), new Vector3(0f, 0f, 4.6f), new Vector3(-2f, 0f, 1.2f) })
            {
                var lamp = Part(group, "Lámpara colgante", PrimitiveType.Cylinder, new Vector3(p.x, (ceiling + bulbY) / 2f + 0.1f, p.z), new Vector3(0.012f, (ceiling - bulbY) / 2f - 0.1f, 0.012f), housing);
                Part(group, "Pantalla", PrimitiveType.Cylinder, new Vector3(p.x, bulbY + 0.07f, p.z), new Vector3(0.36f, 0.07f, 0.36f), shade).transform.SetParent(lamp.transform, true);
                Part(group, "Bombillo", PrimitiveType.Sphere, new Vector3(p.x, bulbY - 0.01f, p.z), Vector3.one * 0.1f, bulbMat).transform.SetParent(lamp.transform, true);
                var l = new GameObject("Luz").AddComponent<Light>();
                l.transform.SetParent(lamp.transform, false);
                l.transform.position = new Vector3(p.x, bulbY - 0.12f, p.z);
                l.type = LightType.Point;
                l.color = new Color(1f, 0.76f, 0.48f);
                l.intensity = 1.3f;
                l.range = 4.5f;
                l.shadows = LightShadows.None;
            }
        }

        /// <summary>
        /// Los parqueaderos son grandes y tenían dos luces: se agrega una cuadrícula de tubos (cada uno con su luz, sin sombra)
        /// cada 4 m. Van dentro de "Ambiente visual", así que el paso se puede repetir sin duplicarlos.
        /// </summary>
        static void ExtraTubes(Transform root)
        {
            var group = new GameObject("Tubos del parqueadero").transform;
            group.SetParent(root, false);
            var housing = SceneKit.Mat("dark");
            var tubeMat = Emissive("emit_tubo", new Color(0.86f, 0.94f, 1f), 3.2f);
            foreach (var f in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r => r.name.StartsWith("Parqueadero") && r.name.EndsWith(" piso")))
            {
                var b = f.bounds;
                int n = 0;
                int every = b.size.x * b.size.z > 150f ? 3 : 2; // parqueaderos grandes: menos luces reales (rendimiento)
                for (float x = b.min.x + 2f; x < b.max.x - 1f; x += 4f)
                    for (float z = b.min.z + 2f; z < b.max.z - 1f; z += 4f)
                    {
                        bool withLight = n++ % every == 0; // los demás tubos solo brillan
                        var p = new Vector3(x, SceneKit.WallHeight, z);
                        var t = Part(group, "Fluorescente (parqueadero)", PrimitiveType.Cube, p + Vector3.down * 0.05f, new Vector3(1.25f, 0.05f, 0.18f), housing);
                        var tube = Part(group, "Tubo", PrimitiveType.Cube, p + Vector3.down * 0.085f, new Vector3(1.18f, 0.03f, 0.1f), tubeMat);
                        tube.transform.SetParent(t.transform, true);
                        if (!withLight) continue;
                        var l = new GameObject("Luz del tubo").AddComponent<Light>();
                        l.transform.SetParent(t.transform, false);
                        l.transform.position = p + Vector3.down * 0.3f;
                        l.type = LightType.Point;
                        l.color = new Color(0.85f, 0.92f, 1f);
                        l.intensity = 2.2f;
                        l.range = 8f;
                        l.shadows = LightShadows.None;
                    }
            }
        }

        static GameObject Part(Transform parent, string name, PrimitiveType type, Vector3 pos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = scale;
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off;
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
            return go;
        }

        /// <summary>
        /// Sombras suaves solo en la luz más fuerte de cada escena (cada luz puntual con sombra cuesta 6 mapas;
        /// limitarlas mantiene los 60 FPS). El resto ilumina sin sombra, y la oclusión horneada pone el contacto.
        /// </summary>
        static void Shadows(List<Light> lights)
        {
            foreach (var l in lights) l.shadows = LightShadows.None;
            foreach (var l in lights.Where(l => !IsGlow(l)).OrderByDescending(l => l.intensity * l.range).Take(1))
            {
                l.shadows = LightShadows.Soft;
                l.shadowStrength = 0.85f;
                l.shadowResolution = LightShadowResolution.Medium;
                l.shadowNearPlane = 0.15f;
            }
        }

        /// <summary>
        /// Una reflection probe horneada por cuarto (por cada piso que hizo FloorAndCeiling), con proyección de caja:
        /// el metal, el vidrio y los pisos brillantes reflejan su propio cuarto y no un cielo genérico.
        /// </summary>
        static void ReflectionProbes(Transform root)
        {
            var group = new GameObject("Reflection probes").transform;
            group.SetParent(root, false);
            foreach (var mr in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
                if (GameObjectUtility.AreStaticEditorFlagsSet(mr.gameObject, StaticEditorFlags.ContributeGI))
                    GameObjectUtility.SetStaticEditorFlags(mr.gameObject, GameObjectUtility.GetStaticEditorFlags(mr.gameObject) | StaticEditorFlags.ReflectionProbeStatic);
            int n = 0;
            foreach (var mr in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).OrderBy(r => r.name))
            {
                if (!mr.name.EndsWith(" piso") && !mr.name.StartsWith("Piso")) continue;
                var b = mr.bounds;
                if (b.size.x < 1.5f || b.size.z < 1.5f || b.size.x * b.size.z > 900f) continue;
                var go = new GameObject($"Probe {mr.name}");
                go.transform.SetParent(group, false);
                go.transform.position = new Vector3(b.center.x, b.max.y + 1.5f, b.center.z);
                var probe = go.AddComponent<ReflectionProbe>();
                probe.mode = ReflectionProbeMode.Baked;
                probe.boxProjection = true;
                probe.size = new Vector3(b.size.x, SceneKit.WallHeight + 0.2f, b.size.z);
                probe.center = new Vector3(0f, b.max.y + SceneKit.WallHeight / 2f - go.transform.position.y, 0f);
                probe.resolution = 128;
                probe.hdr = true;
                probe.blendDistance = 0.5f;
                probe.importance = 1;
                n++;
            }
        }

        /// <summary>
        /// Luz de relleno suave frente a la cara de cada personaje (sin sombra), hija del NPC: si el personaje no aparece,
        /// su luz tampoco. Así los personajes interactivos siempre se distinguen, incluso a contraluz o de noche.
        /// </summary>
        static void CharacterFill(Ambience a)
        {
            foreach (var npc in Object.FindObjectsByType<Caso404.Dialogue.CharacterNpc>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var t = npc.transform;
                for (int i = t.childCount - 1; i >= 0; i--)
                    if (t.GetChild(i).name == "Luz de relleno") Object.DestroyImmediate(t.GetChild(i).gameObject);
                var l = new GameObject("Luz de relleno").AddComponent<Light>();
                l.transform.SetParent(t, false);
                l.transform.localPosition = new Vector3(0.35f, 1.9f, 1.1f);
                l.type = LightType.Point;
                l.range = 1.8f;
                l.intensity = a == Ambience.Night ? 0.9f : 0.55f;
                l.color = new Color(1f, 0.9f, 0.78f);
                l.shadows = LightShadows.None;
                l.lightmapBakeType = LightmapBakeType.Realtime;
            }
        }

        /// <summary>
        /// Frente de los racks de servidores: una textura generada con unidades de 1U y LEDs verdes, ámbar y azules, que
        /// también es el mapa de emisión. Va en un plano por cada cara larga de cada fila (pocos objetos, casi sin costo).
        /// </summary>
        static void ServerRacks(Transform root)
        {
            var racks = Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r => r.name.StartsWith("Fila de racks")).ToList();
            if (racks.Count == 0) return;
            var mat = RackMaterial();
            var group = new GameObject("Frentes de racks").transform;
            group.SetParent(root, false);
            foreach (var r in racks)
            {
                var b = r.bounds;
                foreach (var side in new[] { 1f, -1f })
                {
                    var q = Part(group, "Frente de rack", PrimitiveType.Quad, new Vector3(side > 0 ? b.max.x + 0.004f : b.min.x - 0.004f, b.center.y, b.center.z),
                        new Vector3(b.size.z, b.size.y, 1f), mat);
                    q.transform.rotation = Quaternion.Euler(0f, side > 0 ? -90f : 90f, 0f);
                    var pulse = q.AddComponent<EmissionPulse>();
                    SceneKit.Set(pulse, "_renderer", q.GetComponent<Renderer>());
                    var so = new SerializedObject(pulse);
                    so.FindProperty("_emission").colorValue = Color.white * 6f;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }
        }

        static Material RackMaterial()
        {
            const string texPath = "Assets/_Project/Art/Textures/Procedural/T_rack.png";
            if (!File.Exists(texPath))
            {
                const int w = 128, h = 512;
                var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                var leds = new[] { new Color(0.2f, 1f, 0.35f), new Color(1f, 0.6f, 0.1f), new Color(0.3f, 0.6f, 1f) };
                var rnd = new System.Random(404);
                int unit = 11;
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        int row = y / unit, iy = y % unit;
                        bool rail = x < 6 || x >= w - 6;
                        float g = rail ? 0.09f : (iy == 0 ? 0.02f : 0.055f + 0.02f * ((row * 7) % 3) / 2f);
                        if (!rail && iy > 2 && iy < unit - 2 && (x / 3) % 2 == 0 && x > 70 && x < 118) g *= 0.6f; // rejilla de ventilación
                        tex.SetPixel(x, y, new Color(g, g, g * 1.1f, 1f));
                    }
                for (int row = 0; row < h / unit; row++)
                {
                    int n = 1 + rnd.Next(4);
                    for (int k = 0; k < n; k++)
                    {
                        if (rnd.NextDouble() < 0.2) continue; // algunos apagados
                        var c = leds[rnd.Next(leds.Length)] * (0.7f + (float)rnd.NextDouble() * 0.3f);
                        int cx = 12 + k * 11, cy = row * unit + unit / 2;
                        for (int dy = -1; dy <= 1; dy++)
                            for (int dx = -2; dx <= 2; dx++)
                                if (cy + dy >= 0 && cy + dy < h) tex.SetPixel(cx + dx, cy + dy, c);
                    }
                }
                tex.Apply();
                File.WriteAllBytes(texPath, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(texPath);
                var imp = (TextureImporter)AssetImporter.GetAtPath(texPath);
                imp.filterMode = FilterMode.Point; // LEDs nítidos
                imp.SaveAndReimport();
            }
            var mat = SceneKit.Mat("rack_frente", Color.white, 0.55f, 0.6f);
            var t = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            mat.SetTexture("_BaseMap", t);
            mat.SetTextureScale("_BaseMap", new Vector2(10f, 1f));
            mat.EnableKeyword("_EMISSION");
            mat.SetTexture("_EmissionMap", t);
            mat.SetColor("_EmissionColor", Color.white * 6f);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // ================= Ventanas: rayos de luz y lluvia =================

        /// <summary>
        /// Por cada vidrio grande: de día, rayos de luz que entran inclinados (dos planos cruzados con degradado, casi
        /// transparentes); de noche, lluvia afuera (partículas estiradas). Máximo 4 ventanas por escena.
        /// </summary>
        static void Windows(Transform root, Ambience a)
        {
            var floors = Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r => r.name.EndsWith(" piso") || r.name.StartsWith("Piso")).Select(r => r.bounds).ToList();
            var panes = Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)
                .Where(r => r.sharedMaterial != null && r.sharedMaterial.name == "mat_glass" && !r.transform.IsChildOf(root))
                .Where(r => r.bounds.size.y > 0.8f && Mathf.Max(r.bounds.size.x, r.bounds.size.z) > 1.1f && Mathf.Min(r.bounds.size.x, r.bounds.size.z) < 0.1f)
                .OrderByDescending(r => r.bounds.size.x * r.bounds.size.z + r.bounds.size.y).Take(4).ToList();
            if (panes.Count == 0) return;
            int rainCount = 0;
            var group = new GameObject(a == Ambience.Night ? "Lluvia" : "Rayos de luz").transform;
            group.SetParent(root, false);
            foreach (var pane in panes)
            {
                var b = pane.bounds;
                bool alongX = b.size.x > b.size.z;
                var n = alongX ? Vector3.forward : Vector3.right;
                bool InRoom(Vector3 p) => floors.Any(f => p.x > f.min.x && p.x < f.max.x && p.z > f.min.z && p.z < f.max.z);
                var inward = InRoom(b.center + n * 0.8f) ? n : InRoom(b.center - n * 0.8f) ? -n : Vector3.zero;
                if (inward == Vector3.zero) continue;
                float width = alongX ? b.size.x : b.size.z;
                if (a == Ambience.Night && rainCount++ < 2) Rain(group, b.center - inward * 2.5f, b.max.y, width, alongX);
                else if (a == Ambience.Overcast) Shaft(group, b.center, inward, width, b.size.y);
            }
        }

        static void Shaft(Transform g, Vector3 center, Vector3 inward, float width, float height)
        {
            var mat = ShaftMaterial();
            var d = (inward * Mathf.Cos(35f * Mathf.Deg2Rad) + Vector3.down * Mathf.Sin(35f * Mathf.Deg2Rad)).normalized;
            var across = Vector3.Cross(Vector3.up, inward).normalized;
            const float length = 3.2f;
            for (int i = 0; i < 2; i++)
            {
                var normal = i == 0 ? Vector3.Cross(across, d).normalized : across;
                var q = Part(g, "Rayo de luz", PrimitiveType.Quad, center + d * (length / 2f) + Vector3.up * 0.15f, new Vector3(i == 0 ? width * 0.9f : height * 0.8f, length, 1f), mat);
                q.transform.rotation = Quaternion.LookRotation(normal, d);
            }
        }

        static Material ShaftMaterial()
        {
            const string texPath = "Assets/_Project/Art/Textures/Procedural/T_rayo.png";
            if (!File.Exists(texPath))
            {
                var tex = new Texture2D(64, 256, TextureFormat.RGBA32, false);
                for (int y = 0; y < 256; y++)
                    for (int x = 0; x < 64; x++)
                    {
                        float u = x / 63f, v = y / 255f;
                        float side = Mathf.Sin(u * Mathf.PI);
                        float along = Mathf.Clamp01(v * 4f) * Mathf.Clamp01((1f - v) * 1.4f) * (1f - v * 0.5f);
                        float streak = 0.75f + 0.25f * Mathf.PerlinNoise(u * 6f, 0.5f);
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, side * side * along * streak));
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
            SharedAssetsBuilder.EnsureFolder(FxMatFolder);
            var path = $"{FxMatFolder}/fx_rayo_luz.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) { mat = new Material(Shader.Find("Universal Render Pipeline/Unlit")); AssetDatabase.CreateAsset(mat, path); }
            mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texPath));
            mat.SetColor("_BaseColor", new Color(0.95f, 0.95f, 1f, 0.07f));
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 2f); // aditivo
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)BlendMode.One);
            mat.SetFloat("_ZWrite", 0f);
            mat.SetFloat("_Cull", 0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)RenderQueue.Transparent;
            mat.SetOverrideTag("RenderType", "Transparent");
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static void Rain(Transform g, Vector3 center, float top, float width, bool alongX)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/ParticlesUnlit.mat");
            if (mat == null) return;
            var go = new GameObject("Lluvia");
            go.transform.SetParent(g, false);
            go.transform.position = new Vector3(center.x, top + 4f, center.z);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true;
            main.prewarm = true;
            main.startLifetime = 1.1f;
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.012f, 0.02f);
            main.startColor = new Color(0.7f, 0.78f, 0.9f, 0.35f);
            main.maxParticles = 350;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(0.6f);
            vel.y = new ParticleSystem.MinMaxCurve(-11f);
            vel.z = new ParticleSystem.MinMaxCurve(0f);
            var em = ps.emission;
            em.rateOverTime = 280f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = alongX ? new Vector3(width + 6f, 0.1f, 3f) : new Vector3(3f, 0.1f, width + 6f);
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = mat;
            r.renderMode = ParticleSystemRenderMode.Stretch;
            r.velocityScale = 0.04f;
            r.lengthScale = 2f;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
        }

        /// <summary>Calle mojada de noche: el asfalto y los andenes brillan (MaterialPropertyBlock: no cambia el material del día).</summary>
        static void WetStreets()
        {
            var block = new MaterialPropertyBlock();
            foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                var m = r.sharedMaterial;
                if (m == null || (m.name != "mat_asfalto" && m.name != "mat_anden")) continue;
                r.GetPropertyBlock(block);
                block.SetFloat("_Smoothness", m.name == "mat_asfalto" ? 0.82f : 0.6f);
                r.SetPropertyBlock(block);
            }
        }

        /// <summary>Partículas de polvo que flotan bajo las lámparas (pocas y diminutas: casi no cuestan).</summary>
        static void Dust(Transform root, List<Light> lights, Ambience ambience)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/ParticlesUnlit.mat");
            if (mat == null) return;
            var group = new GameObject("Polvo en la luz").transform;
            group.SetParent(root, false);
            foreach (var l in lights.Where(l => !IsGlow(l)).Take(8))
            {
                var go = new GameObject($"Polvo ({l.name})");
                go.transform.SetParent(group, false);
                go.transform.position = l.transform.position + Vector3.down * 0.9f;
                var ps = go.AddComponent<ParticleSystem>();
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = ps.main;
                main.loop = true;
                main.playOnAwake = true;
                main.duration = 10f;
                main.startLifetime = new ParticleSystem.MinMaxCurve(7f, 12f);
                main.startSpeed = 0f;
                main.startSize = new ParticleSystem.MinMaxCurve(0.008f, 0.02f);
                var c = l.color;
                main.startColor = new Color(Mathf.Lerp(c.r, 1f, 0.5f), Mathf.Lerp(c.g, 1f, 0.5f), Mathf.Lerp(c.b, 1f, 0.5f), ambience == Ambience.Night ? 0.35f : 0.5f);
                main.maxParticles = 40;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.prewarm = true;
                var em = ps.emission;
                em.rateOverTime = 4f;
                var shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Box;
                shape.scale = new Vector3(2.2f, 1.6f, 2.2f);
                var noise = ps.noise;
                noise.enabled = true;
                noise.strength = 0.04f;
                noise.frequency = 0.25f;
                noise.quality = ParticleSystemNoiseQuality.Low;
                var col = ps.colorOverLifetime;
                col.enabled = true;
                var g = new Gradient();
                g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                          new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.3f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
                col.color = g;
                var r = go.GetComponent<ParticleSystemRenderer>();
                r.sharedMaterial = mat;
                r.shadowCastingMode = ShadowCastingMode.Off;
                r.receiveShadows = false;
            }
        }

        // ================= Perfiles de post-procesado =================

        static VolumeProfile LoadOrCreate(string path)
        {
            SharedAssetsBuilder.EnsureFolder(Folder);
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, path);
            }
            foreach (var c in profile.components) Object.DestroyImmediate(c, true);
            profile.components.Clear();
            return profile;
        }

        /// <summary>Profundidad de campo gaussiana (barata) para las conversaciones: nítido hasta 2 m, borroso desde 6 m.</summary>
        static VolumeProfile FocusProfile()
        {
            var profile = LoadOrCreate($"{Folder}/VP_Dialogo.asset");
            var dof = Add<DepthOfField>(profile);
            dof.mode.Override(DepthOfFieldMode.Gaussian);
            dof.gaussianStart.Override(2.2f);
            dof.gaussianEnd.Override(6f);
            dof.gaussianMaxRadius.Override(1.2f);
            dof.highQualitySampling.Override(false);
            var vg = Add<Vignette>(profile);
            vg.intensity.Override(0.42f);
            vg.smoothness.Override(0.5f);
            EditorUtility.SetDirty(profile);
            return profile;
        }

        /// <summary>Perfil de post-procesado de un ambiente (asset en Art/PostProcess). Se rehace cada vez con los mismos valores.</summary>
        public static VolumeProfile Profile(Ambience a)
        {
            var profile = LoadOrCreate($"{Folder}/VP_{a}.asset");

            // Valores por ambiente: exposición, contraste, saturación, temperatura, tinte, bloom y tono de sombras/luces.
            var (exposure, contrast, saturation, temperature, tint, bloom, shadows, highlights) = a switch
            {
                // Día nublado: frío y desaturado, luces interiores cálidas.
                Ambience.Overcast => (1.0f, 20f, -22f, -4f, 0f, 0.35f, new Color(0.46f, 0.5f, 0.53f), new Color(0.58f, 0.54f, 0.48f)),
                // Fluorescente: verdoso, plano y frío, con la lámpara cálida como contraste.
                Ambience.Fluorescent => (0.7f, 22f, -28f, -3f, 6f, 0.4f, new Color(0.46f, 0.52f, 0.5f), new Color(0.58f, 0.55f, 0.48f)),
                // Noche: sombras azules profundas, neón y ventanas que brillan.
                _ => (1.1f, 18f, -12f, -7f, 2f, 0.7f, new Color(0.4f, 0.46f, 0.56f), new Color(0.62f, 0.54f, 0.44f)),
            };

            var tm = Add<Tonemapping>(profile);
            tm.mode.Override(TonemappingMode.ACES);

            var bl = Add<Bloom>(profile);
            bl.threshold.Override(0.95f);
            bl.intensity.Override(bloom);
            bl.scatter.Override(0.7f);
            bl.highQualityFiltering.Override(false);

            var vg = Add<Vignette>(profile);
            vg.color.Override(Color.black);
            vg.intensity.Override(0.34f);
            vg.smoothness.Override(0.45f);

            var fg = Add<FilmGrain>(profile);
            fg.type.Override(FilmGrainLookup.Medium3);
            fg.intensity.Override(0.22f);
            fg.response.Override(0.8f);

            var ca = Add<ColorAdjustments>(profile);
            ca.postExposure.Override(exposure);
            ca.contrast.Override(contrast);
            ca.saturation.Override(saturation);

            var wb = Add<WhiteBalance>(profile);
            wb.temperature.Override(temperature);
            wb.tint.Override(tint);

            var st = Add<SplitToning>(profile);
            st.shadows.Override(shadows);
            st.highlights.Override(highlights);
            st.balance.Override(-15f);

            var smh = Add<ShadowsMidtonesHighlights>(profile);
            smh.shadows.Override(new Vector4(0.96f, 0.98f, 1.05f, -0.02f)); // sombras un poco más hondas y azuladas

            EditorUtility.SetDirty(profile);
            return profile;
        }

        static T Add<T>(VolumeProfile profile) where T : VolumeComponent
        {
            var c = profile.Add<T>(true);
            c.name = typeof(T).Name;
            c.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
            AssetDatabase.AddObjectToAsset(c, profile);
            return c;
        }
    }
}
