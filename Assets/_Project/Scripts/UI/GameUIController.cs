using System.Linq;
using Caso404.Core;
using Caso404.Data;
using Caso404.Decisions;
using Caso404.Player;
using UnityEngine;

namespace Caso404.UI
{
    /// <summary>
    /// Conecta las pantallas entre sí: atajos (Tab, Q, H), intro de la escena, decisión tras un secreto,
    /// resultado de una decisión y paso a la siguiente escena. No contiene reglas de juego: llama a los servicios.
    /// </summary>
    public class GameUIController : MonoBehaviour
    {
        [SerializeField] private GameContext _game;
        [SerializeField] private InputReader _input;
        [SerializeField] private PlayerContext _player;
        [SerializeField] private UIManager _ui;
        [SerializeField] private UITheme _theme;
        [SerializeField] private HUDView _hud;
        [SerializeField] private JournalView _journal;
        [SerializeField] private AlexPanelView _alex;
        [SerializeField] private HypothesisView _hypothesis;
        [SerializeField] private DecisionView _decision;
        [SerializeField] private DialogueView _dialogue;
        [SerializeField] private MessageView _message;
        [SerializeField] private bool _showIntro = true;

        DecisionData _pendingDecision;

        void OnEnable()
        {
            if (_input != null)
            {
                _input.JournalPressed += OnJournal;
                _input.AlexPressed += OnAlex;
                _input.HypothesisPressed += OnHypothesis;
            }
            if (_player != null) _player.SceneCloseRequested += OpenHypothesis;
            if (_hypothesis != null) _hypothesis.DecisionRequested += ShowDecision;
            if (_decision != null) _decision.Resolved += OnDecisionResolved;
            if (_dialogue != null) _dialogue.Closed += OnDialogueClosed;
            if (_game != null && _game.Dialogue != null) _game.Dialogue.SecretRevealed += OnSecretRevealed;
        }

        void OnDisable()
        {
            if (_input != null)
            {
                _input.JournalPressed -= OnJournal;
                _input.AlexPressed -= OnAlex;
                _input.HypothesisPressed -= OnHypothesis;
            }
            if (_player != null) _player.SceneCloseRequested -= OpenHypothesis;
            if (_hypothesis != null) _hypothesis.DecisionRequested -= ShowDecision;
            if (_decision != null) _decision.Resolved -= OnDecisionResolved;
            if (_dialogue != null) _dialogue.Closed -= OnDialogueClosed;
            if (_game != null && _game.Dialogue != null) _game.Dialogue.SecretRevealed -= OnSecretRevealed;
        }

        void Start()
        {
            var scene = _game.Scene;
            if (_showIntro && !string.IsNullOrEmpty(scene.IntroText) && _game.State.State.foundEvidenceIds.Count == 0)
            {
                var body = _game.Database.IntroText + "\n\n" + scene.IntroText;
                _message.Show($"Caso 1 — {_game.Case.Title}", body, _game.Case.AlexIntroLine, false,
                    ("Empezar", () => _hud.ShowSceneTitle(scene.DisplayName)));
            }
            else
            {
                _hud.ShowSceneTitle(scene.DisplayName);
            }
        }

        // ---------- Atajos: solo funcionan si no hay otro panel encima (por ejemplo, mientras se escribe) ----------
        void OnJournal() => Toggle(_journal);
        void OnAlex() => Toggle(_alex);
        void OnHypothesis() => Toggle(_hypothesis);

        void Toggle(UIPanel panel)
        {
            if (panel == null) return;
            if (_ui.Top == panel) { panel.Close(); return; }
            if (_ui.Top != null) return;
            panel.Open();
        }

        void OpenHypothesis()
        {
            if (_ui.Top == null) _hypothesis.Open();
        }

        // ---------- Decisiones ----------
        void ShowDecision(DecisionData decision) => _decision.Show(decision);

        void OnSecretRevealed(CharacterProfile who, SecretEntry secret)
        {
            if (secret.TriggersDecision != null) _pendingDecision = secret.TriggersDecision;
        }

        void OnDialogueClosed()
        {
            if (_pendingDecision == null) return;
            var d = _pendingDecision;
            _pendingDecision = null;
            _decision.Show(d);
        }

        void OnDecisionResolved(DecisionOutcome outcome)
        {
            var option = outcome.Option;
            bool changesScene = !string.IsNullOrEmpty(outcome.NextSceneId) || outcome.LetPlayerChooseNextScene;
            if (!string.IsNullOrEmpty(option.ResultText))
            {
                string alexLine = option.ResultSpokenByAlex ? option.ResultText : null;
                string body = option.ResultSpokenByAlex ? "" : option.ResultText;
                _message.Show(option.Text, body, alexLine, false, ("Continuar", () => AfterDecision(outcome, changesScene)));
            }
            else
            {
                AfterDecision(outcome, changesScene);
            }
        }

        void AfterDecision(DecisionOutcome outcome, bool changesScene)
        {
            if (!changesScene) return;
            _game.SceneFlow.CompleteCurrentScene();
            if (outcome.LetPlayerChooseNextScene)
            {
                var choices = _game.SceneFlow.AvailableNextScenes()
                    .Select(s => (s.DisplayName, (System.Action)(() => GoTo(s.Id)))).ToArray();
                _message.Show("¿A dónde vamos?", "Elige la siguiente escena de la investigación.", null, false, choices);
                return;
            }
            GoTo(outcome.NextSceneId);
        }

        void GoTo(string sceneId)
        {
            var next = _game.Case.FindScene(sceneId);
            _game.SaveGame(); // se guarda al cambiar de escena
            if (next != null && SceneLoader.TryLoad(next.UnitySceneName)) return;
            string name = next != null ? next.DisplayName : sceneId;
            _message.Show("Fin de la demo",
                $"Completaste «{_game.Scene.DisplayName}». La siguiente escena, «{name}», todavía no está construida.\n\nLa partida quedó guardada.",
                null, false,
                ("Reiniciar escena", SceneLoader.ReloadCurrent),
                ("Salir", Quit));
        }

        static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
