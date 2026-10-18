using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Caso404.EditorTools
{
    /// <summary>
    /// Renderiza capturas de la escena c1_s1 desde varios puntos de vista (a la altura de los ojos) en Logs/preview_*.png.
    /// Sirve para revisar el greybox sin abrir el Editor. Uso: -executeMethod Caso404.EditorTools.ScenePreviewCapture.Capture
    /// </summary>
    public static class ScenePreviewCapture
    {
        [MenuItem("Caso404/Capturar vistas de c1_s1")]
        public static void Capture()
        {
            EditorSceneManager.OpenScene(Apartment404SceneBuilder.ScenePath);
            Shot("01_entrada", new Vector3(2.5f, 1.65f, 1.2f), new Vector3(10, -30, 0));
            Shot("02_sala_balcon", new Vector3(1.5f, 1.65f, 2.5f), new Vector3(8, -20, 0));
            Shot("03_escritorio", new Vector3(2.4f, 1.65f, 3.2f), new Vector3(20, 80, 0));
            Shot("04_balcon", new Vector3(0.3f, 1.65f, 7.3f), new Vector3(35, 200, 0));
            Shot("05_pasillo", new Vector3(3.5f, 1.65f, -1f), new Vector3(5, -100, 0));
            Shot("06_sofa", new Vector3(-1.2f, 1.65f, 2.0f), new Vector3(35, -100, 0));
            Debug.Log("[Caso404] Capturas guardadas en Logs/preview_*.png");
        }

        static void Shot(string name, Vector3 pos, Vector3 euler)
        {
            var go = new GameObject("PreviewCam");
            var cam = go.AddComponent<Camera>();
            cam.transform.SetPositionAndRotation(pos, Quaternion.Euler(euler));
            cam.fieldOfView = 65f;
            cam.nearClipPlane = 0.05f;
            var rt = new RenderTexture(960, 540, 24);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(960, 540, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 960, 540), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            File.WriteAllBytes($"Logs/preview_{name}.png", tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            cam.targetTexture = null;
            rt.Release();
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(go);
        }
    }
}
