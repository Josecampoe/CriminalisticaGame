using UnityEngine;

namespace Caso404.Environment
{
    /// <summary>
    /// Parpadeo leve de un tubo fluorescente viejo: casi siempre encendido, con cortes breves e irregulares.
    /// Cambia la intensidad de la luz y el brillo del tubo (con MaterialPropertyBlock, sin tocar el material compartido).
    /// </summary>
    public class LightFlicker : MonoBehaviour
    {
        static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        [SerializeField] private Light _light;
        [SerializeField] private Renderer _tube;
        [SerializeField] private Color _tubeEmission = Color.white;
        [SerializeField, Range(0f, 1f)] private float _flickerChance = 0.35f;

        float _baseIntensity;
        float _nextBurst;
        float _burstEnd;
        MaterialPropertyBlock _block;

        void Awake()
        {
            if (_light == null) _light = GetComponent<Light>();
            if (_light != null) _baseIntensity = _light.intensity;
            _block = new MaterialPropertyBlock();
            _nextBurst = Time.time + Random.Range(1f, 5f);
        }

        void Update()
        {
            if (_light == null) return;
            float t = Time.time;
            float level = 1f;
            if (t >= _nextBurst)
            {
                _burstEnd = t + Random.Range(0.08f, 0.45f);
                _nextBurst = _burstEnd + Random.Range(2f, 9f);
            }
            if (t < _burstEnd)
                level = Random.value < _flickerChance ? Random.Range(0.05f, 0.4f) : 1f;
            _light.intensity = _baseIntensity * level;
            if (_tube != null)
            {
                _tube.GetPropertyBlock(_block);
                _block.SetColor(EmissionId, _tubeEmission * level);
                _tube.SetPropertyBlock(_block);
            }
        }
    }
}
