using System;
using System.Collections.Generic;
using Rouge.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace Rouge.Unity
{
    // All positions are normalised coordinates on the parchment, top-left origin.
    internal sealed class RouteBoard : IDisposable
    {
        internal static Vector2[] Sites => Sandtable.Sites;
        private static Vector2 Camp => Sandtable.Camp;
        internal readonly WorldAtlas Atlas;
        private readonly Sandtable table;
        internal Texture2D SurveyImage => table.Picture;
        private Texture2D fog;
        private Color32[] mist;
        private int[] fogCell;
        private int fadedIndex = -2;
        private bool fading;
        private RunSession surveyed;
        internal Vector2 Position { get; private set; }
        internal Rect View { get; private set; }
        private VisualElement localLayer, miniLayer, marker, border, miniMarker;
        private VisualElement window, tip;
        private VisualElement localRoads, miniRoads;
        private int highlighted = -1;
        private readonly Dictionary<RoomKind, Texture2D> miniatures = new();
        private readonly List<(VisualElement Pin,Vector2 Position)> pins = new();
        private readonly List<VisualElement> overviewMarkers = new();

        private readonly ScoutFrames scout;
        internal RouteBoard(WorldAtlas atlas, ScoutFrames scout, int seed)
        {
            Atlas=atlas;
            this.scout=scout;
            table = new Sandtable(atlas,seed);
            foreach (RoomKind kind in Enum.GetValues(typeof(RoomKind)))
                miniatures[kind] = Resources.Load<Texture2D>("Waypoints/" + kind);
        }

        private Vector2 GridPosition(RouteMap map,int cell) => table.Node(map,cell);
        private Vector2 PathPosition(RouteMap map,IReadOnlyList<int> path,int index,Talent talent)
        {
            if(talent!=Talent.Wings || map.Terrain.Passable(path[index],Talent.Pathfinder))return GridPosition(map,path[index]);
            int before=index,after=index;
            while(before>0 && !map.Terrain.Passable(path[before],Talent.Pathfinder))before--;
            while(after<path.Count-1 && !map.Terrain.Passable(path[after],Talent.Pathfinder))after++;
            return Vector2.Lerp(GridPosition(map,path[before]),GridPosition(map,path[after]),(float)(index-before)/(after-before));
        }

        private static Rect FogBounds(RouteMap map)
        {
            var bounds=Sandtable.Bounds(map.Region);
            return Rect.MinMaxRect(bounds.xMin,bounds.yMin-.06f,bounds.xMax,bounds.yMax);
        }
        private static Rect Focus(RouteMap map)
        {
            var bounds=FogBounds(map);
            return new Rect(bounds.center-new Vector2(.143f,.188f),new Vector2(.286f,.376f));
        }

        internal void BindRun(RunSession run)
        {
            if(surveyed==run)return;
            surveyed=run;
            if(fog!=null)UnityEngine.Object.Destroy(fog);
            int w=run.Map.Terrain.Width,h=TerrainGrid.Height;
            const int samples=4;
            int fw=w*samples,fh=h*samples;
            fog=new Texture2D(fw,fh,TextureFormat.RGBA32,false){wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};
            mist=new Color32[fw*fh];fogCell=new int[mist.Length];
            for(int y=0;y<fh;y++) for(int x=0;x<fw;x++)
            {
                float cloud=Mathf.PerlinNoise(x*.075f,y*.085f);
                int cx=Mathf.Clamp(Mathf.FloorToInt((x+.5f+(cloud-.5f)*2)/samples),0,w-1);
                int cy=Mathf.Clamp(Mathf.FloorToInt((y+.5f+(Mathf.PerlinNoise(y*.1f,x*.08f)-.5f)*2)/samples),0,h-1);
                var province=Sandtable.Bounds(run.Map.Region);
                var mask=FogBounds(run.Map);
                Vector2 projected=mask.min+Vector2.Scale(mask.size,new Vector2((cx+.5f)/w,1-(cy+.5f)/h));
                Vector2 ground=projected;
                for(int j=0;j<3;j++)ground.y += projected.y-table.Project(ground).y;
                int row=Mathf.Clamp((int)((province.yMax-ground.y)/province.height*h),0,h-1);
                int i=y*fw+x;fogCell[i]=row*w+cx;
                float edge=Mathf.Min(Mathf.Min(x,fw-1-x),Mathf.Min(y,fh-1-y));
                float feather=Mathf.SmoothStep(0,1,(edge+(cloud-.5f)*samples*5)/(samples*5f));
                mist[i]=new Color32((byte)(48+cloud*26),(byte)(58+cloud*27),(byte)(60+cloud*25),
                    run.IsCharted(fogCell[i])?(byte)0:(byte)((103+cloud*45)*feather));
            }
            fading=false;fadedIndex=-2;
            fog.SetPixels32(mist);fog.Apply(false);
        }

        private static VisualElement Image(VisualElement parent,Texture texture,Rect rect)
        {
            var image=new VisualElement{pickingMode=PickingMode.Ignore};
            image.style.backgroundImage=(Texture2D)texture;
            image.style.backgroundSize=new BackgroundSize(Length.Percent(100),Length.Percent(100));
            Place(image,rect); parent.Add(image);return image;
        }
        private static void Place(VisualElement item,Rect rect)
        {
            item.style.position=UnityEngine.UIElements.Position.Absolute;
            item.style.left=Length.Percent(rect.x*100); item.style.top=Length.Percent(rect.y*100);
            item.style.width=Length.Percent(rect.width*100);item.style.height=Length.Percent(rect.height*100);
        }
        private static void At(VisualElement item,Vector2 uv)
        {
            item.style.left=Length.Percent(100*uv.x);item.style.top=Length.Percent(100*uv.y);
        }

        private VisualElement Paper(VisualElement parent,RunSession run)
        {
            var layer=Image(parent,table.Picture,new Rect(0,0,1,1));
            if(run!=null) Image(layer,fog,FogBounds(run.Map));
            return layer;
        }

        internal void Overview(VisualElement host,Action<int> enter)
        {
            window=host;pins.Clear();overviewMarkers.Clear();
            localLayer=Paper(host,surveyed);
            DrawRoads(localLayer,null);
            AddPin(Camp,"余烬营地",-1);
            for(int i=0;i<Sites.Length;i++)
                AddPin(Sites[i],WorldAtlas.Names[i],i);
            void AddPin(Vector2 point,string text,int site)
            {
                var pin=new Button(()=>{if(site>=0 && Atlas.Available(site))enter?.Invoke(site);}){text=text};
                pin.AddToClassList("map-pin");if(site<0 || !Atlas.Available(site))pin.AddToClassList("surveyed");
                if(site>=0 && !Atlas.Available(site)) pin.pickingMode=PickingMode.Ignore;
                host.Add(pin);At(pin,table.Project(point));overviewMarkers.Add(pin);
            }
        }

        internal void Zoom(int region,float t)
        {
            if (surveyed != null)
            {
                var focus = Focus(surveyed.Map);
                var size = focus.size * Mathf.Lerp(1,.76f,t);
                View = new Rect(Vector2.Lerp(focus.center,Position,t) - size*.5f,size);
                Align();
                return;
            }
            var map=Atlas.Regions[region];
            var dest=Focus(map);
            var rect=new Rect(Vector2.Lerp(Vector2.zero,dest.min,t),Vector2.Lerp(Vector2.one,dest.size,t));
            Crop(localLayer,rect);
            foreach(var p in overviewMarkers)p.style.opacity=1-t;
        }

        private static void Crop(VisualElement layer,Rect r) =>
            Place(layer,new Rect(-r.x/r.width,-r.y/r.height,1/r.width,1/r.height));

        internal void Local(VisualElement host,RunSession run,Action<int> travel)
        {
            BindRun(run); pins.Clear();tip=null;highlighted=-1;
            var layout=host; layout.AddToClassList("region-layout");
            host=new VisualElement(); host.AddToClassList("region-map"); layout.Add(host);window=host;
            int start=run.Phase==RunPhase.Travelling ? run.Journey[run.JourneyIndex] : run.Map.Cell(run.Current);
            Position=GridPosition(run.Map,start); View=Focus(run.Map);
            localLayer=Paper(host,run);localRoads=DrawRoads(localLayer,run);
            foreach(var node in run.Map.Nodes)
            {
                if(!run.IsRevealed(node.Id))continue;
                bool next=false;foreach(int id in run.Current.Exits) if(id==node.Id) next=true;
                var pin=new VisualElement{name="node-"+node.Id};
                pin.AddToClassList("waypoint"); if(!next)pin.AddToClassList("past-node");
                pin.style.backgroundImage=miniatures[node.Kind];
                if(next && run.Phase==RunPhase.Map && !run.CanTravel(node.Id))pin.AddToClassList("out-of-range");
                string landmark=run.Map.Landmark(node);
                if(next && landmark!=null)
                {
                    var caption=new Label(WorldAtlas.Names[node.OnwardRegion]){pickingMode=PickingMode.Ignore};
                    caption.AddToClassList("landmark-caption");pin.Add(caption);
                }
                if(next)host.Add(pin); else host.Insert(1,pin);
                pins.Add((pin,GridPosition(run.Map,run.Map.Cell(node))));
                if(node==run.Current){pin.pickingMode=PickingMode.Ignore;continue;}
                if(run.Phase!=RunPhase.Map)continue;
                bool reachable=next;
                pin.RegisterCallback<PointerEnterEvent>(_=>Info(pin,run,node,reachable));
                pin.RegisterCallback<PointerLeaveEvent>(_=>HideTip());
                pin.RegisterCallback<ClickEvent>(evt=>{
                    if(evt.button!=0)return;
                    if(evt.clickCount==2 && reachable && run.CanTravel(node.Id)){HideTip();travel(node.Id);}
                    else Info(pin,run,node,reachable);
                    evt.StopPropagation();
                });
            }
            marker=new VisualElement{name="traveller",pickingMode=PickingMode.Ignore};marker.AddToClassList("walker");
            marker.style.backgroundImage=Background.FromSprite(scout.FrameAt(0));host.Add(marker);

            var sidebar=new VisualElement();sidebar.AddToClassList("region-journal");layout.Add(sidebar);
            void Note(string text,string style)
            {
                var label=new Label(text);label.AddToClassList(style);sidebar.Add(label);
            }
            Note("大陆方位","journal-heading");
            var mini=new VisualElement{pickingMode=PickingMode.Ignore};mini.AddToClassList("mini-map");sidebar.Add(mini);
            miniLayer=Paper(mini,run);miniRoads=DrawRoads(miniLayer,run,true);
            border=new VisualElement{pickingMode=PickingMode.Ignore};border.AddToClassList("view-frame");mini.Add(border);
            miniMarker=new VisualElement{pickingMode=PickingMode.Ignore};miniMarker.AddToClassList("locator");mini.Add(miniMarker);
            Note(run.Current.OnwardRegion >= 0 ? "前往 " + WorldAtlas.Names[run.Current.OnwardRegion] :
                run.Map.Landmark(run.Current) ?? WorldAtlas.Names[run.Map.Region],"journal-heading");
            foreach(int id in run.Current.Exits)
            {
                var destination=run.Map.Nodes[id];
                var row=new VisualElement(); row.AddToClassList("trail-row"); sidebar.Add(row);
                var icon=new VisualElement{pickingMode=PickingMode.Ignore}; icon.AddToClassList("trail-icon");
                icon.style.backgroundImage=miniatures[destination.Kind]; row.Add(icon);
                var copy=new VisualElement();copy.AddToClassList("trail-copy");row.Add(copy);
                string landmark=run.Map.Landmark(destination);
                copy.Add(new Label(landmark==null ? RoomName(run,destination) : destination.Trail){pickingMode=PickingMode.Ignore});
                var detail=new Label((landmark==null ? "" : "至"+WorldAtlas.Names[destination.OnwardRegion]+"  ·  ")+run.TravelCost(id)+" 移动点"){pickingMode=PickingMode.Ignore};
                detail.AddToClassList("trail-cost");copy.Add(detail);
                if(run.Phase==RunPhase.Map){
                    row.RegisterCallback<PointerEnterEvent>(_=>Highlight(id));
                    row.RegisterCallback<PointerLeaveEvent>(_=>Highlight(-1));
                    row.RegisterCallback<ClickEvent>(evt=>{if(evt.button==0 && evt.clickCount==2 && run.CanTravel(id))travel(id);});
                }
            }
            Align();
        }

        private static string RoomName(RunSession run,RouteNode node) => node.Kind switch { RoomKind.Camp=>"营地",RoomKind.Battle or RoomKind.Boss=>run.EnemyAt(node).Name(),RoomKind.Shop=>"行商营地",RoomKind.Passage=>"区域出口",_=>"未知事件" };
        private void Highlight(int id)
        {
            if (highlighted == id) return;
            highlighted=id;
            localRoads?.MarkDirtyRepaint(); miniRoads?.MarkDirtyRepaint();
        }
        private void HideTip(){tip?.RemoveFromHierarchy();tip=null;Highlight(-1);}
        private void Info(VisualElement pin,RunSession run,RouteNode node,bool next)
        {
            HideTip();
            if(next) Highlight(node.Id);
            tip=new VisualElement{name="node-info",pickingMode=PickingMode.Ignore};tip.AddToClassList("node-info");
            string name=node.Trail != null ? node.Trail+" · "+RoomName(run,node) : RoomName(run,node);
            string detail=node.Kind switch{RoomKind.Battle=>"进入战斗，胜利获得战利品。",RoomKind.Shop=>"花费金币购买稀有卡牌。",RoomKind.Boss=>"击败守关者，完成远征。",RoomKind.Passage=>"抵达即进入下一地区。",_=>"探索当地奇遇，选择风险与回报。"};
            if(node.OnwardRegion >= 0) detail += "\n路线终点  " + WorldAtlas.Names[node.OnwardRegion];
            string cost=next ? $"移动点  {run.TravelCost(node.Id)}   ·   "+(run.CanTravel(node.Id)?"双击前往":"需要整备") : run.IsVisited(node.Id)?"已经探索":"无法从当前位置前往";
            var heading=new Label(name){pickingMode=PickingMode.Ignore};heading.AddToClassList("node-heading");tip.Add(heading);
            foreach(string line in new[]{detail,cost})tip.Add(new Label(line){pickingMode=PickingMode.Ignore});
            window.Add(tip);
            var p=window.WorldToLocal(pin.worldBound.center);
            tip.style.left=Mathf.Clamp(p.x+24,4,Mathf.Max(4,window.contentRect.width-249));
            tip.style.top=Mathf.Clamp(p.y-40,4,Mathf.Max(4,window.contentRect.height-115));
        }

        private VisualElement DrawRoads(VisualElement layer,RunSession run,bool miniature=false)
        {
            var ink=new VisualElement{pickingMode=PickingMode.Ignore};Place(ink,new Rect(0,0,1,1));layer.Add(ink);
            ink.generateVisualContent+=ctx=>{
                var pen=ctx.painter2D;var area=ink.contentRect.size;
                if(run==null){
                    foreach(var road in WorldAtlas.Roads){
                        if(road.From>=0&&!Atlas.IsCleared(road.From))continue;
                        Vector2 a=table.Project(road.From<0?Camp:Sites[road.From]),b=table.Project(Sites[road.To]);
                        var bend=new Vector2((b.y-a.y)*.12f,(a.x-b.x)*.12f);
                        Vector2 Along(float t)=>Vector2.Lerp(a,b,t)+bend*Mathf.Sin(Mathf.PI*t);
                        for(int i=0;i<24;i+=2) Stroke(Along(i/24f),Along((i+1.3f)/24f),new Color(.30f,.23f,.17f,.86f),2.7f);
                    }
                }else{
                    foreach(var node in run.Map.Nodes){
                        if(!run.IsVisited(node.Id))continue;
                        foreach(int id in node.Exits){
                            if(node!=run.Current&&!run.IsVisited(id))continue;
                            if(node==run.Current && (id==highlighted || id==run.Destination))continue;
                            Trail(node,id,false);
                        }
                    }
                    int focus=run.Phase==RunPhase.Travelling ? run.Destination : highlighted;
                    if(focus>=0) Trail(run.Current,focus,true);
                }
                void Trail(RouteNode from,int id,bool selected)
                {
                    var path=run.Map.Path(from,id,run.Talent);
                    bool active=from==run.Current;
                    float width=miniature ? 1.3f : Mathf.Clamp(area.x*.0024f,4,8);
                    Vector2 a=PathPosition(run.Map,path,0,run.Talent);
                    pen.lineCap=LineCap.Round;
                    for(int j=1;j<path.Count;j++){
                        Vector2 b=PathPosition(run.Map,path,j,run.Talent);
                        bool crossing=!run.Map.Terrain.Passable(path[j],Talent.Pathfinder) || !run.Map.Terrain.Passable(path[j-1],Talent.Pathfinder);
                        if(miniature) Stroke(a,b,selected?new Color(1,.85f,.48f):new Color(.44f,.34f,.22f),width);
                        else if(crossing){
                            Stroke(Vector2.Lerp(a,b,.2f),Vector2.Lerp(a,b,.63f),selected?new Color(1,.88f,.57f):new Color(.80f,.85f,.77f,.8f),width*.54f);
                        }else{
                            Stroke(a,b,new Color(.30f,.23f,.17f,active?.64f:.35f),width);
                            Stroke(a,b,selected?new Color(.94f,.75f,.44f):new Color(.67f,.58f,.43f,active?.95f:.52f),width*.66f);
                            if(active)Stroke(Vector2.Lerp(a,b,.42f),Vector2.Lerp(a,b,.60f),selected?new Color(1,.91f,.64f):new Color(.83f,.75f,.58f),width*.50f);
                        }
                        a=b;
                    }
                }
                void Stroke(Vector2 a,Vector2 b,Color c,float width){pen.strokeColor=c;pen.lineWidth=width;pen.BeginPath();pen.MoveTo(Vector2.Scale(a,area));pen.LineTo(Vector2.Scale(b,area));pen.Stroke();}
            };
            return ink;
        }

        internal void WalkFrame(int a,int b,float t)
        {
            var path=surveyed.Journey;int i=surveyed.JourneyIndex;
            Position=Vector2.Lerp(PathPosition(surveyed.Map,path,i,surveyed.Talent),PathPosition(surveyed.Map,path,i+1,surveyed.Talent),t);
            marker.style.backgroundImage=Background.FromSprite(scout.Loop("Walk",Time.unscaledTime));
            int dx=b%surveyed.Map.Terrain.Width-a%surveyed.Map.Terrain.Width;
            if(dx!=0) marker.style.scale=new Scale(new Vector3(Mathf.Sign(dx),1,1));
            View=Focus(surveyed.Map);
            Align();
        }
        internal void FadeFog(float dt)
        {
            if(surveyed==null)return;
            int cursor=surveyed.Phase==RunPhase.Travelling ? surveyed.Journey[surveyed.JourneyIndex] : surveyed.Map.Cell(surveyed.Current);
            if(!fading && cursor==fadedIndex)return;
            fadedIndex=cursor;
            bool dirty=false;fading=false;
            for(int i=0;i<mist.Length;i++){
                if(mist[i].a==0||!surveyed.IsCharted(fogCell[i]))continue;
                mist[i].a=(byte)Mathf.Max(0,mist[i].a-Mathf.CeilToInt(dt*430));
                dirty=true;
                if(mist[i].a>0)fading=true;
            }
            if(dirty){fog.SetPixels32(mist);fog.Apply(false);}
        }
        private void Align()
        {
            Crop(localLayer,View);
            foreach(var pin in pins)At(pin.Pin,(pin.Position - View.min) / View.size);
            At(marker,(Position - View.min) / View.size); At(miniMarker,Position); Place(border,View);
        }
        public void Dispose()
        {
            table.Dispose();
            if(fog!=null)UnityEngine.Object.Destroy(fog);
        }
    }
}
