using System;
using Caso404.Core;
using Caso404.Data;
using Caso404.Decisions;
using TMPro;
using UnityEngine;

namespace Caso404.UI
{
    /// <summary>
    /// Pantalla de decisión: oscurecida, la pregunta grande, la recomendación de ALEX en cian y 2–3 opciones grandes.
    /// Las decisiones irreversibles piden confirmación antes de aplicarse.
    /// </summary>
    public class DecisionView : UIPanel
    {
        /// <summary>La decisión ya se aplicó (el controlador muestra el resultado y cambia de escena).</summary>
        public event Action<DecisionOutcome> Resolved;

        [SerializeField] private GameContext _game;

        TextMeshProUGUI _prompt;
        TextMeshProUGUI _alex;
        TextMeshProUGUI _confirmText;
        RectTransform _options;
        RectTransform _confirm;
        DecisionData _decision;
        DecisionOption _pending;

        public override bool ClosesWithEscape => _pending == null;

        protected override void Build()
        {
            var rt = UIFactory.Stretch((RectTransform)transform);
            var dim = UIFactory.Panel(rt, "Dim", UITheme.WithAlpha(_theme.BgDark, 0.97f));
            UIFactory.Stretch(dim.rectTransform);

            _prompt = UIFactory.Text(rt, "Prompt", "", _theme.TitleFont, _theme.SizeXL, _theme.TextLight, TextAlignmentOptions.Center);
            UIFactory.Place(_prompt.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -120), new Vector2(1500, 80));
            _alex = UIFactory.Text(rt, "Alex", "", _theme.MonoFont, _theme.SizeBody, _theme.AlexCyan, TextAlignmentOptions.Center);
            UIFactory.Place(_alex.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -220), new Vector2(1300, 90));

            _options = UIFactory.Rect("Options", rt);
            UIFactory.Place(_options, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -60), new Vector2(980, 300));
            UIFactory.VLayout(_options.gameObject, 18);

            _confirm = UIFactory.Rect("Confirm", rt);
            UIFactory.Place(_confirm, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -60), new Vector2(980, 260));
            UIFactory.VLayout(_confirm.gameObject, 18);
            _confirmText = UIFactory.Text(_confirm, "Text", "", _theme.BodyFont, _theme.SizeM, _theme.TextLight, TextAlignmentOptions.Center);
            UIFactory.Layout(_confirmText.gameObject, preferredHeight: 110);
            UIFactory.Button(_confirm, "Confirmar", _theme, _theme.AccentRed, _theme.TextLight, ConfirmPending, 64, _theme.SizeM);
            UIFactory.Button(_confirm, "Volver", _theme, _theme.Muted, _theme.TextLight, CancelPending, 56);
            _confirm.gameObject.SetActive(false);
        }

        /// <summary>Muestra una decisión.</summary>
        public void Show(DecisionData decision)
        {
            if (decision == null || _game.Decisions.HasTaken(decision)) return;
            _decision = decision;
            _pending = null;
            _prompt.text = decision.Prompt;
            var recommended = decision.FindOption(decision.AlexRecommendedOptionId);
            _alex.text = recommended != null ? $"ALEX recomienda: {recommended.Text}\n\"{decision.AlexRecommendationText}\"" : "";
            UIFactory.Clear(_options);
            foreach (var option in decision.Options)
            {
                var opt = option;
                string label = (opt == recommended) ? $"{opt.Text}   <size=70%>(recomendado por ALEX)</size>" : opt.Text;
                var b = UIFactory.Button(_options, label, _theme, _theme.Paper, _theme.Ink, () => Pick(opt), 72, _theme.SizeM);
                b.interactable = _game.Decisions.IsAvailable(opt);
            }
            _options.gameObject.SetActive(true);
            _confirm.gameObject.SetActive(false);
            Open();
        }

        void Pick(DecisionOption option)
        {
            if (!_decision.Irreversible) { Apply(option); return; }
            _pending = option;
            _confirmText.text = $"«{option.Text}»\n¿Seguro? Esta decisión no se puede deshacer.";
            _options.gameObject.SetActive(false);
            _confirm.gameObject.SetActive(true);
        }

        void ConfirmPending()
        {
            if (_pending != null) Apply(_pending);
        }

        void CancelPending()
        {
            _pending = null;
            _options.gameObject.SetActive(true);
            _confirm.gameObject.SetActive(false);
        }

        void Apply(DecisionOption option)
        {
            var outcome = _game.Decisions.Choose(_decision, option);
            _pending = null;
            Close();
            if (outcome.Applied) Resolved?.Invoke(outcome);
        }
    }
}
