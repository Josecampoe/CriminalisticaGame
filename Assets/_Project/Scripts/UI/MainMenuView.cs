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
        TextMeshProUGUI _sensValue;
        RectTransform _toggles;
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
            UIFactory.Place(buttons, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -110), new Vector2(440, 330));
            UIFactory.VLayout(buttons.gameObject, 14);
            UIFactory.Button(buttons, "Nueva partida", _theme, _theme.Paper, _theme.Ink, StartNewGame, 64, _theme.SizeM);
            _continue = UIFactory.Ghost(buttons, "Continuar", _theme, _theme.TextLight, ContinueGame, 60, _theme.SizeM - 2);
            UIFactory.Ghost(buttons, "Opciones", _theme, _theme.TextLight, () => _options.gameObject.SetActive(true), 56);
            UIFactory.Ghost(buttons, "Salir", _theme, _theme.Muted, Quit, 56);
            _continue.interactable = _saves.TryLoad(out _);

            BuildOptions(rt);

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
            UIFactory.Place(box, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(800, 548));
            var bv = UIFactory.VLayout(box.gameObject, 16, 0);
            bv.padding = new RectOffset(48, 48, 40, 40);
            UIFactory.Kicker(box, "Configuración", _theme, _theme.AlexCyan);
            var title = UIFactory.Text(box, "Title", "Opciones", _theme.TitleFont, _theme.SizeL + 4, _theme.TextLight);
            UIFactory.Layout(title.gameObject, preferredHeight: 50);

            _sensValue = UIFactory.Text(box, "SensLabel", "", _theme.BodyFont, _theme.SizeBody, _theme.TextLight);
            UIFactory.Layout(_sensValue.gameObject, preferredHeight: 30);
            var sliderGo = DefaultControls.CreateSlider(new DefaultControls.Resources());
            sliderGo.transform.SetParent(box, false);
            UIFactory.Layout(sliderGo, minHeight: 30, preferredHeight: 30);
            foreach (var img in sliderGo.GetComponentsInChildren<Image>()) { img.color = UITheme.WithAlpha(_theme.TextLight, 0.15f); UIFactory.Round(img, _theme, 0.5f); }
            var fill = sliderGo.transform.Find("Fill Area/Fill");
            if (fill != null) fill.GetComponent<Image>().color = _theme.AlexCyan;
            var handle = sliderGo.transform.Find("Handle Slide Area/Handle");
            if (handle != null) handle.GetComponent<Image>().color = _theme.TextLight;
            var slider = sliderGo.GetComponent<Slider>();
            slider.minValue = GameSettings.MinSensitivity;
            slider.maxValue = GameSettings.MaxSensitivity;
            slider.value = GameSettings.Sensitivity;
            slider.onValueChanged.AddListener(v => { GameSettings.Sensitivity = v; RefreshOptions(); });

            _toggles = UIFactory.Rect("Casillas", box);
            UIFactory.VLayout(_toggles.gameObject, 10);
            UIFactory.Layout(_toggles.gameObject, preferredHeight: 3 * 52 + 20);
            UIFactory.Ghost(box, "Volver", _theme, _theme.TextLight, () => _options.gameObject.SetActive(false), 52);
            RefreshOptions();
            _options.gameObject.SetActive(false);
        }

        void RefreshOptions()
        {
            _sensValue.text = $"Sensibilidad del mouse: {GameSettings.Sensitivity / GameSettings.DefaultSensitivity:0.0}x";
            // Las casillas se rehacen para mostrar la marca (✓) del estado actual.
            for (int i = _toggles.childCount - 1; i >= 0; i--) DestroyImmediate(_toggles.GetChild(i).gameObject);
            UIFactory.CheckRow(_toggles, "Invertir eje Y", _theme, GameSettings.InvertY, true, () => { GameSettings.InvertY = !GameSettings.InvertY; RefreshOptions(); }, false);
            UIFactory.CheckRow(_toggles, "Balanceo de cámara al caminar", _theme, GameSettings.HeadBob, true, () => { GameSettings.HeadBob = !GameSettings.HeadBob; RefreshOptions(); }, false);
            UIFactory.CheckRow(_toggles, "Pantalla completa", _theme, GameSettings.Fullscreen, true, () => { GameSettings.Fullscreen = !GameSettings.Fullscreen; RefreshOptions(); }, false);
        }

        /// <summary>Empieza una partida nueva en la primera escena del caso 1.</summary>
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
            Load(first.UnitySceneName);
        }

        /// <summary>Continúa en la escena guardada.</summary>
        public void ContinueGame()
        {
            if (!_saves.TryLoad(out var state)) { StartNewGame(); return; }
            var scene = _database.FindScene(state.currentSceneId) ?? _database.FindCase(_firstCaseId).Scenes[0];
            Load(scene.UnitySceneName);
        }

        void Load(string unityScene)
        {
            if (_fader != null) _fader.FadeOutThen(() => SceneLoader.TryLoad(unityScene));
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
