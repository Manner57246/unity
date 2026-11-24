using System;
using System.Collections.Generic;

namespace Rouge.Core
{
    public enum EventKind { Nest, Ferry, CaveIn, Shrine, Herbalist, Cache, Beacon }

    public readonly struct EventOutcome
    {
        public readonly int Health, Movement, Gold;
        public readonly bool Card;
        public EventOutcome(int health=0,int movement=0,int gold=0,bool card=false)
        { Health=health; Movement=movement; Gold=gold; Card=card; }

        public string Describe()
        {
            var parts=new List<string>();
            if(Health!=0) parts.Add($"{Health:+0;-0} 生命");
            if(Movement!=0) parts.Add($"{Movement:+0;-0} 移动点");
            if(Gold!=0) parts.Add($"{Gold:+0;-0} 金币");
            if(Card) parts.Add("获得稀有牌");
            return string.Join("，",parts);
        }
    }

    public sealed class EventChoice
    {
        public string Name { get; }
        public EventOutcome Success { get; }
        public EventOutcome Failure { get; }
        public int Threshold { get; }
        public bool Advantage { get; }
        internal EventChoice(string name,EventOutcome success,int threshold=0,EventOutcome failure=default,bool advantage=false)
        { Name=name; Success=success; Threshold=threshold; Failure=failure; Advantage=advantage; }
        public string Terms => Threshold==0 ? Success.Describe() :
            $"{(Advantage?"双骰取高":"D20")} ≥ {Threshold}　{Success.Describe()}\n失手　{Failure.Describe()}";
    }

    public sealed class ExpeditionEvent
    {
        public EventKind Kind { get; }
        public string Title { get; }
        public string Story { get; }
        public IReadOnlyList<EventChoice> Choices { get; }
        private ExpeditionEvent(EventKind kind,string title,string story,params EventChoice[] choices)
        { Kind=kind; Title=title; Story=story; Choices=Array.AsReadOnly(choices); }

        internal static EventKind Draw(Random rng,int region,EventKind? previous)
        {
            var pool=new List<EventKind>{EventKind.Shrine,EventKind.Herbalist,EventKind.Cache,EventKind.Beacon};
            if(region==0 || region==2)pool.AddRange(new[]{EventKind.Ferry,EventKind.Ferry,EventKind.Nest});
            else if(region==WorldAtlas.Mine || region==WorldAtlas.Forge)pool.AddRange(new[]{EventKind.CaveIn,EventKind.CaveIn});
            else pool.AddRange(new[]{EventKind.Nest,EventKind.Nest,EventKind.CaveIn});
            pool.RemoveAll(kind=>kind==previous);
            return pool[rng.Next(pool.Count)];
        }

        internal static ExpeditionEvent Create(EventKind kind,Talent talent,RunRules rules,bool windy)
        {
            switch(kind)
            {
                case EventKind.Nest:
                    bool wings=talent==Talent.Wings;
                    return new(kind,windy?"风暴中的崖巢":"悬崖上的鸟巢","乌鸦衔来一张带金边的牌，把它压在崖巢的枯枝下。",
                        new(wings ? (windy?"迎风取牌":"展翅直达") : "攀崖取牌",new(card:true),wings&&!windy?0:10,new(health:-3),wings&&windy),
                        new("避风休整",new(health:rules.healAmount,movement:3)),new("搜寻散币",new(gold:rules.victoryGold)));
                case EventKind.Ferry:
                    bool sailor=talent==Talent.Ferryman;
                    return new(kind,"沉钟渡口","一艘翻覆的渡船卡在浅滩。船钟每响一次，船舱里的钱匣就沉下一寸。",
                        new(sailor?"识水打捞":"潜水捞匣",new(gold:7),sailor?0:12,new(health:-3)),
                        new("雇船抄近道",new(movement:4,gold:-2)));
                case EventKind.CaveIn:
                    return new(kind,"石缝里的矿灯","断梁下响起三声敲击。困住的矿工用灯照亮一条只容一人侧身通过的缝隙。",
                        new("钻入石缝救人",new(card:true),11,new(health:-4),talent==Talent.Climber),
                        new("搬开断梁",new(movement:-2,gold:6)));
                case EventKind.Shrine:
                    return new(kind,"无名骑士的血誓","石像的剑横在祭台上。献上一滴心血，剑铭就会化为一张牌。",
                        new("立下血誓",new(health:-4,card:true)),new("点灯祈福",new(health:5,gold:-2)));
                case EventKind.Herbalist:
                    return new(kind,"会走路的药铺","背着小屋的驮兽停下来。药师指着两排瓶子，一排止血，一排能让脚步轻快。",
                        new("买止血药",new(health:7,gold:-3)),new("用脚力换药",new(health:4,movement:-2)),
                        new("买疾行露",new(movement:5,gold:-3)));
                case EventKind.Cache:
                    return new(kind,"走私客的双底箱","旧箱的夹层里闪着金边，箱盖下却绷着一根细索。外侧的零钱袋伸手可及。",
                        new("拆除机关",new(card:true),13,new(health:-3),talent==Talent.Pathfinder),
                        new("只拿零钱袋",new(gold:2)));
                default:
                    return new(kind,"熄灭的引路石","里程碑上刻着一条早已失传的小路。补全缺口，符文便会指向近道。",
                        new("辨认旧路",new(movement:6),10,new(movement:-2),talent==Talent.Pathfinder),
                        new("嵌入银币引路",new(movement:3,gold:-1)));
            }
        }
    }
}
