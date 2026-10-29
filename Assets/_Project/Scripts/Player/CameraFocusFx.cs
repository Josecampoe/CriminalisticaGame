using UnityEngine;
using UnityEngine.Rendering;

namespace Caso404.Player
{
    /// <summary>
    /// Efecto de cámara al conversar: cuando la vista se fija en un personaje, la cámara se acerca (baja el campo de visión)
    /// y sube el peso de un Volume con profundidad de campo, que desenfoca el fondo. Al terminar vuelve a la normalidad.
    /// También aplica el balanceo suave al caminar (head bob), si está activado en Opciones.
    /// </summary>
    public class CameraFocusFx : MonoBehaviour
    {
        [SerializeField] private FirstPersonController _controller;
        [SerializeField] private Camera _camera;
        [SerializeField] private Volume _focusVolume;
        [Tooltip("Lo que se balancea al caminar: el pivote de la cámara (la cámara misma la usa CameraShake).")]
        [SerializeField] private Transform _bobTarget;
        [SerializeField] private float _normalFov = 65f;
        [SerializeField] private float _focusFov = 50f;
        [SerializeField] private float _blendSpeed = 3f;
        [SerializeField] private float _bobAmplitude = 0.025f;
        [SerializeField] private float _bobFrequency = 1.9f;

        float _blend;
        float _bobPhase;
        float _bobWeight;
        Vector3 _baseLocalPos;
        bool _headBob;
        bool _wasLocked;

        void Awake()
        {
            if (_camera == null) _camera = GetComponent<Camera>();
            if (_bobTarget == null) _bobTarget = transform.parent != null ? transform.parent : transform;
            _baseLocalPos = _bobTarget.localPosition;
        }

        void OnEnable() => RefreshSettings();

        /// <summary>Lee la preferencia de head bob (al empezar y al cerrar paneles).</summary>
        public void RefreshSettings() => _headBob = Core.GameSettings.HeadBob;

        void LateUpdate()
        {
            if (_controller == null) return;
            float target = _controller.IsFocusing ? 1f : 0f;
            if (Mathf.Abs(_blend - target) > 0.0001f)
            {
                _blend = Mathf.MoveTowards(_blend, target, Time.unscaledDeltaTime * _blendSpeed);
                float s = Mathf.SmoothStep(0f, 1f, _blend);
                if (_camera != null) _camera.fieldOfView = Mathf.Lerp(_normalFov, _focusFov, s);
                if (_focusVolume != null) _focusVolume.weight = s;
            }
            if (_wasLocked && !_controller.IsLocked) RefreshSettings(); // al cerrar un panel (p. ej. Opciones)
            _wasLocked = _controller.IsLocked;
            UpdateBob();
        }

        void UpdateBob()
        {
            float speed = _headBob ? _controller.PlanarSpeed : 0f;
            _bobWeight = Mathf.MoveTowards(_bobWeight, speed > 0.1f ? 1f : 0f, Time.deltaTime * 4f);
            if (_bobWeight <= 0f) { _bobTarget.localPosition = _baseLocalPos; return; }
            _bobPhase += Time.deltaTime * _bobFrequency * Mathf.PI * 2f * Mathf.Clamp01(speed / 2.2f + 0.3f);
            var offset = new Vector3(Mathf.Cos(_bobPhase * 0.5f) * _bobAmplitude * 0.6f, Mathf.Abs(Mathf.Sin(_bobPhase)) * _bobAmplitude, 0f);
            _bobTarget.localPosition = _baseLocalPos + offset * _bobWeight;
        }
    }
}
