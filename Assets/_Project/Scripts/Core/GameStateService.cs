using System;
using System.Collections.Generic;
using System.Linq;
using Caso404.Data;
using UnityEngine;

namespace Caso404.Core
{
    /// <summary>
    /// Único punto que modifica el <see cref="GameState"/>. Avisa de cada cambio con eventos
    /// para que la UI y los demás sistemas reaccionen sin referencias cruzadas.
    /// Clase C# pura (sin MonoBehaviour) para poder probarla en EditMode.
    /// </summary>
    public class GameStateService : IGameStateReader
    {
        public event Action Changed;
        public event Action<int> TrustChanged;
        public event Action<string> FlagAdded;
        public event Action<string> FactLearned;
        public event Action<string> LieExposed;
        public event Action<string, string> ContradictionAdded; // caseId, lieId

        public GameState State { get; private set; }

        public GameStateService(GameState state = null) => State = state ?? new GameState();

        /// <summary>Reemplaza el estado (al cargar una partida o empezar una nueva).</summary>
        public void Load(GameState state)
        {
            State = state ?? new GameState();
            Changed?.Invoke();
            TrustChanged?.Invoke(State.alexTrust);
        }

        // ---------- Lectura (IGameStateReader) ----------
        public bool IsEvidenceFound(string id) => State.foundEvidenceIds.Contains(id);
        public bool IsEvidenceAnalyzed(string id) => State.analyzedEvidenceIds.Contains(id);
        public bool HasFlag(string id) => State.flags.Contains(id);
        public bool IsLieExposed(string id) => State.exposedLieIds.Contains(id);
        public bool KnowsFact(string id) => State.knownFactIds.Contains(id);
        public bool IsHiddenDetailSeen(string evidenceId) => State.inspectedDetailEvidenceIds.Contains(evidenceId);
        public bool IsExtraAnalysisDone(string evidenceId) => State.extraAnalysisDoneIds.Contains(evidenceId);
        public bool IsAlexClaimRaised(string lieId) => State.raisedAlexClaimIds.Contains(lieId);
        public bool IsAlexClaimAccepted(string lieId) => State.acceptedAlexClaimIds.Contains(lieId);
        public bool IsSceneCompleted(string sceneId) => State.completedSceneIds.Contains(sceneId);
        public int AlexTrust => State.alexTrust;

        /// <summary>Número de contradicciones de ALEX detectadas en un caso.</summary>
        public int GetContradictionCount(string caseId) => FindContradictions(caseId)?.lieIds.Count ?? 0;

        /// <summary>True si la mentira de ALEX ya fue refutada en ese caso.</summary>
        public bool IsContradictionDetected(string caseId, string lieId) => FindContradictions(caseId)?.lieIds.Contains(lieId) ?? false;

        /// <summary>Opción elegida en una decisión, o null si no se ha tomado.</summary>
        public string GetDecisionOption(string decisionId)
        {
            var prefix = decisionId + ":";
            var entry = State.decisionsTaken.FirstOrDefault(d => d.StartsWith(prefix, StringComparison.Ordinal));
            return entry?.Substring(prefix.Length);
        }

        public HypothesisAnswer GetHypothesisAnswer(string questionId) => State.hypothesisAnswers.FirstOrDefault(a => a.questionId == questionId);

        // ---------- Escritura ----------
        public void SetCurrentScene(string caseId, string sceneId)
        {
            State.currentCaseId = caseId;
            State.currentSceneId = sceneId;
            Changed?.Invoke();
        }

        public void CompleteScene(string sceneId) { if (AddUnique(State.completedSceneIds, sceneId)) Changed?.Invoke(); }

        public bool MarkFound(string evidenceId) => AddAndNotify(State.foundEvidenceIds, evidenceId);
        public bool MarkAnalyzed(string evidenceId) => AddAndNotify(State.analyzedEvidenceIds, evidenceId);
        public bool MarkHiddenDetailSeen(string evidenceId) => AddAndNotify(State.inspectedDetailEvidenceIds, evidenceId);
        public bool MarkExtraAnalysisDone(string evidenceId) => AddAndNotify(State.extraAnalysisDoneIds, evidenceId);
        public bool MarkAlexClaimRaised(string lieId) => AddAndNotify(State.raisedAlexClaimIds, lieId);
        public bool MarkAlexClaimAccepted(string lieId) => AddAndNotify(State.acceptedAlexClaimIds, lieId);

        public void AddFlag(string flagId)
        {
            if (string.IsNullOrEmpty(flagId) || !AddUnique(State.flags, flagId)) return;
            FlagAdded?.Invoke(flagId);
            Changed?.Invoke();
        }

        public void AddFlags(IEnumerable<FlagData> flags)
        {
            if (flags == null) return;
            foreach (var f in flags) if (f != null) AddFlag(f.Id);
        }

        public void LearnFact(string factId)
        {
            if (string.IsNullOrEmpty(factId) || !AddUnique(State.knownFactIds, factId)) return;
            FactLearned?.Invoke(factId);
            Changed?.Invoke();
        }

        public void LearnFacts(IEnumerable<string> factIds)
        {
            if (factIds == null) return;
            foreach (var f in factIds) LearnFact(f);
        }

        public bool ExposeLie(string lieId)
        {
            if (!AddUnique(State.exposedLieIds, lieId)) return false;
            LieExposed?.Invoke(lieId);
            Changed?.Invoke();
            return true;
        }

        /// <summary>Registra una contradicción de ALEX en el caso. Devuelve false si ya estaba.</summary>
        public bool AddContradiction(string caseId, string lieId)
        {
            var entry = FindContradictions(caseId);
            if (entry == null)
            {
                entry = new CaseContradictions { caseId = caseId };
                State.alexContradictions.Add(entry);
            }
            if (!AddUnique(entry.lieIds, lieId)) return false;
            ContradictionAdded?.Invoke(caseId, lieId);
            Changed?.Invoke();
            return true;
        }

        /// <summary>Cambia alexTrust y lo mantiene entre 0 y 100.</summary>
        public void ChangeTrust(int delta)
        {
            if (delta == 0) return;
            State.alexTrust = Mathf.Clamp(State.alexTrust + delta, 0, 100);
            TrustChanged?.Invoke(State.alexTrust);
            Changed?.Invoke();
        }

        public void RecordDecision(string decisionId, string optionId)
        {
            if (GetDecisionOption(decisionId) != null) return;
            State.decisionsTaken.Add($"{decisionId}:{optionId}");
            Changed?.Invoke();
        }

        public void SetHypothesisAnswer(string questionId, IEnumerable<string> optionIds, bool correct)
        {
            var answer = GetHypothesisAnswer(questionId);
            if (answer == null)
            {
                answer = new HypothesisAnswer { questionId = questionId };
                State.hypothesisAnswers.Add(answer);
            }
            answer.optionIds = optionIds.ToList();
            answer.correct = correct;
            Changed?.Invoke();
        }

        public void RecordAccusation(AccusationRecord record)
        {
            State.accusations.RemoveAll(a => a.caseId == record.caseId);
            State.accusations.Add(record);
            Changed?.Invoke();
        }

        // ---------- Internos ----------
        CaseContradictions FindContradictions(string caseId) => State.alexContradictions.FirstOrDefault(c => c.caseId == caseId);

        static bool AddUnique(List<string> list, string value)
        {
            if (string.IsNullOrEmpty(value) || list.Contains(value)) return false;
            list.Add(value);
            return true;
        }

        bool AddAndNotify(List<string> list, string value)
        {
            if (!AddUnique(list, value)) return false;
            Changed?.Invoke();
            return true;
        }
    }
}
