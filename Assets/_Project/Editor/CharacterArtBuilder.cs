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
    /// Personajes 3D a partir de Universal Base Characters (Quaternius, CC0):
    /// 1) importa los dos cuerpos base como Humanoid;
    /// 2) crea animaciones humanoides por código (reposo, hablar, sentado y sentado hablando) y su Animator Controller;
    /// 3) pinta la ropa de cada personaje sobre la textura (por zonas del cuerpo, según el hueso que mueve cada triángulo);
    /// 4) arma un prefab por personaje con pelo, accesorios simples, escala y el Animator listo.
    /// Uso: menú Caso404/Arte/Construir personajes, o -executeMethod Caso404.EditorTools.CharacterArtBuilder.Build
    /// </summary>
    public static class CharacterArtBuilder
    {
        public const string PackRoot = "Assets/_Project/Art/ThirdParty/Universal Base Characters[Standard]/Universal Base Characters[Standard]";
        public const string MaleFbx = PackRoot + "/Base Characters/Unity/Superhero_Male_FullBody.fbx";
        public const string FemaleFbx = PackRoot + "/Base Characters/Unity/Superhero_Female_FullBody.fbx";
        const string BaseTextures = PackRoot + "/Base Characters/Textures";
        const string HairFolder = PackRoot + "/Hairstyles/Origin at 0/FBX (Unity)";
        const string HairTextures = PackRoot + "/Hairstyles/Textures";

        const string ArtFolder = "Assets/_Project/Art/Characters";
        const string AnimFolder = ArtFolder + "/Animations";
        const string TexFolder = ArtFolder + "/Textures";
        const string MatFolder = ArtFolder + "/Materials";
        public const string PrefabFolder = "Assets/_Project/Prefabs/Characters";
        public const string ControllerPath = AnimFolder + "/chr_npc.controller";
        const int TexSize = 1024;

        // ================= Vestuario según el guion =================

        enum Hair { None, Long, Buns, SimpleParted, Buzzed, BuzzedFemale }
        enum Pattern { Plain, Suit, Chef, Overall, Uniform, Scrubs, Hoodie, Casual }

        /// <summary>Cómo se ve cada personaje (edad, oficio, ropa). Los colores son decisiones de arte (DECISIONES.md, N2).</summary>
        sealed class Look
        {
            public string Id;
            public bool Female;
            public float SkinTone;          // 0 = piel clara, 1 = piel oscura (mezcla de las dos texturas del paquete)
            public Color Top, Pants, Shoes;
            public bool ShortSleeves;
            public Pattern Pattern;
            public Hair Hair;
            public Color HairColor;
            public bool Beard;
            public Vector3 Scale = Vector3.one;
            public bool Cap;                // gorra (Andrés)
            public Color CapColor;
            public bool Badge;              // placa en el pecho (uniformes)
            public bool ChefHat;
        }

        static readonly Look[] Looks =
        {
            // Caso 1
            new() { Id = "laura", Female = true, SkinTone = 0.3f, Pattern = Pattern.Scrubs, ShortSleeves = true,
                Top = C(0.27f, 0.58f, 0.6f), Pants = C(0.24f, 0.52f, 0.55f), Shoes = C(0.9f, 0.9f, 0.88f),
                Hair = Hair.Buns, HairColor = C(0.38f, 0.2f, 0.12f), Scale = new Vector3(0.97f, 0.97f, 0.97f) },
            new() { Id = "carlos", SkinTone = 0.45f, Pattern = Pattern.Chef,
                Top = C(0.93f, 0.93f, 0.9f), Pants = C(0.12f, 0.12f, 0.13f), Shoes = C(0.08f, 0.08f, 0.08f),
                Hair = Hair.SimpleParted, HairColor = C(0.14f, 0.09f, 0.06f), Beard = true },
            new() { Id = "hernan", SkinTone = 0.4f, Pattern = Pattern.Uniform, Badge = true,
                Top = C(0.2f, 0.26f, 0.38f), Pants = C(0.17f, 0.18f, 0.2f), Shoes = C(0.06f, 0.06f, 0.06f),
                Hair = Hair.Buzzed, HairColor = C(0.68f, 0.68f, 0.66f), Beard = true, Scale = new Vector3(1.05f, 0.95f, 1.05f) },
            new() { Id = "andres", SkinTone = 0.6f, Pattern = Pattern.Overall,
                Top = C(0.44f, 0.46f, 0.49f), Pants = C(0.44f, 0.46f, 0.49f), Shoes = C(0.2f, 0.14f, 0.09f),
                Hair = Hair.Buzzed, HairColor = C(0.07f, 0.06f, 0.05f), Beard = true, Cap = true, CapColor = C(0.32f, 0.33f, 0.35f) },
            // Caso 2
            new() { Id = "sebastian", SkinTone = 0.25f, Pattern = Pattern.Hoodie,
                Top = C(0.16f, 0.26f, 0.2f), Pants = C(0.2f, 0.27f, 0.42f), Shoes = C(0.92f, 0.92f, 0.92f),
                Hair = Hair.SimpleParted, HairColor = C(0.05f, 0.04f, 0.04f), Scale = new Vector3(0.94f, 1f, 0.94f) },
            new() { Id = "valeria", Female = true, SkinTone = 0.12f, Pattern = Pattern.Suit,
                Top = C(0.15f, 0.15f, 0.17f), Pants = C(0.15f, 0.15f, 0.17f), Shoes = C(0.05f, 0.05f, 0.05f),
                Hair = Hair.Buns, HairColor = C(0.62f, 0.56f, 0.46f), Scale = new Vector3(0.95f, 1f, 0.95f) },
            new() { Id = "ines", Female = true, SkinTone = 0.8f, Pattern = Pattern.Uniform, Badge = true,
                Top = C(0.12f, 0.16f, 0.3f), Pants = C(0.08f, 0.08f, 0.09f), Shoes = C(0.05f, 0.05f, 0.05f),
                Hair = Hair.BuzzedFemale, HairColor = C(0.05f, 0.04f, 0.04f) },
            new() { Id = "elena", Female = true, SkinTone = 0.5f, Pattern = Pattern.Casual,
                Top = C(0.36f, 0.33f, 0.2f), Pants = C(0.2f, 0.27f, 0.42f), Shoes = C(0.25f, 0.16f, 0.1f),
                Hair = Hair.Long, HairColor = C(0.07f, 0.05f, 0.05f) },
        };

        static Color C(float r, float g, float b) => new(r, g, b);

        // ================= Entrada =================

        [MenuItem("Caso404/Arte/Construir personajes")]
        public static void Build()
        {
            ConfigureImporters();
            EnsureFolder(AnimFolder); EnsureFolder(TexFolder); EnsureFolder(MatFolder); EnsureFolder(PrefabFolder);
            var controller = BuildAnimations();
            var regionMaps = new Dictionary<bool, BodyMap> { [false] = BuildBodyMap(MaleFbx), [true] = BuildBodyMap(FemaleFbx) };
            var skins = new Dictionary<bool, (Color[] light, Color[] dark)>
            {
                [false] = (LoadPixels(BaseTextures + "/T_Superhero_Male_Ligh.png"), LoadPixels(BaseTextures + "/T_Superhero_Male_Dark.png")),
                [true] = (LoadPixels(BaseTextures + "/T_Superhero_Female_Light_BaseColor.png"), LoadPixels(BaseTextures + "/T_Superhero_Female_Dark_BaseColor.png")),
            };
            foreach (var look in Looks)
            {
                var tex = PaintOutfit(look, regionMaps[look.Female], skins[look.Female]);
                BuildPrefab(look, tex, controller, regionMaps[look.Female]);
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[Caso404] {Looks.Length} personajes construidos en {PrefabFolder}.");
        }

        /// <summary>Ruta del prefab de un personaje (o null si no existe todavía).</summary>
        public static GameObject LoadPrefab(string characterId) =>
            AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/chr_{characterId}.prefab");

        /// <summary>Importa los dos cuerpos base como Humanoid (avatar creado desde el propio modelo).</summary>
        public static void ConfigureImporters()
        {
            foreach (var path in new[] { MaleFbx, FemaleFbx })
            {
                var imp = (ModelImporter)AssetImporter.GetAtPath(path);
                if (imp == null) { Debug.LogError($"[Caso404] No se encontró {path}"); continue; }
                if (imp.animationType == ModelImporterAnimationType.Human && imp.avatarSetup == ModelImporterAvatarSetup.CreateFromThisModel) continue;
                imp.animationType = ModelImporterAnimationType.Human;
                imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                imp.SaveAndReimport();
            }
        }

        // ================= Animaciones (músculos humanoides) =================

        /// <summary>
        /// El paquete no trae animaciones. Se crean clips humanoides por código: funcionan con cualquier modelo
        /// configurado como Humanoid. Valores medidos con HumanPoseHandler (pose T: brazo 0.43, muslo 0.6).
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

            // Respiración y postura
            Wave(clip, "Spine Front-Back", seated ? 0.08f : 0.02f, 0.02f, L, 1);
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
                Const(clip, $"{side} Arm Twist In-Out", 0.1f);
                Const(clip, $"{side} Forearm Twist In-Out", 0.3f);
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
                    Wave(clip, $"{side} Arm Down-Up", -0.55f, 0.01f, L, 1, right ? 0f : 0.5f);
                    Const(clip, $"{side} Arm Front-Back", 0.3f);
                    Const(clip, $"{side} Forearm Stretch", 0.82f);
                    Const(clip, $"{side} Hand Down-Up", 0f);
                }
                // Dedos semicerrados y naturales
                foreach (var f in new[] { "Index", "Middle", "Ring", "Little" })
                    for (int j = 1; j <= 3; j++) Const(clip, $"{side}Hand.{f}.{j} Stretched", talking && right ? 0.1f : -0.1f);
                Const(clip, $"{side}Hand.Thumb.1 Stretched", 0.2f);
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

        // ================= Mapa de zonas del cuerpo =================

        enum Region : byte { None, Head, Neck, Hands, Torso, UpperArm, LowerArm, Hips, Legs, Feet }

        /// <summary>Para cada píxel de la textura: zona del cuerpo y posición 3D aproximada (para detalles como el cuello en V).</summary>
        sealed class BodyMap
        {
            public Region[] Regions = new Region[TexSize * TexSize];
            public Vector3[] Positions = new Vector3[TexSize * TexSize];
            public float Height;
            public Vector3 ChestFront;   // punto del pecho (lado izquierdo) para la placa
            public Vector3 HeadTop;      // coronilla, para gorras
        }

        static Region RegionOfBone(string bone)
        {
            var b = bone.ToLowerInvariant();
            if (b == "head") return Region.Head;
            if (b.StartsWith("neck")) return Region.Neck;
            if (b.StartsWith("hand") || b.StartsWith("index") || b.StartsWith("middle") || b.StartsWith("ring") || b.StartsWith("pinky") || b.StartsWith("thumb")) return Region.Hands;
            if (b.StartsWith("upperarm")) return Region.UpperArm;
            if (b.StartsWith("lowerarm")) return Region.LowerArm;
            if (b.StartsWith("spine") || b.StartsWith("clavicle")) return Region.Torso;
            if (b.StartsWith("pelvis") || b == "root") return Region.Hips;
            if (b.StartsWith("thigh") || b.StartsWith("calf")) return Region.Legs;
            if (b.StartsWith("foot") || b.StartsWith("ball")) return Region.Feet;
            return Region.Torso;
        }

        static BodyMap BuildBodyMap(string fbx)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
            var inst = Object.Instantiate(model);
            var smr = inst.GetComponentsInChildren<SkinnedMeshRenderer>().OrderByDescending(s => s.sharedMesh.vertexCount).First();
            var mesh = smr.sharedMesh;
            var baked = new Mesh();
            smr.BakeMesh(baked, true);
            var verts = baked.vertices.Select(v => smr.transform.TransformPoint(v)).ToArray();
            var uvs = mesh.uv;
            var weights = mesh.boneWeights;
            var boneRegions = smr.bones.Select(b => RegionOfBone(b.name)).ToArray();
            var map = new BodyMap { Height = verts.Max(v => v.y) };
            map.HeadTop = verts.OrderByDescending(v => v.y).First();

            // Región por vértice (suma de pesos por zona)
            var vRegion = new Region[verts.Length];
            var acc = new float[10];
            for (int i = 0; i < verts.Length; i++)
            {
                System.Array.Clear(acc, 0, acc.Length);
                var w = weights[i];
                acc[(int)boneRegions[w.boneIndex0]] += w.weight0;
                acc[(int)boneRegions[w.boneIndex1]] += w.weight1;
                acc[(int)boneRegions[w.boneIndex2]] += w.weight2;
                acc[(int)boneRegions[w.boneIndex3]] += w.weight3;
                int best = 1;
                for (int r = 1; r < acc.Length; r++) if (acc[r] > acc[best]) best = r;
                vRegion[i] = (Region)best;
            }

            // Rasteriza cada triángulo en el espacio UV
            var tris = mesh.triangles;
            var filled = new bool[TexSize * TexSize];
            for (int t = 0; t < tris.Length; t += 3)
            {
                int a = tris[t], b = tris[t + 1], c = tris[t + 2];
                var region = vRegion[a] == vRegion[b] || vRegion[a] == vRegion[c] ? vRegion[a] : vRegion[b];
                RasterTriangle(uvs[a], uvs[b], uvs[c], (x, y, wa, wb, wc) =>
                {
                    int idx = y * TexSize + x;
                    map.Regions[idx] = region;
                    map.Positions[idx] = verts[a] * wa + verts[b] * wb + verts[c] * wc;
                    filled[idx] = true;
                });
            }
            Dilate(map, filled, 4);

            // Punto del pecho izquierdo (para la placa): el más adelantado a la altura del pecho
            float chestY = map.Height * 0.74f;
            float front = verts.Where(v => Mathf.Abs(v.y - chestY) < 0.03f && v.x > 0.06f && v.x < 0.14f).Select(v => v.z).DefaultIfEmpty(0.12f).Min();
            map.ChestFront = new Vector3(0.1f, chestY, front);
            Object.DestroyImmediate(inst);
            Object.DestroyImmediate(baked);
            return map;
        }

        delegate void PixelFn(int x, int y, float wa, float wb, float wc);

        static void RasterTriangle(Vector2 ua, Vector2 ub, Vector2 uc, PixelFn fn)
        {
            Vector2 A = ua * TexSize, B = ub * TexSize, Cc = uc * TexSize;
            int minX = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(A.x, Mathf.Min(B.x, Cc.x))));
            int maxX = Mathf.Min(TexSize - 1, Mathf.CeilToInt(Mathf.Max(A.x, Mathf.Max(B.x, Cc.x))));
            int minY = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(A.y, Mathf.Min(B.y, Cc.y))));
            int maxY = Mathf.Min(TexSize - 1, Mathf.CeilToInt(Mathf.Max(A.y, Mathf.Max(B.y, Cc.y))));
            float area = (B.x - A.x) * (Cc.y - A.y) - (Cc.x - A.x) * (B.y - A.y);
            if (Mathf.Abs(area) < 1e-6f) return;
            for (int y = minY; y <= maxY; y++)
                for (int x = minX; x <= maxX; x++)
                {
                    var p = new Vector2(x + 0.5f, y + 0.5f);
                    float wa = ((B.x - p.x) * (Cc.y - p.y) - (Cc.x - p.x) * (B.y - p.y)) / area;
                    float wb = ((Cc.x - p.x) * (A.y - p.y) - (A.x - p.x) * (Cc.y - p.y)) / area;
                    float wc = 1f - wa - wb;
                    if (wa < -0.01f || wb < -0.01f || wc < -0.01f) continue;
                    fn(x, y, wa, wb, wc);
                }
        }

        /// <summary>Extiende las zonas unos píxeles hacia afuera para que no se vean costuras en los bordes UV.</summary>
        static void Dilate(BodyMap map, bool[] filled, int passes)
        {
            for (int pass = 0; pass < passes; pass++)
            {
                var next = (bool[])filled.Clone();
                for (int y = 1; y < TexSize - 1; y++)
                    for (int x = 1; x < TexSize - 1; x++)
                    {
                        int i = y * TexSize + x;
                        if (filled[i]) continue;
                        foreach (var n in new[] { i - 1, i + 1, i - TexSize, i + TexSize })
                            if (filled[n]) { map.Regions[i] = map.Regions[n]; map.Positions[i] = map.Positions[n]; next[i] = true; break; }
                    }
                System.Array.Copy(next, filled, filled.Length);
            }
        }

        // ================= Ropa pintada =================

        static Color[] LoadPixels(string assetPath)
        {
            var src = new Texture2D(2, 2);
            src.LoadImage(File.ReadAllBytes(assetPath));
            var px = new Color[TexSize * TexSize];
            for (int y = 0; y < TexSize; y++)
                for (int x = 0; x < TexSize; x++)
                    px[y * TexSize + x] = src.GetPixelBilinear((x + 0.5f) / TexSize, (y + 0.5f) / TexSize);
            Object.DestroyImmediate(src);
            return px;
        }

        static float Lum(Color c) => c.r * 0.3f + c.g * 0.59f + c.b * 0.11f;

        static Texture2D PaintOutfit(Look look, BodyMap map, (Color[] light, Color[] dark) skin)
        {
            var px = new Color[TexSize * TexSize];
            float h = map.Height;
            float waist = h * 0.585f;   // donde termina la camisa y empieza el pantalón
            for (int i = 0; i < px.Length; i++)
            {
                var region = map.Regions[i];
                var p = map.Positions[i];
                var baseSkin = Color.Lerp(skin.light[i], skin.dark[i], look.SkinTone);
                float shade = Mathf.Clamp(Lum(skin.light[i]) / 0.62f, 0.55f, 1.25f);
                shade = Mathf.Lerp(1f, shade, 0.45f); // conserva un poco del volumen pintado, sin músculos marcados
                Color? cloth = null;
                switch (region)
                {
                    case Region.Torso:
                        cloth = p.y < waist ? look.Pants : look.Top;
                        break;
                    case Region.UpperArm:
                        cloth = look.Top;
                        break;
                    case Region.LowerArm:
                        if (!look.ShortSleeves) cloth = look.Top;
                        break;
                    case Region.Hips:
                        cloth = p.y > waist + 0.02f && look.Pattern != Pattern.Overall ? look.Top : look.Pants;
                        break;
                    case Region.Legs:
                        cloth = look.Pants;
                        break;
                    case Region.Feet:
                        cloth = look.Shoes;
                        break;
                }
                if (cloth != null) cloth = Details(look, region, p, h, waist, cloth.Value, i);
                px[i] = cloth != null ? cloth.Value * shade : baseSkin;
                px[i].a = 1f;
            }
            var tex = new Texture2D(TexSize, TexSize, TextureFormat.RGB24, false);
            tex.SetPixels(px);
            tex.Apply();
            var path = $"{TexFolder}/T_chr_{look.Id}.png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.maxTextureSize = TexSize;
            imp.mipmapEnabled = true;
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        /// <summary>Detalles por tipo de ropa: cuello en V del traje, cinturón, cuadros del pantalón de chef, bolsillos del overol...</summary>
        static Color Details(Look look, Region region, Vector3 p, float h, float waist, Color cloth, int pixel)
        {
            bool front = p.z < -0.02f; // en la pose T el modelo base mira a -Z
            float ax = Mathf.Abs(p.x);
            bool pantsPart = region == Region.Legs || (region is Region.Hips or Region.Torso && p.y < waist);
            switch (look.Pattern)
            {
                case Pattern.Suit:
                    // Camisa blanca en V bajo la chaqueta
                    if (region == Region.Torso && front && p.y > h * 0.71f && ax < (p.y - h * 0.71f) * 0.55f + 0.012f) return C(0.92f, 0.92f, 0.94f);
                    break;
                case Pattern.Chef:
                    if (pantsPart)
                    {
                        // Pantalón de cuadros (pied-de-poule simplificado)
                        int u = pixel % TexSize / 6, v = pixel / TexSize / 6;
                        return (u + v) % 2 == 0 ? C(0.12f, 0.12f, 0.13f) : C(0.75f, 0.75f, 0.74f);
                    }
                    // Doble fila de botones de la chaquetilla
                    if (region == Region.Torso && front && p.y > waist + 0.05f && p.y < h * 0.78f)
                    {
                        float bx = Mathf.Abs(ax - 0.06f), by = Mathf.Repeat(p.y, 0.07f);
                        if (bx < 0.008f && by < 0.012f) return C(0.25f, 0.25f, 0.27f);
                    }
                    break;
                case Pattern.Overall:
                    // Bolsillo de pecho y costuras más oscuras
                    if (region == Region.Torso && front && p.y > h * 0.68f && p.y < h * 0.74f && ax < 0.06f) return cloth * 0.82f;
                    if (Mathf.Abs(p.y - waist) < 0.008f) return cloth * 0.75f;
                    break;
                case Pattern.Uniform:
                    if (Mathf.Abs(p.y - waist) < 0.018f) return C(0.06f, 0.06f, 0.06f); // cinturón
                    if (region == Region.Torso && front && p.y > h * 0.7f && Mathf.Abs(ax - 0.1f) < 0.035f && p.y < h * 0.75f) return cloth * 0.8f; // bolsillos
                    break;
                case Pattern.Scrubs:
                    if (region == Region.Torso && front && p.y > h * 0.72f && ax < (p.y - h * 0.72f) * 0.6f + 0.01f) return cloth * 0.7f; // cuello en V
                    break;
                case Pattern.Hoodie:
                    if (region == Region.Torso && front && p.y > waist + 0.06f && p.y < waist + 0.16f && ax < 0.1f) return cloth * 0.8f; // bolsillo canguro
                    if (pantsPart && Mathf.Abs(p.y - waist) < 0.012f) return C(0.25f, 0.18f, 0.1f);
                    break;
                case Pattern.Casual:
                    // Chaqueta abierta sobre camiseta clara
                    if (region == Region.Torso && front && p.y > waist && ax < 0.045f) return C(0.85f, 0.83f, 0.78f);
                    if (pantsPart && Mathf.Abs(p.y - waist) < 0.012f) return C(0.25f, 0.18f, 0.1f);
                    break;
            }
            return cloth;
        }

        // ================= Prefab =================

        static void BuildPrefab(Look look, Texture2D bodyTex, AnimatorController controller, BodyMap map)
        {
            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(look.Female ? FemaleFbx : MaleFbx);
            var root = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
            PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            root.name = $"chr_{look.Id}";
            var animator = root.GetComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;

            var body = Lit($"M_chr_{look.Id}", bodyTex, Color.white, 0.18f);
            var hairMat = HairMaterial(look);
            var eyes = Lit("M_chr_eyes", AssetDatabase.LoadAssetAtPath<Texture2D>(BaseTextures + "/T_Eye_Brown.png"), Color.white, 0.75f);
            foreach (var smr in root.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                smr.updateWhenOffscreen = false;
                smr.sharedMaterial = smr.name.StartsWith("Eyebrows") ? hairMat : smr.name.StartsWith("Eyes") ? eyes : body;
            }

            var head = animator.GetBoneTransform(HumanBodyBones.Head);
            var chest = animator.GetBoneTransform(HumanBodyBones.UpperChest) ?? animator.GetBoneTransform(HumanBodyBones.Chest);
            if (look.Hair != Hair.None) AttachStatic(HairFolder + $"/Hair_{look.Hair}.fbx", head, hairMat, "Pelo");
            if (look.Beard) AttachStatic(HairFolder + "/Hair_Beard.fbx", head, hairMat, "Barba");

            // El modelo base mira a -Z en pose T: los detalles se ubican en ese espacio y luego el Animator lo gira a +Z.
            if (look.Cap)
            {
                var top = map.HeadTop;
                var cap = Prim(PrimitiveType.Cylinder, "Gorra", head, new Vector3(top.x, top.y - 0.045f, top.z + 0.005f), new Vector3(0.2f, 0.045f, 0.21f), look.CapColor);
                Prim(PrimitiveType.Cube, "Visera", cap.transform.parent, new Vector3(top.x, top.y - 0.075f, top.z - 0.12f), new Vector3(0.16f, 0.012f, 0.09f), look.CapColor * 0.85f);
            }
            if (look.Badge)
            {
                var badgePos = new Vector3(map.ChestFront.x, map.ChestFront.y, -Mathf.Abs(map.ChestFront.z) - 0.005f);
                Prim(PrimitiveType.Cube, "Placa", chest, badgePos, new Vector3(0.045f, 0.055f, 0.01f), C(0.85f, 0.7f, 0.25f), 0.7f);
            }

            root.AddComponent<NpcAnimator>();
            root.transform.localScale = look.Scale; // al final: el pelo y los accesorios se pegan en la pose original
            var path = $"{PrefabFolder}/chr_{look.Id}.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }

        /// <summary>Pega un modelo estático (pelo, barba) al hueso de la cabeza conservando su posición de la pose T.</summary>
        static void AttachStatic(string fbxPath, Transform bone, Material mat, string name)
        {
            var src = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (src == null) { Debug.LogError($"[Caso404] No se encontró {fbxPath}"); return; }
            var go = Object.Instantiate(src);
            go.name = name;
            // Los peinados del paquete miran al lado contrario que el cuerpo en pose T: se giran 180° sobre el eje vertical.
            var turn = Quaternion.Euler(0f, 180f, 0f);
            go.transform.SetPositionAndRotation(turn * go.transform.position, turn * go.transform.rotation);
            go.transform.SetParent(bone, true);
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                r.sharedMaterial = mat;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            }
        }

        static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 worldPos, Vector3 size, Color color, float smooth = 0.2f)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.position = worldPos;
            go.transform.localScale = size;
            go.transform.SetParent(parent, true);
            go.GetComponent<Renderer>().sharedMaterial = Lit($"M_chr_{name.ToLowerInvariant()}_{ColorUtility.ToHtmlStringRGB(color)}", null, color, smooth);
            return go;
        }

        static Material HairMaterial(Look look)
        {
            bool type2 = look.Hair is Hair.Long or Hair.Buns;
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{HairTextures}/T_Hair_{(type2 ? 2 : 1)}_BaseColor.png");
            // El color del pelo se logra dividiendo por el color medio de la textura (así el gris queda gris).
            var avg = AverageColor(tex);
            var tint = new Color(Mathf.Clamp(look.HairColor.r / Mathf.Max(avg.r, 0.05f), 0f, 6f),
                                 Mathf.Clamp(look.HairColor.g / Mathf.Max(avg.g, 0.05f), 0f, 6f),
                                 Mathf.Clamp(look.HairColor.b / Mathf.Max(avg.b, 0.05f), 0f, 6f));
            return Lit($"M_hair_{look.Id}", tex, tint, 0.3f);
        }

        static readonly Dictionary<Texture2D, Color> AvgCache = new();

        static Color AverageColor(Texture2D tex)
        {
            if (tex == null) return Color.gray;
            if (AvgCache.TryGetValue(tex, out var c)) return c;
            var path = AssetDatabase.GetAssetPath(tex);
            var src = new Texture2D(2, 2);
            src.LoadImage(File.ReadAllBytes(path));
            var sum = Color.black; int n = 0;
            for (int y = 0; y < 64; y++)
                for (int x = 0; x < 64; x++)
                {
                    var p = src.GetPixelBilinear((x + 0.5f) / 64f, (y + 0.5f) / 64f);
                    if (p.a < 0.5f) continue;
                    sum += p; n++;
                }
            Object.DestroyImmediate(src);
            c = n > 0 ? sum / n : Color.gray;
            AvgCache[tex] = c;
            return c;
        }

        static Material Lit(string name, Texture2D tex, Color color, float smooth)
        {
            var path = $"{MatFolder}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetTexture("_BaseMap", tex);
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Smoothness", smooth);
            mat.SetFloat("_Metallic", 0f);
            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static void EnsureFolder(string path) => SharedAssetsBuilder.EnsureFolder(path);
    }
}
