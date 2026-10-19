using System.Threading;
using System.Threading.Tasks;
using Caso404.Data;

namespace Caso404.AI
{
    /// <summary>
    /// IA sin conexión: responde con las frases de respaldo del guion según el tema.
    /// Es determinista, así que el juego se puede terminar completo sin red.
    /// </summary>
    public class OfflineAiClient : IAiClient
    {
        public bool IsOffline => true;

        public Task<AiReply> AskAsync(AiRequest request, CancellationToken ct) =>
            Task.FromResult(BuildReply(request.Character, request.Topic, request.Question));

        /// <summary>Respuesta de respaldo para un personaje y un tema (también la usa el diálogo si la IA falla).</summary>
        public static AiReply BuildReply(CharacterProfile character, DialogueTopic topic, string question)
        {
            var lines = character.Fallback;
            if (topic == DialogueTopic.FreeText) topic = Classify(question);
            FallbackLine line;
            string emotion;
            switch (topic)
            {
                case DialogueTopic.OtherPerson: line = lines.OtherPerson; emotion = "neutral"; break;
                case DialogueTopic.Evidence: line = lines.Evidence; emotion = "defensivo"; break;
                case DialogueTopic.Press: line = lines.Press; emotion = "nervioso"; break;
                default: line = lines.Crime; emotion = "triste"; break;
            }
            var reply = new AiReply { reply = line?.Text ?? "...", emotion = emotion };
            if (line != null) reply.usedFactIds.AddRange(line.RevealsFactIds);
            return reply;
        }

        /// <summary>Clasifica una pregunta escrita en un tema, por palabras clave.</summary>
        public static DialogueTopic Classify(string question)
        {
            var q = (question ?? "").ToLowerInvariant();
            if (ContainsAny(q, "quién", "quien", "otra persona", "vecin", "carlos", "laura", "hernán", "hernan", "andrés", "andres", "portero", "exnovio", "sospech"))
                return DialogueTopic.OtherPerson;
            if (ContainsAny(q, "prueba", "evidencia", "huella", "colilla", "teléfono", "telefono", "recibo", "cámara", "camara", "overol", "llave", "esto"))
                return DialogueTopic.Evidence;
            return DialogueTopic.Crime;
        }

        static bool ContainsAny(string text, params string[] words)
        {
            foreach (var w in words) if (text.Contains(w)) return true;
            return false;
        }
    }
}
