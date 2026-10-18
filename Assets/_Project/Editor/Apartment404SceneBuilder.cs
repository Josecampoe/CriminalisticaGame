using System.Collections.Generic;
using System.Linq;
using Caso404.AI;
using Caso404.Core;
using Caso404.Core.Events;
using Caso404.Data;
using Caso404.Decisions;
using Caso404.Dialogue;
using Caso404.Evidence;
using Caso404.Interaction;
using Caso404.Player;
using Caso404.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Caso404.EditorTools
{
    /// <summary>
    /// Arma la escena c1_s1 "Apartamento 404" en greybox (primitivas), con sus 7 evidencias, Laura, el jugador,
    /// la UI y la iluminación, y la agrega a Build Settings. Se puede volver a ejecutar: reemplaza la escena.
    /// Uso: menú Caso404/Construir escena c1_s1, o -executeMethod Caso404.EditorTools.Apartment404SceneBuilder.Build
    /// </summary>
    public static class Apartment404SceneBuilder
    {
        public const string ScenePath = "Assets/_Project/Scenes/Apartamento404.unity";
        const string MatFolder = "Assets/_Project/Art/Materials";
        const float WallHeight = 2.7f;
        const float WallThickness = 0.15f;

        static int _interactableLayer;
        static readonly Dictionary<string, Material> Materials = new();

        [MenuItem("Caso404/Construir escena c1_s1 (Apartamento 404)")]
        public static void Build()
        {
            ProjectSetup.EnsureLayer(ProjectSetup.InteractableLayer);
            ProjectSetup.EnsureLayer(ProjectSetup.InspectionLayer);
            _interactableLayer = LayerMask.NameToLayer(ProjectSetup.InteractableLayer);

            // La escena nueva se crea primero: NewScene descarga los assets sin uso y dejaría nulas las referencias cargadas antes.
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var db = AssetDatabase.LoadAssetAtPath<GameDatabase>(Case1DataBuilder.DatabasePath);
            if (db == null) { Case1DataBuilder.Build(); db = AssetDatabase.LoadAssetAtPath<GameDatabase>(Case1DataBuilder.DatabasePath); }
            SharedAssetsBuilder.Build();
            var c1 = db.FindCase("c1");
            var sceneData = c1.FindScene("c1_s1");
            var theme = AssetDatabase.LoadAssetAtPath<UITheme>(SharedAssetsBuilder.ThemePath);
            var ai = AssetDatabase.LoadAssetAtPath<AiSettings>(SharedAssetsBuilder.AiSettingsPath);
            var modal = AssetDatabase.LoadAssetAtPath<BoolEventChannel>(SharedAssetsBuilder.ModalChannelPath);
            var evChannel = AssetDatabase.LoadAssetAtPath<StringEventChannel>(SharedAssetsBuilder.EvidenceChannelPath);
            var decChannel = AssetDatabase.LoadAssetAtPath<StringEventChannel>(SharedAssetsBuilder.DecisionChannelPath);

            CreateMaterials();
            BuildLighting();

            // ---------- Sistemas ----------
            var game = new GameObject("GameContext").AddComponent<GameContext>();
            Set(game, "_database", db);
            Set(game, "_scene", sceneData);
            SetBool(game, "_startNewGame", true);
            Set(game, "_aiSettings", ai);
            Set(game, "_evidenceCollectedChannel", evChannel);
            Set(game, "_decisionMadeChannel", decChannel);

            // ---------- Espacio 3D ----------
            var env = new GameObject("Environment").transform;
            BuildApartment(env);
            BuildHallway(env);
            BuildBalcony(env);

            // ---------- Evidencias ----------
            var evRoot = new GameObject("Evidence").transform;
            BuildEvidence(evRoot, c1, game);

            // ---------- Jugador ----------
            var player = BuildPlayer(game, modal, out var input, out var context, out var interactor, out var inspection);

            // ---------- Laura ----------
            var laura = c1.FindSuspect("laura");
            BuildNpc(laura, game, new Vector3(0.6f, 0f, -1.1f), 60f); // mirando hacia la puerta del 404

            // ---------- Cierre de escena ----------
            var exit = Box("Ascensor (repasar el caso)", null, new Vector3(-3.85f, 1.1f, -1f), new Vector3(0.1f, 2.2f, 1.2f), "metal", keepCollider: true);
            exit.layer = _interactableLayer;
            exit.AddComponent<SceneCloseTrigger>();
            exit.AddComponent<Highlightable>();

            // ---------- UI ----------
            BuildUi(theme, game, input, context, interactor, inspection, modal);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Caso404] Escena {ScenePath} construida y agregada a Build Settings.");
        }

        // ================= Espacio =================

        static void BuildApartment(Transform env)
        {
            var room = new GameObject("Apartamento404").transform;
            room.SetParent(env, false);
            // Sala: x ∈ [-4, 4], z ∈ [0, 6]
            Box("Piso", room, new Vector3(0, -0.05f, 3), new Vector3(8, 0.1f, 6), "floor");
            Box("Techo", room, new Vector3(0, WallHeight + 0.05f, 3), new Vector3(8, 0.1f, 6), "wall");
            Box("Pared Oeste", room, new Vector3(-4, WallHeight / 2, 3), new Vector3(WallThickness, WallHeight, 6), "wall");
            Box("Pared Este", room, new Vector3(4, WallHeight / 2, 3), new Vector3(WallThickness, WallHeight, 6), "wall");
            // Pared sur con la puerta en x ∈ [2, 3]
            WallWithOpening(room, "Pared Sur", 0f, -4f, 4f, 2f, 3f, 0f, 2.1f);
            // Pared norte: ventana rota en x ∈ [-2, -0.2] (y 0.9–2.1) y puerta del balcón en x ∈ [0.6, 1.6]
            Box("Norte A", room, new Vector3(-3f, WallHeight / 2, 6), new Vector3(2f, WallHeight, WallThickness), "wall");
            Box("Norte bajo ventana", room, new Vector3(-1.1f, 0.45f, 6), new Vector3(1.8f, 0.9f, WallThickness), "wall");
            Box("Norte sobre ventana", room, new Vector3(-1.1f, 2.4f, 6), new Vector3(1.8f, 0.6f, WallThickness), "wall");
            Box("Norte entre", room, new Vector3(0.2f, WallHeight / 2, 6), new Vector3(0.8f, WallHeight, WallThickness), "wall");
            Box("Norte sobre puerta", room, new Vector3(1.1f, 2.4f, 6), new Vector3(1f, 0.6f, WallThickness), "wall");
            Box("Norte B", room, new Vector3(2.8f, WallHeight / 2, 6), new Vector3(2.4f, WallHeight, WallThickness), "wall");

            // Muebles (la sala está desordenada)
            var sofa = new GameObject("Sofa").transform;
            sofa.SetParent(room, false);
            Box("Base", sofa, new Vector3(-3.4f, 0.25f, 2.5f), new Vector3(0.9f, 0.5f, 2f), "fabric");
            Box("Respaldo", sofa, new Vector3(-3.8f, 0.7f, 2.5f), new Vector3(0.2f, 0.6f, 2f), "fabric");
            Box("Mesa de centro (corrida)", room, new Vector3(-1.6f, 0.22f, 2.9f), new Vector3(1.1f, 0.44f, 0.6f), "wood", rotY: 18f);
            Box("Silla volcada", room, new Vector3(-0.4f, 0.2f, 4.2f), new Vector3(0.45f, 0.45f, 0.45f), "wood", rotY: 40f, rotX: 80f);
            for (int i = 0; i < 6; i++)
                Box($"Papel {i}", room, new Vector3(-1f + i * 0.35f, 0.006f, 3.6f + (i % 2) * 0.3f), new Vector3(0.21f, 0.004f, 0.29f), "paper", rotY: i * 27f, keepCollider: false);
            Box("Estante", room, new Vector3(-3.8f, 1f, 5.2f), new Vector3(0.35f, 2f, 1.2f), "wood");
            Box("Alfombra", room, new Vector3(-1.4f, 0.003f, 3f), new Vector3(2.4f, 0.005f, 1.8f), "fabric", keepCollider: false);
        }

        static void BuildHallway(Transform env)
        {
            var hall = new GameObject("Pasillo").transform;
            hall.SetParent(env, false);
            // Pasillo: x ∈ [-4, 6], z ∈ [-2, 0]
            Box("Piso", hall, new Vector3(1, -0.05f, -1), new Vector3(10, 0.1f, 2), "floor_hall");
            Box("Techo", hall, new Vector3(1, WallHeight + 0.05f, -1), new Vector3(10, 0.1f, 2), "wall");
            Box("Pared Sur", hall, new Vector3(1, WallHeight / 2, -2), new Vector3(10, WallHeight, WallThickness), "wall_hall");
            Box("Pared Oeste", hall, new Vector3(-4, WallHeight / 2, -1), new Vector3(WallThickness, WallHeight, 2), "wall_hall");
            Box("Pared Este", hall, new Vector3(6, WallHeight / 2, -1), new Vector3(WallThickness, WallHeight, 2), "wall_hall");
            Box("Pared Norte (este)", hall, new Vector3(5f, WallHeight / 2, 0), new Vector3(2f, WallHeight, WallThickness), "wall_hall");
            Box("Puerta 403", hall, new Vector3(-1.5f, 1.05f, -1.93f), new Vector3(1f, 2.1f, 0.05f), "wood");
            Box("Número 404", hall, new Vector3(2.5f, 2.25f, -0.09f), new Vector3(0.3f, 0.12f, 0.02f), "metal", keepCollider: false);
        }

        static void BuildBalcony(Transform env)
        {
            var b = new GameObject("Balcon").transform;
            b.SetParent(env, false);
            // Balcón: x ∈ [-2.5, 2.5], z ∈ [6, 8]
            Box("Piso", b, new Vector3(0, -0.05f, 7), new Vector3(5, 0.1f, 2), "floor_balcony");
            Box("Baranda N", b, new Vector3(0, 0.5f, 8), new Vector3(5, 1f, 0.08f), "metal");
            Box("Baranda O", b, new Vector3(-2.5f, 0.5f, 7), new Vector3(0.08f, 1f, 2), "metal");
            Box("Baranda E", b, new Vector3(2.5f, 0.5f, 7), new Vector3(0.08f, 1f, 2), "metal");
            Box("Matera", b, new Vector3(2.1f, 0.2f, 7.6f), new Vector3(0.4f, 0.4f, 0.4f), "dark");
        }

        static void WallWithOpening(Transform parent, string name, float z, float xMin, float xMax, float openMin, float openMax, float yMin, float yMax)
        {
            float leftW = openMin - xMin, rightW = xMax - openMax;
            Box($"{name} izq", parent, new Vector3(xMin + leftW / 2, WallHeight / 2, z), new Vector3(leftW, WallHeight, WallThickness), "wall");
            Box($"{name} der", parent, new Vector3(openMax + rightW / 2, WallHeight / 2, z), new Vector3(rightW, WallHeight, WallThickness), "wall");
            Box($"{name} dintel", parent, new Vector3((openMin + openMax) / 2, (yMax + WallHeight) / 2, z),
                new Vector3(openMax - openMin, WallHeight - yMax, WallThickness), "wall");
        }

        // ================= Evidencias =================

        static void BuildEvidence(Transform root, CaseData c1, GameContext game)
        {
            // 1. Ventana rota (vidrios hacia el balcón)
            var ventana = Pickup(root, c1.FindEvidence("ev_c1_ventana"), game, new Vector3(-1.1f, 1.5f, 6f), new Vector3(1.8f, 1.2f, 0.25f));
            Box("Marco sup", ventana.transform, new Vector3(-1.1f, 2.08f, 6f), new Vector3(1.8f, 0.05f, 0.1f), "dark", keepCollider: false);
            Box("Marco inf", ventana.transform, new Vector3(-1.1f, 0.92f, 6f), new Vector3(1.8f, 0.05f, 0.1f), "dark", keepCollider: false);
            Box("Vidrio restante", ventana.transform, new Vector3(-1.75f, 1.75f, 6f), new Vector3(0.45f, 0.6f, 0.01f), "glass", rotZ: 12f, keepCollider: false);
            Box("Vidrio restante 2", ventana.transform, new Vector3(-0.5f, 1.15f, 6f), new Vector3(0.4f, 0.35f, 0.01f), "glass", rotZ: -20f, keepCollider: false);
            for (int i = 0; i < 9; i++)
                Box($"Vidrio en el balcón {i}", ventana.transform, new Vector3(-1.8f + i * 0.17f, 0.01f, 6.25f + (i % 3) * 0.15f),
                    new Vector3(0.08f, 0.005f, 0.06f), "glass", rotY: i * 41f, keepCollider: false);

            // 2. Cerradura de la puerta (sin marcas de forzado)
            var puerta = Pickup(root, c1.FindEvidence("ev_c1_puerta"), game, new Vector3(2.1f, 1.0f, 0f), new Vector3(0.35f, 0.4f, 0.35f));
            Box("Hoja de la puerta (entreabierta)", puerta.transform, new Vector3(2.05f, 1.05f, 0.45f), new Vector3(0.05f, 2.1f, 0.9f), "wood", keepCollider: false);
            Box("Cerradura", puerta.transform, new Vector3(2.1f, 1.0f, 0.88f), new Vector3(0.06f, 0.14f, 0.06f), "metal", keepCollider: false);
            Box("Marco", puerta.transform, new Vector3(2.02f, 1.05f, 0f), new Vector3(0.06f, 2.1f, 0.18f), "dark", keepCollider: false);
            puerta.GetComponent<BoxCollider>().center = new Vector3(0f, 0f, 0.85f);

            // 3. Huella de zapato en el balcón
            var huella = Pickup(root, c1.FindEvidence("ev_c1_huella"), game, new Vector3(-0.9f, 0.05f, 6.7f), new Vector3(0.35f, 0.15f, 0.45f));
            Box("Huella", huella.transform, new Vector3(-0.9f, 0.004f, 6.7f), new Vector3(0.11f, 0.004f, 0.29f), "mud", rotY: 15f, keepCollider: false);

            // 4. Colilla en el balcón
            var colilla = Pickup(root, c1.FindEvidence("ev_c1_colilla"), game, new Vector3(1.4f, 0.05f, 7.4f), new Vector3(0.25f, 0.12f, 0.25f));
            var butt = Cylinder("Colilla", colilla.transform, new Vector3(1.4f, 0.01f, 7.4f), new Vector3(0.012f, 0.025f, 0.012f), "paper");
            butt.transform.rotation = Quaternion.Euler(0, 30, 90);

            // 5. Teléfono bajo el sofá
            var telefono = Pickup(root, c1.FindEvidence("ev_c1_telefono"), game, new Vector3(-2.85f, 0.05f, 2.2f), new Vector3(0.3f, 0.12f, 0.3f));
            Box("Teléfono", telefono.transform, new Vector3(-2.85f, 0.006f, 2.2f), new Vector3(0.075f, 0.01f, 0.155f), "dark", rotY: 70f, keepCollider: false);

            // 6. Escritorio vacío (marca del portátil, cargador y fotos con el cuaderno rojo)
            var escritorio = Pickup(root, c1.FindEvidence("ev_c1_escritorio"), game, new Vector3(3.5f, 0.75f, 3.6f), new Vector3(0.8f, 0.3f, 1.5f));
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
            var silla = Box("Silla del escritorio", null, new Vector3(2.9f, 0.25f, 3.6f), new Vector3(0.45f, 0.5f, 0.45f), "dark");
            silla.transform.SetParent(root, true);

            // 7. Cámara del pasillo (la grabación la entrega ALEX)
            var camara = Pickup(root, c1.FindEvidence("ev_c1_camara_pasillo"), game, new Vector3(4.6f, 2.3f, -0.25f), new Vector3(0.4f, 0.35f, 0.45f));
            Box("Cuerpo", camara.transform, new Vector3(4.6f, 2.3f, -0.3f), new Vector3(0.12f, 0.1f, 0.22f), "metal", rotY: -35f, keepCollider: false);
            Box("Lente", camara.transform, new Vector3(4.53f, 2.28f, -0.4f), new Vector3(0.06f, 0.06f, 0.03f), "glass", rotY: -35f, keepCollider: false);
            Box("Soporte", camara.transform, new Vector3(4.6f, 2.42f, -0.18f), new Vector3(0.03f, 0.2f, 0.03f), "metal", keepCollider: false);
        }

        /// <summary>Raíz de una evidencia: collider en la capa Interactable, EvidencePickup y Highlightable.</summary>
        static GameObject Pickup(Transform parent, EvidenceData evidence, GameContext game, Vector3 center, Vector3 colliderSize)
        {
            var go = new GameObject($"EV {evidence.Id}");
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            go.layer = _interactableLayer;
            var col = go.AddComponent<BoxCollider>();
            col.size = colliderSize;
            var pickup = go.AddComponent<EvidencePickup>();
            Set(pickup, "_evidence", evidence);
            Set(pickup, "_game", game);
            go.AddComponent<Highlightable>();
            return go;
        }

        // ================= Jugador y NPC =================

        static GameObject BuildPlayer(GameContext game, BoolEventChannel modal, out InputReader input, out PlayerContext context,
            out PlayerInteractor interactor, out InspectionController inspection)
        {
            var player = new GameObject("Player");
            player.transform.position = new Vector3(2.5f, 0.05f, 1.2f);
            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.75f;
            cc.radius = 0.3f;
            cc.center = new Vector3(0, 0.875f, 0);
            cc.stepOffset = 0.3f;
            input = player.AddComponent<InputReader>();

            var pivot = new GameObject("CameraPivot").transform;
            pivot.SetParent(player.transform, false);
            pivot.localPosition = new Vector3(0, 1.6f, 0);
            var camGo = new GameObject("PlayerCamera");
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(pivot, false);
            var cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.05f;
            cam.fieldOfView = 65f;
            camGo.AddComponent<AudioListener>();

            var fps = player.AddComponent<FirstPersonController>();
            Set(fps, "_input", input);
            Set(fps, "_cameraPivot", pivot);
            Set(fps, "_uiModalChannel", modal);

            context = player.AddComponent<PlayerContext>();
            Set(context, "_controller", fps);
            Set(context, "_camera", cam);

            interactor = player.AddComponent<PlayerInteractor>();
            Set(interactor, "_input", input);
            Set(interactor, "_context", context);
            Set(interactor, "_camera", cam);
            // El rayo también choca con paredes (Default) para no interactuar a través de ellas.
            var so = new SerializedObject(interactor);
            so.FindProperty("_interactableMask").intValue = (1 << _interactableLayer) | (1 << 0);
            so.ApplyModifiedPropertiesWithoutUndo();

            inspection = player.AddComponent<InspectionController>();
            Set(inspection, "_player", context);
            Set(inspection, "_input", input);
            Set(inspection, "_game", game);
            return player;
        }

        static void BuildNpc(CharacterProfile profile, GameContext game, Vector3 position, float yaw)
        {
            var npc = new GameObject($"NPC {profile.DisplayName}");
            npc.transform.position = position;
            npc.transform.rotation = Quaternion.Euler(0, yaw, 0);
            npc.layer = _interactableLayer;
            var col = npc.AddComponent<CapsuleCollider>();
            col.height = 1.7f;
            col.radius = 0.3f;
            col.center = new Vector3(0, 0.85f, 0);

            var mat = GetMaterial($"chr_{profile.Id}", profile.PlaceholderColor);
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Cuerpo";
            Object.DestroyImmediate(body.GetComponent<Collider>());
            body.transform.SetParent(npc.transform, false);
            body.transform.localPosition = new Vector3(0, 0.72f, 0);
            body.transform.localScale = new Vector3(0.5f, 0.72f, 0.4f);
            body.GetComponent<Renderer>().sharedMaterial = mat;
            body.layer = _interactableLayer;

            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Cabeza";
            Object.DestroyImmediate(head.GetComponent<Collider>());
            head.transform.SetParent(npc.transform, false);
            head.transform.localPosition = new Vector3(0, 1.58f, 0);
            head.transform.localScale = Vector3.one * 0.26f;
            head.GetComponent<Renderer>().sharedMaterial = GetMaterial("skin", new Color(0.78f, 0.6f, 0.48f));
            head.layer = _interactableLayer;

            var nose = Box("Nariz (frente)", npc.transform, Vector3.zero, new Vector3(0.05f, 0.05f, 0.06f), "skin", keepCollider: false);
            nose.transform.localPosition = new Vector3(0, 1.57f, 0.13f);
            nose.layer = _interactableLayer;

            var talk = npc.AddComponent<CharacterNpc>();
            Set(talk, "_profile", profile);
            Set(talk, "_game", game);
            Set(talk, "_head", head.transform);
            npc.AddComponent<Highlightable>();
        }

        // ================= UI =================

        static void BuildUi(UITheme theme, GameContext game, InputReader input, PlayerContext context, PlayerInteractor interactor,
            InspectionController inspection, BoolEventChannel modal)
        {
            var es = new GameObject("EventSystem", typeof(EventSystem));
            es.AddComponent<InputSystemUIInputModule>();

            var canvasGo = new GameObject("UI", typeof(RectTransform));
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            var systems = new GameObject("UISystems");
            var ui = systems.AddComponent<UIManager>();
            Set(ui, "_input", input);
            Set(ui, "_modalChannel", modal);

            var hud = UiChild<HUDView>(canvasGo.transform, "HUD", panel: false);
            Set(hud, "_theme", theme);
            Set(hud, "_game", game);
            Set(hud, "_interactor", interactor);

            var inspectionView = Panel<InspectionView>(canvasGo.transform, "InspectionView", theme, ui);
            Set(inspectionView, "_inspection", inspection);
            Set(inspectionView, "_game", game);
            var journal = Panel<JournalView>(canvasGo.transform, "JournalView", theme, ui);
            Set(journal, "_game", game);
            var dialogue = Panel<DialogueView>(canvasGo.transform, "DialogueView", theme, ui);
            Set(dialogue, "_game", game);
            Set(dialogue, "_player", context);
            var alex = Panel<AlexPanelView>(canvasGo.transform, "AlexPanelView", theme, ui);
            Set(alex, "_game", game);
            var hypothesis = Panel<HypothesisView>(canvasGo.transform, "HypothesisView", theme, ui);
            Set(hypothesis, "_game", game);
            var decision = Panel<DecisionView>(canvasGo.transform, "DecisionView", theme, ui);
            Set(decision, "_game", game);
            var message = Panel<MessageView>(canvasGo.transform, "MessageView", theme, ui);

            var controller = systems.AddComponent<GameUIController>();
            Set(controller, "_game", game);
            Set(controller, "_input", input);
            Set(controller, "_player", context);
            Set(controller, "_ui", ui);
            Set(controller, "_theme", theme);
            Set(controller, "_hud", hud);
            Set(controller, "_journal", journal);
            Set(controller, "_alex", alex);
            Set(controller, "_hypothesis", hypothesis);
            Set(controller, "_decision", decision);
            Set(controller, "_dialogue", dialogue);
            Set(controller, "_message", message);
        }

        static T UiChild<T>(Transform parent, string name, bool panel) where T : Component
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            if (panel) go.AddComponent<CanvasGroup>();
            return go.AddComponent<T>();
        }

        static T Panel<T>(Transform parent, string name, UITheme theme, UIManager ui) where T : UIPanel
        {
            var view = UiChild<T>(parent, name, panel: true);
            Set(view, "_theme", theme);
            Set(view, "_ui", ui);
            return view;
        }

        // ================= Iluminación y materiales =================

        static void BuildLighting()
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.32f, 0.34f, 0.38f); // mañana gris
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.55f, 0.58f, 0.62f);
            RenderSettings.fogStartDistance = 12f;
            RenderSettings.fogEndDistance = 40f;

            var sun = new GameObject("Luz de mañana (gris)").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(0.78f, 0.82f, 0.9f);
            sun.intensity = 0.9f;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(28f, 200f, 0f);

            Point("Lámpara de la sala (cálida)", new Vector3(-0.5f, 2.4f, 3f), new Color(1f, 0.86f, 0.7f), 1.0f, 7f);
            Point("Luz del escritorio", new Vector3(3f, 2.3f, 4.5f), new Color(1f, 0.85f, 0.65f), 0.8f, 4f);
            Point("Fluorescente del pasillo", new Vector3(1f, 2.5f, -1f), new Color(0.85f, 0.92f, 1f), 1.2f, 7f);
            Point("Luz del balcón", new Vector3(0f, 2.2f, 7.2f), new Color(0.8f, 0.85f, 0.95f), 0.8f, 4f);
        }

        static void Point(string name, Vector3 pos, Color color, float intensity, float range)
        {
            var l = new GameObject(name).AddComponent<Light>();
            l.type = LightType.Point;
            l.transform.position = pos;
            l.color = color;
            l.intensity = intensity;
            l.range = range;
            l.shadows = LightShadows.None;
        }

        static void CreateMaterials()
        {
            Materials.Clear();
            GetMaterial("floor", new Color(0.42f, 0.33f, 0.26f));
            GetMaterial("floor_hall", new Color(0.36f, 0.36f, 0.34f));
            GetMaterial("floor_balcony", new Color(0.5f, 0.5f, 0.48f));
            GetMaterial("wall", new Color(0.72f, 0.7f, 0.66f));
            GetMaterial("wall_hall", new Color(0.6f, 0.62f, 0.6f));
            GetMaterial("wood", new Color(0.4f, 0.27f, 0.17f));
            GetMaterial("fabric", new Color(0.26f, 0.3f, 0.36f));
            GetMaterial("paper", new Color(0.91f, 0.89f, 0.82f));
            GetMaterial("dark", new Color(0.1f, 0.1f, 0.11f));
            GetMaterial("metal", new Color(0.55f, 0.57f, 0.6f), smooth: 0.7f, metal: 0.8f);
            GetMaterial("glass", new Color(0.7f, 0.85f, 0.9f), smooth: 0.95f);
            GetMaterial("mud", new Color(0.25f, 0.18f, 0.1f));
            GetMaterial("dust", new Color(0.55f, 0.43f, 0.3f));
            GetMaterial("red", new Color(0.75f, 0.12f, 0.1f));
            GetMaterial("skin", new Color(0.78f, 0.6f, 0.48f));
        }

        static Material GetMaterial(string key, Color color, float smooth = 0.2f, float metal = 0f)
        {
            if (Materials.TryGetValue(key, out var cached)) return cached;
            SharedAssetsBuilder.EnsureFolder(MatFolder);
            var path = $"{MatFolder}/mat_{key}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Smoothness", smooth);
            mat.SetFloat("_Metallic", metal);
            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);
            Materials[key] = mat;
            return mat;
        }

        // ================= Ayudantes =================

        static GameObject Box(string name, Transform parent, Vector3 worldPos, Vector3 scale, string material,
            float rotY = 0f, float rotX = 0f, float rotZ = 0f, bool keepCollider = true)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            if (parent != null) go.transform.SetParent(parent, true);
            go.transform.position = worldPos;
            go.transform.rotation = Quaternion.Euler(rotX, rotY, rotZ);
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = Materials[material];
            if (!keepCollider) Object.DestroyImmediate(go.GetComponent<Collider>());
            if (parent != null) go.layer = parent.gameObject.layer;
            GameObjectUtility.SetStaticEditorFlags(go, keepCollider && parent != null && parent.gameObject.layer == 0
                ? StaticEditorFlags.BatchingStatic | StaticEditorFlags.ContributeGI | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic
                : 0);
            return go;
        }

        static GameObject Cylinder(string name, Transform parent, Vector3 worldPos, Vector3 scale, string material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent, true);
            go.transform.position = worldPos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = Materials[material];
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.layer = parent.gameObject.layer;
            return go;
        }

        static void Set(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop == null) { Debug.LogError($"[Caso404] {target.GetType().Name} no tiene el campo {field}"); return; }
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetBool(Object target, string field, bool value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).boolValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void AddToBuildSettings(string path)
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.path != path && s.path != "Assets/Scenes/SampleScene.unity").ToList();
            scenes.Insert(0, new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
