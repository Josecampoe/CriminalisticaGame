using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Caso404.UI
{
    /// <summary>
    /// Carta de contexto antes de cada caso: fondo oscuro, hoja de expediente con sello "CONFIDENCIAL",
    /// texto con efecto de máquina de escribir (un clic lo completa) y botón "Comenzar investigación".
    /// Solo muestra texto y avisa cuando el jugador quiere empezar; no toca el estado del juego.
    /// </summary>
    public class CaseLetterView : MonoBehaviour, IPointerClickHandler
    {
        const float CharsPerSecond = 70f;

        UITheme _theme;
        TextMeshProUGUI _body;
        Button _begin;
        TextMeshProUGUI _hint;
        Action _onBegin;
        string _text;
        bool _done;
        Coroutine _typing;

        /// <summary>True mientras haya una carta abierta (las pruebas la usan para saltarla).</summary>
        public static CaseLetterView Current { get; private set; }

        /// <summary>Crea la carta a pantalla completa bajo el canvas indicado.</summary>
        public static CaseLetterView Show(Transform canvasRoot, UITheme theme, string text, Action onBegin)
        {
            var rt = UIFactory.Stretch(UIFactory.Rect("CartaDeContexto", canvasRoot));
            rt.SetAsLastSibling();
            var view = rt.gameObject.AddComponent<CaseLetterView>();
            view.Build(theme, text, onBegin);
            return view;
        }

        void Build(UITheme theme, string text, Action onBegin)
        {
            _theme = theme;
            _text = text ?? string.Empty;
            _onBegin = onBegin;
            Current = this;

            var bg = gameObject.AddComponent<Image>();
            bg.color = UITheme.WithAlpha(theme.BgDark, 0.97f);

            // Hoja de papel tipo expediente
            var paper = UIFactory.AsPaper(UIFactory.Panel(transform, "Hoja", theme.Paper), theme);
            UIFactory.Region(paper.rectTransform, new Vector2(0.2f, 0.04f), new Vector2(0.8f, 0.96f));
            paper.rectTransform.localRotation = Quaternion.Euler(0, 0, -0.4f);
            var margin = UIFactory.Panel(paper.transform, "Margen", UITheme.WithAlpha(theme.AccentRed, 0.35f));
            UIFactory.Place(margin.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(70, 0), new Vector2(2, 0));
            margin.rectTransform.anchorMax = new Vector2(0, 1);
            margin.rectTransform.sizeDelta = new Vector2(2, 0);

            // Sello CONFIDENCIAL
            var stamp = UIFactory.Text(paper.transform, "Sello", "CONFIDENCIAL", theme.TitleFont, theme.SizeL, UITheme.WithAlpha(theme.AccentRed, 0.75f), TextAlignmentOptions.Center);
            stamp.fontStyle = FontStyles.Bold;
            stamp.characterSpacing = 8f;
            UIFactory.Place(stamp.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-30, -26), new Vector2(380, 70));
            stamp.rectTransform.localRotation = Quaternion.Euler(0, 0, 12f);
            var border = stamp.gameObject.AddComponent<Outline>();
            border.effectColor = UITheme.WithAlpha(theme.AccentRed, 0.4f);

            _body = UIFactory.Text(paper.transform, "Texto", string.Empty, theme.MonoFont, theme.SizeS + 2f, theme.Ink);
            UIFactory.Stretch(_body.rectTransform, 100, 120, 70, 130);
            _body.lineSpacing = 6f;
            _body.text = _text;
            _body.maxVisibleCharacters = 0;
            _body.enableAutoSizing = true;
            _body.fontSizeMin = 14f;
            _body.fontSizeMax = theme.SizeBody + 6f;
            _body.paragraphSpacing = 14f;

            var hint = _hint = UIFactory.Text(paper.transform, "Pista", "Clic para leer todo", theme.BodyFont, theme.SizeS, theme.Muted, TextAlignmentOptions.BottomLeft);
            UIFactory.Place(hint.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(100, 40), new Vector2(500, 40));

            _begin = UIFactory.Button(paper.transform, "Comenzar investigación", theme, theme.Ink, theme.Paper, Begin, 64, theme.SizeM);
            UIFactory.Place((RectTransform)_begin.transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-60, 30), new Vector2(460, 64));
            _begin.gameObject.SetActive(false);

            _typing = StartCoroutine(Type());
        }

        IEnumerator Type()
        {
            int total = _text.Length;
            float shown = 0f;
            while (shown < total)
            {
                shown += Time.unscaledDeltaTime * CharsPerSecond;
                _body.maxVisibleCharacters = Mathf.Min(total, (int)shown);
                yield return null;
            }
            Complete();
        }

        /// <summary>Muestra todo el texto de una vez y habilita el botón.</summary>
        public void Complete()
        {
            if (_done) return;
            _done = true;
            if (_typing != null) StopCoroutine(_typing);
            _body.maxVisibleCharacters = int.MaxValue;
            _begin.gameObject.SetActive(true);
            _hint.gameObject.SetActive(false);
        }

        /// <summary>Equivale a pulsar "Comenzar investigación".</summary>
        public void Begin()
        {
            Complete();
            var cb = _onBegin;
            _onBegin = null;
            if (Current == this) Current = null;
            Destroy(gameObject);
            cb?.Invoke();
        }

        public void OnPointerClick(PointerEventData eventData) => Complete();

        void OnDestroy()
        {
            if (Current == this) Current = null;
        }
    }
}
