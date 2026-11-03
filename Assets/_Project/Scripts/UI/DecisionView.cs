using System;
using Caso404.Core;
using Caso404.Data;
using Caso404.Decisions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
        RectTransform _alexBox;
        TextMeshProUGUI _confirmText;
        RectTransform _options;
        RectTransform _confirm;
        DecisionData _decision;
        DecisionOption _pending;

        public override bool ClosesWithEscape => _pending == null;

        protected override void Build()
        {
            var rt = UIFactory.Stretch((RectTransform)transform);
            UIFactory.Backdrop(rt, _theme, 0.94f);

            var kicker = UIFactory.Kicker(rt, "Decisión", _theme, _theme.AccentRed, TextAlignmentOptions.Center);
            UIFactory.Place(kicker.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -110), new Vector2(800, 24));
            _prompt = UIFactory.Text(rt, "Prompt", "", _theme.TitleFont, 54, _theme.TextLight, TextAlignmentOptions.Center);
            UIFactory.Place(_prompt.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -140), new Vector2(1500, 80));

            // Recomendación de ALEX como una franja de terminal.
            var alexBg = UIFactory.Round(UIFactory.Panel(rt, "Alex", new Color(0.04f, 0.08f, 0.09f, 0.92f)), _theme);
            _alexBox = alexBg.rectTransform;
            UIFactory.Place(_alexBox, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -250), new Vector2(1100, 110));
            UIFactory.Frame(alexBg.transform, _theme, UITheme.WithAlpha(_theme.AlexCyan, 0.35f));
            _alex = UIFactory.Text(alexBg.transform, "Texto", "", _theme.MonoFont, _theme.SizeBody, _theme.AlexCyan, TextAlignmentOptions.MidlineLeft);
            UIFactory.Stretch(_alex.rectTransform, 28, 10, 28, 10);

            _options = UIFactory.Rect("Options", rt);
            UIFactory.Place(_options, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -400), new Vector2(1100, 360));
            UIFactory.VLayout(_options.gameObject, 16);

            _confirm = UIFactory.Rect("Confirm", rt);
            UIFactory.Place(_confirm, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -400), new Vector2(1000, 300));
            UIFactory.VLayout(_confirm.gameObject, 18);
            _confirmText = UIFactory.Text(_confirm, "Text", "", _theme.BodyFont, _theme.SizeM, _theme.TextLight, TextAlignmentOptions.Center);
            UIFactory.Layout(_confirmText.gameObject, preferredHeight: 110);
            UIFactory.Button(_confirm, "Confirmar", _theme, _theme.AccentRed, _theme.TextLight, ConfirmPending, 64, _theme.SizeM);
            UIFactory.Ghost(_confirm, "Volver", _theme, _theme.Muted, CancelPending, 56);
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
            _alex.text = recommended != null ? $"<b>ALEX</b>  <color={UITheme.Hex(UITheme.WithAlpha(_theme.AlexCyan, 0.6f))}>recomienda: {recommended.Text}</color>\n“{decision.AlexRecommendationText}”" : "";
            _alexBox.gameObject.SetActive(recommended != null);
            UIFactory.Clear(_options);
            char letter = 'A';
            foreach (var option in decision.Options)
            {
                var opt = option;
                bool available = _game.Decisions.IsAvailable(opt);
                string tag = opt == recommended ? $"   <color={UITheme.Hex(_theme.AlexCyan)}>· RECOMENDADA POR ALEX</color>" : "";
                string label = $"<size=60%><font=\"{_theme.MonoFont.name}\"><color={UITheme.Hex(UITheme.WithAlpha(_theme.TextLight, 0.5f))}>OPCIÓN {letter}</color>{tag}</font></size>\n{opt.Text}";
                letter++;
                var b = UIFactory.Button(_options, label, _theme, UITheme.WithAlpha(_theme.TextLight, 0.07f), _theme.TextLight, () => Pick(opt), 96, _theme.SizeM);
                var t = b.GetComponentInChildren<TextMeshProUGUI>();
                t.alignment = TextAlignmentOptions.MidlineLeft;
                t.fontStyle = FontStyles.Normal;
                t.rectTransform.offsetMin = new Vector2(36, 6);
                b.interactable = available;
            }
            _options.gameObject.SetActive(true);
            _confirm.gameObject.SetActive(false);
            Open();
        }

        void Pick(DecisionOption option)
        {
            if (!_decision.Irreversible) { Apply(option); return; }
            _pending = option;
            _confirmText.text = $"«{option.Text}»\n<size=75%><color={UITheme.Hex(UITheme.WithAlpha(_theme.TextLight, 0.6f))}>¿Seguro? Esta decisión no se puede deshacer.</color></size>";
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
