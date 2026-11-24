using System.Linq;
using NUnit.Framework;
using Rouge.Core;

namespace Rouge.Tests
{
    public class RunSessionTests
    {
        [Test]
        public void GeneratedRoutesAreConnectedAndRevealOnlyDiscoveredRooms()
        {
            for (int seed = 0; seed < 64; seed++)
            {
                var run = new RunSession(new RunRules(), seed);
                var nodes = run.Map.Nodes;
                var destinations = nodes.SelectMany(n => n.Exits).ToHashSet();
                foreach (var node in nodes)
                {
                    if (node.Id != 0) Assert.That(destinations.Contains(node.Id));
                    Assert.That(run.IsRevealed(node.Id), Is.EqualTo(node.Layer <= 1));
                    foreach (var id in node.Exits)
                        Assert.That(nodes[id].Layer, Is.EqualTo(node.Layer + 1));
                    if (node.Kind != RoomKind.Passage && node.Kind != RoomKind.Boss) Assert.That(node.Exits, Is.Not.Empty);
                }
                Assert.That(run.Travel(nodes.Count - 1), Is.False);
            }
        }

        [Test]
        public void SameSeedReplaysMapAndHand()
        {
            var first = new RunSession(new RunRules(), 407);
            var second = new RunSession(new RunRules(), 407);
            foreach (var node in first.Map.Nodes)
            {
                var other = second.Map.Nodes[node.Id];
                Assert.That(other.Kind, Is.EqualTo(node.Kind));
                Assert.That(other.Exits, Is.EqualTo(node.Exits));
            }
            int target = first.Current.Exits.First(id => first.Map.Nodes[id].Kind == RoomKind.Battle);
            first.Travel(target);
            second.Travel(target);
            for (int i = 0; i < 3; i++)
            {
                first.Battle.DrawHand(); second.Battle.DrawHand();
                Assert.That(first.Battle.Hand, Is.EqualTo(second.Battle.Hand));
                first.EndTurn();
                second.EndTurn();
            }
        }

        [Test]
        public void SlashesResolveImmediatelyAndConsumeTheTurnAllowance()
        {
            var rules = new RunRules { enemyHealth = 100, starterDeck = Enumerable.Repeat("strike",8).ToArray() };
            var run = new RunSession(rules,9);
            run.Travel(run.Current.Exits.First(id=>run.Map.Nodes[id].Kind==RoomKind.Battle));
            var battle=run.Battle;
            Assert.That(battle.Play(0),Is.Null);
            Assert.That(battle.DrawHand());
            Assert.That(battle.Hand.Count,Is.EqualTo(6));
            Assert.That(battle.Play(-1),Is.Null);
            Assert.That(battle.Play(0).Swing,Is.EqualTo(Swing.Slash));
            Assert.That(battle.EnemyHealth,Is.EqualTo(96));
            Assert.That(battle.Play(0).Swing,Is.EqualTo(Swing.Backhand));
            Assert.That(battle.Play(0).Swing,Is.EqualTo(Swing.Overhead));
            Assert.That(battle.EnemyHealth,Is.EqualTo(84));
            battle.Play(0); battle.Play(0);
            Assert.That(battle.Play(0),Is.Null);
            Assert.That(battle.Hand.Count,Is.EqualTo(1));
            Assert.That(run.EndTurn());
            Assert.That(battle.Remaining,Is.EqualTo(5));
            Assert.That(battle.SlashChain,Is.Zero);
        }

        [Test]
        public void RewardsAndBossCanOnlyBeResolvedOnce()
        {
            var rules = new RunRules { layers = 1, enemyHealth = 1, bossHealth = 1,
                starterDeck = new[] { "strike" } };
            var run = new RunSession(rules, 15, new WorldAtlas(15,1).Regions[4], Talent.Pathfinder, Loadout.Provisions);
            run.Travel(run.Current.Exits.First(id => run.Map.Nodes[id].Kind == RoomKind.Battle));
            Assert.That(run.Travel(run.Current.Exits[0]), Is.False);
            run.Battle.DrawHand();
            int hit = run.Battle.Hand.ToList().FindIndex(id => run.Card(id).Type == CardType.Attack); 
            run.Battle.Play(hit); run.EndTurn();
            Assert.That(run.Phase, Is.EqualTo(RunPhase.Reward));
            Assert.That(run.ClaimReward(false));
            Assert.That(run.ClaimReward(false), Is.False);
            Assert.That(run.Gold, Is.EqualTo(rules.startingGold + rules.victoryGold));
            run.Travel(run.Current.Exits[0]);
            run.Battle.DrawHand();
            hit = run.Battle.Hand.ToList().FindIndex(id => run.Card(id).Type == CardType.Attack);
            run.Battle.Play(hit); run.EndTurn();
            Assert.That(run.Phase, Is.EqualTo(RunPhase.Victory));
            Assert.That(run.EndTurn() || run.ClaimReward(true), Is.False);
        }

        [TestCase(0, false)]
        [TestCase(8, true)]
        public void ShopChecksFundsAndChargesOnce(int cash, bool affordable)
        {
            var rules = new RunRules { startingGold = cash };
            for (int seed = 0; seed < 100; seed++)
            {
                var run = new RunSession(rules, seed);
                run.Travel(run.Current.Exits.First(id => run.Map.Nodes[id].Kind == RoomKind.Event));
                var shop = run.Current.Exits.Where(id => run.Map.Nodes[id].Kind == RoomKind.Shop).ToArray();
                if (shop.Length == 0) continue;
                var encounter=run.Encounter;
                Assert.That(run.LeaveEvent(encounter));
                Assert.That(run.LeaveEvent(encounter), Is.False);
                run.Travel(shop[0]);
                int size = run.Deck.Count;
                Assert.That(run.ResolveShop(true), Is.EqualTo(affordable));
                Assert.That(run.Gold, Is.EqualTo(affordable ? cash - rules.shopPrice : cash));
                Assert.That(run.Deck.Count, Is.EqualTo(affordable ? size + 1 : size));
                Assert.That(run.ResolveShop(false), Is.EqualTo(!affordable));
                Assert.That(run.ResolveShop(true), Is.False);
                return;
            }
            Assert.Fail("No route from the event to a shop.");
        }

        [Test]
        public void DeathClosesCombatAndBlocksTravel()
        {
            var rules = new RunRules { maxHealth = 1 };
            var run = new RunSession(rules, 3);
            run.Travel(run.Current.Exits.First(id => run.Map.Nodes[id].Kind == RoomKind.Battle));
            run.Battle.DrawHand(); run.EndTurn();
            Assert.That(run.Phase, Is.EqualTo(RunPhase.Defeat));
            Assert.That(run.Health, Is.Zero);
            Assert.That(run.Travel(run.Current.Exits[0]) || run.EndTurn(), Is.False);
        }
    }
}
