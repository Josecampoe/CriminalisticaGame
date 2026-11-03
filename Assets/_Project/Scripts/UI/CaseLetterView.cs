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
            if (theme.VignetteSprite != null)
            {
                var vignette = UIFactory.Panel(transform, "Vineta", new Color(0f, 0f, 0f, 0.9f));
                vignette.sprite = theme.VignetteSprite;
                UIFactory.Stretch(vignette.rectTransform);
                vignette.raycastTarget = false;
            }

            // Hoja de papel tipo expediente, con un clip arriba a la izquierda.
            var paper = UIFactory.AsPaper(UIFactory.Panel(transform, "Hoja", theme.Paper), theme);
            UIFactory.Region(paper.rectTransform, new Vector2(0.21f, 0.04f), new Vector2(0.79f, 0.96f));
            paper.rectTransform.localRotation = Quaternion.Euler(0, 0, -0.4f);
            var clip = UIFactory.Panel(paper.transform, "Clip", new Color(0.62f, 0.64f, 0.66f, 0.95f));
            UIFactory.Frame(clip.transform, theme, new Color(0.35f, 0.36f, 0.38f, 0.9f));
            UIFactory.Round(clip, theme, 1.6f);
            UIFactory.Place(clip.rectTransform, new Vector2(0, 1), new Vector2(0.5f, 0.5f), new Vector2(150, -4), new Vector2(34, 96));
            clip.rectTransform.localRotation = Quaternion.Euler(0, 0, 4f);
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

            // Tinta de máquina de escribir: el membrete (primer bloque) más pequeño y gris; el cuerpo, en negro azulado.
            _body = UIFactory.Text(paper.transform, "Texto", string.Empty, theme.TitleFont, theme.SizeBody + 4f, new Color(0.13f, 0.13f, 0.17f));
            UIFactory.Stretch(_body.rectTransform, 104, 124, 76, 120);
            _body.lineSpacing = 4f;
            _body.text = Format(_text, theme);
            _body.maxVisibleCharacters = 0;
            _body.enableAutoSizing = true;
            _body.fontSizeMin = 15f;
            _body.fontSizeMax = theme.SizeBody + 5f;
            _body.paragraphSpacing = 16f;

            var hint = _hint = UIFactory.Text(paper.transform, "Pista", "Clic para leer todo", theme.MonoFont, theme.SizeS, UITheme.WithAlpha(theme.Ink, 0.5f), TextAlignmentOptions.BottomLeft);
            UIFactory.Place(hint.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(104, 44), new Vector2(500, 40));
            hint.gameObject.AddComponent<PulseUI>();

            _begin = UIFactory.Button(paper.transform, "Comenzar investigación", theme, theme.Ink, theme.Paper, Begin, 64, theme.SizeM);
            UIFactory.Place((RectTransform)_begin.transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-60, 30), new Vector2(460, 64));
            _begin.gameObject.SetActive(false);

            _typing = StartCoroutine(Type());
        }

        /// <summary>Separa el membrete (hasta la primera línea en blanco) y lo pone en letra pequeña y gris.</summary>
        static string Format(string text, UITheme theme)
        {
            int cut = text.IndexOf("\n\n", StringComparison.Ordinal);
            if (cut <= 0) return text;
            var header = text.Substring(0, cut);
            var body = text.Substring(cut + 2);
            return $"<size=80%><color={UITheme.Hex(UITheme.WithAlpha(theme.Ink, 0.62f))}>{header}</color></size>\n<size=50%> </size>\n{body}";
        }

        IEnumerator Type()
        {
            _body.ForceMeshUpdate();
            int total = _body.textInfo.characterCount;
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
