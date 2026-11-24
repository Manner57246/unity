using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using Rouge.Core;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

namespace Rouge.Unity
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class Expedition : MonoBehaviour
    {
        [SerializeField] private RunDefinition definition;
        [SerializeField] private StyleSheet interfaceStyle;
        [SerializeField] private Camera mapCamera;
        [SerializeField] private bool useFixedSeed;
        [SerializeField] private int seed = 1701;

        private RouteBoard board;
        private ScoutFrames scout;
        private readonly BattleArena arena = new();
        private bool enemyActing;
        private bool performing;
        private ExpeditionHud hud;
        private ExpeditionFoley foley;
        private VisualElement root, page, clouds;
        private Texture2D cloudTexture;
        private WorldAtlas atlas;
        private int runSeed;
        private Talent talent;
        private Loadout pack;
        private DeckStyle deckStyle;
        private bool selecting, travelling, inBattle, switching;
        private readonly List<string> itinerary=new();
        private enum Menu { Closed, Pause, Guide, Abandon }
        private Menu menu;
        private bool pageWasEnabled, audioWasPaused, leaving;
        private float previousSpeed;
        public RunSession Session { get; private set; }

        private void Start()
        {
            root = GetComponent<UIDocument>().rootVisualElement;
            root.styleSheets.Add(interfaceStyle);
            page = new VisualElement();
            root.Add(page);
            page.style.flexGrow = 1;
            scout=new ScoutFrames();
            foley=gameObject.AddComponent<ExpeditionFoley>();
            hud = new ExpeditionHud(page,definition.rules,StartRun,Travel,Commit,PlayCard,foley,OpenPause,ReturnToTitle,itinerary);
            cloudTexture = MakeClouds();
            PrepareRun();
            Title();
        }

        private void StartRun()
        {
            PrepareRun();
            Setup();
        }

        private void PrepareRun()
        {
            if(Session!=null) Session.Changed-=Refresh;
            Session=null;
            itinerary.Clear();
            runSeed=useFixedSeed ? seed : Guid.NewGuid().GetHashCode();
            atlas=new WorldAtlas(runSeed,definition.rules.layers);
            board?.Dispose();
            board=new RouteBoard(atlas,scout,runSeed);
            hud.Map=board;
            talent=Talent.Wings;
            pack=Loadout.Provisions;
            deckStyle=DeckStyle.Balanced;
        }

        private void Title()
        {
            selecting=false;
            hud.TitleScreen(Setup,OpenGuide);
        }

        private void Setup()
        {
            selecting=false;
            hud.Setup(talent,pack,deckStyle,t=>{talent=t;Setup();},p=>{pack=p;Setup();},d=>{deckStyle=d;Setup();},()=>{
                selecting=true;
                hud.Atlas(atlas,Enter,Setup);
            },Title);
        }

        private void Enter(int region)
        {
            if(!selecting || travelling || !atlas.Available(region)) return;
            selecting=false;
            travelling=true;
            StartCoroutine(EnterRegion(region));
        }

        private IEnumerator EnterRegion(int region)
        {
            page.SetEnabled(false);
            clouds=new VisualElement { name="Cloud passage" };
            clouds.style.position=Position.Absolute;
            clouds.style.top=0;clouds.style.bottom=0;
            clouds.style.width=Length.Percent(155);
            clouds.style.backgroundImage=cloudTexture;
            clouds.style.backgroundSize=new BackgroundSize(Length.Percent(100),Length.Percent(100));
            root.Add(clouds);
            yield return Sweep(true,region);
            if (Session != null) Session.Changed -= Refresh;
            Session = Session == null ? new RunSession(definition.rules,runSeed,atlas.Regions[region],talent,pack,deckStyle)
                : Session.ContinueTo(atlas.Regions[region]);
            Session.Changed+=Refresh;
            itinerary.Add(WorldAtlas.Names[region]);
            Refresh();
            yield return Sweep(false,region);
            clouds.RemoveFromHierarchy();
            clouds=null;
            page.SetEnabled(true);
            travelling=false;
        }

        private IEnumerator Sweep(bool entering,int region)
        {
            const float duration=.66f;
            for(float elapsed=0; elapsed<duration; elapsed+=Time.unscaledDeltaTime)
            {
                float t=Mathf.SmoothStep(0,1,elapsed/duration);
                PositionCloud(t);
                yield return null;
            }
            PositionCloud(1);
            yield return null;

            void PositionCloud(float t)
            {
                clouds.style.left=Length.Percent(entering ? Mathf.Lerp(-155,-27.5f,t) : Mathf.Lerp(-27.5f,100,t));
                if (entering) hud.ZoomSurvey(region,t);
            }
        }

        private static Texture2D MakeClouds()
        {
            var tex=new Texture2D(384,160,TextureFormat.RGBA32,false){wrapMode=TextureWrapMode.Clamp};
            var colors=new Color32[tex.width*tex.height];
            for(int y=0;y<tex.height;y++)
            for(int x=0;x<tex.width;x++)
            {
                float u=(float)x/(tex.width-1), v=(float)y/(tex.height-1);
                float noise=.65f*Mathf.PerlinNoise(u*7+4,v*4)+.35f*Mathf.PerlinNoise(u*19,v*11+3);
                float edge=Mathf.Min(u,1-u)+.13f*(noise-.5f);
                var c=Color.Lerp(new Color(.38f,.51f,.56f),new Color(.92f,.94f,.88f),Mathf.SmoothStep(0,1,noise));
                c.a=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.025f,.115f,edge));
                colors[y*tex.width+x]=c;
            }
            tex.SetPixels32(colors);tex.Apply(false,true);return tex;
        }

        private void Travel(int id)
        {
            if (travelling || switching || !Session.BeginTravel(id)) return;
            StartCoroutine(Walk());
        }

        private IEnumerator Walk()
        {
            // Carry excess frame time over corners so long routes keep a constant speed.
            var path = Session.Journey;
            float progress = 0;
            while (Session.Phase == RunPhase.Travelling)
            {
                yield return null;
                if(menu!=Menu.Closed)continue;
                progress += Time.unscaledDeltaTime * 3.2f;
                // On a slow frame let the last crossed cell supply the audible footfall.
                int footfall=-1;
                while (progress >= 1 && Session.Phase == RunPhase.Travelling)
                {
                    progress -= 1;
                    footfall=path[Session.JourneyIndex+1];
                    Session.AdvanceJourney();
                }
                if(footfall>=0)foley.Step(Session.Map,footfall,Session.Talent);
                if (Session.Phase == RunPhase.Travelling)
                {
                    int i=Session.JourneyIndex;
                    board.WalkFrame(path[i],path[i+1],progress);
                }
            }
        }

        private void Refresh()
        {
            if(enemyActing)
            {
                if(Session.Phase==RunPhase.Battle)hud.Show(Session);
                return;
            }
            if (Session.Phase == RunPhase.Passage)
            {
                if (travelling || !atlas.CompleteSite(Session.Map.Region)) return;
                travelling = true;
                hud.Show(Session);
                StartCoroutine(EnterRegion(Session.Current.OnwardRegion));
                return;
            }
            bool needArena = Session.Phase == RunPhase.Battle;
            if (needArena != inBattle && !switching)
                StartCoroutine(SwitchBattle(needArena));
            hud.Show(Session);
        }

        private IEnumerator SwitchBattle(bool enter)
        {
            switching = true;
            page.SetEnabled(false);
            if (enter)
                yield return arena.Open(Session.Battle, scout, mapCamera, Session.Talent == Talent.Wings,
                    Session.Current.Kind == RoomKind.Boss);
            else
                yield return arena.Close(gameObject.scene, mapCamera);
            inBattle = enter;
            switching = false;
            page.SetEnabled(true);
        }
        private void Commit()
        {
            if (performing || switching || !inBattle || Session.Battle?.Phase!=TurnPhase.Plan) return;
            StartCoroutine(EnemyTurn());
        }

        private IEnumerator EnemyTurn()
        {
            enemyActing=performing=true;
            page.SetEnabled(false);
            yield return arena.Act(Session.Battle,()=>Session.EndTurn());
            enemyActing=performing=false;
            page.SetEnabled(true);
            if(Session.Phase!=RunPhase.Battle)Refresh();
        }

        private void PlayCard(int index)
        {
            if (performing || switching || !inBattle) return;
            var battle=Session.Battle;
            if(battle==null)return;
            int before=battle.Hand.Count;
            var play = battle.Play(index);
            if (play == null) return;
            foley.PlayCard(play.Card.Type);
            foley.Draw(battle.Hand.Count-(before-1));
            performing = true;
            hud.Show(Session);
            hud.SetPlaying(true);
            StartCoroutine(AnimateCard(play));
        }

        private IEnumerator AnimateCard(CardPlay play)
        {
            yield return arena.Animate(play);
            if (Session.Battle.Phase == TurnPhase.Finished) yield return arena.Fall();
            performing=false;
            hud.SetPlaying(false);
            if (Session.Battle.Phase == TurnPhase.Finished) Session.EndTurn();
        }

        private void LateUpdate()
        {
            if(Keyboard.current?.escapeKey.wasPressedThisFrame==true)
            {
                if(menu==Menu.Closed)OpenPause(); else Back();
            }
            if(menu==Menu.Closed)
            {
                if (inBattle && !performing && Session?.Battle != null) arena.Idle(Session.Battle);
                board?.FadeFog(Time.unscaledDeltaTime);
            }
            // UI Toolkit can replace the document root after an editor stylesheet reload.
            var current = GetComponent<UIDocument>().rootVisualElement;
            if (page == null || current == null || page.parent == current) return;
            root = current;
            if (!root.styleSheets.Contains(interfaceStyle)) root.styleSheets.Add(interfaceStyle);
            root.Add(page);
            if (clouds != null) root.Add(clouds);
            if(hud.Overlay!=null) root.Add(hud.Overlay);
        }

        private void OpenPause()
        {
            if(leaving || switching || travelling || Session==null ||
                Session.Phase==RunPhase.Victory || Session.Phase==RunPhase.Defeat)return;
            SetMenu(Menu.Pause);
            hud.Pause(Resume,OpenGuide,()=>{SetMenu(Menu.Abandon);hud.ConfirmAbandon(ReturnToTitle,Back);});
        }
        private void OpenGuide()
        {
            SetMenu(Menu.Guide);
            hud.Guide(Back);
        }
        private void SetMenu(Menu next)
        {
            if(menu==Menu.Closed)
            {
                previousSpeed=Time.timeScale;
                audioWasPaused=AudioListener.pause;
                pageWasEnabled=page.enabledSelf;
                Time.timeScale=0;
                AudioListener.pause=true;
                page.SetEnabled(false);
            }
            menu=next;
        }
        private void Back()
        {
            if(menu==Menu.Pause || Session==null)Resume(); else OpenPause();
        }
        private void Resume()
        {
            if(menu==Menu.Closed)return;
            hud.CloseOverlay();
            menu=Menu.Closed;
            Time.timeScale=previousSpeed;
            AudioListener.pause=audioWasPaused;
            page.SetEnabled(pageWasEnabled);
        }
        private void ReturnToTitle()
        {
            if(leaving)return;
            leaving=true;
            Resume();
            page.SetEnabled(false);
            SceneManager.LoadScene(gameObject.scene.buildIndex,LoadSceneMode.Single);
        }
        private void OnDestroy()
        {
            Resume();
            if(Session!=null) Session.Changed-=Refresh;
            board?.Dispose();
            scout?.Dispose();
            if(cloudTexture!=null) Destroy(cloudTexture);
        }
    }
}
