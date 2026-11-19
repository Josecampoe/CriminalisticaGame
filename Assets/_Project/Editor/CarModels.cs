using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Caso404.EditorTools
{
    /// <summary>
    /// Carros low-poly del paquete Cars de Quaternius (CC0). Reemplazan a las cajas de los parqueaderos y de la calle.
    /// Cada material del modelo se cambia por uno propio (vidrio oscuro con brillo, llanta mate, rin metálico y la
    /// pintura del color que pida la escena), así combinan con la luz noir del juego.
    /// </summary>
    public static class CarModels
    {
        public const string Folder = "Assets/_Project/Art/ThirdParty/Quaternius_Cars";

        /// <summary>Modelos de uso particular (sin taxi ni patrulla), para parqueaderos y calles.</summary>
        public static readonly string[] Civil = { "NormalCar1", "NormalCar2", "SUV", "SportsCar2", "NormalCar1", "NormalCar2" };

        /// <summary>Colores de pintura sobrios (gris, plata, vino, azul noche, blanco, verde oscuro).</summary>
        public static readonly Color[] Paints =
        {
            new(0.2f, 0.21f, 0.22f), new(0.58f, 0.6f, 0.62f), new(0.36f, 0.08f, 0.09f),
            new(0.12f, 0.16f, 0.28f), new(0.8f, 0.8f, 0.78f), new(0.13f, 0.22f, 0.18f),
        };

        public static string PathOf(string model) => $"{Folder}/{model}.fbx";

        /// <summary>Taxi y patrulla conservan sus colores; los demás llevan la pintura que se les pida.</summary>
        static bool Repaintable(string model) => model != "Taxi" && model != "Cop";

        /// <summary>
        /// Coloca un carro con las ruedas en <paramref name="floorPos"/> mirando hacia <paramref name="rotY"/> grados.
        /// Tiene una caja de colisión para que el jugador no lo atraviese.
        /// </summary>
        public static GameObject Place(Transform parent, string name, string model, Vector3 floorPos, float rotY, Color paint)
        {
            var src = AssetDatabase.LoadAssetAtPath<GameObject>(PathOf(model));
            if (src == null) { Debug.LogError($"[Caso404] Falta el modelo de carro {model}"); return null; }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(src);
            PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            go.name = name;
            if (parent != null) go.transform.SetParent(parent, true);
            go.transform.SetPositionAndRotation(floorPos, Quaternion.Euler(0f, rotY, 0f));
            foreach (var r in go.GetComponentsInChildren<MeshRenderer>())
                r.sharedMaterials = Remap(r, model, paint);

            var bounds = go.GetComponentsInChildren<Renderer>().Select(r => r.bounds).Aggregate((a, b) => { a.Encapsulate(b); return a; });
            var col = go.AddComponent<BoxCollider>();
            col.center = go.transform.InverseTransformPoint(bounds.center);
            var local = Quaternion.Inverse(go.transform.rotation) * bounds.size;
            col.size = new Vector3(Mathf.Abs(local.x), Mathf.Abs(local.y), Mathf.Abs(local.z)) * 0.96f;
            foreach (var t in go.GetComponentsInChildren<Transform>())
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.ContributeGI | StaticEditorFlags.OccludeeStatic);
            return go;
        }

        /// <summary>Materiales propios para un renderer del modelo (la pintura va en el primer material de la carrocería).</summary>
        public static Material[] Remap(Renderer r, string model, Color paint)
        {
            bool body = r.name == model;
            return r.sharedMaterials.Select((m, i) => MaterialFor(m != null ? m.name : "Grey", body && i == 0 && Repaintable(model), paint)).ToArray();
        }

        /// <summary>Material por nombre del material del paquete. <paramref name="isPaint"/>: la carrocería del color pedido.</summary>
        public static Material MaterialFor(string source, bool isPaint, Color paint)
        {
            if (isPaint) return SceneKit.Mat($"carro_pintura_{ColorUtility.ToHtmlStringRGB(paint)}", paint, 0.62f, 0.25f);
            return source switch
            {
                "Windows" => SceneKit.Mat("carro_vidrio", new Color(0.05f, 0.06f, 0.08f), 0.92f),
                "Headlights" => SceneKit.Mat("carro_faro", new Color(0.86f, 0.84f, 0.76f), 0.9f),
                "TailLights" => SceneKit.Mat("carro_luz_trasera", new Color(0.5f, 0.06f, 0.05f), 0.8f),
                "WhiteLights" => SceneKit.Mat("carro_luz_blanca", new Color(0.9f, 0.9f, 0.9f), 0.8f),
                "BlueLights" => SceneKit.Mat("carro_luz_azul", new Color(0.15f, 0.3f, 0.75f), 0.8f),
                "Black" => SceneKit.Mat("carro_negro_mate", new Color(0.05f, 0.05f, 0.055f), 0.25f),
                "Grey" => SceneKit.Mat("carro_rin", new Color(0.45f, 0.46f, 0.48f), 0.6f, 0.6f),
                "Yellow" => SceneKit.Mat("carro_taxi", new Color(0.85f, 0.68f, 0.15f), 0.62f, 0.2f),
                "White" => SceneKit.Mat("carro_blanco", new Color(0.82f, 0.82f, 0.8f), 0.6f, 0.2f),
                _ => SceneKit.Mat("carro_detalle", new Color(0.7f, 0.71f, 0.72f), 0.5f, 0.3f),
            };
        }
    }
}
