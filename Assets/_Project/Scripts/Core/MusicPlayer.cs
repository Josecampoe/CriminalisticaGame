using UnityEngine;

namespace Caso404.Core
{
    /// <summary>
    /// Música de fondo: varias piezas suaves de suspenso que rotan solas en orden aleatorio (nunca la misma dos veces
    /// seguidas), con un fundido cruzado entre una y otra. Sobrevive a los cambios de escena: la música no se corta ni
    /// vuelve a empezar al pasar de un lugar a otro. Si la escena nueva trae su propio reproductor, ese se retira.
    /// </summary>
    public class MusicPlayer : MonoBehaviour
    {
        [SerializeField] private AudioClip[] _tracks;
        [SerializeField, Range(0f, 1f)] private float _volume = 0.3f;
        [Tooltip("Segundos del fundido cruzado entre piezas.")]
        [SerializeField] private float _crossfade = 4f;

        static MusicPlayer _instance;

        AudioSource _a, _b;
        AudioSource _current;
        int _last = -1;
        float _fade = 1f;

        /// <summary>Pieza que suena ahora (null si no hay música).</summary>
        public AudioClip CurrentTrack => _current != null ? _current.clip : null;

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
            _a = NewSource();
            _b = NewSource();
        }

        void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        AudioSource NewSource()
        {
            var s = gameObject.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.loop = false;
            s.spatialBlend = 0f;
            s.priority = 0; // la música nunca se corta por falta de voces
            s.volume = 0f;
            return s;
        }

        void Start()
        {
            if (_instance == this) PlayNext();
        }

        void Update()
        {
            if (_current == null) return;
            var other = _current == _a ? _b : _a;
            if (_fade < 1f)
            {
                _fade = Mathf.Min(1f, _fade + Time.unscaledDeltaTime / Mathf.Max(0.01f, _crossfade));
                _current.volume = _volume * _fade;
                other.volume = _volume * (1f - _fade);
                if (_fade >= 1f) other.Stop();
            }
            // Empieza la siguiente pieza un poco antes de que termine esta, para que se mezclen.
            if (_current.clip != null && _current.time >= _current.clip.length - _crossfade) PlayNext();
        }

        /// <summary>Pasa a otra pieza al azar (distinta de la actual) con fundido cruzado.</summary>
        public void PlayNext()
        {
            if (_tracks == null || _tracks.Length == 0) return;
            int next = Random.Range(0, _tracks.Length);
            if (_tracks.Length > 1 && next == _last) next = (next + 1 + Random.Range(0, _tracks.Length - 1)) % _tracks.Length;
            _last = next;
            var target = _current == _a ? _b : _a;
            target.clip = _tracks[next];
            target.volume = 0f;
            target.Play();
            _current = target;
            _fade = 0f;
        }
    }
}
