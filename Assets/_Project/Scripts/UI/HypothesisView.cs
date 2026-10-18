using System;
using System.Collections.Generic;
using Caso404.Core;
using Caso404.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Caso404.UI
{
    /// <summary>
    /// Tablero de hipótesis de la escena. Las opciones bloqueadas salen grises con el motivo
    /// (qué falta analizar). Al responder todas, se habilita "Tomar la decisión".
    /// </summary>
    public class HypothesisView : UIPanel
    {
        /// <summary>El jugador quiere pasar a la decisión de cierre.</summary>
        public event Action<DecisionData> DecisionRequested;

        [SerializeField] private GameContext _game;

        RectTransform _content;
        Button _decide;
        TextMeshProUGUI _footer;
        readonly Dictionary<string, HashSet<string>> _selection = new();

        protected override void Build()
        {
            var rt = UIFactory.Stretch((RectTransform)transform);
            var dim = UIFactory.Panel(rt, "Dim", UITheme.WithAlpha(_theme.BgDark, 0.94f));
            UIFactory.Stretch(dim.rectTransform);
            var title = UIFactory.Text(rt, "Title", "TABLERO — Hipótesis de la escena", _theme.TitleFont, _theme.SizeL, _theme.TextLight);
            UIFactory.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(60, -36), new Vector2(1200, 50));
            var close = UIFactory.Button(rt, "Cerrar (Esc)", _theme, _theme.Muted, _theme.TextLight, Close);
            UIFactory.Place((RectTransform)close.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-60, -36), new Vector2(200, 48));

            var board = UIFactory.Panel(rt, "Board", _theme.Paper);
            UIFactory.Region(board.rectTransform, new Vector2(0.05f, 0.14f), new Vector2(0.95f, 0.86f));
            _content = UIFactory.ScrollList(board.transform, "Questions", 18, 28);
            UIFactory.Stretch((RectTransform)_content.parent.parent);

            _footer = UIFactory.Text(rt, "Footer", "", _theme.BodyFont, _theme.SizeBody, _theme.TextLight, TextAlignmentOptions.MidlineLeft);
            UIFactory.Place(_footer.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(60, 40), new Vector2(1100, 50));
            _decide = UIFactory.Button(rt, "Tomar la decisión", _theme, _theme.AccentRed, _theme.TextLight, OnDecide);
            UIFactory.Place((RectTransform)_decide.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-60, 40), new Vector2(360, 56));
        }

        protected override void OnShown() => Rebuild();

        void Rebuild()
        {
            UIFactory.Clear(_content);
            var hyp = _game.Scene.Hypothesis;
            if (hyp == null)
            {
                UIFactory.Text(_content, "None", "Esta escena no tiene hipótesis.", _theme.BodyFont, _theme.SizeM, _theme.Ink);
            }
            else
            {
                foreach (var q in hyp.Questions) BuildQuestion(q);
            }
            RefreshFooter();
        }

        void BuildQuestion(HypothesisQuestion q)
        {
            var svc = _game.Hypotheses;
            if (!_selection.TryGetValue(q.Id, out var selected))
            {
                selected = new HashSet<string>(_game.State.GetHypothesisAnswer(q.Id)?.optionIds ?? new List<string>());
                _selection[q.Id] = selected;
            }
            var qText = UIFactory.Text(_content, $"Q_{q.Id}", q.Text + (q.MultiSelect ? "  <size=70%>(elige todas las que correspondan)</size>" : ""),
                _theme.TitleFont, _theme.SizeM, _theme.Ink);
            UIFactory.Layout(qText.gameObject, preferredHeight: 40);

            foreach (var option in q.Options)
            {
                var opt = option;
                bool unlocked = svc.IsUnlocked(opt);
                bool isSelected = selected.Contains(opt.Id);
                string label = unlocked
                    ? (isSelected ? "[x]  " : "[  ]  ") + opt.Text
                    : $"[Bloqueada] {opt.Text}  <size=75%>— {svc.LockReason(opt)}</size>";
                var bg = !unlocked ? UITheme.WithAlpha(_theme.Muted, 0.35f) : isSelected ? _theme.WarningAmber : UITheme.WithAlpha(_theme.BgDark, 0.08f);
                var b = UIFactory.Button(_content, label, _theme, bg, unlocked ? _theme.Ink : _theme.Muted, () => Toggle(q, opt), 50, _theme.SizeBody);
                b.GetComponentInChildren<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineLeft;
                b.interactable = unlocked;
            }

            var row = UIFactory.Rect($"Row_{q.Id}", _content);
            UIFactory.HLayout(row.gameObject, 12, 0, false);
            UIFactory.Layout(row.gameObject, preferredHeight: 48);
            var answer = UIFactory.Button(row, "Responder", _theme, _theme.BgDark, _theme.TextLight, () => Submit(q), 48);
            UIFactory.Layout(answer.gameObject, minHeight: 48, preferredHeight: 48, preferredWidth: 220);
            answer.interactable = selected.Count > 0;
            var state = UIFactory.Text(row, "State", svc.IsAnswered(q) ? "Respuesta registrada" : "", _theme.BodyFont, _theme.SizeS, _theme.Muted, TextAlignmentOptions.MidlineLeft);
            UIFactory.Layout(state.gameObject, flexibleWidth: 1);
        }

        void Toggle(HypothesisQuestion q, HypothesisOption opt)
        {
            var selected = _selection[q.Id];
            if (q.MultiSelect)
            {
                if (!selected.Remove(opt.Id)) selected.Add(opt.Id);
            }
            else
            {
                selected.Clear();
                selected.Add(opt.Id);
            }
            Rebuild();
        }

        void Submit(HypothesisQuestion q)
        {
            _game.Hypotheses.Submit(q, _selection[q.Id]);
            Rebuild();
        }

        void RefreshFooter()
        {
            var hyp = _game.Scene.Hypothesis;
            var decision = _game.Scene.ClosingDecision;
            bool complete = _game.Hypotheses.IsComplete(hyp);
            bool taken = decision != null && _game.Decisions.HasTaken(decision);
            _decide.gameObject.SetActive(decision != null);
            _decide.interactable = complete && !taken;
            _footer.text = decision == null ? ""
                : taken ? "La decisión de esta escena ya está tomada."
                : complete ? "Hipótesis completa. Puedes tomar la decisión de cierre."
                : "Responde todas las preguntas para cerrar la escena. Puedes volver a explorar y cambiar tus respuestas.";
        }

        void OnDecide()
        {
            var decision = _game.Scene.ClosingDecision;
            Close();
            if (decision != null) DecisionRequested?.Invoke(decision);
        }
    }
}
