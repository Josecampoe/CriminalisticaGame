using Caso404.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Caso404.UI
{
    /// <summary>
    /// Controles de Opciones compartidos por el menú principal y la pausa: sensibilidad del mouse, casillas
    /// (eje Y, balanceo, pantalla completa) y la tarjeta de controles. Solo leen y guardan preferencias (GameSettings).
    /// </summary>
    public static class SettingsWidgets
    {
        /// <summary>Controles del juego: teclas (separadas por "|") y acción.</summary>
        public static readonly (string keys, string action)[] Bindings =
        {
            ("W A S D", "Moverse (también flechas)"),
            ("Mouse", "Mirar alrededor"),
            ("E", "Inspeccionar, hablar y guardar evidencia"),
            ("Clic|Rueda", "Girar y acercar el objeto inspeccionado"),
            ("Tab", "Expediente: analizar evidencias"),
            ("Q", "ALEX: preguntar, aceptar o cuestionar"),
            ("H", "Tablero de hipótesis y decisiones"),
            ("1–6|Enter", "Respuestas rápidas o escribir en un diálogo"),
            ("Esc", "Cerrar ventana o pausar"),
        };

        /// <summary>Etiqueta y barra de sensibilidad del mouse. Guarda el valor al moverla.</summary>
        public static void Sensitivity(Transform parent, UITheme theme)
        {
            var label = UIFactory.Text(parent, "SensLabel", "", theme.BodyFont, theme.SizeBody, theme.TextLight);
            UIFactory.Layout(label.gameObject, preferredHeight: 30);
            void Refresh() => label.text = $"Sensibilidad del mouse: {GameSettings.Sensitivity / GameSettings.DefaultSensitivity:0.0}x";

            var sliderGo = DefaultControls.CreateSlider(new DefaultControls.Resources());
            sliderGo.transform.SetParent(parent, false);
            UIFactory.Layout(sliderGo, minHeight: 30, preferredHeight: 30);
            foreach (var img in sliderGo.GetComponentsInChildren<Image>()) { img.color = UITheme.WithAlpha(theme.TextLight, 0.15f); UIFactory.Round(img, theme, 0.5f); }
            var fill = sliderGo.transform.Find("Fill Area/Fill");
            if (fill != null) fill.GetComponent<Image>().color = theme.AlexCyan;
            var handle = sliderGo.transform.Find("Handle Slide Area/Handle");
            if (handle != null) handle.GetComponent<Image>().color = theme.TextLight;
            var slider = sliderGo.GetComponent<Slider>();
            slider.minValue = GameSettings.MinSensitivity;
            slider.maxValue = GameSettings.MaxSensitivity;
            slider.value = GameSettings.Sensitivity;
            slider.onValueChanged.AddListener(v => { GameSettings.Sensitivity = v; Refresh(); });
            Refresh();
        }

        /// <summary>Contenedor con las casillas de preferencias; se rehace al cambiar una para mostrar la marca.</summary>
        public static void Toggles(Transform parent, UITheme theme)
        {
            var holder = UIFactory.Rect("Casillas", parent);
            UIFactory.VLayout(holder.gameObject, 10);
            UIFactory.Layout(holder.gameObject, preferredHeight: 3 * 52 + 20);
            void Refresh()
            {
                for (int i = holder.childCount - 1; i >= 0; i--) Object.DestroyImmediate(holder.GetChild(i).gameObject);
                UIFactory.CheckRow(holder, "Invertir eje Y", theme, GameSettings.InvertY, true, () => { GameSettings.InvertY = !GameSettings.InvertY; Refresh(); }, false);
                UIFactory.CheckRow(holder, "Balanceo de cámara al caminar", theme, GameSettings.HeadBob, true, () => { GameSettings.HeadBob = !GameSettings.HeadBob; Refresh(); }, false);
                UIFactory.CheckRow(holder, "Pantalla completa", theme, GameSettings.Fullscreen, true, () => { GameSettings.Fullscreen = !GameSettings.Fullscreen; Refresh(); }, false);
            }
            Refresh();
        }

        /// <summary>Lista de controles: a la izquierda las teclas como teclas de teclado, a la derecha la acción.</summary>
        public static void Controls(Transform parent, UITheme theme)
        {
            var title = UIFactory.Kicker(parent, "Controles", theme, theme.AlexCyan);
            UIFactory.Layout(title.gameObject, preferredHeight: 30);
            foreach (var (keys, action) in Bindings)
            {
                var row = UIFactory.Rect("Fila", parent);
                var h = UIFactory.HLayout(row.gameObject, 16, 0, false);
                h.childAlignment = TextAnchor.MiddleLeft;
                UIFactory.Layout(row.gameObject, minHeight: 40, preferredHeight: 40);

                var caps = UIFactory.Rect("Teclas", row);
                var ch = UIFactory.HLayout(caps.gameObject, 6, 0, false);
                ch.childAlignment = TextAnchor.MiddleLeft;
                UIFactory.Layout(caps.gameObject, minHeight: 34, preferredHeight: 34, preferredWidth: 180);
                foreach (var key in keys.Split('|')) KeyCap(caps, key, theme);

                var label = UIFactory.Text(row, "Accion", action, theme.BodyFont, theme.SizeS + 1, UITheme.WithAlpha(theme.TextLight, 0.9f), TextAlignmentOptions.MidlineLeft);
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.overflowMode = TextOverflowModes.Ellipsis;
                UIFactory.Layout(label.gameObject, minHeight: 34, preferredHeight: 34, flexibleWidth: 1);
            }
        }

        /// <summary>Tecla dibujada como una tecla de teclado: caja redondeada clara con borde y letra en mono.</summary>
        static void KeyCap(Transform parent, string key, UITheme theme)
        {
            var bg = UIFactory.Round(UIFactory.Panel(parent, "Tecla", UITheme.WithAlpha(theme.TextLight, 0.1f)), theme, 0.8f);
            bg.raycastTarget = false;
            var h = UIFactory.HLayout(bg.gameObject, 0, 0, false);
            h.padding = new RectOffset(12, 12, 4, 4);
            h.childAlignment = TextAnchor.MiddleCenter;
            UIFactory.Layout(bg.gameObject, minHeight: 34, preferredHeight: 34, preferredWidth: key.Length <= 1 ? 40 : -1);
            var t = UIFactory.Text(bg.transform, "Texto", key, theme.MonoFont, theme.SizeS, theme.WarningAmber, TextAlignmentOptions.Center);
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.fontStyle = FontStyles.Bold;
            UIFactory.Frame(bg.transform, theme, UITheme.WithAlpha(theme.TextLight, 0.22f));
        }
    }
}
