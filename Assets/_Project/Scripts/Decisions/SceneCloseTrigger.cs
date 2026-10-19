using Caso404.Interaction;
using Caso404.Player;
using UnityEngine;

namespace Caso404.Decisions
{
    /// <summary>Objeto de la escena (ej. la puerta de salida) que abre el tablero de hipótesis para cerrar la escena.</summary>
    public class SceneCloseTrigger : MonoBehaviour, IInteractable
    {
        [SerializeField] private string _prompt = "Repasar el caso y decidir";

        public string Prompt => _prompt;
        public bool CanInteract => true;
        public bool IsNew => false;

        public void Interact(PlayerContext ctx) => ctx.RequestSceneClose();
    }
}
