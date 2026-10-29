using UnityEngine;

namespace Caso404.Environment
{
    /// <summary>
    /// Pulso suave del brillo de un objeto emisivo (LEDs de los racks): sube y baja con un ritmo propio y de vez en
    /// cuando titila. Usa MaterialPropertyBlock para no tocar el material compartido.
    /// </summary>
    public class EmissionPulse : MonoBehaviour
    {
        static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        [SerializeField] private Renderer _renderer;
        [SerializeField] private Color _emission = Color.white;
        [SerializeField] private float _speed = 1.3f;
        [SerializeField, Range(0f, 1f)] private float _depth = 0.35f;

        MaterialPropertyBlock _block;
        float _phase;

        void Awake()
        {
            if (_renderer == null) _renderer = GetComponent<Renderer>();
            _block = new MaterialPropertyBlock();
            _phase = Random.value * 10f;
        }

        void Update()
        {
            if (_renderer == null) return;
            float t = Time.time * _speed + _phase;
            float k = 1f - _depth * (0.5f + 0.5f * Mathf.Sin(t)) - (Random.value < 0.02f ? 0.3f : 0f);
            _renderer.GetPropertyBlock(_block);
            _block.SetColor(EmissionId, _emission * k);
            _renderer.SetPropertyBlock(_block);
        }
    }
}
