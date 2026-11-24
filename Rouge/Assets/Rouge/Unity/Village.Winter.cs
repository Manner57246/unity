namespace Rouge.Unity
{
    public sealed partial class Village
    {
        private enum NightChoice { Escort, Ring, Shelter }
        private NightChoice nightChoice;
        private bool nightSettled;

        private void RiverPage()
        {
            if (season != Season.Winter)
            {
                Text("两根褪色的红桩，一根在你脚边，一根在对岸。水从桩间打着旋流过去。想去南岸，眼下得绕东边的桥。");
                return;
            }
            Text(site == Site.South ? "渡屋的窗朝着村子。从这边回望，梨树和井架只隔一个屋顶。门梁下挂着从前叫船用的铜钟。" :
                "春天打着旋的水冻住了。两根红桩之间有一串狐狸脚印，一直通到对岸。地图上亮蓝的冰格现在可以直接走。");
            Choice(site == Site.South ? "踏冰回北岸" : "走过冰面，到南岸渡屋",
                ()=>Visit(site == Site.South ? Site.Ford : Site.South));
        }

        private void DoorChoices()
        {
            Text("门只开了一掌宽。莉娅把村长的小孙女推给你。孩子攥着一张盖了王印的收据：空村一座，明日交割。见证人一栏，村长已经签了名。");
            Text("街口开始挨户点灯。莉娅说：“我去敲各家的后窗。你带孩子走，或去南岸敲渡钟。他们都认得集合的钟声，骑兵也听得见。”磨坊的地窖还空着。");
            Choice("护送孩子和收据，悄悄过河 · 到入夜",()=>ChooseNight(NightChoice.Escort));
            Choice("去南岸敲钟，给全村指路 · 到入夜",()=>ChooseNight(NightChoice.Ring));
            Choice("留下开地窖，收进走不动的人 · 到入夜",()=>ChooseNight(NightChoice.Shelter));
        }

        private void ChooseNight(NightChoice choice)
        {
            nightChoice=choice; raid=Raid.AfterDoor; Refresh();
        }

        private void AfterDoorPage()
        {
            var target = nightChoice == NightChoice.Shelter ? Site.Mill : Site.South;
            if (nightSettled)
            {
                if (dawn != Dawn.None) DawnPage();
                else if (followingLead) FollowNightLead();
                else if (site == target)
                {
                    NightResult();
                    Choice(nightChoice switch {
                        NightChoice.Escort => "把收据翻到背面",
                        NightChoice.Ring => "数一数最后那盏灯下的人",
                        _ => "贴着石轮听一会儿"
                    },()=>{followingLead=true; Refresh();});
                }
                else
                {
                    if (site == Site.Ford || site == Site.South) RiverPage();
                    else Text("远处传来报更声。你还有一件牵挂的事。");
                    Choice("回到"+Names[(int)target],()=>Visit(target));
                }
                return;
            }
            if (site == target)
            {
                Text(nightChoice switch {
                    NightChoice.Escort => "孩子把收据塞在袖里，过冰时一路没出声。渡屋里有人给你们留了一盏灯。",
                    NightChoice.Ring => "你攥住铜钟的绳结。对岸几扇后窗开着。只要一拉，他们就知道往哪儿跑。",
                    _ => "蓓特掀起地板。窖底的老人想挪出地方，腿却抬不起来。外面又响起叩门声。"
                });
                Choice(nightChoice switch { NightChoice.Escort=>"关上渡屋的门", NightChoice.Ring=>"敲响渡钟", _=>"扶最后一家人下窖" },
                    ()=>{nightSettled=true; Refresh();});
                return;
            }
            Text(nightChoice switch {
                NightChoice.Escort=>"孩子把冰凉的手塞进你掌心。避开桥头的灯，走梨湾两根红桩之间的亮冰，到南岸旧渡屋。",
                NightChoice.Ring=>"莉娅带孩子钻进背巷。你得赶到南岸旧渡屋，让跑散的人都听见渡钟。梨湾的冰道是近路。",
                _=>"莉娅带孩子去南岸。你答应守在北岸，替那些走不过冰的人开磨坊地窖。"
            });
            if (site == Site.Ford && nightChoice != NightChoice.Shelter) RiverPage();
            else Choice("前往"+Names[(int)target],()=>Visit(target));
        }

        private void NightResult()
        {
            Text(nightChoice switch {
                NightChoice.Escort=>"入夜 · 渡屋里有两个人\n孩子把收据摊到灯下。你这才看见纸背上还有一座村的名字。北岸没响过钟，莉娅也还没回来。",
                NightChoice.Ring=>"入夜 · 河上亮起十七盏灯\n第一盏是莉娅提的。你数到第十七盏时，夹在队尾的火把照亮了王旗。骑兵认出了冰道。",
                _=>"入夜 · 磨坊又转了起来\n石轮声盖住了窖底十二个人的喘息。南岸的灯亮了两下，像你平日敲门的暗号。蓓特拉住你：现在开门，这十二个人都藏不住。"
            });
        }
    }
}
