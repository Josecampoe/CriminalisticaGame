using System.Linq;
using NUnit.Framework;

namespace Caso404.Tests
{
    /// <summary>Diseño justo (GUION §5): toda mentira se puede refutar con algo alcanzable, y los IDs son coherentes.</summary>
    public class Case1DataValidationTests
    {
        [Test]
        public void EveryLieHasAReachableRefutation()
        {
            var w = new Case1World();
            foreach (var lie in w.Case.Lies)
            {
                Assert.IsTrue(lie.RefutedByEvidence.Count > 0 || lie.RefutedByFactIds.Count > 0, $"{lie.Id} no tiene refutación");
                foreach (var ev in lie.RefutedByEvidence)
                    Assert.IsNotNull(w.Case.FindEvidence(ev.Id), $"{lie.Id}: la evidencia {ev.Id} no está en el caso");
            }
        }

        [Test]
        public void TheTablesOfTheScriptAreRespected()
        {
            var w = new Case1World();
            Assert.AreEqual(19, w.Case.Evidence.Count);
            Assert.AreEqual(11, w.Case.Lies.Count);
            Assert.AreEqual(5, w.Case.AlexClaims.Count(), "Cada caso tiene 5 mentiras de ALEX");
            Assert.AreEqual("andres", w.Case.Culprit.Id);
            Assert.AreEqual("carlos", w.Case.AlexTarget.Id);
            CollectionAssert.AreEquivalent(new[] { "ev_c1_llavero", "ev_c1_overol", "ev_c1_camara_original", "ev_c1_celular_andres" },
                w.Case.SolidProofs.Select(e => e.Id));
            Assert.AreEqual(7, w.Case.FindScene("c1_s1").Evidence.Count, "c1_s1 tiene 7 evidencias");
        }

        [Test]
        public void IdsAreUniqueAndFollowTheConvention()
        {
            var w = new Case1World();
            var evIds = w.Case.Evidence.Select(e => e.Id).ToList();
            CollectionAssert.AllItemsAreUnique(evIds);
            Assert.IsTrue(evIds.All(id => id.StartsWith("ev_c1_")));
            Assert.IsTrue(w.Case.Lies.All(l => l.Id.StartsWith("lie_c1_")));
            Assert.IsTrue(w.Case.Flags.All(f => f.Id.StartsWith("flag_c1_")));
            Assert.IsTrue(w.Case.Endings.All(e => e.Id.StartsWith("end_c1_")));
            Assert.IsTrue(w.Case.Decisions.All(d => d.Id.StartsWith("dec_c1_")));
        }

        [Test]
        public void AlexClaimsHaveTriggers()
        {
            var w = new Case1World();
            foreach (var claim in w.Case.AlexClaims)
            {
                Assert.AreNotEqual(Data.AlexClaimTrigger.None, claim.AlexTrigger, claim.Id);
                Assert.IsNotNull(claim.AlexTriggerEvidence, claim.Id);
            }
        }

        [Test]
        public void FactsReferencedByLiesExist()
        {
            var w = new Case1World();
            var allFacts = w.Case.Suspects.SelectMany(c => c.KnownFacts).Select(f => f.Id).ToHashSet();
            foreach (var lie in w.Case.Lies)
            {
                foreach (var f in lie.RefutedByFactIds) Assert.IsTrue(allFacts.Contains(f), $"{lie.Id}: hecho {f} no existe");
                foreach (var f in lie.RevealsFactIdsOnExposed) Assert.IsTrue(allFacts.Contains(f), $"{lie.Id}: hecho {f} no existe");
            }
        }
    }
}
