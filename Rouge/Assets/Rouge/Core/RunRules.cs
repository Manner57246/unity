using System;
using System.Collections.Generic;

namespace Rouge.Core
{
    public enum CardEffect { Attack, Guard, Expose, Lift, ReturnWing, Slash, ParryCut, Draw, Spell }
    public enum DeckStyle { Balanced, Blades, Sentinel, Arcanist }
    public enum CardType { Attack, Defense, Skill, Spell }
    public enum Rarity { Common, Uncommon, Rare }
    public enum RunPhase { Map, Battle, Reward, Shop, Event, Victory, Defeat, Travelling, Passage, RoadsideResult }
    public enum RoomKind { Camp, Battle, Shop, Event, Boss, Passage }

    [Serializable]
    public class CardRule
    {
        public string id;
        public string title;
        public CardEffect effect;
        public int value;
        public Element element;
        public Element School => effect==CardEffect.Spell ? element : Element.None;
        public Rarity rarity;
        public bool IsSlash => id == "strike" || effect == CardEffect.Slash || effect == CardEffect.ParryCut;
        public CardType Type => effect == CardEffect.Spell ? CardType.Spell : effect == CardEffect.Guard ? CardType.Defense :
            effect == CardEffect.Expose || effect == CardEffect.Lift || effect == CardEffect.Draw ? CardType.Skill : CardType.Attack;
        public string Grade => rarity == Rarity.Common ? "普通" : rarity == Rarity.Uncommon ? "非凡" : "稀有";
        public string Category => Type == CardType.Spell ? Reaction.School(element)+"系法术" : Type == CardType.Attack ? "攻击" : Type == CardType.Defense ? "防御" : "技巧";
        public string Text => effect switch {
            CardEffect.Spell when element==Element.Earth => $"获得 {value} 格挡",
            CardEffect.Spell when element==Element.Wind => $"造成 {value} 伤害，抽 1 张",
            CardEffect.Guard => $"获得 {value} 格挡",
            CardEffect.Expose => $"下一击额外造成 {value} 伤害",
            CardEffect.Draw => $"抽 {value} 张牌",
            CardEffect.ParryCut => $"造成 {value} 伤害，获得 2 格挡",
            CardEffect.Lift => "升空，闪避下次地面攻击",
            CardEffect.ReturnWing => $"造成 {value} 伤害；俯冲后再次升空",
            _ => $"造成 {value} 伤害"
        };

        public CardRule(string id, string title, CardEffect effect, Rarity rarity, int value, Element element=Element.None)
        {
            this.id = id;
            this.title = title;
            this.effect = effect;
            this.rarity = rarity;
            this.value = value;
            this.element = element;
        }
    }

    [Serializable]
    public class RunRules
    {
        public int layers = 5;
        public int maxHealth = 32;
        public int startingGold = 5;
        public int enemyHealth = 18;
        public int enemyAttack = 4;
        public int bossHealth = 32;
        public int bossAttack = 6;
        public int shopPrice = 5;
        public int victoryGold = 3;
        public int healAmount = 8;
        public int roadsideEventPercent = 15;
        public CardRule[] cards = {
            new CardRule("ember", "火矢", CardEffect.Spell, Rarity.Common, 4, Element.Fire),
            new CardRule("rime", "霜针", CardEffect.Spell, Rarity.Common, 2, Element.Ice),
            new CardRule("tide", "涌泉", CardEffect.Spell, Rarity.Common, 2, Element.Water),
            new CardRule("gale", "旋风", CardEffect.Spell, Rarity.Common, 2, Element.Wind),
            new CardRule("crag", "石壁", CardEffect.Spell, Rarity.Common, 4, Element.Earth),
            new CardRule("bolt", "落雷", CardEffect.Spell, Rarity.Common, 5, Element.Lightning),
            new CardRule("strike", "斩击", CardEffect.Attack, Rarity.Common, 4),
            new CardRule("guard", "架盾", CardEffect.Guard, Rarity.Common, 3),
            new CardRule("read", "识破", CardEffect.Expose, Rarity.Common, 2),
            new CardRule("jab", "快刺", CardEffect.Attack, Rarity.Common, 3),
            new CardRule("flap", "振翅", CardEffect.Lift, Rarity.Uncommon, 0),
            new CardRule("pinion", "翼刃", CardEffect.Attack, Rarity.Uncommon, 5),
            new CardRule("track", "追踪", CardEffect.Expose, Rarity.Uncommon, 3),
            new CardRule("ambush", "伏击", CardEffect.Attack, Rarity.Uncommon, 5),
            new CardRule("oar", "撑篙", CardEffect.Guard, Rarity.Uncommon, 4),
            new CardRule("hook", "船钩", CardEffect.Attack, Rarity.Uncommon, 5),
            new CardRule("anchor", "锚定", CardEffect.Guard, Rarity.Uncommon, 5),
            new CardRule("hammer", "岩锤", CardEffect.Attack, Rarity.Uncommon, 5),
            new CardRule("return", "回旋翼", CardEffect.ReturnWing, Rarity.Rare, 3),
            new CardRule("breach", "破阵", CardEffect.Attack, Rarity.Rare, 7),
            new CardRule("cutback", "折锋", CardEffect.ParryCut, Rarity.Common, 3),
            new CardRule("study", "整势", CardEffect.Draw, Rarity.Common, 2),
            new CardRule("moon", "弦月斩", CardEffect.Slash, Rarity.Rare, 6),
            new CardRule("wardcut", "守势斩", CardEffect.ParryCut, Rarity.Uncommon, 4),
            new CardRule("ward", "古老守护", CardEffect.Guard, Rarity.Rare, 7)
        };
        public string[] starterDeck = { "strike", "strike", "strike", "guard", "read", "jab", "cutback", "study" };

        public IEnumerable<string> StartingCards(DeckStyle style) => style switch {
            DeckStyle.Arcanist => new[] { "ember","rime","tide","gale","crag","bolt","strike","guard" },
            DeckStyle.Blades => new[] { "strike","strike","strike","strike","cutback","cutback","study","read" },
            DeckStyle.Sentinel => new[] { "strike","strike","cutback","wardcut","guard","guard","study","read" },
            _ => starterDeck
        };

        public string[] Specialty(Talent talent) => talent switch {
            Talent.Wings => new[] { "flap", "pinion" },
            Talent.Ferryman => new[] { "oar", "hook" },
            Talent.Climber => new[] { "anchor", "hammer" },
            _ => new[] { "track", "ambush" }
        };

        internal Dictionary<string, CardRule> BuildCatalog()
        {
            if (layers < 1 || layers > 20 || maxHealth < 1 || startingGold < 0 ||
                enemyHealth < 1 || enemyAttack < 1 || bossHealth < 1 || bossAttack < 1 ||
                shopPrice < 0 || victoryGold < 0 || healAmount < 1 || roadsideEventPercent < 0 || roadsideEventPercent > 100)
                throw new ArgumentException("Invalid run settings.");
            var catalog = new Dictionary<string, CardRule>();
            foreach (var card in cards)
            {
                if (card == null || string.IsNullOrWhiteSpace(card.id) || string.IsNullOrWhiteSpace(card.title) ||
                    card.value < 0 || !Enum.IsDefined(typeof(CardEffect), card.effect))
                    throw new ArgumentException("Invalid card definition.");
                if (card.effect==CardEffect.Spell && (card.element==Element.None || !Enum.IsDefined(typeof(Element),card.element)))
                    throw new ArgumentException("Invalid spell element: "+card.id);
                catalog.Add(card.id, card);
            }
            bool hasAttack = false;
            foreach (var id in starterDeck)
            {
                if (!catalog.TryGetValue(id, out var card))
                    throw new ArgumentException("Unknown starter card: " + id);
                hasAttack |= card.Type == CardType.Attack || card.Type == CardType.Spell && card.element!=Element.Earth;
            }
            if (!hasAttack) throw new ArgumentException("The starter deck needs an attack card.");
            return catalog;
        }
    }
}
