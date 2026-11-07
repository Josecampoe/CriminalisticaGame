using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Caso404.Player
{
    /// <summary>
    /// Único punto de lectura de controles (Input System). Define las acciones en código para no depender
    /// de un asset externo, y las expone como valores y eventos. Teclado + mouse y mando.
    /// </summary>
    public class InputReader : MonoBehaviour
    {
        public event Action InteractPressed;
        public event Action JournalPressed;
        public event Action AlexPressed;
        public event Action HypothesisPressed;
        public event Action CancelPressed;

        InputAction _move, _look, _interact, _journal, _alex, _hypothesis, _cancel, _rotateHold, _zoom;

        public Vector2 Move => _move.ReadValue<Vector2>();
        public Vector2 Look => _look.ReadValue<Vector2>();
        /// <summary>Botón para girar el objeto en el modo inspección (clic izquierdo o gatillo).</summary>
        public bool RotateHeld => _rotateHold.IsPressed();
        /// <summary>Rueda del mouse (positivo = acercar). Se usa en el modo inspección.</summary>
        public float Zoom => _zoom.ReadValue<float>();

        void Awake()
        {
            _move = new InputAction("Move", InputActionType.Value);
            _move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            _move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
            _move.AddBinding("<Gamepad>/leftStick");

            _look = new InputAction("Look", InputActionType.Value);
            _look.AddBinding("<Mouse>/delta");
            _look.AddBinding("<Gamepad>/rightStick").WithProcessor("scaleVector2(x=12,y=12)");

            _interact = Button("Interact", "<Keyboard>/e", "<Gamepad>/buttonSouth");
            _journal = Button("Journal", "<Keyboard>/tab", "<Gamepad>/select");
            _alex = Button("Alex", "<Keyboard>/q", "<Gamepad>/buttonNorth");
            _hypothesis = Button("Hypothesis", "<Keyboard>/h", "<Gamepad>/buttonWest");
            _cancel = Button("Cancel", "<Keyboard>/escape", "<Gamepad>/buttonEast");
            _rotateHold = Button("RotateHold", "<Mouse>/leftButton", "<Gamepad>/rightTrigger");
            _zoom = new InputAction("Zoom", InputActionType.Value);
            _zoom.AddBinding("<Mouse>/scroll/y");
        }

        void OnEnable()
        {
            foreach (var a in All()) a.Enable();
            _interact.performed += OnInteract;
            _journal.performed += OnJournal;
            _alex.performed += OnAlex;
            _hypothesis.performed += OnHypothesis;
            _cancel.performed += OnCancel;
        }

        void OnDisable()
        {
            _interact.performed -= OnInteract;
            _journal.performed -= OnJournal;
            _alex.performed -= OnAlex;
            _hypothesis.performed -= OnHypothesis;
            _cancel.performed -= OnCancel;
            foreach (var a in All()) a.Disable();
        }

        void OnDestroy()
        {
            foreach (var a in All()) a.Dispose();
        }

        void OnInteract(InputAction.CallbackContext _) => InteractPressed?.Invoke();
        void OnJournal(InputAction.CallbackContext _) => JournalPressed?.Invoke();
        void OnAlex(InputAction.CallbackContext _) => AlexPressed?.Invoke();
        void OnHypothesis(InputAction.CallbackContext _) => HypothesisPressed?.Invoke();
        void OnCancel(InputAction.CallbackContext _) => CancelPressed?.Invoke();

        InputAction[] All() => new[] { _move, _look, _interact, _journal, _alex, _hypothesis, _cancel, _rotateHold, _zoom };

        static InputAction Button(string name, params string[] bindings)
        {
            var a = new InputAction(name, InputActionType.Button);
            foreach (var b in bindings) a.AddBinding(b);
            return a;
        }
    }
}
