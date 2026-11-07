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
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Caso404.UI
{
    /// <summary>
    /// Diálogo en formato de cine: franjas negras arriba, degradado abajo y el texto sobre la escena (la cámara mira
    /// al rostro del personaje). Nombre, rol, emoción, texto con efecto de escritura, botones rápidos con atajos 1–6
    /// (crimen, otra persona, presentar evidencia, presionar, despedirse, hablar con calma) y campo para escribir.
    /// Muestra "X está pensando…" mientras responde la IA.
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
        TextMeshProUGUI _thinking;
        RectTransform _tagHolder;
        RectTransform _quick;
        RectTransform _extraRow;
        RectTransform _chooser;
        RectTransform _chooserList;
        TextMeshProUGUI _chooserTitle;
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

            // Franja negra superior (formato cine) y degradado inferior para leer el texto sobre la escena.
            var bar = UIFactory.Panel(rt, "FranjaCine", Color.black);
            UIFactory.Place(bar.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0, 64));
            bar.rectTransform.anchorMin = new Vector2(0f, 1f);
            bar.rectTransform.anchorMax = new Vector2(1f, 1f);
            var shade = UIFactory.Panel(rt, "Degradado", new Color(0.02f, 0.02f, 0.03f, 0.96f));
            if (_theme.GradientSprite != null) shade.sprite = _theme.GradientSprite;
            UIFactory.Region(shade.rectTransform, Vector2.zero, new Vector2(1f, 0.66f));
            shade.raycastTarget = true;

            var column = UIFactory.Rect("Columna", rt);
            UIFactory.Place(column, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 34), new Vector2(1560, 520));
            var v = UIFactory.VLayout(column.gameObject, 12);
            v.childAlignment = TextAnchor.LowerLeft;

            // Encabezado: nombre y rol a la izquierda, emoción a la derecha.
            var header = UIFactory.Rect("Encabezado", column);
            var h = UIFactory.HLayout(header.gameObject, 16, 0, false);
            h.childAlignment = TextAnchor.LowerLeft;
            UIFactory.Layout(header.gameObject, preferredHeight: 74);
            var names = UIFactory.Rect("Nombres", header);
            UIFactory.VLayout(names.gameObject, 0);
            UIFactory.Layout(names.gameObject, flexibleWidth: 1, preferredHeight: 74);
            _name = UIFactory.Text(names, "Nombre", "", _theme.TitleFont, 42, _theme.WarningAmber);
            UIFactory.Layout(_name.gameObject, preferredHeight: 46);
            _role = UIFactory.Text(names, "Rol", "", _theme.BodyFont, _theme.SizeBody, UITheme.WithAlpha(_theme.TextLight, 0.6f));
            _role.fontStyle = FontStyles.Italic;
            UIFactory.Layout(_role.gameObject, preferredHeight: 26);
            _tagHolder = UIFactory.Rect("Emocion", header);
            var th = UIFactory.HLayout(_tagHolder.gameObject, 0, 0, false);
            th.childAlignment = TextAnchor.LowerRight;
            th.childControlWidth = false;
            UIFactory.Layout(_tagHolder.gameObject, preferredWidth: 420, preferredHeight: 40);

            UIFactory.Divider(column, UITheme.WithAlpha(_theme.WarningAmber, 0.35f));

            _line = UIFactory.Text(column, "Linea", "", _theme.BodyFont, 27, _theme.TextLight);
            _line.lineSpacing = 8f;
            _line.enableAutoSizing = true;
            _line.fontSizeMin = 20f;
            _line.fontSizeMax = 27f;
            // Sombra para que el texto se lea aunque pase sobre la ropa clara del personaje.
            var lineShadow = _line.gameObject.AddComponent<Shadow>();
            lineShadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            lineShadow.effectDistance = new Vector2(2f, -2f);
            UIFactory.Layout(_line.gameObject, minHeight: 112, preferredHeight: 112);
            _thinking = UIFactory.Text(column, "Pensando", "", _theme.MonoFont, _theme.SizeS + 1, UITheme.WithAlpha(_theme.WarningAmber, 0.85f));
            UIFactory.Layout(_thinking.gameObject, preferredHeight: 22);

            _quick = UIFactory.Rect("Rapidas", column);
            UIFactory.HLayout(_quick.gameObject, 10);
            UIFactory.Layout(_quick.gameObject, preferredHeight: 48);
            _quickButtons = new[]
            {
                QuickButton(1, "Sobre el crimen", _theme.Paper, () => Ask(DialogueTopic.Crime, null)),
                QuickButton(2, "Sobre otra persona", _theme.Paper, () => Ask(DialogueTopic.OtherPerson, null)),
                QuickButton(3, "Presentar evidencia", _theme.WarningAmber, OpenChooser),
                QuickButton(4, "Presionar", _theme.AccentRed, () => Ask(DialogueTopic.Press, null)),
                QuickButton(5, "Despedirse", _theme.Muted, Close),
            };
            _calm = QuickButton(6, "Hablarle con calma", _theme.AlexCyan, () => Ask(DialogueTopic.Calm, null));

            _extraRow = UIFactory.Rect("TemasExtra", column);
            UIFactory.HLayout(_extraRow.gameObject, 10, 0, false);
            UIFactory.Layout(_extraRow.gameObject, preferredHeight: 44);

            var inputRow = UIFactory.Rect("Escribir", column);
            UIFactory.HLayout(inputRow.gameObject, 10, 0, false);
            UIFactory.Layout(inputRow.gameObject, preferredHeight: 52);
            _input = UIFactory.Input(inputRow, _theme, "Escribe tu pregunta y pulsa Enter (máx. 200 caracteres)", 200);
            UIFactory.Layout(_input.gameObject, minHeight: 52, preferredHeight: 52, flexibleWidth: 1);
            _input.onSubmit.AddListener(_ => AskTyped());
            _ask = UIFactory.Button(inputRow, "Preguntar", _theme, _theme.Paper, _theme.Ink, AskTyped, 52);
            UIFactory.Layout(_ask.gameObject, minHeight: 52, preferredHeight: 52, preferredWidth: 200);

            BuildChooser(rt);
        }

        /// <summary>Botón rápido con su número de atajo a la izquierda.</summary>
        Button QuickButton(int key, string label, Color accent, Action action)
        {
            var text = $"<color={UITheme.Hex(UITheme.WithAlpha(accent, 0.75f))}>{key}</color>   {label}";
            var b = UIFactory.Ghost(_quick, text, _theme, accent, action, 48, _theme.SizeS + 2);
            b.gameObject.name = $"Btn_{label}";
            return b;
        }

        /// <summary>Selector de evidencias y testimonios: tarjeta centrada sobre la escena.</summary>
        void BuildChooser(RectTransform rt)
        {
            var dim = UIFactory.Backdrop(rt, _theme, 0.7f);
            dim.gameObject.name = "Selector";
            _chooser = dim.rectTransform;
            var card = UIFactory.Card(_chooser, "Tarjeta", UITheme.WithAlpha(_theme.BgDark, 0.98f), _theme);
            UIFactory.Place(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(1000, 720));
            UIFactory.Frame(card.transform, _theme, UITheme.WithAlpha(_theme.WarningAmber, 0.4f));
            var inner = UIFactory.Rect("Contenido", card.transform);
            UIFactory.Stretch(inner, 36, 28, 36, 30);
            UIFactory.VLayout(inner.gameObject, 12);
            UIFactory.Kicker(inner, "Presentar", _theme, _theme.WarningAmber);
            _chooserTitle = UIFactory.Text(inner, "Titulo", "", _theme.TitleFont, _theme.SizeL, _theme.TextLight);
            UIFactory.Layout(_chooserTitle.gameObject, preferredHeight: 44);
            UIFactory.Divider(inner, UITheme.WithAlpha(_theme.TextLight, 0.12f));
            var listHolder = UIFactory.Rect("Lista", inner);
            UIFactory.Layout(listHolder.gameObject, flexibleHeight: 1);
            _chooserList = UIFactory.ScrollList(listHolder, "Items", 8, 2);
            UIFactory.Stretch((RectTransform)_chooserList.parent.parent);
            var back = UIFactory.Ghost(inner, "Volver", _theme, _theme.Muted, CloseChooser, 48);
            UIFactory.Layout(back.gameObject, minHeight: 48, preferredHeight: 48);
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
            SetTag("", _theme.Muted);
            _input.text = "";
            _chooser.gameObject.SetActive(false);
            var greeting = _game.Dialogue.Greeting(character);
            SetLine(string.IsNullOrEmpty(greeting)
                ? $"<color={UITheme.Hex(UITheme.WithAlpha(_theme.TextLight, 0.55f))}><i>¿Qué le preguntas a {character.DisplayName}?</i></color>"
                : $"“{greeting}”", instant: string.IsNullOrEmpty(greeting));
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

        void Update()
        {
            if (!IsOpen) return;
            if (_busy && _current != null)
                _thinking.text = $"{_current.DisplayName} está pensando" + new string('.', 1 + (int)(Time.unscaledTime * 3f) % 3);

            var kb = Keyboard.current;
            // Clic o Espacio completan la frase que se está escribiendo (para quien lee rápido).
            if (_typing != null && !UIFactory.IsTyping())
            {
                var mouse = Mouse.current;
                if ((kb != null && kb.spaceKey.wasPressedThisFrame) || (mouse != null && mouse.leftButton.wasPressedThisFrame))
                    CompleteLine();
            }
            if (kb == null || _busy || _chooser.gameObject.activeSelf || UIFactory.IsTyping()) return;
            for (int i = 0; i < 6; i++)
            {
                if (!kb[Key.Digit1 + i].wasPressedThisFrame && !kb[Key.Numpad1 + i].wasPressedThisFrame) continue;
                var b = i < _quickButtons.Length ? _quickButtons[i] : _calm;
                if (b.gameObject.activeInHierarchy && b.interactable) b.onClick.Invoke();
                return;
            }
            if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame) _input.ActivateInputField();
        }

        async void Ask(DialogueTopic topic, string question)
        {
            if (_busy || _current == null) return;
            _busy = true;
            RefreshButtons();
            CancelPending();
            _cts = new CancellationTokenSource();
            try
            {
                var line = await _game.Dialogue.AskAsync(_current, topic, question, _cts.Token);
                if (this != null && IsOpen) ShowLine(line);
            }
            catch (OperationCanceledException) { }
            finally
            {
                _busy = false;
                if (this != null) RefreshButtons();
            }
        }

        void AskTyped()
        {
            var q = _input.text?.Trim();
            if (string.IsNullOrEmpty(q) || _busy) return;
            _input.text = "";
            SetLine($"<color={UITheme.Hex(UITheme.WithAlpha(_theme.TextLight, 0.55f))}><i>Tú: {q}</i></color>", instant: true);
            Ask(DialogueTopic.FreeText, q);
        }

        void OpenChooser()
        {
            if (_current == null) return;
            _chooserTitle.text = $"¿Qué le presentas a {_current.DisplayName}?";
            UIFactory.Clear(_chooserList);
            var items = _game.Dialogue.GetPresentableItems().ToList();
            if (items.Count == 0)
                UIFactory.Text(_chooserList, "Vacio", "No tienes evidencias analizadas ni testimonios todavía.", _theme.BodyFont, _theme.SizeBody, _theme.Muted);
            foreach (var item in items)
            {
                var captured = item;
                bool evidence = item.Evidence != null;
                string kind = evidence ? "EVIDENCIA ANALIZADA" : "TESTIMONIO";
                var color = evidence ? _theme.WarningAmber : _theme.AlexCyan;
                var label = $"{item.Label}\n<size=70%><color={UITheme.Hex(UITheme.WithAlpha(color, 0.85f))}>{kind}</color></size>";
                var b = UIFactory.Button(_chooserList, label, _theme, UITheme.WithAlpha(_theme.TextLight, 0.06f), _theme.TextLight, () => Present(captured), 66, _theme.SizeBody);
                var t = b.GetComponentInChildren<TextMeshProUGUI>();
                t.alignment = TextAlignmentOptions.MidlineLeft;
                t.rectTransform.offsetMin = new Vector2(24, 4);
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
            if (line.ExposedLie) SetTag("¡MENTIRA DESCUBIERTA!", _theme.AccentRed);
            else SetTag(string.IsNullOrEmpty(line.Emotion) ? "" : line.Emotion.ToUpperInvariant(), _theme.Muted);
            SetLine($"“{line.Text}”", instant: false);
        }

        /// <summary>Etiqueta de emoción (o de mentira descubierta) a la derecha del nombre.</summary>
        void SetTag(string text, Color color)
        {
            UIFactory.Clear(_tagHolder);
            if (!string.IsNullOrEmpty(text)) UIFactory.Chip(_tagHolder, text, _theme, color);
        }

        void SetLine(string text, bool instant)
        {
            if (_typing != null) StopCoroutine(_typing);
            _line.text = text;
            _line.maxVisibleCharacters = instant ? int.MaxValue : 0;
            if (!instant && isActiveAndEnabled) _typing = StartCoroutine(Typewriter());
        }

        void CompleteLine()
        {
            if (_typing != null) StopCoroutine(_typing);
            _typing = null;
            _line.maxVisibleCharacters = int.MaxValue;
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
            _input.interactable = !_busy;
            bool calm = _current != null && _game.Dialogue.CanUseCalmApproach(_current);
            _calm.gameObject.SetActive(calm);
            _calm.interactable = !_busy;
            RebuildExtraTopics();
            _thinking.text = "";
        }

        void RebuildExtraTopics()
        {
            UIFactory.Clear(_extraRow);
            _extraRow.gameObject.SetActive(false);
            if (_current == null) return;
            var c = _current;
            int count = 0;
            foreach (var topic in _game.Dialogue.AvailableExtraTopics(c))
            {
                var t = topic;
                var b = UIFactory.Ghost(_extraRow, $"▲  {t.Label}", _theme, _theme.WarningAmber, () => ShowLine(_game.Dialogue.AskExtra(c, t)), 44, _theme.SizeS + 1);
                UIFactory.Layout(b.gameObject, minHeight: 44, preferredHeight: 44, preferredWidth: 420);
                b.interactable = !_busy;
                count++;
            }
            _extraRow.gameObject.SetActive(count > 0);
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
