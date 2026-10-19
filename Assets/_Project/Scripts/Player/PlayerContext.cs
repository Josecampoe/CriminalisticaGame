using System;
using Caso404.Data;
using UnityEngine;

namespace Caso404.Player
{
    /// <summary>
    /// Lo que un objeto interactivo puede pedir al jugador. Solo emite intenciones:
    /// quien sabe resolverlas (inspección, diálogo, tablero) se suscribe. Así los objetos no conocen la UI.
    /// </summary>
    public class PlayerContext : MonoBehaviour
    {
        /// <summary>Inspeccionar un objeto: la evidencia y el objeto de la escena que se va a girar.</summary>
        public event Action<EvidenceData, GameObject> InspectRequested;
        /// <summary>Hablar con un personaje: su perfil y el punto de su cabeza (para enfocar la cámara).</summary>
        public event Action<CharacterProfile, Transform> TalkRequested;
        /// <summary>Abrir el tablero de hipótesis y cerrar la escena.</summary>
        public event Action SceneCloseRequested;
        /// <summary>Abrir la pantalla de acusación (Comisaría).</summary>
        public event Action AccusationRequested;
        /// <summary>Abrir el panel de ALEX (pantalla de ALEX en la Comisaría).</summary>
        public event Action AlexRequested;

        [SerializeField] private FirstPersonController _controller;
        [SerializeField] private Camera _camera;

        public FirstPersonController Controller => _controller;
        public Camera Camera => _camera;

        public void RequestInspect(EvidenceData evidence, GameObject source) => InspectRequested?.Invoke(evidence, source);
        public void RequestTalk(CharacterProfile character, Transform head) => TalkRequested?.Invoke(character, head);
        public void RequestSceneClose() => SceneCloseRequested?.Invoke();
        public void RequestAccusation() => AccusationRequested?.Invoke();
        public void RequestAlex() => AlexRequested?.Invoke();
    }
}
