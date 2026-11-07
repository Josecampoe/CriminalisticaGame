using System.Collections;
using System.Threading;
using Caso404.AI;
using Caso404.Core;
using Caso404.Data;
using Caso404.Save;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Caso404.UI
{
    /// <summary>
    /// Menú principal: título con efecto de máquina de escribir, Nueva partida, Continuar (si hay partida guardada) y Salir.
    /// Nueva partida crea un GameState limpio y lo guarda; cada escena lo carga desde el guardado.
    /// </summary>
    public class MainMenuView : MonoBehaviour
    {
        [SerializeField] private UITheme _theme;
        [SerializeField] private GameDatabase _database;
        [SerializeField] private ScreenFader _fader;
        [SerializeField] private string _firstCaseId = "c1";
        [Tooltip("Para mostrar si la IA está conectada. Vacío: no se muestra.")]
        [SerializeField] private AiSettings _aiSettings;

        SaveService _saves;
        TextMeshProUGUI _title;
        Button _continue;
        RectTransform _options;
        RectTransform _confirm;
        TextMeshProUGUI _confirmText;
        TextMeshProUGUI _aiStatus;
        readonly CancellationTokenSource _cts = new();

        void Awake()
        {
            _saves = new SaveService();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Build();
        }

        void Start()
        {
            StartCoroutine(TypeTitle());
            CheckAi();
        }

        void OnDestroy()
        {
            _cts.Cancel();
            _cts.Dispose();
        }

        /// <summary>Pregunta al servidor si la IA está lista (solo lectura) y lo muestra abajo a la izquierda.</summary>
        async void CheckAi()
        {
            if (_aiSettings == null) return;
            SetAiStatus("Comprobando la IA…", _theme.Muted);
            AiConnection status;
            try { status = await AiStatus.CheckAsync(_aiSettings, _cts.Token); }
            catch (System.OperationCanceledException) { return; }
            if (this == null || _aiStatus == null) return;
            switch (status)
            {
                case AiConnection.Online: SetAiStatus("IA conectada: los personajes responden lo que escribas", _theme.AlexCyan); break;
                case AiConnection.Disabled: SetAiStatus("IA desactivada: los personajes usan frases del guion", _theme.Muted); break;
                default: SetAiStatus("Sin conexión con la IA: los personajes usan frases del guion", _theme.WarningAmber); break;
            }
        }

        void SetAiStatus(string text, Color color)
        {
            _aiStatus.text = $"<color={UITheme.Hex(color)}>●</color>  {text}";
            _aiStatus.color = UITheme.WithAlpha(color, 0.85f);
        }

        void Build()
        {
            var rt = UIFactory.Stretch((RectTransform)transform);
            // Semitransparente: detrás se ve Valdora de noche (la arma el paso visual en la escena del menú).
            var bg = UIFactory.Panel(rt, "Bg", UITheme.WithAlpha(_theme.BgDark, 0.62f));
            UIFactory.Stretch(bg.rectTransform);

            if (_theme.VignetteSprite != null)
            {
                var vignette = UIFactory.Panel(rt, "Vineta", new Color(0f, 0f, 0f, 0.95f));
                vignette.sprite = _theme.VignetteSprite;
                UIFactory.Stretch(vignette.rectTransform);
                vignette.raycastTarget = false;
            }

            var kicker = UIFactory.Kicker(rt, "Expediente reservado", _theme, UITheme.WithAlpha(_theme.AccentRed, 0.9f), TextAlignmentOptions.Center);
            UIFactory.Place(kicker.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 345), new Vector2(800, 24));
            _title = UIFactory.Text(rt, "Title", $"EL CASO <color={UITheme.Hex(_theme.AccentRed)}>404</color>", _theme.TitleFont, 132, _theme.TextLight, TextAlignmentOptions.Center);
            _title.characterSpacing = 6f;
            UIFactory.Place(_title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 250), new Vector2(1500, 160));
            var tshadow = _title.gameObject.AddComponent<Shadow>();
            tshadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
            tshadow.effectDistance = new Vector2(3f, -4f);
            var sub = UIFactory.Text(rt, "Subtitle", "Valdora, 2031  ·  Unidad de Criminalística", _theme.MonoFont, _theme.SizeM - 2, _theme.AlexCyan, TextAlignmentOptions.Center);
            sub.characterSpacing = 4f;
            UIFactory.Place(sub.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 150), new Vector2(1400, 40));

            var buttons = UIFactory.Rect("Buttons", rt);
            UIFactory.Place(buttons, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -120), new Vector2(480, 350));
            UIFactory.VLayout(buttons.gameObject, 14);
            UIFactory.Button(buttons, "Nueva partida", _theme, _theme.Paper, _theme.Ink, RequestNewGame, 64, _theme.SizeM);
            _continue = UIFactory.Ghost(buttons, "Continuar", _theme, _theme.TextLight, ContinueGame, 60, _theme.SizeM - 2);
            UIFactory.Ghost(buttons, "Opciones", _theme, _theme.TextLight, () => _options.gameObject.SetActive(true), 56);
            UIFactory.Ghost(buttons, "Salir", _theme, _theme.Muted, Quit, 56);
            bool hasSave = _saves.TryLoad(out var saved);
            _continue.interactable = hasSave;
            var progress = hasSave ? SavedProgress(saved) : "";
            if (progress.Length > 0)
                UIFactory.SetLabel(_continue, $"Continuar\n<size=62%><font=\"{_theme.MonoFont.name}\"><color={UITheme.Hex(_theme.WarningAmber)}>{progress}</color></font></size>");
            if (progress.Length > 0) UIFactory.Layout(_continue.gameObject, minHeight: 74, preferredHeight: 74);

            BuildOptions(rt);
            BuildConfirm(rt);

            _aiStatus = UIFactory.Text(rt, "EstadoIA", "", _theme.MonoFont, _theme.SizeS, _theme.Muted, TextAlignmentOptions.MidlineLeft);
            UIFactory.Place(_aiStatus.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(40, 30), new Vector2(900, 30));
            _aiStatus.raycastTarget = false;

            var hint = UIFactory.Text(rt, "Hint", "Proyecto universitario · Ingeniería de Software · Unity", _theme.BodyFont, _theme.SizeS, _theme.Muted, TextAlignmentOptions.Center);
            UIFactory.Place(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 30), new Vector2(1400, 30));
        }

        void BuildOptions(RectTransform root)
        {
            var bg = UIFactory.Backdrop(root, _theme, 0.92f);
            bg.gameObject.name = "Options";
            _options = bg.rectTransform;
            var boxBg = UIFactory.Card(_options, "Caja", new Color(0.06f, 0.065f, 0.08f, 0.98f), _theme);
            UIFactory.Frame(boxBg.transform, _theme, UITheme.WithAlpha(_theme.TextLight, 0.12f));
            var box = boxBg.rectTransform;
            UIFactory.Place(box, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1240, 0));
            var row = UIFactory.HLayout(box.gameObject, 48, 0, false);
            row.padding = new RectOffset(52, 52, 44, 44);
            row.childAlignment = TextAnchor.UpperLeft;
            box.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var left = UIFactory.Rect("Preferencias", box);
            UIFactory.VLayout(left.gameObject, 16);
            UIFactory.Layout(left.gameObject, preferredWidth: 520);
            UIFactory.Kicker(left, "Configuración", _theme, _theme.AlexCyan);
            var title = UIFactory.Text(left, "Title", "Opciones", _theme.TitleFont, _theme.SizeL + 4, _theme.TextLight);
            UIFactory.Layout(title.gameObject, preferredHeight: 50);
            SettingsWidgets.Sensitivity(left, _theme);
            SettingsWidgets.Toggles(left, _theme);
            UIFactory.Ghost(left, "Volver", _theme, _theme.TextLight, () => _options.gameObject.SetActive(false), 52);

            var right = UIFactory.Rect("Controles", box);
            UIFactory.VLayout(right.gameObject, 12);
            UIFactory.Layout(right.gameObject, preferredWidth: 560);
            SettingsWidgets.Controls(right, _theme);
            _options.gameObject.SetActive(false);
        }

        /// <summary>Ventana de confirmación: "Nueva partida" con una partida guardada la borraría.</summary>
        void BuildConfirm(RectTransform root)
        {
            var bg = UIFactory.Backdrop(root, _theme, 0.92f);
            bg.gameObject.name = "ConfirmarNueva";
            _confirm = bg.rectTransform;
            var card = UIFactory.Card(_confirm, "Caja", new Color(0.06f, 0.065f, 0.08f, 0.98f), _theme);
            UIFactory.Frame(card.transform, _theme, UITheme.WithAlpha(_theme.AccentRed, 0.5f));
            UIFactory.Place(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760, 0));
            var v = UIFactory.VLayout(card.gameObject, 16);
            v.padding = new RectOffset(48, 48, 40, 40);
            card.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            UIFactory.Kicker(card.transform, "Nueva partida", _theme, _theme.AccentRed);
            var title = UIFactory.Text(card.transform, "Titulo", "¿Empezar desde cero?", _theme.TitleFont, _theme.SizeL + 4, _theme.TextLight);
            UIFactory.Layout(title.gameObject, preferredHeight: 50);
            _confirmText = UIFactory.Text(card.transform, "Texto", "", _theme.BodyFont, _theme.SizeBody, UITheme.WithAlpha(_theme.TextLight, 0.85f));
            var buttons = UIFactory.Rect("Botones", card.transform);
            UIFactory.HLayout(buttons.gameObject, 12);
            UIFactory.Layout(buttons.gameObject, minHeight: 56, preferredHeight: 56);
            UIFactory.Button(buttons, "Borrar y empezar", _theme, _theme.AccentRed, _theme.TextLight, () => { _confirm.gameObject.SetActive(false); StartNewGame(); }, 56);
            UIFactory.Ghost(buttons, "Cancelar", _theme, _theme.TextLight, () => _confirm.gameObject.SetActive(false), 56);
            _confirm.gameObject.SetActive(false);
        }

        /// <summary>Texto corto con el caso y la escena de la partida guardada (vacío si no hay).</summary>
        string SavedProgress(GameState state)
        {
            if (state == null || _database == null) return "";
            var c = _database.FindCase(state.currentCaseId);
            var scene = _database.FindScene(state.currentSceneId);
            if (c == null) return "";
            return scene != null ? $"{c.Title} — {scene.DisplayName}" : c.Title;
        }

        /// <summary>Botón "Nueva partida": si hay una partida guardada, pide confirmación antes de borrarla.</summary>
        public void RequestNewGame()
        {
            if (_saves.TryLoad(out var saved) && _confirm != null)
            {
                var progress = SavedProgress(saved);
                _confirmText.text = "Tienes una investigación en curso" + (progress.Length > 0 ? $": <b>{progress}</b>" : "") +
                                    ".\nSi empiezas una nueva, se perderá ese progreso.";
                _confirm.gameObject.SetActive(true);
                return;
            }
            StartNewGame();
        }

        /// <summary>Empieza una partida nueva en la primera escena del caso 1, sin preguntar.</summary>
        public void StartNewGame()
        {
            var c = _database.FindCase(_firstCaseId);
            // Antes del caso 1 se lee la carta de la Comisaria Lozano.
            if (!string.IsNullOrEmpty(c.IntroLetter)) CaseLetterView.Show(transform.parent, _theme, c.IntroLetter, BeginNewGame);
            else BeginNewGame();
        }

        void BeginNewGame()
        {
            var c = _database.FindCase(_firstCaseId);
            var first = c.Scenes[0];
            var state = new GameState
            {
                alexTrust = _database.Alex != null ? _database.Alex.StartTrust : GameState.DefaultAlexTrust,
                currentCaseId = c.Id,
                currentSceneId = first.Id
            };
            _saves.Save(state);
            Load(first.UnitySceneName, first.DisplayName);
        }

        /// <summary>Continúa en la escena guardada.</summary>
        public void ContinueGame()
        {
            if (!_saves.TryLoad(out var state)) { StartNewGame(); return; }
            var scene = _database.FindScene(state.currentSceneId) ?? _database.FindCase(_firstCaseId).Scenes[0];
            Load(scene.UnitySceneName, scene.DisplayName);
        }

        void Load(string unityScene, string displayName)
        {
            if (_fader != null) _fader.FadeOutThen(() => SceneLoader.TryLoad(unityScene), displayName);
            else SceneLoader.TryLoad(unityScene);
        }

        IEnumerator TypeTitle()
        {
            _title.ForceMeshUpdate();
            int total = _title.textInfo.characterCount;
            for (int i = 0; i <= total; i++)
            {
                _title.maxVisibleCharacters = i;
                yield return new WaitForSecondsRealtime(0.09f);
            }
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
