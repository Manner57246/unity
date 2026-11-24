using System;
using System.Collections.Generic;

namespace Rouge.Core
{
    public sealed class RunSession
    {
        private readonly RunRules rules;
        private readonly Random random;
        private readonly Random eventRandom;
        private EventKind? lastEvent;
        public ExpeditionEvent Encounter { get; private set; }
        public bool IsRoadside { get; private set; }
        private readonly Dictionary<string, CardRule> cards;
        private readonly List<string> deck;
        private readonly HashSet<int> explored = new HashSet<int>();
        private readonly HashSet<int> revealed = new HashSet<int>();
        public event Action Changed;
        public int Seed { get; }
        public RouteMap Map { get; }
        public Talent Talent { get; }
        public Loadout Loadout { get; }
        public int Movement { get; private set; }
        public int MaxMovement { get; }
        public IReadOnlyList<int> Journey { get; private set; }
        public int JourneyIndex { get; private set; }
        public int Destination { get; private set; } = -1;
        private readonly HashSet<int> charted = new HashSet<int>();
        public bool IsCharted(int cell) => charted.Contains(cell);
        public int TravelCost(int id)
        {
            int steps = Map.Path(Current, id, Talent).Count - 1;
            return (steps + 2) / 3;
        }

        public bool Regroup()
        {
            if (Phase != RunPhase.Map || Movement == MaxMovement) return false;
            Movement = MaxMovement;
            return Notify();
        }

        public RouteNode Current { get; private set; }
        public RunPhase Phase { get; private set; }
        public Battle Battle { get; private set; }
        public DeckStyle Style { get; }
        public int Health { get; private set; }
        public int Gold { get; private set; }
        public IReadOnlyList<string> Deck { get; }
        public string EventReport { get; private set; }
        public bool WindyNest { get; private set; }
        public string Offer { get; private set; }

        public RunSession(RunRules rules, int seed)
            : this(rules, seed, new WorldAtlas(seed, rules.layers).Regions[0], Talent.Pathfinder, Loadout.Provisions) { }

        public RunSession(RunRules rules, int seed, RouteMap map, Talent talent, Loadout loadout, DeckStyle deckStyle=DeckStyle.Balanced)
        {
            this.rules = rules;
            cards = rules.BuildCatalog();
            Seed = seed;
            Map = map;
            Talent = talent;
            Loadout = loadout;
            Style = deckStyle;
            MaxMovement = 12 + (loadout == Loadout.Provisions ? 4 : 0);
            Movement = MaxMovement;
            random = new Random(unchecked(seed + map.Region * 7919));
            eventRandom = new Random(unchecked(seed * 397 ^ map.Region * 7919 ^ 0x524f4144));
            deck = new List<string>(rules.StartingCards(deckStyle));
            deck.AddRange(rules.Specialty(talent));
            Deck = deck.AsReadOnly();
            Gold = rules.startingGold + (loadout == Loadout.Purse ? 5 : 0);
            Health = rules.maxHealth;
            Visit(Map.Nodes[0]);
        }

        private bool passageUsed;
        public RunSession ContinueTo(RouteMap next)
        {
            if (Phase != RunPhase.Passage || passageUsed || next == null || next.Region != Current.OnwardRegion)
                throw new InvalidOperationException("Reach the matching passage before changing region.");
            var run = new RunSession(rules, Seed, next, Talent, Loadout, Style);
            run.deck.Clear();
            run.deck.AddRange(deck);
            run.Health = Health;
            run.Gold = Gold;
            run.Movement = Movement;
            run.lastEvent = lastEvent;
            passageUsed = true;
            return run;
        }

        public CardRule Card(string id) => cards[id];
        public EnemyKind EnemyAt(RouteNode node)
        {
            if(node.Kind==RoomKind.Boss) return EnemyKind.CrownKeeper;
            uint hash=unchecked((uint)Seed*2654435761u+(uint)Map.Region*374761393u+(uint)node.Id*668265263u);
            return (EnemyKind)((hash ^ (hash>>16)) % 3);
        }
        public bool IsRevealed(int id) => revealed.Contains(id);
        public bool IsVisited(int id) => explored.Contains(id);
        public bool CanTravel(int id)
        {
            if (Phase != RunPhase.Map) return false;
            foreach (var next in Current.Exits) if (next == id) return Movement >= TravelCost(id);
            return false;
        }

        private void Visit(RouteNode node)
        {
            Current = node;
            explored.Add(node.Id);
            Reveal(Map.Cell(node), Talent == Talent.Pathfinder ? 5 : 3);
            revealed.Add(node.Id);
            foreach (var id in node.Exits) revealed.Add(id);
        }

        private void Reveal(int cell, int radius)
        {
            int w = Map.Terrain.Width;
            int cx = cell % w, cy = cell / w;
            for (int y = Math.Max(0, cy-radius); y <= Math.Min(TerrainGrid.Height-1, cy+radius); y++)
                for (int x = Math.Max(0, cx-radius); x <= Math.Min(w-1, cx+radius); x++)
                    if ((cx-x)*(cx-x) + (cy-y)*(cy-y) <= radius*radius)
                        charted.Add(Map.Terrain.Cell(x,y));
        }

        public bool BeginTravel(int id)
        {
            if (!CanTravel(id)) return false;
            Journey = Map.Path(Current, id, Talent);
            JourneyIndex = 0;
            Destination = id;
            Movement -= TravelCost(id);
            EventReport = null;
            Phase = RunPhase.Travelling;
            return Notify();
        }

        public bool AdvanceJourney()
        {
            if (Phase != RunPhase.Travelling) return false;
            int cell = Journey[++JourneyIndex];
            Reveal(cell, Talent == Talent.Pathfinder ? 4 : 2);
            if (JourneyIndex < Journey.Count-1) return true;
            Visit(Map.Nodes[Destination]);
            Destination = -1;
            Journey = null;
            if(eventRandom.Next(100)<rules.roadsideEventPercent)
            {
                StartEvent(true);
                return Notify();
            }
            EnterCurrentRoom();
            return Notify();
        }

        private void EnterCurrentRoom()
        {
            switch (Current.Kind)
            {
                case RoomKind.Passage:
                    Phase = RunPhase.Passage;
                    break;
                case RoomKind.Battle:
                case RoomKind.Boss:
                    bool boss = Current.Kind == RoomKind.Boss;
                    Battle = new Battle(deck, boss ? rules.bossHealth : rules.enemyHealth,
                        boss ? rules.bossAttack : rules.enemyAttack, random, Card, Talent, EnemyAt(Current), Style==DeckStyle.Arcanist);
                    Phase = RunPhase.Battle;
                    break;
                case RoomKind.Event:
                    StartEvent(false);
                    break;
                case RoomKind.Shop:
                    RollOffer(Rarity.Rare);
                    Phase = RunPhase.Shop;
                    break;
            }
        }

        public bool EndTurn()
        {
            if (Phase != RunPhase.Battle || Battle.Phase == TurnPhase.Draw) return false;
            int wound = Battle.EndEnemyAction();
            Health = Math.Max(0, Health - wound);
            if (Health == 0) Phase = RunPhase.Defeat;
            else if (Battle.EnemyHealth == 0)
            {
                if (Current.Kind == RoomKind.Boss) Phase = RunPhase.Victory;
                else { RollOffer(random.Next(4)==0 && Array.Exists(rules.cards,c=>c.rarity==Rarity.Uncommon) ? Rarity.Uncommon : Rarity.Common); Phase = RunPhase.Reward; }
            }
            if (Phase != RunPhase.Battle) Battle = null;
            return Notify();
        }

        public bool ClaimReward(bool takeCard)
        {
            if (Phase != RunPhase.Reward) return false;
            if (takeCard) deck.Add(Offer);
            else Gold += rules.victoryGold;
            return ReturnToMap();
        }

        public bool ResolveShop(bool purchase)
        {
            if (Phase != RunPhase.Shop || (purchase && Gold < rules.shopPrice)) return false;
            if (purchase)
            {
                Gold -= rules.shopPrice;
                deck.Add(Offer);
            }
            return ReturnToMap();
        }

        private void StartEvent(bool roadside)
        {
            var kind=ExpeditionEvent.Draw(eventRandom,Map.Region,lastEvent);
            WindyNest=kind==EventKind.Nest && eventRandom.Next(2)==0;
            Encounter=ExpeditionEvent.Create(kind,Talent,rules,WindyNest);
            IsRoadside=roadside;
            lastEvent=kind;
            Offer=null;
            foreach(var choice in Encounter.Choices)
                if(choice.Success.Card){RollOffer(Rarity.Rare,eventRandom);break;}
            Phase=RunPhase.Event;
        }

        public bool CanChoose(EventChoice choice)
        {
            if(Phase!=RunPhase.Event || choice==null || Encounter==null) return false;
            foreach(var available in Encounter.Choices)
                if(available==choice) return Health+choice.Success.Health>0 &&
                    Gold+choice.Success.Gold>=0 && Movement+choice.Success.Movement>=0;
            return false;
        }

        public bool ChooseEvent(EventChoice choice)
        {
            if(!CanChoose(choice))return false;
            var outcome=choice.Success;
            string check=choice.Name;
            if(choice.Threshold>0)
            {
                int first=eventRandom.Next(1,21);
                int second=choice.Advantage ? eventRandom.Next(1,21) : 0;
                bool passed=Math.Max(first,second)>=choice.Threshold;
                check+=choice.Advantage ? $" · 优势检定 {first} / {second}" : $" · 检定 {first}";
                check+=passed ? " 成功" : " 失手";
                if(!passed)outcome=choice.Failure;
            }
            int hp=Health, moves=Movement;
            Health=Math.Clamp(Health+outcome.Health,0,rules.maxHealth);
            Movement=Math.Clamp(Movement+outcome.Movement,0,MaxMovement);
            Gold+=outcome.Gold;
            if(outcome.Card)deck.Add(Offer);
            string result=new EventOutcome(Health-hp,Movement-moves,outcome.Gold).Describe();
            if(outcome.Card)result+=(result.Length>0?"，":"")+"获得「"+Card(Offer).title+"」";
            EventReport=check+" · "+(result.Length==0?"资源无变化":result);
            Offer=null;
            Encounter=null;
            Phase=Health==0 ? RunPhase.Defeat : IsRoadside ? RunPhase.RoadsideResult : RunPhase.Map;
            return Notify();
        }

        public bool LeaveEvent(ExpeditionEvent encounter)
        {
            if(Phase!=RunPhase.Event || Encounter!=encounter)return false;
            Encounter=null;
            Offer=null;
            if(IsRoadside)return ResumeRoom();
            return ReturnToMap();
        }

        public bool ContinueAfterEvent() => Phase==RunPhase.RoadsideResult && ResumeRoom();

        private bool ResumeRoom()
        {
            IsRoadside=false;
            EnterCurrentRoom();
            return Notify();
        }

        public bool ResolveEvent(bool rest) => Encounter?.Kind==EventKind.Nest && ChooseEvent(Encounter.Choices[rest?1:2]);
        public bool SearchNest() => Encounter?.Kind==EventKind.Nest && ChooseEvent(Encounter.Choices[0]);

        private void RollOffer(Rarity rarity,Random source=null)
        {
            var pool = Array.FindAll(rules.cards, card => card.rarity == rarity &&
                (card.effect != CardEffect.ReturnWing || Talent == Talent.Wings));
            Offer = pool[(source??random).Next(pool.Length)].id;
        }

        private bool ReturnToMap()
        {
            Offer = null;
            Phase = RunPhase.Map;
            return Notify();
        }

        private bool Notify()
        {
            Changed?.Invoke();
            return true;
        }
    }
}
