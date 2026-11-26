using UnityEngine;

namespace SuperDodgeball
{
    public sealed partial class DodgeReferee
    {
        const float WhistleGesture=1.1f;
        readonly MeshRenderer[] halfInk=new MeshRenderer[2];
        readonly float[] halfLevel=new float[2];
        readonly Transform[] whistleMarks=new Transform[7];
        MaterialPropertyBlock warningInk;
        readonly static int Strength=Shader.PropertyToID("_Strength");
        AudioSource coughSource,whistleSource;
        AudioClip whistle;
        int coughBeat;
        bool audioPaused;
        Vector3 seated;
        Transform leftBoot,rightBoot;
        public float HalfAlert(int team)=>halfLevel[team];
        int CurrentCough(out float age)
        {
            float elapsed = WarningSeconds - Remaining;
            int beat = elapsed >= 1.06f ? 3 : elapsed >= .48f ? 2 : 1;
            age = elapsed - (beat == 3 ? 1.06f : beat == 2 ? .48f : 0f);
            return Phase == DodgeRefereePhase.Warning ? beat : 0;
        }

        internal bool TryGetCoughCue(Camera camera, out Vector2 direction, out int beat, out float age)
        {
            direction = Vector2.zero;
            beat = CurrentCough(out age);
            int slot = app.LocalSlot;
            if (beat == 0 || age > .64f || !app.MatchRunning || !app.ShowReticle ||
                !camera || !head || slot < 0 || slot >= app.Actors.Length) return false;
            var player = app.Actors[slot];
            if (!player || player.IsStunned || IsRestricted(slot) ||
                !IsAcross(player.transform.position, app.Room.seats[slot].team)) return false;

            var viewport = camera.WorldToViewportPoint(head.position);
            if (viewport.z >= camera.nearClipPlane && viewport.x >= 0 && viewport.x <= 1 &&
                viewport.y >= 0 && viewport.y <= 1) return false;

            // Screen-space y points down. A source directly behind points to the
            // bottom edge; turning toward either shoulder moves it to that side.
            var local = camera.transform.InverseTransformPoint(head.position);
            direction = new Vector2(local.x, -local.y + Mathf.Max(0, -local.z));
            direction = direction.sqrMagnitude > .0001f ? direction.normalized : Vector2.up;
            return true;
        }
        void BuildSignals(Material navy,Material cream,Material wood)
        {
            warningInk=new MaterialPropertyBlock();
            var chair=new GameObject("Referee swivel chair").transform;
            chair.SetParent(root.transform,false);chair.position=official.position;
            Part("Chair stem",chair,new Vector3(0,.30f,0),new Vector3(.24f,.30f,.24f),navy,PrimitiveType.Cylinder);
            Part("Seat cushion",chair,new Vector3(0,.65f,0),new Vector3(1.22f,.19f,1.07f),wood);
            Part("Backrest",chair,new Vector3(-.57f,1.08f,0),new Vector3(.16f,.90f,1.06f),wood);
            seated=official.localPosition;
            leftBoot=official.Find("Left foot");rightBoot=official.Find("Right foot");
            for(int i=0;i<whistleMarks.Length;i++)
                whistleMarks[i]=Part("Whistle burst stroke",official,Vector3.zero,Vector3.one,i%2==0?cream:wood);
            var shader=Resources.Load<Shader>("Court/RefereeHalf");
            var paint=new Material(shader);materials.Add(paint);
            for(int team=0;team<2;team++)
            {
                var go=GameObject.CreatePrimitive(PrimitiveType.Quad);go.name="Trespass warning half "+team;
                var trigger=go.GetComponent<Collider>();trigger.enabled=false;Destroy(trigger);
                go.transform.SetParent(root.transform,false);
                go.transform.localRotation=Quaternion.Euler(90,0,0);
                go.transform.localScale=new Vector3(2*DodgeCourt.PlayWidth,DodgeCourt.PlayDepth,1);
                go.transform.localPosition=new Vector3(0,.035f,(team==0?-1:1)*DodgeCourt.PlayDepth*.5f);
                var r=halfInk[team]=go.GetComponent<MeshRenderer>();r.sharedMaterial=paint;
                r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r.receiveShadows=false;r.enabled=false;
            }
            whistle=Resources.Load<AudioClip>("Court/RefereeWhistle");
            coughSource=AudioAtChair(chair,.18f);whistleSource=AudioAtChair(chair,.08f);
        }
        AudioSource AudioAtChair(Transform chair,float spatial)
        {
            var src=chair.gameObject.AddComponent<AudioSource>();src.playOnAwake=false;src.spatialBlend=spatial;
            src.dopplerLevel=0;src.minDistance=8;src.maxDistance=44;return src;
        }
        void Sounds(bool match)
        {
            if(!match)
            {
                coughSource.Stop();whistleSource.Stop();audioPaused=false;return;
            }
            bool pause=!app.SimulationRunning;
            if(audioPaused!=pause)
            {
                if(pause){coughSource.Pause();whistleSource.Pause();}
                else {coughSource.UnPause();whistleSource.UnPause();}
                audioPaused=pause;
            }
            if(pause)return;
            if(Phase==DodgeRefereePhase.Warning)
            {
                int beat=CurrentCough(out _);
                if(beat>coughBeat && warningCough)
                {
                    coughSource.pitch=beat==2?1.09f:beat==3?.94f:1;
                    coughSource.PlayOneShot(warningCough,.59f+.055f*beat);
                }
                coughBeat=beat;
            }
            else coughBeat=0;
            if(Whistles!=heardWhistles)
            {
                heardWhistles=Whistles;
                if(whistle){whistleSource.Stop();whistleSource.PlayOneShot(whistle,.91f);}
                if(app.LocalSlot==LastOffender)game.view.Kick(.023f);
            }
        }
        void PoseCall(bool match,float fold)
        {
            bool call=match && announcementLeft>0;
            float age=WhistleGesture-announcementLeft;
            float jump=call?Mathf.SmoothStep(0,1,(age-.075f)/.18f)*(1-Mathf.SmoothStep(0,1,(age-.67f)/.31f)):0;
            float crouch=call?Mathf.Max(0,1-Mathf.Abs(age-.07f)/.07f):0;
            float tremble=jump*Mathf.Sin(age*103);
            float cough=match && Phase==DodgeRefereePhase.Warning?Mathf.Max(0,Mathf.Cos((WarningSeconds-Remaining)*11.8f))*(1-fold):0;
            official.localPosition=seated+new Vector3(tremble*.068f,1.48f*jump-.15f*crouch-.07f*cough,0);
            official.localScale=1.36f*new Vector3(1+.10f*crouch,1-.16f*crouch+.07f*jump,1+.06f*crouch);
            official.localRotation*=Quaternion.Euler(5*cough,0,tremble*6f);
            for(int s=0;s<2;s++)
            {
                var foot=s==0?leftBoot:rightBoot;float side=s==0?-1:1;
                foot.localPosition=new Vector3(side*(.24f+jump*.14f),.33f+jump*.1f,-.18f);
                foot.localRotation=Quaternion.Euler(24f+jump*(s==0?34:-46),0,side*jump*24);
            }
            for(int i=0;i<whistleMarks.Length;i++)
            {
                var mark=whistleMarks[i];mark.gameObject.SetActive(jump>.1f);
                if(jump<=.1f)continue;
                float angle=i*2*Mathf.PI/whistleMarks.Length;
                var ray=new Vector3(Mathf.Cos(angle),.32f+Mathf.Sin(angle)*.48f,Mathf.Sin(angle)).normalized;
                mark.localPosition=Vector3.up*1.78f+ray*(.63f+.26f*jump);
                mark.localScale=new Vector3(.052f,.27f*jump,.057f);
                mark.localRotation=Quaternion.FromToRotation(Vector3.up,ray);
            }
        }
        void DrawHalves(bool match)
        {
            for(int team=0;team<2;team++)
            {
                float urgency=0;
                if(match)for(int i=0;i<5;i++)
                {
                    if(app.Room.seats[i].team!=1-team || (trespass[i]<=0 && !Chasing(i)))continue;
                    if(Chasing(i) && !IsAcross(app.Actors[i].transform.position,1-team))continue;
                    urgency=Mathf.Max(urgency,Chasing(i)?1:Mathf.Lerp(.35f,1,trespass[i]/TrespassLimit));
                }
                halfLevel[team]=urgency;
                var r=halfInk[team];r.enabled=urgency>0;
                if(!r.enabled)continue;
                float pulse=.5f+.5f*Mathf.Cos(Clock*7.6f);
                warningInk.SetFloat(Strength,(.42f+.48f*pulse)*(.60f+.40f*urgency));
                r.SetPropertyBlock(warningInk);
            }
        }
    }
}
