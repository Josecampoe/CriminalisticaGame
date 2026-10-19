using Caso404.Interaction;
using Caso404.Player;
using UnityEngine;

namespace Caso404.Decisions
{
    /// <summary>Objeto de la Comisaría que abre la pantalla de acusación.</summary>
    public class AccusationTrigger : MonoBehaviour, IInteractable
    {
        [SerializeField] private string _prompt = "Presentar la acusación";

        public string Prompt => _prompt;
        public bool CanInteract => true;
        public bool IsNew => true;

        public void Interact(PlayerContext ctx) => ctx.RequestAccusation();
    }
}
