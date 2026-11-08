using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Caso404.UI
{
    /// <summary>
    /// Opciones y controles dentro de la partida (se abre desde la pausa). Usa los mismos controles que el menú
    /// principal. Solo cambia preferencias del jugador (GameSettings), nunca el GameState.
    /// </summary>
    public class OptionsView : UIPanel
    {
        /// <summary>Se cerró la ventana (el controlador vuelve a mostrar la pausa).</summary>
        public event Action Closed;

        /// <summary>
        /// Crea la vista bajo el canvas indicado. Se crea inactiva para asignar el tema y el UIManager
        /// antes de que Awake construya la jerarquía.
        /// </summary>
        public static OptionsView Create(Transform canvasRoot, UITheme theme, UIManager ui)
        {
            var go = new GameObject("OptionsView", typeof(RectTransform), typeof(CanvasGroup));
            go.SetActive(false);
            go.transform.SetParent(canvasRoot, false);
            var view = go.AddComponent<OptionsView>();
            view._theme = theme;
            view._ui = ui;
            go.SetActive(true);
            return view;
        }

        protected override void Build()
        {
            var rt = UIFactory.Stretch((RectTransform)transform);
            UIFactory.Backdrop(rt, _theme, 0.9f);

            var card = UIFactory.Card(rt, "Caja", new Color(0.06f, 0.065f, 0.08f, 0.98f), _theme);
            UIFactory.Frame(card.transform, _theme, UITheme.WithAlpha(_theme.TextLight, 0.12f));
            UIFactory.Place(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1240, 0));
            var row = UIFactory.HLayout(card.gameObject, 48, 0, false);
            row.padding = new RectOffset(52, 52, 44, 44);
            row.childAlignment = TextAnchor.UpperLeft;
            card.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Columna izquierda: preferencias.
            var left = UIFactory.Rect("Preferencias", card.transform);
            UIFactory.VLayout(left.gameObject, 16);
            UIFactory.Layout(left.gameObject, preferredWidth: 520);
            UIFactory.Kicker(left, "Configuración", _theme, _theme.AlexCyan);
            var title = UIFactory.Text(left, "Titulo", "Opciones", _theme.TitleFont, _theme.SizeL + 4, _theme.TextLight);
            UIFactory.Layout(title.gameObject, preferredHeight: 50);
            SettingsWidgets.Sensitivity(left, _theme);
            SettingsWidgets.Toggles(left, _theme);
            var back = UIFactory.Button(left, "Volver   Esc", _theme, _theme.Paper, _theme.Ink, Close, 56);
            UIFactory.Layout(back.gameObject, minHeight: 56, preferredHeight: 56);

            // Columna derecha: controles.
            var right = UIFactory.Rect("Controles", card.transform);
            UIFactory.VLayout(right.gameObject, 12);
            UIFactory.Layout(right.gameObject, preferredWidth: 560);
            SettingsWidgets.Controls(right, _theme);
        }

        protected override void OnHidden() => Closed?.Invoke();
    }
}
