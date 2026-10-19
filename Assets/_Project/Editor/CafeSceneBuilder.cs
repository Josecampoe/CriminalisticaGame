using Caso404.Decisions;
using UnityEditor;
using UnityEngine;
using static Caso404.EditorTools.SceneKit;

namespace Caso404.EditorTools
{
    /// <summary>
    /// Escena c1_s2 "Café La Esquina": café pequeño de barrio, de noche, televisor con un partido repetido.
    /// 3 evidencias y Carlos (que no aparece si fue detenido en dec_c1_s1).
    /// </summary>
    public static class CafeSceneBuilder
    {
        public const string ScenePath = ScenesFolder + "/CafeLaEsquina.unity";

        [MenuItem("Caso404/Escenas/c1_s2 Café La Esquina")]
        public static void Build()
        {
            var scene = Begin("c1", "c1_s2");

            Ambient(new Color(0.12f, 0.12f, 0.17f), new Color(0.08f, 0.08f, 0.12f), 10f, 30f); // noche
            Sun("Luz de calle (azul)", new Color(0.35f, 0.42f, 0.6f), 0.25f, new Vector3(35f, 160f, 0f));
            PointLight("Lámpara sobre la barra", new Vector3(-1.5f, 2.4f, 5.4f), new Color(1f, 0.75f, 0.45f), 1.6f, 6f);
            PointLight("Lámpara mesa 1", new Vector3(2.5f, 2.3f, 2.5f), new Color(1f, 0.78f, 0.5f), 1.2f, 4.5f);
            PointLight("Lámpara mesa 2", new Vector3(-2.5f, 2.3f, 2.2f), new Color(1f, 0.78f, 0.5f), 1.0f, 4.5f);
            PointLight("Brillo del televisor", new Vector3(4.2f, 2f, 4f), new Color(0.5f, 0.8f, 1f), 0.8f, 3.5f);

            var env = Group("Environment");
            // Salón: x ∈ [-5, 5], z ∈ [0, 7]
            FloorAndCeiling(env, "Salón", new Vector2(-5, 0), new Vector2(5, 7), "floor_tile");
            Wall(env, "Pared Sur", new Vector3(-5, 0, 0), new Vector3(5, 0, 0), "wall_cafe", new Opening(5f, 1.2f), new Opening(2.2f, 2.4f, 1.3f, 0.8f), new Opening(7.8f, 2.4f, 1.3f, 0.8f));
            Wall(env, "Pared Norte", new Vector3(-5, 0, 7), new Vector3(5, 0, 7), "wall_cafe");
            Wall(env, "Pared Oeste", new Vector3(-5, 0, 0), new Vector3(-5, 0, 7), "wall_cafe");
            Wall(env, "Pared Este", new Vector3(5, 0, 0), new Vector3(5, 0, 7), "wall_cafe");
            Box("Calle (afuera)", env, new Vector3(0, -0.06f, -3f), new Vector3(14, 0.1f, 6), "concrete");
            Box("Vidrio vitrina izq", env, new Vector3(-2.8f, 1.45f, 0f), new Vector3(2.4f, 1.3f, 0.02f), "screen_off", keepCollider: false);
            Box("Vidrio vitrina der", env, new Vector3(2.8f, 1.45f, 0f), new Vector3(2.4f, 1.3f, 0.02f), "screen_off", keepCollider: false);

            // Barra y cafetera
            Box("Barra", env, new Vector3(-1.5f, 0.52f, 5.6f), new Vector3(5f, 1.05f, 0.7f), "wood");
            Box("Cubierta de la barra", env, new Vector3(-1.5f, 1.07f, 5.6f), new Vector3(5.1f, 0.04f, 0.8f), "dark", keepCollider: false);
            Box("Cafetera", env, new Vector3(-3.4f, 1.3f, 5.7f), new Vector3(0.6f, 0.45f, 0.4f), "metal");
            Box("Repisa", env, new Vector3(-1.5f, 1.8f, 6.85f), new Vector3(4.5f, 0.05f, 0.25f), "wood", keepCollider: false);
            for (int i = 0; i < 8; i++) Cylinder($"Botella {i}", env, new Vector3(-3.4f + i * 0.5f, 1.97f, 6.85f), new Vector3(0.07f, 0.15f, 0.07f), i % 2 == 0 ? "glass" : "dark");

            // Televisor con el partido repetido
            Box("Televisor", env, new Vector3(4.9f, 2.0f, 4f), new Vector3(0.08f, 0.7f, 1.2f), "dark");
            Box("Pantalla (partido)", env, new Vector3(4.85f, 2.0f, 4f), new Vector3(0.01f, 0.6f, 1.1f), "screen", keepCollider: false);

            // Mesas
            Table(env, new Vector3(2.5f, 0, 2.5f));
            Table(env, new Vector3(-2.5f, 0, 2.2f));
            Table(env, new Vector3(0f, 0, 3.6f));
            Table(env, new Vector3(3.2f, 0, 5.2f));

            var ev = Group("Evidence");
            // 1. Recibo del datáfono, junto al datáfono sobre la barra
            var recibo = Pickup(ev, "ev_c1_recibo_cafe", new Vector3(0.2f, 1.12f, 5.45f), new Vector3(0.4f, 0.15f, 0.35f));
            Box("Datáfono", recibo.transform, new Vector3(0.35f, 1.13f, 5.55f), new Vector3(0.09f, 0.05f, 0.17f), "dark", keepCollider: false);
            Box("Recibo", recibo.transform, new Vector3(0.1f, 1.093f, 5.4f), new Vector3(0.07f, 0.004f, 0.16f), "paper", rotY: 20f, keepCollider: false);
            // 2. Video del café: monitor de seguridad al final de la barra
            var video = Pickup(ev, "ev_c1_camara_cafe", new Vector3(0.85f, 1.3f, 5.7f), new Vector3(0.5f, 0.45f, 0.4f));
            Box("Monitor", video.transform, new Vector3(0.85f, 1.3f, 5.75f), new Vector3(0.42f, 0.3f, 0.06f), "dark", rotY: 180f, keepCollider: false);
            Box("Imagen de la cámara", video.transform, new Vector3(0.85f, 1.3f, 5.715f), new Vector3(0.38f, 0.26f, 0.01f), "screen", rotY: 180f, keepCollider: false);
            Box("Cámara del techo", video.transform, new Vector3(4.6f, 2.55f, 0.4f), new Vector3(0.1f, 0.08f, 0.18f), "metal", rotY: -140f, keepCollider: false);
            // 3. Servilleta con nota, en la mesa de Carlos
            var servilleta = Pickup(ev, "ev_c1_servilleta", new Vector3(2.3f, 0.8f, 2.35f), new Vector3(0.3f, 0.12f, 0.3f));
            Box("Servilleta", servilleta.transform, new Vector3(2.3f, 0.765f, 2.35f), new Vector3(0.14f, 0.003f, 0.14f), "paper", rotY: 30f, keepCollider: false);
            Cylinder("Taza de tinto", servilleta.transform, new Vector3(2.65f, 0.8f, 2.6f), new Vector3(0.08f, 0.045f, 0.08f), "paper");

            SpawnPlayer(new Vector3(0f, 0.05f, 0.9f), 0f);
            Npc("carlos", new Vector3(2.55f, 0f, 3.2f), 180f, seated: true);
            Trigger<SceneCloseTrigger>("Puerta del café (repasar el caso)", new Vector3(0f, 1.05f, -0.05f), new Vector3(1.1f, 2.1f, 0.08f), "glass", "Repasar el caso (hipótesis)");

            Finish(scene, ScenePath);
        }

        static void Table(Transform parent, Vector3 floorPos)
        {
            Cylinder("Mesa", parent, floorPos + new Vector3(0, 0.74f, 0), new Vector3(0.8f, 0.02f, 0.8f), "wood", keepCollider: true);
            Cylinder("Pata", parent, floorPos + new Vector3(0, 0.37f, 0), new Vector3(0.08f, 0.37f, 0.08f), "dark", keepCollider: true);
        }
    }
}
