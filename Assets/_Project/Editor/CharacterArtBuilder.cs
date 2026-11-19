using System.Collections.Generic;
using System.IO;
using System.Linq;
using Caso404.Characters;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Caso404.EditorTools
{
    /// <summary>
    /// Personajes 3D a partir de Ultimate Modular Men / Women (Quaternius, CC0): la ropa ya viene modelada.
    /// 1) importa las piezas como Humanoid;
    /// 2) crea las animaciones humanoides por código (reposo, hablar, sentado y sentado hablando) y su Animator Controller;
    /// 3) arma cada personaje combinando piezas del paquete (cabeza, torso, piernas y pies comparten el mismo esqueleto)
    ///    y les cambia el color por material (piel, pelo, camisa, pantalón...) según su oficio en el guion;
    /// 4) agrega detalles simples (placa, gorra), escala y el Animator listo, y guarda un prefab por personaje.
    /// Antes la ropa se generaba por código sobre un cuerpo base y se veía rara (DECISIONES J24).
    /// Uso: menú Caso404/Arte/Construir personajes, o -executeMethod Caso404.EditorTools.CharacterArtBuilder.Build
    /// </summary>
    public static class CharacterArtBuilder
    {
        public const string PackRoot = "Assets/_Project/Art/ThirdParty/Quaternius_ModularCharacters";

        const string ArtFolder = "Assets/_Project/Art/Characters";
        const string AnimFolder = ArtFolder + "/Animations";
        const string MatFolder = ArtFolder + "/Materials";
        public const string PrefabFolder = "Assets/_Project/Prefabs/Characters";
        public const string ControllerPath = AnimFolder + "/chr_npc.controller";

        // ================= Vestuario según el guion =================

        /// <summary>Una pieza del paquete: "Men/Suit" + "Body" = la malla Suit_Body de Men/Suit.fbx.</summary>
        readonly struct Piece
        {
            public readonly string Model, Part;
            public Piece(string model, string part) { Model = model; Part = part; }
            public string Fbx => $"{PackRoot}/{Model}.fbx";
            public string Mesh => $"{Model.Split('/')[1]}_{Part}";
        }

        /// <summary>
        /// Cómo se ve cada personaje: qué cabeza, torso, piernas y pies usa, y de qué color queda cada material.
        /// Los colores son decisiones de arte según el oficio (DECISIONES N2 y J24).
        /// </summary>
        sealed class Look
        {
            public string Id;
            public bool Female;
            public Piece Head, Body, Legs, Feet;
            public float SkinTone;             // 0 = piel clara, 1 = piel oscura
            public Color Hair;
            public Dictionary<string, Color> Colors = new(); // nombre del material del paquete → color nuevo
            public float Height = 1f;           // escala uniforme (el paquete mide ~1.85 m)
            public bool Badge, Cap;
            public Color CapColor;
        }

        static Piece M(string model, string part) => new($"Men/{model}", part);
        static Piece W(string model, string part) => new($"Women/{model}", part);

        static readonly Look[] Looks =
        {
            // Caso 1
            new() { Id = "carlos", SkinTone = 0.45f, Hair = C(0.12f, 0.08f, 0.06f), Height = 0.96f,   // chef: chaqueta blanca
                Head = M("Adventurer", "Head"), Body = M("Casual2", "Body"), Legs = M("Suit", "Legs"), Feet = M("Suit", "Feet"),
                Colors = { ["LightBrown"] = C(0.9f, 0.9f, 0.87f), ["Suit"] = C(0.13f, 0.13f, 0.14f), ["Black"] = C(0.07f, 0.07f, 0.07f) } },
            new() { Id = "laura", Female = true, SkinTone = 0.3f, Hair = C(0.3f, 0.16f, 0.1f), Height = 0.94f,  // enfermera: uniforme verde azulado
                Head = W("Formal", "Head"), Body = W("Casual", "Body"), Legs = W("Casual", "Legs"), Feet = W("Casual", "Feet"),
                Colors = { ["White"] = C(0.25f, 0.55f, 0.57f), ["Orange"] = C(0.22f, 0.5f, 0.52f), ["Grey"] = C(0.88f, 0.88f, 0.86f), ["Red"] = C(0.3f, 0.16f, 0.1f) } },
            new() { Id = "hernan", SkinTone = 0.4f, Hair = C(0.7f, 0.7f, 0.68f), Height = 0.93f, Badge = true, // portero mayor: saco azul de uniforme
                Head = M("Adventurer", "Head"), Body = M("Suit", "Body"), Legs = M("Suit", "Legs"), Feet = M("Suit", "Feet"),
                Colors = { ["Suit"] = C(0.17f, 0.22f, 0.33f), ["Tie"] = C(0.32f, 0.34f, 0.38f), ["White"] = C(0.78f, 0.8f, 0.82f), ["Black"] = C(0.06f, 0.06f, 0.06f) } },
            new() { Id = "andres", SkinTone = 0.6f, Hair = C(0.07f, 0.06f, 0.05f), Height = 0.95f, Cap = true, CapColor = C(0.3f, 0.31f, 0.33f), // mantenimiento: overol gris
                Head = M("Casual2", "Head"), Body = M("Farmer", "Body"), Legs = M("Farmer", "Pants"), Feet = M("Worker", "Feet"),
                Colors = { ["LightBlue"] = C(0.4f, 0.42f, 0.45f), ["Beige"] = C(0.62f, 0.6f, 0.55f), ["Brown"] = C(0.3f, 0.3f, 0.32f), ["Grey"] = C(0.22f, 0.16f, 0.1f), ["Black"] = C(0.18f, 0.13f, 0.09f) } },
            // Caso 2
            new() { Id = "sebastian", SkinTone = 0.25f, Hair = C(0.05f, 0.04f, 0.04f), Height = 0.95f,  // ingeniero joven: buzo verde, jean, tenis blancos
                Head = M("Suit", "Head"), Body = M("Casual", "Body"), Legs = M("Casual2", "Legs"), Feet = M("Casual", "Feet"),
                Colors = { ["Purple"] = C(0.16f, 0.27f, 0.21f), ["LightBlue"] = C(0.2f, 0.27f, 0.4f), ["White"] = C(0.92f, 0.92f, 0.92f) } },
            new() { Id = "valeria", Female = true, SkinTone = 0.12f, Hair = C(0.62f, 0.55f, 0.43f), Height = 0.95f, // directora: traje oscuro
                Head = W("Suit", "Head"), Body = W("Suit", "Body"), Legs = W("Suit", "Legs"), Feet = W("Suit", "Feet"),
                Colors = { ["Black"] = C(0.13f, 0.13f, 0.15f), ["White"] = C(0.85f, 0.85f, 0.86f) } },
            new() { Id = "ines", Female = true, SkinTone = 0.85f, Hair = C(0.05f, 0.04f, 0.04f), Height = 0.95f, Badge = true, // guardia nocturna: saco azul marino
                Head = W("Soldier", "Head"), Body = W("Suit", "Body"), Legs = W("Suit", "Legs"), Feet = W("Suit", "Feet"),
                Colors = { ["Black"] = C(0.11f, 0.15f, 0.27f), ["White"] = C(0.72f, 0.78f, 0.86f) } },
            new() { Id = "elena", Female = true, SkinTone = 0.5f, Hair = C(0.07f, 0.05f, 0.05f), Height = 0.94f,  // periodista: camiseta oliva, jean, botines
                Head = W("Casual", "Head"), Body = W("Casual", "Body"), Legs = W("Casual", "Legs"), Feet = W("Adventurer", "Feet"),
                Colors = { ["White"] = C(0.38f, 0.36f, 0.22f), ["Orange"] = C(0.2f, 0.27f, 0.42f), ["Brown_02"] = C(0.3f, 0.2f, 0.12f), ["Brown2"] = C(0.22f, 0.15f, 0.09f) } },
        };

        static Color C(float r, float g, float b) => new(r, g, b);

        // Materiales del paquete que son pelo (se pintan con el color de pelo de cada personaje).
        static readonly HashSet<string> HairMats = new() { "Hair", "Eyebrows", "Hair_Brown", "Hair_Blond", "DarkBrown", "Moustache" };
        static readonly Color SkinLight = C(0.93f, 0.78f, 0.66f), SkinDark = C(0.36f, 0.24f, 0.17f);

        // ================= Entrada =================

        [MenuItem("Caso404/Arte/Construir personajes")]
        public static void Build()
        {
            ConfigureImporters();
            EnsureFolder(AnimFolder); EnsureFolder(MatFolder); EnsureFolder(PrefabFolder);
            CleanOldArt();
            var controller = BuildAnimations();
            foreach (var look in Looks) BuildPrefab(look, controller);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Caso404] {Looks.Length} personajes construidos en {PrefabFolder}.");
        }

        /// <summary>Ruta del prefab de un personaje (o null si no existe todavía).</summary>
        public static GameObject LoadPrefab(string characterId) =>
            AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/chr_{characterId}.prefab");

        /// <summary>Importa los modelos del paquete como Humanoid (avatar creado desde el propio modelo).</summary>
        public static void ConfigureImporters()
        {
            foreach (var path in Looks.SelectMany(l => new[] { l.Head, l.Body, l.Legs, l.Feet }).Select(p => p.Fbx).Distinct())
            {
                var imp = (ModelImporter)AssetImporter.GetAtPath(path);
                if (imp == null) { Debug.LogError($"[Caso404] No se encontró {path}"); continue; }
                if (imp.animationType == ModelImporterAnimationType.Human && imp.avatarSetup == ModelImporterAvatarSetup.CreateFromThisModel && !imp.importAnimation) continue;
                imp.animationType = ModelImporterAnimationType.Human;
                imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                imp.importAnimation = false;
                imp.SaveAndReimport();
            }
        }

        /// <summary>Borra el arte del sistema anterior (ropa y texturas generadas por código) y los materiales viejos.</summary>
        static void CleanOldArt()
        {
            foreach (var folder in new[] { ArtFolder + "/Meshes", ArtFolder + "/Textures" })
                if (AssetDatabase.IsValidFolder(folder)) AssetDatabase.DeleteAsset(folder);
            foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { MatFolder }))
                AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(guid));
        }

        // ================= Animaciones (músculos humanoides) =================

        /// <summary>
        /// Los paquetes no traen animaciones de reposo ni de conversación. Se crean clips humanoides por código: funcionan
        /// con cualquier modelo configurado como Humanoid (músculos normalizados, no rotaciones de huesos).
        /// </summary>
        static AnimatorController BuildAnimations()
        {
            var idle = Clip("chr_idle", 4f, false, false);
            var talk = Clip("chr_talk", 3f, true, false);
            var sit = Clip("chr_sit_idle", 4f, false, true);
            var sitTalk = Clip("chr_sit_talk", 3f, true, true);

            AssetDatabase.DeleteAsset(ControllerPath);
            var ac = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            ac.AddParameter("Talking", AnimatorControllerParameterType.Bool);
            ac.AddParameter("Seated", AnimatorControllerParameterType.Bool);
            ac.AddParameter("CycleOffset", AnimatorControllerParameterType.Float);
            var layers = ac.layers;
            layers[0].iKPass = true; // para que la cabeza mire al jugador (NpcAnimator.OnAnimatorIK)
            ac.layers = layers;
            var sm = ac.layers[0].stateMachine;
            AnimatorState State(string name, Motion m, Vector3 pos)
            {
                var s = sm.AddState(name, pos);
                s.motion = m;
                s.cycleOffsetParameter = "CycleOffset";
                s.cycleOffsetParameterActive = true;
                return s;
            }
            var sIdle = State("Reposo", idle, new Vector3(300, 0));
            var sTalk = State("Hablar", talk, new Vector3(300, 120));
            var sSit = State("Sentado", sit, new Vector3(600, 0));
            var sSitTalk = State("Sentado hablando", sitTalk, new Vector3(600, 120));
            sm.defaultState = sIdle;
            void Link(AnimatorState a, AnimatorState b, params (string p, bool v)[] conds)
            {
                var t = a.AddTransition(b);
                t.hasExitTime = false;
                t.duration = 0.35f;
                foreach (var (p, v) in conds) t.AddCondition(v ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, p);
            }
            Link(sIdle, sTalk, ("Talking", true), ("Seated", false));
            Link(sTalk, sIdle, ("Talking", false));
            Link(sIdle, sSit, ("Seated", true));
            Link(sSit, sSitTalk, ("Talking", true));
            Link(sSitTalk, sSit, ("Talking", false));
            Link(sTalk, sSitTalk, ("Seated", true));
            EditorUtility.SetDirty(ac);
            return ac;
        }

        static AnimationClip Clip(string name, float length, bool talking, bool seated)
        {
            var path = $"{AnimFolder}/{name}.anim";
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null) { clip = new AnimationClip(); AssetDatabase.CreateAsset(clip, path); }
            clip.ClearCurves();
            float L = length;

            // Cuerpo: raíz quieta mirando a +Z. Sentado: cadera más baja (a la altura de una silla).
            Const(clip, "RootT.x", 0f); Const(clip, "RootT.z", 0f);
            Wave(clip, "RootT.y", seated ? 0.56f : 1.0f, 0.004f, L, 1);
            Const(clip, "RootQ.x", 0f); Const(clip, "RootQ.y", 0f); Const(clip, "RootQ.z", 0f); Const(clip, "RootQ.w", 1f);

            // Respiración, postura y un leve cambio de peso de un pie al otro (de pie).
            Wave(clip, "Spine Front-Back", seated ? 0.08f : 0.02f, 0.02f, L, 1);
            if (!seated) Wave(clip, "Spine Left-Right", 0f, 0.025f, L, 1, 0.1f);
            Wave(clip, "Chest Front-Back", 0.02f, 0.025f, L, 1, 0.2f);
            Wave(clip, "Head Turn Left-Right", 0f, talking ? 0.06f : 0.04f, L, 1, 0.5f);
            if (talking) Wave(clip, "Head Nod Down-Up", -0.05f, 0.12f, L, 3);
            else Wave(clip, "Head Nod Down-Up", -0.04f, 0.02f, L, 1, 0.3f);

            // Piernas
            foreach (var side in new[] { "Left", "Right" })
            {
                Const(clip, $"{side} Upper Leg Front-Back", seated ? -0.72f : 0.6f);
                Const(clip, $"{side} Upper Leg In-Out", seated ? 0.12f : 0.05f);
                Const(clip, $"{side} Lower Leg Stretch", seated ? 0.02f : 0.97f);
                Const(clip, $"{side} Foot Up-Down", seated ? 0.1f : 0f);
            }

            // Brazos: abajo y relajados; al hablar, la mano derecha gesticula y la izquierda acompaña.
            foreach (var side in new[] { "Left", "Right" })
            {
                bool right = side == "Right";
                Const(clip, $"{side} Shoulder Down-Up", -0.2f);
                // De pie y en reposo, la palma mira al muslo (antes miraba al frente y la mano parecía una garra).
                bool restingStanding = !seated && !talking;
                Const(clip, $"{side} Arm Twist In-Out", restingStanding ? 0f : 0.1f);
                Const(clip, $"{side} Forearm Twist In-Out", restingStanding ? -0.05f : 0.3f);
                if (seated && !talking)
                {
                    // Manos sobre los muslos
                    Const(clip, $"{side} Arm Down-Up", -0.45f);
                    Const(clip, $"{side} Arm Front-Back", -0.15f);
                    Const(clip, $"{side} Forearm Stretch", 0.25f);
                    Const(clip, $"{side} Hand Down-Up", -0.2f);
                }
                else if (talking)
                {
                    float amp = right ? 1f : 0.5f;
                    Keys(clip, $"{side} Arm Down-Up", L, -0.5f, -0.5f + 0.18f * amp, -0.45f, -0.5f + 0.12f * amp, -0.5f);
                    Keys(clip, $"{side} Arm Front-Back", L, -0.05f, -0.25f * amp, -0.15f * amp, -0.3f * amp, -0.05f);
                    Keys(clip, $"{side} Forearm Stretch", L, 0.55f, 0.15f + (1 - amp) * 0.3f, 0.35f, 0.1f + (1 - amp) * 0.3f, 0.55f);
                    Keys(clip, $"{side} Hand Down-Up", L, 0f, 0.2f * amp, -0.1f, 0.25f * amp, 0f);
                    Keys(clip, $"{side} Hand In-Out", L, 0f, 0.2f * amp, 0f, -0.1f * amp, 0f);
                }
                else
                {
                    // Brazos caídos junto al cuerpo (antes quedaban abiertos en "A", como un maniquí) y codo apenas flexionado.
                    Wave(clip, $"{side} Arm Down-Up", -0.55f, 0.015f, L, 1, right ? 0f : 0.5f);
                    Const(clip, $"{side} Arm Front-Back", 0.02f);
                    Const(clip, $"{side} Forearm Stretch", 0.78f);
                    Const(clip, $"{side} Hand Down-Up", 0.05f);
                    Const(clip, $"{side} Hand In-Out", right ? -0.1f : -0.05f);
                }
                // Dedos semicerrados y naturales
                // Dedos relajados: un poco más cerrados hacia el meñique, como una mano en reposo real.
                string[] fingers = { "Index", "Middle", "Ring", "Little" };
                for (int f = 0; f < fingers.Length; f++)
                    for (int j = 1; j <= 3; j++)
                        Const(clip, $"{side}Hand.{fingers[f]}.{j} Stretched", talking && right ? 0.15f : 0.3f + 0.05f * f);
                Const(clip, $"{side}Hand.Thumb.1 Stretched", 0.2f);
                Const(clip, $"{side}Hand.Thumb.Spread", 0.1f);
            }

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.keepOriginalOrientation = true;
            settings.keepOriginalPositionY = true;
            settings.keepOriginalPositionXZ = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
            return clip;
        }

        static void SetCurve(AnimationClip clip, string attr, AnimationCurve curve) =>
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(Animator), attr), curve);

        static void Const(AnimationClip clip, string attr, float v) => SetCurve(clip, attr, AnimationCurve.Constant(0f, 1f, v));

        /// <summary>Onda senoidal que cierra el ciclo (cycles vueltas en length segundos).</summary>
        static void Wave(AnimationClip clip, string attr, float center, float amplitude, float length, int cycles, float phase = 0f)
        {
            var keys = new List<Keyframe>();
            const int steps = 16;
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                keys.Add(new Keyframe(t * length, center + amplitude * Mathf.Sin((t * cycles + phase) * Mathf.PI * 2f)));
            }
            var c = new AnimationCurve(keys.ToArray());
            for (int i = 0; i < c.length; i++) c.SmoothTangents(i, 0f);
            SetCurve(clip, attr, c);
        }

        /// <summary>Valores repartidos en el tiempo (el último debe ser igual al primero para que el ciclo cierre).</summary>
        static void Keys(AnimationClip clip, string attr, float length, params float[] values)
        {
            var keys = values.Select((v, i) => new Keyframe(length * i / (values.Length - 1), v)).ToArray();
            var c = new AnimationCurve(keys);
            for (int i = 0; i < c.length; i++) c.SmoothTangents(i, 0f);
            SetCurve(clip, attr, c);
        }

        // ================= Prefab =================

        static void BuildPrefab(Look look, AnimatorController controller)
        {
            // El torso define el esqueleto y el avatar; las demás piezas se enganchan a esos mismos huesos (por nombre).
            var root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(look.Body.Fbx));
            PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            root.name = $"chr_{look.Id}";
            var bones = root.GetComponentsInChildren<Transform>().GroupBy(t => t.name).ToDictionary(g => g.Key, g => g.First());

            var wanted = new[] { look.Head, look.Body, look.Legs, look.Feet };
            foreach (var smr in root.GetComponentsInChildren<SkinnedMeshRenderer>())
                if (!wanted.Any(p => p.Model == look.Body.Model && p.Mesh == smr.name)) Object.DestroyImmediate(smr.gameObject);
            foreach (var piece in wanted.Where(p => p.Model != look.Body.Model)) Graft(root, bones, piece);

            foreach (var smr in root.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                smr.updateWhenOffscreen = false;
                smr.sharedMaterials = smr.sharedMaterials.Select(m => MaterialFor(look, m)).ToArray();
            }

            var animator = root.GetComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;

            var head = animator.GetBoneTransform(HumanBodyBones.Head);
            var chest = animator.GetBoneTransform(HumanBodyBones.UpperChest) ?? animator.GetBoneTransform(HumanBodyBones.Chest);
            if (look.Cap && head != null) AddCap(root, head, look.CapColor);
            if (look.Badge && chest != null) AddBadge(root, chest, look.Female);

            root.AddComponent<NpcAnimator>();
            root.transform.localScale = Vector3.one * look.Height;
            PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabFolder}/chr_{look.Id}.prefab");
            Object.DestroyImmediate(root);
        }

        /// <summary>Copia una malla de otro modelo del paquete y la ata a los huesos del personaje con el mismo nombre.</summary>
        static void Graft(GameObject root, Dictionary<string, Transform> bones, Piece piece)
        {
            var src = (GameObject)Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(piece.Fbx));
            var from = src.GetComponentsInChildren<SkinnedMeshRenderer>().FirstOrDefault(s => s.name == piece.Mesh);
            if (from == null) { Debug.LogError($"[Caso404] {piece.Fbx} no tiene la malla {piece.Mesh}"); Object.DestroyImmediate(src); return; }
            var go = new GameObject(piece.Mesh);
            go.transform.SetParent(root.transform, false);
            var smr = go.AddComponent<SkinnedMeshRenderer>();
            smr.sharedMesh = from.sharedMesh;
            smr.sharedMaterials = from.sharedMaterials;
            smr.bones = from.bones.Select(b => b != null && bones.TryGetValue(b.name, out var t) ? t : null).ToArray();
            smr.rootBone = from.rootBone != null && bones.TryGetValue(from.rootBone.name, out var rb) ? rb : null;
            smr.localBounds = from.localBounds;
            if (smr.bones.Any(b => b == null)) Debug.LogWarning($"[Caso404] {piece.Mesh}: hay huesos sin pareja en el esqueleto.");
            Object.DestroyImmediate(src);
        }

        /// <summary>Material propio del personaje para cada material del paquete (piel, pelo, ropa), con su color.</summary>
        static Material MaterialFor(Look look, Material original)
        {
            string name = original != null ? original.name : "Ropa";
            var baseColor = original != null && original.HasProperty("_BaseColor") ? original.GetColor("_BaseColor")
                          : original != null ? original.color : Color.gray;
            float smooth = 0.18f;
            Color color;
            if (look.Colors.TryGetValue(name, out var custom)) color = custom;
            else if (name.StartsWith("Skin")) { color = Color.Lerp(SkinLight, SkinDark, look.SkinTone) * (name == "Skin_Darker" ? 0.9f : 1f); smooth = 0.32f; }
            else if (HairMats.Contains(name)) { color = look.Hair; smooth = 0.35f; }
            else if (name is "Eye" or "Brown") { color = C(0.12f, 0.08f, 0.06f); smooth = 0.6f; }
            else color = baseColor;
            color.a = 1f;
            return Lit($"M_chr_{look.Id}_{name}", color, smooth);
        }

        /// <summary>Gorra de trabajo: casquete y visera sobre la cabeza (en la pose original del modelo, que mira a +Z).</summary>
        static void AddCap(GameObject root, Transform head, Color color)
        {
            var headMesh = root.GetComponentsInChildren<SkinnedMeshRenderer>().First(s => s.name.EndsWith("_Head"));
            var b = BakedBounds(headMesh);
            var top = new Vector3(b.center.x, b.max.y, b.center.z);
            float w = b.size.x * 1.04f;
            Prim(PrimitiveType.Sphere, "Gorra", head, top + Vector3.down * 0.06f, new Vector3(w, 0.13f, b.size.z * 1.02f), color);
            Prim(PrimitiveType.Cube, "Visera", head, new Vector3(top.x, top.y - 0.085f, b.max.z + 0.03f), new Vector3(w * 0.75f, 0.012f, 0.09f), color * 0.85f);
        }

        /// <summary>Placa dorada en el lado izquierdo del pecho (uniformes de portería y vigilancia).</summary>
        static void AddBadge(GameObject root, Transform chest, bool female)
        {
            var body = root.GetComponentsInChildren<SkinnedMeshRenderer>().First(s => s.name.EndsWith("_Body"));
            var mesh = new Mesh();
            body.BakeMesh(mesh, true);
            var m = body.transform.localToWorldMatrix;
            float y = chest.position.y + (female ? 0.02f : 0.06f);
            const float x = 0.1f;
            float front = float.MinValue;
            foreach (var v in mesh.vertices)
            {
                var p = m.MultiplyPoint3x4(v);
                if (Mathf.Abs(p.y - y) < 0.04f && Mathf.Abs(p.x - x) < 0.04f) front = Mathf.Max(front, p.z);
            }
            Object.DestroyImmediate(mesh);
            if (front == float.MinValue) return;
            Prim(PrimitiveType.Cube, "Placa", chest, new Vector3(x, y, front + 0.004f), new Vector3(0.045f, 0.055f, 0.01f), C(0.85f, 0.7f, 0.25f), 0.7f);
        }

        static Bounds BakedBounds(SkinnedMeshRenderer smr)
        {
            var mesh = new Mesh();
            smr.BakeMesh(mesh, true);
            var m = smr.transform.localToWorldMatrix;
            var verts = mesh.vertices;
            var b = new Bounds(m.MultiplyPoint3x4(verts[0]), Vector3.zero);
            foreach (var v in verts) b.Encapsulate(m.MultiplyPoint3x4(v));
            Object.DestroyImmediate(mesh);
            return b;
        }

        static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 worldPos, Vector3 size, Color color, float smooth = 0.2f)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.position = worldPos;
            go.transform.localScale = size;
            go.transform.SetParent(parent, true);
            go.GetComponent<Renderer>().sharedMaterial = Lit($"M_chr_{name.ToLowerInvariant()}_{ColorUtility.ToHtmlStringRGB(color)}", color, smooth);
            return go;
        }

        /// <summary>Material URP Lit de color plano (estilo low-poly del paquete). Se reutiliza si ya existe.</summary>
        static Material Lit(string name, Color color, float smooth)
        {
            var path = $"{MatFolder}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Smoothness", smooth);
            mat.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static void EnsureFolder(string path) => SharedAssetsBuilder.EnsureFolder(path);
    }
}
