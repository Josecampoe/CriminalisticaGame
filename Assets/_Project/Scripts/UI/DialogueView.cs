using System;
using System.Collections;
using System.Linq;
using System.Threading;
using Caso404.AI;
using Caso404.Core;
using Caso404.Data;
using Caso404.Dialogue;
using Caso404.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Caso404.UI
{
    /// <summary>
    /// Diálogo: nombre del personaje, texto con efecto de escritura, botones rápidos (crimen, otra persona,
    /// presentar evidencia, presionar, hablar con calma) y campo para escribir. Muestra "pensando…" mientras responde.
    /// </summary>
    public class DialogueView : UIPanel
    {
        /// <summary>Se cerró la conversación (el controlador abre decisiones pendientes, como la del cuaderno).</summary>
        public event Action Closed;

        [SerializeField] private GameContext _game;
        [SerializeField] private PlayerContext _player;
        [SerializeField] private float _charsPerSecond = 60f;

        TextMeshProUGUI _name;
        TextMeshProUGUI _role;
        TextMeshProUGUI _line;
        TextMeshProUGUI _tag;
        TextMeshProUGUI _thinking;
        RectTransform _quick;
        RectTransform _chooser;
        RectTransform _chooserList;
        TMP_InputField _input;
        Button _ask, _calm;
        Button[] _quickButtons;
        CharacterProfile _current;
        CancellationTokenSource _cts;
        Coroutine _typing;
        bool _busy;

        protected override void Build()
        {
            var rt = UIFactory.Stretch((RectTransform)transform);
            var panel = UIFactory.Panel(rt, "Panel", UITheme.WithAlpha(_theme.BgDark, 0.95f));
            UIFactory.Region(panel.rectTransform, new Vector2(0.08f, 0.03f), new Vector2(0.92f, 0.42f));
            UIFactory.VLayout(panel.gameObject, 10, 28);

            var header = UIFactory.Rect("Header", panel.transform);
            UIFactory.HLayout(header.gameObject, 16, 0, false);
            UIFactory.Layout(header.gameObject, preferredHeight: 40);
            _name = UIFactory.Text(header, "Name", "", _theme.TitleFont, _theme.SizeL, _theme.WarningAmber);
            UIFactory.Layout(_name.gameObject, preferredWidth: 520);
            _role = UIFactory.Text(header, "Role", "", _theme.BodyFont, _theme.SizeS, _theme.Muted, TextAlignmentOptions.BottomLeft);
            UIFactory.Layout(_role.gameObject, flexibleWidth: 1);
            _tag = UIFactory.Text(header, "Tag", "", _theme.BodyFont, _theme.SizeBody, _theme.AccentRed, TextAlignmentOptions.Right);
            UIFactory.Layout(_tag.gameObject, preferredWidth: 360);

            _line = UIFactory.Text(panel.transform, "Line", "", _theme.BodyFont, 22, _theme.TextLight);
            UIFactory.Layout(_line.gameObject, flexibleHeight: 1, minHeight: 90);
            _thinking = UIFactory.Text(panel.transform, "Thinking", "pensando…", _theme.BodyFont, _theme.SizeS, _theme.Muted);
            UIFactory.Layout(_thinking.gameObject, preferredHeight: 20);

            _quick = UIFactory.Rect("Quick", panel.transform);
            UIFactory.HLayout(_quick.gameObject, 10);
            UIFactory.Layout(_quick.gameObject, preferredHeight: 48);
            _quickButtons = new[]
            {
                UIFactory.Button(_quick, "Sobre el crimen", _theme, _theme.Paper, _theme.Ink, () => Ask(DialogueTopic.Crime, null), 46, _theme.SizeS),
                UIFactory.Button(_quick, "Sobre otra persona", _theme, _theme.Paper, _theme.Ink, () => Ask(DialogueTopic.OtherPerson, null), 46, _theme.SizeS),
                UIFactory.Button(_quick, "Presentar evidencia", _theme, _theme.WarningAmber, _theme.Ink, OpenChooser, 46, _theme.SizeS),
                UIFactory.Button(_quick, "Presionar", _theme, _theme.AccentRed, _theme.TextLight, () => Ask(DialogueTopic.Press, null), 46, _theme.SizeS),
                UIFactory.Button(_quick, "Despedirse", _theme, _theme.Muted, _theme.TextLight, Close, 46, _theme.SizeS),
            };
            _calm = UIFactory.Button(_quick, "Hablarle con calma", _theme, _theme.AlexCyan, _theme.Ink, () => Ask(DialogueTopic.Calm, null), 46, _theme.SizeS);

            var inputRow = UIFactory.Rect("InputRow", panel.transform);
            UIFactory.HLayout(inputRow.gameObject, 10, 0, false);
            UIFactory.Layout(inputRow.gameObject, preferredHeight: 48);
            _input = UIFactory.Input(inputRow, _theme, "Escribe tu pregunta (máx. 200 caracteres)", 200);
            _ask = UIFactory.Button(inputRow, "Preguntar", _theme, _theme.Paper, _theme.Ink, AskTyped, 48);
            UIFactory.Layout(_ask.gameObject, minHeight: 48, preferredHeight: 48, preferredWidth: 180);

            // Selector de evidencias / testimonios (encima del diálogo).
            var chooserBg = UIFactory.Panel(rt, "Chooser", UITheme.WithAlpha(_theme.BgDark, 0.97f));
            _chooser = chooserBg.rectTransform;
            UIFactory.Region(_chooser, new Vector2(0.2f, 0.45f), new Vector2(0.8f, 0.92f));
            var chooserTitle = UIFactory.Text(_chooser, "Title", "¿Qué le presentas?", _theme.TitleFont, _theme.SizeM, _theme.TextLight);
            UIFactory.Place(chooserTitle.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24, -16), new Vector2(800, 40));
            var listHolder = UIFactory.Rect("ListHolder", _chooser);
            UIFactory.Stretch(listHolder, 16, 76, 16, 64);
            _chooserList = UIFactory.ScrollList(listHolder, "List", 8, 4);
            UIFactory.Stretch((RectTransform)_chooserList.parent.parent);
            var back = UIFactory.Button(_chooser, "Volver", _theme, _theme.Muted, _theme.TextLight, CloseChooser);
            UIFactory.Place((RectTransform)back.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 14), new Vector2(240, 48));
            _chooser.gameObject.SetActive(false);
        }

        void OnEnable()
        {
            if (_player != null) _player.TalkRequested += OnTalkRequested;
            if (_game != null && _game.State != null) _game.State.Changed += RefreshButtons;
        }

        void OnDisable()
        {
            if (_player != null) _player.TalkRequested -= OnTalkRequested;
            if (_game != null && _game.State != null) _game.State.Changed -= RefreshButtons;
            CancelPending();
        }

        void OnTalkRequested(CharacterProfile character, Transform head)
        {
            _current = character;
            _game.Dialogue.Begin(character);
            _name.text = character.DisplayName;
            _role.text = character.Role;
            _tag.text = "";
            _input.text = "";
            _chooser.gameObject.SetActive(false);
            SetLine($"<color=#{ColorUtility.ToHtmlStringRGB(_theme.Muted)}>¿Qué le preguntas a {character.DisplayName}?</color>", instant: true);
            if (_player.Controller != null) _player.Controller.FocusOn(head);
            Open();
            RefreshButtons();
        }

        protected override void OnHidden()
        {
            CancelPending();
            _game.Dialogue.End();
            _current = null;
            Closed?.Invoke();
        }

        async void Ask(DialogueTopic topic, string question)
        {
            if (_busy || _current == null) return;
            _busy = true;
            _thinking.gameObject.SetActive(true);
            RefreshButtons();
            CancelPending();
            _cts = new CancellationTokenSource();
            try
            {
                var line = await _game.Dialogue.AskAsync(_current, topic, question, _cts.Token);
                ShowLine(line);
            }
            catch (OperationCanceledException) { }
            finally
            {
                _busy = false;
                if (_thinking != null) _thinking.gameObject.SetActive(false);
                RefreshButtons();
            }
        }

        void AskTyped()
        {
            var q = _input.text?.Trim();
            if (string.IsNullOrEmpty(q)) return;
            _input.text = "";
            Ask(DialogueTopic.FreeText, q);
        }

        void OpenChooser()
        {
            UIFactory.Clear(_chooserList);
            var items = _game.Dialogue.GetPresentableItems().ToList();
            if (items.Count == 0)
                UIFactory.Text(_chooserList, "Empty", "No tienes evidencias analizadas ni testimonios todavía.", _theme.BodyFont, _theme.SizeBody, _theme.TextLight);
            foreach (var item in items)
            {
                var captured = item;
                var color = item.Evidence != null ? _theme.Paper : UITheme.WithAlpha(_theme.Paper, 0.75f);
                var b = UIFactory.Button(_chooserList, item.Label, _theme, color, _theme.Ink, () => Present(captured), 56, _theme.SizeS);
                b.GetComponentInChildren<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineLeft;
            }
            _chooser.gameObject.SetActive(true);
        }

        void CloseChooser() => _chooser.gameObject.SetActive(false);

        void Present(PresentableItem item)
        {
            CloseChooser();
            if (_current == null) return;
            ShowLine(_game.Dialogue.Present(_current, item));
        }

        void ShowLine(DialogueLine line)
        {
            _tag.text = line.ExposedLie ? "¡Mentira descubierta!" : $"[{line.Emotion}]";
            _tag.color = line.ExposedLie ? _theme.AccentRed : _theme.Muted;
            SetLine(line.Text, instant: false);
        }

        void SetLine(string text, bool instant)
        {
            if (_typing != null) StopCoroutine(_typing);
            _line.text = text;
            _line.maxVisibleCharacters = instant ? int.MaxValue : 0;
            if (!instant && isActiveAndEnabled) _typing = StartCoroutine(Typewriter());
        }

        IEnumerator Typewriter()
        {
            _line.ForceMeshUpdate();
            int total = _line.textInfo.characterCount;
            float shown = 0f;
            while (shown < total)
            {
                shown += _charsPerSecond * Time.unscaledDeltaTime;
                _line.maxVisibleCharacters = Mathf.Min(total, Mathf.CeilToInt(shown));
                yield return null;
            }
            _line.maxVisibleCharacters = int.MaxValue;
            _typing = null;
        }

        void RefreshButtons()
        {
            if (_quickButtons == null) return;
            foreach (var b in _quickButtons) b.interactable = !_busy;
            _ask.interactable = !_busy;
            bool calm = _current != null && _game.Dialogue.CanUseCalmApproach(_current);
            _calm.gameObject.SetActive(calm);
            _calm.interactable = !_busy;
            if (!_busy) _thinking.gameObject.SetActive(false);
        }

        void CancelPending()
        {
            if (_cts == null) return;
            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }
    }
}
