using Caso404.Decisions;
using UnityEditor;
using UnityEngine;
using static Caso404.EditorTools.SceneKit;

namespace Caso404.EditorTools
{
    /// <summary>
    /// Escena c1_s2 "Café La Esquina": café de barrio, de noche, televisor con un partido repetido.
    /// Salón con barra y mesas, trastienda con el monitor de seguridad y baño. 3 evidencias repartidas;
    /// Carlos en su mesa (no aparece si fue detenido en dec_c1_s1).
    /// </summary>
    public static class CafeSceneBuilder
    {
        public const string ScenePath = ScenesFolder + "/CafeLaEsquina.unity";

        // Salón: x ∈ [-7, 7], z ∈ [0, 9] · Trastienda: x ∈ [2, 7], z ∈ [9, 12.5] · Baño: x ∈ [-7, -4], z ∈ [9, 12.5]

        [MenuItem("Caso404/Escenas/c1_s2 Café La Esquina")]
        public static void Build()
        {
            var scene = Begin("c1", "c1_s2");

            Ambient(new Color(0.13f, 0.13f, 0.18f), new Color(0.06f, 0.07f, 0.11f)); // noche
            Sun("Luz de la calle (azul)", new Color(0.35f, 0.42f, 0.6f), 0.2f, new Vector3(35f, 160f, 0f));
            PointLight("Lámpara sobre la barra 1", new Vector3(-3f, 2.4f, 7.4f), new Color(1f, 0.75f, 0.45f), 1.4f, 6f);
            PointLight("Lámpara sobre la barra 2", new Vector3(1.5f, 2.4f, 7.4f), new Color(1f, 0.75f, 0.45f), 1.4f, 6f);
            PointLight("Lámpara mesas oeste", new Vector3(-4f, 2.3f, 3f), new Color(1f, 0.78f, 0.5f), 1.1f, 5.5f);
            PointLight("Lámpara mesas este", new Vector3(4f, 2.3f, 3f), new Color(1f, 0.78f, 0.5f), 1.2f, 5.5f);
            PointLight("Brillo del televisor", new Vector3(6.4f, 2f, 5f), new Color(0.5f, 0.8f, 1f), 0.8f, 3.5f);
            PointLight("Bombillo de la trastienda", new Vector3(4.5f, 2.4f, 10.8f), new Color(1f, 0.85f, 0.6f), 1.0f, 4.5f);
            PointLight("Luz del baño", new Vector3(-5.5f, 2.4f, 10.8f), new Color(0.9f, 0.95f, 1f), 0.7f, 3.5f);

            var env = Group("Environment");
            FloorAndCeiling(env, "Salón", new Vector2(-7, 0), new Vector2(7, 9), "floor_tile");
            FloorAndCeiling(env, "Trastienda", new Vector2(2, 9), new Vector2(7, 12.5f), "concrete");
            FloorAndCeiling(env, "Baño", new Vector2(-7, 9), new Vector2(-4, 12.5f), "floor_tile");
            // Fachada al sur con puerta y dos vitrinas hacia la calle
            Wall(env, "Pared Sur (vitrinas)", new Vector3(-7, 0, 0), new Vector3(7, 0, 0), "wall_cafe",
                new Opening(7f, 1.2f), new Opening(3.4f, 4f, 1.4f, 0.75f), new Opening(10.6f, 4f, 1.4f, 0.75f));
            Box("Vitrina izquierda", env, new Vector3(-3.6f, 1.45f, 0f), new Vector3(4f, 1.4f, 0.03f), "glass");
            Box("Vitrina derecha", env, new Vector3(3.6f, 1.45f, 0f), new Vector3(4f, 1.4f, 0.03f), "glass");
            Box("Letrero CAFÉ LA ESQUINA", env, new Vector3(0f, 2.45f, -0.12f), new Vector3(3f, 0.35f, 0.06f), "yellow", keepCollider: false);
            Wall(env, "Pared Oeste", new Vector3(-7, 0, 0), new Vector3(-7, 0, 12.5f), "wall_cafe");
            Wall(env, "Pared Este", new Vector3(7, 0, 0), new Vector3(7, 0, 12.5f), "wall_cafe");
            Wall(env, "Pared Norte (salón)", new Vector3(-7, 0, 9), new Vector3(7, 0, 9), "wall_cafe", new Opening(1.5f, 0.9f), new Opening(12f, 1f));
            Wall(env, "Fondo", new Vector3(-7, 0, 12.5f), new Vector3(7, 0, 12.5f), "wall_cafe");
            Wall(env, "Baño / Depósito", new Vector3(-4, 0, 9), new Vector3(-4, 0, 12.5f), "wall_cafe");
            Wall(env, "Depósito / Trastienda", new Vector3(2, 0, 9), new Vector3(2, 0, 12.5f), "wall_cafe");
            Box("Depósito (cerrado)", env, new Vector3(-1f, 1.35f, 10.75f), new Vector3(6f, 2.7f, 3.5f), "wall_cafe");

            // Barra a lo largo del fondo del salón
            Box("Barra", env, new Vector3(-1f, 0.52f, 7.6f), new Vector3(8f, 1.05f, 0.7f), "wood");
            Box("Cubierta de la barra", env, new Vector3(-1f, 1.07f, 7.6f), new Vector3(8.1f, 0.04f, 0.8f), "dark", keepCollider: false);
            Box("Cafetera", env, new Vector3(-4f, 1.3f, 8.6f), new Vector3(0.7f, 0.45f, 0.4f), "metal");
            Box("Vitrina de postres", env, new Vector3(1.8f, 1.25f, 7.6f), new Vector3(1.2f, 0.35f, 0.6f), "glass", keepCollider: false);
            Box("Repisa", env, new Vector3(-1f, 1.85f, 8.85f), new Vector3(6f, 0.05f, 0.25f), "wood", keepCollider: false);
            for (int i = 0; i < 12; i++) Cylinder($"Botella {i}", env, new Vector3(-3.8f + i * 0.5f, 2.02f, 8.85f), new Vector3(0.07f, 0.15f, 0.07f), i % 2 == 0 ? "glass" : "dark");
            for (int i = 0; i < 4; i++) Cylinder($"Butaco {i}", env, new Vector3(-3.5f + i * 1.6f, 0.35f, 6.9f), new Vector3(0.35f, 0.35f, 0.35f), "dark", keepCollider: true);

            // Televisor con el partido repetido (pared este)
            Box("Televisor", env, new Vector3(6.9f, 2.0f, 5f), new Vector3(0.08f, 0.75f, 1.3f), "dark");
            Box("Pantalla (partido)", env, new Vector3(6.85f, 2.0f, 5f), new Vector3(0.01f, 0.65f, 1.2f), "screen", keepCollider: false);

            // Mesas
            foreach (var p in new[] { new Vector3(-5f, 0, 2.2f), new Vector3(-2.5f, 0, 2.6f), new Vector3(0.2f, 0, 3.4f), new Vector3(4.5f, 0, 2.6f),
                                      new Vector3(-5.2f, 0, 5.2f), new Vector3(2.6f, 0, 5.4f), new Vector3(5.3f, 0, 5.6f) })
                Table(env, p);
            Box("Tablero de precios", env, new Vector3(-6.9f, 1.8f, 5f), new Vector3(0.04f, 0.9f, 1.6f), "dark", keepCollider: false);

            // Trastienda: escritorio con el monitor de la cámara del salón
            Box("Escritorio de la trastienda", env, new Vector3(5.6f, 0.37f, 11.6f), new Vector3(1.6f, 0.74f, 0.7f), "wood");
            Box("Cajas de mercancía", env, new Vector3(2.8f, 0.4f, 12f), new Vector3(1f, 0.8f, 0.8f), "cork");
            Box("Estantería", env, new Vector3(6.75f, 1f, 9.8f), new Vector3(0.4f, 2f, 1.4f), "metal");
            // Baño
            Box("Lavamanos", env, new Vector3(-6.7f, 0.85f, 10.2f), new Vector3(0.45f, 0.15f, 0.5f), "paper");
            Box("Sanitario", env, new Vector3(-5f, 0.4f, 12.1f), new Vector3(0.4f, 0.8f, 0.6f), "paper");

            var ev = Group("Evidence");
            // 1. Barra: recibo del datáfono junto a la caja registradora
            var recibo = Pickup(ev, "ev_c1_recibo_cafe", new Vector3(0.4f, 1.12f, 7.45f), new Vector3(0.4f, 0.15f, 0.35f));
            Box("Datáfono", recibo.transform, new Vector3(0.55f, 1.13f, 7.55f), new Vector3(0.09f, 0.05f, 0.17f), "dark", keepCollider: false);
            Box("Recibo", recibo.transform, new Vector3(0.3f, 1.093f, 7.4f), new Vector3(0.07f, 0.004f, 0.16f), "paper", rotY: 20f, keepCollider: false);
            Box("Caja registradora", env, new Vector3(-0.4f, 1.22f, 7.65f), new Vector3(0.4f, 0.25f, 0.35f), "dark", keepCollider: false);
            // 2. Trastienda: monitor con el video de la cámara del salón
            var video = Pickup(ev, "ev_c1_camara_cafe", new Vector3(5.6f, 0.98f, 11.75f), new Vector3(0.6f, 0.45f, 0.4f));
            Box("Monitor", video.transform, new Vector3(5.6f, 0.98f, 11.8f), new Vector3(0.5f, 0.35f, 0.06f), "dark", keepCollider: false);
            Box("Imagen de la cámara", video.transform, new Vector3(5.6f, 0.98f, 11.765f), new Vector3(0.45f, 0.3f, 0.01f), "screen", keepCollider: false);
            Box("Cámara del techo", env, new Vector3(6.6f, 2.55f, 0.5f), new Vector3(0.1f, 0.08f, 0.18f), "metal", rotY: -140f, keepCollider: false);
            // 3. Mesa de Carlos (junto a la vitrina derecha): servilleta con nota
            var servilleta = Pickup(ev, "ev_c1_servilleta", new Vector3(4.3f, 0.8f, 2.45f), new Vector3(0.3f, 0.12f, 0.3f));
            Box("Servilleta", servilleta.transform, new Vector3(4.3f, 0.765f, 2.45f), new Vector3(0.14f, 0.003f, 0.14f), "paper", rotY: 30f, keepCollider: false);
            Cylinder("Taza de tinto", servilleta.transform, new Vector3(4.7f, 0.8f, 2.75f), new Vector3(0.08f, 0.045f, 0.08f), "paper");

            SpawnPlayer(new Vector3(0f, 0.05f, 1.0f), 0f);
            Npc("carlos", new Vector3(4.55f, 0f, 3.35f), 180f, seated: true);
            Trigger<SceneCloseTrigger>("Puerta del café (repasar el caso)", new Vector3(0f, 1.05f, -0.05f), new Vector3(1.1f, 2.1f, 0.08f), "glass", "Repasar el caso (hipótesis)");

            CityKit.Surround(new CityKit.Options
            {
                StreetY = -0.15f, Night = true, Seed = 12,
                HostMin = new Vector2(-7.2f, -0.2f), HostMax = new Vector2(7.2f, 12.7f), GridCenter = new Vector2(0f, 14f),
                HostHeight = 12f, PlayFloorY = 0f, PlayCeilingY = WallHeight, HostMaterial = "fachada_cafe"
            });
            Finish(scene, ScenePath);
        }

        static void Table(Transform parent, Vector3 floorPos)
        {
            Cylinder("Mesa", parent, floorPos + new Vector3(0, 0.74f, 0), new Vector3(0.8f, 0.02f, 0.8f), "wood", keepCollider: true);
            Cylinder("Pata", parent, floorPos + new Vector3(0, 0.37f, 0), new Vector3(0.08f, 0.37f, 0.08f), "dark", keepCollider: true);
        }
    }
}
