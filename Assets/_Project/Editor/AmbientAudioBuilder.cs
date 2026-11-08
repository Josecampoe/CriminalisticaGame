using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Random = System.Random;

namespace Caso404.EditorTools
{
    /// <summary>
    /// Música de fondo generada por código (no hay archivos de audio de terceros): dos piezas suaves de suspenso hechas
    /// solo con notas musicales, sin ruido. Cada una tiene un colchón de acordes menores, una nota grave sostenida y una
    /// melodía escasa de timbre tipo celesta, todo con reverberación. Duran 64 s y se repiten sin corte: se sintetizan
    /// dos vueltas seguidas y se guarda la segunda, que ya trae las colas de la primera.
    /// Antes había ruido ambiente (ciudad, lluvia, zumbido); se cambió porque el ruido de fondo molestaba (decisión J16).
    /// </summary>
    public static class AmbientAudioBuilder
    {
        const string Folder = "Assets/_Project/Audio/Ambiente";
        const int Rate = 22050;
        const float Seconds = 64f;
        const float ChordSeconds = 8f;

        /// <summary>Caso 1 y menú: La menor. Caso 2: Re menor, más grave y con un acorde napolitano inquietante.</summary>
        static readonly string[] Names = { "mus_suspenso_caso1", "mus_suspenso_caso2" };

        /// <summary>Pieza y volumen de cada escena.</summary>
        static (string clip, float volume) For(string scene) => scene switch
        {
            "MainMenu" => ("mus_suspenso_caso1", 0.4f),
            "Apartamento404" or "CafeLaEsquina" or "PorteriaSotano" or "Comisaria" => ("mus_suspenso_caso1", 0.32f),
            "ServidoresB" or "PisoEjecutivo" or "CasetaSeguridad" => ("mus_suspenso_caso2", 0.32f),
            _ => (null, 0f),
        };

        [MenuItem("Caso404/Arte/Generar música de fondo")]
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
                s.quality = 0.6f;
                imp.defaultSampleSettings = s;
                imp.SaveAndReimport();
            }
        }

        /// <summary>Agrega el AudioSource de la música de la escena bajo <paramref name="root"/>.</summary>
        public static void AddTo(Transform root, string sceneName)
        {
            var (clipName, volume) = For(sceneName);
            if (clipName == null) return;
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{Folder}/{clipName}.wav");
            if (clip == null) { Build(false); clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{Folder}/{clipName}.wav"); }
            var go = new GameObject($"Música de fondo ({clipName})");
            go.transform.SetParent(root, false);
            var src = go.AddComponent<AudioSource>();
            src.clip = clip;
            src.loop = true;
            src.playOnAwake = true;
            src.volume = volume;
            src.spatialBlend = 0f;
            src.priority = 0; // la música nunca se corta por falta de voces
        }

        // ================= Composición =================

        sealed class Piece
        {
            public int[][] Chords;     // notas MIDI del colchón, un acorde cada 8 s
            public int[] Bass;         // nota grave de cada acorde
            public int[] Scale;        // notas permitidas para la melodía (además de las del acorde)
            public int MelodyLow, MelodyHigh;
            public float NotesPerChord;
            public int Seed;
        }

        static Piece PieceFor(string name) => name == "mus_suspenso_caso2"
            ? new Piece
            {
                // Dm – B♭maj7 – Gm7 – A7 – Dm – E♭ (napolitano) – Gm – A
                Chords = new[]
                {
                    new[] { 50, 53, 57, 60 }, new[] { 46, 50, 53, 57 }, new[] { 55, 58, 62, 65 }, new[] { 49, 52, 55, 57 },
                    new[] { 50, 53, 57, 62 }, new[] { 51, 55, 58, 62 }, new[] { 55, 58, 62, 67 }, new[] { 49, 52, 57, 61 },
                },
                Bass = new[] { 38, 34, 43, 33, 38, 39, 43, 33 },
                Scale = new[] { 62, 64, 65, 67, 69, 70, 72, 74, 76, 77 },
                MelodyLow = 62, MelodyHigh = 79, NotesPerChord = 3f, Seed = 404,
            }
            : new Piece
            {
                // Am – Fmaj7 – Dm7 – E7 – Am – Cmaj7 – Dm(add9) – Esus4
                Chords = new[]
                {
                    new[] { 57, 60, 64, 69 }, new[] { 53, 57, 60, 64 }, new[] { 50, 53, 57, 60 }, new[] { 52, 56, 59, 62 },
                    new[] { 57, 60, 64, 67 }, new[] { 48, 52, 55, 59 }, new[] { 50, 53, 57, 64 }, new[] { 52, 57, 59, 64 },
                },
                Bass = new[] { 45, 41, 38, 40, 45, 36, 38, 40 },
                Scale = new[] { 69, 71, 72, 74, 76, 77, 79, 81, 84 },
                MelodyLow = 69, MelodyHigh = 86, NotesPerChord = 4f, Seed = 2026,
            };

        static float Freq(int midi) => 440f * Mathf.Pow(2f, (midi - 69) / 12f);

        static float[] Generate(string name)
        {
            var piece = PieceFor(name);
            int loop = (int)(Rate * Seconds);
            var dry = new float[loop * 2];
            var bell = new float[loop * 2];
            var melody = Melody(piece);
            for (int pass = 0; pass < 2; pass++)
            {
                int offset = pass * loop;
                for (int c = 0; c < piece.Chords.Length; c++)
                {
                    float start = c * ChordSeconds;
                    foreach (var note in piece.Chords[c]) Pad(dry, offset, start, Freq(note), 0.05f);
                    Pad(dry, offset, start, Freq(piece.Bass[c]), 0.09f, harmonics: 3);
                }
                foreach (var (time, midi, amp) in melody) Celesta(bell, offset + (int)(time * Rate), Freq(midi), amp);
            }

            // Melodía con más eco que el colchón; todo pasa por la misma reverberación.
            var mix = new float[dry.Length];
            for (int i = 0; i < mix.Length; i++) mix[i] = dry[i] + bell[i];
            var wet = Reverb(mix);
            var outBuf = new float[loop];
            for (int i = 0; i < loop; i++)
            {
                int j = loop + i;
                // Respiración muy lenta del volumen (un ciclo cada 16 s; 64 s es múltiplo, así el bucle cierra).
                float breathe = 0.9f + 0.1f * Mathf.Sin(2f * Mathf.PI * i / (Rate * 16f));
                outBuf[i] = (dry[j] * 0.75f + bell[j] * 0.6f + wet[j] * 0.55f) * breathe;
            }
            float peak = 0.0001f;
            foreach (var v in outBuf) peak = Mathf.Max(peak, Mathf.Abs(v));
            for (int i = 0; i < loop; i++) outBuf[i] = outBuf[i] / peak * 0.6f;
            return outBuf;
        }

        /// <summary>
        /// Notas de la melodía: pocas por acorde, en una cuadrícula de medio segundo, casi siempre notas del acorde y a
        /// veces una vecina de la escala. Se mueven por pasos cortos para que suene a frase y no a notas al azar.
        /// </summary>
        static List<(float time, int midi, float amp)> Melody(Piece piece)
        {
            var rnd = new Random(piece.Seed);
            var notes = new List<(float, int, float)>();
            int last = (piece.MelodyLow + piece.MelodyHigh) / 2;
            for (int c = 0; c < piece.Chords.Length; c++)
            {
                var pool = new List<int>();
                foreach (var n in piece.Chords[c])
                    for (int oct = -24; oct <= 24; oct += 12)
                        if (n + oct >= piece.MelodyLow && n + oct <= piece.MelodyHigh) pool.Add(n + oct);
                int count = Mathf.RoundToInt(piece.NotesPerChord + (float)rnd.NextDouble() * 1.5f - 0.75f);
                var slots = new SortedSet<int>();
                slots.Add(rnd.Next(0, 3)); // la primera nota cae cerca del cambio de acorde
                while (slots.Count < count) slots.Add(rnd.Next(1, 15));
                foreach (var slot in slots)
                {
                    int target;
                    if (rnd.NextDouble() < 0.78 || pool.Count == 0)
                    {
                        // Nota del acorde más cercana a la anterior (con algo de variación).
                        pool.Sort((a, b) => Mathf.Abs(a - last).CompareTo(Mathf.Abs(b - last)));
                        target = pool[Mathf.Min(pool.Count - 1, rnd.Next(0, 3))];
                    }
                    else target = piece.Scale[rnd.Next(piece.Scale.Length)];
                    last = target;
                    float amp = 0.11f + (float)rnd.NextDouble() * 0.05f;
                    notes.Add((c * ChordSeconds + slot * 0.5f, target, amp));
                }
            }
            return notes;
        }

        /// <summary>Colchón: dos voces apenas desafinadas con pocos armónicos, entrada y salida lentas de 3 s.</summary>
        static void Pad(float[] buf, int offset, float start, float freq, float amp, int harmonics = 5)
        {
            const float attack = 3f, release = 3.5f;
            float length = ChordSeconds + release;
            int s0 = offset + (int)(start * Rate);
            int count = (int)(length * Rate);
            var detune = new[] { 0.9985f, 1.0015f };
            for (int i = 0; i < count; i++)
            {
                int idx = s0 + i;
                if (idx >= buf.Length) break;
                float t = i / (float)Rate;
                float env = t < attack ? Mathf.SmoothStep(0f, 1f, t / attack)
                    : t < ChordSeconds ? 1f
                    : Mathf.SmoothStep(1f, 0f, (t - ChordSeconds) / release);
                float s = 0f;
                foreach (var d in detune)
                    for (int h = 1; h <= harmonics; h++)
                        s += (float)Math.Sin(2.0 * Math.PI * freq * d * h * t) / Mathf.Pow(h, 1.8f);
                buf[idx] += s * amp * env;
            }
        }

        /// <summary>Nota tipo celesta o caja de música: ataque corto y caída lenta, con un armónico brillante que se apaga rápido.</summary>
        static void Celesta(float[] buf, int start, float freq, float amp)
        {
            int count = (int)(Rate * 4.5f);
            for (int i = 0; i < count && start + i < buf.Length; i++)
            {
                float t = i / (float)Rate;
                float env = Mathf.Min(1f, t / 0.006f) * Mathf.Exp(-t / 1.1f);
                double w = 2.0 * Math.PI * freq * t;
                float s = (float)(Math.Sin(w) + 0.35 * Math.Exp(-t / 0.25) * Math.Sin(2 * w) + 0.12 * Math.Exp(-t / 0.08) * Math.Sin(4.2 * w));
                buf[start + i] += s * amp * env;
            }
        }

        /// <summary>Reverberación de Schroeder: cuatro filtros peine con amortiguación y dos pasa-todo.</summary>
        static float[] Reverb(float[] input)
        {
            int[] combs = { 1116, 1188, 1277, 1356 };
            int[] allpasses = { 225, 341 };
            var outBuf = new float[input.Length];
            foreach (var len in combs)
            {
                var line = new float[len];
                int p = 0;
                float store = 0f;
                for (int i = 0; i < input.Length; i++)
                {
                    float y = line[p];
                    store = y * 0.6f + store * 0.4f; // amortigua los agudos de la cola
                    line[p] = input[i] + store * 0.86f;
                    p = (p + 1) % len;
                    outBuf[i] += y * 0.25f;
                }
            }
            foreach (var len in allpasses)
            {
                var line = new float[len];
                int p = 0;
                for (int i = 0; i < outBuf.Length; i++)
                {
                    float b = line[p];
                    float x = outBuf[i];
                    line[p] = x + b * 0.5f;
                    outBuf[i] = b - x * 0.5f;
                    p = (p + 1) % len;
                }
            }
            return outBuf;
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
