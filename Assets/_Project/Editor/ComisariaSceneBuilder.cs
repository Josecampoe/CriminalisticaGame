using Caso404.AI;
using Caso404.Decisions;
using UnityEditor;
using UnityEngine;
using static Caso404.EditorTools.SceneKit;

namespace Caso404.EditorTools
{
    /// <summary>
    /// Escena c1_s4 "Comisaría" (compartida entre casos): oficina con tablero de corcho, pantalla de ALEX y el expediente
    /// para la Fiscalía (acusación), y sala de interrogatorio con todos los personajes del caso 1.
    /// </summary>
    public static class ComisariaSceneBuilder
    {
        public const string ScenePath = ScenesFolder + "/Comisaria.unity";

        [MenuItem("Caso404/Escenas/c1_s4 Comisaría")]
        public static void Build()
        {
            var scene = Begin("c1", "c1_s4");

            Ambient(new Color(0.26f, 0.27f, 0.3f), new Color(0.2f, 0.21f, 0.23f), 12f, 40f);
            Sun("Luz de ventana", new Color(0.75f, 0.8f, 0.9f), 0.4f, new Vector3(35f, 210f, 0f));
            PointLight("Lámpara de la oficina", new Vector3(-1f, 2.5f, 4f), new Color(1f, 0.88f, 0.7f), 1.4f, 8f);
            PointLight("Luz del tablero", new Vector3(-4.5f, 2.3f, 4f), new Color(1f, 0.8f, 0.55f), 1.0f, 4f);
            PointLight("Lámpara de interrogatorio", new Vector3(8f, 2.4f, 4f), new Color(1f, 0.92f, 0.8f), 1.8f, 6f);
            PointLight("Brillo de ALEX", new Vector3(0f, 1.8f, 7.4f), new Color(0.31f, 0.82f, 0.77f), 0.9f, 3.5f);

            var env = Group("Environment");
            // Oficina: x ∈ [-6, 4], z ∈ [0, 8]
            FloorAndCeiling(env, "Oficina", new Vector2(-6, 0), new Vector2(4, 8), "floor_hall");
            Wall(env, "Oficina Sur", new Vector3(-6, 0, 0), new Vector3(4, 0, 0), "wall_office", new Opening(4f, 2.4f, 1.2f, 0.9f));
            Wall(env, "Oficina Norte", new Vector3(-6, 0, 8), new Vector3(4, 0, 8), "wall_office");
            Wall(env, "Oficina Oeste", new Vector3(-6, 0, 0), new Vector3(-6, 0, 8), "wall_office");
            Wall(env, "Oficina/Interrogatorio", new Vector3(4, 0, 0), new Vector3(4, 0, 8), "wall_office", new Opening(2f, 1f), new Opening(5.5f, 2f, 1f, 1f));
            Box("Vidrio espía", env, new Vector3(4f, 1.5f, 5.5f), new Vector3(0.02f, 1f, 2f), "screen_off", keepCollider: true);
            Desk(env, new Vector3(-2.5f, 0, 2.2f));
            Desk(env, new Vector3(0.8f, 0, 2.2f));
            Box("Archivador", env, new Vector3(-5.7f, 0.65f, 0.8f), new Vector3(0.5f, 1.3f, 0.6f), "metal");
            Box("Dispensador de agua", env, new Vector3(3.5f, 0.6f, 0.6f), new Vector3(0.35f, 1.2f, 0.35f), "glass");

            // Interrogatorio: x ∈ [4, 12], z ∈ [0, 8]
            FloorAndCeiling(env, "Interrogatorio", new Vector2(4, 0), new Vector2(12, 8), "concrete");
            Wall(env, "Inter Sur", new Vector3(4, 0, 0), new Vector3(12, 0, 0), "wall_hall");
            Wall(env, "Inter Norte", new Vector3(4, 0, 8), new Vector3(12, 0, 8), "wall_hall");
            Wall(env, "Inter Este", new Vector3(12, 0, 0), new Vector3(12, 0, 8), "wall_hall");
            Box("Mesa de interrogatorio", env, new Vector3(8f, 0.38f, 4f), new Vector3(1.2f, 0.76f, 2.4f), "metal");
            for (int i = 0; i < 4; i++) Box($"Silla {i}", env, new Vector3(i < 2 ? 7f : 9f, 0.22f, 3.2f + (i % 2) * 1.6f), new Vector3(0.45f, 0.44f, 0.45f), "dark");

            // Tablero de hipótesis (corcho con fotos e hilo rojo)
            var board = Trigger<SceneCloseTrigger>("Tablero de corcho (hipótesis)", new Vector3(-5.9f, 1.6f, 4f), new Vector3(0.08f, 1.4f, 2.6f), "cork", "Revisar el tablero de hipótesis");
            for (int i = 0; i < 6; i++)
                Box($"Foto {i}", board.transform, new Vector3(-5.85f, 1.25f + (i % 2) * 0.55f, 3.0f + (i / 2) * 0.9f), new Vector3(0.01f, 0.3f, 0.25f), "paper", keepCollider: false);
            Box("Hilo rojo 1", board.transform, new Vector3(-5.84f, 1.6f, 3.9f), new Vector3(0.005f, 0.01f, 1.8f), "red", rotX: 20f, keepCollider: false);
            Box("Hilo rojo 2", board.transform, new Vector3(-5.84f, 1.5f, 4.4f), new Vector3(0.005f, 0.01f, 1.2f), "red", rotX: -25f, keepCollider: false);

            // Pantalla de ALEX
            var alex = Trigger<AlexScreenTrigger>("Pantalla de ALEX", new Vector3(0f, 1.7f, 7.92f), new Vector3(1.4f, 0.8f, 0.05f), "screen", "Hablar con ALEX");
            Box("Marco", alex.transform, new Vector3(0f, 1.7f, 7.95f), new Vector3(1.5f, 0.9f, 0.03f), "dark", keepCollider: false);

            // Expediente para la Fiscalía (acusación)
            Trigger<AccusationTrigger>("Expediente para la Fiscalía", new Vector3(0.8f, 0.8f, 2.2f), new Vector3(0.32f, 0.06f, 0.42f), "red", "Presentar la acusación");

            SpawnPlayer(new Vector3(-1.5f, 0.05f, 0.9f), 20f);
            Npc("carlos", new Vector3(7f, 0f, 3.2f), 90f, seated: true);
            Npc("laura", new Vector3(7f, 0f, 4.8f), 90f, seated: true);
            Npc("hernan", new Vector3(9f, 0f, 3.2f), 270f, seated: true);
            Npc("andres", new Vector3(9f, 0f, 4.8f), 270f, seated: true);

            Finish(scene, ScenePath);
        }

        static void Desk(Transform parent, Vector3 floorPos)
        {
            Box("Escritorio", parent, floorPos + new Vector3(0, 0.38f, 0), new Vector3(1.4f, 0.76f, 0.7f), "wood");
            Box("Monitor", parent, floorPos + new Vector3(-0.3f, 0.98f, 0.2f), new Vector3(0.5f, 0.32f, 0.04f), "screen_off", keepCollider: false);
            Box("Papeles", parent, floorPos + new Vector3(0.35f, 0.765f, -0.05f), new Vector3(0.25f, 0.01f, 0.32f), "paper", rotY: 10f, keepCollider: false);
        }
    }
}
