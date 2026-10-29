using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Random = System.Random;

namespace Caso404.EditorTools
{
    /// <summary>
    /// Sonido ambiente generado por código (no hay archivos de audio en el proyecto): ciudad de día, murmullo del café,
    /// zumbido de fluorescentes, ventiladores de servidores y lluvia de noche. Cada uno es un WAV de 12 s que se repite
    /// sin cortes (el final se funde con el principio). Cada escena recibe un AudioSource 2D en bucle.
    /// </summary>
    public static class AmbientAudioBuilder
    {
        const string Folder = "Assets/_Project/Audio/Ambiente";
        const int Rate = 22050;
        const float Seconds = 12f;

        static readonly string[] Names = { "amb_ciudad_dia", "amb_cafe", "amb_zumbido", "amb_servidores", "amb_lluvia_noche" };

        /// <summary>Clip y volumen de cada escena.</summary>
        static (string clip, float volume) For(string scene) => scene switch
        {
            "Apartamento404" => ("amb_ciudad_dia", 0.35f),
            "CafeLaEsquina" => ("amb_cafe", 0.4f), // de noche; la lluvia se ve por las vitrinas
            "PorteriaSotano" => ("amb_zumbido", 0.3f),
            "Comisaria" => ("amb_zumbido", 0.22f),
            "ServidoresB" => ("amb_servidores", 0.4f),
            "PisoEjecutivo" => ("amb_lluvia_noche", 0.3f),
            "CasetaSeguridad" => ("amb_lluvia_noche", 0.42f),
            _ => (null, 0f),
        };

        [MenuItem("Caso404/Arte/Generar sonidos ambiente")]
        public static void BuildMenu() => Build(true);

        public static void Build(bool regenerate)
        {
            SharedAssetsBuilder.EnsureFolder(Folder);
            foreach (var name in Names)
            {
                var path = $"{Folder}/{name}.wav";
                if (!regenerate && File.Exists(path)) continue;
                WriteWav(path, Generate(name));
                AssetDatabase.ImportAsset(path);
                var imp = (AudioImporter)AssetImporter.GetAtPath(path);
                var s = imp.defaultSampleSettings;
                s.loadType = AudioClipLoadType.CompressedInMemory;
                s.compressionFormat = AudioCompressionFormat.Vorbis;
                s.quality = 0.5f;
                imp.defaultSampleSettings = s;
                imp.SaveAndReimport();
            }
        }

        /// <summary>Agrega el AudioSource del ambiente de la escena bajo <paramref name="root"/>.</summary>
        public static void AddTo(Transform root, string sceneName)
        {
            var (clipName, volume) = For(sceneName);
            if (clipName == null) return;
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{Folder}/{clipName}.wav");
            if (clip == null) { Build(false); clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{Folder}/{clipName}.wav"); }
            var go = new GameObject($"Sonido ambiente ({clipName})");
            go.transform.SetParent(root, false);
            var src = go.AddComponent<AudioSource>();
            src.clip = clip;
            src.loop = true;
            src.playOnAwake = true;
            src.volume = volume;
            src.spatialBlend = 0f;
            src.priority = 200;
        }

        // ================= Síntesis =================

        static float[] Generate(string name)
        {
            int n = (int)(Rate * Seconds), fade = Rate; // 1 s de fundido para el bucle
            var buf = new float[n + fade];
            var rnd = new Random(name.GetHashCode());
            float lp1 = 0, lp2 = 0, lp3 = 0, hp = 0, prev = 0, brown = 0;
            float White() => (float)(rnd.NextDouble() * 2 - 1);

            // Eventos (autos que pasan, voces, tintineos, gotas) como envolventes precalculadas.
            var env = new float[buf.Length];
            var env2 = new float[buf.Length];
            if (name == "amb_ciudad_dia" || name == "amb_lluvia_noche")
                for (int k = 0; k < 6; k++) Swell(env, rnd.Next(0, buf.Length), (int)(Rate * (2.5 + rnd.NextDouble() * 2)), 0.6f + (float)rnd.NextDouble() * 0.6f);
            if (name == "amb_cafe")
            {
                for (int k = 0; k < 90; k++) Swell(env, rnd.Next(0, buf.Length), (int)(Rate * (0.12 + rnd.NextDouble() * 0.3)), 0.4f + (float)rnd.NextDouble() * 0.6f);
                for (int k = 0; k < 7; k++) Clink(buf, rnd.Next(0, buf.Length - Rate), 2400f + (float)rnd.NextDouble() * 1800f, 0.12f);
            }

            for (int i = 0; i < buf.Length; i++)
            {
                float t = i / (float)Rate, w = White(), s = 0f;
                brown = Mathf.Clamp(brown + w * 0.02f, -1f, 1f) * 0.998f;
                lp1 += (w - lp1) * 0.05f;   // ~180 Hz
                lp2 += (w - lp2) * 0.25f;   // ~1 kHz
                lp3 += (w - lp3) * 0.6f;
                hp = 0.92f * (hp + w - prev); prev = w; // pasa altos
                switch (name)
                {
                    case "amb_ciudad_dia":
                        s = brown * 0.5f + lp1 * 0.6f * (0.4f + env[i]) + lp2 * 0.05f * env[i];
                        break;
                    case "amb_cafe":
                        float voice = (lp2 - lp1) * (0.15f + 0.85f * Mathf.Clamp01(env[i])) * (0.8f + 0.2f * Mathf.Sin(t * 2f * Mathf.PI * 3.1f));
                        s = voice * 0.9f + brown * 0.25f + buf[i];
                        break;
                    case "amb_zumbido":
                        s = 0.18f * Mathf.Sin(2f * Mathf.PI * 120f * t) + 0.07f * Mathf.Sin(2f * Mathf.PI * 240f * t) + 0.03f * Mathf.Sin(2f * Mathf.PI * 360f * t)
                            + lp2 * 0.08f + brown * 0.15f;
                        break;
                    case "amb_servidores":
                        s = lp3 * 0.35f + lp1 * 0.4f + 0.12f * Mathf.Sin(2f * Mathf.PI * 60f * t) + 0.015f * Mathf.Sin(2f * Mathf.PI * 7200f * t)
                            + 0.05f * Mathf.Sin(2f * Mathf.PI * 180f * t);
                        break;
                    case "amb_lluvia_noche":
                        float drop = rnd.NextDouble() < 0.0025 ? (float)rnd.NextDouble() * 0.6f : 0f;
                        env2[i] = drop + (i > 0 ? env2[i - 1] * 0.92f : 0f);
                        s = hp * 0.32f + lp2 * 0.12f + env2[i] * hp * 0.8f + lp1 * 0.35f * (0.3f + env[i]) + brown * 0.2f;
                        break;
                }
                buf[i] = s;
            }

            // Bucle sin corte: el último segundo se funde con el primero.
            var outBuf = new float[n];
            for (int i = 0; i < n; i++) outBuf[i] = buf[i];
            for (int i = 0; i < fade; i++)
            {
                float a = i / (float)fade;
                outBuf[i] = buf[i] * a + buf[n + i] * (1f - a);
            }
            float peak = 0.0001f;
            foreach (var v in outBuf) peak = Mathf.Max(peak, Mathf.Abs(v));
            for (int i = 0; i < n; i++) outBuf[i] = outBuf[i] / peak * 0.7f;
            return outBuf;
        }

        static void Swell(float[] env, int start, int length, float amp)
        {
            for (int i = 0; i < length; i++)
            {
                int j = (start + i) % env.Length;
                env[j] += amp * Mathf.Sin(Mathf.PI * i / length);
            }
        }

        static void Clink(float[] buf, int start, float freq, float amp)
        {
            int len = Rate / 4;
            for (int i = 0; i < len && start + i < buf.Length; i++)
            {
                float t = i / (float)Rate;
                buf[start + i] += amp * Mathf.Exp(-t * 25f) * (Mathf.Sin(2f * Mathf.PI * freq * t) + 0.5f * Mathf.Sin(2f * Mathf.PI * freq * 2.7f * t));
            }
        }

        static void WriteWav(string path, float[] samples)
        {
            using var fs = new FileStream(path, FileMode.Create);
            using var w = new BinaryWriter(fs);
            int bytes = samples.Length * 2;
            w.Write(new[] { 'R', 'I', 'F', 'F' }); w.Write(36 + bytes);
            w.Write(new[] { 'W', 'A', 'V', 'E' }); w.Write(new[] { 'f', 'm', 't', ' ' });
            w.Write(16); w.Write((short)1); w.Write((short)1); w.Write(Rate); w.Write(Rate * 2); w.Write((short)2); w.Write((short)16);
            w.Write(new[] { 'd', 'a', 't', 'a' }); w.Write(bytes);
            foreach (var s in samples) w.Write((short)Mathf.Clamp(Mathf.RoundToInt(s * 32767f), -32768, 32767));
        }
    }
}
