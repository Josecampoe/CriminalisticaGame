using Caso404.Decisions;
using UnityEditor;
using UnityEngine;
using static Caso404.EditorTools.SceneKit;

namespace Caso404.EditorTools
{
    /// <summary>
    /// Escena c2_s1 "Sala de servidores B, piso 12 (Torre Nexo)": pasillos de racks con luces azules, puerta de seguridad,
    /// panel rojo del sistema de gas, frío. Sin personajes: solo comentarios de ALEX. 5 evidencias.
    /// </summary>
    public static class ServidoresSceneBuilder
    {
        public const string ScenePath = ScenesFolder + "/ServidoresB.unity";

        [MenuItem("Caso404/Escenas/c2_s1 Sala de servidores B")]
        public static void Build()
        {
            var scene = Begin("c2", "c2_s1");

            Ambient(new Color(0.1f, 0.13f, 0.2f), new Color(0.05f, 0.08f, 0.14f), 8f, 30f); // frío
            for (int i = 0; i < 3; i++)
                PointLight($"Luz azul de racks {i}", new Vector3(-3f + i * 3f, 2.4f, 4f), new Color(0.35f, 0.55f, 1f), 1.2f, 5f);
            PointLight("Luz de emergencia (panel de gas)", new Vector3(4.4f, 2.2f, 1.2f), new Color(1f, 0.25f, 0.2f), 1.0f, 3f);
            PointLight("Luz del pasillo exterior", new Vector3(0f, 2.4f, -1.2f), new Color(0.85f, 0.9f, 1f), 1.0f, 5f);

            var env = Group("Environment");
            // Sala B: x ∈ [-5, 5], z ∈ [0, 8]; pasillo exterior al sur z ∈ [-2.5, 0]
            FloorAndCeiling(env, "Sala B", new Vector2(-5, 0), new Vector2(5, 8), "screen_off", "dark");
            FloorAndCeiling(env, "Pasillo", new Vector2(-5, -2.5f), new Vector2(5, 0), "floor_hall");
            Wall(env, "Sala Norte", new Vector3(-5, 0, 8), new Vector3(5, 0, 8), "dark");
            Wall(env, "Sala Oeste", new Vector3(-5, 0, -2.5f), new Vector3(-5, 0, 8), "dark");
            Wall(env, "Sala Este", new Vector3(5, 0, -2.5f), new Vector3(5, 0, 8), "dark");
            Wall(env, "Sala Sur (puerta de seguridad)", new Vector3(-5, 0, 0), new Vector3(5, 0, 0), "metal", new Opening(5f, 1.3f));
            Wall(env, "Pasillo Sur", new Vector3(-5, 0, -2.5f), new Vector3(5, 0, -2.5f), "wall_office");
            Box("Puerta de seguridad (abierta)", env, new Vector3(-0.65f, 1.05f, 0.65f), new Vector3(0.06f, 2.1f, 1.3f), "metal");
            // Racks con luces azules
            foreach (var x in new[] { -3.2f, 0f, 3.2f })
            {
                Box($"Fila de racks {x}", env, new Vector3(x, 1f, 4.8f), new Vector3(0.9f, 2f, 4.4f), "dark");
                for (int j = 0; j < 8; j++)
                    Box($"LED {x} {j}", env, new Vector3(x + 0.46f, 0.4f + (j % 4) * 0.42f, 3.0f + (j / 4) * 3.2f), new Vector3(0.01f, 0.02f, 0.25f), "screen", keepCollider: false);
            }
            // Escritorio de Tomás con su portátil ausente y la taza
            Box("Escritorio técnico", env, new Vector3(-3.8f, 0.37f, 1.4f), new Vector3(1.4f, 0.74f, 0.7f), "metal");
            // Cuerpo de Tomás cubierto (sin detalle)
            Box("Cuerpo cubierto (Tomás)", env, new Vector3(1.2f, 0.15f, 1.9f), new Vector3(0.6f, 0.3f, 1.8f), "paper", rotY: 75f);
            Box("Cinta policial", env, new Vector3(1.2f, 0.9f, 0.9f), new Vector3(2.6f, 0.04f, 0.02f), "yellow", keepCollider: false);

            var ev = Group("Evidence");
            // Registro de la puerta (vía ALEX): lector de la puerta de seguridad
            var log = Pickup(ev, "ev_c2_puerta_log", new Vector3(0.95f, 1.3f, -0.1f), new Vector3(0.3f, 0.35f, 0.25f));
            Box("Lector de tarjetas", log.transform, new Vector3(0.95f, 1.3f, -0.09f), new Vector3(0.14f, 0.2f, 0.04f), "dark", keepCollider: false);
            Box("Pantallita", log.transform, new Vector3(0.95f, 1.34f, -0.115f), new Vector3(0.1f, 0.06f, 0.01f), "screen", keepCollider: false);
            // Sensor de humo en el techo
            var sensor = Pickup(ev, "ev_c2_sensor_humo", new Vector3(-1.4f, 2.55f, 2.4f), new Vector3(0.4f, 0.3f, 0.4f));
            Cylinder("Sensor", sensor.transform, new Vector3(-1.4f, 2.65f, 2.4f), new Vector3(0.18f, 0.025f, 0.18f), "paper");
            // Panel del sistema de extinción (rojo)
            var panel = Pickup(ev, "ev_c2_panel_gas", new Vector3(4.85f, 1.4f, 1.2f), new Vector3(0.3f, 0.7f, 0.6f));
            Box("Panel rojo", panel.transform, new Vector3(4.9f, 1.4f, 1.2f), new Vector3(0.08f, 0.6f, 0.45f), "red", keepCollider: false);
            Box("Pantalla del panel", panel.transform, new Vector3(4.855f, 1.5f, 1.2f), new Vector3(0.01f, 0.12f, 0.25f), "screen", keepCollider: false);
            // USB en la mano de Tomás
            var usb = Pickup(ev, "ev_c2_usb_elena", new Vector3(0.55f, 0.08f, 2.4f), new Vector3(0.3f, 0.16f, 0.3f));
            Box("Mano", usb.transform, new Vector3(0.6f, 0.04f, 2.4f), new Vector3(0.08f, 0.04f, 0.1f), "skin", keepCollider: false);
            Box("USB", usb.transform, new Vector3(0.52f, 0.05f, 2.42f), new Vector3(0.05f, 0.012f, 0.02f), "metal", keepCollider: false);
            // Nota pegada en una taza (la fecha se ve al girarla)
            var nota = Pickup(ev, "ev_c2_nota", new Vector3(-3.5f, 0.85f, 1.35f), new Vector3(0.25f, 0.25f, 0.25f));
            Cylinder("Taza", nota.transform, new Vector3(-3.5f, 0.8f, 1.35f), new Vector3(0.08f, 0.05f, 0.08f), "paper");
            Box("Nota", nota.transform, new Vector3(-3.5f, 0.81f, 1.305f), new Vector3(0.06f, 0.05f, 0.003f), "yellow", keepCollider: false);
            Box("Fecha (detrás)", nota.transform, new Vector3(-3.5f, 0.81f, 1.395f), new Vector3(0.04f, 0.015f, 0.003f), "dark", keepCollider: false);

            SpawnPlayer(new Vector3(0f, 0.05f, -1.5f), 0f);
            Trigger<SceneCloseTrigger>("Ascensor del piso 12 (repasar el caso)", new Vector3(-4.9f, 1.1f, -1.25f), new Vector3(0.1f, 2.2f, 1.2f), "metal", "Repasar el caso y decidir");

            Finish(scene, ScenePath);
        }
    }
}
