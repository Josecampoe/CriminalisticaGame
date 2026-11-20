using System.Collections;
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
    /// Laboratorio forense (Tab), con paleta clínica blanca y azul claro:
    /// a la izquierda las muestras recogidas; en el centro la cámara de análisis con la evidencia en 3D girando sobre
    /// una mesa (se puede girar arrastrando) y el botón Iniciar análisis; a la derecha el informe del laboratorio.
    /// Solo llama a EvidenceService: el resultado se guarda al pulsar el botón y la animación del escáner lo presenta.
    /// </summary>
    public class JournalView : UIPanel
    {
        [SerializeField] private GameContext _game;
        [Tooltip("Duración de la animación de análisis (segundos).")]
        [SerializeField] private float _scanSeconds = 1.8f;

        RectTransform _list;
        TextMeshProUGUI _header;
        // Cámara de análisis
        LabStage _stage;
        RawImage _view;
        RectTransform _scanBand;
        TextMeshProUGUI _sampleTag;
        TextMeshProUGUI _name;
        TextMeshProUGUI _description;
        TextMeshProUGUI _status;
        TextMeshProUGUI _percent;
        RectTransform _progressFill;
        Image _progressImage;
        Button _analyze;
        TextMeshProUGUI _reason;
        // Informe
        TextMeshProUGUI _reportTitle;
        TextMeshProUGUI _reportNumber;
        Image _reportBand;
        TextMeshProUGUI _report;
        TextMeshProUGUI _detail;
        TextMeshProUGUI _extraResult;
        TextMeshProUGUI _stamp;
        Image _stampFrame;
        Button _extra;
        TMP_InputField _password;
        TextMeshProUGUI _glitch;

        EvidenceData _selected;
        EvidenceData _shown;
        EvidenceData _scanning;
        float _scanProgress;

        Color Blue => _theme.LabBlue;
        Color Ink => _theme.LabInk;

        protected override void Build()
        {
            var rt = UIFactory.Stretch((RectTransform)transform);
            var bg = UIFactory.Panel(rt, "FondoLaboratorio", _theme.LabBg);
            UIFactory.Stretch(bg.rectTransform);
            bg.raycastTarget = true;
            BuildGrid(rt);

            _stage = new GameObject("Mesa del laboratorio").AddComponent<LabStage>();
            _stage.Build(Color.Lerp(_theme.LabBg, Color.white, 0.55f));

            // Encabezado
            var head = UIFactory.Rect("Encabezado", rt);
            UIFactory.Place(head, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(60, -28), new Vector2(1300, 84));
            UIFactory.VLayout(head.gameObject, 2);
            UIFactory.Kicker(head, "Laboratorio forense  ·  Unidad de Criminalística de Valdora", _theme, Blue);
            _header = UIFactory.Text(head, "Titulo", "", _theme.BodyFont, 38, Ink);
            _header.fontStyle = FontStyles.Bold;
            // Azul claro en vez de blanco: el blanco con el tinte de reposo del botón quedaba gris y parecía deshabilitado.
            var close = UIFactory.Button(rt, "Cerrar   Esc", _theme, _theme.LabBlueLight, Blue, Close, 48);
            UIFactory.Frame(close.transform, _theme, UITheme.WithAlpha(Blue, 0.6f));
            UIFactory.Place((RectTransform)close.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-60, -40), new Vector2(200, 48));

            BuildSampleList(rt);
            BuildAnalysis(rt);
            BuildReport(rt);

            var keys = UIFactory.Text(rt, "Teclas",
                $"<mark={UITheme.Hex(UITheme.WithAlpha(Blue, 0.15f))} padding=\"10,10,4,4\"><b>↑ ↓</b></mark>  cambiar de muestra      " +
                $"<mark={UITheme.Hex(UITheme.WithAlpha(Blue, 0.15f))} padding=\"10,10,4,4\"><b>Enter</b></mark>  analizar      " +
                $"<mark={UITheme.Hex(UITheme.WithAlpha(Blue, 0.15f))} padding=\"10,10,4,4\"><b>Arrastrar</b></mark>  girar la evidencia      " +
                $"<mark={UITheme.Hex(UITheme.WithAlpha(Blue, 0.15f))} padding=\"10,10,4,4\"><b>Tab</b></mark>  cerrar",
                _theme.BodyFont, _theme.SizeS, _theme.LabMuted, TextAlignmentOptions.MidlineLeft);
            UIFactory.Place(keys.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(60, 22), new Vector2(1200, 30));
            keys.raycastTarget = false;

            _glitch = UIFactory.Text(rt, "Glitch404", "404", _theme.MonoFont, 220, _theme.AlexCyan, TextAlignmentOptions.Center);
            UIFactory.Stretch(_glitch.rectTransform);
            _glitch.gameObject.SetActive(false);
        }

        /// <summary>Cuadrícula muy tenue de fondo, como papel milimetrado de laboratorio.</summary>
        void BuildGrid(RectTransform rt)
        {
            var grid = UIFactory.Rect("Rejilla", rt);
            UIFactory.Stretch(grid);
            var line = UITheme.WithAlpha(Blue, 0.06f);
            for (int x = 0; x <= 1920; x += 48)
            {
                var v = UIFactory.Panel(grid, "V", line);
                v.raycastTarget = false;
                UIFactory.Place(v.rectTransform, new Vector2(x / 1920f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1, 1200));
            }
            for (int y = 0; y <= 1080; y += 48)
            {
                var h = UIFactory.Panel(grid, "H", line);
                h.raycastTarget = false;
                UIFactory.Place(h.rectTransform, new Vector2(0.5f, y / 1080f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(2000, 1));
            }
        }

        /// <summary>Tarjeta blanca con sombra suave y borde azul tenue.</summary>
        Image LabCard(Transform parent, string name, Vector2 min, Vector2 max)
        {
            var card = UIFactory.Card(parent, name, _theme.LabPanel, _theme);
            var shadow = card.GetComponent<Shadow>();
            if (shadow != null) { shadow.effectColor = new Color(0.1f, 0.25f, 0.4f, 0.12f); shadow.effectDistance = new Vector2(0f, -6f); }
            UIFactory.Region(card.rectTransform, min, max);
            UIFactory.Frame(card.transform, _theme, UITheme.WithAlpha(Blue, 0.18f));
            return card;
        }

        void BuildSampleList(RectTransform rt)
        {
            var card = LabCard(rt, "Muestras", new Vector2(0.03f, 0.08f), new Vector2(0.255f, 0.86f));
            var inner = UIFactory.Rect("Contenido", card.transform);
            UIFactory.Stretch(inner, 18, 16, 18, 20);
            UIFactory.VLayout(inner.gameObject, 10);
            UIFactory.Kicker(inner, "Muestras recibidas", _theme, _theme.LabMuted);
            UIFactory.Divider(inner, UITheme.WithAlpha(Blue, 0.15f));
            var holder = UIFactory.Rect("Lista", inner);
            UIFactory.Layout(holder.gameObject, flexibleHeight: 1);
            _list = UIFactory.ScrollList(holder, "Cards", 8, 2);
            UIFactory.Stretch((RectTransform)_list.parent.parent);
        }

        void BuildAnalysis(RectTransform rt)
        {
            var card = LabCard(rt, "CamaraDeAnalisis", new Vector2(0.27f, 0.08f), new Vector2(0.64f, 0.86f));
            var inner = UIFactory.Rect("Contenido", card.transform);
            UIFactory.Stretch(inner, 26, 22, 26, 22);
            UIFactory.VLayout(inner.gameObject, 10);

            _sampleTag = UIFactory.Kicker(inner, "", _theme, Blue);

            // Visor: la imagen 3D de la mesa, con esquinas de mira y la banda del escáner encima.
            var viewer = UIFactory.Round(UIFactory.Panel(inner, "Visor", Color.white), _theme);
            UIFactory.Layout(viewer.gameObject, preferredHeight: 430, flexibleHeight: 1);
            viewer.gameObject.AddComponent<RectMask2D>();
            _view = new GameObject("Imagen", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
            _view.transform.SetParent(viewer.transform, false);
            UIFactory.Stretch(_view.rectTransform);
            _view.texture = _stage.Texture;
            _view.gameObject.AddComponent<LabDragArea>().Stage = _stage;
            UIFactory.Frame(viewer.transform, _theme, UITheme.WithAlpha(Blue, 0.25f));
            foreach (var corner in new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) })
                Corner(viewer.transform, corner);
            var hint = UIFactory.Text(viewer.transform, "Ayuda", "Arrastra para girar", _theme.MonoFont, _theme.SizeS - 2, _theme.LabMuted, TextAlignmentOptions.Bottom);
            hint.characterSpacing = 2f;
            hint.raycastTarget = false;
            // Centrado abajo, entre las esquinas de mira (en la esquina derecha quedaba encima del trazo azul).
            UIFactory.Place(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 16), new Vector2(300, 22));
            var band = UIFactory.Panel(viewer.transform, "Escaner", UITheme.WithAlpha(Blue, 0.18f));
            band.raycastTarget = false;
            _scanBand = band.rectTransform;
            _scanBand.anchorMin = new Vector2(0f, 0f);
            _scanBand.anchorMax = new Vector2(1f, 0f);
            _scanBand.pivot = new Vector2(0.5f, 0.5f);
            _scanBand.sizeDelta = new Vector2(0f, 40f);
            var beam = UIFactory.Panel(band.transform, "Linea", UITheme.WithAlpha(Blue, 0.85f));
            beam.raycastTarget = false;
            beam.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            beam.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            beam.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            beam.rectTransform.anchoredPosition = Vector2.zero;
            beam.rectTransform.sizeDelta = new Vector2(0f, 2f);
            _scanBand.gameObject.SetActive(false);

            _name = UIFactory.Text(inner, "Nombre", "", _theme.BodyFont, 30, Ink);
            _name.fontStyle = FontStyles.Bold;
            _name.textWrappingMode = TextWrappingModes.NoWrap;
            _name.overflowMode = TextOverflowModes.Ellipsis;
            UIFactory.Layout(_name.gameObject, minHeight: 44, preferredHeight: 44);
            _description = UIFactory.Text(inner, "Descripcion", "", _theme.BodyFont, _theme.SizeBody, _theme.LabMuted);
            _description.enableAutoSizing = true;
            _description.fontSizeMin = 14f;
            _description.fontSizeMax = _theme.SizeBody;
            UIFactory.Layout(_description.gameObject, preferredHeight: 50);

            var statusRow = UIFactory.Rect("Estado", inner);
            UIFactory.HLayout(statusRow.gameObject, 10, 0, false);
            UIFactory.Layout(statusRow.gameObject, preferredHeight: 22);
            _status = UIFactory.Text(statusRow, "Texto", "", _theme.MonoFont, _theme.SizeS, Blue);
            _status.characterSpacing = 3f;
            UIFactory.Layout(_status.gameObject, flexibleWidth: 1, preferredHeight: 22);
            _percent = UIFactory.Text(statusRow, "Porcentaje", "", _theme.MonoFont, _theme.SizeS, Blue, TextAlignmentOptions.TopRight);
            UIFactory.Layout(_percent.gameObject, preferredWidth: 80, preferredHeight: 22);
            var track = UIFactory.Round(UIFactory.Panel(inner, "Progreso", _theme.LabBlueLight), _theme, 0.4f);
            track.raycastTarget = false;
            UIFactory.Layout(track.gameObject, minHeight: 8, preferredHeight: 8);
            _progressImage = UIFactory.Round(UIFactory.Panel(track.transform, "Relleno", Blue), _theme, 0.4f);
            _progressImage.raycastTarget = false;
            _progressFill = _progressImage.rectTransform;
            _progressFill.anchorMin = Vector2.zero;
            _progressFill.anchorMax = new Vector2(0f, 1f);
            _progressFill.offsetMin = _progressFill.offsetMax = Vector2.zero;

            var actions = UIFactory.Rect("Acciones", inner);
            var ah = UIFactory.HLayout(actions.gameObject, 18, 0, false);
            ah.childAlignment = TextAnchor.MiddleLeft;
            UIFactory.Layout(actions.gameObject, preferredHeight: 52);
            _analyze = UIFactory.Button(actions, "Iniciar análisis   Enter", _theme, Blue, Color.white, OnAnalyze, 52);
            UIFactory.Layout(_analyze.gameObject, minHeight: 52, preferredHeight: 52, preferredWidth: 320);
            _reason = UIFactory.Text(actions, "Motivo", "", _theme.BodyFont, _theme.SizeS, _theme.LabMuted, TextAlignmentOptions.MidlineLeft);
            _reason.enableAutoSizing = true;
            _reason.fontSizeMin = 12f;
            _reason.fontSizeMax = _theme.SizeS;
            UIFactory.Layout(_reason.gameObject, flexibleWidth: 1, preferredHeight: 52);
        }

        /// <summary>Esquina de mira (dos trazos azules en L) sobre el visor.</summary>
        void Corner(Transform parent, Vector2 anchor)
        {
            var inward = new Vector2(anchor.x < 0.5f ? 14f : -14f, anchor.y < 0.5f ? 14f : -14f);
            var h = UIFactory.Panel(parent, "Mira", Blue);
            h.raycastTarget = false;
            UIFactory.Place(h.rectTransform, anchor, anchor, inward, new Vector2(30, 3));
            var v = UIFactory.Panel(parent, "Mira", Blue);
            v.raycastTarget = false;
            UIFactory.Place(v.rectTransform, anchor, anchor, inward, new Vector2(3, 30));
        }

        void BuildReport(RectTransform rt)
        {
            var card = LabCard(rt, "Informe", new Vector2(0.655f, 0.08f), new Vector2(0.97f, 0.86f));
            // Franja superior azul con el título del informe y su número.
            _reportBand = UIFactory.Panel(card.transform, "Franja", Blue);
            _reportBand.raycastTarget = false;
            var brt = _reportBand.rectTransform;
            brt.anchorMin = new Vector2(0f, 1f);
            brt.anchorMax = new Vector2(1f, 1f);
            brt.pivot = new Vector2(0.5f, 1f);
            brt.sizeDelta = new Vector2(0f, 64f);
            brt.anchoredPosition = Vector2.zero;
            _reportTitle = UIFactory.Text(_reportBand.transform, "Titulo", "", _theme.MonoFont, _theme.SizeS + 1, Color.white, TextAlignmentOptions.MidlineLeft);
            _reportTitle.characterSpacing = 6f;
            _reportTitle.fontStyle = FontStyles.Bold;
            UIFactory.Stretch(_reportTitle.rectTransform, 26, 0, 26, 0);
            _reportNumber = UIFactory.Text(_reportBand.transform, "Numero", "", _theme.MonoFont, _theme.SizeS - 1, UITheme.WithAlpha(Color.white, 0.85f), TextAlignmentOptions.MidlineRight);
            UIFactory.Stretch(_reportNumber.rectTransform, 26, 0, 26, 0);

            var content = UIFactory.ScrollList(card.transform, "Hoja", 12, 0);
            var scrollRoot = (RectTransform)content.parent.parent;
            UIFactory.Stretch(scrollRoot, 28, 88, 28, 88);
            _report = UIFactory.Text(content, "Resultado", "", _theme.BodyFont, 22, Ink);
            _report.lineSpacing = 6f;
            _detail = UIFactory.Text(content, "Detalle", "", _theme.BodyFont, _theme.SizeBody, _theme.AccentRed);
            _extra = UIFactory.Button(content, "Análisis adicional", _theme, Blue, Color.white, OnExtra, 50);
            _password = UIFactory.Input(content, _theme, "Contraseña", 40, Blue);
            _extraResult = UIFactory.Text(content, "ResultadoExtra", "", _theme.BodyFont, _theme.SizeBody, Ink);

            var footer = UIFactory.Text(card.transform, "Firma", "ANALISTA: INV. RIVERA\nVERIFICÓ: ____________________",
                _theme.MonoFont, _theme.SizeS - 2, _theme.LabMuted, TextAlignmentOptions.BottomLeft);
            footer.characterSpacing = 2f;
            footer.lineSpacing = 10f;
            footer.raycastTarget = false;
            UIFactory.Place(footer.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(28, 18), new Vector2(420, 50));

            _stamp = UIFactory.Stamp(card.transform, "CONCLUIDO", _theme, 26f, 8f);
            UIFactory.Place(_stamp.rectTransform, new Vector2(1, 0), new Vector2(0.5f, 0.5f), new Vector2(-160, 64), new Vector2(210, 48));
            _stampFrame = _stamp.GetComponentInChildren<Image>();
        }

        void OnEnable()
        {
            if (_game != null && _game.State != null) _game.State.Changed += OnStateChanged;
        }

        void OnDisable()
        {
            if (_game != null && _game.State != null) _game.State.Changed -= OnStateChanged;
            _scanning = null;
        }

        void OnDestroy()
        {
            if (_stage != null) Destroy(_stage.gameObject);
        }

        protected override void OnShown()
        {
            _scanning = null;
            _shown = null;
            _stamp.rectTransform.localScale = Vector3.one;
            _stage.SetActive(true);
            UpdateHeader();
            // Se abre en la primera muestra que falta analizar: es lo que el jugador viene a hacer.
            var pendingEvidence = _game.Evidence.FoundEvidence.FirstOrDefault(e => _game.Evidence.CanAnalyze(e, out _));
            if (pendingEvidence != null) _selected = pendingEvidence;
            else if (_selected == null || !_game.Evidence.IsFound(_selected)) _selected = _game.Evidence.FoundEvidence.FirstOrDefault();
            Refresh();
        }

        protected override void OnHidden()
        {
            _stage.SetScan(-1f);
            _stage.SetActive(false);
        }

        void UpdateHeader()
        {
            int found = _game.Evidence.FoundEvidence.Count();
            int pending = _game.Evidence.UnanalyzedCount;
            _header.text = $"{_game.Case.Title}   <size=50%><color={UITheme.Hex(_theme.LabMuted)}>{found} muestra{(found == 1 ? "" : "s")}" +
                           (pending > 0 ? $" · <color={UITheme.Hex(Blue)}>{pending} sin analizar</color>" : "") + "</color></size>";
        }

        /// <summary>Teclado: flechas (o W/S) para cambiar de muestra y Enter para analizar.</summary>
        void Update()
        {
            if (IsOpen) FitView();
            if (!IsOpen || _ui == null || _ui.Top != this || UIFactory.IsTyping() || _scanning != null) return;
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
                // Mantiene visible la muestra elegida cuando la lista es más larga que la pantalla.
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

        /// <summary>Recorta la imagen de la mesa a la forma del visor (sin estirarla).</summary>
        void FitView()
        {
            var size = _view.rectTransform.rect.size;
            var tex = _stage.Texture;
            if (size.x <= 1f || size.y <= 1f || tex == null) return;
            float view = size.x / size.y, image = (float)tex.width / tex.height;
            _view.uvRect = view > image
                ? new Rect(0f, (1f - image / view) * 0.5f, 1f, image / view)
                : new Rect((1f - view / image) * 0.5f, 0f, view / image, 1f);
        }

        void OnStateChanged()
        {
            if (!IsOpen) return;
            UpdateHeader();
            Refresh();
        }

        void Refresh()
        {
            RebuildList();
            ShowDetail(_selected);
        }

        string SampleCode(EvidenceData ev) => $"M-{_game.Evidence.FoundEvidence.ToList().IndexOf(ev) + 1:00}";

        void RebuildList()
        {
            UIFactory.Clear(_list);
            var found = _game.Evidence.FoundEvidence.ToList();
            if (found.Count == 0)
            {
                UIFactory.Text(_list, "Empty", "Todavía no hay muestras. Explora la escena y pulsa E sobre los objetos.",
                    _theme.BodyFont, _theme.SizeBody, _theme.LabMuted);
                return;
            }
            for (int i = 0; i < found.Count; i++)
            {
                var ev = found[i];
                var captured = ev;
                var (status, color) = StatusOf(ev);
                bool selected = ev == _selected;
                var bg = selected ? _theme.LabBlueLight : UITheme.WithAlpha(_theme.LabBg, 0.6f);
                var code = $"<font=\"{_theme.MonoFont.name}\"><color={UITheme.Hex(_theme.LabMuted)}>M-{i + 1:00}</color></font>   ";
                var btn = UIFactory.Button(_list, $"{code}{ev.DisplayName}\n<size=66%><font=\"{_theme.MonoFont.name}\"><color={UITheme.Hex(color)}>● {status}</color></font></size>",
                    _theme, bg, Ink, () => { if (_scanning == null) { _selected = captured; Refresh(); } }, 68);
                var label = btn.GetComponentInChildren<TextMeshProUGUI>();
                label.alignment = TextAlignmentOptions.MidlineLeft;
                label.fontStyle = FontStyles.Normal;
                label.rectTransform.offsetMin = new Vector2(26, 4);
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.overflowMode = TextOverflowModes.Ellipsis;
                // Franja de color a la izquierda, como la etiqueta de un tubo de muestra.
                var strip = UIFactory.Panel(btn.transform, "Etiqueta", color);
                strip.raycastTarget = false;
                UIFactory.Ignore(strip.gameObject);
                strip.rectTransform.anchorMin = new Vector2(0f, 0f);
                strip.rectTransform.anchorMax = new Vector2(0f, 1f);
                strip.rectTransform.pivot = new Vector2(0f, 0.5f);
                strip.rectTransform.offsetMin = new Vector2(8, 14);
                strip.rectTransform.offsetMax = new Vector2(12, -14);
                if (selected) UIFactory.Frame(btn.transform, _theme, UITheme.WithAlpha(Blue, 0.9f));
            }
        }

        (string status, Color color) StatusOf(EvidenceData ev)
        {
            if (_scanning == ev) return ("ANALIZANDO…", Blue);
            if (!ev.CanBeAnalyzed) return ("SIN VALOR COMO PRUEBA", _theme.LabMuted);
            if (_game.Evidence.IsAnalyzed(ev)) return ev.AnalyzedByAlex ? ("PROCESADA POR ALEX", Darken(_theme.AlexCyan)) : ("ANALIZADA", _theme.LabGreen);
            return ("PENDIENTE DE ANÁLISIS", Blue);
        }

        static string SourceLabel(EvidenceSource source) => source switch
        {
            EvidenceSource.Dialogue => "ENTREGADA EN DECLARACIÓN",
            EvidenceSource.Unlock => "DERIVADA DE OTRO ANÁLISIS",
            _ => "RECOGIDA EN LA ESCENA"
        };

        void ShowDetail(EvidenceData ev)
        {
            bool has = ev != null;
            bool scanning = has && _scanning == ev;
            bool analyzed = has && _game.Evidence.IsAnalyzed(ev) && !scanning;
            bool byAlex = analyzed && ev.AnalyzedByAlex;

            if (ev != _shown)
            {
                _shown = ev;
                _stage.Show(ev);
            }

            _sampleTag.text = has ? $"CÁMARA DE ANÁLISIS  ·  MUESTRA {SampleCode(ev)}  ·  {SourceLabel(ev.Source)}" : "CÁMARA DE ANÁLISIS";
            _name.text = has ? ev.DisplayName : "Ninguna muestra seleccionada";
            _description.text = has ? ev.Description : "Elige una muestra de la lista.";

            string reason = "";
            bool canAnalyze = has && !scanning && _game.Evidence.CanAnalyze(ev, out reason);
            _analyze.gameObject.SetActive(has && !analyzed && !scanning && ev.CanBeAnalyzed);
            _analyze.interactable = canAnalyze;
            _reason.text = has && !analyzed && !scanning && ev.CanBeAnalyzed && !canAnalyze ? (reason ?? "") : "";

            var accent = byAlex ? Darken(_theme.AlexCyan) : analyzed ? _theme.LabGreen : Blue;
            if (!scanning)
            {
                _scanBand.gameObject.SetActive(false);
                _stage.SetScan(-1f);
                _progressFill.anchorMax = new Vector2(analyzed ? 1f : 0f, 1f);
                _progressImage.color = accent;
                _status.color = accent;
                _percent.color = accent;
                _percent.text = analyzed ? "100%" : "";
                _status.text = !has ? "EN ESPERA"
                    : !ev.CanBeAnalyzed ? "NO REQUIERE ANÁLISIS"
                    : analyzed ? (byAlex ? "ANÁLISIS COMPLETO · PROCESADO POR ALEX" : "ANÁLISIS COMPLETO")
                    : canAnalyze ? "LISTA PARA ANALIZAR" : "EN ESPERA DE REQUISITOS";
            }

            // Informe
            _reportBand.color = byAlex ? Darken(_theme.AlexCyan) : Blue;
            _reportTitle.text = byAlex ? "INFORME PROCESADO POR ALEX" : "INFORME DE LABORATORIO";
            _reportNumber.text = has ? $"N.º 404-{_game.Case.Id.ToUpperInvariant()}-{SampleCode(ev).Substring(2)}" : "";
            if (!has) _report.text = "";
            else if (scanning) _report.text = $"<color={UITheme.Hex(_theme.LabMuted)}><i>Procesando la muestra…</i></color>";
            else if (analyzed || !ev.CanBeAnalyzed) _report.text = ev.AnalysisResult;
            else _report.text = $"<color={UITheme.Hex(_theme.LabMuted)}><i>Sin resultados todavía. Inicia el análisis de la muestra en la cámara de análisis.</i></color>";
            _report.font = byAlex ? _theme.MonoFont : _theme.BodyFont;
            _report.color = byAlex ? Darken(_theme.AlexCyan) : Ink;
            _detail.text = has && !scanning && ev.HasHiddenDetail && _game.State.IsHiddenDetailSeen(ev.Id) ? $"Observación: {ev.HiddenDetailText}" : "";

            bool showStamp = analyzed || (has && !ev.CanBeAnalyzed);
            _stamp.gameObject.SetActive(showStamp);
            if (showStamp)
            {
                _stamp.text = !ev.CanBeAnalyzed ? "SIN VALOR" : byAlex ? "ALEX" : "CONCLUIDO";
                var stampColor = byAlex ? Darken(_theme.AlexCyan) : !ev.CanBeAnalyzed ? _theme.LabMuted : _theme.AccentRed;
                _stamp.color = UITheme.WithAlpha(stampColor, 0.75f);
                if (_stampFrame != null) _stampFrame.color = UITheme.WithAlpha(stampColor, 0.65f);
            }

            bool extraVisible = analyzed && ev.HasExtraAnalysis;
            bool extraDone = extraVisible && _game.State.IsExtraAnalysisDone(ev.Id);
            _extra.gameObject.SetActive(extraVisible && !extraDone);
            if (extraVisible) UIFactory.SetLabel(_extra, ev.ExtraAnalysis.ButtonLabel);
            _password.gameObject.SetActive(extraVisible && !extraDone && ev.ExtraAnalysis.Kind == ExtraAnalysisKind.Password);
            _extraResult.text = extraDone
                ? ev.ExtraAnalysis.ResultText + (string.IsNullOrEmpty(ev.ExtraAnalysis.AlexComment) ? "" : $"\n<color={UITheme.Hex(Darken(_theme.AlexCyan))}>ALEX: \"{ev.ExtraAnalysis.AlexComment}\"</color>")
                : "";
        }

        void OnAnalyze()
        {
            if (_selected == null || _scanning != null) return;
            var ev = _selected;
            if (!_game.Evidence.CanAnalyze(ev, out _)) return;
            // El resultado queda guardado de inmediato; la animación solo lo presenta.
            _scanning = ev;
            _scanProgress = 0f;
            _game.Evidence.Analyze(ev);
            if (isActiveAndEnabled) StartCoroutine(Scan(ev));
            else { _scanning = null; Refresh(); }
        }

        IEnumerator Scan(EvidenceData ev)
        {
            UiSound.Click();
            _scanBand.gameObject.SetActive(true);
            Refresh();
            var viewer = (RectTransform)_scanBand.parent;
            for (float t = 0f; t < _scanSeconds && _scanning == ev; t += Time.unscaledDeltaTime)
            {
                _scanProgress = t / _scanSeconds;
                _stage.SetScan(_scanProgress);
                // La banda sube y baja dos veces sobre el visor.
                _scanBand.anchoredPosition = new Vector2(0f, Mathf.PingPong(_scanProgress * 2f, 1f) * viewer.rect.height);
                _progressFill.anchorMax = new Vector2(_scanProgress, 1f);
                _percent.text = $"{Mathf.FloorToInt(_scanProgress * 100f)}%";
                _status.text = _scanProgress < 0.35f ? "PREPARANDO MUESTRA…" : _scanProgress < 0.75f ? "ESCANEANDO…" : "COMPARANDO CON LA BASE DE DATOS…";
                yield return null;
            }
            if (_scanning != ev) yield break;
            _scanning = null;
            Refresh();
            UiSound.Click();
            // El sello cae sobre el informe.
            var srt = _stamp.rectTransform;
            for (float t = 0f; t < 0.2f; t += Time.unscaledDeltaTime)
            {
                float k = 1f - t / 0.2f;
                srt.localScale = Vector3.one * (1f + 0.6f * k * k);
                yield return null;
            }
            srt.localScale = Vector3.one;
        }

        void OnExtra()
        {
            if (_selected == null) return;
            var ev = _selected;
            var result = _game.Evidence.RunExtraAnalysis(ev, _password.text);
            Refresh();
            if (!result.Success) _extraResult.text = $"<color={UITheme.Hex(_theme.AccentRed)}>{result.Text}</color>";
            else if (ev.ExtraAnalysis.AlexGlitch) StartCoroutine(Glitch404());
        }

        /// <summary>GUION §4.5: al abrir la carpeta directiva_404, la interfaz de ALEX hace glitch y aparece "404" medio segundo.</summary>
        IEnumerator Glitch404()
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

        // El cian sobre fondo blanco no llega a 4.5:1; se oscurece para mantener el contraste.
        static Color Darken(Color c) => Color.Lerp(c, Color.black, 0.55f);
    }
}
