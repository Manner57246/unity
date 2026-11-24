namespace Rouge.Core
{
    public enum Element { None, Fire, Ice, Water, Wind, Earth, Lightning }

    public sealed class Reaction
    {
        public readonly string Name;
        public readonly int Damage, Guard, Draw;
        public readonly bool Freeze;
        private Reaction(string name,int damage=0,int guard=0,int draw=0,bool freeze=false)
        {
            Name=name; Damage=damage; Guard=guard; Draw=draw; Freeze=freeze;
        }
        private static readonly Reaction Frost = new("冻结",freeze:true);
        private static readonly Reaction Arc = new("导电",damage:5);
        private static readonly Reaction Shatter = new("碎冰",damage:5);
        private static readonly Reaction Magma = new("熔岩",damage:4);
        private static readonly Reaction Wildfire = new("烈焰风暴",damage:4);
        private static readonly Reaction Storm = new("雷暴",damage:2,draw:1);
        private static readonly Reaction Ward = new("潮汐护幕",guard:4);
        private static readonly Reaction FlameBlade = new("焰刃",damage:3);

        public static Reaction For(Element previous,CardRule next)
        {
            if (previous==Element.Fire && next.IsSlash) return FlameBlade;
            return (previous,next.School) switch {
                (Element.Water,Element.Ice) => Frost,
                (Element.Water,Element.Lightning) => Arc,
                (Element.Ice,Element.Earth) => Shatter,
                (Element.Earth,Element.Fire) => Magma,
                (Element.Fire,Element.Wind) => Wildfire,
                (Element.Wind,Element.Lightning) => Storm,
                (Element.Lightning,Element.Water) => Ward,
                _ => null
            };
        }

        public string Description => Freeze ? "跳过本回合敌方行动" :
            Guard>0 ? $"格挡 +{Guard}" : Draw>0 ? $"伤害 +{Damage}，抽 {Draw} 张" : $"伤害 +{Damage}";

        public static string School(Element element) => element switch {
            Element.Fire=>"火",Element.Ice=>"冰",Element.Water=>"水",Element.Wind=>"风",
            Element.Earth=>"土",Element.Lightning=>"电",_=>""
        };
    }
}
