using System;
using System.IO;
using Caso404.Core;
using UnityEngine;

namespace Caso404.Save
{
    /// <summary>
    /// Guarda y carga el GameState en JSON (JsonUtility) en Application.persistentDataPath/save.json.
    /// La ruta se puede cambiar (las pruebas usan una carpeta temporal).
    /// </summary>
    public class SaveService
    {
        public const string DefaultFileName = "save.json";

        readonly string _path;

        public SaveService(string path = null) =>
            _path = string.IsNullOrEmpty(path) ? Path.Combine(Application.persistentDataPath, DefaultFileName) : path;

        public string FilePath => _path;
        public bool HasSave => File.Exists(_path);

        /// <summary>Escribe el estado en disco. Primero en un archivo temporal para no corromper la partida si falla.</summary>
        public bool Save(GameState state)
        {
            try
            {
                state.saveVersion = GameState.CurrentSaveVersion;
                var dir = Path.GetDirectoryName(_path);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                var tmp = _path + ".tmp";
                File.WriteAllText(tmp, JsonUtility.ToJson(state, true));
                if (File.Exists(_path)) File.Delete(_path);
                File.Move(tmp, _path);
                return true;
            }
            catch (Exception e)
            {
                GameLog.Error($"No se pudo guardar la partida: {e.Message}");
                return false;
            }
        }

        /// <summary>Lee la partida. False si no existe, está dañada o es de una versión más nueva.</summary>
        public bool TryLoad(out GameState state)
        {
            state = null;
            if (!HasSave) return false;
            try
            {
                state = JsonUtility.FromJson<GameState>(File.ReadAllText(_path));
                if (state == null || state.saveVersion > GameState.CurrentSaveVersion) { state = null; return false; }
                Migrate(state);
                return true;
            }
            catch (Exception e)
            {
                GameLog.Error($"No se pudo cargar la partida: {e.Message}");
                state = null;
                return false;
            }
        }

        /// <summary>Borra la partida guardada.</summary>
        public void Delete()
        {
            if (HasSave) File.Delete(_path);
        }

        /// <summary>Punto para migrar partidas de versiones anteriores (hoy solo existe la versión 1).</summary>
        static void Migrate(GameState state) => state.saveVersion = GameState.CurrentSaveVersion;
    }
}
