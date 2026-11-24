using System;
using System.Collections.Generic;

namespace Rouge.Core
{
    public enum TurnPhase { Draw, Plan, Finished }
    public enum Swing { Slash, Backhand, Overhead }

    public sealed class CardPlay
    {
        public CardRule Card { get; internal set; }
        public Swing Swing { get; internal set; }
        public bool Dive { get; internal set; }
        public Reaction Reaction { get; internal set; }
        public string Report { get; internal set; }
    }

    public sealed class Battle
    {
        public const int HandSize = 6;
        private const int NormalLimit = 5;
        private readonly int turnLimit;
        private readonly List<CardPlay> plays = new();
        public IReadOnlyList<CardPlay> TurnPlays => plays;
        private readonly Random random;
        private readonly List<string> draw;
        private readonly List<string> discard = new();
        private readonly List<string> hand = new();
        private readonly Func<string,CardRule> cardById;
        private readonly Talent talent;
        private readonly int startingHealth;
        private int turnStartHealth;
        private bool liftedThisTurn;
        public EnemyKind Enemy { get; }
        public EnemyIntent Intent { get; private set; }
        public int Armor { get; private set; }
        private CardType? previous;
        private readonly bool arcaneOpening;
        public Element LastElement { get; private set; }
        public bool Frozen { get; private set; }
        public Reaction PreviewReaction(CardRule card) => Reaction.For(LastElement,card);
        public IReadOnlyList<string> Hand { get; }
        public TurnPhase Phase { get; private set; }
        public int Turn { get; private set; } = 1;
        public int EnemyHealth { get; private set; }
        public int EnemyAttack { get; }
        public bool Snaring => Intent.Move == EnemyMove.Bind;
        public bool Airborne { get; private set; }
        public int Exposed { get; private set; }
        public int Guard { get; private set; }
        public int SlashChain { get; private set; }
        public int Limit { get; private set; } = NormalLimit;
        public int Remaining { get; private set; } = NormalLimit;
        public string LastTurn { get; private set; }

        internal Battle(IEnumerable<string> deck,int health,int attack,Random random,Func<string,CardRule> cardById,Talent talent,EnemyKind enemy,bool arcaneOpening=false,int turnLimit=NormalLimit)
        {
            this.arcaneOpening=arcaneOpening;
            this.turnLimit=Limit=Remaining=turnLimit;
            this.random=random; this.cardById=cardById; this.talent=talent; Enemy=enemy;
            EnemyHealth=health; EnemyAttack=attack;
            startingHealth=turnStartHealth=health;
            Intent=EnemyTactics.Plan(enemy,attack,1,EnemyMove.Strike,false,false,false,false,false);
            draw=new List<string>(deck); Hand=hand.AsReadOnly(); Shuffle();
        }

        public bool DrawHand()
        {
            if(Phase!=TurnPhase.Draw)return false;
            // Deal up to three actual slash cards on the first turn, without duplicating any.
            if(Turn==1 && arcaneOpening)
            {
                var dealt=new HashSet<Element>();
                for(int i=draw.Count-1;i>=0 && hand.Count<HandSize;i--)
                {
                    var element=cardById(draw[i]).School;
                    if(element==Element.None || !dealt.Add(element)) continue;
                    hand.Add(draw[i]); draw.RemoveAt(i);
                }
            }
            else if(Turn==1)
            {
                for(int i=draw.Count-1;i>=0 && hand.Count<3;i--)
                    if(cardById(draw[i]).IsSlash){ hand.Add(draw[i]); draw.RemoveAt(i); }
            }
            Draw(HandSize-hand.Count);
            Phase=TurnPhase.Plan;
            return true;
        }

        private void Draw(int count)
        {
            while(count-->0 && hand.Count<8)
            {
                if(draw.Count==0){draw.AddRange(discard); discard.Clear(); Shuffle();}
                if(draw.Count==0)break;
                int top=draw.Count-1; hand.Add(draw[top]); draw.RemoveAt(top);
            }
        }

        public CardPlay Play(int index)
        {
            if(Phase!=TurnPhase.Plan || Remaining==0 || index<0 || index>=hand.Count)return null;
            var card=cardById(hand[index]);
            hand.RemoveAt(index);
            Remaining--;
            var result=new CardPlay { Card=card, Reaction=PreviewReaction(card) };
            SlashChain=card.IsSlash ? SlashChain+1 : 0;
            if(SlashChain==2)result.Swing=Swing.Backhand;
            if(SlashChain==3){result.Swing=Swing.Overhead;SlashChain=0;}
            string effect;
            if(card.Type==CardType.Spell)
            {
                var reaction=result.Reaction;
                int guard=(card.School==Element.Earth ? card.value : 0)+(reaction?.Guard ?? 0);
                int damage=(card.School==Element.Earth ? 0 : card.value)+(reaction?.Damage ?? 0);
                if(damage>0){damage=Hit(damage+Exposed);Exposed=0;}
                Guard+=guard;
                int draws=(card.School==Element.Wind ? 1 : 0)+(reaction?.Draw ?? 0);
                int before=hand.Count;Draw(draws);
                if(reaction?.Freeze==true)Frozen=true;
                var parts=new List<string>();
                if(reaction!=null)parts.Add(reaction.Name);
                if(damage>0)parts.Add($"伤害 {damage}");
                if(guard>0)parts.Add($"格挡 +{guard}");
                if(hand.Count>before)parts.Add($"抽 {hand.Count-before} 张");
                effect=string.Join(" · ",parts);
            }
            else if(card.Type==CardType.Attack)
            {
                int damage=card.value+Exposed+(result.Reaction?.Damage ?? 0);
                Exposed=0;
                result.Dive=talent==Talent.Wings && Airborne && previous==CardType.Skill;
                string combo="";
                if(result.Dive){damage+=3;Airborne=card.effect==CardEffect.ReturnWing;combo="俯冲 · ";}
                if(talent==Talent.Climber && previous==CardType.Defense){damage+=2;combo="落锤 · ";}
                if(talent==Talent.Pathfinder && previous==CardType.Skill){damage+=2;combo="伏击 · ";}
                if(talent==Talent.Ferryman && previous==CardType.Defense){Guard+=2;combo="回舟 +2 格挡 · ";}
                if(card.effect==CardEffect.ParryCut)Guard+=2;
                if(result.Swing==Swing.Overhead){damage+=4;combo+="重劈 · ";}
                if(result.Swing==Swing.Backhand)combo+="反手斩 · ";
                damage=Hit(damage);
                effect=(result.Reaction!=null ? result.Reaction.Name+" · " : "")+combo+damage+" 伤害"+(card.effect==CardEffect.ParryCut?" · +2 格挡":"");
            }
            else
            {
                switch(card.effect)
                {
                    case CardEffect.Guard: Guard+=card.value;effect="格挡 +"+card.value;break;
                    case CardEffect.Lift: liftedThisTurn=Airborne=true;effect="升空";break;
                    case CardEffect.Draw: int before=hand.Count;Draw(card.value);effect="抽 "+(hand.Count-before)+" 张";break;
                    default: Exposed=Math.Max(Exposed,card.value);effect="破绽 +"+Exposed;break;
                }
            }
            discard.Add(card.id);
            previous=card.Type;
            LastElement=card.School;
            LastTurn=result.Report=card.title+" · "+effect;
            plays.Add(result);
            if(EnemyHealth==0) Phase=TurnPhase.Finished;
            return result;
        }

        private int Hit(int damage)
        {
            int absorbed=Math.Min(Armor,damage);
            Armor-=absorbed;
            EnemyHealth=Math.Max(0,EnemyHealth-(damage-absorbed));
            return damage-absorbed;
        }

        internal int EndEnemyAction()
        {
            if(Phase==TurnPhase.Finished)return 0;
            int wound=Intent.Wound(Guard,Airborne,Frozen);
            Armor=Intent.Move==EnemyMove.Brace && !Frozen ? Intent.Power : 0;
            LastTurn=Frozen ? "敌方被冻结 · 跳过行动" : Intent.Move==EnemyMove.Brace ? $"敌方架盾 · 护甲 {Armor}" :
                Airborne && !Intent.HitsAir ? "闪避地面攻击 · 落地" : Intent.Name+" · 失血 "+wound;
            Limit=Snaring && wound>0 ? turnLimit-1 : turnLimit;
            plays.Clear();
            Intent=EnemyTactics.Plan(Enemy,EnemyAttack,Turn+1,Intent.Move,!Frozen,
                EnemyHealth<turnStartHealth,Guard>0,liftedThisTurn,EnemyHealth<=startingHealth/2);
            turnStartHealth=EnemyHealth;
            liftedThisTurn=false;
            LastElement=Element.None;Frozen=false;
            Remaining=Limit; Guard=0; Airborne=false; SlashChain=0; previous=null;
            Turn++; Phase=TurnPhase.Draw;
            return wound;
        }

        private void Shuffle()
        {
            for(int i=draw.Count-1;i>0;i--){int j=random.Next(i+1);(draw[i],draw[j])=(draw[j],draw[i]);}
        }
    }
}
