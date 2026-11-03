using UnityEngine;

namespace Caso404.AI
{
    /// <summary>
    /// Configuración de la IA. Aquí van la URL del proxy y el modelo; NUNCA una clave de API
    /// (la clave vive en el servidor: secreto de Cloudflare o variable de entorno del proxy local).
    /// Un archivo ia.json junto al juego puede cambiar la URL sin recompilar (ver <see cref="AiRuntimeConfig"/>).
    /// </summary>
    [CreateAssetMenu(menuName = "Caso404/Configuración IA", fileName = "AiSettings")]
    public class AiSettings : ScriptableObject
    {
        [SerializeField] private bool _useOffline = true;
        /// <summary>Servidor público del juego (Server/worker.mjs en Cloudflare Workers).</summary>
        public const string PublicProxyUrl = "https://caso404-ia.tallerpro-ai-proxy.workers.dev/dialogue";
        /// <summary>Proxy en este mismo equipo (Server/proxy.mjs), para desarrollo.</summary>
        public const string LocalProxyUrl = "http://127.0.0.1:8790/dialogue";
        /// <summary>Cabecera que identifica al juego ante el servidor público (no es secreta).</summary>
        public const string ClientHeader = "X-Caso404-Client";

        [SerializeField] private string _proxyUrl = PublicProxyUrl;
        [SerializeField] private string _model = "";
        [SerializeField] private int _timeoutSeconds = 15;
        [SerializeField] private int _maxQuestionLength = 200;
        [Tooltip("Segundos para comprobar si el servidor de IA responde. Por internet conviene dejar margen.")]
        [SerializeField] private int _healthTimeoutSeconds = 4;

        /// <summary>Si es true se usan solo las frases de respaldo del guion.</summary>
        public bool UseOffline { get => _useOffline; internal set => _useOffline = value; }
        public string ProxyUrl { get => _proxyUrl; internal set => _proxyUrl = value; }
        public string Model { get => _model; internal set => _model = value; }
        public int TimeoutSeconds { get => _timeoutSeconds; internal set => _timeoutSeconds = value; }
        public int MaxQuestionLength { get => _maxQuestionLength; internal set => _maxQuestionLength = value; }
        public int HealthTimeoutSeconds { get => _healthTimeoutSeconds; internal set => _healthTimeoutSeconds = value; }
    }
}
