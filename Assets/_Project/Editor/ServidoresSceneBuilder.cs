using Caso404.Decisions;
using UnityEditor;
using UnityEngine;
using static Caso404.EditorTools.SceneKit;

namespace Caso404.EditorTools
{
    /// <summary>
    /// Escena c2_s1 "Sala de servidores B, piso 12 (Torre Nexo)": pasillos de racks con luces azules, puerta de seguridad,
    /// panel rojo del sistema de gas, frío. Pasillo exterior con ventanal hacia la ciudad de noche.
    /// Sin personajes: solo comentarios de ALEX. 5 evidencias repartidas.
    /// </summary>
    public static class ServidoresSceneBuilder
    {
        public const string ScenePath = ScenesFolder + "/ServidoresB.unity";

        // Sala B: x ∈ [-7, 7], z ∈ [0, 12] · Pasillo exterior: x ∈ [-7, 7], z ∈ [-3, 0]

        [MenuItem("Caso404/Escenas/c2_s1 Sala de servidores B")]
        public static void Build()
        {
            var scene = Begin("c2", "c2_s1");

            Ambient(new Color(0.1f, 0.13f, 0.2f), new Color(0.05f, 0.08f, 0.14f)); // frío
            // Cuadrícula de luces frías entre las filas de racks
            foreach (var x in new[] { -3f, 0f, 3f })
                foreach (var z in new[] { 3.2f, 6.8f, 10.2f })
                    PointLight($"Luz azul de racks {x} {z}", new Vector3(x, 2.5f, z), new Color(0.55f, 0.72f, 1f), 1.8f, 6f);
            PointLight("Luz de emergencia (panel de gas)", new Vector3(6.4f, 2.2f, 1.6f), new Color(1f, 0.25f, 0.2f), 1.1f, 3.5f);
            PointLight("Luz del puesto técnico", new Vector3(-5.2f, 2.4f, 1.8f), new Color(0.9f, 0.92f, 1f), 0.9f, 4f);
            PointLight("Luz del pasillo exterior 1", new Vector3(-3.5f, 2.5f, -1.5f), new Color(0.85f, 0.9f, 1f), 0.9f, 5f);
            PointLight("Luz del pasillo exterior 2", new Vector3(3.5f, 2.5f, -1.5f), new Color(0.85f, 0.9f, 1f), 0.9f, 5f);

            var env = Group("Environment");
            FloorAndCeiling(env, "Sala B", new Vector2(-7, 0), new Vector2(7, 12), "floor_hall", "dark"); // piso técnico
            FloorAndCeiling(env, "Pasillo", new Vector2(-7, -3), new Vector2(7, 0), "floor_hall");
            Wall(env, "Sala Norte", new Vector3(-7, 0, 12), new Vector3(7, 0, 12), "dark");
            Wall(env, "Sala Oeste", new Vector3(-7, 0, -3), new Vector3(-7, 0, 12), "dark");
            Wall(env, "Sala Este", new Vector3(7, 0, -3), new Vector3(7, 0, 12), "dark");
            Wall(env, "Sala Sur (puerta de seguridad)", new Vector3(-7, 0, 0), new Vector3(7, 0, 0), "metal", new Opening(7f, 1.3f));
            // Ventanal del pasillo hacia la ciudad de noche
            Wall(env, "Pasillo Sur (ventanal)", new Vector3(-7, 0, -3), new Vector3(7, 0, -3), "wall_office", new Opening(7f, 12f, 1.8f, 0.6f));
            Box("Vidrio del ventanal", env, new Vector3(0f, 1.5f, -3f), new Vector3(12f, 1.8f, 0.03f), "glass");
            Box("Puerta de seguridad (abierta)", env, new Vector3(-0.65f, 1.05f, 0.65f), new Vector3(0.06f, 2.1f, 1.3f), "metal");
            Box("Letrero SALA B", env, new Vector3(1.2f, 2.3f, -0.1f), new Vector3(0.6f, 0.2f, 0.02f), "paper", keepCollider: false);

            // Cuatro filas de racks con luces azules
            foreach (var x in new[] { -4.5f, -1.5f, 1.5f, 4.5f })
            {
                Box($"Fila de racks {x}", env, new Vector3(x, 1f, 7.5f), new Vector3(0.9f, 2f, 6f), "dark");
                for (int j = 0; j < 12; j++)
                    Box($"LED {x} {j}", env, new Vector3(x + 0.46f, 0.4f + (j % 4) * 0.42f, 5f + (j / 4) * 2.5f), new Vector3(0.01f, 0.02f, 0.25f), "screen", keepCollider: false);
            }
            Box("Bandeja de cables", env, new Vector3(0f, 2.45f, 7.5f), new Vector3(10f, 0.08f, 0.4f), "metal", keepCollider: false);
            Box("Aire acondicionado", env, new Vector3(-6.6f, 1f, 10.5f), new Vector3(0.7f, 2f, 1.4f), "metal");
            // Puesto técnico de Tomás (esquina suroeste)
            Box("Escritorio técnico", env, new Vector3(-5.6f, 0.37f, 1.6f), new Vector3(1.4f, 0.74f, 0.7f), "metal");
            Box("Monitor técnico", env, new Vector3(-5.8f, 1.0f, 1.85f), new Vector3(0.6f, 0.38f, 0.04f), "screen_off", keepCollider: false);
            Box("Silla técnica", env, new Vector3(-5.6f, 0.25f, 0.9f), new Vector3(0.45f, 0.5f, 0.45f), "dark");
            // Cuerpo de Tomás cubierto (sin detalle), entre la puerta y los racks
            Box("Cuerpo cubierto (Tomás)", env, new Vector3(2.4f, 0.15f, 2.6f), new Vector3(0.6f, 0.3f, 1.8f), "paper", rotY: 75f);
            Box("Cinta policial", env, new Vector3(2.4f, 0.9f, 1.5f), new Vector3(2.8f, 0.04f, 0.02f), "yellow", keepCollider: false);

            var ev = Group("Evidence");
            // 1. Puerta: registro del lector de tarjetas (vía ALEX)
            var log = Pickup(ev, "ev_c2_puerta_log", new Vector3(0.95f, 1.3f, -0.1f), new Vector3(0.3f, 0.35f, 0.25f));
            Box("Lector de tarjetas", log.transform, new Vector3(0.95f, 1.3f, -0.09f), new Vector3(0.14f, 0.2f, 0.04f), "dark", keepCollider: false);
            Box("Pantallita", log.transform, new Vector3(0.95f, 1.34f, -0.115f), new Vector3(0.1f, 0.06f, 0.01f), "screen", keepCollider: false);
            // 2. Fondo de la sala: sensor de humo en el techo, entre los racks
            var sensor = Pickup(ev, "ev_c2_sensor_humo", new Vector3(0f, 2.55f, 9.8f), new Vector3(0.4f, 0.3f, 0.4f));
            Cylinder("Sensor", sensor.transform, new Vector3(0f, 2.65f, 9.8f), new Vector3(0.18f, 0.025f, 0.18f), "paper");
            // 3. Pared este: panel del sistema de extinción (rojo)
            var panel = Pickup(ev, "ev_c2_panel_gas", new Vector3(6.85f, 1.4f, 1.6f), new Vector3(0.3f, 0.7f, 0.6f));
            Box("Panel rojo", panel.transform, new Vector3(6.9f, 1.4f, 1.6f), new Vector3(0.08f, 0.6f, 0.45f), "red", keepCollider: false);
            Box("Pantalla del panel", panel.transform, new Vector3(6.855f, 1.5f, 1.6f), new Vector3(0.01f, 0.12f, 0.25f), "screen", keepCollider: false);
            // 4. Junto al cuerpo: USB en la mano de Tomás
            var usb = Pickup(ev, "ev_c2_usb_elena", new Vector3(1.75f, 0.08f, 3.1f), new Vector3(0.3f, 0.16f, 0.3f));
            Box("Mano", usb.transform, new Vector3(1.8f, 0.04f, 3.1f), new Vector3(0.08f, 0.04f, 0.1f), "skin", keepCollider: false);
            Box("USB", usb.transform, new Vector3(1.72f, 0.05f, 3.12f), new Vector3(0.05f, 0.012f, 0.02f), "metal", keepCollider: false);
            // 5. Puesto técnico: nota pegada en una taza (la fecha se ve al girarla)
            var nota = Pickup(ev, "ev_c2_nota", new Vector3(-5.2f, 0.85f, 1.45f), new Vector3(0.25f, 0.25f, 0.25f));
            Cylinder("Taza", nota.transform, new Vector3(-5.2f, 0.8f, 1.45f), new Vector3(0.08f, 0.05f, 0.08f), "paper");
            Box("Nota", nota.transform, new Vector3(-5.2f, 0.81f, 1.405f), new Vector3(0.06f, 0.05f, 0.003f), "yellow", keepCollider: false);
            Box("Fecha (detrás)", nota.transform, new Vector3(-5.2f, 0.81f, 1.495f), new Vector3(0.04f, 0.015f, 0.003f), "dark", keepCollider: false);

            SpawnPlayer(new Vector3(-4.5f, 0.05f, -1.5f), 90f);
            Trigger<SceneCloseTrigger>("Ascensor del piso 12 (repasar el caso)", new Vector3(-6.9f, 1.1f, -1.5f), new Vector3(0.1f, 2.2f, 1.2f), "metal", "Repasar el caso y decidir");

            CityKit.Surround(new CityKit.Options
            {
                StreetY = -36f, Night = true, Seed = 2012, Downtown = true, // piso 12 de la Torre Nexo
                HostMin = new Vector2(-7.3f, -3.3f), HostMax = new Vector2(7.3f, 12.3f), GridCenter = new Vector2(0f, 11f),
                HostHeight = 66f, PlayFloorY = 0f, PlayCeilingY = WallHeight, HostMaterial = "fachada_torre"
            });
            Finish(scene, ScenePath);
        }
    }
}
