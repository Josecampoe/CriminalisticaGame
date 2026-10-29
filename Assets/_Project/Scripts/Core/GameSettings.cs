using UnityEngine;

namespace Caso404.Core
{
    /// <summary>
    /// Preferencias del jugador (no son parte de la partida): sensibilidad del mouse, eje Y invertido, balanceo de cámara y pantalla completa.
    /// Se guardan en PlayerPrefs porque pertenecen a la máquina del jugador, no al GameState.
    /// </summary>
    public static class GameSettings
    {
        const string SensitivityKey = "caso404.sensitivity";
        const string InvertYKey = "caso404.invertY";
        const string HeadBobKey = "caso404.headBob";
        public const float DefaultSensitivity = 0.12f;
        public const float MinSensitivity = 0.03f;
        public const float MaxSensitivity = 0.4f;

        /// <summary>Grados por píxel de movimiento del mouse.</summary>
        public static float Sensitivity
        {
            get => Mathf.Clamp(PlayerPrefs.GetFloat(SensitivityKey, DefaultSensitivity), MinSensitivity, MaxSensitivity);
            set { PlayerPrefs.SetFloat(SensitivityKey, Mathf.Clamp(value, MinSensitivity, MaxSensitivity)); PlayerPrefs.Save(); }
        }

        /// <summary>Si es true, mover el mouse hacia arriba mira hacia abajo.</summary>
        public static bool InvertY
        {
            get => PlayerPrefs.GetInt(InvertYKey, 0) == 1;
            set { PlayerPrefs.SetInt(InvertYKey, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        /// <summary>Balanceo suave de la cámara al caminar. Activado por defecto; se puede apagar si marea.</summary>
        public static bool HeadBob
        {
            get => PlayerPrefs.GetInt(HeadBobKey, 1) == 1;
            set { PlayerPrefs.SetInt(HeadBobKey, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        /// <summary>Pantalla completa (Unity la recuerda entre sesiones).</summary>
        public static bool Fullscreen
        {
            get => Screen.fullScreen;
            set => Screen.fullScreen = value;
        }
    }
}
