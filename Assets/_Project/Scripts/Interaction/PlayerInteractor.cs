using System;
using Caso404.Player;
using UnityEngine;

namespace Caso404.Interaction
{
    /// <summary>
    /// Busca qué interactivo apunta la cámara (hasta 3 m), lo resalta, avisa al HUD y ejecuta la interacción al pulsar E.
    /// Primero lanza un rayo fino; si no encuentra nada, prueba con una esfera pequeña para perdonar la puntería en
    /// objetos chicos. Solo las paredes (colliders altos) tapan: mesas, sillas y mostradores no, para poder agarrar
    /// algo de una mesa desde muy cerca o hablar con alguien sentado detrás de un escritorio.
    /// </summary>
    public class PlayerInteractor : MonoBehaviour
    {
        /// <summary>Cambió el objeto apuntado (null = nada). El HUD muestra su prompt.</summary>
        public event Action<IInteractable> FocusChanged;

        [SerializeField] private InputReader _input;
        [SerializeField] private PlayerContext _context;
        [SerializeField] private Camera _camera;
        [Tooltip("Distancia máxima desde los ojos. 3 m alcanza un objeto en el piso a unos 2.5 m de los pies.")]
        [SerializeField] private float _range = 3f;
        [Tooltip("Radio de la esfera de respaldo cuando el rayo no toca nada (perdona la puntería).")]
        [SerializeField] private float _aimRadius = 0.12f;
        [Tooltip("Capa Interactable + capas que pueden tapar (Default).")]
        [SerializeField] private LayerMask _interactableMask;

        /// <summary>Un collider que no es interactivo tapa la vista solo si es al menos así de alto (paredes, puertas).</summary>
        public const float BlockerMinHeight = 1.3f;
        /// <summary>Margen para objetos pegados a una pared (ventana, post-it): la pared no los tapa.</summary>
        const float FlushTolerance = 0.12f;

        readonly RaycastHit[] _hits = new RaycastHit[24];
        int _interactableLayer;
        Collider _currentCollider;
        IInteractable _current;
        Highlightable _currentHighlight;

        public IInteractable Current => _current;
        public float Range => _range;

        void Awake() => _interactableLayer = LayerMask.NameToLayer("Interactable");

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
            SetFocus(FindTarget());
            if (_currentHighlight != null && _current != null) _currentHighlight.SetHighlight(true, _current.IsNew);
        }

        /// <summary>Collider interactivo al que apunta la cámara ahora mismo (null si no hay).</summary>
        public Collider FindTarget()
        {
            var ray = new Ray(_camera.transform.position, _camera.transform.forward);
            var target = Cast(ray, 0f, out bool blocked);
            if (target == null && !blocked && _aimRadius > 0f) target = Cast(ray, _aimRadius, out _);
            return target;
        }

        Collider Cast(Ray ray, float radius, out bool blocked)
        {
            int n = radius > 0f
                ? Physics.SphereCastNonAlloc(ray, radius, _hits, _range, _interactableMask, QueryTriggerInteraction.Collide)
                : Physics.RaycastNonAlloc(ray, _hits, _range, _interactableMask, QueryTriggerInteraction.Collide);
            Collider best = null;
            float bestDistance = float.MaxValue, blocker = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                var h = _hits[i];
                var col = h.collider;
                if (col.transform.IsChildOf(transform)) continue; // el propio jugador
                if (col.gameObject.layer == _interactableLayer)
                {
                    if (h.distance < bestDistance) { best = col; bestDistance = h.distance; }
                }
                // Una esfera que ya empieza tocando una pared (distancia 0) no cuenta como tapada.
                else if (col.bounds.size.y >= BlockerMinHeight && (radius <= 0f || h.distance > 0f))
                    blocker = Mathf.Min(blocker, h.distance);
            }
            // Sin interactivo, cualquier pared en el rayo cuenta como tapada (no se prueba la esfera a través de ella).
            blocked = best == null ? blocker < float.MaxValue : blocker + FlushTolerance < bestDistance;
            return blocked ? null : best;
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
            if (_context.Controller != null && (_context.Controller.IsLocked || _context.Controller.UnlockedThisFrame)) return;
            _current.Interact(_context);
        }

        /// <summary>Vuelve a notificar el foco actual (cuando cambia el estado del objeto, ej. tras recogerlo).</summary>
        public void RefreshFocus() => FocusChanged?.Invoke(_current);
    }
}
