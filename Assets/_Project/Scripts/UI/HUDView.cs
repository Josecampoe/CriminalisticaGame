using System.Collections;
using System.Collections.Generic;
using Caso404.Core;
using Caso404.Data;
using Caso404.Interaction;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Caso404.UI
{
    /// <summary>
    /// HUD de exploración: retícula que crece al apuntar, prompt "E — ...", nombre de la escena (se desvanece en 3 s),
    /// atajos, contador de evidencias sin analizar y avisos cortos. Solo lee estado.
    /// </summary>
    public class HUDView : MonoBehaviour
    {
        [SerializeField] private UITheme _theme;
        [SerializeField] private GameContext _game;
        [SerializeField] private PlayerInteractor _interactor;
        [SerializeField] private Core.Events.BoolEventChannel _uiModalChannel;

        Image _crosshair;
        Image _ring;
        TextMeshProUGUI _prompt;
        CanvasGroup _promptGroup;
        CanvasGroup _toastGroup;
        CanvasGroup _titleGroup;
        Image _toastBar;
        RectTransform _objectiveCard;
        TextMeshProUGUI _sceneTitle;
        TextMeshProUGUI _counter;
        TextMeshProUGUI _toast;
        TextMeshProUGUI _objective;
        readonly Queue<(string text, Color color)> _toasts = new();
        Coroutine _toastRoutine;
        Coroutine _titleRoutine;

        CanvasGroup _group;

        void Awake()
        {
            _group = gameObject.AddComponent<CanvasGroup>();
            _group.blocksRaycasts = false;
            Build();
        }

        /// <summary>Con un panel abierto el HUD se oculta para no encimarse (los avisos siguen en la capa de arriba).</summary>
        void OnModalChanged(bool open) => _group.alpha = open ? 0f : 1f;

        void OnEnable()
        {
            if (_interactor != null) _interactor.FocusChanged += OnFocusChanged;
            if (_uiModalChannel != null) { _uiModalChannel.OnRaised += OnModalChanged; OnModalChanged(_uiModalChannel.LastValue); }
            if (_game == null || _game.State == null) return;
            _game.State.Changed += RefreshCounter;
            _game.Evidence.EvidenceCollected += OnCollected;
            _game.Alex.ClaimRaised += OnClaimRaised;
            _game.Alex.ClaimRefuted += OnClaimRefuted;
        }

        void OnDisable()
        {
            if (_interactor != null) _interactor.FocusChanged -= OnFocusChanged;
            if (_uiModalChannel != null) _uiModalChannel.OnRaised -= OnModalChanged;
            if (_game == null || _game.State == null) return;
            _game.State.Changed -= RefreshCounter;
            _game.Evidence.EvidenceCollected -= OnCollected;
            _game.Alex.ClaimRaised -= OnClaimRaised;
            _game.Alex.ClaimRefuted -= OnClaimRefuted;
        }

        void Start()
        {
            RefreshCounter();
            OnFocusChanged(null);
        }

        /// <summary>Muestra el nombre de la escena y lo desvanece en 3 segundos.</summary>
        public void ShowSceneTitle(string title)
        {
            if (_titleRoutine != null) StopCoroutine(_titleRoutine);
            _titleRoutine = StartCoroutine(FadeTitle(title));
        }

        /// <summary>Aviso corto en la parte baja de la pantalla.</summary>
        public void ShowToast(string text, Color color)
        {
            _toasts.Enqueue((text, color));
            _toastRoutine ??= StartCoroutine(PlayToasts());
        }

        /// <summary>Tecla dibujada como una tecla real (fondo claro translúcido) para textos del HUD.</summary>
        static string Key(string key) => $"<mark=#EDEDED26 padding=\"12,12,6,6\"><b>{key}</b></mark>";

        void Build()
        {
            var root = UIFactory.Stretch((RectTransform)transform);

            // Retícula: punto central y un anillo que aparece al apuntar a algo.
            _ring = UIFactory.Panel(root, "Anillo", UITheme.WithAlpha(_theme.TextLight, 0f));
            if (_theme.FrameSprite != null) { _ring.sprite = _theme.FrameSprite; _ring.type = Image.Type.Sliced; _ring.pixelsPerUnitMultiplier = 0.9f; }
            UIFactory.Place(_ring.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(30, 30));
            _ring.raycastTarget = false;
            _crosshair = UIFactory.Panel(root, "Reticula", UITheme.WithAlpha(_theme.TextLight, 0.8f));
            UIFactory.Round(_crosshair, _theme, 0.3f);
            UIFactory.Place(_crosshair.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(7, 7));
            _crosshair.raycastTarget = false;
            // Contorno oscuro: el punto se ve también sobre paredes claras o lámparas.
            var outline = _crosshair.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.65f);
            outline.effectDistance = new Vector2(1.2f, -1.2f);

            // Indicación de interacción bajo la retícula, en una píldora oscura.
            var promptBg = UIFactory.Round(UIFactory.Panel(root, "Indicacion", new Color(0.03f, 0.035f, 0.045f, 0.72f)), _theme, 1.4f);
            UIFactory.Place(promptBg.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1f), new Vector2(0, -34), new Vector2(10, 46));
            var ph = UIFactory.HLayout(promptBg.gameObject, 0, 0, false);
            ph.padding = new RectOffset(18, 20, 8, 8);
            var pfit = promptBg.gameObject.AddComponent<ContentSizeFitter>();
            pfit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            _prompt = UIFactory.Text(promptBg.transform, "Texto", "", _theme.BodyFont, _theme.SizeBody, _theme.TextLight, TextAlignmentOptions.Center);
            _prompt.textWrappingMode = TextWrappingModes.NoWrap;
            _promptGroup = promptBg.gameObject.AddComponent<CanvasGroup>();
            _promptGroup.alpha = 0f;

            // Nombre de la escena: rótulo a máquina de escribir con una línea debajo.
            // Banda oscura detrás del título para que se lea aunque haya una lámpara encendida detrás.
            var band = UIFactory.Round(UIFactory.Panel(root, "BandaTitulo", new Color(0.02f, 0.025f, 0.03f, 0.7f)), _theme, 1.4f);
            UIFactory.Place(band.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -130), new Vector2(10, 74));
            var bh = UIFactory.HLayout(band.gameObject, 0, 0, false);
            bh.padding = new RectOffset(40, 40, 8, 10);
            band.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            band.raycastTarget = false;
            _titleGroup = band.gameObject.AddComponent<CanvasGroup>();
            _titleGroup.alpha = 0f;
            _sceneTitle = UIFactory.Text(band.transform, "TituloEscena", "", _theme.TitleFont, 44, _theme.TextLight, TextAlignmentOptions.Center);
            _sceneTitle.textWrappingMode = TextWrappingModes.NoWrap;
            _sceneTitle.characterSpacing = 4f;
            var tshadow = _sceneTitle.gameObject.AddComponent<Shadow>();
            tshadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
            tshadow.effectDistance = new Vector2(2f, -2f);

            // Atajos como teclas, abajo a la izquierda.
            var shortcuts = UIFactory.Text(root, "Atajos",
                $"{Key("WASD")} Moverse     {Key("E")} Interactuar     {Key("Tab")} Expediente     {Key("Q")} ALEX     {Key("H")} Tablero     {Key("Esc")} Pausa",
                _theme.BodyFont, _theme.SizeS, UITheme.WithAlpha(_theme.TextLight, 0.8f));
            UIFactory.Place(shortcuts.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(34, 26), new Vector2(1300, 30));

            // Objetivo: tarjeta translúcida arriba a la izquierda con rótulo ámbar.
            var objBg = UIFactory.Round(UIFactory.Panel(root, "Objetivo", new Color(0.03f, 0.035f, 0.045f, 0.62f)), _theme);
            _objectiveCard = objBg.rectTransform;
            UIFactory.Place(_objectiveCard, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28, -24), new Vector2(560, 10));
            var ov = UIFactory.VLayout(objBg.gameObject, 4, 0);
            ov.padding = new RectOffset(20, 18, 12, 14);
            var ofit = objBg.gameObject.AddComponent<ContentSizeFitter>();
            ofit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var accent = UIFactory.Panel(objBg.transform, "Acento", _theme.WarningAmber);
            accent.rectTransform.anchorMin = new Vector2(0, 0);
            accent.rectTransform.anchorMax = new Vector2(0, 1);
            accent.rectTransform.pivot = new Vector2(0, 0.5f);
            accent.rectTransform.offsetMin = new Vector2(0, 10);
            accent.rectTransform.offsetMax = new Vector2(3, -10);
            UIFactory.Ignore(accent.gameObject);
            UIFactory.Kicker(objBg.transform, "Objetivo", _theme, _theme.WarningAmber);
            _objective = UIFactory.Text(objBg.transform, "Texto", "", _theme.BodyFont, _theme.SizeS + 2, UITheme.WithAlpha(_theme.TextLight, 0.92f));

            _counter = UIFactory.Text(root, "Contador", "", _theme.BodyFont, _theme.SizeBody, _theme.WarningAmber, TextAlignmentOptions.TopRight);
            UIFactory.Place(_counter.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-34, -28), new Vector2(700, 36));
            var cshadow = _counter.gameObject.AddComponent<Shadow>();
            cshadow.effectColor = new Color(0f, 0f, 0f, 0.7f);
            cshadow.effectDistance = new Vector2(1f, -1f);

            // Avisos: píldora oscura con barra de color a la izquierda.
            var toastBg = UIFactory.Round(UIFactory.Panel(root, "Aviso", new Color(0.03f, 0.035f, 0.045f, 0.88f)), _theme);
            UIFactory.Place(toastBg.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -150), new Vector2(10, 56));
            var th = UIFactory.HLayout(toastBg.gameObject, 0, 0, false);
            th.padding = new RectOffset(30, 26, 12, 12);
            var tfit = toastBg.gameObject.AddComponent<ContentSizeFitter>();
            tfit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            _toastBar = UIFactory.Panel(toastBg.transform, "Barra", _theme.WarningAmber);
            _toastBar.rectTransform.anchorMin = new Vector2(0, 0);
            _toastBar.rectTransform.anchorMax = new Vector2(0, 1);
            _toastBar.rectTransform.pivot = new Vector2(0, 0.5f);
            _toastBar.rectTransform.offsetMin = new Vector2(10, 12);
            _toastBar.rectTransform.offsetMax = new Vector2(14, -12);
            UIFactory.Ignore(_toastBar.gameObject);
            _toast = UIFactory.Text(toastBg.transform, "Texto", "", _theme.BodyFont, _theme.SizeBody + 2, _theme.TextLight, TextAlignmentOptions.Center);
            _toast.textWrappingMode = TextWrappingModes.NoWrap;
            _toastGroup = toastBg.gameObject.AddComponent<CanvasGroup>();
            _toastGroup.alpha = 0f;
        }

        void OnFocusChanged(IInteractable target)
        {
            bool has = target != null && target.CanInteract;
            _prompt.text = has ? $"{Key("E")}  {target.Prompt}" : "";
            _promptGroup.alpha = has ? 1f : 0f;
            var color = has && target.IsNew ? _theme.WarningAmber : _theme.TextLight;
            _crosshair.rectTransform.sizeDelta = has ? new Vector2(9, 9) : new Vector2(7, 7);
            _crosshair.color = has ? color : UITheme.WithAlpha(_theme.TextLight, 0.8f);
            _ring.color = has ? UITheme.WithAlpha(color, 0.9f) : UITheme.WithAlpha(color, 0f);
        }

        void RefreshCounter()
        {
            if (_game == null || _game.Evidence == null) return;
            int n = _game.Evidence.UnanalyzedCount;
            _counter.text = n > 0 ? $"Evidencias sin analizar: {n}  (Tab)" : "";
            var objective = Objective();
            _objective.text = objective.StartsWith("Objetivo: ") ? objective.Substring(10) : objective;
            _objectiveCard.gameObject.SetActive(!string.IsNullOrEmpty(objective));
            if (_interactor != null) OnFocusChanged(_interactor.Current);
        }

        /// <summary>Qué le falta al jugador en esta escena (guía para no perderse; solo lee el estado).</summary>
        string Objective()
        {
            var g = _game;
            var scene = g.Scene;
            if (g.State.State.accusations.Exists(a => a.caseId == g.Case.Id)) return "Objetivo: caso cerrado.";
            int pending = 0, found = 0;
            foreach (var e in scene.Evidence)
            {
                if (e.Source != EvidenceSource.Scene || !g.Evidence.IsAvailable(e)) continue;
                if (g.Evidence.IsFound(e)) found++; else pending++;
            }
            string explore = pending > 0 ? $"  ·  Objetos por revisar aquí: {pending}" : "";
            // Al llegar a una escena grande, lo primero es recorrerla.
            if (found == 0 && pending > 0) return $"Objetivo: recorre todo el lugar y revisa los objetos sospechosos (E)  ·  Por revisar: {pending}";
            if (g.Evidence.UnanalyzedCount > 0) return "Objetivo: analiza tus evidencias nuevas (Tab)" + explore;
            if (scene.Hypothesis != null && !g.Hypotheses.IsComplete(scene.Hypothesis)) return "Objetivo: investiga y completa la hipótesis (H)" + explore;
            if (scene.ClosingDecision != null && !g.Decisions.HasTaken(scene.ClosingDecision)) return "Objetivo: toma la decisión de cierre (H)" + explore;
            if (scene.HasAccusation) return "Objetivo: interroga y presenta la acusación (expediente para la Fiscalía)";
            if (!g.State.IsSceneCompleted(scene.Id)) return "Objetivo: termina la escena desde el tablero (H)" + explore;
            return "";
        }

        void OnCollected(EvidenceData ev) =>
            ShowToast($"Guardada en el expediente: {ev.DisplayName}" + (ev.CanBeAnalyzed ? "   ·   Tab para analizar" : ""), _theme.WarningAmber);
        void OnClaimRaised(LieData claim) => ShowToast("ALEX tiene una observación  (Q)", _theme.AlexCyan);
        void OnClaimRefuted(LieData claim) => ShowToast("Contradicción de ALEX detectada", _theme.AccentRed);

        IEnumerator FadeTitle(string title)
        {
            _sceneTitle.text = title;
            for (float t = 0; t < 0.4f; t += Time.deltaTime)
            {
                _titleGroup.alpha = t / 0.4f;
                yield return null;
            }
            _titleGroup.alpha = 1f;
            yield return new WaitForSeconds(2.2f);
            for (float t = 0; t < 0.8f; t += Time.deltaTime)
            {
                _titleGroup.alpha = 1f - t / 0.8f;
                yield return null;
            }
            _titleGroup.alpha = 0f;
        }

        IEnumerator PlayToasts()
        {
            while (_toasts.Count > 0)
            {
                var (text, color) = _toasts.Dequeue();
                _toast.text = text;
                _toast.color = Color.Lerp(color, Color.white, 0.25f);
                _toastBar.color = color;
                for (float t = 0; t < 0.15f; t += Time.unscaledDeltaTime)
                {
                    _toastGroup.alpha = t / 0.15f;
                    yield return null;
                }
                _toastGroup.alpha = 1f;
                yield return new WaitForSecondsRealtime(2.2f);
                for (float t = 0; t < 0.3f; t += Time.unscaledDeltaTime)
                {
                    _toastGroup.alpha = 1f - t / 0.3f;
                    yield return null;
                }
                _toastGroup.alpha = 0f;
            }
            _toastRoutine = null;
        }
    }
}
