using System;
using UnityEngine;

namespace Caso404.Core.Events
{
    /// <summary>
    /// Canal de evento global sin parámetros (ej. "partida cargada").
    /// Quien emite llama a Raise(); quien escucha se suscribe a OnRaised. Ninguno conoce al otro.
    /// </summary>
    [CreateAssetMenu(menuName = "Caso404/Eventos/Canal simple", fileName = "evt_")]
    public class GameEventChannel : ScriptableObject
    {
        public event Action OnRaised;

        /// <summary>Emite el evento a todos los suscriptores.</summary>
        public void Raise() => OnRaised?.Invoke();
    }
}
