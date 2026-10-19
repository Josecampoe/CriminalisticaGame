using Caso404.Decisions;
using UnityEditor;
using UnityEngine;
using static Caso404.EditorTools.SceneKit;

namespace Caso404.EditorTools
{
    /// <summary>
    /// Arma la escena c1_s1 "Apartamento 404" en greybox: sala, balcón y pasillo, sus 7 evidencias, Laura, el jugador y la UI.
    /// Uso: menú Caso404/Escenas/c1_s1, o -executeMethod Caso404.EditorTools.Apartment404SceneBuilder.Build
    /// </summary>
    public static class Apartment404SceneBuilder
    {
        public const string ScenePath = ScenesFolder + "/Apartamento404.unity";

        [MenuItem("Caso404/Escenas/c1_s1 Apartamento 404")]
        public static void Build()
        {
            var scene = Begin("c1", "c1_s1");

            Ambient(new Color(0.32f, 0.34f, 0.38f), new Color(0.55f, 0.58f, 0.62f)); // mañana gris
            Sun("Luz de mañana (gris)", new Color(0.78f, 0.82f, 0.9f), 0.9f, new Vector3(28f, 200f, 0f));
            PointLight("Lámpara de la sala (cálida)", new Vector3(-0.5f, 2.4f, 3f), new Color(1f, 0.86f, 0.7f), 1.0f, 7f);
            PointLight("Luz del escritorio", new Vector3(3f, 2.3f, 4.5f), new Color(1f, 0.85f, 0.65f), 0.8f, 4f);
            PointLight("Fluorescente del pasillo", new Vector3(1f, 2.5f, -1f), new Color(0.85f, 0.92f, 1f), 1.2f, 7f);
            PointLight("Luz del balcón", new Vector3(0f, 2.2f, 7.2f), new Color(0.8f, 0.85f, 0.95f), 0.8f, 4f);

            var env = Group("Environment");
            BuildApartment(Group("Apartamento404", env));
            BuildHallway(Group("Pasillo", env));
            BuildBalcony(Group("Balcon", env));
            BuildEvidence(Group("Evidence"));

            SpawnPlayer(new Vector3(2.5f, 0.05f, 1.2f), 0f);
            Npc("laura", new Vector3(0.6f, 0f, -1.1f), 60f); // mirando hacia la puerta del 404
            Trigger<SceneCloseTrigger>("Ascensor (repasar el caso)", new Vector3(-3.85f, 1.1f, -1f), new Vector3(0.1f, 2.2f, 1.2f), "metal", "Repasar el caso y decidir");

            CityKit.Surround(-12f, false, 404); // 4.º piso, mañana gris
            Finish(scene, ScenePath);
        }

        static void BuildApartment(Transform room)
        {
            // Sala: x ∈ [-4, 4], z ∈ [0, 6]
            FloorAndCeiling(room, "Sala", new Vector2(-4, 0), new Vector2(4, 6), "floor");
            Wall(room, "Pared Oeste", new Vector3(-4, 0, 0), new Vector3(-4, 0, 6), "wall");
            Wall(room, "Pared Este", new Vector3(4, 0, 0), new Vector3(4, 0, 6), "wall");
            Wall(room, "Pared Sur", new Vector3(-4, 0, 0), new Vector3(4, 0, 0), "wall", new Opening(6.5f, 1f));
            // Norte: ventana rota (x -2..-0.2, alto 0.9–2.1) y puerta del balcón (x 0.6..1.6)
            Wall(room, "Pared Norte", new Vector3(-4, 0, 6), new Vector3(4, 0, 6), "wall", new Opening(2.9f, 1.8f, 1.2f, 0.9f), new Opening(5.1f, 1f));

            Box("Sofá base", room, new Vector3(-3.4f, 0.25f, 2.5f), new Vector3(0.9f, 0.5f, 2f), "fabric");
            Box("Sofá respaldo", room, new Vector3(-3.8f, 0.7f, 2.5f), new Vector3(0.2f, 0.6f, 2f), "fabric");
            Box("Mesa de centro (corrida)", room, new Vector3(-1.6f, 0.22f, 2.9f), new Vector3(1.1f, 0.44f, 0.6f), "wood", rotY: 18f);
            Box("Silla volcada", room, new Vector3(-0.4f, 0.2f, 4.2f), new Vector3(0.45f, 0.45f, 0.45f), "wood", rotY: 40f, rotX: 80f);
            for (int i = 0; i < 6; i++)
                Box($"Papel {i}", room, new Vector3(-1f + i * 0.35f, 0.006f, 3.6f + (i % 2) * 0.3f), new Vector3(0.21f, 0.004f, 0.29f), "paper", rotY: i * 27f, keepCollider: false);
            Box("Estante", room, new Vector3(-3.8f, 1f, 5.2f), new Vector3(0.35f, 2f, 1.2f), "wood");
            Box("Alfombra", room, new Vector3(-1.4f, 0.003f, 3f), new Vector3(2.4f, 0.005f, 1.8f), "fabric", keepCollider: false);
        }

        static void BuildHallway(Transform hall)
        {
            // Pasillo: x ∈ [-4, 6], z ∈ [-2, 0]
            FloorAndCeiling(hall, "Pasillo", new Vector2(-4, -2), new Vector2(6, 0), "floor_hall");
            Wall(hall, "Pared Sur", new Vector3(-4, 0, -2), new Vector3(6, 0, -2), "wall_hall");
            Wall(hall, "Pared Oeste", new Vector3(-4, 0, -2), new Vector3(-4, 0, 0), "wall_hall");
            Wall(hall, "Pared Este", new Vector3(6, 0, -2), new Vector3(6, 0, 0), "wall_hall");
            Wall(hall, "Pared Norte (este)", new Vector3(4, 0, 0), new Vector3(6, 0, 0), "wall_hall");
            Box("Puerta 403", hall, new Vector3(-1.5f, 1.05f, -1.93f), new Vector3(1f, 2.1f, 0.05f), "wood");
            Box("Número 404", hall, new Vector3(2.5f, 2.25f, -0.09f), new Vector3(0.3f, 0.12f, 0.02f), "metal", keepCollider: false);
        }

        static void BuildBalcony(Transform b)
        {
            // Balcón: x ∈ [-2.5, 2.5], z ∈ [6, 8]
            Box("Piso", b, new Vector3(0, -0.05f, 7), new Vector3(5, 0.1f, 2), "floor_balcony");
            Box("Baranda N", b, new Vector3(0, 0.5f, 8), new Vector3(5, 1f, 0.08f), "metal");
            Box("Baranda O", b, new Vector3(-2.5f, 0.5f, 7), new Vector3(0.08f, 1f, 2), "metal");
            Box("Baranda E", b, new Vector3(2.5f, 0.5f, 7), new Vector3(0.08f, 1f, 2), "metal");
            Box("Matera", b, new Vector3(2.1f, 0.2f, 7.6f), new Vector3(0.4f, 0.4f, 0.4f), "dark");
            // Colliders invisibles sobre la baranda: el jugador no puede caerse del balcón.
            foreach (var (pos, size) in new[] { (new Vector3(0, 1.5f, 8.05f), new Vector3(5.2f, 3f, 0.1f)),
                                                (new Vector3(-2.55f, 1.5f, 7f), new Vector3(0.1f, 3f, 2.2f)),
                                                (new Vector3(2.55f, 1.5f, 7f), new Vector3(0.1f, 3f, 2.2f)) })
            {
                var wall = new GameObject("Límite invisible del balcón");
                wall.transform.SetParent(b, false);
                wall.transform.position = pos;
                wall.AddComponent<BoxCollider>().size = size;
            }
        }

        static void BuildEvidence(Transform root)
        {
            // 1. Ventana rota (vidrios hacia el balcón)
            var ventana = Pickup(root, "ev_c1_ventana", new Vector3(-1.1f, 1.5f, 6f), new Vector3(1.8f, 1.2f, 0.25f));
            Box("Marco sup", ventana.transform, new Vector3(-1.1f, 2.08f, 6f), new Vector3(1.8f, 0.05f, 0.1f), "dark", keepCollider: false);
            Box("Marco inf", ventana.transform, new Vector3(-1.1f, 0.92f, 6f), new Vector3(1.8f, 0.05f, 0.1f), "dark", keepCollider: false);
            Box("Vidrio restante", ventana.transform, new Vector3(-1.75f, 1.75f, 6f), new Vector3(0.45f, 0.6f, 0.01f), "glass", rotZ: 12f, keepCollider: false);
            Box("Vidrio restante 2", ventana.transform, new Vector3(-0.5f, 1.15f, 6f), new Vector3(0.4f, 0.35f, 0.01f), "glass", rotZ: -20f, keepCollider: false);
            for (int i = 0; i < 9; i++)
                Box($"Vidrio en el balcón {i}", ventana.transform, new Vector3(-1.8f + i * 0.17f, 0.01f, 6.25f + (i % 3) * 0.15f),
                    new Vector3(0.08f, 0.005f, 0.06f), "glass", rotY: i * 41f, keepCollider: false);

            // 2. Cerradura de la puerta (sin marcas de forzado)
            var puerta = Pickup(root, "ev_c1_puerta", new Vector3(2.1f, 1.0f, 0f), new Vector3(0.35f, 0.4f, 0.35f));
            Box("Hoja de la puerta (entreabierta)", puerta.transform, new Vector3(2.05f, 1.05f, 0.45f), new Vector3(0.05f, 2.1f, 0.9f), "wood", keepCollider: false);
            Box("Cerradura", puerta.transform, new Vector3(2.1f, 1.0f, 0.88f), new Vector3(0.06f, 0.14f, 0.06f), "metal", keepCollider: false);
            Box("Marco", puerta.transform, new Vector3(2.02f, 1.05f, 0f), new Vector3(0.06f, 2.1f, 0.18f), "dark", keepCollider: false);
            puerta.GetComponent<BoxCollider>().center = new Vector3(0f, 0f, 0.85f);

            // 3. Huella de zapato en el balcón
            var huella = Pickup(root, "ev_c1_huella", new Vector3(-0.9f, 0.05f, 6.7f), new Vector3(0.35f, 0.15f, 0.45f));
            Box("Huella", huella.transform, new Vector3(-0.9f, 0.004f, 6.7f), new Vector3(0.11f, 0.004f, 0.29f), "mud", rotY: 15f, keepCollider: false);

            // 4. Colilla en el balcón
            var colilla = Pickup(root, "ev_c1_colilla", new Vector3(1.4f, 0.05f, 7.4f), new Vector3(0.25f, 0.12f, 0.25f));
            var butt = Cylinder("Colilla", colilla.transform, new Vector3(1.4f, 0.01f, 7.4f), new Vector3(0.012f, 0.025f, 0.012f), "paper");
            butt.transform.rotation = Quaternion.Euler(0, 30, 90);

            // 5. Teléfono bajo el sofá
            var telefono = Pickup(root, "ev_c1_telefono", new Vector3(-2.85f, 0.05f, 2.2f), new Vector3(0.3f, 0.12f, 0.3f));
            Box("Teléfono", telefono.transform, new Vector3(-2.85f, 0.006f, 2.2f), new Vector3(0.075f, 0.01f, 0.155f), "dark", rotY: 70f, keepCollider: false);

            // 6. Escritorio vacío (marca del portátil, cargador y fotos con el cuaderno rojo)
            var escritorio = Pickup(root, "ev_c1_escritorio", new Vector3(3.5f, 0.75f, 3.6f), new Vector3(0.8f, 0.3f, 1.5f));
            Box("Tablero", escritorio.transform, new Vector3(3.5f, 0.73f, 3.6f), new Vector3(0.75f, 0.04f, 1.4f), "wood", keepCollider: false);
            foreach (var z in new[] { 2.95f, 4.25f })
                foreach (var x in new[] { 3.2f, 3.8f })
                    Box("Pata", escritorio.transform, new Vector3(x, 0.355f, z), new Vector3(0.05f, 0.71f, 0.05f), "wood", keepCollider: false);
            Box("Marca en el polvo", escritorio.transform, new Vector3(3.5f, 0.752f, 3.6f), new Vector3(0.24f, 0.002f, 0.34f), "dust", keepCollider: false);
            Box("Cargador", escritorio.transform, new Vector3(3.55f, 0.76f, 3.95f), new Vector3(0.06f, 0.03f, 0.1f), "dark", keepCollider: false);
            Box("Cable", escritorio.transform, new Vector3(3.6f, 0.755f, 3.7f), new Vector3(0.01f, 0.008f, 0.5f), "dark", keepCollider: false);
            for (int i = 0; i < 3; i++)
            {
                Box($"Foto {i}", escritorio.transform, new Vector3(3.93f, 1.5f + (i % 2) * 0.22f, 3.1f + i * 0.45f), new Vector3(0.02f, 0.25f, 0.33f), "paper", keepCollider: false);
                Box($"Cuaderno rojo en la foto {i}", escritorio.transform, new Vector3(3.915f, 1.46f + (i % 2) * 0.22f, 3.1f + i * 0.45f), new Vector3(0.01f, 0.06f, 0.05f), "red", keepCollider: false);
            }
            Box("Silla del escritorio", root, new Vector3(2.9f, 0.25f, 3.6f), new Vector3(0.45f, 0.5f, 0.45f), "dark");

            // 7. Cámara del pasillo (la grabación la entrega ALEX)
            var camara = Pickup(root, "ev_c1_camara_pasillo", new Vector3(4.6f, 2.3f, -0.25f), new Vector3(0.4f, 0.35f, 0.45f));
            Box("Cuerpo", camara.transform, new Vector3(4.6f, 2.3f, -0.3f), new Vector3(0.12f, 0.1f, 0.22f), "metal", rotY: -35f, keepCollider: false);
            Box("Lente", camara.transform, new Vector3(4.53f, 2.28f, -0.4f), new Vector3(0.06f, 0.06f, 0.03f), "glass", rotY: -35f, keepCollider: false);
            Box("Soporte", camara.transform, new Vector3(4.6f, 2.42f, -0.18f), new Vector3(0.03f, 0.2f, 0.03f), "metal", keepCollider: false);
        }
    }
}
