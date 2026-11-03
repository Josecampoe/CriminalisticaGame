using System;
using System.IO;
using Caso404.Core;
using UnityEngine;

namespace Caso404.AI
{
    /// <summary>
    /// Permite cambiar el servidor de IA sin recompilar: si junto al juego (la carpeta del .exe, o la raíz del
    /// proyecto en el Editor) hay un archivo ia.json, sus valores reemplazan a los de AiSettings.
    /// Ejemplo: { "proxyUrl": "http://127.0.0.1:8790/dialogue" } para usar el proxy local, o { "offline": true }.
    /// Nunca lleva claves: solo la dirección del servidor.
    /// </summary>
    public static class AiRuntimeConfig
    {
        public const string FileName = "ia.json";

        [Serializable]
        class FileData
        {
            public string proxyUrl;
            public bool offline;
        }

        static AiSettings _source;
        static AiSettings _resolved;

        /// <summary>Ruta donde se busca ia.json.</summary>
        public static string FilePath => Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? ".", FileName);

        /// <summary>Devuelve la configuración efectiva: una copia con los cambios de ia.json, o la misma si no hay archivo.</summary>
        public static AiSettings Resolve(AiSettings settings)
        {
            if (settings == null) return null;
            if (settings == _source && _resolved != null) return _resolved;
            _source = settings;
            _resolved = settings;
            var path = FilePath;
            if (!File.Exists(path)) return _resolved;
            try
            {
                var data = JsonUtility.FromJson<FileData>(File.ReadAllText(path));
                if (data == null) return _resolved;
                var copy = UnityEngine.Object.Instantiate(settings);
                if (!string.IsNullOrWhiteSpace(data.proxyUrl)) copy.ProxyUrl = data.proxyUrl.Trim();
                if (data.offline) copy.UseOffline = true;
                GameLog.Info($"IA configurada por {FileName}: {(copy.UseOffline ? "sin conexión" : copy.ProxyUrl)}");
                _resolved = copy;
            }
            catch (Exception e)
            {
                GameLog.Warn($"No se pudo leer {FileName} ({e.Message}); se usa la configuración del juego.");
            }
            return _resolved;
        }
    }
}
