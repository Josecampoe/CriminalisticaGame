using System.Collections.Generic;
using Caso404.Core.Events;
using Caso404.Player;
using UnityEngine;

namespace Caso404.UI
{
    /// <summary>
    /// Pila de paneles abiertos. Escape cierra el de arriba. Avisa por un canal cuándo hay un panel modal abierto
    /// (el jugador se bloquea y se libera el cursor) sin conocer al jugador.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [SerializeField] private InputReader _input;
        [SerializeField] private BoolEventChannel _modalChannel;

        readonly List<UIPanel> _stack = new();

        /// <summary>Frame en el que se cerró el último panel (para que Escape no cierre un panel y abra la pausa a la vez).</summary>
        public int LastCloseFrame { get; private set; } = -1;

        public UIPanel Top => _stack.Count > 0 ? _stack[^1] : null;
        public bool AnyModalOpen => _stack.Exists(p => p.BlocksGameplay);

        void OnEnable()
        {
            if (_input != null) _input.CancelPressed += CloseTop;
        }

        void OnDisable()
        {
            if (_input != null) _input.CancelPressed -= CloseTop;
        }

        void Start() => NotifyModal();

        /// <summary>Abre un panel encima de los demás.</summary>
        public void Push(UIPanel panel)
        {
            if (panel == null) return;
            _stack.Remove(panel);
            _stack.Add(panel);
            panel.ShowInternal();
            NotifyModal();
        }

        /// <summary>Cierra un panel esté donde esté en la pila.</summary>
        public void Remove(UIPanel panel)
        {
            if (panel == null || !_stack.Remove(panel)) return;
            LastCloseFrame = Time.frameCount;
            panel.HideInternal();
            NotifyModal();
        }

        /// <summary>Cierra el panel de arriba si lo permite.</summary>
        public void CloseTop()
        {
            var top = Top;
            if (top != null && top.ClosesWithEscape) Remove(top);
        }

        /// <summary>True si el panel está abierto.</summary>
        public bool IsOpen(UIPanel panel) => _stack.Contains(panel);

        void NotifyModal()
        {
            if (_modalChannel != null) _modalChannel.Raise(AnyModalOpen);
        }
    }
}
