using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Caso404.Environment
{
    /// <summary>
    /// Medición de rendimiento en el ejecutable. Solo se activa si el juego se abre con el argumento -benchmark:
    /// recorre las 7 escenas, gira la vista 360° durante 6 s en el punto de inicio y guarda el tiempo de frame promedio y
    /// el percentil 95 en benchmark.csv (junto al ejecutable). Respalda y restaura la partida guardada.
    /// Ejemplo: ElCaso404.exe -benchmark -screen-width 1920 -screen-height 1080 -screen-fullscreen 0
    /// </summary>
    public class FrameBenchmark : MonoBehaviour
    {
        const float WarmUp = 2f, Measure = 6f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-benchmark") < 0) return;
            var go = new GameObject("FrameBenchmark");
            DontDestroyOnLoad(go);
            go.AddComponent<FrameBenchmark>();
        }

        IEnumerator Start()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
            string save = Path.Combine(Application.persistentDataPath, "save.json");
            string backup = File.Exists(save) ? File.ReadAllText(save) : null;
            var csv = new StringBuilder($"escena;resolucion;ms_promedio;fps_promedio;ms_p95\n");
            var samples = new System.Collections.Generic.List<float>();
            for (int i = 1; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                yield return SceneManager.LoadSceneAsync(i);
                float t = 0f;
                while (t < WarmUp) { t += Time.unscaledDeltaTime; yield return null; }
                var cam = Camera.main;
                var player = cam != null ? cam.transform.root : null;
                samples.Clear();
                t = 0f;
                while (t < Measure)
                {
                    float dt = Time.unscaledDeltaTime;
                    t += dt;
                    samples.Add(dt * 1000f);
                    if (player != null) player.Rotate(0f, 360f / Measure * dt, 0f);
                    yield return null;
                }
                samples.Sort();
                float avg = 0f;
                foreach (var s in samples) avg += s;
                avg /= samples.Count;
                float p95 = samples[Mathf.Min(samples.Count - 1, (int)(samples.Count * 0.95f))];
                csv.Append($"{SceneManager.GetActiveScene().name};{Screen.width}x{Screen.height};{avg:F2};{1000f / avg:F0};{p95:F2}\n");
            }
            var outPath = Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? ".", "benchmark.csv");
            File.WriteAllText(outPath, csv.ToString());
            if (backup != null) File.WriteAllText(save, backup);
            else if (File.Exists(save)) File.Delete(save);
            Application.Quit();
        }
    }
}
