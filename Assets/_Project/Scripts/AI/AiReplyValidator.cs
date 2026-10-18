using System.Linq;
using Caso404.Data;

namespace Caso404.AI
{
    /// <summary>
    /// Valida una respuesta de la IA contra el perfil del personaje: los IDs deben existir y un secreto
    /// solo puede revelarse si se cumple su condición. Si no pasa, el diálogo usa la frase de respaldo.
    /// </summary>
    public static class AiReplyValidator
    {
        public static readonly string[] AllowedEmotions = { "neutral", "nervioso", "defensivo", "triste", "enojado", "confiado" };

        /// <summary>True si la respuesta es usable.</summary>
        public static bool IsValid(AiReply reply, CharacterProfile character, IGameStateReader state)
        {
            if (reply == null || string.IsNullOrWhiteSpace(reply.reply) || character == null) return false;
            if (!AllowedEmotions.Contains(reply.emotion)) return false;
            if (reply.usedFactIds != null && reply.usedFactIds.Any(f => character.FindFact(f) == null)) return false;
            if (reply.usedLieIds != null && reply.usedLieIds.Any(l => character.Lies.All(x => x == null || x.Id != l))) return false;
            if (reply.revealedSecretIds != null)
                foreach (var id in reply.revealedSecretIds)
                {
                    var secret = character.Secrets.FirstOrDefault(s => s.FactId == id);
                    if (secret == null || secret.OnlyInEnding || !secret.UnlockCondition.Evaluate(state)) return false;
                }
            // Un hecho secreto no puede aparecer como "usado" si su secreto no está desbloqueado.
            if (reply.usedFactIds != null)
                foreach (var f in reply.usedFactIds)
                {
                    var secret = character.Secrets.FirstOrDefault(s => s.FactId == f);
                    if (secret != null && !state.KnowsFact(f)) return false;
                }
            return true;
        }
    }
}
