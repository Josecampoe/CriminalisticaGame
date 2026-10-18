using Caso404.Decisions;
using UnityEditor;
using UnityEngine;
using static Caso404.EditorTools.SceneKit;

namespace Caso404.EditorTools
{
    /// <summary>
    /// Escena c1_s3 "Portería y sótano": caseta de portería con libro de registro, pasillo de sótano con casilleros,
    /// cuarto de administración (DVR), cuarto de mantenimiento (llavero) y parqueadero con contenedor.
    /// El DVR desbloquea la grabación original (ERROR 404 → archivo encontrado). El overol aparece solo si Carlos
    /// no fue detenido; si lo fue, aparecen las cenizas.
    /// </summary>
    public static class PorteriaSceneBuilder
    {
        public const string ScenePath = ScenesFolder + "/PorteriaSotano.unity";

        [MenuItem("Caso404/Escenas/c1_s3 Portería y sótano")]
        public static void Build()
        {
            var scene = Begin("c1", "c1_s3");

            Ambient(new Color(0.22f, 0.23f, 0.25f), new Color(0.15f, 0.16f, 0.17f), 10f, 35f);
            Sun("Luz exterior (tenue)", new Color(0.7f, 0.75f, 0.85f), 0.3f, new Vector3(40f, 120f, 0f));
            PointLight("Lámpara de la portería", new Vector3(-3f, 2.4f, 3f), new Color(1f, 0.85f, 0.65f), 1.4f, 7f);
            PointLight("Fluorescente del sótano 1", new Vector3(3f, 2.5f, 3f), new Color(0.85f, 0.95f, 1f), 1.1f, 5f);
            PointLight("Fluorescente del sótano 2", new Vector3(9f, 2.5f, 3f), new Color(0.85f, 0.95f, 1f), 1.1f, 5f);
            PointLight("Bombillo de administración", new Vector3(2f, 2.4f, 6f), new Color(1f, 0.85f, 0.6f), 1.0f, 4f);
            PointLight("Bombillo de mantenimiento", new Vector3(6.5f, 2.4f, 6f), new Color(1f, 0.85f, 0.6f), 1.2f, 5f);
            PointLight("Parqueadero 1", new Vector3(15f, 2.5f, 1f), new Color(0.8f, 0.9f, 1f), 1.0f, 7f);
            PointLight("Parqueadero 2", new Vector3(19f, 2.5f, 6f), new Color(0.8f, 0.9f, 1f), 1.0f, 7f);

            var env = Group("Environment");
            // Portería (lobby): x ∈ [-6, 0], z ∈ [0, 6]
            FloorAndCeiling(env, "Portería", new Vector2(-6, 0), new Vector2(0, 6), "floor_tile");
            Wall(env, "Portería Oeste", new Vector3(-6, 0, 0), new Vector3(-6, 0, 6), "wall");
            Wall(env, "Portería Sur", new Vector3(-6, 0, 0), new Vector3(0, 0, 0), "wall");
            Wall(env, "Portería Norte", new Vector3(-6, 0, 6), new Vector3(0, 0, 6), "wall");
            Wall(env, "Portería Este", new Vector3(0, 0, 0), new Vector3(0, 0, 6), "wall", new Opening(3f, 1.4f));
            Box("Puerta de la calle", env, new Vector3(-3f, 1.1f, 0.05f), new Vector3(1.4f, 2.2f, 0.06f), "glass", keepCollider: false);
            // Caseta
            Box("Caseta mostrador", env, new Vector3(-4.6f, 0.55f, 4.2f), new Vector3(1.6f, 1.1f, 0.5f), "wood");
            Box("Caseta lateral", env, new Vector3(-3.85f, 0.55f, 5.1f), new Vector3(0.1f, 1.1f, 1.3f), "wood");
            Box("Silla de Hernán", env, new Vector3(-5.2f, 0.25f, 5.2f), new Vector3(0.45f, 0.5f, 0.45f), "dark");
            Box("Casillero de correo", env, new Vector3(-5.9f, 1.3f, 2.5f), new Vector3(0.2f, 1.2f, 1.6f), "metal");

            // Pasillo del sótano: x ∈ [0, 12], z ∈ [2, 4]
            FloorAndCeiling(env, "Sótano", new Vector2(0, 2), new Vector2(12, 4), "concrete");
            Wall(env, "Sótano Sur (a)", new Vector3(0, 0, 2), new Vector3(12, 0, 2), "wall_hall");
            Wall(env, "Sótano Norte", new Vector3(0, 0, 4), new Vector3(12, 0, 4), "wall_hall", new Opening(2f, 1f), new Opening(6.5f, 1f));
            // Casilleros del personal
            for (int i = 0; i < 6; i++)
                if (i != 4) Box($"Casillero {i}", env, new Vector3(6.8f + i * 0.55f, 0.95f, 2.3f), new Vector3(0.5f, 1.9f, 0.45f), "locker");
            Box("Casillero de Andrés (abierto) fondo", env, new Vector3(9.0f, 0.95f, 2.12f), new Vector3(0.5f, 1.9f, 0.08f), "locker");
            Box("Puerta del casillero de Andrés", env, new Vector3(8.7f, 0.95f, 2.75f), new Vector3(0.04f, 1.9f, 0.5f), "locker", rotY: 25f, keepCollider: false);

            // Administración (DVR): x ∈ [0, 4], z ∈ [4, 8]
            FloorAndCeiling(env, "Administración", new Vector2(0, 4), new Vector2(4, 8), "floor_hall");
            Wall(env, "Admin Oeste", new Vector3(0, 0, 4), new Vector3(0, 0, 8), "wall");
            Wall(env, "Admin Norte", new Vector3(0, 0, 8), new Vector3(4, 0, 8), "wall");
            Wall(env, "Admin/Mantenimiento", new Vector3(4, 0, 4), new Vector3(4, 0, 8), "wall");
            Box("Estantería", env, new Vector3(1.2f, 0.9f, 7.7f), new Vector3(2f, 1.8f, 0.4f), "metal");
            Box("Escritorio de administración", env, new Vector3(3.2f, 0.37f, 6f), new Vector3(0.7f, 0.74f, 1.4f), "wood");

            // Mantenimiento (llavero): x ∈ [4, 9], z ∈ [4, 8]
            FloorAndCeiling(env, "Mantenimiento", new Vector2(4, 4), new Vector2(9, 8), "concrete");
            Wall(env, "Mant Norte", new Vector3(4, 0, 8), new Vector3(9, 0, 8), "wall_hall");
            Wall(env, "Mant Este", new Vector3(9, 0, 4), new Vector3(9, 0, 8), "wall_hall");
            Box("Banco de trabajo", env, new Vector3(8.4f, 0.45f, 6f), new Vector3(0.8f, 0.9f, 2.4f), "wood");
            Box("Caja de herramientas", env, new Vector3(8.4f, 1.0f, 5.5f), new Vector3(0.5f, 0.2f, 0.3f), "red");

            // Parqueadero subterráneo: x ∈ [12, 22], z ∈ [-3, 9]
            FloorAndCeiling(env, "Parqueadero", new Vector2(12, -3), new Vector2(22, 9), "concrete", "concrete");
            Wall(env, "Parq Oeste", new Vector3(12, 0, -3), new Vector3(12, 0, 9), "concrete", new Opening(6f, 1.6f));
            Wall(env, "Parq Sur", new Vector3(12, 0, -3), new Vector3(22, 0, -3), "concrete");
            Wall(env, "Parq Norte", new Vector3(12, 0, 9), new Vector3(22, 0, 9), "concrete");
            Wall(env, "Parq Este", new Vector3(22, 0, -3), new Vector3(22, 0, 9), "concrete");
            for (int i = 0; i < 3; i++) Box($"Columna {i}", env, new Vector3(16f + i * 3f, 1.35f, 3f), new Vector3(0.5f, 2.7f, 0.5f), "concrete");
            Car(env, new Vector3(14.5f, 0, -1f));
            Car(env, new Vector3(20f, 0, 0.5f));
            for (int i = 0; i < 4; i++) Box($"Línea {i}", env, new Vector3(13.2f + i * 2.8f, 0.003f, -0.5f), new Vector3(0.1f, 0.005f, 4f), "yellow", keepCollider: false);
            Box("Contenedor", env, new Vector3(18f, 0.7f, 7.6f), new Vector3(2.6f, 1.4f, 1.4f), "container");

            var ev = Group("Evidence");
            // Libro de registro de la portería
            var registro = Pickup(ev, "ev_c1_registro_porteria", new Vector3(-4.6f, 1.15f, 4.15f), new Vector3(0.5f, 0.15f, 0.4f));
            Box("Libro abierto", registro.transform, new Vector3(-4.6f, 1.115f, 4.15f), new Vector3(0.42f, 0.03f, 0.3f), "paper", keepCollider: false);
            Box("Esfero", registro.transform, new Vector3(-4.35f, 1.135f, 4.05f), new Vector3(0.14f, 0.012f, 0.012f), "dark", rotY: 30f, keepCollider: false);
            // Recibo en la chaqueta de Hernán (colgada en su silla)
            var chaqueta = Pickup(ev, "ev_c1_recibo_tienda", new Vector3(-5.2f, 0.75f, 5.42f), new Vector3(0.55f, 0.6f, 0.25f));
            Box("Chaqueta", chaqueta.transform, new Vector3(-5.2f, 0.75f, 5.42f), new Vector3(0.48f, 0.55f, 0.08f), "fabric", keepCollider: false);
            Box("Recibo asomando", chaqueta.transform, new Vector3(-5.05f, 0.9f, 5.37f), new Vector3(0.05f, 0.09f, 0.005f), "paper", keepCollider: false);
            // DVR en la estantería de administración (desbloquea la grabación original)
            var dvr = Pickup(ev, "ev_c1_dvr", new Vector3(1.2f, 1.2f, 7.4f), new Vector3(0.6f, 0.25f, 0.45f));
            Box("DVR", dvr.transform, new Vector3(1.2f, 1.17f, 7.45f), new Vector3(0.43f, 0.08f, 0.32f), "dark", keepCollider: false);
            Box("Luz del DVR", dvr.transform, new Vector3(1.05f, 1.18f, 7.285f), new Vector3(0.02f, 0.02f, 0.005f), "red", keepCollider: false);
            Box("Monitor de cámaras", dvr.transform, new Vector3(3.2f, 0.98f, 6f), new Vector3(0.06f, 0.4f, 0.6f), "dark", keepCollider: false);
            // Registro del llavero maestro (tablero de llaves en mantenimiento)
            var llavero = Pickup(ev, "ev_c1_llavero", new Vector3(6f, 1.45f, 7.85f), new Vector3(1f, 0.8f, 0.25f));
            Box("Tablero de llaves", llavero.transform, new Vector3(6f, 1.55f, 7.9f), new Vector3(0.8f, 0.5f, 0.03f), "cork", keepCollider: false);
            for (int i = 0; i < 6; i++) Box($"Llave {i}", llavero.transform, new Vector3(5.7f + i * 0.12f, 1.6f, 7.87f), new Vector3(0.02f, 0.07f, 0.01f), "metal", keepCollider: false);
            Box("Hoja de control", llavero.transform, new Vector3(6f, 1.2f, 7.88f), new Vector3(0.21f, 0.29f, 0.005f), "paper", keepCollider: false);
            // Overol gris en el casillero de Andrés (solo si Carlos no fue detenido)
            var overol = Pickup(ev, "ev_c1_overol", new Vector3(9.0f, 1.2f, 2.4f), new Vector3(0.45f, 1.1f, 0.35f));
            Box("Overol (torso)", overol.transform, new Vector3(9.0f, 1.35f, 2.3f), new Vector3(0.4f, 0.6f, 0.12f), "overall", keepCollider: false);
            Box("Overol (piernas)", overol.transform, new Vector3(9.0f, 0.8f, 2.3f), new Vector3(0.36f, 0.55f, 0.1f), "overall", keepCollider: false);
            Box("Manga", overol.transform, new Vector3(9.25f, 1.25f, 2.3f), new Vector3(0.1f, 0.45f, 0.1f), "overall", keepCollider: false);
            Box("Vidrios en la manga (detalle oculto)", overol.transform, new Vector3(9.25f, 1.15f, 2.24f), new Vector3(0.04f, 0.04f, 0.01f), "glass", keepCollider: false);
            // Caneca con cenizas (solo si Carlos fue detenido: Andrés quemó el overol)
            var cenizas = Pickup(ev, "ev_c1_cenizas", new Vector3(10.4f, 0.35f, 2.45f), new Vector3(0.5f, 0.7f, 0.5f));
            Cylinder("Caneca", cenizas.transform, new Vector3(10.4f, 0.3f, 2.45f), new Vector3(0.4f, 0.3f, 0.4f), "metal");
            Cylinder("Cenizas", cenizas.transform, new Vector3(10.4f, 0.58f, 2.45f), new Vector3(0.36f, 0.02f, 0.36f), "ash");
            // Portátil de Elena en el contenedor del parqueadero
            var portatil = Pickup(ev, "ev_c1_portatil", new Vector3(18.3f, 1.45f, 7.3f), new Vector3(0.5f, 0.2f, 0.45f));
            Box("Portátil", portatil.transform, new Vector3(18.3f, 1.415f, 7.3f), new Vector3(0.34f, 0.025f, 0.24f), "dark", rotY: 20f, keepCollider: false);

            SpawnPlayer(new Vector3(-3.8f, 0.05f, 1f), 30f);
            Npc("hernan", new Vector3(-2.6f, 0f, 4.6f), 200f);
            Npc("andres", new Vector3(7.2f, 0f, 6.6f), 180f);
            Trigger<SceneCloseTrigger>("Puerta de la calle (repasar el caso)", new Vector3(-3f, 1.1f, 0.12f), new Vector3(1.4f, 2.2f, 0.06f), "glass", "Repasar el caso (hipótesis)");

            Finish(scene, ScenePath);
        }

        static void Car(Transform parent, Vector3 floorPos)
        {
            Box("Carro", parent, floorPos + new Vector3(0, 0.55f, 0), new Vector3(1.8f, 0.8f, 4.2f), "car");
            Box("Cabina", parent, floorPos + new Vector3(0, 1.15f, -0.2f), new Vector3(1.6f, 0.5f, 2.2f), "screen_off", keepCollider: false);
        }
    }
}
