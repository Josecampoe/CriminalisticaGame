using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Caso404.EditorTools
{
    /// <summary>
    /// Renderiza capturas de las escenas desde varios puntos de vista (a la altura de los ojos) en Logs/preview_*.png.
    /// Sirve para revisar el greybox sin abrir el Editor. Uso: -executeMethod Caso404.EditorTools.ScenePreviewCapture.CaptureAll
    /// </summary>
    public static class ScenePreviewCapture
    {
        [MenuItem("Caso404/Capturar vistas de las escenas")]
        public static void CaptureAll()
        {
            Capture();
            EditorSceneManager.OpenScene(CafeSceneBuilder.ScenePath);
            Shot("c1s2_entrada", new Vector3(0f, 1.65f, 0.9f), new Vector3(12, 10, 0));
            Shot("c1s2_barra", new Vector3(1.5f, 1.65f, 3.5f), new Vector3(20, -40, 0));
            EditorSceneManager.OpenScene(PorteriaSceneBuilder.ScenePath);
            Shot("c1s3_porteria", new Vector3(-3.8f, 1.65f, 1f), new Vector3(10, 10, 0));
            Shot("c1s3_sotano", new Vector3(1f, 1.65f, 3f), new Vector3(8, 90, 0));
            Shot("c1s3_mantenimiento", new Vector3(6.5f, 1.65f, 4.6f), new Vector3(10, 0, 0));
            Shot("c1s3_parqueadero", new Vector3(13f, 1.65f, 3f), new Vector3(10, 60, 0));
            EditorSceneManager.OpenScene(ComisariaSceneBuilder.ScenePath);
            Shot("c1s4_oficina", new Vector3(-1.5f, 1.65f, 0.9f), new Vector3(10, -20, 0));
            Shot("c1s4_interrogatorio", new Vector3(5f, 1.65f, 2f), new Vector3(15, 60, 0));
            EditorSceneManager.OpenScene(ServidoresSceneBuilder.ScenePath);
            Shot("c2s1_entrada", new Vector3(0f, 1.65f, -1.5f), new Vector3(10, 0, 0));
            Shot("c2s1_sala", new Vector3(-1.5f, 1.65f, 0.8f), new Vector3(25, 50, 0));
            Debug.Log("[Caso404] Capturas guardadas en Logs/preview_*.png");
        }

        [MenuItem("Caso404/Capturar vistas de c1_s1")]
        public static void Capture()
        {
            EditorSceneManager.OpenScene(Apartment404SceneBuilder.ScenePath);
            Shot("01_entrada", new Vector3(2.5f, 1.65f, 1.2f), new Vector3(10, -30, 0));
            Shot("04_balcon", new Vector3(0.3f, 1.65f, 7.3f), new Vector3(35, 200, 0));
            Shot("05_pasillo", new Vector3(3.5f, 1.65f, -1f), new Vector3(5, -100, 0));
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
