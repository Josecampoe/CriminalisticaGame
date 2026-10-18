using System.Diagnostics;

namespace Caso404.Core
{
    /// <summary>Logger que solo existe en el Editor y en builds de desarrollo.</summary>
    public static class GameLog
    {
        /// <summary>Mensaje informativo (se elimina en builds finales).</summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Info(string message) => UnityEngine.Debug.Log($"[Caso404] {message}");

        /// <summary>Advertencia (se elimina en builds finales).</summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Warn(string message) => UnityEngine.Debug.LogWarning($"[Caso404] {message}");

        /// <summary>Error: siempre se registra.</summary>
        public static void Error(string message, UnityEngine.Object context = null) => UnityEngine.Debug.LogError($"[Caso404] {message}", context);
    }
}
