using System;
using Caso404.Core;
using Caso404.Data;
using Caso404.Player;
using UnityEngine;

namespace Caso404.Interaction
{
    /// <summary>
    /// Modo inspección: copia el objeto a un "estudio" lejos de la escena, lo filma con una cámara propia
    /// en una RenderTexture (que la UI muestra) y deja girarlo con el mouse. Si la evidencia tiene un detalle
    /// oculto, se revela al girarla lo suficiente.
    /// </summary>
    public class InspectionController : MonoBehaviour
    {
        public event Action<EvidenceData> Started;
        public event Action Ended;
        public event Action<EvidenceData> DetailRevealed;

        [SerializeField] private PlayerContext _player;
        [SerializeField] private InputReader _input;
        [SerializeField] private GameContext _game;
        [SerializeField] private string _inspectionLayer = "Inspection";
        [SerializeField] private float _rotateSpeed = 0.35f;
        [SerializeField] private float _revealAngle = 150f;
        [SerializeField] private Vector3 _studioPosition = new(0f, -500f, 0f);

        Camera _studioCamera;
        Transform _studioRoot;
        GameObject _preview;
        RenderTexture _texture;
        float _accumulatedAngle;
        int _layer;

        public EvidenceData Current { get; private set; }
        public bool IsActive => Current != null;
        /// <summary>Imagen de la cámara de inspección (la muestra la UI).</summary>
        public RenderTexture Texture => _texture;

        void Awake()
        {
            _layer = LayerMask.NameToLayer(_inspectionLayer);
            if (_layer < 0) { GameLog.Error($"Falta la capa {_inspectionLayer}. Ejecuta Caso404/Preparar proyecto.", this); _layer = 0; }
            BuildStudio();
        }

        void OnEnable()
        {
            if (_player != null) _player.InspectRequested += Begin;
        }

        void OnDisable()
        {
            if (_player != null) _player.InspectRequested -= Begin;
        }

        void OnDestroy()
        {
            if (_texture != null) _texture.Release();
        }

        /// <summary>Empieza a inspeccionar el objeto de la escena.</summary>
        public void Begin(EvidenceData evidence, GameObject source)
        {
            if (IsActive || evidence == null) return;
            Current = evidence;
            _accumulatedAngle = 0f;
            _preview = CreatePreview(evidence.InspectPrefab != null ? evidence.InspectPrefab : source);
            _studioCamera.enabled = true;
            Started?.Invoke(evidence);
            if (evidence.HasHiddenDetail && _game.State.IsHiddenDetailSeen(evidence.Id)) DetailRevealed?.Invoke(evidence);
        }

        /// <summary>"Guardar en expediente": recoge la evidencia y cierra el modo inspección.</summary>
        public bool Keep()
        {
            if (!IsActive) return false;
            var collected = _game.Evidence.TryCollect(Current);
            End();
            return collected;
        }

        /// <summary>"Soltar": cierra sin guardar.</summary>
        public void End()
        {
            if (!IsActive) return;
            if (_preview != null) Destroy(_preview);
            _preview = null;
            _studioCamera.enabled = false;
            Current = null;
            Ended?.Invoke();
        }

        void Update()
        {
            if (!IsActive || _preview == null || _input == null || !_input.RotateHeld) return;
            var delta = _input.Look * _rotateSpeed;
            _preview.transform.Rotate(_studioCamera.transform.up, -delta.x, Space.World);
            _preview.transform.Rotate(_studioCamera.transform.right, delta.y, Space.World);
            _accumulatedAngle += delta.magnitude;
            if (Current.HasHiddenDetail && _accumulatedAngle >= _revealAngle && !_game.State.IsHiddenDetailSeen(Current.Id))
            {
                _game.Evidence.MarkHiddenDetailSeen(Current);
                DetailRevealed?.Invoke(Current);
            }
        }

        void BuildStudio()
        {
            _studioRoot = new GameObject("InspectionStudio").transform;
            _studioRoot.SetParent(transform, false);
            _studioRoot.position = _studioPosition;

            _texture = new RenderTexture(1024, 1024, 24) { name = "InspectionRT" };
            var camGo = new GameObject("InspectionCamera");
            camGo.transform.SetParent(_studioRoot, false);
            camGo.transform.localPosition = new Vector3(0f, 0f, -1.2f);
            _studioCamera = camGo.AddComponent<Camera>();
            _studioCamera.clearFlags = CameraClearFlags.SolidColor;
            _studioCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            _studioCamera.cullingMask = 1 << _layer;
            _studioCamera.fieldOfView = 30f;
            _studioCamera.nearClipPlane = 0.05f;
            _studioCamera.farClipPlane = 5f;
            _studioCamera.targetTexture = _texture;
            _studioCamera.enabled = false;

            var key = new GameObject("InspectionKeyLight").AddComponent<Light>();
            key.transform.SetParent(_studioRoot, false);
            key.transform.localPosition = new Vector3(0.6f, 0.8f, -1f);
            key.type = LightType.Point;
            key.range = 4f;
            key.intensity = 3f;
            key.color = new Color(1f, 0.93f, 0.82f);

            if (_player != null && _player.Camera != null) _player.Camera.cullingMask &= ~(1 << _layer);
        }

        GameObject CreatePreview(GameObject source)
        {
            // Se copia dentro de un padre inactivo para que los scripts de la copia no se inicien.
            var holder = new GameObject("PreviewHolder");
            holder.SetActive(false);
            var copy = Instantiate(source, holder.transform);
            foreach (var c in copy.GetComponentsInChildren<Collider>(true)) DestroyImmediate(c);
            foreach (var b in copy.GetComponentsInChildren<MonoBehaviour>(true)) DestroyImmediate(b);
            copy.SetActive(true);
            SetLayer(copy, _layer);

            var pivot = new GameObject($"Preview_{source.name}").transform;
            pivot.SetParent(_studioRoot, false);
            copy.transform.SetParent(pivot, true);
            copy.transform.rotation = source.transform.rotation;

            // Centrar y escalar para que quepa en la vista (~0.5 m).
            var renderers = copy.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length > 0)
            {
                copy.transform.position = pivot.position;
                var b = renderers[0].bounds;
                foreach (var r in renderers) b.Encapsulate(r.bounds);
                copy.transform.position += pivot.position - b.center;
                float size = Mathf.Max(b.size.x, b.size.y, b.size.z);
                if (size > 0.0001f) pivot.localScale = Vector3.one * (0.5f / size);
                // Objetos planos (huella, papeles, teléfono): se orientan hacia la cámara para que no se vean de canto.
                float min = Mathf.Min(b.size.x, b.size.y, b.size.z);
                if (min < size * 0.35f)
                {
                    if (Mathf.Approximately(min, b.size.y)) pivot.localRotation = Quaternion.Euler(-65f, 0f, 0f);
                    else if (Mathf.Approximately(min, b.size.x)) pivot.localRotation = Quaternion.Euler(0f, 90f, 0f);
                }
            }
            Destroy(holder);
            return pivot.gameObject;
        }

        static void SetLayer(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform t in go.transform) SetLayer(t.gameObject, layer);
        }
    }
}
