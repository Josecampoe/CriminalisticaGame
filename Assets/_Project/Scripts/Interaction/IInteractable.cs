using Caso404.Player;

namespace Caso404.Interaction
{
    /// <summary>Algo que el jugador puede usar apuntándole y pulsando E (objetos, personajes, salidas).</summary>
    public interface IInteractable
    {
        /// <summary>Texto bajo la retícula, ej. "Inspeccionar Huella".</summary>
        string Prompt { get; }

        /// <summary>False si ahora no se puede usar (el HUD no muestra el prompt).</summary>
        bool CanInteract { get; }

        /// <summary>True si es algo nuevo (resaltado ámbar); false si ya se revisó (blanco).</summary>
        bool IsNew { get; }

        /// <summary>Ejecuta la interacción. Solo emite intenciones a través del contexto del jugador.</summary>
        void Interact(PlayerContext ctx);
    }
}
