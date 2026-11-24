using System.Linq;
using NUnit.Framework;
using Rouge.Core;

namespace Rouge.Tests
{
    public class ElementTests
    {
        private static RunSession Enter(RunRules rules, DeckStyle style)
        {
            var run=new RunSession(rules,51,new WorldAtlas(51,1).Regions[0],Talent.Wings,Loadout.Provisions,style);
            run.Travel(run.Current.Exits.First(i=>run.Map.Nodes[i].Kind==RoomKind.Battle));
            Assert.That(run.Battle.DrawHand());
            return run;
        }

        [TestCase("tide","rime","冻结",4,0,0)]
        [TestCase("tide","bolt","导电",12,0,0)]
        [TestCase("rime","crag","碎冰",7,4,0)]
        [TestCase("crag","ember","熔岩",8,4,0)]
        [TestCase("ember","gale","烈焰风暴",10,0,1)]
        [TestCase("gale","bolt","雷暴",9,0,2)]
        [TestCase("bolt","tide","潮汐护幕",7,4,0)]
        public void OrderedSpellsPreviewAndApply(string first,string second,string reaction,int damage,int guard,int draws)
        {
            var run=Enter(new RunRules{enemyHealth=100},DeckStyle.Arcanist);
            var b=run.Battle;
            Assert.That(b.Hand.Select(id=>run.Card(id).element).Distinct().Count(),Is.EqualTo(6));
            b.Play(b.Hand.ToList().IndexOf(first));
            var preview=b.PreviewReaction(run.Card(second));
            Assert.That(preview?.Name,Is.EqualTo(reaction));
            Assert.That(b.Play(b.Hand.ToList().IndexOf(second)).Reaction,Is.SameAs(preview));
            Assert.That(b.EnemyHealth,Is.EqualTo(100-damage));
            Assert.That(b.Guard,Is.EqualTo(guard));
            Assert.That(b.Hand.Count,Is.EqualTo(4+draws));
            Assert.That(b.Remaining,Is.EqualTo(3));
            int health=run.Health;
            run.EndTurn();
            Assert.That(run.Health,Is.EqualTo(health-(reaction=="冻结"?0:System.Math.Max(0,b.EnemyAttack-guard))));
            Assert.That(b.Frozen,Is.False);
            Assert.That(b.LastElement,Is.EqualTo(Element.None));
        }

        [TestCase(false,89)]
        [TestCase(true,92)]
        public void FlameBladeRequiresAdjacentSlash(bool interrupt,int hp)
        {
            var run=Enter(new RunRules{starterDeck=new[]{"ember","guard","strike"},enemyHealth=100},DeckStyle.Balanced);
            var b=run.Battle;
            void Play(string id)=>b.Play(b.Hand.ToList().IndexOf(id));
            Play("ember");
            if(interrupt)Play("guard");
            Play("strike");
            Assert.That(b.EnemyHealth,Is.EqualTo(hp));
        }
    }
}
