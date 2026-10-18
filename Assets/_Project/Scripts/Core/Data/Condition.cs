using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Caso404.Data
{
    /// <summary>Lo que una condición necesita saber del estado de la partida (solo lectura).</summary>
    public interface IGameStateReader
    {
        bool IsEvidenceFound(string evidenceId);
        bool IsEvidenceAnalyzed(string evidenceId);
        bool HasFlag(string flagId);
        bool IsLieExposed(string lieId);
        bool KnowsFact(string factId);
    }

    /// <summary>
    /// Condición reutilizable: todas las listas deben cumplirse a la vez (AND entre listas).
    /// "any" significa al menos uno de la lista. Una lista vacía no exige nada.
    /// Ejemplo: "ventana Y (overol O cámara original)" = allAnalyzed[ventana] + anyAnalyzed[overol, camara].
    /// </summary>
    [Serializable]
    public class Condition
    {
        [SerializeField] private List<EvidenceData> _allEvidenceAnalyzed = new();
        [SerializeField] private List<EvidenceData> _anyEvidenceAnalyzed = new();
        [SerializeField] private List<FlagData> _allFlags = new();
        [SerializeField] private List<FlagData> _anyFlags = new();
        [SerializeField] private List<FlagData> _noneFlags = new();
        [SerializeField] private List<LieData> _exposedLies = new();
        [SerializeField] private List<string> _knownFactIds = new();

        public IReadOnlyList<EvidenceData> AllEvidenceAnalyzed { get => _allEvidenceAnalyzed; internal set => _allEvidenceAnalyzed = new(value); }
        public IReadOnlyList<EvidenceData> AnyEvidenceAnalyzed { get => _anyEvidenceAnalyzed; internal set => _anyEvidenceAnalyzed = new(value); }
        public IReadOnlyList<FlagData> AllFlags { get => _allFlags; internal set => _allFlags = new(value); }
        public IReadOnlyList<FlagData> AnyFlags { get => _anyFlags; internal set => _anyFlags = new(value); }
        public IReadOnlyList<FlagData> NoneFlags { get => _noneFlags; internal set => _noneFlags = new(value); }
        public IReadOnlyList<LieData> ExposedLies { get => _exposedLies; internal set => _exposedLies = new(value); }
        public IReadOnlyList<string> KnownFactIds { get => _knownFactIds; internal set => _knownFactIds = new(value); }

        /// <summary>True si la condición no exige nada.</summary>
        public bool IsEmpty =>
            _allEvidenceAnalyzed.Count == 0 && _anyEvidenceAnalyzed.Count == 0 && _allFlags.Count == 0 &&
            _anyFlags.Count == 0 && _noneFlags.Count == 0 && _exposedLies.Count == 0 && _knownFactIds.Count == 0;

        /// <summary>Evalúa la condición contra el estado actual. Lógica pura y determinista.</summary>
        public bool Evaluate(IGameStateReader state)
        {
            if (state == null) return IsEmpty;
            if (_allEvidenceAnalyzed.Any(e => e != null && !state.IsEvidenceAnalyzed(e.Id))) return false;
            if (_anyEvidenceAnalyzed.Count > 0 && !_anyEvidenceAnalyzed.Any(e => e != null && state.IsEvidenceAnalyzed(e.Id))) return false;
            if (_allFlags.Any(f => f != null && !state.HasFlag(f.Id))) return false;
            if (_anyFlags.Count > 0 && !_anyFlags.Any(f => f != null && state.HasFlag(f.Id))) return false;
            if (_noneFlags.Any(f => f != null && state.HasFlag(f.Id))) return false;
            if (_exposedLies.Any(l => l != null && !state.IsLieExposed(l.Id))) return false;
            if (_knownFactIds.Any(f => !state.KnowsFact(f))) return false;
            return true;
        }

        /// <summary>Texto para el jugador con lo que falta (para opciones bloqueadas). Vacío si se cumple.</summary>
        public string DescribeMissing(IGameStateReader state)
        {
            var parts = new List<string>();
            var missingAll = _allEvidenceAnalyzed.Where(e => e != null && !state.IsEvidenceAnalyzed(e.Id)).Select(e => e.DisplayName).ToList();
            if (missingAll.Count > 0) parts.Add("Analiza: " + string.Join(", ", missingAll));
            if (_anyEvidenceAnalyzed.Count > 0 && !_anyEvidenceAnalyzed.Any(e => e != null && state.IsEvidenceAnalyzed(e.Id)))
                parts.Add("Analiza una de: " + string.Join(" o ", _anyEvidenceAnalyzed.Where(e => e != null).Select(e => e.DisplayName)));
            if (_exposedLies.Any(l => l != null && !state.IsLieExposed(l.Id))) parts.Add("Descubre una mentira");
            if (parts.Count == 0 && !Evaluate(state)) parts.Add("Aún no tienes lo necesario");
            return string.Join(". ", parts);
        }
    }
}
