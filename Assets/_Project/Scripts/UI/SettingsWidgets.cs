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
        /// <summary>Texto con todos los controles del juego, en dos columnas (tecla y acción).</summary>
        public static string ControlsText(UITheme theme)
        {
            string Row(string key, string action) =>
                $"<color={UITheme.Hex(theme.WarningAmber)}><b>{key}</b></color><pos=34%>{action}";
            return string.Join("\n",
                Row("WASD / flechas", "Moverse"),
                Row("Mouse", "Mirar"),
                Row("E", "Inspeccionar, hablar, guardar evidencia"),
                Row("Clic izq. + mouse", "Girar el objeto (rueda: acercar)"),
                Row("Tab", "Expediente: analizar evidencias"),
                Row("Q", "ALEX: preguntar, aceptar o cuestionar"),
                Row("H", "Tablero de hipótesis y decisiones"),
                Row("1–6 / Enter", "Respuestas rápidas / escribir en un diálogo"),
                Row("Esc", "Cerrar ventana o pausar"));
        }

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

        /// <summary>Tarjeta oscura con la lista de controles.</summary>
        public static void Controls(Transform parent, UITheme theme)
        {
            UIFactory.Kicker(parent, "Controles", theme, theme.AlexCyan);
            var text = UIFactory.Text(parent, "Controles", ControlsText(theme), theme.BodyFont, theme.SizeS + 1, UITheme.WithAlpha(theme.TextLight, 0.88f));
            text.lineSpacing = 6f;
        }
    }
}
