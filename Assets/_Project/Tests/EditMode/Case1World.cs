using System.Linq;
using Caso404.AI;
using Caso404.Core;
using Caso404.Data;
using Caso404.Decisions;
using Caso404.Evidence;
using NUnit.Framework;
using UnityEditor;

namespace Caso404.Tests
{
    /// <summary>Arma los servicios del juego sobre los assets reales del caso 1 para simular partidas.</summary>
    public class Case1World
    {
        public const string DatabasePath = "Assets/_Project/Data/GameDatabase.asset";

        public readonly GameDatabase Db;
        public readonly CaseData Case;
        public readonly GameStateService State;
        public readonly EvidenceService Evidence;
        public readonly AlexService Alex;
        public readonly DecisionService Decisions;
        public readonly HypothesisService Hypotheses;
        public readonly AccusationService Accusation;

        public Case1World(string caseId = "c1")
        {
            Db = AssetDatabase.LoadAssetAtPath<GameDatabase>(DatabasePath);
            Assert.IsNotNull(Db, "Falta GameDatabase: ejecuta Caso404/Generar datos caso 1.");
            Case = Db.FindCase(caseId);
            State = new GameStateService(new GameState { alexTrust = Db.Alex.StartTrust });
            Evidence = new EvidenceService(State, Case);
            Alex = new AlexService(State, Case, Db.Alex, Evidence);
            Decisions = new DecisionService(State);
            Hypotheses = new HypothesisService(State);
            Accusation = new AccusationService(State, Case);
        }

        public EvidenceData Ev(string id)
        {
            var e = Case.FindEvidence(id);
            Assert.IsNotNull(e, $"No existe la evidencia {id}");
            return e;
        }

        public LieData Lie(string id)
        {
            var l = Case.FindLie(id);
            Assert.IsNotNull(l, $"No existe la mentira {id}");
            return l;
        }

        /// <summary>Recoge y analiza (girando el objeto si tiene detalle oculto).</summary>
        public void CollectAndAnalyze(params string[] ids)
        {
            foreach (var id in ids)
            {
                var e = Ev(id);
                if (!Evidence.IsFound(e)) Assert.IsTrue(Evidence.TryCollect(e), $"No se pudo recoger {id}");
                if (e.HasHiddenDetail) Evidence.MarkHiddenDetailSeen(e);
                Assert.IsTrue(Evidence.Analyze(e).Success, $"No se pudo analizar {id}");
            }
        }

        public void Decide(string decisionId, string optionId)
        {
            var d = Case.FindDecision(decisionId);
            Assert.IsTrue(Decisions.Choose(d, d.FindOption(optionId)).Applied, $"No se aplicó {decisionId}:{optionId}");
        }

        public void Refute(string claimId, string evidenceId)
        {
            var r = Alex.Question(Lie(claimId), Ev(evidenceId));
            Assert.IsTrue(r.Success, $"{evidenceId} debería refutar {claimId}");
        }

        public EndingResult Accuse(string suspect, params string[] proofs)
        {
            var r = Accusation.Accuse(suspect, proofs.ToList());
            Assert.IsNotNull(r, "Acusación inválida");
            return r;
        }
    }
}
