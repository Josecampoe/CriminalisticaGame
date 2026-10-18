using Caso404.Core;
using Caso404.Data;
using Caso404.Interaction;
using Caso404.Player;
using UnityEngine;

namespace Caso404.Evidence
{
    /// <summary>Objeto de la escena que entrega una evidencia al inspeccionarlo. Se puede volver a revisar.</summary>
    public class EvidencePickup : MonoBehaviour, IInteractable
    {
        [SerializeField] private EvidenceData _evidence;
        [SerializeField] private GameContext _game;

        public EvidenceData Evidence => _evidence;

        public string Prompt => _game.Evidence.IsFound(_evidence)
            ? $"Revisar: {_evidence.DisplayName}"
            : $"Inspeccionar: {_evidence.DisplayName}";

        public bool CanInteract => _evidence != null && _game != null && _game.Evidence.IsAvailable(_evidence);

        public bool IsNew => !_game.Evidence.IsFound(_evidence);

        void Awake()
        {
            if (_evidence == null || _game == null) GameLog.Error($"{name}: asigna Evidence y Game en el Inspector.", this);
        }

        void Start()
        {
            // Si la historia dice que no existe (ej. el overol quemado), no aparece.
            if (_game != null && !_game.Evidence.IsAvailable(_evidence)) gameObject.SetActive(false);
        }

        public void Interact(PlayerContext ctx) => ctx.RequestInspect(_evidence, gameObject);
    }
}
