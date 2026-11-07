using UnityEngine;

namespace Caso404.UI
{
    /// <summary>
    /// Clic suave al pulsar un botón de la interfaz (skill diseno-ui: confirmar la acción en menos de 100 ms).
    /// El sonido se genera por código y suena desde una fuente de audio compartida que se crea al primer uso.
    /// </summary>
    public static class UiSound
    {
        const float Volume = 0.18f;

        static AudioSource _source;
        static AudioClip _click;

        /// <summary>Reproduce el clic de botón.</summary>
        public static void Click()
        {
            if (_source == null)
            {
                var go = new GameObject("UiSound") { hideFlags = HideFlags.HideInHierarchy };
                Object.DontDestroyOnLoad(go);
                _source = go.AddComponent<AudioSource>();
                _source.playOnAwake = false;
                _source.spatialBlend = 0f;
                _source.ignoreListenerPause = true;
            }
            if (_click == null) _click = MakeClick();
            _source.pitch = Random.Range(0.97f, 1.03f);
            _source.PlayOneShot(_click, Volume);
        }

        /// <summary>Tic corto y seco, como una tecla de máquina de escribir lejana.</summary>
        static AudioClip MakeClick()
        {
            const int rate = 44100;
            int length = Mathf.CeilToInt(0.045f * rate);
            var data = new float[length];
            var rng = new System.Random(404);
            for (int i = 0; i < length; i++)
            {
                float t = i / (float)rate;
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                float tone = Mathf.Sin(2f * Mathf.PI * 1800f * t);
                data[i] = (0.45f * noise + 0.55f * tone) * Mathf.Exp(-t * 140f);
            }
            var clip = AudioClip.Create("ui_click", length, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
