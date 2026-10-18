using System.Collections;
using Caso404.Core;
using Caso404.Data;
using UnityEngine;

namespace Caso404.Player
{
    /// <summary>Leve sacudida de cámara al detectar una contradicción o descubrir una mentira (skill diseno-ui).</summary>
    public class CameraShake : MonoBehaviour
    {
        [SerializeField] private GameContext _game;
        [SerializeField] private float _amplitude = 0.025f;
        [SerializeField] private float _seconds = 0.25f;

        Vector3 _rest;
        Coroutine _routine;

        void Awake() => _rest = transform.localPosition;

        void OnEnable()
        {
            if (_game == null || _game.State == null) return;
            _game.Alex.ClaimRefuted += OnRefuted;
            _game.Dialogue.LieExposed += OnLie;
        }

        void OnDisable()
        {
            if (_game == null || _game.State == null) return;
            _game.Alex.ClaimRefuted -= OnRefuted;
            _game.Dialogue.LieExposed -= OnLie;
            transform.localPosition = _rest;
        }

        void OnRefuted(LieData _) => Shake(1f);
        void OnLie(CharacterProfile _, LieData __) => Shake(0.6f);

        /// <summary>Sacude la cámara con una intensidad relativa (0–1).</summary>
        public void Shake(float strength)
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(ShakeRoutine(strength));
        }

        IEnumerator ShakeRoutine(float strength)
        {
            for (float t = 0; t < _seconds; t += Time.unscaledDeltaTime)
            {
                float k = 1f - t / _seconds;
                transform.localPosition = _rest + Random.insideUnitSphere * (_amplitude * strength * k);
                yield return null;
            }
            transform.localPosition = _rest;
            _routine = null;
        }
    }
}
