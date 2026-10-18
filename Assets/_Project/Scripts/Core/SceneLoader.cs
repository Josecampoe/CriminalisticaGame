using UnityEngine;
using UnityEngine.SceneManagement;

namespace Caso404.Core
{
    /// <summary>Carga escenas de Unity por nombre, solo si están en Build Settings.</summary>
    public static class SceneLoader
    {
        /// <summary>True si la escena existe en Build Settings.</summary>
        public static bool CanLoad(string unitySceneName) =>
            !string.IsNullOrEmpty(unitySceneName) && Application.CanStreamedLevelBeLoaded(unitySceneName);

        /// <summary>Carga la escena de forma asíncrona. Devuelve false si no existe.</summary>
        public static bool TryLoad(string unitySceneName)
        {
            if (!CanLoad(unitySceneName)) return false;
            SceneManager.LoadSceneAsync(unitySceneName);
            return true;
        }

        /// <summary>Vuelve a cargar la escena actual.</summary>
        public static void ReloadCurrent() => SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex);
    }
}
