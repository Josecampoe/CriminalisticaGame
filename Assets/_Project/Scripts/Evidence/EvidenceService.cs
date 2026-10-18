using System;
using System.Collections.Generic;
using System.Linq;
using Caso404.Core;
using Caso404.Data;

namespace Caso404.Evidence
{
    /// <summary>Resultado de pedir un análisis.</summary>
    public readonly struct AnalysisOutcome
    {
        public readonly bool Success;
        public readonly string Text;
        public AnalysisOutcome(bool success, string text) { Success = success; Text = text; }
    }

    /// <summary>
    /// Recoger, inspeccionar y analizar evidencias. La verdad del análisis sale del EvidenceData;
    /// este servicio solo actualiza el GameState y avisa con eventos.
    /// </summary>
    public class EvidenceService
    {
        public event Action<EvidenceData> EvidenceCollected;
        public event Action<EvidenceData> EvidenceAnalyzed;
        public event Action<EvidenceData> ExtraAnalysisDone;
        public event Action<EvidenceData> HiddenDetailFound;

        readonly GameStateService _state;
        readonly CaseData _case;

        public EvidenceService(GameStateService state, CaseData caseData)
        {
            _state = state;
            _case = caseData;
        }

        public CaseData Case => _case;

        /// <summary>True si la evidencia existe en este momento de la historia (condición de aparición).</summary>
        public bool IsAvailable(EvidenceData ev) => ev != null && ev.SpawnCondition.Evaluate(_state);

        public bool IsFound(EvidenceData ev) => ev != null && _state.IsEvidenceFound(ev.Id);
        public bool IsAnalyzed(EvidenceData ev) => ev != null && _state.IsEvidenceAnalyzed(ev.Id);

        /// <summary>Evidencias encontradas, en el orden en que se recogieron.</summary>
        public IEnumerable<EvidenceData> FoundEvidence =>
            _state.State.foundEvidenceIds.Select(id => _case.FindEvidence(id)).Where(e => e != null);

        /// <summary>Evidencias analizadas (las que se pueden presentar o usar como prueba).</summary>
        public IEnumerable<EvidenceData> AnalyzedEvidence => FoundEvidence.Where(IsAnalyzed);

        /// <summary>Cuántas evidencias encontradas faltan por analizar (para el HUD).</summary>
        public int UnanalyzedCount => FoundEvidence.Count(e => e.CanBeAnalyzed && !IsAnalyzed(e));

        /// <summary>Guarda la evidencia en el expediente. False si no está disponible o ya estaba.</summary>
        public bool TryCollect(EvidenceData ev)
        {
            if (!IsAvailable(ev) || IsFound(ev)) return false;
            _state.MarkFound(ev.Id);
            EvidenceCollected?.Invoke(ev);
            return true;
        }

        /// <summary>El jugador giró el objeto y vio su detalle oculto.</summary>
        public void MarkHiddenDetailSeen(EvidenceData ev)
        {
            if (ev == null || !ev.HasHiddenDetail) return;
            if (_state.MarkHiddenDetailSeen(ev.Id)) HiddenDetailFound?.Invoke(ev);
        }

        /// <summary>Dice si se puede analizar y, si no, por qué (texto para el jugador).</summary>
        public bool CanAnalyze(EvidenceData ev, out string reason)
        {
            reason = "";
            if (ev == null) { reason = "Sin evidencia."; return false; }
            if (!IsFound(ev)) { reason = "Aún no la has encontrado."; return false; }
            if (IsAnalyzed(ev)) { reason = "Ya está analizada."; return false; }
            if (!ev.CanBeAnalyzed) { reason = "No se puede analizar."; return false; }
            if (ev.HasHiddenDetail && !_state.IsHiddenDetailSeen(ev.Id)) { reason = "Revísala mejor en el modo inspección: gírala."; return false; }
            return true;
        }

        /// <summary>Analiza la evidencia: aplica hechos y flags, y desbloquea evidencias que dependan de ella.</summary>
        public AnalysisOutcome Analyze(EvidenceData ev)
        {
            if (!CanAnalyze(ev, out var reason)) return new AnalysisOutcome(false, reason);
            _state.MarkAnalyzed(ev.Id);
            _state.LearnFacts(ev.RevealsFactIds);
            _state.AddFlags(ev.SetsFlagsOnAnalyze);
            EvidenceAnalyzed?.Invoke(ev);
            CollectUnlockedEvidence();
            return new AnalysisOutcome(true, ev.AnalysisResult);
        }

        /// <summary>Análisis adicional (profundo, externo o con contraseña).</summary>
        public AnalysisOutcome RunExtraAnalysis(EvidenceData ev, string password = null)
        {
            if (ev == null || !ev.HasExtraAnalysis) return new AnalysisOutcome(false, "Esta evidencia no admite otro análisis.");
            if (!IsAnalyzed(ev)) return new AnalysisOutcome(false, "Primero haz el análisis normal.");
            if (_state.IsExtraAnalysisDone(ev.Id)) return new AnalysisOutcome(true, ev.ExtraAnalysis.ResultText);
            var extra = ev.ExtraAnalysis;
            if (extra.Kind == ExtraAnalysisKind.Password &&
                !string.Equals((password ?? "").Trim(), extra.Password, StringComparison.OrdinalIgnoreCase))
                return new AnalysisOutcome(false, "Contraseña incorrecta.");
            _state.MarkExtraAnalysisDone(ev.Id);
            _state.AddFlags(extra.SetsFlags);
            ExtraAnalysisDone?.Invoke(ev);
            return new AnalysisOutcome(true, extra.ResultText);
        }

        /// <summary>Entrega una evidencia que no se recoge en la escena (ej. el celular que da Andrés).</summary>
        public bool Grant(EvidenceData ev) => TryCollect(ev);

        void CollectUnlockedEvidence()
        {
            foreach (var e in _case.Evidence)
                if (e != null && e.Source == EvidenceSource.Unlock && !IsFound(e) && IsAvailable(e))
                    TryCollect(e);
        }
    }
}
