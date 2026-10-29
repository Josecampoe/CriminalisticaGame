using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Caso404.Characters;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Caso404.EditorTools
{
    /// <summary>
    /// Auditoría visual: capturas de cada escena desde puntos de vista del jugador, primer plano de cada personaje
    /// y medición del tiempo de render a 1920×1080. Sirve para comparar "antes" y "después" de un cambio visual.
    /// Uso: -executeMethod Caso404.EditorTools.VisualAudit.RunFromCommandLine -auditFolder antes
    /// </summary>
    public static class VisualAudit
    {
        public readonly struct View
        {
            public readonly string Name;
            public readonly Vector3 Position, Euler;
            public View(string name, Vector3 position, Vector3 euler) { Name = name; Position = position; Euler = euler; }
        }

        /// <summary>Escena → vistas a la altura de los ojos (1.65 m). Son las mismas antes y después.</summary>
        public static readonly (string scene, View[] views)[] Plan =
        {
            (Apartment404SceneBuilder.ScenePath, new[]
            {
                new View("c1s1_pasillo", new Vector3(-12f, 1.65f, -1.5f), new Vector3(5, 90, 0)),
                new View("c1s1_sala", new Vector3(0.6f, 1.65f, 0.8f), new Vector3(10, -45, 0)),
                new View("c1s1_estudio", new Vector3(2.6f, 1.65f, 4.4f), new Vector3(12, 50, 0)),
                new View("c1s1_balcon", new Vector3(1.4f, 1.65f, 9f), new Vector3(20, -95, 0)),
                new View("c1s1_ciudad", new Vector3(-1f, 1.65f, 10.1f), new Vector3(8, 0, 0)),
            }),
            (CafeSceneBuilder.ScenePath, new[]
            {
                new View("c1s2_entrada", new Vector3(0f, 1.65f, 1f), new Vector3(10, 15, 0)),
                new View("c1s2_barra", new Vector3(-5f, 1.65f, 3.5f), new Vector3(12, 50, 0)),
                new View("c1s2_trastienda", new Vector3(3.2f, 1.65f, 9.8f), new Vector3(15, 70, 0)),
            }),
            (PorteriaSceneBuilder.ScenePath, new[]
            {
                new View("c1s3_porteria", new Vector3(-3.8f, 1.65f, 1f), new Vector3(10, 10, 0)),
                new View("c1s3_sotano", new Vector3(1f, 1.65f, 3f), new Vector3(8, 90, 0)),
                new View("c1s3_parqueadero", new Vector3(13f, 1.65f, 3f), new Vector3(10, 60, 0)),
            }),
            (ComisariaSceneBuilder.ScenePath, new[]
            {
                new View("c1s4_oficina", new Vector3(-1.5f, 1.65f, 0.9f), new Vector3(10, -20, 0)),
                new View("c1s4_ventana", new Vector3(-1.5f, 1.65f, 2.2f), new Vector3(3, 180, 0)),
                new View("c1s4_interrogatorio", new Vector3(5f, 1.65f, 2f), new Vector3(15, 60, 0)),
            }),
            (ServidoresSceneBuilder.ScenePath, new[]
            {
                new View("c2s1_pasillo", new Vector3(-4.5f, 1.65f, -1.2f), new Vector3(3, 150, 0)),
                new View("c2s1_sala", new Vector3(-3f, 1.65f, 0.8f), new Vector3(12, 40, 0)),
                new View("c2s1_racks", new Vector3(0f, 1.65f, 2f), new Vector3(8, 90, 0)),
            }),
            (PisoEjecutivoSceneBuilder.ScenePath, new[]
            {
                new View("c2s2_oficinas", new Vector3(3f, 1.65f, 1.2f), new Vector3(8, -20, 0)),
                new View("c2s2_ventanal", new Vector3(0f, 1.65f, 4.5f), new Vector3(2, 0, 0)),
                new View("c2s2_pasillo", new Vector3(-3f, 1.65f, 3f), new Vector3(8, 60, 0)),
            }),
            (CasetaSceneBuilder.ScenePath, new[]
            {
                new View("c2s3_caseta", new Vector3(2.5f, 1.65f, 1f), new Vector3(10, -90, 0)),
                new View("c2s3_parqueadero", new Vector3(1f, 1.65f, 6f), new Vector3(10, 120, 0)),
                new View("c2s3_calle", new Vector3(6f, 1.65f, 2f), new Vector3(5, 200, 0)),
            }),
        };

        const int ShotW = 1280, ShotH = 720;
        const int BenchW = 1920, BenchH = 1080, BenchFrames = 40;

        /// <summary>Entrada por línea de comandos: lee -auditFolder (antes/despues) y corre capturas y medición.</summary>
        public static void RunFromCommandLine()
        {
            var args = System.Environment.GetCommandLineArgs();
            string folder = "despues";
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == "-auditFolder") folder = args[i + 1];
            Run(folder);
        }

        [MenuItem("Caso404/Auditoría visual (capturas y tiempos)")]
        public static void RunDespues() => Run("despues");

        public static void Run(string folder)
        {
            var dir = Path.Combine("Docs", "capturas", folder);
            Directory.CreateDirectory(dir);
            var csv = new StringBuilder("escena;vista;ms_por_frame;fps_equivalentes\n");
            foreach (var (scene, views) in Plan)
            {
                if (!File.Exists(scene)) continue;
                EditorSceneManager.OpenScene(scene, OpenSceneMode.Single);
                PoseCharacters();
                foreach (var v in views)
                {
                    Shot(Path.Combine(dir, v.Name + ".png"), v.Position, Quaternion.Euler(v.Euler), ShotW, ShotH);
                    float ms = Benchmark(v.Position, Quaternion.Euler(v.Euler));
                    csv.Append($"{Path.GetFileNameWithoutExtension(scene)};{v.Name};{ms:F2};{1000f / ms:F0}\n");
                }
                CharacterCloseUps(dir, Path.GetFileNameWithoutExtension(scene));
            }
            File.WriteAllText(Path.Combine(dir, "tiempos_frame.csv"), csv.ToString());
            Debug.Log($"[Caso404] Auditoría visual guardada en {dir}\n{csv}");
        }

        /// <summary>Primer plano de cada personaje de la escena, con la luz de la escena (a 1 m de la cara).</summary>
        static void CharacterCloseUps(string dir, string sceneName)
        {
            var done = new HashSet<string>();
            foreach (var anim in Object.FindObjectsByType<Animator>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (!anim.isHuman) continue;
                var head = anim.GetBoneTransform(HumanBodyBones.Head);
                if (head == null) continue;
                var npcName = anim.transform.parent != null ? anim.transform.parent.name : anim.name;
                var id = npcName.Replace("NPC ", "").Replace(" ", "_").ToLowerInvariant();
                if (!done.Add(id)) continue;
                var fwd = anim.transform.forward;
                var pos = head.position + fwd * 1.0f + Vector3.down * 0.08f + anim.transform.right * 0.15f;
                var rot = Quaternion.LookRotation(head.position + Vector3.down * 0.15f - pos);
                Shot(Path.Combine(dir, $"personaje_{sceneName}_{id}.png"), pos, rot, 960, 720);
            }
        }

        /// <summary>En modo edición el Animator no corre: aplica la pose de reposo (de pie o sentado).</summary>
        static void PoseCharacters()
        {
            var idle = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/_Project/Art/Characters/Animations/chr_idle.anim");
            var sit = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/_Project/Art/Characters/Animations/chr_sit_idle.anim");
            if (idle == null || sit == null) return;
            foreach (var npc in Object.FindObjectsByType<NpcAnimator>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                (npc.Seated ? sit : idle).SampleAnimation(npc.gameObject, 0.5f);
        }

        /// <summary>Cámara igual a la del jugador (FOV 65, post-procesado según la configuración de la escena).</summary>
        static Camera MakeCamera(Vector3 pos, Quaternion rot)
        {
            var go = new GameObject("AuditCam") { hideFlags = HideFlags.DontSave };
            var cam = go.AddComponent<Camera>();
            cam.transform.SetPositionAndRotation(pos, rot);
            cam.fieldOfView = 65f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 1000f;
            var data = go.AddComponent<UniversalAdditionalCameraData>();
            var player = Object.FindAnyObjectByType<Caso404.Player.FirstPersonController>();
            var playerCam = player != null ? player.GetComponentInChildren<Camera>(true) : null;
            var playerData = playerCam != null ? playerCam.GetComponent<UniversalAdditionalCameraData>() : null;
            if (playerData != null)
            {
                data.renderPostProcessing = playerData.renderPostProcessing;
                data.antialiasing = playerData.antialiasing;
                data.antialiasingQuality = playerData.antialiasingQuality;
                cam.allowHDR = playerCam.allowHDR;
            }
            return cam;
        }

        static void Shot(string file, Vector3 pos, Quaternion rot, int w, int h)
        {
            var cam = MakeCamera(pos, rot);
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            File.WriteAllBytes(file, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            cam.targetTexture = null;
            rt.Release();
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(cam.gameObject);
        }

        /// <summary>
        /// Milisegundos por frame renderizando a 1920×1080 (CPU de render + GPU: al final se lee un píxel para esperar a la GPU).
        /// Es una aproximación del costo de dibujar la escena; no incluye la lógica del juego, que es mínima.
        /// </summary>
        static float Benchmark(Vector3 pos, Quaternion rot)
        {
            var cam = MakeCamera(pos, rot);
            var rt = new RenderTexture(BenchW, BenchH, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            var probe = new Texture2D(1, 1, TextureFormat.RGB24, false);
            void Sync() { RenderTexture.active = rt; probe.ReadPixels(new Rect(0, 0, 1, 1), 0, 0); RenderTexture.active = null; }
            for (int i = 0; i < 5; i++) cam.Render();
            Sync();
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < BenchFrames; i++) cam.Render();
            Sync();
            sw.Stop();
            cam.targetTexture = null;
            rt.Release();
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(probe);
            Object.DestroyImmediate(cam.gameObject);
            return (float)sw.Elapsed.TotalMilliseconds / BenchFrames;
        }
    }
}
