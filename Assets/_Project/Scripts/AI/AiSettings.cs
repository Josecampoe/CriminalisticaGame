using UnityEngine;

namespace Caso404.AI
{
    /// <summary>
    /// Configuración de la IA. Aquí van la URL del proxy y el modelo; NUNCA una clave de API
    /// (la clave vive en el servidor proxy, en una variable de entorno).
    /// </summary>
    [CreateAssetMenu(menuName = "Caso404/Configuración IA", fileName = "AiSettings")]
    public class AiSettings : ScriptableObject
    {
        [SerializeField] private bool _useOffline = true;
        [SerializeField] private string _proxyUrl = "http://localhost:8787/dialogue";
        [SerializeField] private string _model = "";
        [SerializeField] private int _timeoutSeconds = 15;
        [SerializeField] private int _maxQuestionLength = 200;

        /// <summary>Si es true se usan solo las frases de respaldo del guion.</summary>
        public bool UseOffline { get => _useOffline; internal set => _useOffline = value; }
        public string ProxyUrl { get => _proxyUrl; internal set => _proxyUrl = value; }
        public string Model { get => _model; internal set => _model = value; }
        public int TimeoutSeconds { get => _timeoutSeconds; internal set => _timeoutSeconds = value; }
        public int MaxQuestionLength { get => _maxQuestionLength; internal set => _maxQuestionLength = value; }
    }
}
