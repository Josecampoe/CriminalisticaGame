using Caso404.Decisions;
using UnityEditor;
using UnityEngine;
using static Caso404.EditorTools.SceneKit;

namespace Caso404.EditorTools
{
    /// <summary>
    /// Escena c2_s2 "Piso ejecutivo": oficinas de vidrio de Tomás, Valeria y Sebastián, ciudad de noche al fondo,
    /// pizarras con diagramas. 6 evidencias; Valeria en su oficina y Sebastián en su escritorio (si no fue detenido).
    /// </summary>
    public static class PisoEjecutivoSceneBuilder
    {
        public const string ScenePath = ScenesFolder + "/PisoEjecutivo.unity";

        [MenuItem("Caso404/Escenas/c2_s2 Piso ejecutivo")]
        public static void Build()
        {
            var scene = Begin("c2", "c2_s2");

            Ambient(new Color(0.16f, 0.17f, 0.22f), new Color(0.06f, 0.07f, 0.12f), 12f, 40f);
            Sun("Luz de la ciudad (noche)", new Color(0.4f, 0.45f, 0.65f), 0.3f, new Vector3(20f, 180f, 0f));
            PointLight("Oficina de Tomás", new Vector3(-5.5f, 2.4f, 8f), new Color(1f, 0.88f, 0.7f), 1.1f, 5f);
            PointLight("Oficina de Valeria", new Vector3(0f, 2.4f, 8f), new Color(1f, 0.92f, 0.8f), 1.2f, 5f);
            PointLight("Oficina de Sebastián", new Vector3(5.3f, 2.4f, 8f), new Color(0.9f, 0.95f, 1f), 1.0f, 5f);
            PointLight("Pasillo", new Vector3(0f, 2.5f, 3f), new Color(0.9f, 0.92f, 1f), 1.2f, 8f);

            var env = Group("Environment");
            // Planta: x ∈ [-8, 8], z ∈ [0, 10]. Oficinas al norte (z 6–10) con frente de vidrio.
            FloorAndCeiling(env, "Piso 12", new Vector2(-8, 0), new Vector2(8, 10), "floor_hall");
            Wall(env, "Sur (ascensores)", new Vector3(-8, 0, 0), new Vector3(8, 0, 0), "wall_office");
            Wall(env, "Oeste", new Vector3(-8, 0, 0), new Vector3(-8, 0, 10), "wall_office");
            Wall(env, "Este", new Vector3(8, 0, 0), new Vector3(8, 0, 10), "wall_office");
            // Ventanal de piso a techo hacia la ciudad de noche (vano con antepecho bajo y límite invisible)
            Wall(env, "Norte (ventanal)", new Vector3(-8, 0, 10), new Vector3(8, 0, 10), "dark", new Opening(8f, 15.4f, 2.3f, 0.3f));
            Box("Vidrio del ventanal", env, new Vector3(0, 1.45f, 10f), new Vector3(15.4f, 2.3f, 0.02f), "glass", keepCollider: false);
            var limit = new GameObject("Límite invisible del ventanal");
            limit.transform.SetParent(env, false);
            limit.transform.position = new Vector3(0, 1.5f, 10f);
            limit.AddComponent<BoxCollider>().size = new Vector3(16f, 3f, 0.2f);
            // Frentes de vidrio con puertas y divisiones
            Wall(env, "Vidrio frente", new Vector3(-8, 0, 6), new Vector3(8, 0, 6), "glass", new Opening(3.5f, 1f), new Opening(8.5f, 1f), new Opening(13.5f, 1f));
            Wall(env, "División Tomás/Valeria", new Vector3(-3, 0, 6), new Vector3(-3, 0, 10), "glass");
            Wall(env, "División Valeria/Sebastián", new Vector3(2.5f, 0, 6), new Vector3(2.5f, 0, 10), "glass");
            Box("Pizarra con diagramas", env, new Vector3(-7.9f, 1.5f, 8f), new Vector3(0.04f, 1f, 1.8f), "paper");
            Box("Escritorio de Tomás", env, new Vector3(-5.5f, 0.37f, 8.4f), new Vector3(1.6f, 0.74f, 0.8f), "wood");
            Box("Escritorio de Valeria", env, new Vector3(0f, 0.37f, 8.4f), new Vector3(1.8f, 0.74f, 0.8f), "dark");
            Box("Escritorio de Sebastián", env, new Vector3(5.3f, 0.37f, 8.4f), new Vector3(1.4f, 0.74f, 0.7f), "wood");
            Box("Sofá de espera", env, new Vector3(-5f, 0.25f, 1f), new Vector3(2f, 0.5f, 0.8f), "fabric");
            Box("Puertas del ascensor", env, new Vector3(3f, 1.1f, 0.09f), new Vector3(1.4f, 2.2f, 0.04f), "metal", keepCollider: false);

            var ev = Group("Evidence");
            // Oficina de Tomás: boceto, post-it y portátil
            var boceto = Pickup(ev, "ev_c2_boceto", new Vector3(-7.85f, 1.55f, 7.0f), new Vector3(0.2f, 0.6f, 0.6f));
            Box("Marco", boceto.transform, new Vector3(-7.92f, 1.55f, 7.0f), new Vector3(0.03f, 0.5f, 0.42f), "dark", keepCollider: false);
            Box("Boceto LUCERO", boceto.transform, new Vector3(-7.9f, 1.55f, 7.0f), new Vector3(0.01f, 0.44f, 0.36f), "paper", keepCollider: false);
            var monitor = Box("Monitor de Tomás", env, new Vector3(-5.9f, 1.0f, 8.65f), new Vector3(0.6f, 0.38f, 0.04f), "screen_off", keepCollider: false);
            var postit = Pickup(ev, "ev_c2_postit", new Vector3(-5.65f, 1.15f, 8.6f), new Vector3(0.2f, 0.2f, 0.15f));
            Box("Post-it", postit.transform, new Vector3(-5.65f, 1.15f, 8.625f), new Vector3(0.08f, 0.08f, 0.003f), "yellow", keepCollider: false);
            var portatil = Pickup(ev, "ev_c2_portatil_tomas", new Vector3(-5.1f, 0.8f, 8.3f), new Vector3(0.45f, 0.15f, 0.4f));
            Box("Portátil", portatil.transform, new Vector3(-5.1f, 0.755f, 8.3f), new Vector3(0.34f, 0.02f, 0.24f), "metal", keepCollider: false);
            Box("Pantalla", portatil.transform, new Vector3(-5.1f, 0.87f, 8.42f), new Vector3(0.34f, 0.22f, 0.01f), "screen_off", rotX: -15f, keepCollider: false);
            // Oficina de Valeria: consola
            var consola = Pickup(ev, "ev_c2_consola_valeria", new Vector3(0.3f, 0.95f, 8.5f), new Vector3(0.7f, 0.45f, 0.4f));
            Box("Consola", consola.transform, new Vector3(0.3f, 1.0f, 8.6f), new Vector3(0.6f, 0.36f, 0.04f), "screen", keepCollider: false);
            Box("Teclado", consola.transform, new Vector3(0.3f, 0.75f, 8.3f), new Vector3(0.45f, 0.02f, 0.15f), "dark", keepCollider: false);
            // Escritorio de Sebastián: teléfono olvidado
            var chat = Pickup(ev, "ev_c2_chat_kairos", new Vector3(5.6f, 0.8f, 8.25f), new Vector3(0.3f, 0.12f, 0.3f));
            Box("Teléfono", chat.transform, new Vector3(5.6f, 0.75f, 8.25f), new Vector3(0.075f, 0.01f, 0.155f), "dark", rotY: 35f, keepCollider: false);
            // Registro de accesos del piso 12: panel junto al ascensor
            var tarjeta = Pickup(ev, "ev_c2_tarjeta_valeria", new Vector3(4.2f, 1.3f, 0.15f), new Vector3(0.35f, 0.4f, 0.25f));
            Box("Lector de accesos", tarjeta.transform, new Vector3(4.2f, 1.3f, 0.09f), new Vector3(0.18f, 0.26f, 0.04f), "dark", keepCollider: false);
            Box("Pantalla de accesos", tarjeta.transform, new Vector3(4.2f, 1.36f, 0.065f), new Vector3(0.13f, 0.08f, 0.01f), "screen", keepCollider: false);

            SpawnPlayer(new Vector3(3f, 0.05f, 1.2f), 0f);
            Npc("valeria", new Vector3(-0.8f, 0f, 7.6f), 180f);
            Npc("sebastian", new Vector3(5.3f, 0f, 9.1f), 180f, seated: true);
            Trigger<SceneCloseTrigger>("Ascensor (repasar el caso)", new Vector3(-1f, 1.1f, 0.12f), new Vector3(1.4f, 2.2f, 0.06f), "metal", "Repasar el caso (hipótesis)");

            CityKit.Surround(new CityKit.Options
            {
                StreetY = -36f, Night = true, Seed = 1212, Downtown = true, // piso 12 de la Torre Nexo, de noche
                HostMin = new Vector2(-8.3f, -3f), HostMax = new Vector2(8.3f, 10.15f), GridCenter = new Vector2(0f, -3f),
                HostHeight = 66f, PlayFloorY = 0f, PlayCeilingY = WallHeight, HostMaterial = "fachada_torre"
            });
            Finish(scene, ScenePath);
        }
    }
}
