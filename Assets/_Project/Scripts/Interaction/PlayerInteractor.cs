using System;
using Caso404.Player;
using UnityEngine;

namespace Caso404.Interaction
{
    /// <summary>
    /// Lanza un rayo desde el centro de la cámara (2.5 m, solo capa Interactable). Resalta lo que apunta,
    /// avisa al HUD y ejecuta la interacción al pulsar E.
    /// </summary>
    public class PlayerInteractor : MonoBehaviour
    {
        /// <summary>Cambió el objeto apuntado (null = nada). El HUD muestra su prompt.</summary>
        public event Action<IInteractable> FocusChanged;

        [SerializeField] private InputReader _input;
        [SerializeField] private PlayerContext _context;
        [SerializeField] private Camera _camera;
        [SerializeField] private float _range = 2.5f;
        [SerializeField] private LayerMask _interactableMask;

        Collider _currentCollider;
        IInteractable _current;
        Highlightable _currentHighlight;

        public IInteractable Current => _current;

        void OnEnable()
        {
            if (_input != null) _input.InteractPressed += OnInteract;
        }

        void OnDisable()
        {
            if (_input != null) _input.InteractPressed -= OnInteract;
            SetFocus(null);
        }

        void Update()
        {
            if (_context.Controller != null && _context.Controller.IsLocked)
            {
                SetFocus(null);
                return;
            }
            var ray = new Ray(_camera.transform.position, _camera.transform.forward);
            var hitSomething = Physics.Raycast(ray, out var hit, _range, _interactableMask, QueryTriggerInteraction.Collide);
            SetFocus(hitSomething ? hit.collider : null);
            if (_currentHighlight != null && _current != null) _currentHighlight.SetHighlight(true, _current.IsNew);
        }

        void SetFocus(Collider col)
        {
            if (col == _currentCollider) return;
            if (_currentHighlight != null) _currentHighlight.SetHighlight(false, false);
            _currentCollider = col;
            _current = null;
            _currentHighlight = null;
            // GetComponent solo cuando cambia el objeto apuntado, no en cada frame.
            if (col != null)
            {
                _current = col.GetComponentInParent<IInteractable>();
                _currentHighlight = col.GetComponentInParent<Highlightable>();
            }
            FocusChanged?.Invoke(_current);
        }

        void OnInteract()
        {
            if (_current == null || !_current.CanInteract) return;
            if (_context.Controller != null && _context.Controller.IsLocked) return;
            _current.Interact(_context);
        }

        /// <summary>Vuelve a notificar el foco actual (cuando cambia el estado del objeto, ej. tras recogerlo).</summary>
        public void RefreshFocus() => FocusChanged?.Invoke(_current);
    }
}
