using System;
using UnityEngine;

namespace Caso404.Core.Events
{
    /// <summary>Canal de evento global con un valor bool (ej. "hay un panel modal abierto").</summary>
    [CreateAssetMenu(menuName = "Caso404/Eventos/Canal bool", fileName = "evt_")]
    public class BoolEventChannel : ScriptableObject
    {
        public event Action<bool> OnRaised;

        /// <summary>Último valor emitido (para quien se suscribe tarde).</summary>
        public bool LastValue { get; private set; }

        /// <summary>Emite el valor a todos los suscriptores.</summary>
        public void Raise(bool value)
        {
            LastValue = value;
            OnRaised?.Invoke(value);
        }

        void OnEnable() => LastValue = false;
    }
}
