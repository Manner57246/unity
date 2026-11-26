using System.IO;
using UnityEngine;

namespace SuperDodgeball
{
    public sealed partial class DodgePartyKit
    {
        internal const float BiteSeconds = .9f, BiteContact = .39f, BulkSeconds = 15f;
        public float BiteLeft { get; private set; }
        public float BulkLeft { get; private set; }
        public float SprintLeft { get; private set; }
        public float PantLeft { get; private set; }
        float injectionPose, muscleBlend;
        public float MuscleBlend => muscleBlend;
        public float ResponseTime => BulkLeft > 0 ? 1.85f : 1f;
        public float ThrowStrength => BulkLeft > 0 ? 1.3f : 1f;
        public float RunMultiplier => SprintLeft > 0 ? 2f : 1f;
        public int Vouchers => StarterClaimed ? 0 : 1;
        public static bool Regulated(DodgeItem item) => item == DodgeItem.ThunderPill || item == DodgeItem.SprintShot;
        internal DodgeItem DisplayItem => BiteLeft > 0 ? DodgeItem.ThunderPill : injectionPose > 0 ? DodgeItem.SprintShot : Item;

        bool UseLockerItem(DodgeItem item)
        {
            if (BiteLeft > 0 || PantLeft > 0 || (item == DodgeItem.ThunderPill ? BulkLeft > 0 : SprintLeft > 0)) return false;
            Item = DodgeItem.None;
            if (DodgeApp.Instance.Referee.JudgeItem(actor,item)) return false;
            if (item == DodgeItem.ThunderPill) BiteLeft = BiteSeconds;
            else { SprintLeft = 10f; injectionPose = .52f; }
            DodgeGame.Instance.PlayTone(item == DodgeItem.ThunderPill ? 260 : 680,.09f,.08f);
            return true;
        }
        void TickLockerItems(float dt)
        {
            BulkLeft = Mathf.Max(0,BulkLeft-dt);
            PantLeft = Mathf.Max(0,PantLeft-dt);
            injectionPose = Mathf.Max(0,injectionPose-dt);
            if (SprintLeft > 0)
            {
                float remaining=SprintLeft-dt;
                SprintLeft=Mathf.Max(0,remaining);
                if(remaining<=0) PantLeft=Mathf.Max(0,2f+remaining);
            }
            if(BiteLeft<=0)return;
            float before=BiteLeft;
            BiteLeft=Mathf.Max(0,BiteLeft-dt);
            if(before>BiteContact && (actor.IsStunned || DodgeReferee.Restricted(actor) ||
                DodgeApp.Instance.Referee.JudgeItem(actor,DodgeItem.ThunderPill))) { BiteLeft=0;return; }
            if(before>BiteContact && BiteLeft<=BiteContact)
            {
                BulkLeft=BulkSeconds;
                DodgeGame.Instance.EmitFoam(actor.Puppet.BodyCenter+Vector3.up*.42f,Vector3.up,7,.7f);
                DodgeGame.Instance.PlayTone(170,.12f,.09f);
            }
        }
        AudioSource breath;
        static AudioClip huff;
        int breathBeat=-1;
        void DrawBreath()
        {
            if(PantLeft<=0){breathBeat=-1;return;}
            int beat=Mathf.FloorToInt((2-PantLeft)*3.25f);
            if(breathBeat==beat)return;
            breathBeat=beat;
            if(!breath)
            {
                breath=gameObject.AddComponent<AudioSource>();breath.playOnAwake=false;
                breath.spatialBlend=.85f;breath.minDistance=3f;breath.maxDistance=20f;
            }
            if(!huff)huff=Resources.Load<AudioClip>("Court/LockerHuff");
            breath.pitch=beat%2==0?.93f:1.08f;
            if(huff)breath.PlayOneShot(huff,.75f);
            var mouth=actor.Puppet.HeadSocket.position-Vector3.up*.32f+transform.forward*.33f;
            DodgeGame.Instance.EmitFoam(mouth,transform.forward*.65f+Vector3.up*.20f,2,.52f);
        }
        void ResetLockerItems(){breathBeat=-1;BiteLeft=BulkLeft=SprintLeft=PantLeft=injectionPose=muscleBlend=0;}
        void WriteLockerItems(BinaryWriter w)
        {
            w.Write(StarterClaimed);w.Write(BiteLeft);w.Write(BulkLeft);w.Write(SprintLeft);w.Write(PantLeft);w.Write(injectionPose);
        }
        void ReadLockerItems(BinaryReader r)
        {
            StarterClaimed=r.ReadBoolean();BiteLeft=r.ReadSingle();BulkLeft=r.ReadSingle();SprintLeft=r.ReadSingle();PantLeft=r.ReadSingle();injectionPose=r.ReadSingle();
        }
        internal void PoseConsumable(DodgeActor body)
        {
            if(BiteLeft>0)
            {
                float reach=Mathf.Clamp01((BiteSeconds-BiteLeft)/.38f)*Mathf.Clamp01(BiteLeft/.28f);
                body.leftArm.localRotation=Quaternion.Slerp(body.leftArm.localRotation,Quaternion.Euler(-75,12,-20),reach);
                body.leftForearm.localRotation=Quaternion.Slerp(body.leftForearm.localRotation,Quaternion.Euler(-73,12,0),reach);
                body.head.localRotation*=Quaternion.Euler((BiteLeft>BiteContact?-11:16)*reach,0,0);
            }
            if(PantLeft<=0)return;
            float huff=Mathf.Sin((2-PantLeft)*18);
            body.upperBody.localPosition+=Vector3.up*(.035f*huff);
            body.upperBody.localRotation*=Quaternion.Euler(22+5*huff,0,0);
            body.head.localRotation*=Quaternion.Euler(12-7*huff,0,0);
            body.leftArm.localRotation=Quaternion.Euler(-23,0,-18);
            body.rightArm.localRotation=Quaternion.Euler(-23,0,18);
        }
        void DrawPocket(bool active)
        {
            var kind=DisplayItem;
            if(kind!=DodgeItem.None && (!pocket || pocketKind!=kind))
            {
                if(pocket) Destroy(pocket.gameObject);
                pocket=DodgePartyVisuals.Item(transform,kind);pocketKind=kind;
            }
            if(!pocket)return;
            pocket.gameObject.SetActive(active && kind!=DodgeItem.None);
            if(kind==DodgeItem.None)return;
            pocket.localPosition=new Vector3(-.48f,.8f,-.12f);
            pocket.localRotation=Quaternion.identity;
            pocket.localScale=Vector3.one*(Regulated(kind)?1f:.45f);
            if(kind==DodgeItem.ThunderPill)
            {
                float raised=BiteLeft<=0?0:Mathf.SmoothStep(0,1,(BiteSeconds-BiteLeft)/.38f)*Mathf.Clamp01(BiteLeft/.24f);
                Vector3 start=transform.TransformPoint(new Vector3(-.59f,1.02f,.13f));
                Vector3 mouth=actor.Puppet.HeadSocket.position+transform.forward*.32f-Vector3.up*.35f-transform.right*.18f;
                pocket.position=Vector3.Lerp(start,mouth,raised);
                pocket.localRotation=Quaternion.Euler(0,-18,Mathf.Lerp(-30,12,raised));
                bool bitten=BiteLeft>0 && BiteLeft<=BiteContact;
                pocket.GetChild(0).gameObject.SetActive(!bitten);pocket.GetChild(1).gameObject.SetActive(bitten);
            }
            else if(kind==DodgeItem.SprintShot && injectionPose>0)
            {
                float press=Mathf.Sin(Mathf.PI*injectionPose/.52f);
                pocket.localPosition+=new Vector3(.24f,.24f,.19f)*press;
                pocket.localRotation=Quaternion.Euler(0,0,48*press);
            }
        }
        DodgeItem pocketKind;
    }
}
