using System;
using System.Collections;
using System.Linq;
using Caso404.Core;
using Caso404.Decisions;
using TMPro;
using UnityEngine;

namespace Caso404.UI
{
    /// <summary>
    /// Pantalla de final: título del final, texto del guion, resumen de lo que el jugador descubrió y lo que se le escapó,
    /// y después la pantalla negra "Tres meses después...".
    /// </summary>
    public class EndingView : UIPanel
    {
        [SerializeField] private GameContext _game;

        RectTransform _page1;
        RectTransform _page2;
        TextMeshProUGUI _title;
        TextMeshProUGUI _text;
        TextMeshProUGUI _summary;
        TextMeshProUGUI _closing;
        Action _onFinished;
        bool _skipClosing;
        Coroutine _typing;

        public override bool ClosesWithEscape => false;

        /// <summary>Id del final que se está mostrando (para pruebas).</summary>
        public string ShownEndingId { get; private set; }

        protected override void Build()
        {
            var rt = UIFactory.Stretch((RectTransform)transform);
            var bg = UIFactory.Panel(rt, "Bg", _theme.BgDark);
            UIFactory.Stretch(bg.rectTransform);

            _page1 = UIFactory.Stretch(UIFactory.Rect("Page1", rt));
            if (_theme.VignetteSprite != null)
            {
                var vignette = UIFactory.Panel(rt, "Vineta", new Color(0f, 0f, 0f, 0.9f));
                vignette.sprite = _theme.VignetteSprite;
                UIFactory.Stretch(vignette.rectTransform);
                vignette.transform.SetSiblingIndex(1);
            }
            var card = UIFactory.Rect("Card", _page1);
            UIFactory.Place(card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1240, 0));
            var cv = UIFactory.VLayout(card.gameObject, 26);
            card.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>().verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
            cv.childAlignment = TextAnchor.MiddleCenter;
            UIFactory.Kicker(card, "Final", _theme, _theme.AccentRed, TextAlignmentOptions.Center);
            _title = UIFactory.Text(card, "Title", "", _theme.TitleFont, 66, _theme.TextLight, TextAlignmentOptions.Center);
            UIFactory.Layout(_title.gameObject, preferredHeight: 84);
            var line = UIFactory.Divider(card, UITheme.WithAlpha(_theme.AccentRed, 0.5f));
            UIFactory.Layout(line.gameObject, minHeight: 2, preferredHeight: 2);
            _text = UIFactory.Text(card, "Text", "", _theme.BodyFont, _theme.SizeM + 2, UITheme.WithAlpha(_theme.TextLight, 0.92f), TextAlignmentOptions.Top);
            _text.lineSpacing = 10f;
            UIFactory.Layout(_text.gameObject, minHeight: 120);
            var sumBg = UIFactory.Round(UIFactory.Panel(card, "Resumen", UITheme.WithAlpha(_theme.TextLight, 0.04f)), _theme);
            UIFactory.Frame(sumBg.transform, _theme, UITheme.WithAlpha(_theme.TextLight, 0.1f));
            var sv = UIFactory.VLayout(sumBg.gameObject, 6, 0);
            sv.padding = new RectOffset(36, 36, 22, 24);
            UIFactory.Kicker(sumBg.transform, "Informe de la investigación", _theme, UITheme.WithAlpha(_theme.TextLight, 0.5f));
            _summary = UIFactory.Text(sumBg.transform, "Summary", "", _theme.BodyFont, _theme.SizeBody, _theme.TextLight);
            _summary.lineSpacing = 12f;
            UIFactory.Button(card, "Continuar", _theme, _theme.Paper, _theme.Ink, ShowClosing, 58);

            _page2 = UIFactory.Stretch(UIFactory.Rect("Page2", rt));
            _closing = UIFactory.Text(_page2, "Closing", "", _theme.TitleFont, _theme.SizeXL, _theme.TextLight, TextAlignmentOptions.Center);
            UIFactory.Stretch(_closing.rectTransform, 100, 200, 100, 200);
            var next = UIFactory.Ghost(_page2, "Continuar", _theme, _theme.TextLight, Finish, 56);
            UIFactory.Place((RectTransform)next.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 80), new Vector2(300, 56));
        }

        /// <summary>Muestra el final y llama a onFinished después de "Tres meses después...".</summary>
        public void Show(EndingResult result, Action onFinished, bool skipClosing = false)
        {
            _onFinished = onFinished;
            _skipClosing = skipClosing || result.Ending == null || string.IsNullOrEmpty(result.Ending.ClosingText);
            ShownEndingId = result.Ending != null ? result.Ending.Id : "";
            _title.text = result.Ending != null ? result.Ending.Title : "Caso sin resolver";
            _text.text = result.Text;
            _summary.text = BuildSummary(result);
            _closing.text = result.Ending != null ? result.Ending.ClosingText : "";
            _page1.gameObject.SetActive(true);
            _page2.gameObject.SetActive(false);
            Open();
        }

        string BuildSummary(EndingResult result)
        {
            var c = _game.Case;
            var record = _game.State.State.accusations.FirstOrDefault(a => a.caseId == c.Id);
            var accused = record != null ? c.FindSuspect(record.suspectId) : null;
            int found = c.Evidence.Count(e => _game.State.IsEvidenceFound(e.Id));
            int available = c.Evidence.Count(e => e.SpawnCondition.Evaluate(_game.State));
            int claims = c.AlexClaims.Count();
            var missedSolid = c.SolidProofs.Where(e => e.SpawnCondition.Evaluate(_game.State) && !_game.State.IsEvidenceAnalyzed(e.Id)).Select(e => e.DisplayName).ToList();
            string Row(string label, string value) =>
                $"<color={UITheme.Hex(UITheme.WithAlpha(_theme.TextLight, 0.55f))}>{label}</color><pos=48%><b>{value}</b>";
            var lines = Row("Acusaste a", accused != null ? accused.DisplayName : "—") + "\n" +
                        Row("Pruebas sólidas presentadas", result.SolidProofs.ToString()) + "\n" +
                        Row("Contradicciones de ALEX detectadas", $"{result.AlexContradictions} de {claims}") + "\n" +
                        Row("Evidencias encontradas", $"{found} de {available}");
            if (missedSolid.Count > 0)
                lines += "\n" + Row("Se te escapó", $"<color={UITheme.Hex(_theme.WarningAmber)}>{string.Join(", ", missedSolid)}</color>");
            return lines;
        }

        /// <summary>Muestra solo la pantalla de cierre (ej. los créditos del final secreto tras dec_c2_alex).</summary>
        public void ShowClosingOnly(EndingResult result, Action onFinished)
        {
            _onFinished = onFinished;
            _skipClosing = false;
            _closing.text = result.Ending != null ? result.Ending.ClosingText : "";
            if (string.IsNullOrEmpty(_closing.text)) { onFinished?.Invoke(); return; }
            Open();
            ShowClosing();
        }

        void ShowClosing()
        {
            if (_skipClosing) { Finish(); return; }
            _page1.gameObject.SetActive(false);
            _page2.gameObject.SetActive(true);
            if (_typing != null) StopCoroutine(_typing);
            _typing = StartCoroutine(Typewriter());
        }

        IEnumerator Typewriter()
        {
            _closing.ForceMeshUpdate();
            int total = _closing.textInfo.characterCount;
            _closing.maxVisibleCharacters = 0;
            for (float shown = 0; shown < total; shown += 18f * Time.unscaledDeltaTime)
            {
                _closing.maxVisibleCharacters = Mathf.CeilToInt(shown);
                yield return null;
            }
            _closing.maxVisibleCharacters = int.MaxValue;
        }

        /// <summary>Termina la pantalla de final (también lo usan las pruebas).</summary>
        public void Finish()
        {
            Close();
            var cb = _onFinished;
            _onFinished = null;
            cb?.Invoke();
        }
    }
}
