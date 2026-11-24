using System;
using System.Collections.Generic;

namespace Rouge.Core
{
    public sealed class RouteNode
    {
        private readonly List<int> exits = new List<int>();
        public int Id { get; }
        public int Layer { get; }
        public int Lane { get; }
        public RoomKind Kind { get; }
        public int OnwardRegion { get; }
        public string Trail { get; }
        public IReadOnlyList<int> Exits { get; }

        internal RouteNode(int id, int layer, int lane, RoomKind kind, int onwardRegion, string trail)
        {
            Id = id;
            Layer = layer;
            Lane = lane;
            Kind = kind;
            OnwardRegion = onwardRegion;
            Trail = trail;
            Exits = exits.AsReadOnly();
        }

        internal void Connect(RouteNode target) => exits.Add(target.Id);
    }

    public sealed class RouteMap
    {
        private readonly List<RouteNode> nodes = new List<RouteNode>();
        public IReadOnlyList<RouteNode> Nodes { get; }
        public TerrainGrid Terrain { get; }
        public int Region { get; }
        private readonly Dictionary<(int, int, Talent), IReadOnlyList<int>> paths = new Dictionary<(int, int, Talent), IReadOnlyList<int>>();
        private readonly List<int> locations = new List<int>();
        private readonly Random random;
        private readonly int forkLayer;
        public int Cell(RouteNode node) => locations[node.Id];

        public string Landmark(RouteNode node)
        {
            if (node.Kind == RoomKind.Passage || (node.Trail != null && node.Layer == forkLayer))
                return node.Trail + " → " + WorldAtlas.Names[node.OnwardRegion];
            if (Region == WorldAtlas.Highland && node.Layer == 0) return "崖顶营地";
            return null;
        }

        public IReadOnlyList<int> Path(RouteNode from, int target, Talent talent)
        {
            var key = (from.Id, target, talent);
            if (!paths.TryGetValue(key, out var path))
            {
                int start = Cell(from), end = Cell(nodes[target]);
                if (Region == WorldAtlas.Highland && from.Kind == RoomKind.Camp && nodes[target].Lane > 0)
                {
                    // Approach from the west abutment, including when the traveller can fly.
                    int abutment = Terrain.Cell(9,12);
                    var crossing = new List<int>(Terrain.FindPath(start, abutment, talent));
                    crossing.RemoveAt(crossing.Count - 1);
                    crossing.AddRange(Terrain.FindPath(abutment, end, talent));
                    path = crossing.AsReadOnly();
                }
                else path = Array.AsReadOnly(Terrain.FindPath(start, end, talent));
                paths.Add(key, path);
            }
            return path;
        }

        internal RouteMap(int layers, Random random, int region = 0)
        {
            this.random = random;
            Nodes = nodes.AsReadOnly();
            Region = region;
            Terrain = new TerrainGrid(layers, random, region);
            var roads = Array.FindAll(WorldAtlas.Roads, road => road.From == region);
            forkLayer = region == WorldAtlas.Highland ? 1 : Math.Max(1, (layers + 1) / 2);
            var previous = new List<RouteNode> { Add(0, 0, RoomKind.Camp) };
            for (int layer = 1; layer <= layers; layer++)
            {
                if (roads.Length > 0 && layer == forkLayer)
                {
                    bool battleLeft = random.Next(2) == 0;
                    for (int side = 0; side < roads.Length; side++)
                    {
                        var road = roads[side];
                        int exitLayer = side == 0 ? Math.Min(layers + 1, forkLayer + 2) : layers + 1;
                        BuildTrail(previous, road.To, road.Trail, side == 0 ? -1 : 1,
                            exitLayer, (side == 0) == battleLeft ? RoomKind.Battle : RoomKind.Event);
                    }
                    return;
                }
                RoomKind special = layer % 3 == 1 ? RoomKind.Event : RoomKind.Shop;
                if (layer % 3 == 0)
                    special = random.Next(2) == 0 ? RoomKind.Event : RoomKind.Shop;
                bool battleFirst = random.Next(2) == 0;
                var next = new List<RouteNode> {
                    Add(layer, -random.Next(2,6), battleFirst ? RoomKind.Battle : special),
                    Add(layer, random.Next(2,6), battleFirst ? special : RoomKind.Battle)
                };
                if (layer > 1 && random.Next(2) == 0)
                    next.Add(Add(layer, random.Next(-1,2), random.Next(2) == 0 ? RoomKind.Shop : RoomKind.Event));
                if (previous.Count == 1)
                {
                    previous[0].Connect(next[0]);
                    previous[0].Connect(next[1]);
                }
                else
                {
                    previous[0].Connect(next[0]);
                    previous[1].Connect(next[1]);
                    int crossing = random.Next(2);
                    previous[crossing].Connect(next[1 - crossing]);
                    if (next.Count == 3) previous[random.Next(2)].Connect(next[2]);
                    if (previous.Count == 3) previous[2].Connect(next[random.Next(next.Count)]);
                }
                previous = next;
            }
            var boss = Add(layers + 1, 0, RoomKind.Boss);
            foreach (var node in previous) node.Connect(boss);
        }

        private void BuildTrail(List<RouteNode> approaches, int destination, string trail, int side,
            int exitLayer, RoomKind firstRoom)
        {
            var previous = approaches;
            for (int layer = forkLayer; layer <= exitLayer; layer++)
            {
                RoomKind kind = layer == exitLayer ? RoomKind.Passage :
                    layer == forkLayer ? firstRoom : (RoomKind)random.Next((int)RoomKind.Battle, (int)RoomKind.Event + 1);
                var main = Add(layer, side * random.Next(3,6), kind, destination, trail);
                foreach (var node in previous) node.Connect(main);
                var next = new List<RouteNode> { main };
                if (layer > forkLayer && layer < exitLayer && random.Next(2) == 0)
                {
                    var detour = Add(layer, side, kind == RoomKind.Shop ? RoomKind.Battle : RoomKind.Shop, destination, trail);
                    foreach (var node in previous) node.Connect(detour);
                    next.Add(detour);
                }
                previous = next;
            }
        }

        private RouteNode Add(int layer, int lane, RoomKind kind, int destination = -1, string trail = null)
        {
            var node = new RouteNode(nodes.Count, layer, lane, kind, destination, trail);
            locations.Add(Terrain.PlaceNode(layer,lane,random));
            nodes.Add(node);
            return node;
        }
    }
}
