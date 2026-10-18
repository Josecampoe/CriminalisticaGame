using Caso404.Interaction;
using Caso404.Player;
using UnityEngine;

namespace Caso404.AI
{
    /// <summary>Pantalla de ALEX en la escena: abre su panel.</summary>
    public class AlexScreenTrigger : MonoBehaviour, IInteractable
    {
        [SerializeField] private string _prompt = "Hablar con ALEX";

        public string Prompt => _prompt;
        public bool CanInteract => true;
        public bool IsNew => false;

        public void Interact(PlayerContext ctx) => ctx.RequestAlex();
    }
}
