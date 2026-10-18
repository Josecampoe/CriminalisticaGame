using Caso404.Core;
using Caso404.Data;
using UnityEngine;

namespace Caso404.UI
{
    /// <summary>
    /// Sonidos de interfaz generados por código (sin archivos de audio): evidencia guardada, análisis,
    /// observación de ALEX, mentira descubierta y contradicción. Son marcadores hasta tener sonidos definitivos.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class AudioFeedback : MonoBehaviour
    {
        [SerializeField] private GameContext _game;
        [SerializeField, Range(0f, 1f)] private float _volume = 0.35f;

        AudioSource _source;
        AudioClip _collect, _analyze, _alex, _lie, _contradiction;

        void Awake()
        {
            _source = GetComponent<AudioSource>();
            _source.playOnAwake = false;
            _collect = Tone("collect", new[] { 880f, 1320f }, 0.07f, 0.5f);           // "clic" de cámara / papel
            _analyze = Tone("analyze", new[] { 660f, 880f, 990f }, 0.06f, 0.4f);      // sello de informe
            _alex = Tone("alex", new[] { 1046f, 1568f }, 0.08f, 0.25f);               // aviso digital de ALEX
            _lie = Tone("lie", new[] { 330f, 262f }, 0.12f, 0.6f);                    // mentira descubierta
            _contradiction = Tone("contradiction", new[] { 110f, 82f }, 0.22f, 0.9f); // sonido grave (skill diseno-ui)
        }

        void OnEnable()
        {
            if (_game == null || _game.State == null) return;
            _game.Evidence.EvidenceCollected += OnCollected;
            _game.Evidence.EvidenceAnalyzed += OnAnalyzed;
            _game.Alex.ClaimRaised += OnClaim;
            _game.Alex.ClaimRefuted += OnRefuted;
            _game.Dialogue.LieExposed += OnLie;
        }

        void OnDisable()
        {
            if (_game == null || _game.State == null) return;
            _game.Evidence.EvidenceCollected -= OnCollected;
            _game.Evidence.EvidenceAnalyzed -= OnAnalyzed;
            _game.Alex.ClaimRaised -= OnClaim;
            _game.Alex.ClaimRefuted -= OnRefuted;
            _game.Dialogue.LieExposed -= OnLie;
        }

        void OnCollected(EvidenceData _) => Play(_collect);
        void OnAnalyzed(EvidenceData _) => Play(_analyze);
        void OnClaim(LieData _) => Play(_alex);
        void OnRefuted(LieData _) => Play(_contradiction);
        void OnLie(CharacterProfile _, LieData __) => Play(_lie);

        void Play(AudioClip clip)
        {
            if (clip != null) _source.PlayOneShot(clip, _volume);
        }

        /// <summary>Crea un clip con una secuencia de notas cortas con caída suave.</summary>
        static AudioClip Tone(string name, float[] freqs, float noteSeconds, float gain)
        {
            const int rate = 44100;
            int perNote = Mathf.CeilToInt(noteSeconds * rate);
            var data = new float[perNote * freqs.Length];
            for (int n = 0; n < freqs.Length; n++)
                for (int i = 0; i < perNote; i++)
                {
                    float t = i / (float)rate;
                    float env = Mathf.Exp(-6f * i / perNote);
                    data[n * perNote + i] = gain * env * Mathf.Sin(2f * Mathf.PI * freqs[n] * t);
                }
            var clip = AudioClip.Create(name, data.Length, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
