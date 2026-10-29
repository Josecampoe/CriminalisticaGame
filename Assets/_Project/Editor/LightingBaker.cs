using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Caso404.EditorTools
{
    /// <summary>
    /// Hornea la iluminación de las escenas: todas las luces quedan mixtas (luz directa en tiempo real, rebotes y oclusión
    /// ambiental horneados) y se crea una cuadrícula de light probes para que los personajes reciban la luz indirecta.
    /// La ciudad no se hornea (no contribuye a la iluminación global): así el horneado es rápido.
    /// Uso: menú Caso404/Hornear iluminación (después de "Construir todo"), o -executeMethod Caso404.EditorTools.LightingBaker.BakeAll
    /// </summary>
    public static class LightingBaker
    {
        [MenuItem("Caso404/Hornear iluminación (todas las escenas)")]
        public static void BakeAll()
        {
            foreach (var path in SceneKit.BuildOrder)
                if (!path.EndsWith("MainMenu.unity") && File.Exists(path)) BakeScene(path);
        }

        /// <summary>Hornea solo el apartamento (para probar rápido).</summary>
        public static void BakeApartment() => BakeScene(Apartment404SceneBuilder.ScenePath);

        public static void BakeScene(string path)
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            var t0 = EditorApplication.timeSinceStartup;

            foreach (var light in Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (light.name != "Luz de relleno") light.lightmapBakeType = LightmapBakeType.Mixed; // luz directa en tiempo real + rebotes horneados

            AddProbes();

            var name = Path.GetFileNameWithoutExtension(path);
            var settingsPath = $"{SceneKit.ScenesFolder}/{name}_Iluminacion.lighting";
            var settings = AssetDatabase.LoadAssetAtPath<LightingSettings>(settingsPath);
            if (settings == null) { settings = new LightingSettings(); AssetDatabase.CreateAsset(settings, settingsPath); }
            settings.bakedGI = true;
            settings.realtimeGI = false;
            settings.lightmapper = LightingSettings.Lightmapper.ProgressiveGPU;
            settings.mixedBakeMode = MixedLightingMode.IndirectOnly;
            settings.lightmapResolution = 8f;
            settings.lightmapPadding = 2;
            settings.lightmapMaxSize = 1024;
            settings.directSampleCount = 16;
            settings.indirectSampleCount = 64;
            settings.environmentSampleCount = 32;
            settings.maxBounces = 2;
            settings.ao = true;
            settings.aoMaxDistance = 0.6f;
            settings.lightmapCompression = LightmapCompression.NormalQuality;
            settings.filteringMode = LightingSettings.FilterMode.Auto;
            EditorUtility.SetDirty(settings);
            Lightmapping.lightingSettings = settings;

            bool ok = Lightmapping.Bake();
            BakeOcclusion();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[Caso404] Iluminación horneada en {name}: {(ok ? "OK" : "FALLÓ")} ({EditorApplication.timeSinceStartup - t0:F0} s)");
        }

        /// <summary>
        /// Occlusion culling: las paredes del nivel jugable tapan (Occluder) y los edificios de la ciudad se ocultan
        /// (Occludee). Así no se dibuja la ciudad que queda detrás de las paredes: era el mayor costo en los interiores.
        /// </summary>
        static void BakeOcclusion()
        {
            var city = GameObject.Find("Ciudad de Valdora");
            if (city != null)
                foreach (var r in city.GetComponentsInChildren<Renderer>(true))
                    GameObjectUtility.SetStaticEditorFlags(r.gameObject, GameObjectUtility.GetStaticEditorFlags(r.gameObject) | StaticEditorFlags.OccludeeStatic);
            // Los vidrios no tapan: por las ventanas se tiene que seguir viendo la ciudad.
            foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
                if (r.sharedMaterial != null && r.sharedMaterial.name == "mat_glass")
                    GameObjectUtility.SetStaticEditorFlags(r.gameObject, GameObjectUtility.GetStaticEditorFlags(r.gameObject) & ~StaticEditorFlags.OccluderStatic);
            StaticOcclusionCulling.smallestOccluder = 1.5f;
            StaticOcclusionCulling.smallestHole = 0.25f;
            StaticOcclusionCulling.backfaceThreshold = 100f;
            StaticOcclusionCulling.Compute();
        }

        /// <summary>Cuadrícula de light probes sobre el piso jugable (cada 2 m, a 0.6 m y 1.7 m de altura).</summary>
        static void AddProbes()
        {
            var old = Object.FindAnyObjectByType<LightProbeGroup>();
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var bounds = new Bounds();
            bool any = false;
            foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                if (!GameObjectUtility.AreStaticEditorFlagsSet(r.gameObject, StaticEditorFlags.ContributeGI)) continue;
                if (r.bounds.center.y < -1f || r.bounds.size.y > 10f) continue;
                if (!any) { bounds = r.bounds; any = true; } else bounds.Encapsulate(r.bounds);
            }
            if (!any) return;
            var positions = new List<Vector3>();
            for (float x = bounds.min.x + 1f; x <= bounds.max.x - 0.5f; x += 2f)
                for (float z = bounds.min.z + 1f; z <= bounds.max.z - 0.5f; z += 2f)
                {
                    positions.Add(new Vector3(x, 0.6f, z));
                    positions.Add(new Vector3(x, 1.7f, z));
                }
            var go = new GameObject("Light probes");
            go.AddComponent<LightProbeGroup>().probePositions = positions.ToArray();
        }
    }
}
