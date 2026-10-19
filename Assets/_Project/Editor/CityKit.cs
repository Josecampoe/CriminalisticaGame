using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Caso404.EditorTools
{
    /// <summary>
    /// Ciudad de Valdora alrededor de una escena (versión básica): suelo de calle, anillo de edificios de Kenney City Kit,
    /// montañas lejanas, cielo nublado (día) o nocturno con ventanas encendidas, y niebla que oculta el borde del mundo.
    /// Todo es estático y sin colliders (el jugador no puede llegar allí).
    /// </summary>
    public static class CityKit
    {
        const string KenneyFolder = "Assets/_Project/Art/ThirdParty/kenney_city-kit-commercial_2.1/Models/FBX format";
        const string MatFolder = "Assets/_Project/Art/Materials";

        static readonly string[] Near = { "building-a", "building-b", "building-c", "building-d", "building-e", "building-f", "building-g",
            "building-h", "building-i", "building-j", "building-k", "building-l", "building-m", "building-n" };
        static readonly string[] Tall = { "building-skyscraper-a", "building-skyscraper-b", "building-skyscraper-c", "building-skyscraper-d", "building-skyscraper-e" };
        static readonly string[] Far = { "low-detail-building-a", "low-detail-building-b", "low-detail-building-c", "low-detail-building-d",
            "low-detail-building-e", "low-detail-building-f", "low-detail-building-g", "low-detail-building-h", "low-detail-building-wide-a", "low-detail-building-wide-b" };

        /// <summary>Arma la ciudad. streetY: altura de la calle respecto al piso de la escena (negativa si la escena está en un piso alto).</summary>
        public static void Surround(float streetY, bool night, int seed, float clearRadius = 28f)
        {
            var root = new GameObject("Ciudad de Valdora").transform;
            var rnd = new System.Random(seed);
            float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);

            // Cielo y niebla
            var sky = new Material(Shader.Find("Skybox/Procedural"));
            sky.SetFloat("_SunSize", 0f);
            sky.SetFloat("_AtmosphereThickness", night ? 0.4f : 1.6f);
            sky.SetColor("_SkyTint", night ? new Color(0.1f, 0.12f, 0.2f) : new Color(0.55f, 0.57f, 0.6f));
            sky.SetColor("_GroundColor", night ? new Color(0.05f, 0.05f, 0.07f) : new Color(0.4f, 0.41f, 0.43f));
            sky.SetFloat("_Exposure", night ? 0.25f : 0.9f);
            var skyPath = $"{MatFolder}/sky_{(night ? "noche" : "nublado")}.mat";
            AssetDatabase.DeleteAsset(skyPath);
            AssetDatabase.CreateAsset(sky, skyPath);
            RenderSettings.skybox = sky;
            var fog = night ? new Color(0.07f, 0.08f, 0.12f) : new Color(0.62f, 0.64f, 0.67f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = fog;
            RenderSettings.fogStartDistance = 30f;
            RenderSettings.fogEndDistance = night ? 320f : 420f;

            // Suelo (reemplaza al horizonte infinito del cielo por defecto)
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Calles";
            Object.DestroyImmediate(ground.GetComponent<Collider>());
            ground.transform.SetParent(root, false);
            ground.transform.position = new Vector3(0, streetY, 0);
            ground.transform.localScale = new Vector3(120, 1, 120);
            ground.GetComponent<Renderer>().sharedMaterial = SceneKit.Mat("asfalto", new Color(0.16f, 0.16f, 0.17f));
            Static(ground);

            var buildingMat = BuildingMaterial(night);
            // Anillo cercano: edificios detallados; anillo lejano: mallas simples.
            for (int ring = 0; ring < 6; ring++)
            {
                float dist = clearRadius + 12f + ring * 26f;
                int count = Mathf.RoundToInt(2f * Mathf.PI * dist / 22f);
                for (int i = 0; i < count; i++)
                {
                    float ang = (i + R(-0.25f, 0.25f)) / count * Mathf.PI * 2f;
                    var pos = new Vector3(Mathf.Cos(ang) * dist, streetY, Mathf.Sin(ang) * dist);
                    string model = ring < 3
                        ? (rnd.NextDouble() < 0.18 ? Tall[rnd.Next(Tall.Length)] : Near[rnd.Next(Near.Length)])
                        : Far[rnd.Next(Far.Length)];
                    float s = ring < 3 ? R(11f, 14f) : R(18f, 24f);
                    Place(root, model, pos, Quaternion.Euler(0, Mathf.Round(R(0, 4)) * 90f, 0), new Vector3(s, s * R(0.9f, 1.25f), s), buildingMat);
                }
            }

            // Montañas andinas a lo lejos (siluetas suavizadas por la niebla)
            var mountainMat = SceneKit.Mat(night ? "montana_noche" : "montana", night ? new Color(0.06f, 0.07f, 0.1f) : new Color(0.36f, 0.42f, 0.4f));
            for (int i = 0; i < 18; i++)
            {
                float ang = i / 18f * Mathf.PI * 2f + R(-0.1f, 0.1f);
                float d = R(330f, 400f);
                var m = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                m.name = "Montaña";
                Object.DestroyImmediate(m.GetComponent<Collider>());
                m.transform.SetParent(root, false);
                float w = R(120f, 220f);
                m.transform.position = new Vector3(Mathf.Cos(ang) * d, streetY - 20f, Mathf.Sin(ang) * d);
                m.transform.localScale = new Vector3(w, R(120f, 260f), w * 0.8f);
                m.GetComponent<Renderer>().sharedMaterial = mountainMat;
                Static(m);
            }
        }

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

        /// <summary>Material de los edificios: de noche las ventanas (zonas claras del colormap) brillan.</summary>
        static Material BuildingMaterial(bool night)
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{KenneyFolder}/Textures/colormap.png");
            var path = $"{MatFolder}/mat_ciudad_{(night ? "noche" : "dia")}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat, path); }
            mat.SetTexture("_BaseMap", tex);
            mat.SetColor("_BaseColor", night ? new Color(0.35f, 0.37f, 0.45f) : new Color(0.85f, 0.85f, 0.85f));
            mat.SetFloat("_Smoothness", 0.1f);
            if (night)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetTexture("_EmissionMap", tex);
                mat.SetColor("_EmissionColor", new Color(0.55f, 0.45f, 0.25f));
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }
            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static void Static(GameObject go)
        {
            foreach (var t in go.GetComponentsInChildren<Transform>())
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);
        }
    }
}
