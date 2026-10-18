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
            var card = UIFactory.Rect("Card", _page1);
            UIFactory.Place(card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1300, 860));
            UIFactory.VLayout(card.gameObject, 22);
            var label = UIFactory.Text(card, "Label", "FINAL", _theme.MonoFont, _theme.SizeS, _theme.AccentRed, TextAlignmentOptions.Center);
            UIFactory.Layout(label.gameObject, preferredHeight: 24);
            _title = UIFactory.Text(card, "Title", "", _theme.TitleFont, _theme.SizeXL, _theme.TextLight, TextAlignmentOptions.Center);
            UIFactory.Layout(_title.gameObject, preferredHeight: 70);
            _text = UIFactory.Text(card, "Text", "", _theme.BodyFont, _theme.SizeM, _theme.TextLight, TextAlignmentOptions.Top);
            UIFactory.Layout(_text.gameObject, flexibleHeight: 1, minHeight: 260);
            _summary = UIFactory.Text(card, "Summary", "", _theme.BodyFont, _theme.SizeBody, _theme.Muted);
            UIFactory.Layout(_summary.gameObject, preferredHeight: 180);
            UIFactory.Button(card, "Continuar", _theme, _theme.Paper, _theme.Ink, ShowClosing, 56);

            _page2 = UIFactory.Stretch(UIFactory.Rect("Page2", rt));
            _closing = UIFactory.Text(_page2, "Closing", "", _theme.TitleFont, _theme.SizeXL, _theme.TextLight, TextAlignmentOptions.Center);
            UIFactory.Stretch(_closing.rectTransform, 100, 200, 100, 200);
            var next = UIFactory.Button(_page2, "Continuar", _theme, _theme.Paper, _theme.Ink, Finish, 56);
            UIFactory.Place((RectTransform)next.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 80), new Vector2(300, 56));
        }

        /// <summary>Muestra el final y llama a onFinished después de "Tres meses después...".</summary>
        public void Show(EndingResult result, Action onFinished)
        {
            _onFinished = onFinished;
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
            var lines = $"Acusaste a: {(accused != null ? accused.DisplayName : "—")}\n" +
                        $"Pruebas sólidas presentadas: {result.SolidProofs}\n" +
                        $"Contradicciones de ALEX detectadas: {result.AlexContradictions} de {claims}\n" +
                        $"Evidencias encontradas: {found} de {available}";
            if (missedSolid.Count > 0) lines += $"\nSe te escapó: {string.Join(", ", missedSolid)}";
            return lines;
        }

        void ShowClosing()
        {
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
