using Caso404.Core.Events;
using UnityEngine;

namespace Caso404.Player
{
    /// <summary>
    /// Movimiento en primera persona con CharacterController: caminar y mirar, sin correr ni saltar
    /// (es un juego de investigación). Se bloquea mientras haya un panel abierto.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class FirstPersonController : MonoBehaviour
    {
        [SerializeField] private InputReader _input;
        [SerializeField] private Transform _cameraPivot;
        [SerializeField] private BoolEventChannel _uiModalChannel;
        [SerializeField] private float _walkSpeed = 2.2f;
        [SerializeField] private float _lookSensitivity = 0.12f;
        [SerializeField] private float _gravity = -9.81f;
        [SerializeField] private float _maxPitch = 80f;
        [SerializeField] private float _focusSpeed = 6f;

        CharacterController _controller;
        float _pitch;
        float _verticalVelocity;
        bool _locked;
        Transform _focusTarget;
        float _sensitivityScale = 1f;
        bool _invertY;

        /// <summary>True mientras un panel o el modo inspección bloquean el movimiento.</summary>
        public bool IsLocked => _locked;

        /// <summary>True mientras la vista está fija en un personaje (conversación).</summary>
        public bool IsFocusing => _locked && _focusTarget != null;

        /// <summary>Velocidad horizontal actual en m/s (para el balanceo de la cámara).</summary>
        public float PlanarSpeed { get; private set; }

        void Awake()
        {
            _controller = GetComponent<CharacterController>();
            if (_input == null) Caso404.Core.GameLog.Error($"{name}: falta InputReader", this);
            if (_cameraPivot == null) Caso404.Core.GameLog.Error($"{name}: falta Camera Pivot", this);
        }

        void OnEnable()
        {
            if (_uiModalChannel != null) _uiModalChannel.OnRaised += SetLocked;
            SetLocked(_uiModalChannel != null && _uiModalChannel.LastValue);
        }

        void OnDisable()
        {
            if (_uiModalChannel != null) _uiModalChannel.OnRaised -= SetLocked;
        }

        /// <summary>Bloquea o libera el movimiento y el cursor.</summary>
        public void SetLocked(bool locked)
        {
            if (!locked) RefreshSettings();
            _locked = locked;
            Cursor.lockState = locked ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = locked;
            if (!locked) _focusTarget = null;
        }

        /// <summary>Lee las preferencias del jugador (se llama al empezar y al cerrar paneles, no en cada frame).</summary>
        void RefreshSettings()
        {
            _sensitivityScale = Core.GameSettings.Sensitivity / Core.GameSettings.DefaultSensitivity;
            _invertY = Core.GameSettings.InvertY;
        }

        /// <summary>Gira la vista suavemente hacia un punto (ej. el rostro del personaje al hablar).</summary>
        public void FocusOn(Transform target) => _focusTarget = target;

        void Update()
        {
            if (_input == null) return;
            ApplyGravity();
            if (_locked)
            {
                PlanarSpeed = 0f;
                UpdateFocus();
                return;
            }
            Look(_input.Look);
            Move(_input.Move);
        }

        void Look(Vector2 delta)
        {
            float sens = _lookSensitivity * _sensitivityScale;
            float y = _invertY ? -delta.y : delta.y;
            transform.Rotate(0f, delta.x * sens, 0f);
            _pitch = Mathf.Clamp(_pitch - y * sens, -_maxPitch, _maxPitch);
            _cameraPivot.localEulerAngles = new Vector3(_pitch, 0f, 0f);
        }

        void Move(Vector2 input)
        {
            var dir = transform.right * input.x + transform.forward * input.y;
            if (dir.sqrMagnitude > 1f) dir.Normalize();
            PlanarSpeed = dir.magnitude * _walkSpeed;
            _controller.Move((dir * _walkSpeed + Vector3.up * _verticalVelocity) * Time.deltaTime);
        }

        void ApplyGravity()
        {
            if (_controller.isGrounded && _verticalVelocity < 0f) _verticalVelocity = -1f;
            else _verticalVelocity += _gravity * Time.deltaTime;
            if (_locked) _controller.Move(Vector3.up * _verticalVelocity * Time.deltaTime);
        }

        void UpdateFocus()
        {
            if (_focusTarget == null) return;
            var toTarget = _focusTarget.position - _cameraPivot.position;
            var flat = new Vector3(toTarget.x, 0f, toTarget.z);
            if (flat.sqrMagnitude < 0.001f) return;
            var targetYaw = Quaternion.LookRotation(flat);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetYaw, Time.deltaTime * _focusSpeed);
            float targetPitch = -Mathf.Atan2(toTarget.y, flat.magnitude) * Mathf.Rad2Deg;
            _pitch = Mathf.Lerp(_pitch, Mathf.Clamp(targetPitch, -_maxPitch, _maxPitch), Time.deltaTime * _focusSpeed);
            _cameraPivot.localEulerAngles = new Vector3(_pitch, 0f, 0f);
        }
    }
}
