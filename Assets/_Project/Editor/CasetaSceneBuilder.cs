using Caso404.Decisions;
using UnityEditor;
using UnityEngine;
using static Caso404.EditorTools.SceneKit;

namespace Caso404.EditorTools
{
    /// <summary>
    /// Escena c2_s3 "Caseta de seguridad y parqueadero": caseta con monitores, termo de tinto, servidor pequeño en una esquina
    /// y parqueadero subterráneo. Inés en la caseta. La copia espejo no es un objeto: la entrega Inés (dec_c2_ines A).
    /// </summary>
    public static class CasetaSceneBuilder
    {
        public const string ScenePath = ScenesFolder + "/CasetaSeguridad.unity";

        [MenuItem("Caso404/Escenas/c2_s3 Caseta y parqueadero")]
        public static void Build()
        {
            var scene = Begin("c2", "c2_s3");

            Ambient(new Color(0.18f, 0.19f, 0.22f), new Color(0.1f, 0.11f, 0.13f), 10f, 35f);
            Sun("Luz exterior (tenue)", new Color(0.6f, 0.65f, 0.8f), 0.25f, new Vector3(40f, 140f, 0f));
            PointLight("Lámpara de la caseta", new Vector3(-2f, 2.4f, 2f), new Color(1f, 0.88f, 0.7f), 1.3f, 5f);
            PointLight("Brillo de monitores", new Vector3(-2f, 1.4f, 3.5f), new Color(0.5f, 0.8f, 1f), 0.6f, 3f);
            PointLight("Parqueadero 1", new Vector3(4f, 2.5f, 2f), new Color(0.8f, 0.9f, 1f), 1.0f, 7f);
            PointLight("Parqueadero 2", new Vector3(10f, 2.5f, 4f), new Color(0.8f, 0.9f, 1f), 1.0f, 7f);

            var env = Group("Environment");
            // Caseta: x ∈ [-4, 0], z ∈ [0, 4]
            FloorAndCeiling(env, "Caseta", new Vector2(-4, 0), new Vector2(0, 4), "floor_tile");
            Wall(env, "Caseta Oeste", new Vector3(-4, 0, 0), new Vector3(-4, 0, 4), "wall_office");
            Wall(env, "Caseta Sur", new Vector3(-4, 0, 0), new Vector3(0, 0, 0), "wall_office");
            Wall(env, "Caseta Norte", new Vector3(-4, 0, 4), new Vector3(0, 0, 4), "wall_office");
            Wall(env, "Caseta Este (ventanilla y puerta)", new Vector3(0, 0, 0), new Vector3(0, 0, 4), "wall_office", new Opening(1f, 1f), new Opening(2.9f, 1.6f, 0.9f, 1f));
            Box("Escritorio de monitores", env, new Vector3(-2f, 0.37f, 3.5f), new Vector3(2.4f, 0.74f, 0.7f), "wood");
            for (int i = 0; i < 3; i++)
                Box($"Monitor {i}", env, new Vector3(-2.8f + i * 0.8f, 1.05f, 3.75f), new Vector3(0.7f, 0.42f, 0.04f), "screen", keepCollider: false);
            Cylinder("Termo de tinto", env, new Vector3(-0.9f, 0.88f, 3.4f), new Vector3(0.09f, 0.14f, 0.09f), "red");
            Box("Servidor de la caseta (copias espejo)", env, new Vector3(-3.6f, 0.6f, 0.5f), new Vector3(0.6f, 1.2f, 0.6f), "dark");
            Box("Luces del servidor", env, new Vector3(-3.29f, 0.9f, 0.5f), new Vector3(0.01f, 0.3f, 0.3f), "screen", keepCollider: false);
            Box("Casillero de Inés", env, new Vector3(-3.75f, 0.95f, 2.4f), new Vector3(0.45f, 1.9f, 0.5f), "locker");

            // Parqueadero subterráneo: x ∈ [0, 14], z ∈ [-4, 8]
            FloorAndCeiling(env, "Parqueadero", new Vector2(0, -4), new Vector2(14, 8), "concrete", "concrete");
            Wall(env, "Parq Sur", new Vector3(0, 0, -4), new Vector3(14, 0, -4), "concrete");
            Wall(env, "Parq Norte", new Vector3(0, 0, 8), new Vector3(14, 0, 8), "concrete");
            Wall(env, "Parq Este", new Vector3(14, 0, -4), new Vector3(14, 0, 8), "concrete");
            Wall(env, "Parq Oeste (sur de la caseta)", new Vector3(0, 0, -4), new Vector3(0, 0, 0), "concrete");
            Wall(env, "Parq Oeste (norte de la caseta)", new Vector3(0, 0, 4), new Vector3(0, 0, 8), "concrete");
            for (int i = 0; i < 3; i++) Box($"Columna {i}", env, new Vector3(4f + i * 3.5f, 1.35f, 2f), new Vector3(0.5f, 2.7f, 0.5f), "concrete");
            Box("Carro", env, new Vector3(6f, 0.55f, -1.5f), new Vector3(1.8f, 0.8f, 4.2f), "car");
            Box("Moto (espacio vacío)", env, new Vector3(11f, 0.003f, 5f), new Vector3(1f, 0.005f, 2.2f), "yellow", keepCollider: false);
            for (int i = 0; i < 4; i++) Box($"Línea {i}", env, new Vector3(2f + i * 3f, 0.003f, -1.5f), new Vector3(0.1f, 0.005f, 4f), "yellow", keepCollider: false);

            var ev = Group("Evidence");
            var rondas = Pickup(ev, "ev_c2_libro_rondas", new Vector3(-1.4f, 0.8f, 3.3f), new Vector3(0.45f, 0.15f, 0.35f));
            Box("Libro de rondas", rondas.transform, new Vector3(-1.4f, 0.755f, 3.3f), new Vector3(0.36f, 0.025f, 0.26f), "paper", keepCollider: false);
            Box("Tachón de corrector", rondas.transform, new Vector3(-1.45f, 0.77f, 3.3f), new Vector3(0.1f, 0.003f, 0.04f), "paper", keepCollider: false);
            var sobre = Pickup(ev, "ev_c2_sobre", new Vector3(-3.45f, 1.25f, 2.4f), new Vector3(0.2f, 0.25f, 0.35f));
            Box("Sobre", sobre.transform, new Vector3(-3.48f, 1.25f, 2.4f), new Vector3(0.02f, 0.12f, 0.22f), "paper", keepCollider: false);
            var camara = Pickup(ev, "ev_c2_camara_parqueadero", new Vector3(1.2f, 2.35f, 7.6f), new Vector3(0.4f, 0.35f, 0.45f));
            Box("Cámara", camara.transform, new Vector3(1.2f, 2.35f, 7.7f), new Vector3(0.12f, 0.1f, 0.22f), "metal", rotY: 150f, keepCollider: false);
            Box("Soporte", camara.transform, new Vector3(1.2f, 2.47f, 7.85f), new Vector3(0.03f, 0.2f, 0.03f), "metal", keepCollider: false);

            SpawnPlayer(new Vector3(2.5f, 0.05f, 1f), -90f);
            Npc("ines", new Vector3(-2f, 0f, 2.6f), 90f, seated: true);
            Trigger<SceneCloseTrigger>("Ascensor del parqueadero (repasar el caso)", new Vector3(13.9f, 1.1f, 2f), new Vector3(0.1f, 2.2f, 1.4f), "metal", "Repasar el caso (hipótesis)");

            CityKit.Surround(new CityKit.Options
            {
                StreetY = -0.15f, Night = true, Seed = 99, Downtown = true, // base de la Torre Nexo, de noche
                HostMin = new Vector2(-4.3f, -4.3f), HostMax = new Vector2(14.3f, 8.3f),
                HostHeight = 66f, PlayFloorY = 0f, PlayCeilingY = WallHeight, HostMaterial = "fachada_torre"
            });
            Finish(scene, ScenePath);
        }
    }
}
