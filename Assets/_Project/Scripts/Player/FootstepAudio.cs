using UnityEngine;

namespace Caso404.Player
{
    /// <summary>
    /// Pasos del investigador: un golpe sordo cada ~0.7 m recorridos, con tono y volumen un poco distintos cada vez
    /// para que no suene repetido. Los sonidos se generan por código (no hay grabaciones en el proyecto); se pueden
    /// cambiar por grabaciones asignando clips sin tocar el resto.
    /// </summary>
    [RequireComponent(typeof(FirstPersonController))]
    public class FootstepAudio : MonoBehaviour
    {
        [SerializeField] private AudioClip[] _clips;
        [SerializeField] private float _strideLength = 0.7f;
        [SerializeField, Range(0f, 1f)] private float _volume = 0.28f;

        FirstPersonController _controller;
        AudioSource _source;
        float _distance;
        int _last = -1;

        void Awake()
        {
            _controller = GetComponent<FirstPersonController>();
            _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = 0f;
            if (_clips == null || _clips.Length == 0)
                _clips = new[] { Step("paso_1", 1f, 11), Step("paso_2", 0.92f, 23), Step("paso_3", 1.08f, 37) };
        }

        void Update()
        {
            float speed = _controller.PlanarSpeed;
            if (speed < 0.2f) { _distance = _strideLength * 0.6f; return; } // al arrancar, el primer paso suena pronto
            _distance += speed * Time.deltaTime;
            if (_distance < _strideLength) return;
            _distance = 0f;
            int i = Random.Range(0, _clips.Length);
            if (i == _last) i = (i + 1) % _clips.Length;
            _last = i;
            _source.pitch = Random.Range(0.92f, 1.08f);
            _source.PlayOneShot(_clips[i], _volume * Random.Range(0.85f, 1f));
        }

        /// <summary>Golpe corto de suela: ruido filtrado (roce) más un tono grave (impacto), con caída rápida.</summary>
        static AudioClip Step(string name, float pitch, int seed)
        {
            const int rate = 44100;
            int length = Mathf.CeilToInt(0.16f * rate);
            var data = new float[length];
            var rng = new System.Random(seed);
            float low = 0f;
            for (int i = 0; i < length; i++)
            {
                float t = i / (float)rate;
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                low += (noise - low) * 0.08f; // filtro paso bajo: el ruido queda como roce apagado
                float thump = Mathf.Sin(2f * Mathf.PI * 85f * pitch * t) * Mathf.Exp(-t * 38f);
                float scuff = low * 2.2f * Mathf.Exp(-t * 26f);
                float attack = Mathf.Clamp01(t * 400f); // sin clic al empezar
                data[i] = attack * (0.75f * thump + scuff);
            }
            var clip = AudioClip.Create(name, length, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
