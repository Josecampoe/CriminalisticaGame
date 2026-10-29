using System.Linq;
using Caso404.Core;
using Caso404.Data;
using TMPro;
using UnityEngine;
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
            var dim = UIFactory.Panel(rt, "Dim", UITheme.WithAlpha(_theme.BgDark, 0.94f));
            UIFactory.Stretch(dim.rectTransform);

            _header = UIFactory.Text(rt, "Header", "EXPEDIENTE", _theme.TitleFont, _theme.SizeL, _theme.TextLight);
            UIFactory.Place(_header.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(60, -36), new Vector2(1200, 50));
            var close = UIFactory.Button(rt, "Cerrar (Esc)", _theme, _theme.Muted, _theme.TextLight, Close);
            UIFactory.Place((RectTransform)close.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-60, -36), new Vector2(200, 48));

            var listBg = UIFactory.Panel(rt, "ListBg", UITheme.WithAlpha(_theme.Paper, 0.06f));
            UIFactory.Region(listBg.rectTransform, new Vector2(0.03f, 0.06f), new Vector2(0.38f, 0.86f));
            _list = UIFactory.ScrollList(listBg.transform, "Cards", 10, 12);
            UIFactory.Stretch((RectTransform)_list.parent.parent);

            var card = UIFactory.AsPaper(UIFactory.Panel(rt, "Detail", _theme.Paper), _theme);
            UIFactory.Region(card.rectTransform, new Vector2(0.41f, 0.06f), new Vector2(0.97f, 0.86f));
            var stamp = UIFactory.Stamp(card.transform, "EVIDENCIA", _theme, _theme.SizeM, 8f);
            UIFactory.Place(stamp.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-24, -18), new Vector2(260, 50));
            UIFactory.VLayout(card.gameObject, 12, 36);
            _name = UIFactory.Text(card.transform, "Name", "", _theme.TitleFont, _theme.SizeL, _theme.Ink);
            _description = UIFactory.Text(card.transform, "Description", "", _theme.BodyFont, _theme.SizeBody, _theme.Ink);
            _detail = UIFactory.Text(card.transform, "Detail", "", _theme.BodyFont, _theme.SizeBody, _theme.AccentRed);
            _reportTitle = UIFactory.Text(card.transform, "ReportTitle", "", _theme.TitleFont, _theme.SizeM, _theme.AccentRed);
            _report = UIFactory.Text(card.transform, "Report", "", _theme.BodyFont, _theme.SizeM, _theme.Ink);
            _reason = UIFactory.Text(card.transform, "Reason", "", _theme.BodyFont, _theme.SizeS, _theme.Muted);
            _analyze = UIFactory.Button(card.transform, "Analizar", _theme, _theme.WarningAmber, _theme.Ink, OnAnalyze, 52);
            _password = UIFactory.Input(card.transform, _theme, "Contraseña", 40);
            _extra = UIFactory.Button(card.transform, "Análisis adicional", _theme, _theme.BgDark, _theme.TextLight, OnExtra, 52);
            _extraResult = UIFactory.Text(card.transform, "ExtraResult", "", _theme.BodyFont, _theme.SizeBody, _theme.Ink);
            UIFactory.Layout(UIFactory.Rect("Spacer", card.transform).gameObject, flexibleHeight: 1);

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
            _header.text = $"EXPEDIENTE — Caso: {_game.Case.Title}";
            if (_selected == null || !_game.Evidence.IsFound(_selected)) _selected = _game.Evidence.FoundEvidence.FirstOrDefault();
            Refresh();
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
                string status = !ev.CanBeAnalyzed ? "Sin valor como prueba" : analyzed ? "ANALIZADA" : "NUEVA — sin analizar";
                var bg = ev == _selected ? _theme.WarningAmber : _theme.Paper;
                var btn = UIFactory.Button(_list, $"{ev.DisplayName}\n<size=70%>{status}</size>", _theme, bg, _theme.Ink, () => { _selected = captured; Refresh(); }, 74);
                btn.GetComponentInChildren<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineLeft;
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
