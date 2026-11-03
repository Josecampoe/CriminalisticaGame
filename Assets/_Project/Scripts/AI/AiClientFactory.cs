namespace Caso404.AI
{
    /// <summary>
    /// Elige el cliente de IA según AiSettings (con los cambios de ia.json, si existe).
    /// Sin settings, o con UseOffline, usa el modo sin conexión.
    /// </summary>
    public static class AiClientFactory
    {
        /// <summary>Crea el cliente de IA configurado.</summary>
        public static IAiClient Create(AiSettings settings)
        {
            var s = AiRuntimeConfig.Resolve(settings);
            return s == null || s.UseOffline ? new OfflineAiClient() : new HttpAiClient(s);
        }
    }
}
