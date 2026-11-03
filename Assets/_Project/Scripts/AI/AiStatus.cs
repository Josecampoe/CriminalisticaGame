using System.Threading;
using System.Threading.Tasks;

namespace Caso404.AI
{
    /// <summary>Estado de la conexión con la IA, para mostrarlo en el menú.</summary>
    public enum AiConnection
    {
        /// <summary>El servidor responde: los personajes hablan con IA.</summary>
        Online,
        /// <summary>Sin servidor o sin conexión: se usan las frases del guion (el juego se puede terminar igual).</summary>
        ScriptOnly,
        /// <summary>La IA está desactivada en la configuración.</summary>
        Disabled
    }

    /// <summary>Comprueba si la IA está disponible sin hacer ninguna pregunta (solo GET /health).</summary>
    public static class AiStatus
    {
        public static async Task<AiConnection> CheckAsync(AiSettings settings, CancellationToken ct)
        {
            var s = AiRuntimeConfig.Resolve(settings);
            if (s == null || s.UseOffline) return AiConnection.Disabled;
            return await new HttpAiClient(s).CheckAsync(ct) ? AiConnection.Online : AiConnection.ScriptOnly;
        }
    }
}
