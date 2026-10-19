using System.Collections.Generic;
using System.IO;
using System.Linq;
using Caso404.AI;
using Caso404.Characters;
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
    /// Kit para armar escenas jugables en greybox desde scripts de Editor: materiales, cajas, cuartos con puertas,
    /// evidencias, personajes, jugador, UI e iluminación. Cada escena tiene su builder que usa este kit.
    /// </summary>
    public static class SceneKit
    {
        public const string ScenesFolder = "Assets/_Project/Scenes";
        const string MatFolder = "Assets/_Project/Art/Materials";
        public const float WallHeight = 2.7f;
        public const float WallThickness = 0.15f;

        /// <summary>Orden de Build Settings (el menú primero).</summary>
        public static readonly string[] BuildOrder =
        {
            ScenesFolder + "/MainMenu.unity",
            ScenesFolder + "/Apartamento404.unity",
            ScenesFolder + "/CafeLaEsquina.unity",
            ScenesFolder + "/PorteriaSotano.unity",
            ScenesFolder + "/Comisaria.unity",
            ScenesFolder + "/ServidoresB.unity",
            ScenesFolder + "/PisoEjecutivo.unity",
            ScenesFolder + "/CasetaSeguridad.unity",
        };

        public static GameDatabase Db { get; private set; }
        public static CaseData Case { get; private set; }
        public static SceneData Scene { get; private set; }
        public static GameContext Game { get; private set; }
        public static UITheme Theme { get; private set; }
        public static int InteractableLayer { get; private set; }

        static readonly Dictionary<string, Material> Materials = new();
        static InputReader _input;
        static PlayerContext _context;
        static PlayerInteractor _interactor;
        static InspectionController _inspection;
        static BoolEventChannel _modal;

        // ================= Inicio y fin =================

        /// <summary>Crea una escena vacía y carga los datos. Primero la escena: NewScene descarga assets sin uso.</summary>
        public static UnityEngine.SceneManagement.Scene BeginEmpty()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            ProjectSetup.EnsureLayer(ProjectSetup.InteractableLayer);
            ProjectSetup.EnsureLayer(ProjectSetup.InspectionLayer);
            InteractableLayer = LayerMask.NameToLayer(ProjectSetup.InteractableLayer);
            Db = AssetDatabase.LoadAssetAtPath<GameDatabase>(Case1DataBuilder.DatabasePath);
            if (Db == null) { Case1DataBuilder.Build(); Db = AssetDatabase.LoadAssetAtPath<GameDatabase>(Case1DataBuilder.DatabasePath); }
            SharedAssetsBuilder.Build();
            Theme = AssetDatabase.LoadAssetAtPath<UITheme>(SharedAssetsBuilder.ThemePath);
            _modal = AssetDatabase.LoadAssetAtPath<BoolEventChannel>(SharedAssetsBuilder.ModalChannelPath);
            CreateMaterials();
            return scene;
        }

        /// <summary>Crea una escena jugable de un caso con su GameContext.</summary>
        public static UnityEngine.SceneManagement.Scene Begin(string caseId, string sceneId)
        {
            var scene = BeginEmpty();
            Case = Db.FindCase(caseId);
            Scene = Case.FindScene(sceneId);
            Game = new GameObject("GameContext").AddComponent<GameContext>();
            Set(Game, "_database", Db);
            Set(Game, "_scene", Scene);
            SetBool(Game, "_startNewGame", false); // el estado llega por el guardado (menú → Nueva partida / Continuar)
            Set(Game, "_aiSettings", AssetDatabase.LoadAssetAtPath<AiSettings>(SharedAssetsBuilder.AiSettingsPath));
            Set(Game, "_evidenceCollectedChannel", AssetDatabase.LoadAssetAtPath<StringEventChannel>(SharedAssetsBuilder.EvidenceChannelPath));
            Set(Game, "_decisionMadeChannel", AssetDatabase.LoadAssetAtPath<StringEventChannel>(SharedAssetsBuilder.DecisionChannelPath));
            return scene;
        }

        /// <summary>Arma la UI, guarda la escena y actualiza Build Settings.</summary>
        public static void Finish(UnityEngine.SceneManagement.Scene scene, string path)
        {
            BuildGameUi();
            EditorSceneManager.SaveScene(scene, path);
            RefreshBuildSettings();
            AssetDatabase.SaveAssets();
            Debug.Log($"[Caso404] Escena {path} construida.");
        }

        /// <summary>Pone en Build Settings las escenas del juego que existan, en orden.</summary>
        public static void RefreshBuildSettings()
        {
            var list = BuildOrder.Where(File.Exists).Select(p => new EditorBuildSettingsScene(p, true)).ToArray();
            EditorBuildSettings.scenes = list;
        }

        // ================= Geometría =================

        public static Transform Group(string name, Transform parent = null)
        {
            var t = new GameObject(name).transform;
            if (parent != null) t.SetParent(parent, false);
            return t;
        }

        public static GameObject Box(string name, Transform parent, Vector3 worldPos, Vector3 scale, string material,
            float rotY = 0f, float rotX = 0f, float rotZ = 0f, bool keepCollider = true)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            if (parent != null) go.transform.SetParent(parent, true);
            go.transform.position = worldPos;
            go.transform.rotation = Quaternion.Euler(rotX, rotY, rotZ);
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = Mat(material);
            if (!keepCollider) Object.DestroyImmediate(go.GetComponent<Collider>());
            if (parent != null) go.layer = parent.gameObject.layer;
            bool isStatic = keepCollider && go.layer == 0;
            GameObjectUtility.SetStaticEditorFlags(go, isStatic
                ? StaticEditorFlags.BatchingStatic | StaticEditorFlags.ContributeGI | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic
                : 0);
            return go;
        }

        public static GameObject Cylinder(string name, Transform parent, Vector3 worldPos, Vector3 scale, string material, bool keepCollider = false)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            if (parent != null) go.transform.SetParent(parent, true);
            go.transform.position = worldPos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = Mat(material);
            if (!keepCollider) Object.DestroyImmediate(go.GetComponent<Collider>());
            if (parent != null) go.layer = parent.gameObject.layer;
            return go;
        }

        /// <summary>Hueco en una pared: posición a lo largo de la pared (desde su inicio), ancho y alto.</summary>
        public readonly struct Opening
        {
            public readonly float Center, Width, Height, Bottom;
            public Opening(float center, float width, float height = 2.1f, float bottom = 0f) { Center = center; Width = width; Height = height; Bottom = bottom; }
        }

        /// <summary>Pared recta entre dos puntos del piso (alineada a X o a Z), con huecos de puertas o ventanas.</summary>
        public static void Wall(Transform parent, string name, Vector3 from, Vector3 to, string material, params Opening[] openings)
        {
            var dir = to - from;
            float length = dir.magnitude;
            var axis = dir.normalized;
            bool alongX = Mathf.Abs(axis.x) > 0.5f;
            Vector3 Size(float len, float h) => alongX ? new Vector3(len, h, WallThickness) : new Vector3(WallThickness, h, len);
            float cursor = 0f;
            int i = 0;
            foreach (var o in openings.OrderBy(o => o.Center))
            {
                float start = o.Center - o.Width / 2f, end = o.Center + o.Width / 2f;
                if (start > cursor) Box($"{name} {i++}", parent, from + axis * ((cursor + start) / 2f) + Vector3.up * WallHeight / 2f, Size(start - cursor, WallHeight), material);
                float top = o.Bottom + o.Height;
                if (top < WallHeight) Box($"{name} dintel {i++}", parent, from + axis * o.Center + Vector3.up * (top + WallHeight) / 2f, Size(o.Width, WallHeight - top), material);
                if (o.Bottom > 0f) Box($"{name} antepecho {i++}", parent, from + axis * o.Center + Vector3.up * o.Bottom / 2f, Size(o.Width, o.Bottom), material);
                cursor = end;
            }
            if (cursor < length) Box($"{name} {i}", parent, from + axis * ((cursor + length) / 2f) + Vector3.up * WallHeight / 2f, Size(length - cursor, WallHeight), material);
        }

        /// <summary>Piso y techo rectangulares entre min y max (x, z).</summary>
        public static void FloorAndCeiling(Transform parent, string name, Vector2 min, Vector2 max, string floorMat, string ceilingMat = "wall")
        {
            var center = new Vector3((min.x + max.x) / 2f, 0f, (min.y + max.y) / 2f);
            var size = new Vector3(max.x - min.x, 0.1f, max.y - min.y);
            Box($"{name} piso", parent, center + Vector3.down * 0.05f, size, floorMat);
            if (ceilingMat != null) Box($"{name} techo", parent, center + Vector3.up * (WallHeight + 0.05f), size, ceilingMat);
        }

        // ================= Interactivos =================

        /// <summary>Raíz de una evidencia: collider en la capa Interactable, EvidencePickup y Highlightable.</summary>
        public static GameObject Pickup(Transform parent, string evidenceId, Vector3 center, Vector3 colliderSize)
        {
            var evidence = Case.FindEvidence(evidenceId);
            if (evidence == null) Debug.LogError($"[Caso404] No existe la evidencia {evidenceId}");
            var go = new GameObject($"EV {evidenceId}");
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            go.layer = InteractableLayer;
            go.AddComponent<BoxCollider>().size = colliderSize;
            var pickup = go.AddComponent<EvidencePickup>();
            Set(pickup, "_evidence", evidence);
            Set(pickup, "_game", Game);
            go.AddComponent<Highlightable>();
            return go;
        }

        /// <summary>Objeto que emite una intención del jugador (tablero, acusación, ALEX, salida).</summary>
        public static GameObject Trigger<T>(string name, Vector3 pos, Vector3 size, string material, string prompt) where T : Component
        {
            var go = Box(name, null, pos, size, material);
            go.layer = InteractableLayer;
            GameObjectUtility.SetStaticEditorFlags(go, 0);
            var t = go.AddComponent<T>();
            var so = new SerializedObject(t);
            var p = so.FindProperty("_prompt");
            if (p != null) { p.stringValue = prompt; so.ApplyModifiedPropertiesWithoutUndo(); }
            go.AddComponent<Highlightable>();
            return go;
        }

        /// <summary>
        /// Personaje con su modelo 3D (prefab de CharacterArtBuilder), collider para hablarle, CharacterNpc y resaltado.
        /// Si está sentado se crea también su silla. Si el prefab no existe, usa el muñeco de primitivas de antes.
        /// </summary>
        public static GameObject Npc(string characterId, Vector3 position, float yaw, bool seated = false, string caseId = null, string chairMaterial = "dark", bool withChair = true)
        {
            var profile = (caseId == null ? Case : Db.FindCase(caseId)).FindCharacter(characterId);
            if (profile == null) { Debug.LogError($"[Caso404] No existe el personaje {characterId}"); return null; }
            var npc = new GameObject($"NPC {profile.DisplayName}");
            npc.transform.position = position;
            npc.transform.rotation = Quaternion.Euler(0, yaw, 0);
            npc.layer = InteractableLayer;
            var col = npc.AddComponent<CapsuleCollider>();
            col.height = seated ? 1.35f : 1.75f;
            col.radius = seated ? 0.4f : 0.3f;
            col.center = new Vector3(0, col.height / 2f, seated ? 0.1f : 0f);

            var talk = npc.AddComponent<CharacterNpc>();
            Set(talk, "_profile", profile);
            Set(talk, "_game", Game);
            SetBool(talk, "_turnBody", !seated); // sentado solo gira la cabeza
            npc.AddComponent<Highlightable>();

            if (seated && withChair) Chair(npc.transform, chairMaterial);

            var prefab = CharacterArtBuilder.LoadPrefab(characterId);
            if (prefab != null)
            {
                var model = (GameObject)PrefabUtility.InstantiatePrefab(prefab, npc.transform);
                model.name = "Modelo";
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;
                var anim = model.GetComponent<NpcAnimator>();
                Set(anim, "_npc", talk);
                SetBool(anim, "_seated", seated);
                var head = model.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.Head);
                Set(talk, "_head", head != null ? head : model.transform);
                foreach (var r in model.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = ShadowCastingMode.On;
                return npc;
            }
            Debug.LogWarning($"[Caso404] Falta el prefab de {characterId}: usa Caso404/Arte/Construir personajes. Se pone un muñeco de primitivas.");
            PrimitiveDummy(npc, profile, seated, talk);
            return npc;
        }

        /// <summary>Silla bajo un personaje sentado (asiento a 0.46 m, respaldo detrás).</summary>
        static void Chair(Transform npc, string material)
        {
            // Fuera del NPC: no se resalta con él y queda vacía si el personaje no está en la escena.
            var chair = Group($"Silla ({npc.name})");
            var seat = Box("Asiento", chair, npc.TransformPoint(new Vector3(0, 0.43f, 0.08f)), new Vector3(0.46f, 0.06f, 0.46f), material, rotY: npc.eulerAngles.y);
            Box("Respaldo", chair, npc.TransformPoint(new Vector3(0, 0.75f, -0.17f)), new Vector3(0.46f, 0.6f, 0.05f), material, rotY: npc.eulerAngles.y);
            foreach (var x in new[] { -0.2f, 0.2f })
                foreach (var z in new[] { -0.13f, 0.29f })
                    Box("Pata", chair, npc.TransformPoint(new Vector3(x, 0.2f, z)), new Vector3(0.04f, 0.4f, 0.04f), "metal", rotY: npc.eulerAngles.y, keepCollider: false);
            seat.layer = 0;
        }

        static void PrimitiveDummy(GameObject npc, CharacterProfile profile, bool seated, CharacterNpc talk)
        {
            float drop = seated ? 0.4f : 0f;
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Cuerpo";
            Object.DestroyImmediate(body.GetComponent<Collider>());
            body.transform.SetParent(npc.transform, false);
            body.transform.localPosition = new Vector3(0, 0.72f - drop, 0);
            body.transform.localScale = new Vector3(0.5f, 0.72f, 0.4f);
            body.GetComponent<Renderer>().sharedMaterial = Mat($"chr_{profile.Id}", profile.PlaceholderColor);
            body.layer = InteractableLayer;

            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Cabeza";
            Object.DestroyImmediate(head.GetComponent<Collider>());
            head.transform.SetParent(npc.transform, false);
            head.transform.localPosition = new Vector3(0, 1.58f - drop, 0);
            head.transform.localScale = Vector3.one * 0.26f;
            head.GetComponent<Renderer>().sharedMaterial = Mat("skin");
            head.layer = InteractableLayer;

            var nose = Box("Nariz (frente)", npc.transform, Vector3.zero, new Vector3(0.05f, 0.05f, 0.06f), "skin", keepCollider: false);
            nose.transform.localPosition = new Vector3(0, 1.57f - drop, 0.13f);
            nose.transform.localRotation = Quaternion.identity;
            nose.layer = InteractableLayer;
            Set(talk, "_head", head.transform);
        }

        public static void SpawnPlayer(Vector3 position, float yaw)
        {
            var player = new GameObject("Player");
            player.transform.SetPositionAndRotation(position, Quaternion.Euler(0, yaw, 0));
            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.75f;
            cc.radius = 0.3f;
            cc.center = new Vector3(0, 0.875f, 0);
            cc.stepOffset = 0.3f;
            _input = player.AddComponent<InputReader>();

            var pivot = new GameObject("CameraPivot").transform;
            pivot.SetParent(player.transform, false);
            pivot.localPosition = new Vector3(0, 1.6f, 0);
            var camGo = new GameObject("PlayerCamera") { tag = "MainCamera" };
            camGo.transform.SetParent(pivot, false);
            var cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.05f;
            cam.fieldOfView = 65f;
            camGo.AddComponent<AudioListener>();
            Set(camGo.AddComponent<CameraShake>(), "_game", Game);

            var fps = player.AddComponent<FirstPersonController>();
            Set(fps, "_input", _input);
            Set(fps, "_cameraPivot", pivot);
            Set(fps, "_uiModalChannel", _modal);

            _context = player.AddComponent<PlayerContext>();
            Set(_context, "_controller", fps);
            Set(_context, "_camera", cam);

            _interactor = player.AddComponent<PlayerInteractor>();
            Set(_interactor, "_input", _input);
            Set(_interactor, "_context", _context);
            Set(_interactor, "_camera", cam);
            var so = new SerializedObject(_interactor);
            // El rayo también choca con paredes (Default) para no interactuar a través de ellas.
            so.FindProperty("_interactableMask").intValue = (1 << InteractableLayer) | 1;
            so.ApplyModifiedPropertiesWithoutUndo();

            _inspection = player.AddComponent<InspectionController>();
            Set(_inspection, "_player", _context);
            Set(_inspection, "_input", _input);
            Set(_inspection, "_game", Game);
        }

        // ================= UI =================

        /// <summary>Canvas con escalado 1920×1080 y EventSystem del Input System.</summary>
        public static Canvas CreateCanvas(string name, int sortingOrder = 0)
        {
            if (Object.FindAnyObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem)).AddComponent<InputSystemUIInputModule>();
            var go = new GameObject(name, typeof(RectTransform));
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        static void BuildGameUi()
        {
            var canvas = CreateCanvas("UI");
            var systems = new GameObject("UISystems");
            Set(systems.AddComponent<AudioFeedback>(), "_game", Game);
            var ui = systems.AddComponent<UIManager>();
            Set(ui, "_input", _input);
            Set(ui, "_modalChannel", _modal);

            var hud = UiChild<HUDView>(canvas.transform, "HUD", false);
            Set(hud, "_theme", Theme);
            Set(hud, "_game", Game);
            Set(hud, "_interactor", _interactor);
            Set(hud, "_uiModalChannel", _modal);

            var inspectionView = Panel<InspectionView>(canvas.transform, "InspectionView", ui);
            Set(inspectionView, "_inspection", _inspection);
            Set(inspectionView, "_game", Game);
            var journal = Panel<JournalView>(canvas.transform, "JournalView", ui);
            Set(journal, "_game", Game);
            var dialogue = Panel<DialogueView>(canvas.transform, "DialogueView", ui);
            Set(dialogue, "_game", Game);
            Set(dialogue, "_player", _context);
            var alex = Panel<AlexPanelView>(canvas.transform, "AlexPanelView", ui);
            Set(alex, "_game", Game);
            var hypothesis = Panel<HypothesisView>(canvas.transform, "HypothesisView", ui);
            Set(hypothesis, "_game", Game);
            var accusation = Panel<AccusationView>(canvas.transform, "AccusationView", ui);
            Set(accusation, "_game", Game);
            var decision = Panel<DecisionView>(canvas.transform, "DecisionView", ui);
            Set(decision, "_game", Game);
            var ending = Panel<EndingView>(canvas.transform, "EndingView", ui);
            Set(ending, "_game", Game);
            var message = Panel<MessageView>(canvas.transform, "MessageView", ui);

            var faderCanvas = CreateCanvas("Fader", 100);
            var fader = UiChild<ScreenFader>(faderCanvas.transform, "ScreenFader", false);
            Set(fader, "_theme", Theme);

            var controller = systems.AddComponent<GameUIController>();
            Set(controller, "_game", Game);
            Set(controller, "_input", _input);
            Set(controller, "_player", _context);
            Set(controller, "_ui", ui);
            Set(controller, "_theme", Theme);
            Set(controller, "_hud", hud);
            Set(controller, "_journal", journal);
            Set(controller, "_alex", alex);
            Set(controller, "_hypothesis", hypothesis);
            Set(controller, "_accusation", accusation);
            Set(controller, "_decision", decision);
            Set(controller, "_ending", ending);
            Set(controller, "_dialogue", dialogue);
            Set(controller, "_message", message);
            Set(controller, "_fader", fader);
        }

        public static T UiChild<T>(Transform parent, string name, bool panel) where T : Component
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

        static T Panel<T>(Transform parent, string name, UIManager ui) where T : UIPanel
        {
            var view = UiChild<T>(parent, name, true);
            Set(view, "_theme", Theme);
            Set(view, "_ui", ui);
            return view;
        }

        // ================= Luz =================

        public static void Ambient(Color ambient, Color fog, float fogStart = 12f, float fogEnd = 40f)
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = ambient;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = fog;
            RenderSettings.fogStartDistance = fogStart;
            RenderSettings.fogEndDistance = fogEnd;
        }

        public static void Sun(string name, Color color, float intensity, Vector3 euler)
        {
            var sun = new GameObject(name).AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = color;
            sun.intensity = intensity;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(euler);
        }

        public static void PointLight(string name, Vector3 pos, Color color, float intensity, float range)
        {
            var l = new GameObject(name).AddComponent<Light>();
            l.type = LightType.Point;
            l.transform.position = pos;
            l.color = color;
            l.intensity = intensity;
            l.range = range;
            l.shadows = LightShadows.None;
        }

        // ================= Materiales =================

        static void CreateMaterials()
        {
            Materials.Clear();
            Mat("floor", new Color(0.42f, 0.33f, 0.26f));
            Mat("floor_hall", new Color(0.36f, 0.36f, 0.34f));
            Mat("floor_balcony", new Color(0.5f, 0.5f, 0.48f));
            Mat("floor_tile", new Color(0.55f, 0.5f, 0.42f));
            Mat("concrete", new Color(0.42f, 0.42f, 0.42f));
            Mat("wall", new Color(0.72f, 0.7f, 0.66f));
            Mat("wall_hall", new Color(0.6f, 0.62f, 0.6f));
            Mat("wall_cafe", new Color(0.55f, 0.38f, 0.28f));
            Mat("wall_office", new Color(0.62f, 0.64f, 0.66f));
            Mat("wood", new Color(0.4f, 0.27f, 0.17f));
            Mat("fabric", new Color(0.26f, 0.3f, 0.36f));
            Mat("paper", new Color(0.91f, 0.89f, 0.82f));
            Mat("dark", new Color(0.1f, 0.1f, 0.11f));
            Mat("metal", new Color(0.55f, 0.57f, 0.6f), 0.7f, 0.8f);
            Mat("locker", new Color(0.35f, 0.42f, 0.38f), 0.5f, 0.6f);
            Mat("glass", new Color(0.7f, 0.85f, 0.9f), 0.95f);
            Mat("mud", new Color(0.25f, 0.18f, 0.1f));
            Mat("dust", new Color(0.55f, 0.43f, 0.3f));
            Mat("red", new Color(0.75f, 0.12f, 0.1f));
            Mat("skin", new Color(0.78f, 0.6f, 0.48f));
            Mat("cork", new Color(0.6f, 0.45f, 0.28f));
            Mat("screen", new Color(0.31f, 0.82f, 0.77f));
            Mat("screen_off", new Color(0.05f, 0.08f, 0.1f), 0.9f);
            Mat("overall", new Color(0.45f, 0.47f, 0.5f));
            Mat("ash", new Color(0.18f, 0.17f, 0.16f));
            Mat("container", new Color(0.18f, 0.35f, 0.22f), 0.3f, 0.4f);
            Mat("car", new Color(0.3f, 0.12f, 0.12f), 0.6f, 0.5f);
            Mat("yellow", new Color(0.85f, 0.7f, 0.2f));
        }

        /// <summary>Material URP Lit compartido (un asset por color).</summary>
        public static Material Mat(string key, Color? color = null, float smooth = 0.2f, float metal = 0f)
        {
            if (Materials.TryGetValue(key, out var cached) && color == null) return cached;
            SharedAssetsBuilder.EnsureFolder(MatFolder);
            var path = $"{MatFolder}/mat_{key}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            if (color != null)
            {
                if (key == "glass") MakeTransparent(mat);
                mat.SetColor("_BaseColor", key == "glass" ? new Color(color.Value.r, color.Value.g, color.Value.b, 0.22f) : color.Value);
                mat.SetFloat("_Smoothness", smooth);
                mat.SetFloat("_Metallic", metal);
                mat.enableInstancing = true;
                EditorUtility.SetDirty(mat);
            }
            Materials[key] = mat;
            return mat;
        }

        /// <summary>URP Lit transparente (vidrios: se ve la ciudad a través).</summary>
        static void MakeTransparent(Material mat)
        {
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetFloat("_ZWrite", 0f);
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)RenderQueue.Transparent;
            mat.SetOverrideTag("RenderType", "Transparent");
        }

        // ================= Serialización =================

        public static void Set(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop == null) { Debug.LogError($"[Caso404] {target.GetType().Name} no tiene el campo {field}"); return; }
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetBool(Object target, string field, bool value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).boolValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
