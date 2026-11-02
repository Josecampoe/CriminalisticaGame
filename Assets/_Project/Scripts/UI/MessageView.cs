using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Caso404.UI
{
    /// <summary>
    /// Ventana modal genérica (intros, resultados de decisiones, fin de la demo): una hoja de papel que se ajusta a su
    /// texto, título a máquina de escribir, la línea de ALEX como una franja de terminal y los botones abajo.
    /// </summary>
    public class MessageView : UIPanel
    {
        TextMeshProUGUI _title;
        TextMeshProUGUI _body;
        TextMeshProUGUI _alexLine;
        RectTransform _alexBox;
        RectTransform _card;
        RectTransform _buttons;
        bool _escapable = true;
        readonly List<(string label, Action action)> _actions = new();

        public override bool ClosesWithEscape => _escapable;

        protected override void Build()
        {
            var rt = UIFactory.Stretch((RectTransform)transform);
            UIFactory.Backdrop(rt, _theme, 0.78f);

            var card = UIFactory.AsPaper(UIFactory.Panel(rt, "Hoja", _theme.Paper), _theme);
            _card = card.rectTransform;
            UIFactory.Place(_card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1040, 0));
            _card.localRotation = Quaternion.Euler(0, 0, -0.35f);
            var v = UIFactory.VLayout(card.gameObject, 18);
            v.padding = new RectOffset(64, 64, 52, 48);
            var fit = card.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var margin = UIFactory.Panel(card.transform, "Margen", UITheme.WithAlpha(_theme.AccentRed, 0.3f));
            margin.rectTransform.anchorMin = new Vector2(0, 0);
            margin.rectTransform.anchorMax = new Vector2(0, 1);
            margin.rectTransform.pivot = new Vector2(0, 0.5f);
            margin.rectTransform.offsetMin = new Vector2(40, 0);
            margin.rectTransform.offsetMax = new Vector2(42, 0);
            UIFactory.Ignore(margin.gameObject);

            UIFactory.Kicker(card.transform, "Unidad de Criminalística · Valdora", _theme, UITheme.WithAlpha(_theme.Ink, 0.5f));
            _title = UIFactory.Text(card.transform, "Titulo", "", _theme.TitleFont, 40, _theme.Ink);
            UIFactory.Divider(card.transform, UITheme.WithAlpha(_theme.Ink, 0.25f));
            _body = UIFactory.Text(card.transform, "Texto", "", _theme.BodyFont, _theme.SizeM, _theme.Ink);
            _body.lineSpacing = 10f;
            _body.paragraphSpacing = 12f;
            UIFactory.Layout(_body.gameObject, minHeight: 60);

            // Franja de terminal con la línea de ALEX.
            var alexBg = UIFactory.Round(UIFactory.Panel(card.transform, "Alex", new Color(0.05f, 0.08f, 0.09f, 0.95f)), _theme);
            _alexBox = alexBg.rectTransform;
            var ah = UIFactory.HLayout(alexBg.gameObject, 14, 0, false);
            ah.padding = new RectOffset(18, 18, 14, 14);
            ah.childAlignment = TextAnchor.MiddleLeft;
            var tag = UIFactory.Text(alexBg.transform, "Quien", "ALEX", _theme.MonoFont, _theme.SizeS, _theme.AlexCyan);
            tag.fontStyle = FontStyles.Bold;
            tag.characterSpacing = 8f;
            UIFactory.Layout(tag.gameObject, preferredWidth: 70);
            _alexLine = UIFactory.Text(alexBg.transform, "Linea", "", _theme.MonoFont, _theme.SizeBody, _theme.AlexCyan);
            UIFactory.Layout(_alexLine.gameObject, flexibleWidth: 1);

            _buttons = UIFactory.Rect("Botones", card.transform);
            var bh = UIFactory.HLayout(_buttons.gameObject, 12);
            bh.padding = new RectOffset(0, 0, 10, 0);
            UIFactory.Layout(_buttons.gameObject, minHeight: 66, preferredHeight: 66);
        }

        /// <summary>Textos de los botones actuales.</summary>
        public IEnumerable<string> ButtonLabels => _actions.ConvertAll(a => a.label);

        /// <summary>Elige un botón por su texto: cierra la ventana y ejecuta su acción. False si no existe.</summary>
        public bool Choose(string label)
        {
            var index = _actions.FindIndex(a => a.label == label);
            if (index < 0) return false;
            var action = _actions[index].action;
            Close();
            action?.Invoke();
            return true;
        }

        /// <summary>Muestra el mensaje. Cada botón cierra la ventana y luego ejecuta su acción.</summary>
        public void Show(string title, string body, string alexLine, bool escapable, params (string label, Action action)[] buttons)
        {
            _title.text = title;
            _body.text = body;
            _body.gameObject.SetActive(!string.IsNullOrEmpty(body));
            _alexLine.text = string.IsNullOrEmpty(alexLine) ? "" : $"“{alexLine}”";
            _alexBox.gameObject.SetActive(!string.IsNullOrEmpty(alexLine));
            _escapable = escapable;
            UIFactory.Clear(_buttons);
            _actions.Clear();
            _actions.AddRange(buttons);
            if (_actions.Count == 0) _actions.Add(("Continuar", null));
            for (int i = 0; i < _actions.Count; i++)
            {
                var captured = _actions[i].label;
                // El primer botón es la acción principal (tinta); los demás, secundarios.
                if (i == 0) UIFactory.Button(_buttons, captured, _theme, _theme.Ink, _theme.Paper, () => Choose(captured), 56);
                else UIFactory.Button(_buttons, captured, _theme, UITheme.WithAlpha(_theme.Ink, 0.08f), _theme.Ink, () => Choose(captured), 56);
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(_card);
            Open();
        }
    }
}
