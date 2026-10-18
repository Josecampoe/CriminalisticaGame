using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Caso404.Data;

namespace Caso404.AI
{
    /// <summary>Tema de la pregunta: los 4 botones rápidos del guion, más la pregunta escrita y "hablar con calma".</summary>
    public enum DialogueTopic { Crime, OtherPerson, Evidence, Press, Calm, FreeText }

    /// <summary>Lo que se le pide a la IA en un turno.</summary>
    public class AiRequest
    {
        public CharacterProfile Character;
        public DialogueTopic Topic;
        public string Question;
        /// <summary>Resúmenes de evidencias que el jugador ya analizó ("ev_c1_huella: ...").</summary>
        public List<string> AnalyzedEvidence = new();
        /// <summary>Hechos que el jugador ya conoce.</summary>
        public List<string> KnownFacts = new();
        /// <summary>Últimas líneas de la conversación.</summary>
        public List<string> History = new();
        public string SceneId;
        public int AlexTrust;
    }

    /// <summary>Respuesta de la IA (formato JSON obligatorio, skill dialogo-ia).</summary>
    [Serializable]
    public class AiReply
    {
        public string reply;
        public string emotion = "neutral";
        public List<string> usedFactIds = new();
        public List<string> usedLieIds = new();
        public List<string> revealedSecretIds = new();
    }

    /// <summary>Cliente de IA. Hay dos implementaciones: sin conexión (frases del guion) y HTTP (vía proxy).</summary>
    public interface IAiClient
    {
        /// <summary>True si es el modo sin conexión (respuestas deterministas).</summary>
        bool IsOffline { get; }

        /// <summary>Pide una respuesta. Puede lanzar excepción o devolver null si falla; quien llama usa el respaldo.</summary>
        Task<AiReply> AskAsync(AiRequest request, CancellationToken ct);
    }
}
