using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Caso404.AI;
using Caso404.Core;
using Caso404.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Caso404.UI
{
    /// <summary>
    /// Panel de ALEX (estilo terminal cian, entra deslizándose desde la derecha). Arriba, la conversación con ALEX
    /// como un historial de chat; en medio, preguntas rápidas y un campo para escribirle; abajo, sus afirmaciones
    /// como tarjetas con "Aceptar" o "Cuestionar". Cuestionar pide una evidencia analizada; la lógica la decide AlexService.
    /// Con confianza baja, o al descubrir una mentira, el panel hace glitch.
    /// </summary>
    public class AlexPanelView : UIPanel
    {
        const float PanelWidth = 660f;
        const int MaxLogEntries = 14;

        [SerializeField] private GameContext _game;

        RectTransform _panel;
        TextMeshProUGUI _status;
        RectTransform _toneHolder;
        TextMeshProUGUI _glitchLabel;
        TextMeshProUGUI _thinking;
        TextMeshProUGUI _claimsTitle;
        RectTransform _log;
        ScrollRect _logScroll;
        RectTransform _claims;
        RectTransform _chooser;
        RectTransform _chooserList;
        TextMeshProUGUI _chooserTitle;
        TMP_InputField _input;
        Button _askButton;
        Button[] _quick;
        TextMeshProUGUI _lastAlexText;
        LieData _questioning;
        bool _greeted;
        float _nextAmbientGlitch;
        Coroutine _glitch;
        Coroutine _slide;
        CancellationTokenSource _cts;
        bool _busy;
        readonly List<(bool alex, string text)> _entries = new();

        protected override void Build()
        {
            var rt = UIFactory.Stretch((RectTransform)transform);
            var bg = UIFactory.AsScreen(UIFactory.Panel(rt, "Panel", UITheme.WithAlpha(_theme.BgDark, 0.97f)), _theme, _theme.AlexCyan);
            _panel = bg.rectTransform;
            _panel.anchorMin = new Vector2(1f, 0f);
            _panel.anchorMax = new Vector2(1f, 1f);
            _panel.pivot = new Vector2(1f, 0.5f);
            _panel.sizeDelta = new Vector2(PanelWidth, 0);
            _panel.anchoredPosition = Vector2.zero;
            var border = UIFactory.Panel(_panel, "Borde", _theme.AlexCyan);
            border.rectTransform.anchorMin = new Vector2(0, 0);
            border.rectTransform.anchorMax = new Vector2(0, 1);
            border.rectTransform.sizeDelta = new Vector2(3, 0);

            var content = UIFactory.Rect("Contenido", _panel);
            UIFactory.Stretch(content, 32, 24, 28, 26);
            UIFactory.VLayout(content.gameObject, 12);

            BuildHeader(content);
            UIFactory.Divider(content, UITheme.WithAlpha(_theme.AlexCyan, 0.25f));

            // Historial de la conversación con ALEX.
            var logBox = UIFactory.Round(UIFactory.Panel(content, "Conversacion", UITheme.WithAlpha(_theme.AlexCyan, 0.05f)), _theme);
            UIFactory.Layout(logBox.gameObject, flexibleHeight: 1.3f, minHeight: 220);
            UIFactory.Frame(logBox.transform, _theme, UITheme.WithAlpha(_theme.AlexCyan, 0.18f));
            var logHolder = UIFactory.Rect("Lista", logBox.transform);
            UIFactory.Stretch(logHolder, 6, 6, 6, 6);
            _log = UIFactory.ScrollList(logHolder, "Mensajes", 12, 12);
            _logScroll = _log.GetComponentInParent<ScrollRect>();
            UIFactory.Stretch((RectTransform)_log.parent.parent);

            _thinking = UIFactory.Text(content, "Procesando", "", _theme.MonoFont, _theme.SizeS, _theme.AlexCyan);
            UIFactory.Layout(_thinking.gameObject, preferredHeight: 18);

            var ask = UIFactory.Rect("Preguntas", content);
            UIFactory.VLayout(ask.gameObject, 6);
            _quick = new Button[3];
            for (int i = 0; i < 3; i++)
            {
                int index = i;
                _quick[i] = UIFactory.Ghost(ask, "", _theme, _theme.AlexCyan, () => AskQuick(index), 40, _theme.SizeS);
                var label = _quick[i].GetComponentInChildren<TextMeshProUGUI>();
                label.alignment = TextAlignmentOptions.MidlineLeft;
                label.font = _theme.MonoFont;
                label.fontStyle = FontStyles.Normal;
            }

            var inputRow = UIFactory.Rect("Escribir", content);
            UIFactory.HLayout(inputRow.gameObject, 8, 0, false);
            UIFactory.Layout(inputRow.gameObject, minHeight: 48, preferredHeight: 48);
            _input = UIFactory.Input(inputRow, _theme, "Pregúntale algo a ALEX…", AlexService.MaxQuestionLength, _theme.AlexCyan);
            if (_input.textComponent is TextMeshProUGUI it) it.font = _theme.MonoFont;
            _input.onSubmit.AddListener(_ => AskTyped());
            _askButton = UIFactory.Button(inputRow, "Enviar", _theme, _theme.AlexCyan, _theme.Ink, AskTyped, 48, _theme.SizeS + 1);
            UIFactory.Layout(_askButton.gameObject, minHeight: 48, preferredHeight: 48, preferredWidth: 120);

            _claimsTitle = UIFactory.Kicker(content, "Observaciones de ALEX", _theme, UITheme.WithAlpha(_theme.TextLight, 0.55f));
            var claimsHolder = UIFactory.Rect("Observaciones", content);
            UIFactory.Layout(claimsHolder.gameObject, flexibleHeight: 1, minHeight: 180);
            _claims = UIFactory.ScrollList(claimsHolder, "Tarjetas", 10, 0);
            UIFactory.Stretch((RectTransform)_claims.parent.parent);

            var close = UIFactory.Ghost(content, "Cerrar   Q / Esc", _theme, _theme.Muted, Close, 44, _theme.SizeS);
            UIFactory.Layout(close.gameObject, minHeight: 44, preferredHeight: 44);

            _glitchLabel = UIFactory.Text(_panel, "404", "404", _theme.MonoFont, 160, UITheme.WithAlpha(_theme.AccentRed, 0.85f), TextAlignmentOptions.Center);
            UIFactory.Stretch(_glitchLabel.rectTransform);
            _glitchLabel.gameObject.SetActive(false);

            BuildChooser(rt);
        }

        void BuildHeader(RectTransform content)
        {
            var header = UIFactory.Rect("Encabezado", content);
            var h = UIFactory.HLayout(header.gameObject, 14, 0, false);
            h.childAlignment = TextAnchor.MiddleLeft;
            UIFactory.Layout(header.gameObject, preferredHeight: 58);
            var dot = UIFactory.Dot(header, "Luz", _theme.AlexCyan, _theme, 14);
            dot.gameObject.AddComponent<PulseUI>();
            var titles = UIFactory.Rect("Titulos", header);
            UIFactory.VLayout(titles.gameObject, 0);
            UIFactory.Layout(titles.gameObject, flexibleWidth: 1, preferredHeight: 58);
            var title = UIFactory.Text(titles, "Titulo", "ALEX", _theme.MonoFont, 32, _theme.AlexCyan);
            title.fontStyle = FontStyles.Bold;
            title.characterSpacing = 12f;
            UIFactory.Layout(title.gameObject, preferredHeight: 36);
            var sub = UIFactory.Text(titles, "Subtitulo", "Asistente Lógico de EXpedientes · Nexo Analytics", _theme.MonoFont, _theme.SizeS - 1, UITheme.WithAlpha(_theme.TextLight, 0.5f));
            UIFactory.Layout(sub.gameObject, preferredHeight: 20);
            _toneHolder = UIFactory.Rect("Tono", header);
            var th = UIFactory.HLayout(_toneHolder.gameObject, 0, 0, false);
            th.childAlignment = TextAnchor.MiddleRight;
            th.childControlWidth = false;
            UIFactory.Layout(_toneHolder.gameObject, preferredWidth: 200, preferredHeight: 34);

            _status = UIFactory.Text(content, "Estado", "", _theme.MonoFont, _theme.SizeS, UITheme.WithAlpha(_theme.TextLight, 0.55f));
            UIFactory.Layout(_status.gameObject, preferredHeight: 20);
        }

        /// <summary>Selector de evidencia para Cuestionar, a la izquierda del panel.</summary>
        void BuildChooser(RectTransform rt)
        {
            var dim = UIFactory.Backdrop(rt, _theme, 0.55f);
            dim.gameObject.name = "Selector";
            _chooser = dim.rectTransform;
            _chooser.offsetMax = new Vector2(-PanelWidth, 0);
            var card = UIFactory.Card(_chooser, "Tarjeta", UITheme.WithAlpha(_theme.BgDark, 0.98f), _theme);
            UIFactory.Place(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900, 720));
            UIFactory.Frame(card.transform, _theme, UITheme.WithAlpha(_theme.AccentRed, 0.45f));
            var inner = UIFactory.Rect("Contenido", card.transform);
            UIFactory.Stretch(inner, 36, 28, 36, 30);
            UIFactory.VLayout(inner.gameObject, 12);
            UIFactory.Kicker(inner, "Cuestionar a ALEX", _theme, _theme.AccentRed);
            _chooserTitle = UIFactory.Text(inner, "Titulo", "", _theme.BodyFont, _theme.SizeM, _theme.TextLight);
            UIFactory.Layout(_chooserTitle.gameObject, preferredHeight: 110);
            UIFactory.Divider(inner, UITheme.WithAlpha(_theme.TextLight, 0.12f));
            var holder = UIFactory.Rect("Lista", inner);
            UIFactory.Layout(holder.gameObject, flexibleHeight: 1);
            _chooserList = UIFactory.ScrollList(holder, "Items", 8, 2);
            UIFactory.Stretch((RectTransform)_chooserList.parent.parent);
            var back = UIFactory.Ghost(inner, "Volver", _theme, _theme.Muted, CloseChooser, 48);
            UIFactory.Layout(back.gameObject, minHeight: 48, preferredHeight: 48);
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
            RefreshQuickQuestions();
            Refresh();
            if (_slide != null) StopCoroutine(_slide);
            _slide = StartCoroutine(SlideIn());
        }

        protected override void OnHidden()
        {
            CloseChooser();
            CancelPending();
        }

        void OnDestroy() => CancelPending();

        IEnumerator SlideIn()
        {
            for (float t = 0; t < 0.22f; t += Time.unscaledDeltaTime)
            {
                float k = 1f - Mathf.Pow(1f - t / 0.22f, 3f);
                _panel.anchoredPosition = new Vector2(Mathf.Lerp(PanelWidth * 0.4f, 0f, k), 0f);
                yield return null;
            }
            _panel.anchoredPosition = Vector2.zero;
            _slide = null;
        }

        /// <summary>Textos de las preguntas rápidas: los del caso (GUION) o unos genéricos si el caso no los trae.</summary>
        void RefreshQuickQuestions()
        {
            var q = _game.Case.AlexQuestions;
            string[] defaults = { "¿Quién crees que fue?", "¿Qué opinas de esta evidencia?", "¿Por qué fallaron tus registros?" };
            for (int i = 0; i < _quick.Length; i++)
            {
                string text = q != null && i < q.Length && !string.IsNullOrEmpty(q[i]) ? q[i] : defaults[i];
                UIFactory.SetLabel(_quick[i], $"<color={UITheme.Hex(UITheme.WithAlpha(_theme.AlexCyan, 0.6f))}>?</color>  {text}");
            }
        }

        void AskQuick(int index)
        {
            if (_busy) return;
            var label = _quick[index].GetComponentInChildren<TextMeshProUGUI>().GetParsedText();
            int cut = label.IndexOf("  ");
            AddEntry(false, cut >= 0 ? label.Substring(cut + 2) : label);
            var lines = _game.Case.AlexLines;
            Say(index == 0 ? lines.Crime.Text : index == 1 ? lines.Evidence.Text : lines.Press.Text);
        }

        /// <summary>Envía la pregunta escrita a ALEX. Mientras responde, el campo queda bloqueado y se ve "procesando…".</summary>
        async void AskTyped()
        {
            var q = _input.text?.Trim();
            if (_busy || string.IsNullOrEmpty(q)) return;
            _input.text = "";
            AddEntry(false, q);
            SetBusy(true);
            CancelPending();
            _cts = new CancellationTokenSource();
            try
            {
                var answer = await _game.Alex.AskAsync(q, _cts.Token);
                if (this != null && IsOpen && !string.IsNullOrEmpty(answer)) Say(answer);
            }
            catch (System.OperationCanceledException) { }
            finally
            {
                if (this != null) SetBusy(false);
            }
        }

        void SetBusy(bool busy)
        {
            _busy = busy;
            _input.interactable = !busy;
            _askButton.interactable = !busy;
            foreach (var b in _quick) b.interactable = !busy;
            _thinking.text = "";
        }

        void CancelPending()
        {
            if (_cts == null) return;
            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }

        void OnClaimRaised(LieData claim) => Say(claim.Statement);

        void OnStateChanged()
        {
            if (IsOpen) Refresh();
        }

        void Refresh()
        {
            var alex = _game.Alex;
            var (tone, color) = alex.Tone switch
            {
                AlexTone.Warm => ("EN LÍNEA", _theme.AlexCyan),
                AlexTone.Formal => ("MODO FORMAL", _theme.WarningAmber),
                _ => ("RESPUESTA LIMITADA", _theme.AccentRed),
            };
            UIFactory.Clear(_toneHolder);
            UIFactory.Chip(_toneHolder, tone, _theme, color, _theme.SizeS - 2);
            _status.text = $"Contradicciones detectadas: {alex.ContradictionCount}";
            if (_lastAlexText != null) _lastAlexText.color = SpeechColor();

            UIFactory.Clear(_claims);
            var claims = alex.RaisedClaims.ToList();
            int pending = claims.Count(c => !alex.IsRefuted(c) && !alex.IsAccepted(c));
            _claimsTitle.text = pending > 0 ? $"OBSERVACIONES DE ALEX · {pending} PENDIENTE{(pending > 1 ? "S" : "")}" : "OBSERVACIONES DE ALEX";
            if (claims.Count == 0)
                UIFactory.Text(_claims, "Vacio", "ALEX todavía no ha hecho observaciones. Aparecen al analizar evidencias.", _theme.MonoFont, _theme.SizeS, _theme.Muted);
            foreach (var claim in claims) BuildCard(claim);
        }

        Color SpeechColor() => _game.Alex.Tone == AlexTone.Cold ? Color.Lerp(_theme.AlexCyan, _theme.Muted, 0.5f) : _theme.AlexCyan;

        void BuildCard(LieData claim)
        {
            var alex = _game.Alex;
            bool refuted = alex.IsRefuted(claim);
            bool accepted = alex.IsAccepted(claim);
            var accent = refuted ? _theme.AccentRed : accepted ? _theme.AlexCyan : _theme.WarningAmber;
            var card = UIFactory.Round(UIFactory.Panel(_claims, $"Tarjeta_{claim.Id}", UITheme.WithAlpha(accent, 0.08f)), _theme);
            var v = UIFactory.VLayout(card.gameObject, 8, 0);
            v.padding = new RectOffset(22, 14, 14, 14);
            var bar = UIFactory.Panel(card.transform, "Acento", accent);
            bar.rectTransform.anchorMin = new Vector2(0, 0);
            bar.rectTransform.anchorMax = new Vector2(0, 1);
            bar.rectTransform.pivot = new Vector2(0, 0.5f);
            bar.rectTransform.offsetMin = new Vector2(0, 8);
            bar.rectTransform.offsetMax = new Vector2(4, -8);
            UIFactory.Ignore(bar.gameObject);

            var top = UIFactory.Rect("Estado", card.transform);
            var th = UIFactory.HLayout(top.gameObject, 0, 0, false);
            th.childControlWidth = false;
            UIFactory.Layout(top.gameObject, preferredHeight: 26);
            UIFactory.Chip(top, refuted ? "REFUTADA · CONTRADICCIÓN" : accepted ? "ACEPTADA" : "PENDIENTE", _theme, accent, _theme.SizeS - 2);

            var text = UIFactory.Text(card.transform, "Afirmacion", $"“{claim.Statement}”", _theme.MonoFont, _theme.SizeS + 1, refuted ? _theme.Muted : _theme.AlexCyan);
            if (refuted) text.fontStyle = FontStyles.Strikethrough;
            if (refuted) return;
            var row = UIFactory.Rect("Botones", card.transform);
            UIFactory.HLayout(row.gameObject, 8);
            UIFactory.Layout(row.gameObject, preferredHeight: 40);
            if (!accepted) UIFactory.Button(row, "Aceptar", _theme, _theme.AlexCyan, _theme.Ink, () => Say(alex.Accept(claim)), 40, _theme.SizeS);
            UIFactory.Ghost(row, "Cuestionar", _theme, _theme.AccentRed, () => OpenChooser(claim), 40, _theme.SizeS);
        }

        void OpenChooser(LieData claim)
        {
            _questioning = claim;
            _chooserTitle.text = $"¿Con qué evidencia refutas a ALEX?\n<size=80%><color={UITheme.Hex(_theme.AlexCyan)}>“{claim.Statement}”</color></size>";
            UIFactory.Clear(_chooserList);
            var evidence = _game.Evidence.AnalyzedEvidence.ToList();
            if (evidence.Count == 0)
                UIFactory.Text(_chooserList, "Vacio", "No tienes evidencias analizadas.", _theme.BodyFont, _theme.SizeBody, _theme.Muted);
            foreach (var ev in evidence)
            {
                var captured = ev;
                var label = $"{ev.DisplayName}\n<size=72%><color={UITheme.Hex(UITheme.WithAlpha(_theme.TextLight, 0.55f))}>{ev.AnalysisResult}</color></size>";
                var b = UIFactory.Button(_chooserList, label, _theme, UITheme.WithAlpha(_theme.TextLight, 0.06f), _theme.TextLight, () => Question(captured), 70, _theme.SizeBody);
                var t = b.GetComponentInChildren<TextMeshProUGUI>();
                t.alignment = TextAlignmentOptions.MidlineLeft;
                t.rectTransform.offsetMin = new Vector2(24, 4);
                t.textWrappingMode = TextWrappingModes.Normal;
                t.overflowMode = TextOverflowModes.Ellipsis;
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
            AddEntry(false, $"Te cuestiono con: {evidence.DisplayName}");
            var result = _game.Alex.Question(claim, evidence);
            Say(result.Response);
            if (result.Success) Glitch(0.2f, show404: true);
        }

        void Say(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            AddEntry(true, text);
        }

        /// <summary>Agrega un mensaje al historial (ALEX a la izquierda en cian, el jugador a la derecha).</summary>
        void AddEntry(bool alex, string text)
        {
            _entries.Add((alex, text));
            if (_entries.Count > MaxLogEntries) _entries.RemoveAt(0);
            if (_log == null) return;
            UIFactory.Clear(_log);
            _lastAlexText = null;
            foreach (var (isAlex, line) in _entries)
            {
                var who = UIFactory.Text(_log, "Quien", isAlex ? "ALEX" : "TÚ", _theme.MonoFont, _theme.SizeS - 3,
                    UITheme.WithAlpha(isAlex ? _theme.AlexCyan : _theme.TextLight, 0.45f), isAlex ? TextAlignmentOptions.Left : TextAlignmentOptions.Right);
                who.characterSpacing = 6f;
                UIFactory.Layout(who.gameObject, preferredHeight: 14);
                var t = UIFactory.Text(_log, "Mensaje", isAlex ? $"> {line}" : line, isAlex ? _theme.MonoFont : _theme.BodyFont,
                    _theme.SizeBody - (isAlex ? 1 : 0), isAlex ? SpeechColor() : UITheme.WithAlpha(_theme.TextLight, 0.85f),
                    isAlex ? TextAlignmentOptions.TopLeft : TextAlignmentOptions.TopRight);
                if (!isAlex) t.fontStyle = FontStyles.Italic;
                if (isAlex) _lastAlexText = t;
            }
            if (isActiveAndEnabled) StartCoroutine(ScrollToEnd());
        }

        IEnumerator ScrollToEnd()
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            if (_logScroll != null) _logScroll.verticalNormalizedPosition = 0f;
        }

        void Update()
        {
            if (!IsOpen) return;
            if (_busy) _thinking.text = "ALEX está procesando" + new string('.', 1 + (int)(Time.unscaledTime * 3f) % 3) + (Time.unscaledTime % 1f < 0.5f ? "_" : "");
            if (_game.Alex.Tone != AlexTone.Cold || Time.unscaledTime < _nextAmbientGlitch) return;
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
                if (_lastAlexText != null) _lastAlexText.color = Random.value > 0.5f ? _theme.AccentRed : _theme.AlexCyan;
                yield return null;
            }
            _panel.anchoredPosition = basePos;
            if (_lastAlexText != null) _lastAlexText.color = SpeechColor();
            if (show404)
            {
                yield return new WaitForSecondsRealtime(0.3f);
                _glitchLabel.gameObject.SetActive(false);
            }
            _glitch = null;
        }
    }
}
