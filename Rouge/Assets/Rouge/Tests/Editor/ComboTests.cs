using System.Linq;
using NUnit.Framework;
using Rouge.Core;

namespace Rouge.Tests
{
    public class ComboTests
    {
        [TestCase(false,7,3)]
        [TestCase(true,4,0)]
        public void WingsReadOrderAtResolution(bool guardInMiddle,int damage,int wound)
        {
            var rules = new RunRules { enemyHealth=20, enemyAttack=6 };
            for(int seed=0;seed<256;seed++)
            {
                var run=new RunSession(rules,seed,new WorldAtlas(seed,1).Regions[0],Talent.Wings,Loadout.Purse);
                run.Travel(run.Current.Exits.First(i=>run.Map.Nodes[i].Kind==RoomKind.Battle));
                var battle=run.Battle;
                battle.DrawHand();
                var hand=battle.Hand.ToList();
                if(!new[]{"flap","strike","guard"}.All(hand.Contains)) continue;
                void Play(string id) => battle.Play(battle.Hand.ToList().IndexOf(id));
                Play("flap");
                Play(guardInMiddle?"guard":"strike");
                Play(guardInMiddle?"strike":"guard");
                int hp=run.Health;
                Assert.That(run.EndTurn());
                Assert.That(battle.EnemyHealth,Is.EqualTo(20-damage));
                Assert.That(run.Health,Is.EqualTo(hp-wound));
                return;
            }
            Assert.Fail("Missing representative hand.");
        }

        [Test]
        public void CollectingNestCardIsOnceOnlyAndUsesWings()
        {
            bool calm=false, windy=false;
            for(int seed=0;seed<64 && !(calm&&windy);seed++)
            {
                var run=new RunSession(new RunRules(),seed,new WorldAtlas(seed,1).Regions[0],Talent.Wings,Loadout.Purse);
                Assert.That(run.Deck.Count,Is.EqualTo(10));
                run.Travel(run.Current.Exits.First(i=>run.Map.Nodes[i].Kind==RoomKind.Event));
                if(run.Encounter.Kind!=EventKind.Nest)continue;
                string offer=run.Offer;
                Assert.That(run.Card(offer).rarity,Is.EqualTo(Rarity.Rare));
                Assert.That(run.SearchNest());
                Assert.That(run.SearchNest(),Is.False);
                if(run.WindyNest){Assert.That(run.EventReport,Does.Contain("优势检定"));windy=true;}
                else {Assert.That(run.Deck.Last(),Is.EqualTo(offer));Assert.That(run.Deck.Count,Is.EqualTo(11));calm=true;}
            }
            Assert.That(calm&&windy);
        }
    }
}
