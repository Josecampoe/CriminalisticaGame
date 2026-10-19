using Caso404.Save;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Caso404.EditorTools
{
    /// <summary>Atajos para probar el juego desde el Editor.</summary>
    public static class PlaytestMenu
    {
        /// <summary>Abre el menú principal y entra en Play (pide guardar la escena actual si tiene cambios).</summary>
        [MenuItem("Caso404/Jugar desde el menú %#p")]
        public static void PlayFromMenu()
        {
            if (EditorApplication.isPlaying) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(MainMenuSceneBuilder.ScenePath);
            EditorApplication.isPlaying = true;
        }

        /// <summary>Borra la partida guardada (save.json) para empezar de cero.</summary>
        [MenuItem("Caso404/Borrar partida guardada")]
        public static void DeleteSave()
        {
            var saves = new SaveService();
            if (!saves.HasSave) { EditorUtility.DisplayDialog("El Caso 404", "No hay partida guardada.", "OK"); return; }
            if (!EditorUtility.DisplayDialog("El Caso 404", $"¿Borrar la partida guardada?\n{saves.FilePath}", "Borrar", "Cancelar")) return;
            saves.Delete();
            Debug.Log("[Caso404] Partida guardada borrada.");
        }
    }
}
