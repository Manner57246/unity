using UnityEngine.SceneManagement;

namespace Rouge.Unity
{
    public sealed partial class Village
    {
        private enum Dawn { None, Dispatch, Testimony, Passage, Homecoming, OpenCellar, KeepWatch }
        private Dawn dawn;
        private bool followingLead;
        private Site DawnSite => nightChoice switch {
            NightChoice.Escort => Site.Wood, NightChoice.Ring => Site.Ford, _ => Site.Mill
        };
        private string DawnLandmark => dawn switch {
            Dawn.Dispatch => "林场 · 空邮架", Dawn.Testimony => "林场 · 候审的火堆",
            Dawn.Passage => "梨湾 · 王旗下的路", Dawn.Homecoming => "梨湾 · 无旗的渡口",
            Dawn.OpenCellar => "磨坊 · 门上的封蜡", _ => "磨坊 · 十二只碗"
        };

        private void FollowNightLead()
        {
            if (site != DawnSite)
            {
                Text(nightChoice switch {
                    NightChoice.Escort => "纸背上写着榛溪。孩子念出来，又用指甲刮了刮王印：“姨妈就住那儿。你告诉她，爷爷走的时候还戴着她织的帽子。”她拿走灯，要和你一起去东边林场赶邮橇。",
                    NightChoice.Ring => "第十七盏灯停在北岸红桩旁。莉娅已经把前头的人安顿进渡屋，回来拉你的袖子：“你看，那匹马是空鞍。举灯的人在下面走。”",
                    _ => "磨坊的窗缝里还能看见那盏灯。蓓特守着地窖，等你回去接她的班。"
                });
                Choice("去"+Names[(int)DawnSite],()=>Visit(DawnSite));
                return;
            }

            switch (nightChoice)
            {
                case NightChoice.Escort:
                    Text("邮娘在梳最后一匹马的尾巴。她肯顺路叫醒榛溪的人，可昨夜也有人冒充王使骗开过村门。她要带那张盖印的原件，天亮前就得上路。");
                    Text("天亮也有巡回法官在林场换马。孩子想当面问他：“纸上说明天才把村子交出去。那我爷爷今天怎么就死了？”她看着你手里的纸，等你拿主意。");
                    Choice("把原件交给邮娘，等榛溪回信 · 到天亮",()=>MeetDawn(Dawn.Dispatch));
                    Choice("留下原件，陪孩子等法官 · 到天亮",()=>MeetDawn(Dawn.Testimony));
                    break;
                case NightChoice.Ring:
                    Text("骑手把王旗斜靠在红桩上，蹲下来替一个老太太裹脚。她拧了儿子的耳朵：“我叫你捎盐，你带这些人来做什么？”莉娅听出来了。是从前替磨坊赶车的阿韦。");
                    Text("桥头已经换岗。阿韦肯举旗领大家过去，岗哨要记下领队的名字。他压低声音：“过了岗，弟兄们就会知道是谁放走了你们。我得留下答话。”若让他脱了军袍陪母亲藏进渡屋，十七盏灯就都得在这里熄到天亮。");
                    Choice("请阿韦举旗，带大家过岗 · 到天亮",()=>MeetDawn(Dawn.Passage));
                    Choice("收下他的军袍，留大家在渡屋 · 到天亮",()=>MeetDawn(Dawn.Homecoming));
                    break;
                case NightChoice.Shelter:
                    Text("灯又亮了两下。小窗外响起两下指节声。莉娅隔着门说：“孩子在对岸替我打灯。我带稳婆回来了。”窖里那个怀孕的女人咬着一团围裙，已经疼了半夜。");
                    Text("方才巡查的人以为磨坊空了，顺手在门缝上滴了蜡。稳婆在外面说，她摸到了王印。开门，天亮时守卫就知道有人进过磨坊。她也可以留下，把手伸进送面的小窗，一句一句教你接生。");
                    Choice("揭封开门，让稳婆下窖 · 到天亮",()=>MeetDawn(Dawn.OpenCellar));
                    Choice("保住封蜡，守在送面窗两边 · 到天亮",()=>MeetDawn(Dawn.KeepWatch));
                    break;
            }
        }

        private void MeetDawn(Dawn result)
        {
            if (dawn != Dawn.None) return;
            dawn=result;
            date=date.AddDays(1);
            Refresh();
        }

        private void DawnPage()
        {
            if (site == DawnSite)
            {
                Text(dawn switch {
                    Dawn.Dispatch => "邮架上只剩半截麻绳。天刚亮，一辆运奶车从榛溪过来，车尾坐着穿睡衣的三姐弟。最大的那个把一顶没织完的帽子递给孩子：“你姨妈让我来接你。”孩子把脸埋了进去。法官换过马，已经走了。",
                    Dawn.Testimony => "法官给孩子垫了一只马鞍，好让她够着桌面。她自己写下爷爷的名字。法官把收据缝进案卷，给你一张有案号的存根。东去的邮橇昨夜就走了。孩子把存根折好：“现在我们自己去找姨妈。”",
                    Dawn.Passage => "守门人果然让了路。他在册子上写下阿韦的军号，把他扣在岗亭。走出半里地，他母亲才肯松开你的手。她从口袋里摸出两枚铜子：“往回走的时候，给他带点盐。”",
                    Dawn.Homecoming => "天亮时，王旗还卷在老太太膝上。阿韦穿着借来的面粉褂，蹲在渡屋门后给母亲穿鞋。第一班巡骑从桥上过去了。昨夜过河的人还挤在南岸，莉娅开始挨家数剩下的面饼。",
                    Dawn.OpenCellar => "第一声啼哭时，蓓特把轮闸又松了一格。稳婆把孩子放在母亲胸口，莉娅才坐下来。天亮，有人来验封，把两块断蜡揣进兜里。午前还会有人来，蓓特已经开始给老人套鞋。",
                    _ => "稳婆的手从小窗里伸进来。你照她说的接住孩子，又把小手送过去，让她摸一摸。查封的人看了一眼完整的蜡，打着呵欠走了。莉娅在门外坐了一宿，隔着木板问：“像谁？”"
                });
            }
            else if (site == Site.South && nightChoice == NightChoice.Ring)
                Text(dawn == Dawn.Passage ? "渡屋空了，门槛边剩一小撮倒出来的鞋砂。冰上那串脚印一路向东。" : "十七盏灯倒扣在窗台上。阿韦的母亲揭开一盏，下面藏着她给儿子留的半块饼。");
            else if (site == Site.South && nightChoice == NightChoice.Shelter)
                Text("村长的孙女踮着脚把油灯放回窗台。灯把儿上缠了布，她说这样来回遮火苗就不会烫着。");
            else if (site == Site.South)
                Text(dawn == Dawn.Dispatch ? "渡屋的桌子上留着一圈红印。那张盖印的纸已经在榛溪人手里了。" : "你摸了摸怀里那张硬挺的存根。孩子翻遍渡屋，找出一张通往榛溪的旧路图。");
            else if (site == Site.Ford) RiverPage();
            else Text(site switch {
                Site.Home => evening == Evening.Pears ? "晨风吹得梨干一下下碰窗。屋里没人，那串梨干还好好地挂在原处。" : "院门开着。面袋上那只炭画的歪梨沾了雪，你把袋口扎紧，放回檐下。",
                Site.Green => "井台边挂着一段断绳。木勺已经被人拾起来，搁在井沿上。",
                Site.Wood => "邮橇清早已经出发。林场外有两道新辙，沿东边的路一直伸进雾里。",
                _ => "磨坊主坐在后阶上，往一张旧面袋上记人名。看见有人回来，她就在名字旁画一个圈。"
            });
            if (site != DawnSite) Choice("去"+DawnLandmark,()=>Visit(DawnSite));
            Choice("重玩村居开场",()=>SceneManager.LoadScene("Village"));
        }
    }
}
