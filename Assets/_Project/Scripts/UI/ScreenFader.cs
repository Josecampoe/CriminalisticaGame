using System;
using System.Collections;
using TMPro;
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
        TextMeshProUGUI _label;

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

            // Texto "Cargando…" abajo a la derecha mientras la pantalla está en negro (la carga puede tardar unos segundos).
            if (_theme != null && _theme.MonoFont != null)
            {
                _label = UIFactory.Text(transform, "Cargando", "", _theme.MonoFont, _theme.SizeS + 2,
                    UITheme.WithAlpha(_theme.TextLight, 0.7f), TMPro.TextAlignmentOptions.BottomRight);
                UIFactory.Place(_label.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-48, 40), new Vector2(1200, 40));
                _label.characterSpacing = 4f;
                _label.raycastTarget = false;
            }
        }

        void Update()
        {
            if (_label == null || !IsFadingOut || string.IsNullOrEmpty(_label.text)) return;
            _label.alpha = 0.45f + 0.35f * Mathf.Sin(Time.unscaledTime * 4f); // parpadeo suave
        }

        void Start() => _routine = StartCoroutine(Fade(0f, null));

        /// <summary>Oscurece la pantalla y luego ejecuta la acción (normalmente cargar otra escena).</summary>
        public void FadeOutThen(Action action) => FadeOutThen(action, null);

        /// <summary>Igual que <see cref="FadeOutThen(Action)"/>, mostrando "Cargando · destino" sobre el negro.</summary>
        public void FadeOutThen(Action action, string destination)
        {
            if (IsFadingOut) return;
            IsFadingOut = true;
            if (_label != null)
                _label.text = string.IsNullOrEmpty(destination) ? "CARGANDO…" : $"CARGANDO  ·  {destination.ToUpperInvariant()}";
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
