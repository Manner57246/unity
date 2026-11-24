using System;
using System.IO;
using UnityEngine;

namespace SuperDodgeball
{
    public enum DodgeRefereePhase { Slacking, Warning, Watching }

    // The host judges root positions; clients only display the replicated decision.
    [DefaultExecutionOrder(-180)]
    public sealed class DodgeReferee : MonoBehaviour
    {
        public DodgeRefereePhase Phase { get; private set; }
        public float Remaining { get; private set; }
        public float Clock { get; private set; }
        public float PendingTea {get;private set;}
        public bool OfferTea() {if(DodgeGame.IsReplica || PendingTea>0)return false;PendingTea=6f;return true;}
        public int Whistles { get; private set; }
        public int LastOffender { get; private set; } = -1;
        public int GuardCalls { get; private set; }
        public const float WarningSeconds=2.2f, GuardSeconds=4.5f;
        readonly int[] strikes=new int[5];
        readonly float[] trespass=new float[5], grace=new float[5], decay=new float[5], detention=new float[5], correction=new float[5];
        readonly Vector3[] caughtAt=new Vector3[5];
        readonly Transform[,] guards=new Transform[5,2];
        readonly TextMesh[] badges=new TextMesh[5];
        readonly System.Collections.Generic.List<Material> materials=new System.Collections.Generic.List<Material>();
        DodgeApp app; DodgeGame game;
        GameObject root;Transform official,head,leftArm,rightArm,cup,paper,card,lamp;
        Transform centerLine;TextMesh frontSign,backSign,announcement;
        float announcementLeft;int heardWhistles,heardGuardCalls;DodgeRefereePhase heardPhase;
        System.Random random=new System.Random();
        public int Strikes(int slot)=>strikes[slot];
        public float GuardRemaining(int slot)=>detention[slot];
        public bool IsRestricted(int slot)=>app && app.Page==DodgePage.Playing && slot>=0 && slot<5 && (detention[slot]>0f || correction[slot]>0f);
        public static bool Restricted(DodgeActor actor)
        {
            if(!actor || !DodgeApp.Active)return false;
            var app=DodgeApp.Instance;
            return app.Referee && app.Referee.IsRestricted(app.SeatOf(actor));
        }
        public bool IsAcross(Vector3 position,int team)=>team>=0 && Mathf.Abs(position.x)<DodgeCourt.HalfWidth && Mathf.Abs(position.z)<DodgeCourt.HalfDepth
            && (team==0?position.z>.65f:position.z<-.65f);
        public bool MustRetreat(DodgeActor actor)
        {
            int slot=app.SeatOf(actor);
            return slot>=0 && Phase!=DodgeRefereePhase.Slacking && IsAcross(actor.transform.position,app.Room.seats[slot].team);
        }
        public Vector3 BotDestination(DodgeActor actor,Vector3 desired,bool mischievous)
        {
            int slot=app.SeatOf(actor);if(slot<0 || app.Page!=DodgePage.Playing)return desired;
            int team=app.Room.seats[slot].team;float sign=team==0?-1f:1f;
            if(MustRetreat(actor))return new Vector3(Mathf.Clamp(actor.transform.position.x,-7f,7f),.05f,sign*3.2f);
            if(mischievous && Phase==DodgeRefereePhase.Slacking && Remaining>3f && strikes[slot]<2)
                desired.z=-sign*2.6f;
            else desired.z=team==0?Mathf.Min(desired.z,-1.1f):Mathf.Max(desired.z,1.1f);
            return desired;
        }
        public void Build(DodgeApp owner,DodgeGame source)
        {
            app=owner;game=source;root=new GameObject("09 - Slacking referee and security");
            var cream=Mat(new Color(1f,.96f,.84f));var navy=Mat(new Color(.19f,.28f,.34f));
            var skin=Mat(new Color(.98f,.77f,.51f));var yellow=Mat(new Color(1f,.78f,.2f));
            var mint=Mat(new Color(.5f,.83f,.72f));
            Part("Referee podium",root.transform,new Vector3(-10.4f*DodgeCourt.Scale,.65f,0),new Vector3(1.5f,1.3f,2.6f),mint);
            official=Person("Lazy referee",root.transform,skin,navy,cream,false);
            official.localPosition=new Vector3(-10.4f*DodgeCourt.Scale,1.3f,0);official.localScale=Vector3.one*1.2f;
            head=official.Find("Head");leftArm=official.Find("Left arm");rightArm=official.Find("Right arm");
            for(int i=0;i<4;i++)Part("Referee shirt stripe",official,new Vector3(-.3f+i*.2f,1.05f,-.37f),new Vector3(.085f,.56f,.025f),cream);
            cup=Part("Slacking drink",rightArm,new Vector3(0,-.47f,-.1f),new Vector3(.24f,.3f,.24f),mint,PrimitiveType.Cylinder);
            Part("Drink straw",cup,new Vector3(0,.6f,0),new Vector3(.08f,.6f,.08f),yellow);
            paper=Part("Sports newspaper",leftArm,new Vector3(0,-.4f,-.23f),new Vector3(.7f,.52f,.04f),cream);
            card=Part("Yellow card",rightArm,new Vector3(0,-.52f,0),new Vector3(.25f,.36f,.035f),yellow);
            Part("Whistle",official,new Vector3(0,1.65f,-.36f),new Vector3(.14f,.12f,.17f),yellow,PrimitiveType.Sphere);
            lamp=Part("Referee attention lamp",root.transform,new Vector3(-10.4f*DodgeCourt.Scale,4.4f,0),Vector3.one*.55f,Mat(new Color(.5f,.83f,.72f)),PrimitiveType.Sphere);
            centerLine=Part("Visible center rule",root.transform,new Vector3(0,.035f,0),new Vector3(20.5f*DodgeCourt.Scale,.025f,.13f),Mat(new Color(1f,.78f,.2f)));
            var board=Part("Referee world sign",root.transform,new Vector3(-7.7f*DodgeCourt.Scale,4.9f,0),new Vector3(5.4f,1.2f,.16f),navy);
            frontSign=app.Clubhouse.WorldLabel("Referee sign south",board,new Vector3(0,0,-.55f),"",.0115f,Color.white);
            backSign=app.Clubhouse.WorldLabel("Referee sign north",board,new Vector3(0,0,.55f),"",.0115f,Color.white,180);
            // Keep letters in world units, independent of the board's non-uniform scale.
            frontSign.transform.SetParent(root.transform,true);backSign.transform.SetParent(root.transform,true);
            frontSign.transform.localScale=backSign.transform.localScale=Vector3.one;
            frontSign.characterSize=backSign.characterSize=.072f;
            announcement=app.Clubhouse.WorldLabel("Referee call",root.transform,new Vector3(-9.5f*DodgeCourt.Scale,4.9f,0),"",.075f,new Color(.2f,.27f,.32f),90);
            for(int slot=0;slot<5;slot++)
            {
                badges[slot]=app.Clubhouse.WorldLabel("Foul warning "+slot,root.transform,Vector3.zero,"",.058f,new Color(.85f,.37f,.12f));
                for(int side=0;side<2;side++)
                {
                    guards[slot,side]=Person("Security "+slot+" / "+side,root.transform,skin,navy,navy,true);
                    guards[slot,side].localScale=new Vector3(1.25f,1.05f,1.25f);
                }
            }
            ResetRound();root.SetActive(false);
        }
        Material Mat(Color color)
        {
            var material=new Material(game.popMaterial);material.SetColor("_BaseColor",color);material.mainTexture=null;
            materials.Add(material);return material;
        }
        Transform Part(string name,Transform parent,Vector3 position,Vector3 scale,Material material,PrimitiveType shape=PrimitiveType.Cube)
        {
            var part=GameObject.CreatePrimitive(shape);part.name=name;part.transform.SetParent(parent,false);
            part.transform.localPosition=position;part.transform.localScale=scale;part.GetComponent<Renderer>().sharedMaterial=material;
            var collider=part.GetComponent<Collider>();collider.enabled=false;Destroy(collider);return part.transform;
        }
        Transform Person(string name,Transform parent,Material skin,Material clothes,Material trim,bool security)
        {
            var person=new GameObject(name).transform;person.SetParent(parent,false);
            Part("Body",person,new Vector3(0,1,0),new Vector3(.86f,1f,.68f),clothes,PrimitiveType.Capsule);
            Part("Head",person,new Vector3(0,1.78f,0),new Vector3(.73f,.68f,.7f),skin,PrimitiveType.Sphere);
            Part("Cap",person,new Vector3(0,2.05f,0),new Vector3(.8f,.16f,.78f),clothes,PrimitiveType.Sphere);
            for(int side=-1;side<=1;side+=2)
            {
                var arm=new GameObject(side<0?"Left arm":"Right arm").transform;arm.SetParent(person,false);arm.localPosition=new Vector3(side*.5f,1.35f,0);
                Part("Sleeve",arm,new Vector3(0,-.24f,0),new Vector3(.3f,.38f,.32f),trim,PrimitiveType.Capsule);
                Part("Hand",arm,new Vector3(0,-.54f,0),Vector3.one*.29f,skin,PrimitiveType.Sphere);
                Part(side<0?"Left foot":"Right foot",person,new Vector3(side*.24f,.28f,-.05f),new Vector3(.34f,.55f,.4f),clothes,PrimitiveType.Capsule);
            }
            if(security)Part("Tie",person,new Vector3(0,1.1f,-.35f),new Vector3(.12f,.44f,.04f),skin);
            return person;
        }
        public void ResetRound()
        {
            PendingTea=0;Phase=DodgeRefereePhase.Slacking;Remaining=8f;Clock=0f;Whistles=GuardCalls=0;LastOffender=-1;announcementLeft=0;
            heardWhistles=heardGuardCalls=0;heardPhase=Phase;
            for(int i=0;i<5;i++)
            {
                strikes[i]=0;trespass[i]=grace[i]=decay[i]=detention[i]=correction[i]=0;
                if(app.Actors!=null && app.Actors[i])
                {
                    app.Actors[i].SetDive(0f);
                    var motor=app.Actors[i].GetComponent<CharacterController>();if(!app.IsClient)motor.enabled=true;
                }
            }
        }
        void Update()
        {
            if(!app || !app.Initialized || !root)return;
            bool visible=DodgeApp.Active && (app.Page==DodgePage.Staging || app.Page==DodgePage.Countdown || app.Page==DodgePage.Playing || app.Page==DodgePage.Results);
            root.SetActive(visible);
            if(!DodgeApp.Active)return;
            if(!app.IsClient && app.Page==DodgePage.Playing && app.SimulationRunning)Tick(Time.deltaTime);
            if(!visible)return;
            Sounds();Draw();
        }
        void Tick(float dt)
        {
            Clock+=dt;Remaining-=dt;announcementLeft=Mathf.Max(0,announcementLeft-dt);
            if(Remaining<=0f)
            {
                if(Phase==DodgeRefereePhase.Slacking){Phase=DodgeRefereePhase.Warning;Remaining=WarningSeconds;}
                else if(Phase==DodgeRefereePhase.Warning){Phase=DodgeRefereePhase.Watching;Remaining=4.5f+(float)random.NextDouble()*1.5f;}
                else {Phase=DodgeRefereePhase.Slacking;Remaining=7f+(float)random.NextDouble()*3f+PendingTea;PendingTea=0;}
                Array.Clear(trespass,0,trespass.Length);
            }
            for(int i=0;i<5;i++)
            {
                if(app.Room.seats[i].kind==DodgeSeatKind.Empty)continue;
                var actor=app.Actors[i];
                if(detention[i]>0f || correction[i]>0f){AdvancePenalty(i,dt);continue;}
                grace[i]=Mathf.Max(0,grace[i]-dt);
                decay[i]+=dt;if(decay[i]>(actor.Kit.Has(DodgeSkill.GoodBehaviour)?15f:25f) && strikes[i]>0){strikes[i]--;decay[i]=0;}
                // Getting launched across the line is not deliberate cheating.
                if(actor.IsStunned){grace[i]=1.8f;trespass[i]=0f;continue;}
                bool across=IsAcross(actor.transform.position,app.Room.seats[i].team);
                if(Phase!=DodgeRefereePhase.Watching || grace[i]>0f || !across){trespass[i]=0;continue;}
                trespass[i]+=dt;
                if(trespass[i]>=.35f)Catch(i);
            }
        }
        void Catch(int slot)
        {
            strikes[slot]++;Whistles++;LastOffender=slot;announcementLeft=3.2f;
            trespass[slot]=decay[slot]=0;grace[slot]=3f;
            var actor=app.Actors[slot];caughtAt[slot]=actor.transform.position;
            app.Controllers[slot].CancelForReferee();
            var bot=actor.GetComponent<DodgeBot>();if(bot)bot.CancelForReferee();
            actor.GetComponent<CharacterController>().enabled=false;
            if(strikes[slot]>=3){detention[slot]=GuardSeconds;GuardCalls++;}
            else correction[slot]=.75f;
        }
        Vector3 SafeCourt(int slot)
        {
            int team=app.Room.seats[slot].team;
            return new Vector3(Mathf.Clamp(caughtAt[slot].x,-7f,7f),.05f,team==0?-4.2f:4.2f);
        }
        Vector3 EscortPoint(int slot,float elapsed)
        {
            float sign=app.Room.seats[slot].team==0?-1f:1f;
            Vector3 destination=app.Clubhouse.RestSpawn(app.Room.seats[slot].team,slot);
            Vector3 door=new Vector3(0,.05f,sign*DodgeCourt.RestZ(14.5f));
            float t=Mathf.SmoothStep(0,1,Mathf.Clamp01((elapsed-.65f)/2.15f));
            Vector3 position=t<.65f?Vector3.Lerp(caughtAt[slot],door,t/.65f):Vector3.Lerp(door,destination,(t-.65f)/.35f);
            position.y=.8f+Mathf.Sin(Clock*13f)*.035f;return position;
        }
        void AdvancePenalty(int slot,float dt)
        {
            var actor=app.Actors[slot];
            if(detention[slot]>0)
            {
                detention[slot]=Mathf.Max(0,detention[slot]-dt);
                float elapsed=GuardSeconds-detention[slot];
                actor.transform.position=elapsed<.65f?caughtAt[slot]:EscortPoint(slot,elapsed);
                actor.SetDive(elapsed>.65f?.8f:0f);actor.SetMovement(Vector2.zero);actor.SetLocomotion(0);
                if(detention[slot]<=0){strikes[slot]=0;Release(slot);}
            }
            else
            {
                correction[slot]=Mathf.Max(0,correction[slot]-dt);
                float t=Mathf.SmoothStep(0,1,1-correction[slot]/.75f);
                actor.transform.position=Vector3.Lerp(caughtAt[slot],SafeCourt(slot),t)+Vector3.up*Mathf.Sin(t*Mathf.PI)*.45f;
                actor.SetDive(.25f);
                if(correction[slot]<=0)Release(slot);
            }
        }
        void Release(int slot)
        {
            var actor=app.Actors[slot];actor.SetDive(0f);app.Controllers[slot].CancelForReferee();
            var bot=actor.GetComponent<DodgeBot>();if(bot)bot.CancelForReferee();
            app.PlaceActor(slot,SafeCourt(slot),app.Room.seats[slot].team==0?0:180);
            actor.GetComponent<CharacterController>().enabled=true;grace[slot]=3f;
            game.EmitFoam(actor.transform.position,Vector3.up,8,1.2f);
        }
        void Sounds()
        {
            if(heardPhase!=Phase)
            {
                if(Phase==DodgeRefereePhase.Warning){game.PlayTone(460,.14f,.09f);game.PlayTone(650,.08f,.06f);}
                heardPhase=Phase;
            }
            if(heardWhistles!=Whistles){heardWhistles=Whistles;game.PlayTone(1700,.23f,.12f);game.view.Kick(.015f);}
            if(heardGuardCalls!=GuardCalls){heardGuardCalls=GuardCalls;game.PlayTone(135,.28f,.11f);}
        }
        void Draw()
        {
            bool match=app.Page==DodgePage.Playing;
            bool slack=Phase==DodgeRefereePhase.Slacking;
            bool warning=Phase==DodgeRefereePhase.Warning;
            Color color=slack?new Color(.5f,.83f,.72f):warning?new Color(1f,.8f,.2f):new Color(1f,.42f,.31f);
            lamp.GetComponent<Renderer>().sharedMaterial.SetColor("_BaseColor",color);
            centerLine.GetComponent<Renderer>().sharedMaterial.SetColor("_BaseColor",color);
            centerLine.localScale=new Vector3(20.5f*DodgeCourt.Scale,.025f,warning?.13f+Mathf.Sin(Clock*14f)*.045f:.13f);
            float facing=slack?90+Mathf.Sin(Clock*.4f)*25:warning?Mathf.Lerp(90,-90,Mathf.SmoothStep(0,1,1-Remaining/WarningSeconds)):-90+Mathf.Sin(Clock*.7f)*22;
            official.localRotation=Quaternion.Euler(0,facing,slack?Mathf.Sin(Clock*1.4f)*3:0);
            head.localRotation=Quaternion.Euler(slack?18f:0f,Mathf.Sin(Clock)*8,0);
            bool call=announcementLeft>0f && LastOffender>=0;
            leftArm.localRotation=Quaternion.Euler(slack?-75f:-15f,0,slack?15f:25f);
            rightArm.localRotation=Quaternion.Euler(call?-160f:slack?-65f:warning?-100f:-10f,0,slack?-15f:-30f);
            paper.gameObject.SetActive(slack);cup.gameObject.SetActive(slack);card.gameObject.SetActive(call);
            string state=!match?"裁判：先入场\n开赛再执法":slack?"裁判摸鱼中…\n越线要小心回头！":warning?"咳咳！\n裁判要回头了！":"裁判盯场中！\n禁止越过中线";
            frontSign.text=backSign.text=state+(PendingTea>0?"\n奶茶已收 · 下次摸鱼加时":"");
            announcement.text=call?app.Room.seats[LastOffender].name+" 越界！\n"+(detention[LastOffender]>0?"保镖，把这位请走！":"警告 "+strikes[LastOffender]+" / 3"):"";
            for(int i=0;i<5;i++)
            {
                var actor=app.Actors[i];bool occupied=app.Room.seats[i].kind!=DodgeSeatKind.Empty;
                badges[i].text=match && occupied && strikes[i]>0?(detention[i]>0?"保镖接管！":"! "+strikes[i]+" / 3"):"";
                badges[i].transform.position=actor.transform.position+Vector3.up*3.1f;
                badges[i].transform.rotation=game.view.transform.rotation;
                for(int side=0;side<2;side++)
                {
                    var guard=guards[i,side];guard.gameObject.SetActive(match && detention[i]>0f);
                    if(detention[i]<=0)continue;
                    float elapsed=GuardSeconds-detention[i];float sign=app.Room.seats[i].team==0?-1:1;
                    Vector3 offset=Vector3.right*(side==0?-.85f:.85f);
                    Vector3 at=elapsed<.65f?Vector3.Lerp(new Vector3(side==0?-11.25f:11.25f,0,sign*15),caughtAt[i]+offset,Mathf.SmoothStep(0,1,elapsed/.65f))
                        :actor.transform.position+offset-Vector3.up*.75f;
                    guard.position=at;guard.rotation=Quaternion.Euler(0,sign<0?0:180,Mathf.Sin(Clock*12+side)*3);
                    guard.Find("Left arm").localRotation=Quaternion.Euler(-70,0,-30);
                    guard.Find("Right arm").localRotation=Quaternion.Euler(-70,0,30);
                    guard.Find("Left foot").localRotation=Quaternion.Euler(Mathf.Sin(Clock*14)*25,0,0);
                    guard.Find("Right foot").localRotation=Quaternion.Euler(-Mathf.Sin(Clock*14)*25,0,0);
                }
            }
            for(int team=0;team<2;team++)
            {
                bool escort=false;for(int i=0;i<5;i++)if(app.Room.seats[i].team==team && detention[i]>0)escort=true;
                app.Clubhouse.SetSecurityPassage(team,escort);
            }
        }
        public void WriteState(BinaryWriter writer)
        {
            writer.Write(PendingTea);writer.Write((byte)Phase);writer.Write(Remaining);writer.Write(Clock);writer.Write(Whistles);writer.Write(GuardCalls);writer.Write(LastOffender);writer.Write(announcementLeft);
            for(int i=0;i<5;i++){writer.Write((byte)strikes[i]);writer.Write(detention[i]);writer.Write(correction[i]);DodgeNet.WriteVector(writer,caughtAt[i]);}
        }
        public void ReadState(BinaryReader reader)
        {
            PendingTea=reader.ReadSingle();Phase=(DodgeRefereePhase)reader.ReadByte();Remaining=reader.ReadSingle();Clock=reader.ReadSingle();Whistles=reader.ReadInt32();GuardCalls=reader.ReadInt32();LastOffender=reader.ReadInt32();announcementLeft=reader.ReadSingle();
            if((int)Phase>2 || LastOffender< -1 || LastOffender>=5)throw new InvalidDataException("Invalid referee state");
            for(int i=0;i<5;i++){strikes[i]=reader.ReadByte();detention[i]=reader.ReadSingle();correction[i]=reader.ReadSingle();caughtAt[i]=DodgeNet.ReadVector(reader);}
        }
        void OnDestroy()
        {
            if(root)Destroy(root);foreach(var material in materials)if(material)Destroy(material);
        }
    }
}
