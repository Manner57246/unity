using UnityEngine;

namespace SuperDodgeball
{
    public sealed partial class DodgeClubhouse
    {
        internal void BakeVenue(CourtSketchMesh geometry,Transform root,string name)
            =>roomMeshes.Add(geometry.BakeSurfaces(name,root,RoomFinish));
    }

    public sealed partial class DodgeVenueDirector
    {
        const int Steel=0x597078, Shadow=0x30464e, Iron=0x849398, Ocher=0xd38f3c, Rust=0xb9603c,
            Chalk=0xf5dfac, Safety=0xeaba56, Sea=0x387c84, Ice=0xb5e6df, Cerulean=0x427989, Hot=0xffbc72;
        readonly Transform[] rotors=new Transform[4],jets=new Transform[6],pilotLamps=new Transform[6];
        ParticleSystem vapour;
        float vapourCount;

        static Transform Group(Transform parent,string name,Vector3 p)
        {
            var root=new GameObject(name).transform;root.SetParent(parent,false);root.localPosition=p;return root;
        }
        Transform Painted(CourtSketchMesh m,Transform parent,string label)
        {
            var go=m.Bake(label,parent,ink);meshes.Add(go.GetComponent<MeshFilter>().sharedMesh);
            return go.transform;
        }
        void BuildVenue()
        {
            var root=Group(app.Stadium.transform,Title(venue),Vector3.zero);
            sets[(int)venue]=root.gameObject;
            var m=new CourtSketchMesh();
            bool factory=venue==DodgeVenue.Warehouse;
            BuildFloor(m,factory);
            if(factory)BuildWarehouse(m,root);else BuildFlume(m,root);
            app.Clubhouse.BakeVenue(m,root,factory?"Loading hall and dispatch floor":"Exhaust gallery and diffuser deck");
        }

        static void BuildFloor(CourtSketchMesh m,bool factory)
        {
            float w=DodgeCourt.HalfWidth,h=DodgeCourt.HalfDepth;
            m.Box(factory?0xa79577:0x758e94,new(0,-.033f,0),new(2*w,.09f,2*h),.001f);
            for(int k=-3;k<=3;k++)
                m.Box(factory?0x928470:0x627c83,new(k*3.75f,.014f,0),new(.035f,.002f,2*h-.15f));
            for(int k=-4;k<=4;k++)
                m.Box(factory?0x928470:0x627c83,new(0,.014f,k*3.75f),new(2*w-.15f,.002f,.037f));
            for(int side=-1;side<=1;side+=2)
            {
                m.Box(Chalk,new(side*(w-.40f),.019f,0),new(.12f,.003f,2*h-.75f));
                m.Box(Chalk,new(0,.019f,side*(h-.40f)),new(2*w-.72f,.003f,.12f));
                for(int k=0;k<7;k++)
                {
                    float z=(k-3)*4.25f;
                    m.Box(factory?Safety:Ice,new(side*(w-.78f),.018f,z),new(.48f,.003f,.095f));
                }
            }
            m.Box(Chalk,new(0,.018f,0),new(2*w-.78f,.003f,.14f));
            m.Ring(factory?Safety:Ice,new(0,.021f,0),2.35f,2.35f,.11f,48);
        }
        void BuildWarehouse(CourtSketchMesh m,Transform root)
        {
            // Elevated service galleries sit behind the stands and connect the two loading bays.
            for(int wall=-1;wall<=1;wall+=2)
            {
                float x=wall*20.5f;
                m.Box(Shadow,new(x,7.70f,0),new(1.45f,.26f,39.8f));
                for(int rail=0;rail<2;rail++)m.Box(Ocher,new(x-wall*.65f,8.15f+rail*.44f,0),new(.065f,.07f,39.6f));
                for(int n=0;n<9;n++)
                {
                    float z=-18.8f+n*4.7f;
                    m.Box(Steel,new(x,4.6f,z),new(.27f,8.8f,.30f));
                    m.Box(Ocher,new(x-wall*.65f,8.16f,z),new(.08f,.96f,.08f));
                    m.Stick(Shadow,new(x,6.04f,z),new(x,7.56f,z+1.85f),.18f);
                }
            }
            for(int end=-1;end<=1;end+=2)
            {
                m.Push(new(0,0,end*20.78f),end<0?180:0);
                for(int wing=-1;wing<=1;wing+=2)
                {
                    m.Push(new(wing*13.45f,6.9f,0));
                    m.Box(Shadow,new(0,1.5f,.05f),new(9.7f,7.95f,.23f));
                    for(int bay=-1;bay<=1;bay+=2)
                    {
                        m.Box(Steel,new(bay*4.48f,-.90f,-.35f),new(.34f,12.84f,.66f));
                        m.Box(Ocher,new(bay*4.48f,-5.80f,-.36f),new(.47f,1.37f,.73f));
                        for(int level=0;level<2;level++)
                        {
                            float y=-.80f+level*3.28f;
                            m.Box(bay==wing?Rust:0xc08e54,new(bay*2.15f,y+1.15f,-.46f),new(3.47f,2.20f,.98f));
                            for(int band=-1;band<=1;band+=2)
                                m.Box(Shadow,new(bay*2.15f+band*.87f,y+1.15f,-.99f),new(.14f,2.18f,.05f));
                            m.Box(Chalk,new(bay*2.15f+.49f,y+1.41f,-1f),new(.69f,.42f,.055f));
                        }
                    }
                    for(int k=0;k<3;k++)m.Box(Ocher,new(0,-1f+k*3.28f,-.68f),new(9.22f,.22f,.85f));
                    m.Pop();
                }
                m.Box(Ocher,new(0,13.1f,-1.3f),new(40,.44f,.75f));
                m.Box(Ocher,new(0,14.43f,-1.3f),new(40,.21f,.52f));
                for(int n=0;n<16;n++)
                    m.Stick(Ocher,new(-20+n*2.5f,n%2==0?13.26f:14.33f,-1.5f),new(-17.5f+n*2.5f,n%2==0?14.33f:13.26f,-1.5f),.18f);
                m.Box(Steel,new(0,13.48f,-1.3f),new(40,.15f,.88f));
                m.Push(new(end*8.7f,12.9f,-1.47f));
                m.Box(Shadow,Vector3.zero,new(2.1f,.60f,.95f));
                for(int k=-1;k<=1;k+=2)m.Stick(Steel,new(k*.45f,-.2f,0),new(k*.37f,-2.24f,0),.055f);
                m.Box(Safety,new(0,-2.45f,0),new(1.15f,.42f,.58f));
                m.Stick(Shadow,new(0,-2.66f,0),new(-.27f,-3.18f,0),.14f);
                m.Stick(Shadow,new(-.27f,-3.18f,0),new(-.54f,-2.97f,0),.14f);
                m.Pop();m.Pop();
            }
            for(int side=-1;side<=1;side+=2)
                foreach(float z in new float[]{-12,-8,-4,4,8,12})
                {
                    m.Push(new(side*13.53f,0,z),side<0?180:0);
                    m.Push(new(.29f,.52f,0),Quaternion.Euler(0,0,18));
                    m.Box(Shadow,Vector3.zero,new(1.45f,.15f,2.16f));
                    for(int j=-2;j<=2;j++)m.Box(Iron,new(j*.28f,.11f,0),new(.12f,.1f,1.95f));
                    for(int j=-1;j<=1;j+=2)m.Box(Ocher,new(0,.19f,j*1.03f),new(1.50f,.24f,.13f));
                    m.Pop();m.Pop();
                }

            for(int i=0;i<Lanes.Length;i++)
            {
                var part=new CourtSketchMesh();float radius=Radius(i);
                part.Push(Vector3.zero,Quaternion.Euler(90,0,0));
                if(i%3==0)
                {
                    part.Drum(Rust,Vector3.zero,new(1.53f,1.24f,1.53f),20);
                    for(int k=-3;k<=3;k++)part.Drum(0x955038,new(0,k*.15f,0),new(1.6f,.075f,1.6f),20);
                }
                else part.Drum(i%3==1?Shadow:Steel,Vector3.zero,new(radius*1.78f,1.38f,radius*1.78f),24);
                for(int face=-1;face<=1;face+=2)
                {
                    part.Drum(i%3==0?Ocher:i%3==1?0x34444a:Iron,new(0,face*.68f,0),new(radius*1.91f,.17f,radius*1.91f),24);
                    part.Drum(i%3==0?Steel:Shadow,new(0,face*.79f,0),new(.88f,.09f,.88f),14);
                    part.Drum(Safety,new(0,face*.85f,0),new(.31f,.035f,.31f),8);
                    for(int k=0;k<6;k++)
                    {
                        float a=k*60*Mathf.Deg2Rad;
                        part.Drum(i%3==0?Shadow:Iron,new(Mathf.Cos(a)*.32f,face*.85f,Mathf.Sin(a)*.32f),new(.10f,.045f,.10f),6);
                    }
                }
                for(int tooth=0;tooth<12;tooth++)
                {
                    part.Push(Vector3.zero,tooth*30);
                    if(i%3!=0)part.Box(i%3==1?0x415058:Steel,new(0,0,radius*.91f),new(.24f,1.16f,.20f));
                    part.Pop();
                }
                part.Pop();loads[i]=Painted(part,root,"Rolling "+new[]{"cable reel","forklift tyre","gear drum"}[i%3]);loads[i].gameObject.SetActive(false);
                var line=new CourtSketchMesh();
                for(int k=-5;k<=5;k++)
                {
                    float x=k*2.35f;
                    for(int s=-1;s<=1;s+=2)
                    {
                        line.Ground(Safety,.058f,new(x-.48f,s*.48f),new(x-.20f,s*.48f),new(x+.35f,0),new(x+.07f,0));
                        line.Box(Ocher,new(x,.052f,s*.96f),new(1.22f,.004f,.063f));
                    }
                }
                tracks[i]=Painted(line,root,"Roller lane warning");
                tracks[i].localPosition=Vector3.forward*Lanes[i];
                tracks[i].localRotation=Quaternion.Euler(0,Direction(i)==1?0:180,0);tracks[i].gameObject.SetActive(false);
            }
        }

        static void FanBlades(CourtSketchMesh m)
        {
            for(int k=0;k<7;k++)
            {
                m.Push(Vector3.zero,Quaternion.Euler(0,0,k*360f/7));
                m.Face(Sea,new(.08f,.18f,0),new(-.58f,1.02f,-.1f),new(-.48f,1.67f,-.17f),new(.22f,1.56f,-.12f),new(.36f,.68f,0));
                m.Pop();
            }
            m.Push(Vector3.zero,Quaternion.Euler(90,0,0));
            m.Drum(Chalk,Vector3.zero,new(.7f,.27f,.7f),16);m.Pop();
        }
        void BuildFlume(CourtSketchMesh m,Transform root)
        {
            int f=0;
            for(int end=-1;end<=1;end+=2)
            {
                var wall=Group(root,"Turbine gallery",new(0,0,end*20.78f));
                wall.localRotation=Quaternion.Euler(0,end<0?180:0,0);
                m.Push(wall.localPosition,wall.localRotation);
                for(int side=-1;side<=1;side+=2)
                {
                    m.Stick(Cerulean,new(side*20.8f,3.3f,-.30f),new(side*19.1f,10.4f,-.34f),.68f,.83f);
                    m.Stick(Cerulean,new(side*19.1f,10.4f,-.34f),new(side*11.8f,14.14f,-.34f),.66f,.83f);
                }
                m.Box(Cerulean,new(0,14.14f,-.34f),new(24.1f,.61f,.83f));
                for(int s=-1;s<=1;s+=2)
                {
                    m.Push(new(s*13.38f,8.9f,0));
                    m.Box(0x426670,new(0,0,.04f),new(9.8f,8f,.22f),.16f);
                    for(int j=0;j<3;j++)m.Box(Cerulean,new(s*(2.85f+j*.55f),0,-.15f),new(.26f,6.95f,.34f));
                    m.Push(new(0,0,-.26f),Quaternion.Euler(90,0,0));
                    m.Drum(Ice,Vector3.zero,new(4.95f,.37f,4.95f),40);
                    m.Drum(Shadow,new(0,-.26f,0),new(4.41f,.24f,4.41f),40);
                    m.Pop();m.Pop();
                    var blades=new CourtSketchMesh();FanBlades(blades);
                    var rotor=rotors[f++]=Painted(blades,wall,"Intake rotor");rotor.localPosition=new(s*13.38f,8.9f,-.73f);
                    for(int k=0;k<3;k++)m.Box(Ice,new(s*(9.8f+k*.43f),5.85f,-.30f),new(.16f,.61f,.17f));
                }
                m.Pop();
            }
            // The longitudinal troughs sit beyond the playable sidelines. A sloped lip turns the exhaust upward.
            for(int side=-1;side<=1;side+=2)
            {
                for(int bay=0;bay<8;bay++)
                {
                    float z=-16+bay*4.06f;
                    Vector3 lo=new(side*13.49f,.10f,z), hi=new(side*14.52f,1.56f,z);
                    Vector3 along=Vector3.forward*3.96f;
                    m.Face(bay%2==0?Cerulean:Sea,lo,hi,hi+along,lo+along);
                    m.Face(bay%2==0?Cerulean:Sea,lo+along,hi+along,hi,lo);
                    m.Stick(Ice,lo,hi,.095f);m.Stick(Ice,hi,hi+along,.10f);
                    for(int n=0;n<2;n++)
                        m.Stick(Shadow,lo+Vector3.forward*(.86f+n*1.58f),hi+Vector3.forward*(1.20f+n*1.58f),.20f);
                }
                m.Box(Shadow,new(side*8.0f,.015f,0),new(.27f,.003f,31));
                m.Box(Ice,new(side*7.69f,.018f,0),new(.06f,.003f,31));
                for(int n=0;n<7;n++)m.Box(Chalk,new(side*8.0f,.019f,(n-3)*4.45f),new(.69f,.003f,.09f));
            }
            for(int i=0;i<6;i++)
            {
                float x=(i%2==0?-1:1)*10.05f,z=(i/2-1)*10.3f;
                m.Push(new(x,0,z));
                m.Ring(Shadow,new(0,.019f,0),1.52f,1.52f,.28f,36);
                m.Ring(Ice,new(0,.027f,0),1.40f,1.40f,.09f,36);
                for(int k=-4;k<=4;k++)
                    m.Box(Sea,new(k*.24f,.021f,0),new(.16f,.011f,2*Mathf.Sqrt(1.50f-k*k*.0576f)));
                m.Pop();
                var pilot=new CourtSketchMesh();pilot.Ring(Hot,Vector3.zero,1.51f,1.51f,.13f,36);
                pilotLamps[i]=Painted(pilot,root,"Ignition rim");pilotLamps[i].localPosition=new(x,.044f,z);pilotLamps[i].gameObject.SetActive(false);
                var plume=new CourtSketchMesh();
                for(int k=0;k<4;k++)
                {
                    plume.Push(Vector3.zero,k*90+i*17);
                    int colour=k%2==0?Hot:Ice;
                    plume.Face(colour,new(.87f,.04f,-.08f),new(.72f,1.15f,-.08f),new(.35f,2.3f,.12f),new(.64f,1.00f,.09f));
                    plume.Pop();
                }
                jets[i]=Painted(plume,root,"Exhaust core");jets[i].localPosition=new(x,0,z);jets[i].gameObject.SetActive(false);
            }
            var emitter=Group(root,"Updraft vapour",Vector3.zero);
            vapour=emitter.gameObject.AddComponent<ParticleSystem>();
            vapour.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=vapour.main;main.playOnAwake=false;main.loop=true;main.maxParticles=190;main.simulationSpace=ParticleSystemSimulationSpace.Local;
            var emission=vapour.emission;emission.enabled=false;
            var shape=vapour.shape;shape.enabled=false;
            var fade=vapour.colorOverLifetime;fade.enabled=true;var gradient=new Gradient();
            gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(.08f,0),new GradientAlphaKey(.58f,.16f),new GradientAlphaKey(0,1)});
            fade.color=gradient;
            var renderer=vapour.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=DodgeWeaponVfx.PuffMaterial();
            renderer.renderMode=ParticleSystemRenderMode.Stretch;renderer.velocityScale=.09f;renderer.lengthScale=1.2f;
        }

        void DrawJets(float now,bool pre,bool wind)
        {
            float strength=wind?Mathf.SmoothStep(0,1,now/.55f)*(1-Mathf.SmoothStep(0,1,(now-5.4f)/1.85f)):0;
            // Integrate the fan only while the simulated clock advances, including in LAN play.
            float dt=Mathf.Clamp(now-lastAge,0,.1f);
            for(int i=0;i<rotors.Length;i++)rotors[i].Rotate(0,0,(12+strength*230)*dt);
            for(int i=0;i<jets.Length;i++)
            {
                pilotLamps[i].gameObject.SetActive(wind || pre && Mathf.Sin(now*11)>-.3f);
                jets[i].gameObject.SetActive(strength>.02f);
                if(!jets[i].gameObject.activeSelf)continue;
                jets[i].localScale=new(1,(.85f+.15f*Mathf.Sin(now*17+i))*strength,1);
                jets[i].localRotation=Quaternion.Euler(0,now*68+i*30,0);
            }
            if(!wind && !pre)
            {
                if(!vapour.isStopped)vapour.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
                vapourCount=0;return;
            }
            if(!app.SimulationRunning){if(vapour.isPlaying)vapour.Pause();return;}
            if(!vapour.isPlaying)vapour.Play();
            vapourCount+=dt*(pre?3:18*strength);
            while(vapourCount>=1)
            {
                vapourCount--;
                if(wind)
                {
                    foreach(var actor in app.Actors)
                    {
                        if(!actor.isActiveAndEnabled)continue;
                        var feet=actor.transform.position;
                        float curl=now*12+feet.z;
                        vapour.Emit(new ParticleSystem.EmitParams{position=feet+new Vector3(.55f*Mathf.Cos(curl),-.7f,.45f*Mathf.Sin(curl)),
                            velocity=new Vector3(.25f*Mathf.Sin(curl),3.4f,0),startLifetime=.43f,startSize=.22f,startColor=new Color(.72f,.94f,.93f,.67f)},1);
                    }
                }
                for(int i=0;i<jets.Length;i++)
                {
                    float a=now*33+i*2.4f+vapourCount;
                    var radial=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));
                    vapour.Emit(new ParticleSystem.EmitParams{position=jets[i].localPosition+radial*.90f+Vector3.up*.17f,
                        velocity=Vector3.up*(pre?1.45f:5.2f+1.4f*Mathf.Cos(a)) +radial*.55f,
                        startLifetime=pre?.65f:1.25f,startSize=pre?.33f:.60f,startColor=Color.Lerp(new Color(.70f,.95f,.94f),new Color(1,.89f,.67f),.25f+.25f*Mathf.Sin(a))},1);
                }
            }
        }
    }
}
