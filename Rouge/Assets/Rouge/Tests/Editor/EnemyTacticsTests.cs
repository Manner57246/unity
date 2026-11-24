using System.Linq;
using NUnit.Framework;
using Rouge.Core;

namespace Rouge.Tests
{
    public class EnemyTacticsTests
    {
        private static RunSession Encounter(EnemyKind kind)
        {
            var rules=new RunRules { enemyHealth=100, maxHealth=100, starterDeck=new[]{"strike","guard","tide","rime"} };
            for(int seed=0;seed<100;seed++)
            {
                var run=new RunSession(rules,seed,new WorldAtlas(seed,1).Regions[0],Talent.Wings,Loadout.Provisions);
                var room=run.Map.Nodes.First(n=>n.Kind==RoomKind.Battle);
                if(run.EnemyAt(room)!=kind)continue;
                Assert.That(run.Travel(room.Id));
                return run;
            }
            throw new AssertionException("No matching enemy for test seeds.");
        }
        private static void Play(Battle battle,string id) =>
            Assert.That(battle.Play(battle.Hand.ToList().IndexOf(id)),Is.Not.Null);

        [TestCase(false)]
        [TestCase(true)]
        public void FreezeCanInterruptBraceAndItsFollowUp(bool freeze)
        {
            var run=Encounter(EnemyKind.IronWarden);
            var b=run.Battle;
            b.DrawHand();Play(b,"strike");run.EndTurn();
            Assert.That(b.Intent.Move,Is.EqualTo(EnemyMove.Brace));
            b.DrawHand();
            if(freeze){Play(b,"tide");Play(b,"rime");}
            run.EndTurn();
            Assert.That(b.Armor,Is.EqualTo(freeze?0:b.EnemyAttack));
            Assert.That(b.Intent.Move==EnemyMove.Crush,Is.EqualTo(!freeze));
            b.DrawHand();
            int before=b.EnemyHealth;
            Play(b,"strike");
            Assert.That(b.EnemyHealth,Is.EqualTo(before-(freeze?4:0)));
        }
        [Test]
        public void GuardIsSpentAcrossBothHitsAndPromptsAnnouncedFeint()
        {
            var run=Encounter(EnemyKind.KnifeRunner);
            var b=run.Battle;b.DrawHand();Play(b,"guard");
            Assert.That(b.Intent.Move,Is.EqualTo(EnemyMove.TwinCut));
            run.EndTurn();
            Assert.That(run.Health,Is.EqualTo(99));
            Assert.That(b.Intent.Move,Is.EqualTo(EnemyMove.Feint));
            b.DrawHand();Play(b,"guard");run.EndTurn();
            Assert.That(run.Health,Is.EqualTo(96));
        }
        [Test]
        public void LightningCanHitAnAirborneTargetAfterADodgedGroundSpell()
        {
            var run=Encounter(EnemyKind.StormCaller);
            var b=run.Battle;b.DrawHand();Play(b,"flap");run.EndTurn();
            Assert.That(run.Health,Is.EqualTo(100));
            Assert.That(b.Intent.Move,Is.EqualTo(EnemyMove.Bolt));
            b.DrawHand();Play(b,"flap");run.EndTurn();
            Assert.That(run.Health,Is.EqualTo(95));
        }
    }
}
