using UnityEngine;
using UnityEngine.UI;

namespace Caso404.UI
{
    /// <summary>Hace "respirar" un elemento de UI (luz de estado de ALEX, avisos): su transparencia oscila suavemente.</summary>
    [RequireComponent(typeof(Graphic))]
    public class PulseUI : MonoBehaviour
    {
        [SerializeField] private float _speed = 2.2f;
        [SerializeField, Range(0f, 1f)] private float _minAlpha = 0.35f;

        Graphic _graphic;
        float _baseAlpha;

        void Awake()
        {
            _graphic = GetComponent<Graphic>();
            _baseAlpha = _graphic.color.a;
        }

        void Update()
        {
            float k = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * _speed);
            var c = _graphic.color;
            c.a = _baseAlpha * Mathf.Lerp(_minAlpha, 1f, k);
            _graphic.color = c;
        }
    }
}
