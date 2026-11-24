using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Rouge.Core;

namespace Rouge.Tests
{
    public class ExpeditionEventTests
    {
        private static void Arrive(RunSession run,int node)
        {
            Assert.That(run.BeginTravel(node));
            while(run.Phase==RunPhase.Travelling) run.AdvanceJourney();
        }

        [TestCase(RoomKind.Event)]
        [TestCase(RoomKind.Battle)]
        public void RoadEventResumesOriginalRoomExactlyOnce(RoomKind kind)
        {
            var run=new RunSession(new RunRules{roadsideEventPercent=100},44);
            int id=run.Current.Exits.First(i=>run.Map.Nodes[i].Kind==kind);
            int expected=run.Movement-run.TravelCost(id);
            Arrive(run,id);
            var encounter=run.Encounter;
            Assert.That(run.IsRoadside && run.IsVisited(id));
            Assert.That(run.Movement,Is.EqualTo(expected));
            Assert.That(run.AdvanceJourney() || run.BeginTravel(id),Is.False);
            var choice=encounter.Choices.First(run.CanChoose);
            Assert.That(run.ChooseEvent(choice));
            Assert.That(run.Phase,Is.EqualTo(RunPhase.RoadsideResult));
            int hp=run.Health, moves=run.Movement, cards=run.Deck.Count;
            Assert.That(run.ChooseEvent(choice),Is.False);
            Assert.That(run.ContinueAfterEvent());
            Assert.That(run.ContinueAfterEvent() || run.LeaveEvent(encounter),Is.False);
            Assert.That((run.Health,run.Movement,run.Deck.Count),Is.EqualTo((hp,moves,cards)));
            if(kind==RoomKind.Event)
            {
                Assert.That(run.Encounter.Kind,Is.Not.EqualTo(encounter.Kind));
                Assert.That(run.IsRoadside,Is.False);
            }
            else Assert.That(run.Phase,Is.EqualTo(RunPhase.Battle));
        }

        [Test]
        public void RegionalPoolsContainAllEventsAndRespectPaymentAndTalent()
        {
            var seen=new HashSet<EventKind>();
            for(int seed=0;seed<80;seed++)
            {
                var rules=new RunRules{roadsideEventPercent=0,startingGold=0};
                var atlas=new WorldAtlas(seed,1);
                foreach(int region in new[]{0,WorldAtlas.Mine})
                {
                    var run=new RunSession(rules,seed,atlas.Regions[region],Talent.Ferryman,Loadout.Provisions);
                    Arrive(run,run.Current.Exits.First(i=>run.Map.Nodes[i].Kind==RoomKind.Event));
                    Assert.That(run.IsRoadside,Is.False);
                    var encounter=run.Encounter;
                    seen.Add(encounter.Kind);
                    foreach(var choice in encounter.Choices.Where(c=>c.Success.Gold<0))
                        Assert.That(run.ChooseEvent(choice),Is.False);
                    if(encounter.Kind==EventKind.Ferry){
                        Assert.That(region,Is.EqualTo(0));
                        Assert.That(encounter.Choices[0].Threshold,Is.Zero);
                        Assert.That(run.ChooseEvent(encounter.Choices[0]));
                        Assert.That(run.Gold,Is.EqualTo(7));
                    }
                }
            }
            Assert.That(seen.Count,Is.EqualTo(System.Enum.GetValues(typeof(EventKind)).Length));
        }
    }
}
