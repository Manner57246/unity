using System;

namespace Rouge.Core
{
    public enum EnemyKind { IronWarden, KnifeRunner, StormCaller, CrownKeeper, Boar }
    public enum EnemyMove { Strike, Brace, Crush, TwinCut, Feint, Bolt, Bind, GroundSpark }

    public readonly struct EnemyIntent
    {
        public readonly EnemyMove Move;
        public readonly int Power;
        public int Hits => Move == EnemyMove.TwinCut ? 2 : 1;
        public int Pierce => Move == EnemyMove.Feint ? 2 : 0;
        public bool HitsAir => Move == EnemyMove.Bolt;
        public string Name => Move switch {
            EnemyMove.Brace => "架盾", EnemyMove.Crush => "重砸", EnemyMove.TwinCut => "双刃连袭",
            EnemyMove.Feint => "绕盾刺", EnemyMove.Bolt => "追空雷印", EnemyMove.Bind => "锁链缚足", EnemyMove.GroundSpark => "地脉电弧", _ => "突击"
        };
        public string Description => Move switch {
            EnemyMove.Brace => $"下回合获得 {Power} 护甲",
            EnemyMove.TwinCut => $"{Power} × 2 伤害",
            EnemyMove.Feint => $"{Power} 伤害，忽略 {Pierce} 格挡",
            EnemyMove.Bolt => $"{Power} 伤害，可命中空中目标",
            EnemyMove.Bind => $"{Power} 伤害，受伤后下回合少出一张牌",
            _ => $"{Power} 伤害"
        };
        internal EnemyIntent(EnemyMove move,int power) { Move=move; Power=power; }
        public int Wound(int guard,bool airborne,bool frozen)
        {
            if (frozen || Move==EnemyMove.Brace || (airborne && !HitsAir)) return 0;
            return Math.Max(0,Power*Hits-Math.Max(0,guard-Pierce));
        }
    }

    public static class EnemyTactics
    {
        public static string Name(this EnemyKind kind) => kind switch {
            EnemyKind.Boar=>"林地野猪", EnemyKind.IronWarden=>"铁壁卫士", EnemyKind.KnifeRunner=>"灰巷双刃", EnemyKind.StormCaller=>"逐雷术士", _=>"锁冠守关者"
        };

        internal static EnemyIntent Plan(EnemyKind kind,int power,int turn,EnemyMove last,bool completed,
            bool lostHealth,bool defended,bool flew,bool enraged)
        {
            if(turn==1)
            {
                if(kind==EnemyKind.KnifeRunner && power>=2) return new EnemyIntent(EnemyMove.TwinCut,(power+1)/2);
                if(kind==EnemyKind.StormCaller) return new EnemyIntent(EnemyMove.GroundSpark,power);
                return new EnemyIntent(EnemyMove.Strike,power);
            }
            switch(kind)
            {
                case EnemyKind.Boar:
                    return new EnemyIntent(EnemyMove.Strike, turn % 3 == 0 ? power + 1 : power);
                case EnemyKind.IronWarden:
                    if(last==EnemyMove.Brace && completed) return new EnemyIntent(EnemyMove.Crush,power+3);
                    if(last!=EnemyMove.Brace && (lostHealth || turn%3==0)) return new EnemyIntent(EnemyMove.Brace,power);
                    break;
                case EnemyKind.KnifeRunner:
                    if(defended && last!=EnemyMove.Feint) return new EnemyIntent(EnemyMove.Feint,power);
                    if(last!=EnemyMove.TwinCut) return new EnemyIntent(EnemyMove.TwinCut,Math.Max(1,(power+1)/2+1));
                    break;
                case EnemyKind.StormCaller:
                    if(flew || turn%2==0) return new EnemyIntent(EnemyMove.Bolt,power+1);
                    return new EnemyIntent(EnemyMove.GroundSpark,power);
                case EnemyKind.CrownKeeper:
                    if(turn%2==0) return new EnemyIntent(EnemyMove.Bind,power);
                    if(enraged) return new EnemyIntent(EnemyMove.Crush,power+3);
                    break;
            }
            return new EnemyIntent(EnemyMove.Strike,power);
        }
    }
}
