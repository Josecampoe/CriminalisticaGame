using System.Collections;
using System.Linq;
using Caso404.AI;
using Caso404.Core;
using Caso404.Data;
using TMPro;
using UnityEngine;

namespace Caso404.UI
{
    /// <summary>
    /// Panel de ALEX (estilo terminal cian, lateral derecho). Muestra lo último que dijo ALEX y sus afirmaciones
    /// como tarjetas con "Aceptar" o "Cuestionar". Cuestionar pide una evidencia analizada; la lógica la decide AlexService.
    /// Con confianza baja, o al descubrir una mentira, el panel hace glitch.
    /// </summary>
    public class AlexPanelView : UIPanel
    {
        [SerializeField] private GameContext _game;

        RectTransform _panel;
        TextMeshProUGUI _speech;
        TextMeshProUGUI _status;
        TextMeshProUGUI _glitchLabel;
        RectTransform _claims;
        RectTransform _chooser;
        RectTransform _chooserList;
        TextMeshProUGUI _chooserTitle;
        LieData _questioning;
        bool _greeted;
        float _nextAmbientGlitch;
        Coroutine _glitch;

        protected override void Build()
        {
            var rt = UIFactory.Stretch((RectTransform)transform);
            var bg = UIFactory.AsScreen(UIFactory.Panel(rt, "Panel", UITheme.WithAlpha(_theme.BgDark, 0.97f)), _theme, _theme.AlexCyan);
            _panel = bg.rectTransform;
            _panel.anchorMin = new Vector2(1f, 0f);
            _panel.anchorMax = new Vector2(1f, 1f);
            _panel.pivot = new Vector2(1f, 0.5f);
            _panel.sizeDelta = new Vector2(620, 0);
            _panel.anchoredPosition = Vector2.zero;
            var border = UIFactory.Panel(_panel, "Border", _theme.AlexCyan);
            border.rectTransform.anchorMin = new Vector2(0, 0);
            border.rectTransform.anchorMax = new Vector2(0, 1);
            border.rectTransform.sizeDelta = new Vector2(4, 0);

            var content = UIFactory.Rect("Content", _panel);
            UIFactory.Stretch(content, 28, 24, 24, 24);
            UIFactory.VLayout(content.gameObject, 12);

            var title = UIFactory.Text(content, "Title", "ALEX // Asistente Lógico de EXpedientes", _theme.MonoFont, _theme.SizeM, _theme.AlexCyan);
            UIFactory.Layout(title.gameObject, preferredHeight: 34);
            _status = UIFactory.Text(content, "Status", "", _theme.MonoFont, _theme.SizeS, _theme.Muted);
            UIFactory.Layout(_status.gameObject, preferredHeight: 22);

            var speechBox = UIFactory.Panel(content, "Speech", UITheme.WithAlpha(_theme.AlexCyan, 0.08f));
            UIFactory.Layout(speechBox.gameObject, minHeight: 130, preferredHeight: 130);
            _speech = UIFactory.Text(speechBox.transform, "Text", "", _theme.MonoFont, _theme.SizeBody, _theme.AlexCyan);
            UIFactory.Stretch(_speech.rectTransform, 14, 10, 14, 10);

            var ask = UIFactory.Rect("Ask", content);
            UIFactory.VLayout(ask.gameObject, 6);
            UIFactory.Layout(ask.gameObject, preferredHeight: 3 * 40 + 12);
            UIFactory.Button(ask, "¿Quién crees que fue?", _theme, UITheme.WithAlpha(_theme.AlexCyan, 0.18f), _theme.TextLight, () => Say(_game.Case.AlexLines.Crime.Text), 40, _theme.SizeS);
            UIFactory.Button(ask, "Pero la ventana se rompió desde adentro.", _theme, UITheme.WithAlpha(_theme.AlexCyan, 0.18f), _theme.TextLight, () => Say(_game.Case.AlexLines.Evidence.Text), 40, _theme.SizeS);
            UIFactory.Button(ask, "¿Por qué tu archivo dio error justo en esos 15 minutos?", _theme, UITheme.WithAlpha(_theme.AlexCyan, 0.18f), _theme.TextLight, () => Say(_game.Case.AlexLines.Press.Text), 40, _theme.SizeS);

            var claimsTitle = UIFactory.Text(content, "ClaimsTitle", "OBSERVACIONES DE ALEX", _theme.MonoFont, _theme.SizeS, _theme.Muted);
            UIFactory.Layout(claimsTitle.gameObject, preferredHeight: 22);
            var claimsHolder = UIFactory.Rect("ClaimsHolder", content);
            UIFactory.Layout(claimsHolder.gameObject, flexibleHeight: 1, minHeight: 200);
            _claims = UIFactory.ScrollList(claimsHolder, "Claims", 10, 0);
            UIFactory.Stretch((RectTransform)_claims.parent.parent);

            var close = UIFactory.Button(content, "Cerrar (Q / Esc)", _theme, _theme.Muted, _theme.TextLight, Close, 44, _theme.SizeS);
            UIFactory.Layout(close.gameObject, minHeight: 44, preferredHeight: 44);

            _glitchLabel = UIFactory.Text(_panel, "404", "404", _theme.MonoFont, 140, UITheme.WithAlpha(_theme.AccentRed, 0.85f), TextAlignmentOptions.Center);
            UIFactory.Stretch(_glitchLabel.rectTransform);
            _glitchLabel.gameObject.SetActive(false);

            // Selector de evidencia para Cuestionar.
            var chooserBg = UIFactory.Panel(rt, "Chooser", UITheme.WithAlpha(_theme.BgDark, 0.98f));
            _chooser = chooserBg.rectTransform;
            UIFactory.Region(_chooser, new Vector2(0.15f, 0.2f), new Vector2(0.6f, 0.85f));
            _chooserTitle = UIFactory.Text(_chooser, "Title", "", _theme.BodyFont, _theme.SizeBody, _theme.TextLight);
            UIFactory.Stretch(_chooserTitle.rectTransform, 24, 0, 24, 16).anchorMin = new Vector2(0, 0.82f);
            var holder = UIFactory.Rect("ListHolder", _chooser);
            UIFactory.Stretch(holder, 16, 76, 16, 0).anchorMax = new Vector2(1, 0.8f);
            _chooserList = UIFactory.ScrollList(holder, "List", 8, 4);
            UIFactory.Stretch((RectTransform)_chooserList.parent.parent);
            var back = UIFactory.Button(_chooser, "Volver", _theme, _theme.Muted, _theme.TextLight, CloseChooser);
            UIFactory.Place((RectTransform)back.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 14), new Vector2(240, 48));
            _chooser.gameObject.SetActive(false);
        }

        void OnEnable()
        {
            if (_game == null || _game.Alex == null) return;
            _game.Alex.ClaimRaised += OnClaimRaised;
            _game.State.Changed += OnStateChanged;
        }

        void OnDisable()
        {
            if (_game == null || _game.Alex == null) return;
            _game.Alex.ClaimRaised -= OnClaimRaised;
            _game.State.Changed -= OnStateChanged;
        }

        protected override void OnShown()
        {
            if (!_greeted)
            {
                _greeted = true;
                Say(_game.Case.AlexIntroLine);
            }
            Refresh();
        }

        protected override void OnHidden() => CloseChooser();

        void OnClaimRaised(LieData claim) => Say(claim.Statement);

        void OnStateChanged()
        {
            if (IsOpen) Refresh();
        }

        void Refresh()
        {
            var alex = _game.Alex;
            string tone = alex.Tone switch { AlexTone.Warm => "en línea", AlexTone.Formal => "en línea · modo formal", _ => "en línea · respuesta limitada" };
            _status.text = $"Estado: {tone}    Contradicciones detectadas: {alex.ContradictionCount}";
            _speech.color = alex.Tone == AlexTone.Cold ? Color.Lerp(_theme.AlexCyan, _theme.Muted, 0.5f) : _theme.AlexCyan;

            UIFactory.Clear(_claims);
            var claims = alex.RaisedClaims.ToList();
            if (claims.Count == 0)
                UIFactory.Text(_claims, "Empty", "ALEX todavía no ha hecho observaciones.", _theme.MonoFont, _theme.SizeS, _theme.Muted);
            foreach (var claim in claims) BuildCard(claim);
        }

        void BuildCard(LieData claim)
        {
            var alex = _game.Alex;
            bool refuted = alex.IsRefuted(claim);
            bool accepted = alex.IsAccepted(claim);
            var card = UIFactory.Panel(_claims, $"Card_{claim.Id}", UITheme.WithAlpha(refuted ? _theme.AccentRed : _theme.AlexCyan, 0.12f));
            UIFactory.VLayout(card.gameObject, 8, 14);
            var text = UIFactory.Text(card.transform, "Statement", $"\"{claim.Statement}\"", _theme.MonoFont, _theme.SizeS, refuted ? _theme.Muted : _theme.AlexCyan);
            if (refuted) text.fontStyle = FontStyles.Strikethrough;
            string status = refuted ? $"<color=#{ColorUtility.ToHtmlStringRGB(_theme.AccentRed)}>[REFUTADA] Contradicción registrada</color>" : accepted ? "[ACEPTADA]" : "[PENDIENTE]";
            UIFactory.Text(card.transform, "Status", status, _theme.MonoFont, _theme.SizeS, _theme.Muted);
            if (refuted) return;
            var row = UIFactory.Rect("Buttons", card.transform);
            UIFactory.HLayout(row.gameObject, 8);
            UIFactory.Layout(row.gameObject, preferredHeight: 40);
            if (!accepted) UIFactory.Button(row, "Aceptar", _theme, _theme.AlexCyan, _theme.Ink, () => Say(alex.Accept(claim)), 40, _theme.SizeS);
            UIFactory.Button(row, "Cuestionar", _theme, _theme.AccentRed, _theme.TextLight, () => OpenChooser(claim), 40, _theme.SizeS);
        }

        void OpenChooser(LieData claim)
        {
            _questioning = claim;
            _chooserTitle.text = $"¿Con qué evidencia refutas a ALEX?\n<size=80%><color=#{ColorUtility.ToHtmlStringRGB(_theme.AlexCyan)}>\"{claim.Statement}\"</color></size>";
            UIFactory.Clear(_chooserList);
            var evidence = _game.Evidence.AnalyzedEvidence.ToList();
            if (evidence.Count == 0)
                UIFactory.Text(_chooserList, "Empty", "No tienes evidencias analizadas.", _theme.BodyFont, _theme.SizeBody, _theme.TextLight);
            foreach (var ev in evidence)
            {
                var captured = ev;
                var b = UIFactory.Button(_chooserList, ev.DisplayName, _theme, _theme.Paper, _theme.Ink, () => Question(captured), 52, _theme.SizeS);
                b.GetComponentInChildren<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineLeft;
            }
            _chooser.gameObject.SetActive(true);
        }

        void CloseChooser()
        {
            if (_chooser != null) _chooser.gameObject.SetActive(false);
            _questioning = null;
        }

        void Question(EvidenceData evidence)
        {
            var claim = _questioning;
            CloseChooser();
            if (claim == null) return;
            var result = _game.Alex.Question(claim, evidence);
            Say(result.Response);
            if (result.Success) Glitch(0.2f, show404: true);
        }

        void Say(string text) => _speech.text = $"> {text}";

        void Update()
        {
            if (!IsOpen || _game.Alex.Tone != AlexTone.Cold || Time.unscaledTime < _nextAmbientGlitch) return;
            _nextAmbientGlitch = Time.unscaledTime + Random.Range(3f, 6f);
            Glitch(0.15f, show404: false);
        }

        void Glitch(float seconds, bool show404)
        {
            if (_glitch != null) StopCoroutine(_glitch);
            _glitch = StartCoroutine(GlitchRoutine(seconds, show404));
        }

        IEnumerator GlitchRoutine(float seconds, bool show404)
        {
            var basePos = Vector2.zero;
            if (show404) _glitchLabel.gameObject.SetActive(true);
            for (float t = 0; t < seconds; t += Time.unscaledDeltaTime)
            {
                _panel.anchoredPosition = basePos + new Vector2(Random.Range(-8f, 8f), Random.Range(-3f, 3f));
                _speech.color = Random.value > 0.5f ? _theme.AccentRed : _theme.AlexCyan;
                yield return null;
            }
            _panel.anchoredPosition = basePos;
            _speech.color = _theme.AlexCyan;
            if (show404)
            {
                yield return new WaitForSecondsRealtime(0.3f);
                _glitchLabel.gameObject.SetActive(false);
            }
            _glitch = null;
        }
    }
}
