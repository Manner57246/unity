using System;

namespace Rouge.Core
{
    public sealed class VillageHunt
    {
        private readonly CardRule knife = new("jab", "缺口小刀", CardEffect.Attack, Rarity.Common, 2);
        private readonly CardRule stick = new("guard", "横起木棍", CardEffect.Guard, Rarity.Common, 1);
        private readonly CardRule tracks = new("track", "辨蹄印", CardEffect.Expose, Rarity.Common, 1);
        public int Health { get; private set; } = 7;
        public Battle Fight { get; }

        public VillageHunt()
        {
            string[] tools = { "jab", "jab", "jab", "guard", "guard", "track" };
            Fight = new Battle(tools, 14, 3, new Random(), Card, Talent.Pathfinder, EnemyKind.Boar, turnLimit: 6);
        }

        public CardRule Card(string id) => id switch
        {
            "jab" => knife, "guard" => stick, "track" => tracks,
            _ => throw new ArgumentOutOfRangeException(nameof(id))
        };

        public void EndTurn() => Health = Math.Max(0, Health - Fight.EndEnemyAction());
    }
}
