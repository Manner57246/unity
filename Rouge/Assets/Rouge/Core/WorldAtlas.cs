using System;
using System.Collections.Generic;

namespace Rouge.Core
{
    public enum TerrainKind { Meadow, Forest, Lake, Cliff }
    public enum Talent { Pathfinder, Ferryman, Climber, Wings }
    public enum Loadout { Provisions, Purse }

    public sealed class WorldAtlas
    {
        public const int Highland = 1, Mine = 5, Forge = 6;
        public static readonly string[] Names = { "镜湖湿地", "断脊高原", "苍松河谷", "灰烬要塞", "星陨圣堂", "崖脚矿道", "熔心王座" };
        private readonly HashSet<int> cleared = new HashSet<int>();
        // -1 is the only initial camp. The two first expeditions open the outer marches.
        public static readonly (int From, int To, string Trail)[] Roads = {
            (-1,0,"湖滨小径"), (-1,Highland,"高原古道"),
            (0,2,"溯河入林"), (0,Mine,"排水暗渠"),
            (Highland,Mine,"沿坡下崖"), (Highland,3,"跨过石桥"),
            (Mine,2,"通风井梯"), (Mine,3,"运矿甬道"),
            (2,3,"军需栈道"), (2,4,"朝星古径"),
            (3,4,"北门朝圣路"), (3,Forge,"炉底升降井")
        };
        public int LastSite { get; private set; } = -1;
        public bool IsCleared(int site) => cleared.Contains(site);
        public bool Available(int site)
        {
            if (site < 0 || site >= Names.Length || IsCleared(site)) return false;
            foreach (var road in Roads)
                if (road.To == site && road.From == LastSite) return true;
            return false;
        }
        public bool CompleteSite(int site)
        {
            if (!Available(site)) return false;
            cleared.Add(site);
            LastSite = site;
            return true;
        }
        public IReadOnlyList<RouteMap> Regions { get; }
        public WorldAtlas(int seed, int layers)
        {
            var random = new Random(seed);
            var regions = new List<RouteMap>();
            for (int i = 0; i < Names.Length; i++)
                regions.Add(new RouteMap(layers, new Random(random.Next()), i));
            Regions = regions.AsReadOnly();
        }
    }

    public sealed class TerrainGrid
    {
        public int Width { get; }
        public const int Height = 29;
        private readonly TerrainKind[] cells;
        private readonly bool[] reserved;
        private readonly int layers, region;
        public TerrainKind this[int cell] => cells[cell];
        public int Cell(int x, int y) => y * Width + x;

        internal TerrainGrid(int layers, Random random, int region)
        {
            this.layers = layers;
            this.region = region;
            Width = 29;
            cells = new TerrainKind[Width * Height];
            reserved = new bool[cells.Length];
            double bend = random.NextDouble() * 2;
            int tide = random.Next(-2, 3);
            for (int y = 1; y < Height - 1; y++)
            for (int x = 1; x < Width - 1; x++)
            {
                double channel = 14 + 2.2 * Math.Sin(y * .19 + bend);
                double lake = Math.Pow((x - 14 - tide) / 6.4, 2) + Math.Pow((y - 15) / 8.5, 2);
                var cell = Cell(x,y);
                bool trees = Math.Sin(x * .48 + bend) + Math.Cos(y * .48 - bend) + random.NextDouble() > (region == 2 ? .1 : 1.35);
                cells[cell] = trees ? TerrainKind.Forest : TerrainKind.Meadow;
                switch (region)
                {
                    case 0:
                        if (lake < 1 + .12 * Math.Sin(y * .8)) cells[cell] = TerrainKind.Lake;
                        break;
                    case 1:
                        if (Math.Abs(x - channel) < 2.2 && y > 3 && y != 12) cells[cell] = TerrainKind.Cliff;
                        break;
                    case 2:
                        if (Math.Abs(x - channel) < 1.65 && y != 9 && y != 20) cells[cell] = TerrainKind.Lake;
                        break;
                    case 3:
                        if (Math.Abs(y - 12 - Math.Sin(x * .32 + bend) * 3) < 1.8 && x > 5 && x < 23) cells[cell] = TerrainKind.Cliff;
                        break;
                    case WorldAtlas.Mine:
                        cells[cell] = Math.Abs(x - channel) < 2.5 && y != 8 && y != 19
                            ? TerrainKind.Cliff : TerrainKind.Meadow;
                        break;
                    case WorldAtlas.Forge:
                        if (Math.Abs(y - 15 - 4 * Math.Sin(x * .3 + bend)) < 1.6 && x % 11 > 2)
                            cells[cell] = TerrainKind.Cliff;
                        break;
                    default:
                        if (lake > .7 && lake < 1.65 && Math.Abs(x - 14) > 2) cells[cell] = TerrainKind.Cliff;
                        break;
                }
            }
        }

        internal int PlaceNode(int layer, int lane, Random random)
        {
            // Layers determine encounter order. Their positions follow the feature in two dimensions.
            double progress = (double)layer / (layers + 1);
            double x = 14 + lane * 1.65 + 2.3 * Math.Sin(progress * 5);
            double y = 24 - 20 * progress;
            if (region == 0) { x = 14 + lane * 1.9; y = 4 + 20 * progress; }
            if (region == 3) { double swap = x; x = y; y = swap; }
            if (layer != 0 && layer <= layers) { x += random.Next(-1,2); y += random.Next(-1,2); }
            if (region == 1 && layer == 0) { x = 9; y = 23; }
            if (region == WorldAtlas.Highland && layer > 0)
            {
                double t = (double)(layer - 1) / layers;
                x = lane < 0 ? 7 + lane * .32 : 19 + lane * .5;
                y = lane < 0 ? 14 - 10 * (layer - 1d) / Math.Min(2,layers) : 12 + 12 * t;
                if (layer == 1) { x = lane < 0 ? 7 : 20; y = lane < 0 ? 14 : 12; }
            }
            int nearest = -1;
            double best = double.MaxValue;
            for (int cy = 1; cy < Height-1; cy++)
            for (int cx = 1; cx < Width-1; cx++)
            {
                int cell = Cell(cx,cy);
                if (reserved[cell] || !Passable(cell,Talent.Pathfinder)) continue;
                double distance = (x-cx)*(x-cx) + (y-cy)*(y-cy);
                if (distance >= best) continue;
                nearest = cell;
                best = distance;
            }
            reserved[nearest] = true;
            return nearest;
        }

        public bool Passable(int cell, Talent talent) =>
            talent == Talent.Wings || (cells[cell] != TerrainKind.Lake || talent == Talent.Ferryman) &&
            (cells[cell] != TerrainKind.Cliff || talent == Talent.Climber);

        internal int[] FindPath(int start, int goal, Talent talent)
        {

            var parents = new int[cells.Length];
            Array.Fill(parents, -1);
            parents[start] = start;
            var queue = new Queue<int>();
            queue.Enqueue(start);
            while (queue.Count > 0 && parents[goal] == -1)
            {
                int cell = queue.Dequeue();
                int x = cell % Width, y = cell / Width;
                if (x < Width - 1) Try(cell + 1, cell);
                if (y < Height - 1) Try(cell + Width, cell);
                if (y > 0) Try(cell - Width, cell);
                if (x > 0) Try(cell - 1, cell);
            }
            if (parents[goal] < 0) throw new InvalidOperationException("Disconnected waypoint.");
            var steps = new List<int> { goal };
            while (steps[steps.Count - 1] != start) steps.Add(parents[steps[steps.Count - 1]]);
            steps.Reverse();
            return steps.ToArray();

            void Try(int next, int parent)
            {
                if (parents[next] >= 0 || !Passable(next, talent)) return;
                parents[next] = parent;
                queue.Enqueue(next);
            }
        }
    }
}
