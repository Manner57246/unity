using System;
using System.Collections.Generic;
using Rouge.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace Rouge.Unity
{
    internal sealed partial class ExpeditionHud
    {
        private readonly Action pause,home;
        private readonly IReadOnlyList<string> itinerary;
        internal VisualElement Overlay { get; private set; }

        private VisualElement Cover()
        {
            root.Clear();
            var landscape=Group(root,"menu-landscape");
            landscape.style.backgroundImage=Map.SurveyImage;
            var folio=Group(landscape,"cover-folio");
            folio.AddToClassList("panel");
            return folio;
        }

        internal void TitleScreen(Action begin,Action guide)
        {
            var folio=Cover();
            Icon(folio,"compass","cover-compass");
            Label(folio,"雾境远征","game-title");
            Label(folio,"沿岔路穿过迷雾，在星陨圣堂或熔心王座击败守关者。","cover-intro");
            Button(folio,"序章 · 雪山堡垒",()=>UnityEngine.SceneManagement.SceneManager.LoadScene("Frosthold")).AddToClassList("cover-primary");
            Button(folio,"第一章 · 门前的梨树",()=>UnityEngine.SceneManagement.SceneManager.LoadScene("Village"));
            Button(folio,"开始远征",begin).AddToClassList("cover-primary");
            Button(folio,"玩法手册",guide);
#if UNITY_STANDALONE && !UNITY_EDITOR
            Button(folio,"退出游戏",()=>Application.Quit());
#endif
        }

        private void Result()
        {
            bool won=run.Phase==RunPhase.Victory;
            var folio=Cover();
            Icon(folio,won?"Shrine":"Beacon","cover-compass");
            Label(folio,won?"远征凯旋":"远征止步","result-title");
            string end=won?"击败守关者":(run.Current.Kind==RoomKind.Event || run.IsRoadside) ?
                run.EventReport : "败于"+run.EnemyAt(run.Current).Name();
            Label(folio,WorldAtlas.Names[run.Map.Region]+" · "+end,"cover-intro");
            Label(folio,string.Join(" → ",itinerary),"record-route");
            Label(folio,$"生命 {run.Health} / {rules.maxHealth}　金币 {run.Gold}　牌组 {run.Deck.Count}","note");
            Label(folio,$"地图种子　{run.Seed}","record-seed");
            Button(folio,"再次出发",restart).AddToClassList("cover-primary");
            Button(folio,"返回标题",home);
        }

        private VisualElement Modal(string title)
        {
            CloseOverlay();
            Overlay=new VisualElement{name="expedition-menu"};
            Overlay.AddToClassList("expedition");
            Overlay.AddToClassList("menu-shade");
            root.parent.Add(Overlay);
            var sheet=Group(Overlay,"menu-sheet");
            sheet.AddToClassList("panel");
            Label(sheet,title,"title");
            return sheet;
        }

        internal void Pause(Action resume,Action guide,Action abandon)
        {
            var sheet=Modal("远征暂停");
            Label(sheet,WorldAtlas.Names[run.Map.Region]+$"　·　第 {run.Current.Layer} 层","cover-intro");
            Button(sheet,"继续远征",resume).AddToClassList("cover-primary");
            Button(sheet,"玩法手册",guide);
            Button(sheet,"结束本次远征",abandon);
        }
        internal void ConfirmAbandon(Action confirm,Action back)
        {
            var sheet=Modal("结束本次远征？");
            Label(sheet,"返回标题后，本次路线、牌组和战斗进度会清除。","cover-intro");
            Button(sheet,"保留远征",back).AddToClassList("cover-primary");
            Button(sheet,"结束并返回标题",confirm);
        }
        internal void Guide(Action back)
        {
            var sheet=Modal("玩法手册");
            var scroll=new ScrollView();
            sheet.Add(scroll);
            void Entry(string title,string text)
            {
                Label(scroll.contentContainer,title,"guide-topic");
                Label(scroll.contentContainer,text,"note");
            }
            Entry("探索与出口","悬停查看地标和移动消耗，双击出发。沿途驱散迷雾，路标标明出口通往哪个区域。移动点不足时可以扎营。");
            Entry("出牌与连招","先看敌方意图，再抽牌。点击手牌或向上拖出即可打出。连续三张斩击依次打出正斩、反手斩和重劈。");
            Entry("魔法与回合","先水后冰可以冻结敌人，让它跳过行动。右下角显示剩余出牌次数，结束回合后敌人按意图行动。");
            Entry("途中奇遇","抵达节点可能遇到插曲。选项标出检定门槛、收益和代价，也可以绕过，继续原定行程。");
            Button(sheet,"返回",back).AddToClassList("cover-primary");
        }
        internal void CloseOverlay()
        {
            Overlay?.RemoveFromHierarchy();
            Overlay=null;
        }
    }
}
