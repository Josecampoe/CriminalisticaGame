using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Caso404.UI
{
    /// <summary>Fundido a negro entre escenas: aparece desde negro al cargar y se oscurece antes de cambiar de escena.</summary>
    public class ScreenFader : MonoBehaviour
    {
        [SerializeField] private UITheme _theme;
        [SerializeField] private float _seconds = 0.6f;

        CanvasGroup _group;
        Coroutine _routine;

        /// <summary>True mientras está oscureciendo para cambiar de escena.</summary>
        public bool IsFadingOut { get; private set; }

        void Awake()
        {
            var rt = (RectTransform)transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var img = gameObject.AddComponent<Image>();
            img.color = _theme != null ? _theme.BgDark : Color.black;
            _group = gameObject.AddComponent<CanvasGroup>();
            _group.alpha = 1f;
            _group.blocksRaycasts = true;
        }

        void Start() => _routine = StartCoroutine(Fade(0f, null));

        /// <summary>Oscurece la pantalla y luego ejecuta la acción (normalmente cargar otra escena).</summary>
        public void FadeOutThen(Action action)
        {
            if (IsFadingOut) return;
            IsFadingOut = true;
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(Fade(1f, action));
        }

        IEnumerator Fade(float target, Action then)
        {
            _group.blocksRaycasts = true;
            float start = _group.alpha;
            for (float t = 0; t < _seconds; t += Time.unscaledDeltaTime)
            {
                _group.alpha = Mathf.Lerp(start, target, t / _seconds);
                yield return null;
            }
            _group.alpha = target;
            _group.blocksRaycasts = target > 0.5f;
            _routine = null;
            then?.Invoke();
        }
    }
}
