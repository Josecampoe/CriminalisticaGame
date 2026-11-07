using System.Linq;
using Caso404.Core;
using Caso404.Data;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Caso404.UI
{
    /// <summary>
    /// Expediente: a la izquierda las fichas de papel (nombre y estado), a la derecha la ficha ampliada con
    /// el botón Analizar y el informe. La acción principal es Analizar. Llama a EvidenceService; no toca el estado.
    /// </summary>
    public class JournalView : UIPanel
    {
        [SerializeField] private GameContext _game;

        RectTransform _list;
        TextMeshProUGUI _header;
        TextMeshProUGUI _name;
        TextMeshProUGUI _description;
        TextMeshProUGUI _detail;
        TextMeshProUGUI _reportTitle;
        TextMeshProUGUI _report;
        TextMeshProUGUI _extraResult;
        TextMeshProUGUI _reason;
        Button _analyze;
        Button _extra;
        TMP_InputField _password;
        EvidenceData _selected;
        TextMeshProUGUI _glitch;

        protected override void Build()
        {
            var rt = UIFactory.Stretch((RectTransform)transform);
            UIFactory.Backdrop(rt, _theme, 0.9f);
            var head = UIFactory.Rect("Encabezado", rt);
            UIFactory.Place(head, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(60, -30), new Vector2(1300, 84));
            UIFactory.VLayout(head.gameObject, 2);
            UIFactory.Kicker(head, "Expediente", _theme, _theme.WarningAmber);
            _header = UIFactory.Text(head, "Titulo", "", _theme.TitleFont, 40, _theme.TextLight);
            var close = UIFactory.Ghost(rt, "Cerrar   Esc", _theme, _theme.Muted, Close, 48);
            UIFactory.Place((RectTransform)close.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-60, -40), new Vector2(200, 48));

            var listBg = UIFactory.Round(UIFactory.Panel(rt, "Fichas", UITheme.WithAlpha(_theme.TextLight, 0.03f)), _theme);
            UIFactory.Region(listBg.rectTransform, new Vector2(0.03f, 0.06f), new Vector2(0.37f, 0.86f));
            UIFactory.Frame(listBg.transform, _theme, UITheme.WithAlpha(_theme.TextLight, 0.08f));
            _list = UIFactory.ScrollList(listBg.transform, "Cards", 8, 12);
            UIFactory.Stretch((RectTransform)_list.parent.parent);

            var card = UIFactory.AsPaper(UIFactory.Panel(rt, "Detail", _theme.Paper), _theme);
            UIFactory.Region(card.rectTransform, new Vector2(0.41f, 0.06f), new Vector2(0.97f, 0.86f));
            var stamp = UIFactory.Stamp(card.transform, "EVIDENCIA", _theme, _theme.SizeM, 8f);
            UIFactory.Place(stamp.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-24, -18), new Vector2(260, 50));
            UIFactory.VLayout(card.gameObject, 12, 36);
            var v = card.GetComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(56, 56, 48, 40);
            _name = UIFactory.Text(card.transform, "Name", "", _theme.TitleFont, 38, _theme.Ink);
            UIFactory.Divider(card.transform, UITheme.WithAlpha(_theme.Ink, 0.2f));
            _description = UIFactory.Text(card.transform, "Description", "", _theme.BodyFont, _theme.SizeBody, _theme.Ink);
            _detail = UIFactory.Text(card.transform, "Detail", "", _theme.BodyFont, _theme.SizeBody, _theme.AccentRed);
            _reportTitle = UIFactory.Text(card.transform, "ReportTitle", "", _theme.MonoFont, _theme.SizeS + 1, _theme.AccentRed);
            _reportTitle.characterSpacing = 8f;
            _reportTitle.fontStyle = FontStyles.Bold;
            _report = UIFactory.Text(card.transform, "Report", "", _theme.BodyFont, _theme.SizeM, _theme.Ink);
            _reason = UIFactory.Text(card.transform, "Reason", "", _theme.BodyFont, _theme.SizeS, _theme.Muted);
            _analyze = UIFactory.Button(card.transform, "Analizar evidencia   Enter", _theme, _theme.Ink, _theme.Paper, OnAnalyze, 56);
            _password = UIFactory.Input(card.transform, _theme, "Contraseña", 40);
            _extra = UIFactory.Button(card.transform, "Análisis adicional", _theme, _theme.Ink, _theme.Paper, OnExtra, 56);
            _extraResult = UIFactory.Text(card.transform, "ExtraResult", "", _theme.BodyFont, _theme.SizeBody, _theme.Ink);
            UIFactory.Layout(UIFactory.Rect("Spacer", card.transform).gameObject, flexibleHeight: 1);

            var keys = UIFactory.Text(rt, "Teclas",
                "<mark=#EDEDED26 padding=\"10,10,4,4\"><b>↑ ↓</b></mark>  cambiar de ficha      <mark=#EDEDED26 padding=\"10,10,4,4\"><b>Enter</b></mark>  analizar      <mark=#EDEDED26 padding=\"10,10,4,4\"><b>Tab</b></mark>  cerrar",
                _theme.BodyFont, _theme.SizeS, UITheme.WithAlpha(_theme.TextLight, 0.55f), TextAlignmentOptions.MidlineLeft);
            UIFactory.Place(keys.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(60, 18), new Vector2(900, 30));
            keys.raycastTarget = false;

            _glitch = UIFactory.Text(rt, "Glitch404", "404", _theme.MonoFont, 220, _theme.AlexCyan, TextAlignmentOptions.Center);
            UIFactory.Stretch(_glitch.rectTransform);
            _glitch.gameObject.SetActive(false);
        }

        void OnEnable()
        {
            if (_game != null && _game.State != null) _game.State.Changed += OnStateChanged;
        }

        void OnDisable()
        {
            if (_game != null && _game.State != null) _game.State.Changed -= OnStateChanged;
        }

        protected override void OnShown()
        {
            int found = _game.Evidence.FoundEvidence.Count();
            int pending = _game.Evidence.UnanalyzedCount;
            _header.text = $"{_game.Case.Title}   <size=50%><color={UITheme.Hex(UITheme.WithAlpha(_theme.TextLight, 0.55f))}>{found} ficha{(found == 1 ? "" : "s")}" +
                           (pending > 0 ? $" · <color={UITheme.Hex(_theme.WarningAmber)}>{pending} sin analizar</color>" : "") + "</color></size>";
            // Se abre en la primera evidencia que falta analizar: es lo que el jugador viene a hacer.
            var pendingEvidence = _game.Evidence.FoundEvidence.FirstOrDefault(e => _game.Evidence.CanAnalyze(e, out _));
            if (pendingEvidence != null) _selected = pendingEvidence;
            else if (_selected == null || !_game.Evidence.IsFound(_selected)) _selected = _game.Evidence.FoundEvidence.FirstOrDefault();
            Refresh();
        }

        /// <summary>Teclado: flechas (o W/S) para cambiar de ficha y Enter para analizar.</summary>
        void Update()
        {
            if (!IsOpen || _ui == null || _ui.Top != this || UIFactory.IsTyping()) return;
            var kb = Keyboard.current;
            if (kb == null) return;
            int step = kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame ? 1
                     : kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame ? -1 : 0;
            if (step != 0)
            {
                var found = _game.Evidence.FoundEvidence.ToList();
                if (found.Count == 0) return;
                int i = Mathf.Clamp(found.IndexOf(_selected) + step, 0, found.Count - 1);
                _selected = found[i];
                Refresh();
                // Mantiene visible la ficha elegida cuando la lista es más larga que la pantalla.
                var scroll = _list.GetComponentInParent<ScrollRect>();
                if (scroll != null && found.Count > 1) scroll.verticalNormalizedPosition = 1f - (float)i / (found.Count - 1);
                return;
            }
            if ((kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame) &&
                _analyze.gameObject.activeInHierarchy && _analyze.interactable)
            {
                UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);
                OnAnalyze();
            }
        }

        void OnStateChanged()
        {
            if (IsOpen) Refresh();
        }

        void Refresh()
        {
            RebuildList();
            ShowDetail(_selected);
        }

        void RebuildList()
        {
            UIFactory.Clear(_list);
            var found = _game.Evidence.FoundEvidence.ToList();
            if (found.Count == 0)
            {
                UIFactory.Text(_list, "Empty", "Todavía no hay evidencias. Explora la escena y pulsa E sobre los objetos.",
                    _theme.BodyFont, _theme.SizeBody, _theme.TextLight);
                return;
            }
            foreach (var ev in found)
            {
                var captured = ev;
                bool analyzed = _game.Evidence.IsAnalyzed(ev);
                var (status, color) = !ev.CanBeAnalyzed ? ("SIN VALOR COMO PRUEBA", _theme.Muted)
                    : analyzed ? ("ANALIZADA", _theme.AlexCyan) : ("NUEVA · SIN ANALIZAR", _theme.WarningAmber);
                bool selected = ev == _selected;
                var bg = selected ? UITheme.WithAlpha(_theme.Paper, 0.95f) : UITheme.WithAlpha(_theme.TextLight, 0.05f);
                var fg = selected ? _theme.Ink : _theme.TextLight;
                var statusColor = selected ? Color.Lerp(color, Color.black, 0.45f) : color;
                var btn = UIFactory.Button(_list, $"{ev.DisplayName}\n<size=68%><font=\"{_theme.MonoFont.name}\"><color={UITheme.Hex(statusColor)}>● {status}</color></font></size>",
                    _theme, bg, fg, () => { _selected = captured; Refresh(); }, 72);
                var label = btn.GetComponentInChildren<TextMeshProUGUI>();
                label.alignment = TextAlignmentOptions.MidlineLeft;
                label.rectTransform.offsetMin = new Vector2(22, 4);
            }
        }

        void ShowDetail(EvidenceData ev)
        {
            bool has = ev != null;
            _name.text = has ? ev.DisplayName : "";
            _description.text = has ? ev.Description : "Selecciona una ficha.";
            _detail.text = has && ev.HasHiddenDetail && _game.State.IsHiddenDetailSeen(ev.Id) ? ev.HiddenDetailText : "";
            bool analyzed = has && _game.Evidence.IsAnalyzed(ev);
            _reportTitle.text = analyzed ? (ev.AnalyzedByAlex ? "INFORME DE ALEX" : "INFORME DE LABORATORIO") : "";
            _report.text = analyzed ? ev.AnalysisResult : "";
            _report.font = analyzed && ev.AnalyzedByAlex ? _theme.MonoFont : _theme.BodyFont;
            _report.color = analyzed && ev.AnalyzedByAlex ? Darken(_theme.AlexCyan) : _theme.Ink;

            string reason = "";
            bool canAnalyze = has && _game.Evidence.CanAnalyze(ev, out reason);
            _analyze.gameObject.SetActive(has && !analyzed && ev.CanBeAnalyzed);
            _analyze.interactable = canAnalyze;
            _reason.text = has && !analyzed && !canAnalyze ? (reason ?? "") : "";
            if (has && !ev.CanBeAnalyzed) _reason.text = ev.AnalysisResult;

            bool extraVisible = analyzed && ev.HasExtraAnalysis;
            bool extraDone = extraVisible && _game.State.IsExtraAnalysisDone(ev.Id);
            _extra.gameObject.SetActive(extraVisible && !extraDone);
            if (extraVisible) UIFactory.SetLabel(_extra, ev.ExtraAnalysis.ButtonLabel);
            _password.gameObject.SetActive(extraVisible && !extraDone && ev.ExtraAnalysis.Kind == ExtraAnalysisKind.Password);
            _extraResult.text = extraDone
                ? ev.ExtraAnalysis.ResultText + (string.IsNullOrEmpty(ev.ExtraAnalysis.AlexComment) ? "" : $"\n<color=#{ColorUtility.ToHtmlStringRGB(Darken(_theme.AlexCyan))}>ALEX: \"{ev.ExtraAnalysis.AlexComment}\"</color>")
                : "";
        }

        void OnAnalyze()
        {
            if (_selected == null) return;
            _game.Evidence.Analyze(_selected);
            Refresh();
        }

        void OnExtra()
        {
            if (_selected == null) return;
            var ev = _selected;
            var result = _game.Evidence.RunExtraAnalysis(ev, _password.text);
            Refresh();
            if (!result.Success) _reason.text = result.Text;
            else if (ev.ExtraAnalysis.AlexGlitch) StartCoroutine(Glitch404());
        }

        /// <summary>GUION §4.5: al abrir la carpeta directiva_404, la interfaz de ALEX hace glitch y aparece "404" medio segundo.</summary>
        System.Collections.IEnumerator Glitch404()
        {
            _glitch.gameObject.SetActive(true);
            for (float t = 0; t < 0.5f; t += Time.unscaledDeltaTime)
            {
                _glitch.color = Random.value > 0.5f ? _theme.AlexCyan : _theme.AccentRed;
                _glitch.rectTransform.anchoredPosition = new Vector2(Random.Range(-12f, 12f), Random.Range(-4f, 4f));
                yield return null;
            }
            _glitch.rectTransform.anchoredPosition = Vector2.zero;
            _glitch.gameObject.SetActive(false);
        }

        // El cian sobre papel claro no llega a 4.5:1; se oscurece para mantener el contraste.
        static Color Darken(Color c) => Color.Lerp(c, Color.black, 0.55f);
    }
}
