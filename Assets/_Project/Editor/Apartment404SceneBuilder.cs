using Caso404.Decisions;
using UnityEditor;
using UnityEngine;
using static Caso404.EditorTools.SceneKit;

namespace Caso404.EditorTools
{
    /// <summary>
    /// Escena c1_s1 "Apartamento 404" (Edificio Mirador, piso 4), luz de mañana gris.
    /// Pasillo del edificio con las puertas 401–405 y el ascensor; apartamento con sala-comedor, cocina abierta, estudio
    /// con el escritorio de Elena, pasillo interno, baño, habitación y un balcón ancho. Las 7 evidencias están repartidas.
    /// Uso: menú Caso404/Escenas/c1_s1, o -executeMethod Caso404.EditorTools.Apartment404SceneBuilder.Build
    /// </summary>
    public static class Apartment404SceneBuilder
    {
        public const string ScenePath = ScenesFolder + "/Apartamento404.unity";

        // Planta (metros, piso del nivel 4 en y = 0)
        // Pasillo del edificio: x ∈ [-14, 14], z ∈ [-3, 0]
        // Apartamento: x ∈ [-6, 11], z ∈ [0, 8]   ·  Balcón: x ∈ [-6, 2], z ∈ [8, 10.5]
        //   Sala-comedor x ∈ [-6, 2] · Cocina x ∈ [2, 6], z ∈ [0, 3.5] · Estudio x ∈ [2, 6], z ∈ [3.5, 8]
        //   Pasillo interno x ∈ [6, 7.4] · Baño x ∈ [7.4, 11], z ∈ [0, 3.2] · Habitación x ∈ [7.4, 11], z ∈ [3.2, 8]

        [MenuItem("Caso404/Escenas/c1_s1 Apartamento 404")]
        public static void Build()
        {
            var scene = Begin("c1", "c1_s1");

            Ambient(new Color(0.34f, 0.36f, 0.4f), new Color(0.62f, 0.64f, 0.67f)); // mañana gris
            Sun("Luz de mañana (gris)", new Color(0.78f, 0.82f, 0.9f), 0.9f, new Vector3(30f, 200f, 0f));
            PointLight("Lámpara de la sala", new Vector3(-3.5f, 2.45f, 4.5f), new Color(1f, 0.86f, 0.7f), 1.0f, 7f);
            PointLight("Lámpara del comedor", new Vector3(-2.5f, 2.45f, 1.6f), new Color(1f, 0.86f, 0.7f), 0.8f, 5f);
            PointLight("Luz de la cocina", new Vector3(4f, 2.5f, 1.8f), new Color(0.95f, 0.95f, 1f), 0.9f, 5f);
            PointLight("Lámpara del escritorio", new Vector3(4.8f, 2.3f, 6f), new Color(1f, 0.85f, 0.65f), 0.9f, 4.5f);
            PointLight("Luz de la habitación", new Vector3(9.2f, 2.45f, 5.6f), new Color(1f, 0.88f, 0.72f), 0.8f, 5f);
            PointLight("Luz del baño", new Vector3(9.2f, 2.45f, 1.6f), new Color(0.92f, 0.95f, 1f), 0.8f, 4f);
            for (int i = 0; i < 4; i++)
                PointLight($"Fluorescente del pasillo {i}", new Vector3(-10.5f + i * 7f, 2.55f, -1.5f), new Color(0.85f, 0.92f, 1f), 1.0f, 6f);

            var env = Group("Environment");
            BuildHallway(Group("Pasillo del edificio", env));
            BuildApartmentShell(Group("Apartamento 404", env));
            BuildLivingDining(Group("Sala-comedor", env));
            BuildKitchen(Group("Cocina", env));
            BuildStudy(Group("Estudio", env));
            BuildBedroomAndBath(Group("Habitación y baño", env));
            BuildBalcony(Group("Balcón", env));
            BuildEvidence(Group("Evidence"));

            SpawnPlayer(new Vector3(-12.2f, 0.05f, -1.5f), 90f);           // saliendo del ascensor
            Npc("laura", new Vector3(-2.6f, 0f, -1.9f), 70f);               // en el pasillo, junto a la puerta del 404
            var elevator = Trigger<SceneCloseTrigger>("Ascensor (repasar el caso)", new Vector3(-13.9f, 1.1f, -1.5f), new Vector3(0.1f, 2.2f, 1.3f), "metal", "Repasar el caso y decidir");
            Box("Botonera del ascensor", elevator.transform, new Vector3(-13.92f, 1.2f, -0.6f), new Vector3(0.04f, 0.3f, 0.15f), "dark", keepCollider: false);

            CityKit.Surround(new CityKit.Options
            {
                StreetY = -12f, Night = false, Seed = 404,
                HostMin = new Vector2(-15f, -4.5f), HostMax = new Vector2(15f, 8.1f), GridCenter = new Vector2(0f, -3f), // la calle pasa frente al balcón
                HostHeight = 24f, PlayFloorY = 0f, PlayCeilingY = WallHeight
            });
            Finish(scene, ScenePath);
        }

        // ---------------- Pasillo del edificio ----------------

        static void BuildHallway(Transform h)
        {
            FloorAndCeiling(h, "Pasillo", new Vector2(-14, -3), new Vector2(14, 0), "floor_hall");
            Wall(h, "Pared Sur", new Vector3(-14, 0, -3), new Vector3(14, 0, -3), "wall_hall");
            Wall(h, "Pared Oeste (ascensor)", new Vector3(-14, 0, -3), new Vector3(-14, 0, 0), "wall_hall");
            Wall(h, "Pared Este (ventana)", new Vector3(14, 0, -3), new Vector3(14, 0, 0), "wall_hall", new Opening(1.5f, 1.4f, 1.3f, 0.9f));
            Box("Vidrio de la ventana del pasillo", h, new Vector3(14f, 1.55f, -1.5f), new Vector3(0.03f, 1.3f, 1.4f), "glass");
            // Pared norte compartida con el apartamento: 403 y 405 son vecinos; el 404 tiene su puerta abierta
            Wall(h, "Pared Norte", new Vector3(-14, 0, 0), new Vector3(14, 0, 0), "wall_hall", new Opening(14f, 1f));

            Door(h, "401", new Vector3(-8f, 0, -3f), facingNorth: true);
            Door(h, "402", new Vector3(6f, 0, -3f), facingNorth: true);
            Door(h, "403", new Vector3(-10f, 0, 0f), facingNorth: false);
            Door(h, "405", new Vector3(12.5f, 0, 0f), facingNorth: false);
            Box("Número 404", h, new Vector3(0.75f, 1.75f, -0.09f), new Vector3(0.22f, 0.1f, 0.02f), "metal", keepCollider: false);

            // Ascensor
            Box("Marco del ascensor", h, new Vector3(-13.95f, 1.2f, -1.5f), new Vector3(0.05f, 2.4f, 1.6f), "dark", keepCollider: false);
            Box("Indicador de piso (4)", h, new Vector3(-13.92f, 2.45f, -1.5f), new Vector3(0.03f, 0.12f, 0.3f), "red", keepCollider: false);
            Box("Planta del pasillo", h, new Vector3(13.6f, 0.4f, -2.6f), new Vector3(0.45f, 0.8f, 0.45f), "dark");
            Box("Tapete del 404", h, new Vector3(0f, 0.004f, -0.5f), new Vector3(0.9f, 0.006f, 0.55f), "fabric", keepCollider: false);
            Box("Extintor", h, new Vector3(4f, 0.8f, -2.9f), new Vector3(0.18f, 0.5f, 0.18f), "red");
            Box("Señal de escaleras", h, new Vector3(-12.8f, 2.35f, -2.93f), new Vector3(0.5f, 0.18f, 0.02f), "screen", keepCollider: false);
        }

        static void Door(Transform h, string number, Vector3 wallPoint, bool facingNorth)
        {
            float z = wallPoint.z + (facingNorth ? 0.08f : -0.08f);
            Box($"Puerta {number}", h, new Vector3(wallPoint.x, 1.05f, z), new Vector3(1f, 2.1f, 0.06f), "wood", keepCollider: false);
            Box($"Manija {number}", h, new Vector3(wallPoint.x + 0.38f, 1.0f, z + (facingNorth ? 0.05f : -0.05f)), new Vector3(0.12f, 0.03f, 0.03f), "metal", keepCollider: false);
            Box($"Número {number}", h, new Vector3(wallPoint.x, 1.75f, z + (facingNorth ? 0.04f : -0.04f)), new Vector3(0.22f, 0.1f, 0.02f), "metal", keepCollider: false);
        }

        // ---------------- Apartamento: paredes y pisos ----------------

        static void BuildApartmentShell(Transform a)
        {
            FloorAndCeiling(a, "Sala-comedor", new Vector2(-6, 0), new Vector2(2, 8), "floor");
            FloorAndCeiling(a, "Cocina", new Vector2(2, 0), new Vector2(6, 3.5f), "floor_tile");
            FloorAndCeiling(a, "Estudio", new Vector2(2, 3.5f), new Vector2(6, 8), "floor");
            FloorAndCeiling(a, "Pasillo interno", new Vector2(6, 0), new Vector2(7.4f, 8), "floor");
            FloorAndCeiling(a, "Baño", new Vector2(7.4f, 0), new Vector2(11, 3.2f), "floor_tile");
            FloorAndCeiling(a, "Habitación", new Vector2(7.4f, 3.2f), new Vector2(11, 8), "floor");

            Wall(a, "Pared Oeste", new Vector3(-6, 0, 0), new Vector3(-6, 0, 8), "wall");
            Wall(a, "Pared Este", new Vector3(11, 0, 0), new Vector3(11, 0, 8), "wall", new Opening(5.6f, 1.6f, 1.2f, 0.9f));
            Box("Vidrio de la habitación", a, new Vector3(11f, 1.5f, 5.6f), new Vector3(0.03f, 1.2f, 1.6f), "glass");
            // Norte: ventana rota de la sala, puerta del balcón, ventana del estudio y de la habitación
            Wall(a, "Pared Norte", new Vector3(-6, 0, 8), new Vector3(11, 0, 8), "wall",
                new Opening(2.2f, 2.4f, 1.3f, 0.9f), new Opening(6.5f, 1.2f), new Opening(10f, 1.6f, 1.2f, 1f), new Opening(15f, 1.4f, 1.2f, 1f));
            Box("Vidrio del estudio", a, new Vector3(4f, 1.6f, 8f), new Vector3(1.6f, 1.2f, 0.03f), "glass");
            Box("Vidrio de la habitación (norte)", a, new Vector3(9f, 1.6f, 8f), new Vector3(1.4f, 1.2f, 0.03f), "glass");
            // Divisiones internas
            Wall(a, "Sala / Estudio", new Vector3(2, 0, 3.5f), new Vector3(2, 0, 8), "wall", new Opening(1.1f, 1f));
            Wall(a, "Cocina / Estudio", new Vector3(2, 0, 3.5f), new Vector3(6, 0, 3.5f), "wall");
            Wall(a, "Cocina y estudio / Pasillo interno", new Vector3(6, 0, 0), new Vector3(6, 0, 8), "wall", new Opening(1.8f, 1f));
            Wall(a, "Pasillo interno / Baño y habitación", new Vector3(7.4f, 0, 0), new Vector3(7.4f, 0, 8), "wall", new Opening(1.6f, 0.9f), new Opening(5f, 0.9f));
            Wall(a, "Baño / Habitación", new Vector3(7.4f, 0, 3.2f), new Vector3(11, 0, 3.2f), "wall");
        }

        static void BuildLivingDining(Transform r)
        {
            // Sala: sofá contra la pared oeste mirando al mueble del televisor
            Box("Sofá base", r, new Vector3(-5.45f, 0.25f, 4.6f), new Vector3(0.9f, 0.5f, 2.2f), "fabric");
            Box("Sofá respaldo", r, new Vector3(-5.85f, 0.7f, 4.6f), new Vector3(0.2f, 0.6f, 2.2f), "fabric");
            Box("Sofá brazo N", r, new Vector3(-5.45f, 0.45f, 5.75f), new Vector3(0.9f, 0.4f, 0.15f), "fabric");
            Box("Sofá brazo S", r, new Vector3(-5.45f, 0.45f, 3.45f), new Vector3(0.9f, 0.4f, 0.15f), "fabric");
            Box("Mesa de centro (corrida)", r, new Vector3(-3.7f, 0.22f, 4.9f), new Vector3(1.1f, 0.44f, 0.6f), "wood", rotY: 18f);
            Box("Alfombra", r, new Vector3(-3.8f, 0.003f, 4.6f), new Vector3(2.6f, 0.005f, 2.2f), "fabric", keepCollider: false);
            Box("Mueble del televisor", r, new Vector3(1.75f, 0.3f, 6.2f), new Vector3(0.45f, 0.6f, 1.6f), "wood");
            Box("Televisor", r, new Vector3(1.8f, 1.0f, 6.2f), new Vector3(0.06f, 0.65f, 1.15f), "screen_off", keepCollider: false);
            Box("Estante de libros", r, new Vector3(-5.8f, 1f, 7.2f), new Vector3(0.35f, 2f, 1.2f), "wood");
            for (int i = 0; i < 5; i++)
                Box($"Libros {i}", r, new Vector3(-5.68f, 0.35f + i * 0.38f, 7.2f), new Vector3(0.22f, 0.25f, 1f), i % 2 == 0 ? "red" : "paper", keepCollider: false);
            Box("Lámpara de pie", r, new Vector3(-5.6f, 0.8f, 2.9f), new Vector3(0.12f, 1.6f, 0.12f), "metal");
            // Comedor
            Box("Mesa del comedor", r, new Vector3(-2.6f, 0.74f, 1.6f), new Vector3(1.6f, 0.05f, 0.95f), "wood");
            foreach (var x in new[] { -3.3f, -1.9f })
                foreach (var z in new[] { 1.2f, 2f })
                    Box("Pata de la mesa", r, new Vector3(x, 0.37f, z), new Vector3(0.06f, 0.74f, 0.06f), "wood", keepCollider: false);
            Box("Silla del comedor 1", r, new Vector3(-3.2f, 0.25f, 0.85f), new Vector3(0.45f, 0.5f, 0.45f), "wood");
            Box("Silla del comedor 2", r, new Vector3(-2f, 0.25f, 0.85f), new Vector3(0.45f, 0.5f, 0.45f), "wood");
            Box("Silla del comedor 3", r, new Vector3(-3.2f, 0.25f, 2.35f), new Vector3(0.45f, 0.5f, 0.45f), "wood");
            Box("Silla volcada", r, new Vector3(-1.4f, 0.2f, 2.9f), new Vector3(0.45f, 0.45f, 0.45f), "wood", rotY: 40f, rotX: 80f);
            for (int i = 0; i < 7; i++)
                Box($"Papel {i}", r, new Vector3(-2.2f + i * 0.4f, 0.006f, 3.3f + (i % 3) * 0.35f), new Vector3(0.21f, 0.004f, 0.29f), "paper", rotY: i * 27f, keepCollider: false);
            Box("Cuadro", r, new Vector3(-5.92f, 1.6f, 1.6f), new Vector3(0.03f, 0.6f, 0.9f), "cork", keepCollider: false);
        }

        static void BuildKitchen(Transform k)
        {
            // Encimera contra la pared del pasillo y barra (isla) abierta hacia la sala
            Box("Encimera", k, new Vector3(4f, 0.45f, 0.35f), new Vector3(3.8f, 0.9f, 0.6f), "wood");
            Box("Cubierta", k, new Vector3(4f, 0.92f, 0.35f), new Vector3(3.9f, 0.04f, 0.65f), "dark", keepCollider: false);
            Box("Estufa", k, new Vector3(3.2f, 0.945f, 0.35f), new Vector3(0.6f, 0.01f, 0.5f), "metal", keepCollider: false);
            Box("Lavaplatos", k, new Vector3(4.6f, 0.93f, 0.35f), new Vector3(0.6f, 0.02f, 0.4f), "metal", keepCollider: false);
            Box("Gabinetes altos", k, new Vector3(4f, 1.9f, 0.2f), new Vector3(3.8f, 0.7f, 0.35f), "wood", keepCollider: false);
            Box("Nevera", k, new Vector3(5.6f, 0.95f, 2.9f), new Vector3(0.7f, 1.9f, 0.7f), "metal");
            Box("Barra", k, new Vector3(3.6f, 0.5f, 2.3f), new Vector3(2.2f, 1f, 0.6f), "wood");
            Box("Cubierta de la barra", k, new Vector3(3.6f, 1.02f, 2.3f), new Vector3(2.3f, 0.04f, 0.75f), "dark", keepCollider: false);
            Cylinder("Taza", k, new Vector3(3.1f, 1.08f, 2.3f), new Vector3(0.08f, 0.045f, 0.08f), "paper");
            Box("Cafetera", k, new Vector3(5.3f, 1.1f, 0.4f), new Vector3(0.25f, 0.32f, 0.25f), "dark", keepCollider: false);
        }

        static void BuildStudy(Transform s)
        {
            Box("Biblioteca", s, new Vector3(3.2f, 1.1f, 7.75f), new Vector3(1.8f, 2.2f, 0.35f), "wood");
            for (int i = 0; i < 4; i++)
                Box($"Archivos {i}", s, new Vector3(3.2f, 0.4f + i * 0.5f, 7.6f), new Vector3(1.6f, 0.3f, 0.05f), i % 2 == 0 ? "paper" : "dark", keepCollider: false);
            Box("Corcho con notas", s, new Vector3(2.08f, 1.6f, 6.6f), new Vector3(0.03f, 0.8f, 1.2f), "cork", keepCollider: false);
            Box("Hilo rojo", s, new Vector3(2.06f, 1.6f, 6.6f), new Vector3(0.005f, 0.01f, 1f), "red", rotX: 15f, keepCollider: false);
            Box("Archivador", s, new Vector3(2.4f, 0.55f, 4.0f), new Vector3(0.5f, 1.1f, 0.6f), "metal");
        }

        static void BuildBedroomAndBath(Transform r)
        {
            // Habitación
            Box("Cama", r, new Vector3(9.4f, 0.3f, 6.3f), new Vector3(1.6f, 0.6f, 2.1f), "fabric");
            Box("Almohadas", r, new Vector3(9.4f, 0.67f, 7.15f), new Vector3(1.3f, 0.15f, 0.35f), "paper", keepCollider: false);
            Box("Cabecero", r, new Vector3(9.4f, 0.75f, 7.4f), new Vector3(1.7f, 1.5f, 0.08f), "wood");
            Box("Mesa de noche", r, new Vector3(10.5f, 0.3f, 7.3f), new Vector3(0.45f, 0.6f, 0.4f), "wood");
            Box("Armario", r, new Vector3(7.75f, 1.05f, 6.6f), new Vector3(0.6f, 2.1f, 1.6f), "wood");
            Box("Maleta abierta", r, new Vector3(9.4f, 0.12f, 4.4f), new Vector3(0.7f, 0.22f, 0.5f), "dark");
            // Baño
            Box("Lavamanos", r, new Vector3(10.7f, 0.85f, 1.2f), new Vector3(0.45f, 0.15f, 0.55f), "paper");
            Box("Espejo", r, new Vector3(10.97f, 1.6f, 1.2f), new Vector3(0.02f, 0.7f, 0.55f), "glass", keepCollider: false);
            Box("Sanitario", r, new Vector3(8.1f, 0.4f, 0.4f), new Vector3(0.4f, 0.8f, 0.6f), "paper");
            Box("Ducha (vidrio)", r, new Vector3(9.5f, 1.05f, 2.2f), new Vector3(1.2f, 2.1f, 0.03f), "glass");
            Box("Piso de la ducha", r, new Vector3(9.5f, 0.02f, 2.7f), new Vector3(1.2f, 0.04f, 0.9f), "concrete", keepCollider: false);
            Box("Toalla", r, new Vector3(7.46f, 1.3f, 2.5f), new Vector3(0.05f, 0.6f, 0.4f), "red", keepCollider: false);
        }

        static void BuildBalcony(Transform b)
        {
            // Balcón ancho: x ∈ [-6, 2], z ∈ [8, 10.5]
            Box("Piso del balcón", b, new Vector3(-2f, -0.05f, 9.25f), new Vector3(8f, 0.1f, 2.5f), "floor_balcony");
            Box("Losa (canto)", b, new Vector3(-2f, -0.2f, 9.25f), new Vector3(8.1f, 0.2f, 2.6f), "concrete", keepCollider: false);
            Box("Baranda N", b, new Vector3(-2f, 0.5f, 10.5f), new Vector3(8f, 1f, 0.08f), "metal");
            Box("Baranda O", b, new Vector3(-6f, 0.5f, 9.25f), new Vector3(0.08f, 1f, 2.5f), "metal");
            Box("Baranda E", b, new Vector3(2f, 0.5f, 9.25f), new Vector3(0.08f, 1f, 2.5f), "metal");
            Box("Matera 1", b, new Vector3(1.6f, 0.2f, 10.1f), new Vector3(0.4f, 0.4f, 0.4f), "dark");
            Box("Matera 2", b, new Vector3(-5.6f, 0.2f, 10.1f), new Vector3(0.4f, 0.4f, 0.4f), "dark");
            Cylinder("Mesita del balcón", b, new Vector3(-3.6f, 0.36f, 9.6f), new Vector3(0.6f, 0.36f, 0.6f), "metal", keepCollider: true);
            Box("Silla del balcón", b, new Vector3(-4.4f, 0.22f, 9.6f), new Vector3(0.42f, 0.44f, 0.42f), "metal");
            // Colliders invisibles sobre la baranda: el jugador no puede caerse del balcón.
            foreach (var (pos, size) in new[] { (new Vector3(-2f, 1.5f, 10.55f), new Vector3(8.2f, 3f, 0.1f)),
                                                (new Vector3(-6.05f, 1.5f, 9.25f), new Vector3(0.1f, 3f, 2.6f)),
                                                (new Vector3(2.05f, 1.5f, 9.25f), new Vector3(0.1f, 3f, 2.6f)) })
            {
                var wall = new GameObject("Límite invisible del balcón");
                wall.transform.SetParent(b, false);
                wall.transform.position = pos;
                wall.AddComponent<BoxCollider>().size = size;
            }
        }

        // ---------------- Evidencias (repartidas por el apartamento) ----------------

        static void BuildEvidence(Transform root)
        {
            // 1. Sala: ventana rota del balcón (los vidrios cayeron hacia afuera)
            var ventana = Pickup(root, "ev_c1_ventana", new Vector3(-3.8f, 1.55f, 8f), new Vector3(2.4f, 1.3f, 0.25f));
            Box("Marco sup", ventana.transform, new Vector3(-3.8f, 2.2f, 8f), new Vector3(2.4f, 0.05f, 0.1f), "dark", keepCollider: false);
            Box("Marco inf", ventana.transform, new Vector3(-3.8f, 0.92f, 8f), new Vector3(2.4f, 0.05f, 0.1f), "dark", keepCollider: false);
            Box("Vidrio restante", ventana.transform, new Vector3(-4.6f, 1.8f, 8f), new Vector3(0.5f, 0.6f, 0.01f), "glass", rotZ: 12f, keepCollider: false);
            Box("Vidrio restante 2", ventana.transform, new Vector3(-3.0f, 1.15f, 8f), new Vector3(0.45f, 0.35f, 0.01f), "glass", rotZ: -20f, keepCollider: false);
            for (int i = 0; i < 12; i++)
                Box($"Vidrio en el balcón {i}", ventana.transform, new Vector3(-4.9f + i * 0.2f, 0.01f, 8.25f + (i % 3) * 0.17f),
                    new Vector3(0.08f, 0.005f, 0.06f), "glass", rotY: i * 41f, keepCollider: false);

            // 2. Entrada: cerradura de la puerta del 404 (sin marcas de forzado); la hoja quedó entreabierta
            var puerta = Pickup(root, "ev_c1_puerta", new Vector3(-0.42f, 1.0f, 0.85f), new Vector3(0.35f, 0.4f, 0.35f));
            Box("Hoja de la puerta (entreabierta)", puerta.transform, new Vector3(-0.47f, 1.05f, 0.45f), new Vector3(0.05f, 2.1f, 0.9f), "wood", keepCollider: false);
            Box("Cerradura", puerta.transform, new Vector3(-0.42f, 1.0f, 0.85f), new Vector3(0.06f, 0.14f, 0.06f), "metal", keepCollider: false);
            Box("Marco", puerta.transform, new Vector3(-0.53f, 1.05f, 0f), new Vector3(0.06f, 2.1f, 0.18f), "dark", keepCollider: false);

            // 3. Balcón (centro): huella de zapato
            var huella = Pickup(root, "ev_c1_huella", new Vector3(-1.2f, 0.05f, 9.5f), new Vector3(0.35f, 0.15f, 0.45f));
            Box("Huella", huella.transform, new Vector3(-1.2f, 0.004f, 9.5f), new Vector3(0.11f, 0.004f, 0.29f), "mud", rotY: 15f, keepCollider: false);

            // 4. Balcón (esquina este, junto a la matera): colilla
            var colilla = Pickup(root, "ev_c1_colilla", new Vector3(1.2f, 0.05f, 10.2f), new Vector3(0.25f, 0.12f, 0.25f));
            var butt = Cylinder("Colilla", colilla.transform, new Vector3(1.2f, 0.01f, 10.2f), new Vector3(0.012f, 0.025f, 0.012f), "paper");
            butt.transform.rotation = Quaternion.Euler(0, 30, 90);

            // 5. Sala: teléfono de Laura bajo el sofá
            var telefono = Pickup(root, "ev_c1_telefono", new Vector3(-4.9f, 0.05f, 4.1f), new Vector3(0.3f, 0.12f, 0.3f));
            Box("Teléfono", telefono.transform, new Vector3(-4.9f, 0.006f, 4.1f), new Vector3(0.075f, 0.01f, 0.155f), "dark", rotY: 70f, keepCollider: false);

            // 6. Estudio: escritorio vacío de Elena (marca del portátil, cargador y fotos con el cuaderno rojo)
            var escritorio = Pickup(root, "ev_c1_escritorio", new Vector3(5.45f, 0.75f, 5.9f), new Vector3(0.8f, 0.3f, 1.5f));
            Box("Tablero", escritorio.transform, new Vector3(5.45f, 0.73f, 5.9f), new Vector3(0.75f, 0.04f, 1.4f), "wood", keepCollider: false);
            foreach (var z in new[] { 5.25f, 6.55f })
                foreach (var x in new[] { 5.15f, 5.75f })
                    Box("Pata", escritorio.transform, new Vector3(x, 0.355f, z), new Vector3(0.05f, 0.71f, 0.05f), "wood", keepCollider: false);
            Box("Marca en el polvo", escritorio.transform, new Vector3(5.45f, 0.752f, 5.9f), new Vector3(0.24f, 0.002f, 0.34f), "dust", keepCollider: false);
            Box("Cargador", escritorio.transform, new Vector3(5.5f, 0.76f, 6.25f), new Vector3(0.06f, 0.03f, 0.1f), "dark", keepCollider: false);
            Box("Cable", escritorio.transform, new Vector3(5.55f, 0.755f, 6f), new Vector3(0.01f, 0.008f, 0.5f), "dark", keepCollider: false);
            for (int i = 0; i < 3; i++)
            {
                Box($"Foto {i}", escritorio.transform, new Vector3(5.93f, 1.5f + (i % 2) * 0.22f, 5.4f + i * 0.45f), new Vector3(0.02f, 0.25f, 0.33f), "paper", keepCollider: false);
                Box($"Cuaderno rojo en la foto {i}", escritorio.transform, new Vector3(5.915f, 1.46f + (i % 2) * 0.22f, 5.4f + i * 0.45f), new Vector3(0.01f, 0.06f, 0.05f), "red", keepCollider: false);
            }
            Box("Silla del escritorio", root, new Vector3(4.85f, 0.25f, 5.9f), new Vector3(0.45f, 0.5f, 0.45f), "dark");

            // 7. Pasillo del edificio: cámara en el techo que apunta a la puerta del 404 (la grabación la entrega ALEX)
            var camara = Pickup(root, "ev_c1_camara_pasillo", new Vector3(2.4f, 2.4f, -0.3f), new Vector3(0.4f, 0.35f, 0.45f));
            Box("Cuerpo", camara.transform, new Vector3(2.4f, 2.4f, -0.35f), new Vector3(0.12f, 0.1f, 0.22f), "metal", rotY: -60f, keepCollider: false);
            Box("Lente", camara.transform, new Vector3(2.3f, 2.38f, -0.42f), new Vector3(0.06f, 0.06f, 0.03f), "glass", rotY: -60f, keepCollider: false);
            Box("Soporte", camara.transform, new Vector3(2.4f, 2.55f, -0.25f), new Vector3(0.03f, 0.2f, 0.03f), "metal", keepCollider: false);
        }
    }
}
