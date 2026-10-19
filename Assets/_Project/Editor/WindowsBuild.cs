using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Caso404.EditorTools
{
    /// <summary>
    /// Genera el ejecutable de Windows en Builds/ElCaso404/ (carpeta ignorada por git).
    /// Uso: menú Caso404/Build de Windows, o -executeMethod Caso404.EditorTools.WindowsBuild.Build
    /// </summary>
    public static class WindowsBuild
    {
        public const string OutputPath = "Builds/ElCaso404/ElCaso404.exe";

        [MenuItem("Caso404/Build de Windows")]
        public static void Build()
        {
            SceneKit.RefreshBuildSettings();
            var options = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = OutputPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };
            var report = BuildPipeline.BuildPlayer(options);
            var s = report.summary;
            Debug.Log($"[Caso404] Build de Windows: {s.result}, {s.totalSize / (1024 * 1024)} MB, {s.totalErrors} errores → {OutputPath}");
            if (Application.isBatchMode && s.result != BuildResult.Succeeded) EditorApplication.Exit(1);
        }
    }
}
