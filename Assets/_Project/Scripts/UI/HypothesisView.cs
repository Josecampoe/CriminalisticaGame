using System;
using System.Collections.Generic;
using System.Linq;
using Caso404.Core;
using Caso404.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Caso404.UI
{
    /// <summary>
    /// Tablero de hipótesis. En una escena de exploración muestra su hipótesis; en la Comisaría, todas las del caso.
    /// Las opciones bloqueadas salen grises con el motivo (qué falta analizar).
    /// El botón principal cambia según la escena: decisión de cierre, terminar la escena o ir a la acusación.
    /// </summary>
    public class HypothesisView : UIPanel
    {
        /// <summary>El jugador quiere pasar a la decisión de cierre.</summary>
        public event Action<DecisionData> DecisionRequested;
        /// <summary>El jugador terminó una escena sin decisión de cierre (pasa a elegir la siguiente).</summary>
        public event Action SceneFinishRequested;
        /// <summary>El jugador quiere acusar (Comisaría).</summary>
        public event Action AccusationRequested;

        [SerializeField] private GameContext _game;

        RectTransform _content;
        Button _main;
        TextMeshProUGUI _title;
        TextMeshProUGUI _footer;
        readonly Dictionary<string, HashSet<string>> _selection = new();

        protected override void Build()
        {
            var rt = UIFactory.Stretch((RectTransform)transform);
            var dim = UIFactory.Panel(rt, "Dim", UITheme.WithAlpha(_theme.BgDark, 0.94f));
            UIFactory.Stretch(dim.rectTransform);
            _title = UIFactory.Text(rt, "Title", "TABLERO — Hipótesis", _theme.TitleFont, _theme.SizeL, _theme.TextLight);
            UIFactory.Place(_title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(60, -36), new Vector2(1300, 50));
            var close = UIFactory.Button(rt, "Cerrar (Esc)", _theme, _theme.Muted, _theme.TextLight, Close);
            UIFactory.Place((RectTransform)close.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-60, -36), new Vector2(200, 48));

            var board = UIFactory.Panel(rt, "Board", _theme.Paper);
            UIFactory.Region(board.rectTransform, new Vector2(0.05f, 0.14f), new Vector2(0.95f, 0.86f));
            _content = UIFactory.ScrollList(board.transform, "Questions", 18, 28);
            UIFactory.Stretch((RectTransform)_content.parent.parent);

            _footer = UIFactory.Text(rt, "Footer", "", _theme.BodyFont, _theme.SizeBody, _theme.TextLight, TextAlignmentOptions.MidlineLeft);
            UIFactory.Place(_footer.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(60, 40), new Vector2(1100, 50));
            _main = UIFactory.Button(rt, "Tomar la decisión", _theme, _theme.AccentRed, _theme.TextLight, OnMain);
            UIFactory.Place((RectTransform)_main.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-60, 40), new Vector2(380, 56));
        }

        protected override void OnShown() => Rebuild();

        /// <summary>Hipótesis que se muestran: la de la escena, o todas las del caso si la escena no tiene (Comisaría).</summary>
        IEnumerable<HypothesisData> Shown()
        {
            if (_game.Scene.Hypothesis != null) return new[] { _game.Scene.Hypothesis };
            return _game.Case.Hypotheses.Where(h => h != null);
        }

        void Rebuild()
        {
            UIFactory.Clear(_content);
            bool all = _game.Scene.Hypothesis == null;
            _title.text = all ? "TABLERO — Hipótesis del caso" : "TABLERO — Hipótesis de la escena";
            foreach (var hyp in Shown())
            {
                if (all)
                {
                    var scene = _game.Case.FindScene(hyp.SceneId);
                    var header = UIFactory.Text(_content, $"H_{hyp.Id}", scene != null ? scene.DisplayName.ToUpperInvariant() : hyp.Id,
                        _theme.TitleFont, _theme.SizeS, _theme.AccentRed);
                    UIFactory.Layout(header.gameObject, preferredHeight: 26);
                }
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
            var scene = _game.Scene;
            var decision = scene.ClosingDecision;
            bool complete = _game.Hypotheses.IsComplete(scene.Hypothesis);
            if (scene.HasAccusation)
            {
                bool accused = _game.State.State.accusations.Any(a => a.caseId == _game.Case.Id);
                UIFactory.SetLabel(_main, "Ir a la acusación");
                _main.interactable = !accused;
                _footer.text = accused ? "La acusación de este caso ya se presentó." : "Repasa el caso. Cuando estés listo(a), presenta la acusación.";
            }
            else if (decision != null)
            {
                bool taken = _game.Decisions.HasTaken(decision);
                UIFactory.SetLabel(_main, "Tomar la decisión");
                _main.interactable = complete && !taken;
                _footer.text = taken ? "La decisión de esta escena ya está tomada."
                    : complete ? "Hipótesis completa. Puedes tomar la decisión de cierre."
                    : "Responde todas las preguntas para cerrar la escena. Puedes volver a explorar y cambiar tus respuestas.";
            }
            else
            {
                bool done = _game.State.IsSceneCompleted(scene.Id);
                UIFactory.SetLabel(_main, "Terminar la escena");
                _main.interactable = complete && !done;
                _footer.text = done ? "Esta escena ya está terminada."
                    : complete ? "Hipótesis completa. Puedes seguir con la investigación."
                    : "Responde todas las preguntas para terminar la escena.";
            }
        }

        void OnMain()
        {
            var scene = _game.Scene;
            Close();
            if (scene.HasAccusation) AccusationRequested?.Invoke();
            else if (scene.ClosingDecision != null) DecisionRequested?.Invoke(scene.ClosingDecision);
            else SceneFinishRequested?.Invoke();
        }
    }
}
