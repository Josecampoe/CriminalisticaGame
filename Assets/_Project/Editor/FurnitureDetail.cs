using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Caso404.EditorTools
{
    /// <summary>
    /// Convierte los muebles hechos con un solo bloque en muebles armados por piezas: sillas con patas y espaldar,
    /// escritorios con tablero, cajonera y patas, mesas con patas, sofás con base, patas y cojines, cama con marco,
    /// colchón y almohadas, y armarios o gabinetes con puertas, cajones y manijas.
    /// El bloque original se queda en su sitio con su collider (no cambia por dónde camina el jugador ni qué se puede
    /// mirar); solo se oculta su malla. Las piezas nuevas no tienen collider.
    /// </summary>
    public static class FurnitureDetail
    {
        static Transform _group;
        static List<Bounds> _people;
        static List<Bounds> _tables;
        static int _count;

        /// <summary>Arma los muebles de la escena abierta. Devuelve cuántos muebles cambió.</summary>
        public static int Apply(Transform root)
        {
            _count = 0;
            Physics.SyncTransforms();
            _group = new GameObject("Muebles armados").transform;
            _group.SetParent(root, false);
            _people = Object.FindObjectsByType<Caso404.Dialogue.CharacterNpc>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Select(n => new Bounds(n.transform.position + Vector3.up * 0.9f, new Vector3(1.1f, 1.8f, 1.1f))).ToList();

            int interactable = LayerMask.NameToLayer(ProjectSetup.InteractableLayer);
            // Idempotente: si el paso visual ya corrió, los bloques originales están ocultos (y su versión armada se borró
            // con la raíz anterior). Se vuelven a mostrar para armarlos de nuevo; si no, desaparecerían los muebles.
            foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                if (r.enabled || r.transform.IsChildOf(root) || !IsFurniture(r.name)) continue;
                r.enabled = true;
                GameObjectUtility.SetStaticEditorFlags(r.gameObject,
                    GameObjectUtility.GetStaticEditorFlags(r.gameObject) | StaticEditorFlags.ContributeGI | StaticEditorFlags.BatchingStatic);
            }

            var all = Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)
                .Where(r => !r.transform.IsChildOf(root) && r.enabled && r.GetComponent<MeshFilter>() != null)
                // Lo que se puede mirar o recoger (evidencias, personajes, la acusación) no se toca.
                .Where(r => r.gameObject.layer != interactable && r.GetComponentInParent<Caso404.Interaction.IInteractable>() == null)
                .ToList();
            _tables = all.Where(r => IsDesk(r.name) || IsTable(r.name)).Select(r => r.bounds).ToList();

            foreach (var r in all)
            {
                string n = r.name;
                if (n.StartsWith("Silla")) Chair(r);
                else if (IsDesk(n)) Desk(r);
                else if (IsTable(n)) Table(r);
                else if (n == "Sofá base") SofaBase(r);
                else if (n == "Sofá de espera") WaitingSofa(r);
                else if (n == "Cama") Bed(r);
                else if (n == "Almohadas") Pillows(r);
                else if (n is "Armario" or "Mueble del televisor" or "Gabinetes altos" or "Encimera" or "Mesa de noche"
                         or "Archivador" or "Caseta mostrador" or "Banco de trabajo") Cabinet(r);
            }
            return _count;
        }

        static bool IsFurniture(string n) => n.StartsWith("Silla") || IsDesk(n) || IsTable(n)
            || n is "Sofá base" or "Sofá de espera" or "Cama" or "Almohadas" or "Armario" or "Mueble del televisor"
                 or "Gabinetes altos" or "Encimera" or "Mesa de noche" or "Archivador" or "Caseta mostrador" or "Banco de trabajo";

        static bool IsDesk(string n) => n.StartsWith("Escritorio");
        static bool IsTable(string n) => n is "Mesa de interrogatorio" or "Mesa de centro (corrida)";

        // ================= Sillas =================

        /// <summary>Silla de comedor u oficina: asiento, cuatro patas y espaldar del lado contrario a la mesa más cercana.</summary>
        static void Chair(MeshRenderer box)
        {
            var t = box.transform;
            var s = Size(t);
            if (s.y < 0.3f || s.y > 0.7f) return;
            if (_people.Any(p => p.Intersects(box.bounds))) return; // hay alguien sentado: su silla la arma el personaje
            var mat = box.sharedMaterial;
            bool office = mat != null && mat.name == "mat_dark";
            var legMat = office ? SceneKit.Mat("metal") : mat;

            var back = BackDirection(t, box.bounds.center);
            float bottom = -s.y / 2f, seatTop = bottom + 0.46f * (s.y / 0.5f);
            Hide(box);
            Part(t, "Asiento", new Vector3(0, seatTop - 0.025f, 0), new Vector3(s.x, 0.05f, s.z), office ? SceneKit.Mat("fabric") : mat);
            float legH = seatTop - 0.05f - bottom;
            foreach (var (x, z) in Corners(s.x / 2f - 0.035f, s.z / 2f - 0.035f))
                Part(t, "Pata", new Vector3(x, bottom + legH / 2f, z), new Vector3(0.035f, legH, 0.035f), legMat);

            // Espaldar: dos postes y una tabla, en el borde de atrás.
            var side = new Vector3(back.z, 0, back.x); // perpendicular al espaldar, en el plano
            float halfAlong = Mathf.Abs(back.x) > 0.5f ? s.z / 2f : s.x / 2f;
            float edge = Mathf.Abs(back.x) > 0.5f ? s.x / 2f : s.z / 2f;
            var backPos = back * (edge - 0.02f);
            float postH = 0.44f;
            foreach (float k in new[] { -1f, 1f })
                Part(t, "Poste del espaldar", backPos + side * k * (halfAlong - 0.035f) + Vector3.up * (seatTop + postH / 2f),
                    Thin(back, 0.035f, postH, 0.035f), legMat);
            Part(t, "Espaldar", backPos + Vector3.up * (seatTop + postH - 0.11f), Thin(back, 0.025f, 0.18f, halfAlong * 2f - 0.02f), mat);
            _count++;
        }

        /// <summary>Dirección (en el espacio del bloque, x o z) donde va el espaldar: opuesta a la mesa más cercana o hacia la pared.</summary>
        static Vector3 BackDirection(Transform t, Vector3 center)
        {
            var near = _tables.Where(b => b.SqrDistance(center) < 1.6f * 1.6f).OrderBy(b => b.SqrDistance(center)).ToList();
            Vector3 world;
            if (near.Count > 0) world = center - near[0].ClosestPoint(center);
            else world = -NearestWallDirection(center + Vector3.up * 0.3f, 0.6f);
            world.y = 0;
            if (world.sqrMagnitude < 1e-4f) world = -t.forward;
            var local = t.InverseTransformDirection(world);
            return Mathf.Abs(local.x) > Mathf.Abs(local.z) ? new Vector3(Mathf.Sign(local.x), 0, 0) : new Vector3(0, 0, Mathf.Sign(local.z));
        }

        // ================= Mesas y escritorios =================

        /// <summary>Escritorio: tablero, cajonera con tres cajones en un extremo, dos patas en el otro y faldón de atrás.</summary>
        static void Desk(MeshRenderer box)
        {
            var t = box.transform;
            var s = Size(t);
            if (s.y < 0.5f || s.y > 1f) return; // el escritorio de Elena ya es solo tablero con patas
            var mat = box.sharedMaterial;
            var metal = SceneKit.Mat("metal");
            var dark = SceneKit.Mat("dark");
            Hide(box);
            float bottom = -s.y / 2f, top = s.y / 2f;
            Part(t, "Tablero", new Vector3(0, top - 0.02f, 0), new Vector3(s.x + 0.03f, 0.04f, s.z + 0.03f), mat);

            bool alongX = s.x >= s.z;
            float len = alongX ? s.x : s.z, depth = alongX ? s.z : s.x;
            var axis = alongX ? Vector3.right : Vector3.forward;
            var across = alongX ? Vector3.forward : Vector3.right;
            var front = FrontDirection(t, box.bounds, alongX ? new[] { Vector3.forward, Vector3.back } : new[] { Vector3.right, Vector3.left });
            float frontSign = Vector3.Dot(front, across) >= 0 ? 1f : -1f;
            float h = s.y - 0.04f;

            // Cajonera
            float pedW = Mathf.Min(0.42f, len * 0.3f);
            var pedC = axis * (len / 2f - pedW / 2f - 0.01f) + Vector3.up * (bottom + h / 2f);
            Part(t, "Cajonera", pedC, Along(axis, pedW, h, depth - 0.04f), mat);
            for (int d = 0; d < 3; d++)
            {
                float y = bottom + h * (0.18f + d * 0.31f);
                var face = pedC + across * frontSign * ((depth - 0.04f) / 2f + 0.004f);
                face.y = y;
                Part(t, "Ranura del cajón", face + Vector3.up * (h * 0.15f), Along(axis, pedW - 0.04f, 0.006f, 0.006f), dark, false);
                Part(t, "Manija del cajón", face + across * frontSign * 0.01f, Along(axis, 0.14f, 0.02f, 0.02f), metal, false);
            }
            // Patas del otro extremo
            foreach (float k in new[] { -1f, 1f })
                Part(t, "Pata", -axis * (len / 2f - 0.05f) + across * k * (depth / 2f - 0.05f) + Vector3.up * (bottom + h / 2f),
                    new Vector3(0.05f, h, 0.05f), mat);
            // Faldón de atrás (tapa las piernas desde el otro lado)
            Part(t, "Faldón", -across * frontSign * (depth / 2f - 0.03f) + Vector3.up * (top - 0.04f - 0.2f),
                Along(axis, len - pedW - 0.12f, 0.36f, 0.02f), mat);
            _count++;
        }

        /// <summary>Mesa: tablero, faldón corto y cuatro patas.</summary>
        static void Table(MeshRenderer box)
        {
            var t = box.transform;
            var s = Size(t);
            if (s.y < 0.3f) return;
            var mat = box.sharedMaterial;
            Hide(box);
            float bottom = -s.y / 2f, top = s.y / 2f;
            Part(t, "Tablero", new Vector3(0, top - 0.02f, 0), new Vector3(s.x + 0.02f, 0.04f, s.z + 0.02f), mat);
            Part(t, "Faldón", new Vector3(0, top - 0.08f, 0), new Vector3(s.x - 0.1f, 0.08f, s.z - 0.1f), mat);
            float legH = s.y - 0.04f;
            foreach (var (x, z) in Corners(s.x / 2f - 0.06f, s.z / 2f - 0.06f))
                Part(t, "Pata", new Vector3(x, bottom + legH / 2f, z), new Vector3(0.055f, legH, 0.055f), mat);
            _count++;
        }

        // ================= Sofás =================

        /// <summary>Base del sofá del apartamento: cuerpo levantado sobre cuatro patas cortas y cojines del espaldar.</summary>
        static void SofaBase(MeshRenderer box)
        {
            var t = box.transform;
            var s = Size(t);
            var mat = box.sharedMaterial;
            Hide(box);
            float bottom = -s.y / 2f;
            Part(t, "Cuerpo del sofá", new Vector3(0, bottom + 0.1f + (s.y - 0.1f) / 2f, 0), new Vector3(s.x, s.y - 0.1f, s.z), mat);
            SofaLegs(t, s, bottom);

            // Cojines del espaldar, apoyados en el respaldo
            var back = Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).FirstOrDefault(r => r.name == "Sofá respaldo");
            if (back != null)
            {
                var bb = back.bounds;
                var sb = box.bounds;
                bool alongZ = sb.size.z >= sb.size.x;
                float len = alongZ ? sb.size.z : sb.size.x;
                var dir = alongZ ? Vector3.forward : Vector3.right;
                var outward = (sb.center - bb.center);
                outward.y = 0;
                outward = alongZ ? new Vector3(Mathf.Sign(outward.x), 0, 0) : new Vector3(0, 0, Mathf.Sign(outward.z));
                var face = bb.center + Vector3.Scale(outward, bb.extents);
                for (int k = 0; k < 2; k++)
                {
                    var c = new Vector3(face.x, sb.max.y + 0.27f, face.z) + outward * 0.08f + dir * ((k - 0.5f) * len / 2f);
                    var size = alongZ ? new Vector3(0.16f, 0.4f, len / 2f - 0.04f) : new Vector3(len / 2f - 0.04f, 0.4f, 0.16f);
                    var go = World("Cojín del espaldar", c, size, mat);
                    go.transform.rotation = Quaternion.AngleAxis(-8f, Vector3.Cross(Vector3.up, outward));
                }
            }
            _count++;
        }

        /// <summary>Sofá de un solo bloque (sala de espera): base, patas, dos cojines, espaldar contra la pared y brazos.</summary>
        static void WaitingSofa(MeshRenderer box)
        {
            var t = box.transform;
            var s = Size(t);
            var mat = box.sharedMaterial;
            bool alongX = s.x >= s.z;
            float len = alongX ? s.x : s.z, depth = alongX ? s.z : s.x;
            var axis = alongX ? Vector3.right : Vector3.forward;
            var across = alongX ? Vector3.forward : Vector3.right;
            var front = FrontDirection(t, box.bounds, alongX ? new[] { Vector3.forward, Vector3.back } : new[] { Vector3.right, Vector3.left });
            float fs = Vector3.Dot(front, across) >= 0 ? 1f : -1f;
            Hide(box);
            float bottom = -s.y / 2f;
            Part(t, "Cuerpo del sofá", Vector3.up * (bottom + 0.1f + 0.15f), Along(axis, len, 0.3f, depth), mat);
            SofaLegs(t, s, bottom);
            float seatY = bottom + 0.4f + 0.06f;
            for (int k = 0; k < 2; k++)
                Part(t, "Cojín del asiento", axis * ((k - 0.5f) * (len - 0.3f) / 2f) + across * fs * 0.06f + Vector3.up * seatY,
                    Along(axis, (len - 0.3f) / 2f - 0.02f, 0.12f, depth - 0.2f), mat);
            Part(t, "Espaldar", -across * fs * (depth / 2f - 0.09f) + Vector3.up * (bottom + 0.62f), Along(axis, len, 0.44f, 0.18f), mat);
            foreach (float k in new[] { -1f, 1f })
                Part(t, "Brazo", axis * k * (len / 2f - 0.075f) + Vector3.up * (bottom + 0.52f), Along(axis, 0.15f, 0.24f, depth), mat);
            _count++;
        }

        static void SofaLegs(Transform t, Vector3 s, float bottom)
        {
            var legMat = SceneKit.Mat("dark");
            foreach (var (x, z) in Corners(s.x / 2f - 0.08f, s.z / 2f - 0.08f))
                Part(t, "Pata del sofá", new Vector3(x, bottom + 0.05f, z), new Vector3(0.05f, 0.1f, 0.05f), legMat);
        }

        // ================= Cama =================

        /// <summary>Cama: marco de madera con patas y colchón claro encima.</summary>
        static void Bed(MeshRenderer box)
        {
            var t = box.transform;
            var s = Size(t);
            Hide(box);
            float bottom = -s.y / 2f;
            var wood = SceneKit.Mat("wood");
            var mattress = SceneKit.Mat("colchon", new Color(0.78f, 0.76f, 0.72f), 0.08f);
            Part(t, "Marco de la cama", Vector3.up * (bottom + 0.1f + 0.1f), new Vector3(s.x + 0.04f, 0.2f, s.z + 0.04f), wood);
            foreach (var (x, z) in Corners(s.x / 2f - 0.04f, s.z / 2f - 0.04f))
                Part(t, "Pata de la cama", new Vector3(x, bottom + 0.05f, z), new Vector3(0.07f, 0.1f, 0.07f), wood);
            Part(t, "Colchón", Vector3.up * (bottom + 0.3f + (s.y - 0.3f) / 2f), new Vector3(s.x - 0.04f, s.y - 0.3f, s.z - 0.04f), mattress);
            _count++;
        }

        /// <summary>Las almohadas (un bloque) pasan a ser dos almohadas blandas.</summary>
        static void Pillows(MeshRenderer box)
        {
            var t = box.transform;
            var s = Size(t);
            Hide(box);
            var mat = SceneKit.Mat("almohada", new Color(0.86f, 0.85f, 0.82f), 0.06f);
            bool alongX = s.x >= s.z;
            var axis = alongX ? Vector3.right : Vector3.forward;
            float len = alongX ? s.x : s.z;
            for (int k = 0; k < 2; k++)
            {
                var go = Part(t, "Almohada", axis * ((k - 0.5f) * len / 2f), Along(axis, len / 2f - 0.04f, s.y, alongX ? s.z : s.x), mat);
                go.transform.rotation *= Quaternion.Euler(0f, k == 0 ? 4f : -5f, 0f);
            }
            _count++;
        }

        // ================= Gabinetes =================

        /// <summary>
        /// Armario, gabinete o cajonera: en la cara que da al cuarto agrega ranuras de puertas (o cajones si es bajo y
        /// angosto), manijas y un zócalo oscuro abajo si llega al piso.
        /// </summary>
        static void Cabinet(MeshRenderer box)
        {
            var t = box.transform;
            var s = Size(t);
            var b = box.bounds;
            bool alongX = s.x >= s.z;
            var front = FrontDirection(t, b, new[] { Vector3.right, Vector3.left, Vector3.forward, Vector3.back });
            bool frontOnX = Mathf.Abs(front.x) > 0.5f;
            float width = frontOnX ? s.z : s.x, depth = frontOnX ? s.x : s.z;
            var axis = frontOnX ? Vector3.forward : Vector3.right;
            var face = front * (depth / 2f + 0.003f);
            var dark = SceneKit.Mat("dark");
            var metal = SceneKit.Mat("metal");
            float bottom = -s.y / 2f, top = s.y / 2f;
            bool onFloor = b.min.y < 0.05f;
            float kick = onFloor && s.y > 0.5f ? 0.08f : 0f;
            if (kick > 0f)
                Part(t, "Zócalo", face + Vector3.up * (bottom + kick / 2f), Along(axis, width - 0.02f, kick, 0.01f), dark, false);

            bool drawers = box.name is "Mesa de noche" or "Archivador" || (s.y < 1.3f && width < 0.7f);
            float usable = s.y - kick;
            if (drawers)
            {
                int n = Mathf.Clamp(Mathf.RoundToInt(usable / 0.3f), 2, 4);
                for (int i = 0; i < n; i++)
                {
                    float y0 = bottom + kick + usable * i / n, y1 = bottom + kick + usable * (i + 1) / n;
                    if (i > 0) Part(t, "Ranura", face + Vector3.up * y0, Along(axis, width - 0.03f, 0.006f, 0.006f), dark, false);
                    Part(t, "Manija", face + front * 0.012f + Vector3.up * ((y0 + y1) / 2f + (y1 - y0) * 0.15f), Along(axis, 0.12f, 0.022f, 0.022f), metal, false);
                }
            }
            else
            {
                int doors = Mathf.Clamp(Mathf.RoundToInt(width / 0.55f), 1, 6);
                for (int i = 1; i < doors; i++)
                {
                    float u = -width / 2f + width * i / doors;
                    Part(t, "Ranura de puerta", face + axis * u + Vector3.up * (bottom + kick + usable / 2f), Along(axis, 0.006f, usable - 0.02f, 0.006f), dark, false);
                }
                for (int i = 0; i < doors; i++)
                {
                    float u0 = -width / 2f + width * i / doors, u1 = -width / 2f + width * (i + 1) / doors;
                    float hx = i % 2 == 0 ? u1 - 0.05f : u0 + 0.05f; // manijas hacia la unión de cada par de puertas
                    if (doors == 1) hx = u1 - 0.06f;
                    float hy = box.name == "Gabinetes altos" ? bottom + 0.12f : (s.y > 1.4f ? 0.05f : top - 0.12f);
                    float hLen = s.y > 1.4f ? 0.22f : 0.1f;
                    Part(t, "Manija", face + front * 0.012f + axis * hx + Vector3.up * hy, Along(axis, 0.02f, hLen, 0.022f), metal, false);
                }
            }
            _count++;
        }

        // ================= Ayudas =================

        /// <summary>Lado del bloque que da al espacio libre: el que tiene más distancia hasta chocar con algo.</summary>
        static Vector3 FrontDirection(Transform t, Bounds b, Vector3[] localCandidates)
        {
            Vector3 best = localCandidates[0];
            float bestDist = -1f;
            var origin = b.center;
            origin.y = b.min.y + Mathf.Min(0.5f, b.size.y * 0.5f);
            foreach (var local in localCandidates)
            {
                var dir = t.TransformDirection(local);
                dir.y = 0;
                dir.Normalize();
                float reach = Vector3.Dot(b.extents, new Vector3(Mathf.Abs(dir.x), 0, Mathf.Abs(dir.z))) + 0.02f;
                float dist = Physics.Raycast(origin + dir * reach, dir, out var hit, 4f) ? hit.distance : 4f;
                if (dist > bestDist) { bestDist = dist; best = local; }
            }
            return best;
        }

        static Vector3 NearestWallDirection(Vector3 origin, float skip)
        {
            Vector3 best = Vector3.forward;
            float bestDist = float.MaxValue;
            foreach (var dir in new[] { Vector3.forward, Vector3.back, Vector3.left, Vector3.right })
                if (Physics.Raycast(origin + dir * skip * 0.5f, dir, out var hit, 3f) && hit.distance < bestDist)
                { bestDist = hit.distance; best = dir; }
            return best;
        }

        static Vector3 Size(Transform t)
        {
            var s = t.lossyScale;
            return new Vector3(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));
        }

        static IEnumerable<(float x, float z)> Corners(float hx, float hz)
        {
            yield return (-hx, -hz); yield return (hx, -hz); yield return (-hx, hz); yield return (hx, hz);
        }

        /// <summary>Tamaño (largo a lo largo de axis, alto, grosor) convertido a x/y/z del bloque.</summary>
        static Vector3 Along(Vector3 axis, float length, float height, float thickness)
            => Mathf.Abs(axis.x) > 0.5f ? new Vector3(length, height, thickness) : new Vector3(thickness, height, length);

        /// <summary>Pieza delgada perpendicular a <paramref name="normal"/> (grosor en esa dirección).</summary>
        static Vector3 Thin(Vector3 normal, float thickness, float height, float length)
            => Mathf.Abs(normal.x) > 0.5f ? new Vector3(thickness, height, length) : new Vector3(length, height, thickness);

        static void Hide(MeshRenderer r)
        {
            r.enabled = false;
            // Sin malla visible no aporta al horneado; el collider se queda.
            GameObjectUtility.SetStaticEditorFlags(r.gameObject,
                GameObjectUtility.GetStaticEditorFlags(r.gameObject) & ~StaticEditorFlags.ContributeGI & ~StaticEditorFlags.BatchingStatic);
        }

        /// <summary>Pieza en el espacio del bloque: posición en metros desde su centro, tamaño en metros, misma rotación.</summary>
        static GameObject Part(Transform box, string name, Vector3 localMeters, Vector3 size, Material mat, bool shadows = true)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(_group, false);
            var rot = box.rotation;
            go.transform.SetPositionAndRotation(box.position + rot * localMeters, rot);
            go.transform.localScale = size;
            Setup(go, mat, shadows);
            return go;
        }

        static GameObject World(string name, Vector3 pos, Vector3 size, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(_group, false);
            go.transform.position = pos;
            go.transform.localScale = size;
            Setup(go, mat, true);
            return go;
        }

        static void Setup(GameObject go, Material mat, bool shadows)
        {
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            // Las piezas grandes entran al horneado (BevelBoxes les da UV2); las ranuras y manijas usan las sondas de luz.
            var s = go.transform.localScale;
            bool big = shadows && Mathf.Min(s.x, Mathf.Min(s.y, s.z)) >= 0.03f;
            GameObjectUtility.SetStaticEditorFlags(go, big ? StaticEditorFlags.BatchingStatic | StaticEditorFlags.ContributeGI : StaticEditorFlags.BatchingStatic);
        }
    }
}
