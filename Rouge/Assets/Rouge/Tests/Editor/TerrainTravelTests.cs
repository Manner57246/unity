using System;
using System.Linq;
using NUnit.Framework;
using Rouge.Core;

namespace Rouge.Tests
{
    public class TerrainTravelTests
    {
        [Test]
        public void EveryBranchHasALegalPathAndSpecialistsCanShortenDetours()
        {
            int lakeShortcuts=0, cliffShortcuts=0;
            for(int seed=0;seed<24;seed++)
            foreach(var map in new WorldAtlas(seed,5).Regions)
            foreach(var node in map.Nodes)
            foreach(int target in node.Exits)
            {
                var foot=map.Path(node,target,Talent.Pathfinder);
                foreach(Talent talent in Enum.GetValues(typeof(Talent)))
                {
                    var path=map.Path(node,target,talent);
                    Assert.That(path[0],Is.EqualTo(map.Cell(node)));
                    Assert.That(path[path.Count-1],Is.EqualTo(map.Cell(map.Nodes[target])));
                    Assert.That(path.All(c=>map.Terrain.Passable(c,talent)));
                    for(int i=1;i<path.Count;i++)
                    {
                        int a=path[i],b=path[i-1],w=map.Terrain.Width;
                        Assert.That(Math.Abs(a%w-b%w)+Math.Abs(a/w-b/w),Is.EqualTo(1));
                    }
                    Assert.That(path.Count,Is.LessThanOrEqualTo(foot.Count));
                    if(path.Count+3<foot.Count && talent==Talent.Ferryman) lakeShortcuts++;
                    if(path.Count+3<foot.Count && talent==Talent.Climber) cliffShortcuts++;
                }
            }
            Assert.That(lakeShortcuts,Is.Positive);
            Assert.That(cliffShortcuts,Is.Positive);
        }

        [Test]
        public void DepartureAndExplorationAffectResourcesAndPersistentSight()
        {
            var map=new WorldAtlas(85,4).Regions[0];
            var rules=new RunRules();
            var scout=new RunSession(rules,85,map,Talent.Pathfinder,Loadout.Provisions);
            var sailor=new RunSession(rules,85,map,Talent.Ferryman,Loadout.Purse);
            Assert.That(scout.Movement,Is.EqualTo(sailor.Movement+4));
            Assert.That(sailor.Gold,Is.EqualTo(scout.Gold+5));
            var cells=Enumerable.Range(0,map.Terrain.Width*TerrainGrid.Height).ToArray();
            var before=cells.Where(scout.IsCharted).ToArray();
            Assert.That(before.Length,Is.GreaterThan(cells.Count(sailor.IsCharted)));
            int id=scout.Current.Exits[0],hours=scout.TravelCost(id),daylight=scout.Movement;
            var path=map.Path(scout.Current,id,scout.Talent);
            Assert.That(scout.Travel(id));
            Assert.That(scout.Movement,Is.EqualTo(daylight-hours));
            Assert.That(before.All(scout.IsCharted) && path.All(scout.IsCharted));
            Assert.That(cells.Count(scout.IsCharted),Is.GreaterThan(before.Length));
        }

        [Test]
        public void CampOpensAdjacentMarchesBeforeTheOuterSanctum()
        {
            var atlas = new WorldAtlas(70,1);
            Assert.That(Enumerable.Range(0,atlas.Regions.Count).Where(atlas.Available),Is.EqualTo(new[]{0,1}));
            Assert.That(atlas.CompleteSite(4),Is.False);
            Assert.That(atlas.CompleteSite(1));
            Assert.That(atlas.Available(3));
            Assert.That(atlas.Available(WorldAtlas.Mine));
            Assert.That(atlas.Available(4),Is.False);
            Assert.That(atlas.CompleteSite(1),Is.False);
            Assert.That(atlas.CompleteSite(3));
            Assert.That(atlas.Available(4));
            Assert.That(atlas.Available(WorldAtlas.Forge));
            Assert.That(atlas.Available(WorldAtlas.Mine),Is.False);
        }

        [TestCase(3)]
        [TestCase(WorldAtlas.Mine)]
        public void CliffRouteLeadsOnlyToItsAdvertisedDestination(int destination)
        {
            var rules = new RunRules { enemyHealth = 1 };
            var atlas = new WorldAtlas(121, rules.layers);
            var run = new RunSession(rules,121,atlas.Regions[WorldAtlas.Highland],Talent.Wings,Loadout.Purse,DeckStyle.Blades);
            Assert.Throws<InvalidOperationException>(() => run.ContinueTo(atlas.Regions[destination]));
            int first = run.Current.Exits.Single(i => run.Map.Nodes[i].OnwardRegion == destination);
            if (destination == 3)
            {
                var path = run.Map.Path(run.Current,first,run.Talent);
                Assert.That(path.Contains(run.Map.Terrain.Cell(14,12)));
            }
            Assert.That(run.Map.Landmark(run.Map.Nodes[first]),Does.Contain(WorldAtlas.Names[destination]));
            int step = first;
            while(true)
            {
                if(!run.CanTravel(step)) run.Regroup();
                Assert.That(run.Travel(step));
                if(run.Phase == RunPhase.Passage) break;
                if(run.Phase == RunPhase.Battle)
                {
                    run.Battle.DrawHand();
                    run.Battle.Play(run.Battle.Hand.ToList().FindIndex(id=>run.Card(id).Type==CardType.Attack));
                    run.EndTurn();
                    Assert.That(run.ClaimReward(true));
                }
                else if(run.Phase == RunPhase.Shop) run.ResolveShop(false);
                else Assert.That(run.LeaveEvent(run.Encounter));
                Assert.That(run.Current.Exits.All(i => run.Map.Nodes[i].OnwardRegion == destination));
                step = run.Current.Exits[0];
            }
            Assert.Throws<InvalidOperationException>(() => run.ContinueTo(atlas.Regions[destination==3 ? WorldAtlas.Mine : 3]));
            var next = run.ContinueTo(atlas.Regions[destination]);
            Assert.That(next.Current.Kind,Is.EqualTo(RoomKind.Camp));
            Assert.That(next.Map.Region,Is.EqualTo(destination));
            Assert.That((next.Style,next.Talent,next.Loadout,next.Health,next.Gold,next.Movement),
                Is.EqualTo((run.Style,run.Talent,run.Loadout,run.Health,run.Gold,run.Movement)));
            Assert.That(next.Deck,Is.EqualTo(run.Deck));
            Assert.Throws<InvalidOperationException>(() => run.ContinueTo(atlas.Regions[destination]));
        }

        [Test]
        public void RegionalRoutesReachEveryMapAndEndAtABoss()
        {
            var atlas = new WorldAtlas(73,5);
            var seen = new System.Collections.Generic.HashSet<int>();
            foreach(var road in WorldAtlas.Roads.Where(r=>r.From==-1)) Visit(road.To,new int[0]);
            Assert.That(seen.Count,Is.EqualTo(atlas.Regions.Count));
            void Visit(int region,int[] trail)
            {
                Assert.That(trail.Contains(region),Is.False);
                if(!seen.Add(region))return;
                var map=atlas.Regions[region];
                var destinations=WorldAtlas.Roads.Where(r=>r.From==region).Select(r=>r.To).ToArray();
                var leaves=map.Nodes.Where(n=>n.Exits.Count==0).ToArray();
                if(destinations.Length==0) Assert.That(leaves.Single().Kind,Is.EqualTo(RoomKind.Boss));
                else
                {
                    Assert.That(destinations.Distinct().Count(),Is.GreaterThanOrEqualTo(2));
                    Assert.That(leaves.All(n=>n.Kind==RoomKind.Passage));
                    Assert.That(leaves.Select(n=>n.OnwardRegion),Is.EquivalentTo(destinations));
                    foreach(int next in destinations) Visit(next,trail.Concat(new[]{region}).ToArray());
                }
            }
        }

        [Test]
        public void TravelChargesOnceAndRevealsAlongTheWalkBeforeArrival()
        {
            var run = new RunSession(new RunRules(),9);
            int target = run.Current.Exits[0], cost = run.TravelCost(target), before = run.Movement;
            var path = run.Map.Path(run.Current,target,run.Talent);
            var seen = Enumerable.Range(0,run.Map.Terrain.Width*TerrainGrid.Height).Where(run.IsCharted).ToArray();
            Assert.That(run.BeginTravel(target));
            Assert.That(run.BeginTravel(target) || run.Regroup(),Is.False);
            Assert.That(run.Movement,Is.EqualTo(before-cost));
            Assert.That(run.IsVisited(target),Is.False);
            Assert.That(path.Where(c=>!seen.Contains(c)).Any(),Is.True);
            Assert.That(path.All(run.IsCharted),Is.False);
            while(run.JourneyIndex < path.Count-2) {
                run.AdvanceJourney();
                Assert.That(run.Phase,Is.EqualTo(RunPhase.Travelling));
                Assert.That(run.IsCharted(path[run.JourneyIndex]));
            }
            run.AdvanceJourney();
            Assert.That(run.IsVisited(target));
            Assert.That(run.AdvanceJourney(),Is.False);
            Assert.That(run.Movement,Is.EqualTo(before-cost));
        }
    }
}
