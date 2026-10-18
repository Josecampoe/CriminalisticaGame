using System.Collections;
using UnityEngine;

namespace Caso404.UI
{
    /// <summary>
    /// Base de todas las pantallas: se construye por código la primera vez, se abre y cierra con un fundido corto
    /// (150 ms) y se registra en el UIManager, que lleva la pila de paneles abiertos.
    /// Las vistas solo muestran datos y emiten intenciones; no contienen lógica de juego.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class UIPanel : MonoBehaviour
    {
        [SerializeField] protected UITheme _theme;
        [SerializeField] protected UIManager _ui;
        [SerializeField] private float _fadeSeconds = 0.15f;

        CanvasGroup _group;
        bool _built;
        Coroutine _fade;

        public bool IsOpen { get; private set; }

        /// <summary>Si es true, bloquea al jugador y libera el cursor mientras está abierto.</summary>
        public virtual bool BlocksGameplay => true;

        /// <summary>Si es true, Escape lo cierra.</summary>
        public virtual bool ClosesWithEscape => true;

        protected virtual void Awake()
        {
            _group = GetComponent<CanvasGroup>();
            EnsureBuilt();
            ApplyVisible(false, instant: true);
        }

        /// <summary>Construye la jerarquía de la vista (una sola vez).</summary>
        protected abstract void Build();

        protected virtual void OnShown() { }
        protected virtual void OnHidden() { }

        /// <summary>Abre el panel (a través del UIManager).</summary>
        public void Open()
        {
            if (_ui != null) _ui.Push(this);
            else ShowInternal();
        }

        /// <summary>Cierra el panel (a través del UIManager).</summary>
        public void Close()
        {
            if (_ui != null) _ui.Remove(this);
            else HideInternal();
        }

        internal void ShowInternal()
        {
            EnsureBuilt();
            transform.SetAsLastSibling();
            IsOpen = true;
            ApplyVisible(true, instant: false);
            OnShown();
        }

        internal void HideInternal()
        {
            if (!IsOpen) return;
            IsOpen = false;
            ApplyVisible(false, instant: false);
            OnHidden();
        }

        void EnsureBuilt()
        {
            if (_built) return;
            _built = true;
            if (_theme == null) Core.GameLog.Error($"{name}: falta el UITheme.", this);
            Build();
        }

        void ApplyVisible(bool visible, bool instant)
        {
            _group.interactable = visible;
            _group.blocksRaycasts = visible;
            if (_fade != null) StopCoroutine(_fade);
            if (instant || !isActiveAndEnabled || _fadeSeconds <= 0f) { _group.alpha = visible ? 1f : 0f; return; }
            _fade = StartCoroutine(Fade(visible ? 1f : 0f));
        }

        IEnumerator Fade(float target)
        {
            float start = _group.alpha, t = 0f;
            while (t < _fadeSeconds)
            {
                t += Time.unscaledDeltaTime;
                float k = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / _fadeSeconds), 3f); // ease-out
                _group.alpha = Mathf.Lerp(start, target, k);
                yield return null;
            }
            _group.alpha = target;
        }
    }
}
