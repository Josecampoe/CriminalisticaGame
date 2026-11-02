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
            UIFactory.Backdrop(rt, _theme, 0.9f);
            var head = UIFactory.Rect("Encabezado", rt);
            UIFactory.Place(head, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(60, -30), new Vector2(1300, 84));
            UIFactory.VLayout(head.gameObject, 2);
            UIFactory.Kicker(head, "Tablero de hipótesis", _theme, _theme.WarningAmber);
            _title = UIFactory.Text(head, "Titulo", "", _theme.TitleFont, 40, _theme.TextLight);
            var close = UIFactory.Ghost(rt, "Cerrar   Esc", _theme, _theme.Muted, Close, 48);
            UIFactory.Place((RectTransform)close.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-60, -40), new Vector2(200, 48));

            var board = UIFactory.AsPaper(UIFactory.Panel(rt, "Board", _theme.Paper), _theme);
            UIFactory.Region(board.rectTransform, new Vector2(0.06f, 0.15f), new Vector2(0.94f, 0.86f));
            _content = UIFactory.ScrollList(board.transform, "Questions", 12, 44);
            UIFactory.Stretch((RectTransform)_content.parent.parent);

            _footer = UIFactory.Text(rt, "Footer", "", _theme.BodyFont, _theme.SizeBody, _theme.TextLight, TextAlignmentOptions.MidlineLeft);
            UIFactory.Place(_footer.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(60, 40), new Vector2(1100, 50));
            _main = UIFactory.Button(rt, "Tomar la decisión", _theme, _theme.AccentRed, _theme.TextLight, OnMain, 60, _theme.SizeM - 2);
            UIFactory.Place((RectTransform)_main.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-60, 40), new Vector2(400, 60));
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
            _title.text = all ? "Hipótesis del caso" : $"Hipótesis · {_game.Scene.DisplayName}";
            foreach (var hyp in Shown())
            {
                if (all)
                {
                    var scene = _game.Case.FindScene(hyp.SceneId);
                    var header = UIFactory.Kicker(_content, scene != null ? scene.DisplayName : hyp.Id, _theme, _theme.AccentRed);
                    header.gameObject.name = $"H_{hyp.Id}";
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
            var qText = UIFactory.Text(_content, $"Q_{q.Id}", q.Text + (q.MultiSelect
                    ? $"  <size=60%><font=\"{_theme.BodyFont.name}\"><color={UITheme.Hex(UITheme.WithAlpha(_theme.Ink, 0.55f))}>elige todas las que correspondan</color></font></size>" : ""),
                _theme.TitleFont, 30, _theme.Ink);
            UIFactory.Layout(qText.gameObject, preferredHeight: 44);

            foreach (var option in q.Options)
            {
                var opt = option;
                bool unlocked = svc.IsUnlocked(opt);
                bool isSelected = selected.Contains(opt.Id);
                string label = unlocked
                    ? opt.Text
                    : $"<color={UITheme.Hex(_theme.AccentRed)}><font=\"{_theme.MonoFont.name}\"><size=75%>BLOQUEADA</size></font></color>   {opt.Text}" +
                      $"\n<size=75%><i>Falta analizar: {svc.LockReason(opt).Replace("Analiza: ", "")}</i></size>";
                UIFactory.CheckRow(_content, label, _theme, isSelected, unlocked, () => Toggle(q, opt), true, unlocked ? 52 : 64, _theme.SizeBody + 1);
            }

            var row = UIFactory.Rect($"Row_{q.Id}", _content);
            UIFactory.HLayout(row.gameObject, 16, 0, false);
            UIFactory.Layout(row.gameObject, preferredHeight: 50);
            var answer = UIFactory.Button(row, svc.IsAnswered(q) ? "Actualizar respuesta" : "Responder", _theme, _theme.Ink, _theme.Paper, () => Submit(q), 48);
            UIFactory.Layout(answer.gameObject, minHeight: 48, preferredHeight: 48, preferredWidth: 260);
            answer.interactable = selected.Count > 0;
            var state = UIFactory.Text(row, "State", svc.IsAnswered(q) ? "RESPUESTA REGISTRADA" : "", _theme.MonoFont, _theme.SizeS, Color.Lerp(_theme.AlexCyan, Color.black, 0.5f), TextAlignmentOptions.MidlineLeft);
            UIFactory.Layout(state.gameObject, flexibleWidth: 1);
            var gap = UIFactory.Divider(_content, UITheme.WithAlpha(_theme.Ink, 0.12f));
            UIFactory.Layout(gap.gameObject, minHeight: 1, preferredHeight: 1);
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
