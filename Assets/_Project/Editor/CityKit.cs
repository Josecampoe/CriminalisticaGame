using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Caso404.EditorTools
{
    /// <summary>
    /// Valdora alrededor de una escena: cuadras con andenes, calles con líneas, edificios de Kenney City Kit (detallados cerca,
    /// mallas simples lejos), faroles, carros estacionados, árboles, montañas, cielo nublado o nocturno y niebla en el borde.
    /// También levanta el edificio donde está la escena (fachada con ventanas bajo y sobre el piso jugable).
    /// Rendimiento: todo es estático y sin colliders; las piezas repetidas se fusionan en pocas mallas por material.
    /// </summary>
    public static class CityKit
    {
        const string KenneyFolder = "Assets/_Project/Art/ThirdParty/kenney_city-kit-commercial_2.1/Models/FBX format";
        const string MatFolder = "Assets/_Project/Art/Materials";
        const float Pitch = 44f;      // distancia entre ejes de calles
        const float Street = 12f;     // ancho de la calzada (andén incluido en la cuadra)
        const float Curb = 0.15f;     // altura del andén

        static readonly string[] Near = { "building-a", "building-b", "building-c", "building-d", "building-e", "building-f", "building-g",
            "building-h", "building-i", "building-j", "building-k", "building-l", "building-m", "building-n" };
        static readonly string[] Tall = { "building-skyscraper-a", "building-skyscraper-b", "building-skyscraper-c", "building-skyscraper-d", "building-skyscraper-e" };
        static readonly string[] Far = { "low-detail-building-a", "low-detail-building-b", "low-detail-building-c", "low-detail-building-d",
            "low-detail-building-e", "low-detail-building-f", "low-detail-building-g", "low-detail-building-h", "low-detail-building-i",
            "low-detail-building-j", "low-detail-building-k", "low-detail-building-l", "low-detail-building-m", "low-detail-building-wide-a", "low-detail-building-wide-b" };

        /// <summary>Opciones de la ciudad de una escena.</summary>
        public sealed class Options
        {
            /// <summary>Altura de la calle respecto al piso jugable (negativa si la escena está en un piso alto).</summary>
            public float StreetY;
            public bool Night;
            public int Seed = 1;
            /// <summary>Huella (x, z) del edificio donde ocurre la escena. La ciudad no pone nada encima.</summary>
            public Vector2 HostMin = new(-10, -10), HostMax = new(10, 10);
            /// <summary>Altura total del edificio anfitrión sobre la calle (0 = no se dibuja).</summary>
            public float HostHeight;
            /// <summary>Piso y techo del nivel jugable: la fachada del anfitrión no los tapa.</summary>
            public float PlayFloorY = 0f, PlayCeilingY = 2.8f;
            public string HostMaterial = "fachada";
            /// <summary>Centro de la cuadra del anfitrión (null = centro de su huella). Sirve para que la calle quede frente a la fachada.</summary>
            public Vector2? GridCenter;
            /// <summary>Más rascacielos (distrito de la Torre Nexo).</summary>
            public bool Downtown;
            /// <summary>Materiales de la ciudad con emisión controlable (para la Comisaría, que cambia de día a noche).</summary>
            public bool Switchable;
        }

        /// <summary>Renderers con emisión (ventanas y faroles) creados en la última llamada; los usa CaseAtmosphere.</summary>
        public static List<Renderer> LastEmissive { get; } = new();

        public static void Surround(Options o)
        {
            LastEmissive.Clear();
            var root = new GameObject("Ciudad de Valdora").transform;
            var rnd = new System.Random(o.Seed);
            float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);

            SkyAndFog(o.Night);
            var batch = new Batcher(root);

            // La cuadra del anfitrión queda centrada en su huella.
            var hostCenter = o.GridCenter ?? (o.HostMin + o.HostMax) / 2f;
            float y = o.StreetY;
            var asphalt = SceneKit.Mat("asfalto", new Color(0.15f, 0.15f, 0.16f));
            var sidewalk = SceneKit.Mat("anden", new Color(0.5f, 0.5f, 0.48f));
            var line = SceneKit.Mat("linea_calle", new Color(0.85f, 0.85f, 0.8f));
            var buildingMat = BuildingMaterial(o.Night, o.Switchable);
            var lampMat = LampMaterial(o.Night, o.Switchable);
            var pole = SceneKit.Mat("poste", new Color(0.2f, 0.21f, 0.22f), 0.4f, 0.5f);
            var trunk = SceneKit.Mat("tronco", new Color(0.28f, 0.2f, 0.14f));
            var leaves = SceneKit.Mat("follaje", new Color(0.2f, 0.32f, 0.2f));
            var tire = SceneKit.Mat("llanta", new Color(0.05f, 0.05f, 0.05f));
            var carGlass = SceneKit.Mat("vidrio_carro", new Color(0.08f, 0.1f, 0.13f), 0.9f);
            var cable = SceneKit.Mat("cable", new Color(0.05f, 0.05f, 0.05f));
            var lightRed = Glow("semaforo_rojo", new Color(1f, 0.12f, 0.08f), 3f);
            var lightGreen = Glow("semaforo_verde", new Color(0.15f, 1f, 0.45f), 3f);
            var lightOff = SceneKit.Mat("semaforo_apagado", new Color(0.08f, 0.08f, 0.07f), 0.6f);
            Material[] signs =
            {
                Glow(o.Night ? "aviso_rojo_noche" : "aviso_rojo", new Color(1f, 0.15f, 0.2f), o.Night ? 4f : 0.6f),
                Glow(o.Night ? "aviso_cian_noche" : "aviso_cian", new Color(0.2f, 0.9f, 1f), o.Night ? 4f : 0.6f),
                Glow(o.Night ? "aviso_ambar_noche" : "aviso_ambar", new Color(1f, 0.65f, 0.15f), o.Night ? 4f : 0.6f),
                Glow(o.Night ? "aviso_blanco_noche" : "aviso_blanco", new Color(0.95f, 0.93f, 0.85f), o.Night ? 3f : 0.5f),
            };
            var carColors = new[] { "carro_rojo", "carro_blanco", "carro_gris", "carro_azul", "carro_amarillo" };
            var carTints = new[] { new Color(0.55f, 0.1f, 0.1f), new Color(0.85f, 0.85f, 0.83f), new Color(0.4f, 0.42f, 0.44f), new Color(0.14f, 0.22f, 0.45f), new Color(0.85f, 0.65f, 0.1f) };

            // Calzada: un solo plano grande
            batch.Box(asphalt, new Vector3(hostCenter.x, y - 0.05f, hostCenter.y), new Vector3(560, 0.1f, 560), Quaternion.identity);

            const int N = 4;
            for (int i = -N; i <= N; i++)
            for (int j = -N; j <= N; j++)
            {
                var c = new Vector3(hostCenter.x + i * Pitch, y, hostCenter.y + j * Pitch);
                float block = Pitch - Street;
                int ring = Mathf.Max(Mathf.Abs(i), Mathf.Abs(j));
                // Andén (la cuadra entera levantada)
                batch.Box(sidewalk, c + Vector3.up * (Curb / 2f), new Vector3(block, Curb, block), Quaternion.identity);

                if (ring > 2) continue; // lo lejano es un prefab compartido (FarCity)
                // Edificios en una cuadrícula 2x2 dentro de la cuadra
                int cells = 2;
                float cell = (block - 4f) / cells;
                for (int a = 0; a < cells; a++)
                for (int b = 0; b < cells; b++)
                {
                    var p = c + new Vector3(-block / 2f + 2f + cell * (a + 0.5f), Curb, -block / 2f + 2f + cell * (b + 0.5f));
                    if (Overlaps(p, cell / 2f + 2f, o.HostMin, o.HostMax)) continue;
                    // Cada edificio mira a la calle más cercana
                    float yaw = Mathf.Abs(p.x - c.x) > Mathf.Abs(p.z - c.z) ? (p.x > c.x ? 90f : -90f) : (p.z > c.z ? 0f : 180f);
                    {
                        bool tall = rnd.NextDouble() < (o.Downtown ? 0.45 : 0.15) && ring >= 1;
                        string model = tall ? Tall[rnd.Next(Tall.Length)] : Near[rnd.Next(Near.Length)];
                        float s = cell / (tall ? 1.4f : 1.25f) * R(0.85f, 1f);
                        Place(root, model, p, Quaternion.Euler(0, yaw, 0), new Vector3(s, s * R(0.9f, 1.2f), s), buildingMat);
                    }
                }

                if (ring > 1) continue;
                // Faroles y árboles en el borde del andén; carros estacionados junto al andén
                for (int side = 0; side < 4; side++)
                {
                    var dir = side switch { 0 => Vector3.forward, 1 => Vector3.back, 2 => Vector3.right, _ => Vector3.left };
                    var along = new Vector3(dir.z, 0, dir.x);
                    var edge = c + dir * (block / 2f - 0.6f);
                    for (float t = -block / 2f + 4f; t <= block / 2f - 4f; t += 10f)
                    {
                        var lp = edge + along * t + Vector3.up * Curb;
                        if (InsideHost(lp, o.HostMin, o.HostMax, 0.5f)) continue;
                        Lamp(batch, lp, Quaternion.LookRotation(dir), pole, lampMat);
                        if (rnd.NextDouble() < 0.5)
                        {
                            var tp = lp + along * 5f;
                            batch.Box(trunk, tp + Vector3.up * 1.2f, new Vector3(0.25f, 2.4f, 0.25f), Quaternion.identity);
                            batch.Sphere(leaves, tp + Vector3.up * 3.2f, Vector3.one * R(2.2f, 3f));
                        }
                    }
                    var curbLine = c + dir * (block / 2f + 1.3f);
                    for (float t = -block / 2f + 3f; t <= block / 2f - 3f; t += 6f)
                    {
                        if (rnd.NextDouble() < 0.45) continue;
                        int k = rnd.Next(carColors.Length);
                        var body = SceneKit.Mat(carColors[k], carTints[k], 0.6f, 0.3f);
                        Car(batch, curbLine + along * t, Quaternion.LookRotation(along), body, carGlass, tire);
                    }
                }
                // Semáforos en las cuatro esquinas de la cuadra (rojo o verde según la esquina)
                for (int k = 0; k < 4; k++)
                {
                    var corner = c + new Vector3((k % 2 == 0 ? -1 : 1) * (block / 2f - 0.5f), Curb, (k < 2 ? -1 : 1) * (block / 2f - 0.5f));
                    if (InsideHost(corner, o.HostMin, o.HostMax, 1f)) continue;
                    var face = Quaternion.LookRotation(new Vector3(k % 2 == 0 ? -1 : 1, 0, 0));
                    TrafficLight(batch, corner, face, pole, (k + i + j) % 2 == 0 ? lightRed : lightGreen, lightOff);
                }
                // Cables entre los faroles de cada lado (con una leve curva) y avisos en las fachadas
                for (int side = 0; side < 4; side++)
                {
                    var dir = side switch { 0 => Vector3.forward, 1 => Vector3.back, 2 => Vector3.right, _ => Vector3.left };
                    var along = new Vector3(dir.z, 0, dir.x);
                    var edge = c + dir * (block / 2f - 0.6f) + Vector3.up * (Curb + 6.6f);
                    float from = -block / 2f + 4f, to = block / 2f - 4f;
                    if (!InsideHost(edge + along * from, o.HostMin, o.HostMax, 0.5f) && !InsideHost(edge + along * to, o.HostMin, o.HostMax, 0.5f))
                        for (float t = from; t < to - 1f; t += 10f)
                        {
                            var p0 = edge + along * t; var p1 = edge + along * Mathf.Min(t + 10f, to);
                            var mid = (p0 + p1) / 2f + Vector3.down * 0.35f;
                            Segment(batch, cable, p0, mid, 0.025f);
                            Segment(batch, cable, mid, p1, 0.025f);
                        }
                    // Avisos de locales a la altura del primer piso (2 por lado, algunos verticales)
                    for (int n = 0; n < 2; n++)
                    {
                        if (rnd.NextDouble() < 0.35) continue;
                        var sp = c + dir * (block / 2f - 2.15f) + along * R(-block / 3f, block / 3f) + Vector3.up * (Curb + R(3.2f, 4.2f));
                        if (InsideHost(sp, o.HostMin, o.HostMax, 2f)) continue;
                        bool vertical = rnd.NextDouble() < 0.4;
                        var size = vertical ? new Vector3(0.6f, 2.2f, 0.18f) : new Vector3(R(2.2f, 3.6f), 0.7f, 0.18f);
                        var rot = Quaternion.LookRotation(dir);
                        if (vertical) { sp += dir * 0.6f; size = new Vector3(0.18f, 2.2f, 0.6f); rot = Quaternion.identity; }
                        batch.Box(signs[rnd.Next(signs.Length)], sp, size, rot);
                    }
                }
                // Líneas discontinuas en el centro de las calles cercanas
                if (ring <= 1)
                    for (float t = -Pitch / 2f; t < Pitch / 2f; t += 6f)
                    {
                        batch.Box(line, c + new Vector3(Pitch / 2f, 0.005f, t), new Vector3(0.15f, 0.01f, 3f), Quaternion.identity);
                        batch.Box(line, c + new Vector3(t, 0.005f, Pitch / 2f), new Vector3(3f, 0.01f, 0.15f), Quaternion.identity);
                    }
            }

            if (o.HostHeight > 0f) HostBuilding(batch, o);
            var far = (GameObject)PrefabUtility.InstantiatePrefab(FarCityPrefab(o.Downtown), root);
            far.transform.position = new Vector3(hostCenter.x, y, hostCenter.y);
            foreach (var r in far.GetComponentsInChildren<Renderer>()) { r.sharedMaterial = buildingMat; LastEmissive.Add(r); }
            Mountains(batch, hostCenter, y, o.Night, rnd);
            batch.Flush(LastEmissive, lampMat, buildingMat);
            if (o.Night) NightLights(root, hostCenter, o);
            foreach (Transform t in root) foreach (var r in t.GetComponentsInChildren<Renderer>())
                if (r.sharedMaterial == buildingMat && !LastEmissive.Contains(r)) LastEmissive.Add(r);
        }

        /// <summary>Versión corta (compatibilidad): ciudad sin edificio anfitrión.</summary>
        public static void Surround(float streetY, bool night, int seed, float clearRadius = 28f) =>
            Surround(new Options { StreetY = streetY, Night = night, Seed = seed, HostMin = new Vector2(-clearRadius / 2, -clearRadius / 2), HostMax = new Vector2(clearRadius / 2, clearRadius / 2) });

        // ================= Partes =================

        /// <summary>Cielo procedural nublado (día) o nocturno, guardado como asset.</summary>
        public static Material SkyMaterial(bool night)
        {
            var path = $"{MatFolder}/sky_{(night ? "noche" : "nublado")}.mat";
            var sky = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (sky == null) { sky = new Material(Shader.Find("Skybox/Procedural")); AssetDatabase.CreateAsset(sky, path); }
            sky.SetFloat("_SunSize", 0f);
            sky.SetFloat("_AtmosphereThickness", night ? 0.35f : 1.7f);
            sky.SetColor("_SkyTint", night ? new Color(0.12f, 0.14f, 0.24f) : new Color(0.56f, 0.58f, 0.62f));
            sky.SetColor("_GroundColor", night ? new Color(0.05f, 0.05f, 0.07f) : new Color(0.42f, 0.43f, 0.45f));
            sky.SetFloat("_Exposure", night ? 0.3f : 0.95f);
            EditorUtility.SetDirty(sky);
            return sky;
        }

        static void SkyAndFog(bool night)
        {
            RenderSettings.skybox = SkyMaterial(night);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = night ? new Color(0.08f, 0.09f, 0.14f) : new Color(0.62f, 0.64f, 0.67f);
            RenderSettings.fogStartDistance = night ? 90f : 45f;
            RenderSettings.fogEndDistance = night ? 520f : 420f;
        }

        /// <summary>Edificio donde ocurre la escena: volumen con fachada y franjas de ventanas, sin tapar el piso jugable.</summary>
        static void HostBuilding(Batcher batch, Options o)
        {
            var facade = SceneKit.Mat(o.HostMaterial, new Color(0.62f, 0.6f, 0.56f));
            var window = SceneKit.Mat("ventanas_fachada", o.Night ? new Color(0.35f, 0.3f, 0.18f) : new Color(0.2f, 0.24f, 0.28f), 0.85f);
            var min = o.HostMin; var max = o.HostMax;
            var size = max - min; var mid = (min + max) / 2f;
            float bottom = o.StreetY, top = o.StreetY + o.HostHeight;
            void Slab(float y0, float y1)
            {
                if (y1 - y0 < 0.05f) return;
                batch.Box(facade, new Vector3(mid.x, (y0 + y1) / 2f, mid.y), new Vector3(size.x, y1 - y0, size.y), Quaternion.identity);
                // Franjas de ventanas en las cuatro fachadas, una por piso
                for (float fy = y0 + 1.2f; fy < y1 - 0.8f; fy += 3f)
                {
                    batch.Box(window, new Vector3(mid.x, fy, min.y - 0.03f), new Vector3(size.x - 1f, 1.3f, 0.06f), Quaternion.identity);
                    batch.Box(window, new Vector3(mid.x, fy, max.y + 0.03f), new Vector3(size.x - 1f, 1.3f, 0.06f), Quaternion.identity);
                    batch.Box(window, new Vector3(min.x - 0.03f, fy, mid.y), new Vector3(0.06f, 1.3f, size.y - 1f), Quaternion.identity);
                    batch.Box(window, new Vector3(max.x + 0.03f, fy, mid.y), new Vector3(0.06f, 1.3f, size.y - 1f), Quaternion.identity);
                }
            }
            Slab(bottom, o.PlayFloorY - 0.12f);
            Slab(o.PlayCeilingY + 0.12f, top);
        }

        static void Lamp(Batcher b, Vector3 basePos, Quaternion facing, Material pole, Material lamp)
        {
            b.Box(pole, basePos + Vector3.up * 3f, new Vector3(0.14f, 6f, 0.14f), Quaternion.identity);
            var arm = basePos + Vector3.up * 5.9f + facing * Vector3.forward * 0.7f;
            b.Box(pole, arm, new Vector3(0.1f, 0.1f, 1.4f), facing);
            b.Box(lamp, arm + facing * Vector3.forward * 0.7f + Vector3.down * 0.12f, new Vector3(0.35f, 0.15f, 0.6f), facing);
        }

        /// <summary>Semáforo: poste, caja oscura y tres luces (una encendida).</summary>
        static void TrafficLight(Batcher b, Vector3 basePos, Quaternion facing, Material pole, Material lit, Material off)
        {
            b.Box(pole, basePos + Vector3.up * 1.6f, new Vector3(0.12f, 3.2f, 0.12f), Quaternion.identity);
            var head = basePos + Vector3.up * 3.55f;
            b.Box(pole, head, new Vector3(0.32f, 0.9f, 0.28f), facing);
            for (int k = 0; k < 3; k++)
            {
                var lp = head + Vector3.up * (0.28f - k * 0.28f) + facing * Vector3.forward * 0.15f;
                bool on = (k == 0 && lit.name.Contains("rojo")) || (k == 2 && lit.name.Contains("verde"));
                b.Box(on ? lit : off, lp, new Vector3(0.18f, 0.18f, 0.04f), facing);
            }
        }

        /// <summary>Cable recto entre dos puntos (caja delgada).</summary>
        static void Segment(Batcher b, Material mat, Vector3 a, Vector3 c, float thickness)
        {
            var d = c - a;
            if (d.sqrMagnitude < 0.01f) return;
            b.Box(mat, (a + c) / 2f, new Vector3(thickness, thickness, d.magnitude), Quaternion.LookRotation(d));
        }

        /// <summary>Material con emisión (luces de semáforo, avisos).</summary>
        static Material Glow(string key, Color color, float intensity)
        {
            var mat = SceneKit.Mat(key, color * 0.6f, 0.5f);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * intensity);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static void Car(Batcher b, Vector3 pos, Quaternion rot, Material body, Material glass, Material tire)
        {
            Vector3 P(float x, float yy, float z) => pos + rot * new Vector3(x, yy, z);
            b.Box(body, P(0, 0.6f, 0), new Vector3(1.8f, 0.7f, 4.2f), rot);
            b.Box(glass, P(0, 1.15f, -0.2f), new Vector3(1.6f, 0.5f, 2.2f), rot);
            foreach (var x in new[] { -0.85f, 0.85f })
                foreach (var z in new[] { -1.35f, 1.35f })
                    b.Box(tire, P(x, 0.32f, z), new Vector3(0.25f, 0.64f, 0.64f), rot);
        }

        static void Mountains(Batcher batch, Vector2 center, float y, bool night, System.Random rnd)
        {
            var mat = SceneKit.Mat(night ? "montana_noche" : "montana", night ? new Color(0.05f, 0.06f, 0.09f) : new Color(0.34f, 0.4f, 0.38f));
            var snow = SceneKit.Mat(night ? "nieve_noche" : "nieve", night ? new Color(0.25f, 0.27f, 0.32f) : new Color(0.85f, 0.87f, 0.9f));
            for (int i = 0; i < 26; i++)
            {
                float ang = i / 26f * Mathf.PI * 2f + (float)rnd.NextDouble() * 0.12f;
                float d = 360f + (float)rnd.NextDouble() * 60f;
                float w = 120f + (float)rnd.NextDouble() * 120f, h = 140f + (float)rnd.NextDouble() * 160f;
                var p = new Vector3(center.x + Mathf.Cos(ang) * d, y - 30f, center.y + Mathf.Sin(ang) * d);
                batch.Sphere(mat, p, new Vector3(w, h * 2f, w * 0.8f));
                if (h > 240f) batch.Sphere(snow, p + Vector3.up * (h * 0.86f), new Vector3(w * 0.3f, h * 0.3f, w * 0.25f));
            }
        }

        /// <summary>
        /// Anillo lejano de la ciudad (cuadras 3 y 4 alrededor): mallas simples de Kenney fusionadas en un prefab compartido
        /// por todas las escenas. Así el peso queda en un solo archivo y no se repite en cada escena.
        /// </summary>
        public static GameObject FarCityPrefab(bool downtown)
        {
            const string folder = "Assets/_Project/Art/City";
            string name = downtown ? "ciudad_lejana_centro" : "ciudad_lejana";
            var prefabPath = $"{folder}/{name}.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (existing != null && !_rebuildFar) return existing;
            SharedAssetsBuilder.EnsureFolder(folder);
            var rnd = new System.Random(downtown ? 77 : 33);
            float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
            var parts = new List<CombineInstance>();
            const int N = 4;
            float block = Pitch - Street;
            float cell = (block - 4f) / 2f;
            for (int i = -N; i <= N; i++)
            for (int j = -N; j <= N; j++)
            {
                if (Mathf.Max(Mathf.Abs(i), Mathf.Abs(j)) <= 2) continue;
                var c = new Vector3(i * Pitch, 0, j * Pitch);
                for (int a = 0; a < 2; a++)
                for (int b = 0; b < 2; b++)
                {
                    var p = c + new Vector3(-block / 2f + 2f + cell * (a + 0.5f), Curb, -block / 2f + 2f + cell * (b + 0.5f));
                    string model = Far[rnd.Next(Far.Length)];
                    var src = AssetDatabase.LoadAssetAtPath<GameObject>($"{KenneyFolder}/{model}.fbx");
                    if (src == null) continue;
                    float s = cell / (model.Contains("wide") ? 1f : 0.55f) * 0.9f;
                    float h = downtown ? R(1.2f, 2.2f) : R(0.7f, 1.4f);
                    var m = Matrix4x4.TRS(p, Quaternion.Euler(0, Mathf.Round(R(0, 4)) * 90f, 0), new Vector3(s, s * h * 0.5f, s));
                    foreach (var mf in src.GetComponentsInChildren<MeshFilter>())
                        parts.Add(new CombineInstance { mesh = mf.sharedMesh, transform = m * src.transform.worldToLocalMatrix * mf.transform.localToWorldMatrix });
                }
            }
            var mesh = new Mesh { name = name, indexFormat = IndexFormat.UInt32 };
            mesh.CombineMeshes(parts.ToArray(), true, true);
            mesh.tangents = null;
            mesh.RecalculateBounds();
            var meshPath = $"{folder}/{name}_malla.asset";
            AssetDatabase.DeleteAsset(meshPath);
            AssetDatabase.CreateAsset(mesh, meshPath);
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            var rr = go.GetComponent<MeshRenderer>();
            rr.sharedMaterial = BuildingMaterial(false, false);
            rr.shadowCastingMode = ShadowCastingMode.Off;
            Static(go);
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);
            return prefab;
        }

        static bool _rebuildFar;

        /// <summary>Regenera los prefabs de la ciudad lejana (lo llama "Construir todo").</summary>
        public static void RebuildFarCity()
        {
            _rebuildFar = true;
            FarCityPrefab(false);
            FarCityPrefab(true);
            _rebuildFar = false;
        }

        /// <summary>De noche, unas pocas luces reales de farol cerca de la escena (las demás solo brillan).</summary>
        static void NightLights(Transform root, Vector2 center, Options o)
        {
            int n = 0;
            for (int i = -1; i <= 1 && n < 6; i++)
                for (int j = -1; j <= 1 && n < 6; j++)
                {
                    if (i == 0 && j == 0) continue;
                    var l = new GameObject("Luz de farol").AddComponent<Light>();
                    l.transform.SetParent(root, false);
                    l.transform.position = new Vector3(center.x + i * Pitch / 2f, o.StreetY + 5.6f, center.y + j * Pitch / 2f);
                    l.type = LightType.Point;
                    l.color = new Color(1f, 0.78f, 0.45f);
                    l.intensity = 6f;
                    l.range = 22f;
                    l.shadows = LightShadows.None;
                    n++;
                }
        }

        static bool Overlaps(Vector3 p, float half, Vector2 min, Vector2 max) =>
            p.x + half > min.x && p.x - half < max.x && p.z + half > min.y && p.z - half < max.y;

        static bool InsideHost(Vector3 p, Vector2 min, Vector2 max, float margin) =>
            p.x > min.x - margin && p.x < max.x + margin && p.z > min.y - margin && p.z < max.y + margin;

        static void Place(Transform root, string model, Vector3 pos, Quaternion rot, Vector3 scale, Material mat)
        {
            var src = AssetDatabase.LoadAssetAtPath<GameObject>($"{KenneyFolder}/{model}.fbx");
            if (src == null) return;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(src, root);
            go.transform.SetPositionAndRotation(pos, rot);
            go.transform.localScale = scale;
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                r.sharedMaterial = mat;
                r.shadowCastingMode = ShadowCastingMode.Off;
            }
            Static(go);
        }

        /// <summary>Material de los edificios. De noche las zonas claras del colormap (ventanas) brillan.</summary>
        static Material BuildingMaterial(bool night, bool switchable)
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{KenneyFolder}/Textures/colormap.png");
            string name = switchable ? "comisaria" : night ? "noche" : "dia";
            var path = $"{MatFolder}/mat_ciudad_{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat, path); }
            mat.SetTexture("_BaseMap", tex);
            mat.SetColor("_BaseColor", night ? new Color(0.3f, 0.32f, 0.4f) : new Color(0.85f, 0.85f, 0.85f));
            mat.SetFloat("_Smoothness", 0.1f);
            if (night || switchable)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetTexture("_EmissionMap", tex);
                mat.SetColor("_EmissionColor", night ? new Color(1.1f, 0.88f, 0.5f) : Color.black);
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }
            else mat.DisableKeyword("_EMISSION");
            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static Material LampMaterial(bool night, bool switchable)
        {
            string name = switchable ? "farol_comisaria" : night ? "farol_noche" : "farol_dia";
            var mat = SceneKit.Mat(name, new Color(0.9f, 0.88f, 0.8f));
            if (night || switchable)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", night ? new Color(2.2f, 1.7f, 0.9f) : Color.black);
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }
            else mat.DisableKeyword("_EMISSION");
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static void Static(GameObject go)
        {
            foreach (var t in go.GetComponentsInChildren<Transform>())
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);
        }

        // ================= Fusión de mallas =================

        /// <summary>Acumula cajas, cilindros y esferas por material y los fusiona en una malla por material.</summary>
        sealed class Batcher
        {
            readonly Transform _root;
            readonly Dictionary<Material, List<CombineInstance>> _parts = new();
            static Mesh _cube, _cylinder, _sphere;

            public Batcher(Transform root) { _root = root; }

            static Mesh Prim(PrimitiveType t)
            {
                var go = GameObject.CreatePrimitive(t);
                var m = go.GetComponent<MeshFilter>().sharedMesh;
                Object.DestroyImmediate(go);
                return m;
            }

            void Add(Material mat, Mesh mesh, Matrix4x4 m)
            {
                if (!_parts.TryGetValue(mat, out var list)) _parts[mat] = list = new List<CombineInstance>();
                list.Add(new CombineInstance { mesh = mesh, transform = m });
            }

            public void Box(Material mat, Vector3 pos, Vector3 size, Quaternion rot) => Add(mat, _cube ??= Prim(PrimitiveType.Cube), Matrix4x4.TRS(pos, rot, size));
            public void Cylinder(Material mat, Vector3 pos, Vector3 size) => Add(mat, _cylinder ??= Prim(PrimitiveType.Cylinder), Matrix4x4.TRS(pos, Quaternion.identity, size));
            public void Sphere(Material mat, Vector3 pos, Vector3 size) => Add(mat, _sphere ??= LowPolySphere(), Matrix4x4.TRS(pos, Quaternion.identity, size));

            /// <summary>Esfera de pocas caras (diámetro 1): estilo low-poly y escenas livianas.</summary>
            static Mesh LowPolySphere()
            {
                const int lat = 6, lon = 9;
                var verts = new List<Vector3>();
                var tris = new List<int>();
                for (int a = 0; a <= lat; a++)
                {
                    float phi = Mathf.PI * a / lat;
                    for (int b = 0; b <= lon; b++)
                    {
                        float th = 2f * Mathf.PI * b / lon;
                        verts.Add(0.5f * new Vector3(Mathf.Sin(phi) * Mathf.Cos(th), Mathf.Cos(phi), Mathf.Sin(phi) * Mathf.Sin(th)));
                    }
                }
                for (int a = 0; a < lat; a++)
                    for (int b = 0; b < lon; b++)
                    {
                        int i0 = a * (lon + 1) + b, i1 = i0 + lon + 1;
                        tris.AddRange(new[] { i0, i0 + 1, i1, i1, i0 + 1, i1 + 1 });
                    }
                var m = new Mesh { name = "esfera_lowpoly" };
                m.SetVertices(verts);
                m.SetTriangles(tris, 0);
                m.SetUVs(0, verts.ConvertAll(v => new Vector2(v.x + 0.5f, v.y + 0.5f)));
                m.RecalculateNormals();
                m.RecalculateTangents();
                return m;
            }

            /// <summary>Agrega las mallas de un modelo (FBX) con la transformación dada.</summary>
            public void Model(Material mat, string fbxPath, Matrix4x4 m)
            {
                var src = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
                if (src == null) return;
                foreach (var mf in src.GetComponentsInChildren<MeshFilter>())
                    Add(mat, mf.sharedMesh, m * src.transform.worldToLocalMatrix * mf.transform.localToWorldMatrix);
            }

            public void Flush(List<Renderer> emissive, Material lampMat, Material buildingMat)
            {
                foreach (var kv in _parts)
                {
                    var list = kv.Value;
                    for (int start = 0, part = 0; start < list.Count; start += 4000, part++)
                    {
                        var chunk = list.GetRange(start, Mathf.Min(4000, list.Count - start));
                        var mesh = new Mesh { name = $"ciudad_{kv.Key.name}_{part}", indexFormat = IndexFormat.UInt32 };
                        mesh.CombineMeshes(chunk.ToArray(), true, true);
                        // Materiales de color liso: sin UV ni tangentes, la escena pesa la mitad.
                        if (!kv.Key.HasProperty("_BaseMap") || kv.Key.GetTexture("_BaseMap") == null) mesh.uv = null;
                        mesh.tangents = null;
                        mesh.RecalculateBounds();
                        var go = new GameObject(mesh.name, typeof(MeshFilter), typeof(MeshRenderer));
                        go.transform.SetParent(_root, false);
                        go.GetComponent<MeshFilter>().sharedMesh = mesh;
                        var r = go.GetComponent<MeshRenderer>();
                        r.sharedMaterial = kv.Key;
                        r.shadowCastingMode = ShadowCastingMode.Off;
                        Static(go);
                        if (kv.Key == lampMat || kv.Key == buildingMat) emissive.Add(r);
                    }
                }
                _parts.Clear();
            }
        }
    }
}
