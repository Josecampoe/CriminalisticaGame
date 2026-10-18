using System;
using UnityEngine;

namespace Caso404.Core.Events
{
    /// <summary>Canal de evento global con un id (ej. "evidencia recogida: ev_c1_huella").</summary>
    [CreateAssetMenu(menuName = "Caso404/Eventos/Canal con id", fileName = "evt_")]
    public class StringEventChannel : ScriptableObject
    {
        public event Action<string> OnRaised;

        /// <summary>Emite el evento con un id.</summary>
        public void Raise(string id) => OnRaised?.Invoke(id);
    }
}
