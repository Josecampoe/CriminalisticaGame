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
        TextMeshProUGUI _prompt;
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

        void Build()
        {
            var root = UIFactory.Stretch((RectTransform)transform);

            _crosshair = UIFactory.Panel(root, "Crosshair", UITheme.WithAlpha(_theme.TextLight, 0.8f));
            UIFactory.Place(_crosshair.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(6, 6));
            _crosshair.raycastTarget = false;

            _prompt = UIFactory.Text(root, "Prompt", "", _theme.BodyFont, _theme.SizeBody, _theme.TextLight, TextAlignmentOptions.Center);
            UIFactory.Place(_prompt.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1f), new Vector2(0, -28), new Vector2(900, 40));

            _sceneTitle = UIFactory.Text(root, "SceneTitle", "", _theme.TitleFont, _theme.SizeL, _theme.TextLight, TextAlignmentOptions.Center);
            UIFactory.Place(_sceneTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -60), new Vector2(1400, 60));

            var shortcuts = UIFactory.Text(root, "Shortcuts", "<b>Tab</b> Expediente    <b>Q</b> ALEX    <b>H</b> Hipótesis    <b>E</b> Interactuar",
                _theme.BodyFont, _theme.SizeS, UITheme.WithAlpha(_theme.TextLight, 0.75f));
            UIFactory.Place(shortcuts.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(32, 24), new Vector2(900, 30));

            _objective = UIFactory.Text(root, "Objective", "", _theme.BodyFont, _theme.SizeS, UITheme.WithAlpha(_theme.TextLight, 0.85f));
            UIFactory.Place(_objective.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(32, -24), new Vector2(900, 60));

            _counter = UIFactory.Text(root, "Counter", "", _theme.BodyFont, _theme.SizeBody, _theme.WarningAmber, TextAlignmentOptions.TopRight);
            UIFactory.Place(_counter.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-32, -24), new Vector2(600, 36));

            _toast = UIFactory.Text(root, "Toast", "", _theme.BodyFont, _theme.SizeM, _theme.TextLight, TextAlignmentOptions.Center);
            UIFactory.Place(_toast.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -130), new Vector2(1400, 50));
            _toast.alpha = 0f;
        }

        void OnFocusChanged(IInteractable target)
        {
            bool has = target != null && target.CanInteract;
            _prompt.text = has ? $"<b>E</b> — {target.Prompt}" : "";
            _crosshair.rectTransform.sizeDelta = has ? new Vector2(14, 14) : new Vector2(6, 6);
            _crosshair.color = has ? (target.IsNew ? _theme.WarningAmber : _theme.TextLight) : UITheme.WithAlpha(_theme.TextLight, 0.8f);
        }

        void RefreshCounter()
        {
            if (_game == null || _game.Evidence == null) return;
            int n = _game.Evidence.UnanalyzedCount;
            _counter.text = n > 0 ? $"Evidencias sin analizar: {n}  (Tab)" : "";
            _objective.text = Objective();
            if (_interactor != null) OnFocusChanged(_interactor.Current);
        }

        /// <summary>Qué le falta al jugador en esta escena (guía para no perderse; solo lee el estado).</summary>
        string Objective()
        {
            var g = _game;
            var scene = g.Scene;
            if (g.State.State.accusations.Exists(a => a.caseId == g.Case.Id)) return "Objetivo: caso cerrado.";
            int pending = 0;
            foreach (var e in scene.Evidence)
                if (e.Source == EvidenceSource.Scene && g.Evidence.IsAvailable(e) && !g.Evidence.IsFound(e)) pending++;
            string explore = pending > 0 ? $"  ·  Objetos por revisar aquí: {pending}" : "";
            if (g.Evidence.UnanalyzedCount > 0) return "Objetivo: analiza tus evidencias nuevas (Tab)" + explore;
            if (scene.Hypothesis != null && !g.Hypotheses.IsComplete(scene.Hypothesis)) return "Objetivo: investiga y completa la hipótesis (H)" + explore;
            if (scene.ClosingDecision != null && !g.Decisions.HasTaken(scene.ClosingDecision)) return "Objetivo: toma la decisión de cierre (H)" + explore;
            if (scene.HasAccusation) return "Objetivo: interroga y presenta la acusación (expediente para la Fiscalía)";
            if (!g.State.IsSceneCompleted(scene.Id)) return "Objetivo: termina la escena desde el tablero (H)" + explore;
            return "";
        }

        void OnCollected(EvidenceData ev) => ShowToast($"Guardada en el expediente: {ev.DisplayName}", _theme.WarningAmber);
        void OnClaimRaised(LieData claim) => ShowToast("ALEX tiene una observación  (Q)", _theme.AlexCyan);
        void OnClaimRefuted(LieData claim) => ShowToast("Contradicción de ALEX detectada", _theme.AccentRed);

        IEnumerator FadeTitle(string title)
        {
            _sceneTitle.text = title;
            _sceneTitle.alpha = 1f;
            yield return new WaitForSeconds(2.2f);
            for (float t = 0; t < 0.8f; t += Time.deltaTime)
            {
                _sceneTitle.alpha = 1f - t / 0.8f;
                yield return null;
            }
            _sceneTitle.alpha = 0f;
        }

        IEnumerator PlayToasts()
        {
            while (_toasts.Count > 0)
            {
                var (text, color) = _toasts.Dequeue();
                _toast.text = text;
                _toast.color = color;
                _toast.alpha = 1f;
                yield return new WaitForSecondsRealtime(2.2f);
                for (float t = 0; t < 0.3f; t += Time.unscaledDeltaTime)
                {
                    _toast.alpha = 1f - t / 0.3f;
                    yield return null;
                }
                _toast.alpha = 0f;
            }
            _toastRoutine = null;
        }
    }
}
