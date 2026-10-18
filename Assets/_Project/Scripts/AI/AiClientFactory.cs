namespace Caso404.AI
{
    /// <summary>Elige el cliente de IA según AiSettings. Sin settings, o con UseOffline, usa el modo sin conexión.</summary>
    public static class AiClientFactory
    {
        /// <summary>Crea el cliente de IA configurado.</summary>
        public static IAiClient Create(AiSettings settings) =>
            settings == null || settings.UseOffline ? new OfflineAiClient() : new HttpAiClient(settings);
    }
}
