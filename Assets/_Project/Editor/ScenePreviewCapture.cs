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

        /// <summary>Fila con los 8 personajes (de pie, sentados y hablando) para revisar modelos, ropa y poses.</summary>
        [MenuItem("Caso404/Capturar personajes")]
        public static void CaptureCharacters()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.transform.localScale = new Vector3(3, 1, 2);
            var sun = new GameObject("Sol").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.2f;
            sun.transform.rotation = Quaternion.Euler(40, 160, 0);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.45f, 0.45f, 0.5f);
            string[] ids = { "laura", "carlos", "hernan", "andres", "sebastian", "valeria", "ines", "elena" };
            var idle = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/_Project/Art/Characters/Animations/chr_idle.anim");
            var talk = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/_Project/Art/Characters/Animations/chr_talk.anim");
            var sit = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/_Project/Art/Characters/Animations/chr_sit_idle.anim");
            for (int i = 0; i < ids.Length; i++)
            {
                var prefab = CharacterArtBuilder.LoadPrefab(ids[i]);
                if (prefab == null) continue;
                var a = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                a.transform.position = new Vector3(-3.5f + i, 0, 0);
                a.transform.rotation = Quaternion.Euler(0, 180, 0);
                idle.SampleAnimation(a, 0.5f);
                var b = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                b.transform.position = new Vector3(-3.5f + i, 0, 3f);
                b.transform.rotation = Quaternion.Euler(0, 180, 0);
                (i % 2 == 0 ? sit : talk).SampleAnimation(b, 0.8f);
            }
            Shot("chr_fila", new Vector3(0, 1.3f, -5.2f), new Vector3(5, 0, 0));
            Shot("chr_fila_atras", new Vector3(0, 1.5f, -2.2f), new Vector3(8, 0, 0));
            Shot("chr_caras_1", new Vector3(-2f, 1.6f, -1.6f), new Vector3(3, 0, 0));
            Shot("chr_caras_2", new Vector3(2f, 1.6f, -1.6f), new Vector3(3, 0, 0));
            Shot("chr_espalda", new Vector3(0, 1.4f, 6f), new Vector3(5, 180, 0));
        }

        /// <summary>En modo edición el Animator no corre: aplica a cada personaje su pose de reposo (de pie o sentado).</summary>
        static void PoseCharacters()
        {
            var idle = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/_Project/Art/Characters/Animations/chr_idle.anim");
            var sit = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/_Project/Art/Characters/Animations/chr_sit_idle.anim");
            if (idle == null || sit == null) return;
            foreach (var npc in Object.FindObjectsByType<Caso404.Characters.NpcAnimator>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                (npc.Seated ? sit : idle).SampleAnimation(npc.gameObject, 0.5f);
        }

        static void Shot(string name, Vector3 pos, Vector3 euler)
        {
            PoseCharacters();
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
