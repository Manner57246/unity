using System;
using System.Collections.Generic;

namespace Rouge.Core
{
    // These loaned cards use the ordinary battle rules and existing card plates.
    public sealed class FrostholdAssault
    {
        private readonly Dictionary<string, CardRule> arsenal = new();
        private readonly string[] deck = { "moon", "moon", "wardcut", "tide", "rime", "bolt" };
        public int Health { get; private set; } = 36;
        public int Victories { get; private set; }
        public bool StoryEnded { get; private set; }
        public Battle Fight { get; private set; }

        public FrostholdAssault()
        {
            Add("moon", "斩星", CardEffect.Slash, 26);
            Add("wardcut", "永夜返刃", CardEffect.ParryCut, 26);
            Add("tide", "倒悬之海", CardEffect.Spell, 16, Element.Water);
            Add("rime", "白日永冬", CardEffect.Spell, 20, Element.Ice);
            Add("bolt", "王庭落雷", CardEffect.Spell, 38, Element.Lightning);
        }

        private void Add(string id, string title, CardEffect effect, int value, Element element = Element.None)
        {
            arsenal.Add(id, new CardRule(id, title, effect, Rarity.Rare, value, element));
        }

        public CardRule Card(string id) => arsenal[id];

        public void Engage()
        {
            if (Fight != null || Victories >= 3 || Health == 0) return;
            var (health, power, tactics) = Victories switch
            {
                0 => (88, 5, EnemyKind.IronWarden),
                1 => (190, 10, EnemyKind.CrownKeeper),
                _ => (184, 8, EnemyKind.KnifeRunner)
            };
            Fight = new Battle(deck, health, power, new Random(720 + Victories), Card, Talent.Pathfinder, tactics, turnLimit:6);
        }

        public void FinishStory()
        {
            if (Victories != 3 || Fight != null) return;
            Health = 0;
            StoryEnded = true;
        }

        public CardPlay Play(int index)
        {
            var result = Fight?.Play(index);
            if (result != null && Fight.EnemyHealth == 0)
            {
                Victories++;
                Fight = null;
            }
            return result;
        }

        public void EndTurn()
        {
            if (Fight == null || Health == 0 || Fight.Phase != TurnPhase.Plan) return;
            Health = Math.Max(0, Health - Fight.EndEnemyAction());
            
            if (Health == 0) Fight = null;
        }
    }
}

