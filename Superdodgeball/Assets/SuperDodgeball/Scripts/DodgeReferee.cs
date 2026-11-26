using System;
using System.IO;
using UnityEngine;

namespace SuperDodgeball
{
    public enum DodgeRefereePhase { Slacking, Warning, Watching }

    // The host judges root positions; clients only display the replicated decision.
    [DefaultExecutionOrder(-180)]
    public sealed partial class DodgeReferee : MonoBehaviour
    {
        public DodgeRefereePhase Phase { get; private set; }
        public float Remaining { get; private set; }
        public float Clock { get; private set; }
        public float PendingTea {get;private set;}
        public bool OfferTea() {if(DodgeGame.IsReplica || PendingTea>0)return false;PendingTea=6f;return true;}
        public int Whistles { get; private set; }
        public int LastOffender { get; private set; } = -1;
        public int GuardCalls { get; private set; }
        public const float WarningSeconds=2.2f;
        readonly int[] strikes=new int[5];
        readonly float[] trespass=new float[5], grace=new float[5], decay=new float[5], detention=new float[5];
        readonly Vector3[] caughtAt=new Vector3[5];
        readonly System.Collections.Generic.List<Material> materials=new System.Collections.Generic.List<Material>();
        DodgeApp app; DodgeGame game;
        AudioClip warningCough;
        GameObject root;Transform official,head,leftArm,rightArm,cup,paper,card,lamp;
        Transform centerLine;
        float announcementLeft;int heardWhistles;
        System.Random random=new System.Random();
        public int Strikes(int slot)=>strikes[slot];
        public float GuardRemaining(int slot)=>detention[slot];
        public bool IsRestricted(int slot)=>app && app.Page==DodgePage.Playing && slot>=0 && slot<5 && detention[slot]>0f;
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
            warningCough = Resources.Load<AudioClip>("Court/RefereeCough");
            var cream=Mat(new Color(1f,.96f,.84f));var navy=Mat(new Color(.19f,.28f,.34f));
            var skin=Mat(new Color(.98f,.77f,.51f));var yellow=Mat(new Color(1f,.78f,.2f));
            var mint=Mat(new Color(.5f,.83f,.72f));
            Part("Referee podium",root.transform,new Vector3(-10.4f*DodgeCourt.Scale,.65f,0),new Vector3(1.5f,1.3f,2.6f),mint);
            Part("Podium rim", root.transform, new Vector3(-10.4f*DodgeCourt.Scale, 1.24f, 0f),
                new Vector3(1.55f, .14f, 2.65f), yellow);
            official=Person("Lazy referee",root.transform,skin,navy,cream,false);
            official.localPosition=new Vector3(-10.4f*DodgeCourt.Scale,1.3f,0);official.localScale=Vector3.one*1.36f;
            head=official.Find("Head");leftArm=official.Find("Left arm");rightArm=official.Find("Right arm");
            official.Find("Body").localScale = new Vector3(.86f, .66f, .68f);
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI / 4f;
                var stripe = Part("Referee shirt stripe", official,
                    new Vector3(.427f * Mathf.Sin(angle), 1.03f, .338f * Mathf.Cos(angle)),
                    new Vector3(.085f, .55f, .028f), cream);
                stripe.localRotation = Quaternion.Euler(0f, i * 45f, 0f);
            }
            cup=Part("Slacking drink",rightArm,new Vector3(0,-.47f,-.1f),new Vector3(.24f,.3f,.24f),mint,PrimitiveType.Cylinder);
            Part("Drink straw",cup,new Vector3(0,.6f,0),new Vector3(.08f,.6f,.08f),yellow);
            paper = new GameObject("Sports newspaper").transform;
            paper.SetParent(leftArm, false);
            paper.localPosition = new Vector3(.10f, -.43f, -.25f);
            for (int side = -1; side <= 1; side += 2)
            {
                var page = Part("Folded sports page", paper, new Vector3(side * .24f, 0f, 0f),
                    new Vector3(.49f, .69f, .035f), cream);
                page.localRotation = Quaternion.Euler(0f, side * 13f, 0f);
                Part("Masthead", page, new Vector3(0f, .32f, -.57f), new Vector3(.79f, .13f, .12f), navy);
                Part("Photo block", page, new Vector3(-.17f, .04f, -.57f), new Vector3(.32f, .26f, .12f), mint);
                for (int row = 0; row < 3; row++)
                    Part("Print column", page, new Vector3(.20f, .12f - row * .13f, -.57f),
                        new Vector3(.26f, .035f, .12f), navy);
            }
            card=Part("Yellow card",rightArm,new Vector3(0,-.52f,0),new Vector3(.25f,.36f,.035f),yellow);
            Part("Whistle",official,new Vector3(0,1.65f,-.36f),new Vector3(.14f,.12f,.17f),yellow,PrimitiveType.Sphere);
            Part("Midline ink edge", root.transform, new Vector3(0f, .039f, 0f),
                new Vector3(20.5f * DodgeCourt.Scale, .012f, .35f), navy);
            lamp=Part("Referee attention lamp",root.transform,new Vector3(-10.4f*DodgeCourt.Scale,4.4f,-1.12f),Vector3.one*.45f,Mat(new Color(.5f,.83f,.72f)),PrimitiveType.Sphere);
            centerLine=Part("Visible center rule",root.transform,new Vector3(0,.053f,0),new Vector3(20.5f*DodgeCourt.Scale,.025f,.22f),Mat(new Color(1f,.78f,.2f)));
            BuildSignals(navy,cream,yellow);
            for(int slot=0;slot<5;slot++)
                for(int side=0;side<2;side++)
                {
                    var person=Person("Security "+slot+" / "+side,root.transform,skin,navy,navy,true);
                    Part("Security glasses",person,new Vector3(0,1.81f,-.33f),new Vector3(.62f,.14f,.10f),navy);
                    Part("Jacket badge",person,new Vector3(-.20f,1.20f,-.33f),new Vector3(.15f,.17f,.06f),yellow);
                    var guard=guards[slot,side]=person.gameObject.AddComponent<DodgeSecurityGuard>();
                    guard.Initialize(side);
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
            heardWhistles=coughBeat=0;Array.Clear(halfLevel,0,halfLevel.Length);
            for(int i=0;i<5;i++)
            {
                strikes[i]=0;grace[i]=decay[i]=detention[i]=0;Dismiss(i);
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
            Sounds(app.Page==DodgePage.Playing);
            if(visible)Draw();
        }
        void Tick(float dt)
        {
            Clock+=dt;Remaining-=dt;announcementLeft=Mathf.Max(0,announcementLeft-dt);
            if(Remaining<=0f)
            {
                if(Phase==DodgeRefereePhase.Slacking){Phase=DodgeRefereePhase.Warning;Remaining=WarningSeconds;}
                else if(Phase==DodgeRefereePhase.Warning){Phase=DodgeRefereePhase.Watching;Remaining=4.5f+(float)random.NextDouble()*1.5f;}
                else {Phase=DodgeRefereePhase.Slacking;Remaining=7f+(float)random.NextDouble()*3f+PendingTea;PendingTea=0;}
                Array.Clear(trespass,0,trespass.Length);Array.Clear(warned,0,warned.Length);
            }
            for(int i=0;i<5;i++)
            {
                if(app.Room.seats[i].kind==DodgeSeatKind.Empty)
                {
                    if(Chasing(i) || detention[i]>0){detention[i]=0;Dismiss(i);}
                    continue;
                }
                var actor=app.Actors[i];
                if(detention[i]>0){Escort(i,dt);continue;}
                if(Chasing(i)){Pursue(i,dt);continue;}
                grace[i]=Mathf.Max(0,grace[i]-dt);
                decay[i]+=dt;if(decay[i]>(actor.Kit.Has(DodgeSkill.GoodBehaviour)?15f:25f) && strikes[i]>0){strikes[i]--;decay[i]=0;}
                // Getting launched across the line is not deliberate cheating.
                if(actor.IsStunned){grace[i]=1.8f;trespass[i]=0;warned[i]=false;continue;}
                bool across=IsAcross(actor.transform.position,app.Room.seats[i].team);
                if(!across || Phase!=DodgeRefereePhase.Watching){trespass[i]=0;warned[i]=false;continue;}
                if(grace[i]>0 || !SeesItem(actor)){trespass[i]=0;continue;}
                trespass[i]+=dt;
                if(!warned[i] && trespass[i]>=.14f){warned[i]=true;Cite(i);}
                if(trespass[i]>=TrespassLimit)BeginChase(i,false);
            }
        }
        float FacingYaw => Phase==DodgeRefereePhase.Slacking ? 90+Mathf.Sin(Clock*.4f)*25 :
            Phase==DodgeRefereePhase.Warning ? Mathf.Lerp(90,-90,Mathf.SmoothStep(0,1,(WarningSeconds-Remaining-1.10f)/(WarningSeconds-1.10f))) : -90+Mathf.Sin(Clock*.7f)*22;
        public bool SeesItem(DodgeActor actor)
        {
            if(!actor || !head || Phase==DodgeRefereePhase.Slacking)return false;
            var eyes=head.position;
            var chest=actor.Puppet.BodyCenter+Vector3.up*.16f;
            var flat=Vector3.ProjectOnPlane(chest-eyes,Vector3.up);
            var facing=Quaternion.Euler(0,FacingYaw+Mathf.Sin(Clock)*8f,0)*Vector3.back;
            if(flat.sqrMagnitude>28f*28f || Vector3.Dot(facing,flat.normalized)<.42f)return false;
            return DodgePartyEffects.LineClear(eyes,chest);
        }
        internal bool JudgeItem(DodgeActor actor,DodgeItem item)
        {
            int slot=app.SeatOf(actor);
            if(!app.IsHost || slot<0 || !SeesItem(actor))return false;
            Cite(slot);
            if(strikes[slot]>=3)BeginChase(slot,true);
            return true;
        }
        void Cite(int slot)
        {
            strikes[slot]++;Whistles++;LastOffender=slot;announcementLeft=WhistleGesture;decay[slot]=0;
        }
        void Draw()
        {
            bool match=app.Page==DodgePage.Playing;
            bool slack=Phase==DodgeRefereePhase.Slacking;
            bool warning=Phase==DodgeRefereePhase.Warning;
            Color color=slack?new Color(.12f,.83f,.68f):warning?new Color(1f,.78f,.10f):new Color(1f,.24f,.16f);
            lamp.GetComponent<Renderer>().sharedMaterial.SetColor("_BaseColor",color);
            centerLine.GetComponent<Renderer>().sharedMaterial.SetColor("_BaseColor",color);
            centerLine.localScale=new Vector3(20.5f*DodgeCourt.Scale,.025f,warning?.24f+Mathf.Sin(Clock*8f)*.045f:.22f);
            float facing=FacingYaw;
            official.localRotation=Quaternion.Euler(0,facing,slack?Mathf.Sin(Clock*1.4f)*3:0);
            head.localRotation=Quaternion.Euler(slack?18f:0f,Mathf.Sin(Clock)*8,0);
            bool call=announcementLeft>0f && LastOffender>=0;
            float fold = warning ? Mathf.SmoothStep(0f, 1f, (WarningSeconds - Remaining) / .55f) : (slack ? 0f : 1f);
            leftArm.localRotation = Quaternion.Euler(Mathf.Lerp(-88f, -15f, fold), 0f, Mathf.Lerp(15f, 25f, fold));
            paper.localScale = new Vector3(Mathf.Lerp(1f, .10f, fold), 1f, 1f);
            paper.localRotation = Quaternion.Euler(72f, 0f, slack ? Mathf.Sin(Clock * 2.3f) * 4f : 0f);
            rightArm.localRotation=Quaternion.Euler(call?-160f:slack?-65f:warning?-100f:-10f,0,slack?-15f:-30f);
            paper.gameObject.SetActive(slack || (warning && fold < 1f));cup.gameObject.SetActive(slack);card.gameObject.SetActive(call);
            PoseCall(match,fold);DrawHalves(match);
            for(int slot=0;slot<5;slot++)
                for(int side=0;side<2;side++)
                {
                    var guard=guards[slot,side];
                    if(!match && guard.gameObject.activeSelf)guard.Leave();
                    else guard.Pose(Clock,app.SimulationRunning?Time.deltaTime:0);
                }
        }
        public void WriteState(BinaryWriter writer)
        {
            writer.Write(PendingTea);writer.Write((byte)Phase);writer.Write(Remaining);writer.Write(Clock);writer.Write(Whistles);writer.Write(GuardCalls);writer.Write(LastOffender);writer.Write(announcementLeft);
            for(int i=0;i<5;i++){writer.Write((byte)strikes[i]);writer.Write(detention[i]);writer.Write(trespass[i]);writer.Write(chase[i]);
                if(chase[i]>0 || detention[i]>0)for(int side=0;side<2;side++)guards[i,side].Write(writer);}
        }
        public void ReadState(BinaryReader reader)
        {
            PendingTea=reader.ReadSingle();Phase=(DodgeRefereePhase)reader.ReadByte();Remaining=reader.ReadSingle();Clock=reader.ReadSingle();Whistles=reader.ReadInt32();GuardCalls=reader.ReadInt32();LastOffender=reader.ReadInt32();announcementLeft=reader.ReadSingle();
            if((int)Phase>2 || LastOffender< -1 || LastOffender>=5)throw new InvalidDataException("Invalid referee state");
            for(int i=0;i<5;i++){strikes[i]=reader.ReadByte();detention[i]=reader.ReadSingle();trespass[i]=reader.ReadSingle();chase[i]=reader.ReadSingle();
                for(int side=0;side<2;side++){if(chase[i]>0 || detention[i]>0)guards[i,side].Read(reader);else if(guards[i,side].gameObject.activeSelf)guards[i,side].Leave();}}
        }
        void OnDestroy()
        {
            if(root)Destroy(root);foreach(var material in materials)if(material)Destroy(material);
        }
    }
}
