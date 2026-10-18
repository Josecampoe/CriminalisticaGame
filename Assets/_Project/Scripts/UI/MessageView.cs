using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Caso404.UI
{
    /// <summary>Ventana modal genérica: título, texto y botones (intros, resultados de decisiones, fin de la demo).</summary>
    public class MessageView : UIPanel
    {
        TextMeshProUGUI _title;
        TextMeshProUGUI _body;
        TextMeshProUGUI _alexLine;
        RectTransform _buttons;
        bool _escapable = true;
        readonly List<(string label, Action action)> _actions = new();

        public override bool ClosesWithEscape => _escapable;

        protected override void Build()
        {
            var rt = UIFactory.Stretch((RectTransform)transform);
            var dim = UIFactory.Panel(rt, "Dim", UITheme.WithAlpha(_theme.BgDark, 0.85f));
            UIFactory.Stretch(dim.rectTransform);
            dim.raycastTarget = true;

            var card = UIFactory.Panel(rt, "Card", _theme.Paper);
            UIFactory.Place(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000, 560));
            UIFactory.VLayout(card.gameObject, 16, 40);

            _title = UIFactory.Text(card.transform, "Title", "", _theme.TitleFont, _theme.SizeL, _theme.Ink);
            UIFactory.Layout(_title.gameObject, preferredHeight: 48);
            _body = UIFactory.Text(card.transform, "Body", "", _theme.BodyFont, _theme.SizeM, _theme.Ink);
            UIFactory.Layout(_body.gameObject, flexibleHeight: 1);
            _alexLine = UIFactory.Text(card.transform, "Alex", "", _theme.MonoFont, _theme.SizeBody, _theme.BgDark);
            UIFactory.Layout(_alexLine.gameObject, preferredHeight: 60);

            _buttons = UIFactory.Rect("Buttons", card.transform);
            UIFactory.HLayout(_buttons.gameObject, 12);
            UIFactory.Layout(_buttons.gameObject, minHeight: 52, preferredHeight: 52);
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
            _alexLine.text = string.IsNullOrEmpty(alexLine) ? "" : $"ALEX: \"{alexLine}\"";
            _alexLine.gameObject.SetActive(!string.IsNullOrEmpty(alexLine));
            _escapable = escapable;
            UIFactory.Clear(_buttons);
            _actions.Clear();
            _actions.AddRange(buttons);
            if (_actions.Count == 0) _actions.Add(("Continuar", null));
            foreach (var (label, action) in _actions)
            {
                var captured = label;
                UIFactory.Button(_buttons, label, _theme, _theme.BgDark, _theme.TextLight, () => Choose(captured));
            }
            Open();
        }
    }
}
