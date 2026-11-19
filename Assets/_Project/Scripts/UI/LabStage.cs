using Caso404.Data;
using UnityEngine;

namespace Caso404.UI
{
    /// <summary>
    /// Mesa del laboratorio en 3D: un estudio lejos de la escena (como el de la inspección) con una mesa blanca, luz
    /// clínica y una cámara que lo filma en una RenderTexture. La evidencia gira despacio sobre la mesa y se puede girar
    /// arrastrando con el mouse. Durante el análisis, una luz azul de escáner la recorre de abajo arriba.
    /// Solo presenta: no cambia el estado del juego.
    /// </summary>
    public class LabStage : MonoBehaviour
    {
        const string LayerName = "Inspection";
        static readonly Vector3 StudioPosition = new(0f, -800f, 0f);
        const float ObjectSize = 0.42f;   // el objeto se escala para caber en ~42 cm
        const float AutoSpin = 14f;       // grados por segundo

        Camera _camera;
        RenderTexture _texture;
        Transform _turntable;
        GameObject _model;
        Light _scanner;
        float _scanT = -1f;
        float _idleTimer;
        int _layer;
        Material _tableMat, _bagMat, _labelMat;

        /// <summary>Imagen de la mesa (la muestra la UI en un RawImage).</summary>
        public RenderTexture Texture => _texture;

        /// <summary>Crea el estudio. <paramref name="background"/>: color de fondo de la imagen.</summary>
        public void Build(Color background)
        {
            _layer = LayerMask.NameToLayer(LayerName);
            if (_layer < 0) _layer = 0;
            transform.position = StudioPosition;

            _texture = new RenderTexture(1280, 960, 24) { name = "LabRT", antiAliasing = 4 };
            var camGo = new GameObject("LabCamera");
            camGo.transform.SetParent(transform, false);
            camGo.transform.localPosition = new Vector3(0f, 0.5f, -1.15f);
            camGo.transform.localRotation = Quaternion.Euler(14f, 0f, 0f);
            _camera = camGo.AddComponent<Camera>();
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = background;
            _camera.cullingMask = 1 << _layer;
            _camera.fieldOfView = 30f;
            _camera.nearClipPlane = 0.05f;
            _camera.farClipPlane = 6f;
            _camera.targetTexture = _texture;
            _camera.enabled = false;

            var lit = Shader.Find("Universal Render Pipeline/Lit");
            _tableMat = new Material(lit) { color = new Color(0.93f, 0.95f, 0.97f) };
            _tableMat.SetFloat("_Smoothness", 0.75f);
            _bagMat = new Material(lit) { color = new Color(0.62f, 0.5f, 0.34f) };
            _labelMat = new Material(lit) { color = new Color(0.95f, 0.95f, 0.92f) };

            // Mesa: disco blanco brillante con un borde azul.
            var table = Prim(PrimitiveType.Cylinder, "Mesa", transform, new Vector3(0f, -0.02f, 0f), new Vector3(0.9f, 0.02f, 0.9f), _tableMat);
            var rimMat = new Material(lit) { color = new Color(0.17f, 0.47f, 0.72f) };
            Prim(PrimitiveType.Cylinder, "Borde", table.transform.parent, new Vector3(0f, -0.045f, 0f), new Vector3(0.93f, 0.01f, 0.93f), rimMat);

            _turntable = new GameObject("Plato").transform;
            _turntable.SetParent(transform, false);

            // Luz clínica: principal blanca fría, relleno suave y contraluz azul.
            AddLight("Principal", new Vector3(0.5f, 0.9f, -0.7f), new Color(0.95f, 0.97f, 1f), 3.2f, 3f);
            AddLight("Relleno", new Vector3(-0.8f, 0.4f, -0.6f), new Color(0.85f, 0.9f, 1f), 1.2f, 3f);
            AddLight("Contraluz", new Vector3(0f, 0.6f, 0.8f), new Color(0.45f, 0.7f, 1f), 2.2f, 3f);
            _scanner = AddLight("Escaner", Vector3.zero, new Color(0.3f, 0.65f, 1f), 0f, 0.9f);
        }

        /// <summary>Enciende o apaga la cámara (solo filma con el laboratorio abierto).</summary>
        public void SetActive(bool on)
        {
            if (_camera != null) _camera.enabled = on;
        }

        /// <summary>Pone la evidencia sobre la mesa (su modelo de inspección o, si no tiene, una bolsa de evidencia).</summary>
        public void Show(EvidenceData evidence)
        {
            if (_model != null) Destroy(_model);
            _model = null;
            _turntable.localRotation = Quaternion.identity;
            if (evidence == null) return;
            _model = evidence.InspectPrefab != null ? Copy(evidence.InspectPrefab) : EvidenceBag();
            FitOnTable(_model);
        }

        /// <summary>Gira el objeto (arrastre del mouse sobre la imagen).</summary>
        public void Drag(Vector2 delta)
        {
            _turntable.Rotate(Vector3.up, -delta.x * 0.4f, Space.World);
            _idleTimer = 2.5f;
        }

        /// <summary>Progreso del escáner (0 a 1); un valor negativo lo apaga.</summary>
        public void SetScan(float t) => _scanT = t;

        void Update()
        {
            if (_camera == null || !_camera.enabled) return;
            if (_idleTimer > 0f) _idleTimer -= Time.unscaledDeltaTime;
            else _turntable.Rotate(Vector3.up, AutoSpin * Time.unscaledDeltaTime, Space.World);

            if (_scanT >= 0f)
            {
                _scanner.intensity = 4f;
                // Sube y baja dos veces durante el análisis, por delante del objeto.
                float y = Mathf.PingPong(_scanT * 2f, 1f) * ObjectSize;
                _scanner.transform.localPosition = new Vector3(0f, y, -0.3f);
            }
            else _scanner.intensity = 0f;
        }

        void OnDestroy()
        {
            if (_texture != null) _texture.Release();
            foreach (var m in new[] { _tableMat, _bagMat, _labelMat }) if (m != null) Destroy(m);
        }

        GameObject Copy(GameObject source)
        {
            // Se copia dentro de un padre inactivo para que los scripts de la copia no se inicien.
            var holder = new GameObject("Holder");
            holder.SetActive(false);
            var copy = Instantiate(source, holder.transform);
            foreach (var c in copy.GetComponentsInChildren<Collider>(true)) DestroyImmediate(c);
            foreach (var b in copy.GetComponentsInChildren<MonoBehaviour>(true)) DestroyImmediate(b);
            foreach (var l in copy.GetComponentsInChildren<Light>(true)) DestroyImmediate(l);
            copy.transform.SetParent(_turntable, false);
            Destroy(holder);
            copy.SetActive(true);
            return copy;
        }

        /// <summary>Evidencia sin modelo (entregada en un diálogo o derivada de otro análisis): sobre de evidencia con etiqueta.</summary>
        GameObject EvidenceBag()
        {
            var bag = new GameObject("Sobre de evidencia");
            bag.transform.SetParent(_turntable, false);
            Prim(PrimitiveType.Cube, "Sobre", bag.transform, new Vector3(0f, 0.01f, 0f), new Vector3(0.28f, 0.02f, 0.36f), _bagMat);
            Prim(PrimitiveType.Cube, "Etiqueta", bag.transform, new Vector3(0f, 0.021f, 0.05f), new Vector3(0.18f, 0.003f, 0.12f), _labelMat);
            bag.transform.localRotation = Quaternion.Euler(-55f, 0f, 0f);
            return bag;
        }

        /// <summary>Centra el objeto sobre el plato, lo escala a ~42 cm y lo apoya en la mesa.</summary>
        void FitOnTable(GameObject model)
        {
            SetLayer(model, _layer);
            var renderers = model.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return;
            var b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            float size = Mathf.Max(b.size.x, b.size.y, b.size.z);
            float scale = size > 0.0001f ? ObjectSize / size : 1f;
            model.transform.localScale *= scale;
            // Objetos planos (huella, papeles, teléfono): se inclinan hacia la cámara para que no se vean de canto.
            float min = Mathf.Min(b.size.x, b.size.y, b.size.z);
            if (min < size * 0.3f)
            {
                // Lo delgado hacia la cámara: los objetos planos se ven de frente y no de canto.
                if (Mathf.Approximately(min, b.size.y)) model.transform.Rotate(Vector3.right, -50f, Space.World);
                else if (Mathf.Approximately(min, b.size.x)) model.transform.Rotate(Vector3.up, 90f, Space.World);
            }
            b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            var pivot = _turntable.position;
            model.transform.position += new Vector3(pivot.x - b.center.x, pivot.y - b.min.y + 0.005f, pivot.z - b.center.z);
        }

        Light AddLight(string name, Vector3 localPos, Color color, float intensity, float range)
        {
            var l = new GameObject(name).AddComponent<Light>();
            l.transform.SetParent(transform, false);
            l.transform.localPosition = localPos;
            l.type = LightType.Point;
            l.color = color;
            l.intensity = intensity;
            l.range = range;
            l.shadows = LightShadows.None;
            l.cullingMask = 1 << _layer; // no ilumina la escena
            return l;
        }

        GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 localPos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            go.layer = _layer;
            return go;
        }

        static void SetLayer(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform t in go.transform) SetLayer(t.gameObject, layer);
        }
    }
}
