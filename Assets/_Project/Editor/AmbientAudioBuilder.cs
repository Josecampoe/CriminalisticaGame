using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Caso404.Core;
using UnityEditor;
using UnityEngine;
using Random = System.Random;

namespace Caso404.EditorTools
{
    /// <summary>
    /// Música de fondo generada por código (no hay archivos de audio de terceros): cinco piezas suaves de suspenso, solo
    /// con notas musicales. Cada una tiene un colchón de acordes menores, una nota grave sostenida y un piano tenue
    /// (melodía escasa o arpegio), con reverberación y un filtro que quita los agudos ásperos. Duran 75-90 s, entran y
    /// salen con fundido, y <see cref="MusicPlayer"/> las rota solas en orden aleatorio.
    /// Historia: J16 cambió el ruido ambiente por música; J27 pasó de dos piezas en bucle a cinco que rotan, sin el
    /// timbre metálico de celesta que sonaba raro.
    /// </summary>
    public static class AmbientAudioBuilder
    {
        const string Folder = "Assets/_Project/Audio/Ambiente";
        const int Rate = 22050;

        sealed class Piece
        {
            public string Name;
            public int[][] Chords;     // notas MIDI del colchón
            public int[] Bass;         // nota grave de cada acorde
            public int[] Scale;        // notas permitidas para la melodía (además de las del acorde)
            public float ChordSeconds;
            public float NotesPerChord; // 0 = sin melodía
            public bool Arpeggio;       // arpegio lento del acorde en el piano
            public bool Pulse;          // latido grave muy suave (dos golpes por acorde)
            public int Seed;
        }

        /// <summary>Las piezas: tonalidades menores distintas y texturas distintas para que la rotación no aburra.</summary>
        static readonly Piece[] Pieces =
        {
            new() { Name = "mus_lluvia", ChordSeconds = 10f, NotesPerChord = 3f, Seed = 11,     // La menor, piano escaso
                Chords = new[] { new[] { 57, 60, 64, 69 }, new[] { 53, 57, 60, 64 }, new[] { 50, 53, 57, 60 }, new[] { 52, 57, 59, 64 },
                                 new[] { 57, 60, 64, 67 }, new[] { 53, 57, 60, 64 }, new[] { 50, 53, 57, 62 }, new[] { 52, 56, 59, 64 } },
                Bass = new[] { 45, 41, 38, 40, 45, 41, 38, 40 },
                Scale = new[] { 69, 71, 72, 74, 76, 77, 79, 81 } },
            new() { Name = "mus_expediente", ChordSeconds = 9f, NotesPerChord = 0f, Arpeggio = true, Seed = 22, // Re menor, arpegio
                Chords = new[] { new[] { 50, 53, 57, 62 }, new[] { 46, 50, 53, 57 }, new[] { 55, 58, 62, 65 }, new[] { 49, 52, 57, 61 },
                                 new[] { 50, 53, 57, 60 }, new[] { 46, 50, 53, 58 }, new[] { 55, 58, 62, 67 }, new[] { 49, 52, 55, 57 } },
                Bass = new[] { 38, 34, 43, 33, 38, 34, 43, 33 },
                Scale = new[] { 62, 64, 65, 67, 69, 70, 72, 74 } },
            new() { Name = "mus_pasillos", ChordSeconds = 11f, NotesPerChord = 2f, Seed = 33,   // Mi menor, muy espaciada
                Chords = new[] { new[] { 52, 55, 59, 64 }, new[] { 48, 52, 55, 59 }, new[] { 45, 48, 52, 55 }, new[] { 47, 52, 54, 57 },
                                 new[] { 52, 55, 59, 66 }, new[] { 48, 52, 55, 59 }, new[] { 45, 48, 52, 57 }, new[] { 47, 51, 54, 59 } },
                Bass = new[] { 40, 36, 33, 35, 40, 36, 33, 35 },
                Scale = new[] { 64, 66, 67, 69, 71, 72, 74, 76, 79 } },
            new() { Name = "mus_sospecha", ChordSeconds = 9f, NotesPerChord = 2f, Pulse = true, Seed = 44, // Do menor, latido grave
                Chords = new[] { new[] { 48, 51, 55, 60 }, new[] { 44, 48, 51, 55 }, new[] { 53, 56, 60, 65 }, new[] { 50, 55, 59, 62 },
                                 new[] { 48, 51, 55, 63 }, new[] { 44, 48, 51, 56 }, new[] { 53, 56, 60, 63 }, new[] { 50, 55, 60, 62 } },
                Bass = new[] { 36, 32, 41, 31, 36, 32, 41, 31 },
                Scale = new[] { 60, 62, 63, 65, 67, 68, 70, 72, 75 } },
            new() { Name = "mus_medianoche", ChordSeconds = 10f, NotesPerChord = 2.5f, Arpeggio = true, Seed = 55, // Si menor
                Chords = new[] { new[] { 47, 50, 54, 59 }, new[] { 43, 47, 50, 54 }, new[] { 52, 55, 59, 62 }, new[] { 49, 54, 58, 61 },
                                 new[] { 47, 50, 54, 61 }, new[] { 43, 47, 50, 55 }, new[] { 52, 55, 59, 64 }, new[] { 49, 54, 59, 61 } },
                Bass = new[] { 35, 31, 40, 30, 35, 31, 40, 30 },
                Scale = new[] { 66, 68, 69, 71, 73, 74, 76, 78 } },
        };

        [MenuItem("Caso404/Arte/Generar música de fondo")]
        public static void BuildMenu() => Build(true);

        public static void Build(bool regenerate)
        {
            SharedAssetsBuilder.EnsureFolder(Folder);
            // Las dos piezas en bucle de antes ya no se usan.
            foreach (var old in new[] { "mus_suspenso_caso1", "mus_suspenso_caso2" })
                if (File.Exists($"{Folder}/{old}.wav")) AssetDatabase.DeleteAsset($"{Folder}/{old}.wav");
            foreach (var piece in Pieces)
            {
                var path = $"{Folder}/{piece.Name}.wav";
                if (!regenerate && File.Exists(path)) continue;
                WriteWav(path, Generate(piece));
                AssetDatabase.ImportAsset(path);
                var imp = (AudioImporter)AssetImporter.GetAtPath(path);
                var s = imp.defaultSampleSettings;
                s.loadType = AudioClipLoadType.Streaming; // piezas largas: no se cargan enteras en memoria
                s.compressionFormat = AudioCompressionFormat.Vorbis;
                s.quality = 0.6f;
                imp.defaultSampleSettings = s;
                imp.SaveAndReimport();
            }
        }

        /// <summary>Agrega el reproductor de música (todas las piezas, en rotación) bajo <paramref name="root"/>.</summary>
        public static void AddTo(Transform root, string sceneName)
        {
            var clips = Pieces.Select(p => AssetDatabase.LoadAssetAtPath<AudioClip>($"{Folder}/{p.Name}.wav")).ToArray();
            if (clips.Any(c => c == null))
            {
                Build(false);
                clips = Pieces.Select(p => AssetDatabase.LoadAssetAtPath<AudioClip>($"{Folder}/{p.Name}.wav")).ToArray();
            }
            var go = new GameObject("Música de fondo");
            go.transform.SetParent(root, false);
            var player = go.AddComponent<MusicPlayer>();
            var so = new SerializedObject(player);
            var tracks = so.FindProperty("_tracks");
            var valid = clips.Where(c => c != null).ToArray();
            tracks.arraySize = valid.Length;
            for (int i = 0; i < valid.Length; i++) tracks.GetArrayElementAtIndex(i).objectReferenceValue = valid[i];
            so.FindProperty("_volume").floatValue = sceneName == "MainMenu" ? 0.34f : 0.28f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ================= Composición =================

        static float Freq(int midi) => 440f * Mathf.Pow(2f, (midi - 69) / 12f);

        static float[] Generate(Piece piece)
        {
            const float tail = 6f;
            float length = piece.Chords.Length * piece.ChordSeconds + tail;
            int n = (int)(Rate * length);
            var pad = new float[n];
            var keys = new float[n];
            var rnd = new Random(piece.Seed);
            for (int c = 0; c < piece.Chords.Length; c++)
            {
                float start = c * piece.ChordSeconds;
                foreach (var note in piece.Chords[c]) Pad(pad, start, piece.ChordSeconds, Freq(note), 0.045f);
                Pad(pad, start, piece.ChordSeconds, Freq(piece.Bass[c]), 0.085f, harmonics: 2);
                if (piece.Pulse)
                    for (float t = 0.5f; t < piece.ChordSeconds; t += piece.ChordSeconds / 2f)
                        Piano(keys, start + t, Freq(piece.Bass[c] + 12), 0.07f, 1.4f);
                if (piece.Arpeggio)
                {
                    var tones = piece.Chords[c].Select(x => x + 12).ToArray();
                    int k = 0;
                    for (float t = 0.2f; t < piece.ChordSeconds - 0.5f; t += 0.9f, k++)
                        Piano(keys, start + t, Freq(tones[k % tones.Length]), 0.045f + 0.01f * (float)rnd.NextDouble(), 2.2f);
                }
            }
            if (piece.NotesPerChord > 0f)
                foreach (var (time, midi, amp) in Melody(piece, rnd)) Piano(keys, time, Freq(midi), amp, 2.6f);

            var mix = new float[n];
            for (int i = 0; i < n; i++) mix[i] = pad[i] + keys[i];
            var wet = Reverb(mix);
            var outBuf = new float[n];
            float lp = 0f;
            const float alpha = 0.45f; // filtro paso bajo suave (~3 kHz): sin agudos ásperos
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float fade = Mathf.SmoothStep(0f, 1f, t / 3f) * Mathf.SmoothStep(0f, 1f, (length - t) / tail);
                float x = (pad[i] * 0.8f + keys[i] * 0.7f + wet[i] * 0.5f) * fade;
                lp += (x - lp) * alpha;
                outBuf[i] = lp;
            }
            float peak = 0.0001f;
            foreach (var v in outBuf) peak = Mathf.Max(peak, Mathf.Abs(v));
            for (int i = 0; i < n; i++) outBuf[i] = outBuf[i] / peak * 0.5f;
            return outBuf;
        }

        /// <summary>
        /// Notas de la melodía: pocas por acorde, en una cuadrícula de medio segundo, casi siempre notas del acorde y a
        /// veces una vecina de la escala. Se mueven por pasos cortos para que suene a frase y no a notas al azar.
        /// </summary>
        static List<(float time, int midi, float amp)> Melody(Piece piece, Random rnd)
        {
            var notes = new List<(float, int, float)>();
            int low = piece.Scale.Min(), high = piece.Scale.Max();
            int last = (low + high) / 2;
            int slotsPerChord = Mathf.FloorToInt(piece.ChordSeconds / 0.5f) - 2;
            for (int c = 0; c < piece.Chords.Length; c++)
            {
                var pool = new List<int>();
                foreach (var nn in piece.Chords[c])
                    for (int oct = -24; oct <= 24; oct += 12)
                        if (nn + oct >= low && nn + oct <= high) pool.Add(nn + oct);
                int count = Mathf.Max(1, Mathf.RoundToInt(piece.NotesPerChord + (float)rnd.NextDouble() - 0.5f));
                var slots = new SortedSet<int> { rnd.Next(1, 4) };
                while (slots.Count < count) slots.Add(rnd.Next(2, slotsPerChord));
                foreach (var slot in slots)
                {
                    int target;
                    if (rnd.NextDouble() < 0.8 || pool.Count == 0)
                    {
                        pool.Sort((a, b) => Mathf.Abs(a - last).CompareTo(Mathf.Abs(b - last)));
                        target = pool[Mathf.Min(pool.Count - 1, rnd.Next(0, 3))];
                    }
                    else target = piece.Scale[rnd.Next(piece.Scale.Length)];
                    last = target;
                    notes.Add((c * piece.ChordSeconds + slot * 0.5f, target, 0.075f + (float)rnd.NextDouble() * 0.03f));
                }
            }
            return notes;
        }

        /// <summary>Colchón: dos voces apenas desafinadas con pocos armónicos, entrada y salida lentas.</summary>
        static void Pad(float[] buf, float start, float chordSeconds, float freq, float amp, int harmonics = 3)
        {
            const float attack = 3f, release = 4f;
            float length = chordSeconds + release;
            int s0 = (int)(start * Rate);
            int count = (int)(length * Rate);
            var detune = new[] { 0.999f, 1.001f };
            for (int i = 0; i < count; i++)
            {
                int idx = s0 + i;
                if (idx >= buf.Length) break;
                float t = i / (float)Rate;
                float env = t < attack ? Mathf.SmoothStep(0f, 1f, t / attack)
                    : t < chordSeconds ? 1f
                    : Mathf.SmoothStep(1f, 0f, (t - chordSeconds) / release);
                float s = 0f;
                foreach (var d in detune)
                    for (int h = 1; h <= harmonics; h++)
                        s += (float)Math.Sin(2.0 * Math.PI * freq * d * h * t) / (h * h * h);
                buf[idx] += s * amp * env;
            }
        }

        /// <summary>Nota de piano suave: ataque redondeado, caída lenta y armónicos enteros (sin parciales metálicos).</summary>
        static void Piano(float[] buf, float start, float freq, float amp, float decay)
        {
            int s0 = (int)(start * Rate);
            int count = (int)(Rate * decay * 3.5f);
            for (int i = 0; i < count && s0 + i < buf.Length; i++)
            {
                float t = i / (float)Rate;
                float env = Mathf.SmoothStep(0f, 1f, t / 0.015f) * Mathf.Exp(-t / decay);
                // Últimos 0.3 s: llega a cero sin chasquido.
                float tailEnv = Mathf.Clamp01((count - i) / (Rate * 0.3f));
                double w = 2.0 * Math.PI * freq * t;
                float s = (float)(Math.Sin(w) + 0.3 * Math.Exp(-t / 0.6) * Math.Sin(2 * w) + 0.08 * Math.Exp(-t / 0.3) * Math.Sin(3 * w));
                buf[s0 + i] += s * amp * env * tailEnv;
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
                    store = y * 0.5f + store * 0.5f; // amortigua los agudos de la cola
                    line[p] = input[i] + store * 0.82f;
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
