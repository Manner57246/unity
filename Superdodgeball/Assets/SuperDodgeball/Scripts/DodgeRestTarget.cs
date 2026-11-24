using UnityEngine;
namespace SuperDodgeball
{
    // Physical signs work through the same swept ball collisions as the clubhouse.
    public sealed class DodgeRestTarget:MonoBehaviour
    {
        public int Team,Skill=-1;
        public DodgeItem Item;
        public static bool CanAim(DodgeActor actor,Vector3 aim)
        {
            if(!actor || !DodgeCourt.InRest(actor.transform.position))return false;
            foreach(var target in FindObjectsByType<DodgeRestTarget>(FindObjectsSortMode.None))
                if(target.Eligible(actor) && Vector3.Distance(aim,target.transform.position)<1.7f)return true;
            return false;
        }
        bool Eligible(DodgeActor actor)
        {
            var app=DodgeApp.Instance;int slot=app.SeatOf(actor);
            return slot>=0 && app.Room.seats[slot].team==Team && DodgeCourt.InRest(actor.transform.position)
                && (app.Page==DodgePage.Staging || app.Page==DodgePage.Playing);
        }
        public bool Hit(DodgeBall ball)
        {
            if(DodgeGame.IsReplica || !ball.owner || ball.IsEgg || !ball.IsDangerous || !Eligible(ball.owner))return false;
            bool success=Skill>=0?ball.owner.Kit.Upgrade((DodgeSkill)Skill):ball.owner.Kit.ClaimStarter(Item);
            if(success){DodgeGame.Instance.EmitFoam(transform.position,Vector3.up,10,1);DodgeGame.Instance.PlayTone(880,.12f,.08f);}
            else DodgeGame.Instance.PlayTone(180,.08f,.04f);
            return true;
        }
        static readonly float[] nextBotShot=new float[5];
        public static void ResetBots(){System.Array.Clear(nextBotShot,0,5);}
        public static bool BotVisit(DodgeBot bot,CharacterController motor)
        {
            var app=DodgeApp.Instance;var actor=bot.Actor;int slot=app.SeatOf(actor);
            if(slot<0 || actor.Kit.Points<=0 || actor.Kit.Skills==63 || app.Page!=DodgePage.Playing || actor.IsStunned)return false;
            int team=app.Room.seats[slot].team;float sign=team==0?-1:1;
            Vector3 target=Mathf.Abs(actor.transform.position.z)<DodgeCourt.RestZ(18)?new Vector3(0,.05f,sign*DodgeCourt.RestZ(19)):new Vector3(0,.05f,sign*DodgeCourt.RestZ(22));
            Vector3 delta=Vector3.ProjectOnPlane(target-actor.transform.position,Vector3.up);
            bot.CancelForReferee();
            motor.Move((delta.normalized*Mathf.Min(4,delta.magnitude*3)+Vector3.down*2)*Time.deltaTime);
            actor.SetLocomotion(Mathf.Clamp01(delta.magnitude));
            if(delta.sqrMagnitude>.1f)actor.transform.rotation=Quaternion.RotateTowards(actor.transform.rotation,Quaternion.LookRotation(delta),360*Time.deltaTime);
            if(Mathf.Abs(actor.transform.position.z)>DodgeCourt.RestZ(20))BotChoose(bot);
            return true;
        }
        public static void BotChoose(DodgeBot bot)
        {
            var app=DodgeApp.Instance;var actor=bot.Actor;int slot=app.SeatOf(actor);
            if(Time.time<nextBotShot[slot])return;
            DodgeRestTarget chosen=null;
            foreach(var target in FindObjectsByType<DodgeRestTarget>(FindObjectsSortMode.None))
            {
                if(target.Team!=app.Room.seats[slot].team)continue;
                if(actor.Kit.Points>0 && actor.Kit.Skills!=63 && target.Skill>=0 && !actor.Kit.Has((DodgeSkill)target.Skill))
                {if(!chosen || (target.Skill+slot)%6<(chosen.Skill+slot)%6)chosen=target;}
                else if(actor.Kit.Points==0 && !actor.Kit.StarterClaimed && target.Skill<0 && (int)target.Item==slot%5+1)chosen=target;
            }
            if(!chosen)return;
            nextBotShot[slot]=Time.time+1f;
            Vector3 origin=actor.transform.position+Vector3.up*1.4f,aim=chosen.transform.position;
            actor.transform.rotation=Quaternion.LookRotation(Vector3.ProjectOnPlane(aim-origin,Vector3.up));
            DodgeBall.Spawn(origin,DodgeBall.SolveLaunchVelocity(origin,aim,18),actor,false,DodgeGame.Instance.ballMaterial);
            actor.Throw();
        }
        public static void Build(DodgeClubhouse club,Transform parent,int team)
        {
            float sign=team==0?-1:1,yaw=team==0?180:0;
            for(int i=0;i<6;i++)
            {
                Vector3 p=new Vector3((i%3-1)*3.3f,2.1f+(i/3)*1.25f,sign*26.45f);
                var plate=GameObject.CreatePrimitive(PrimitiveType.Cube);plate.name="Skill target "+team+" "+i;plate.transform.SetParent(parent,false);plate.transform.localPosition=p;plate.transform.localScale=new Vector3(3.05f,1.1f,.18f);
                plate.GetComponent<Renderer>().sharedMaterial=DodgeGame.Instance.popMaterial;DodgeWardrobe.Tint(plate.GetComponent<Renderer>(),i%2==0?DodgePartyVisuals.Mint:DodgePartyVisuals.Cream);plate.AddComponent<DodgeSurface>();
                var target=plate.AddComponent<DodgeRestTarget>();target.Team=team;target.Skill=i;
                club.WorldLabel("Skill name",parent,p+Vector3.up*.21f-Vector3.forward*sign*.14f,DodgePartyKit.SkillNames[i],.047f,DodgePartyVisuals.Dark,yaw);
                club.WorldLabel("Skill detail",parent,p-Vector3.up*.22f-Vector3.forward*sign*.14f,DodgePartyKit.SkillDetails[i],.025f,DodgePartyVisuals.Dark,yaw);
            }
            for(int i=1;i<=5;i++)
            {
                Vector3 p=new Vector3((i-3)*1.95f,.85f,sign*24.8f);
                var plate=GameObject.CreatePrimitive(PrimitiveType.Cube);plate.name="Starter item target "+team+" "+i;plate.transform.SetParent(parent,false);plate.transform.localPosition=p;plate.transform.localScale=new Vector3(1.75f,1.1f,.18f);
                plate.GetComponent<Renderer>().sharedMaterial=DodgeGame.Instance.popMaterial;DodgeWardrobe.Tint(plate.GetComponent<Renderer>(),DodgePartyVisuals.Cream);plate.AddComponent<DodgeSurface>();
                var target=plate.AddComponent<DodgeRestTarget>();target.Team=team;target.Item=(DodgeItem)i;
                club.WorldLabel("Item name",parent,p-Vector3.forward*sign*.14f,DodgePartyKit.ItemNames[i],.05f,DodgePartyVisuals.Dark,yaw);
            }
            club.WorldLabel("Rest upgrade instructions",parent,new Vector3(0,4.1f,sign*26.35f),"砸牌升级 · 命中 / 闪避赚点 · 每局领一件道具",.03f,DodgePartyVisuals.Dark,yaw);
        }
    }
}
