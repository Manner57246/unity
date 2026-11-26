using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SuperDodgeball
{
    public enum DodgeTactic { BankShot, Skipper, DodgeDouble, PocketDecoy, TeaSponsor, SmallTeam }

    public static class DodgeTactics
    {
        public static readonly string[] Names = { "借墙得分", "贴地跳球", "擦肩双响", "替身保险", "茶水赞助", "迷你全队" };
        public static readonly string[] Rarity = { "稀有", "普通", "传奇", "稀有", "普通", "史诗" };
        public static readonly Color[] Ink = { new Color(.12f,.38f,.53f), new Color(.27f,.35f,.27f),
            new Color(.60f,.31f,.06f), new Color(.12f,.38f,.53f),new Color(.27f,.35f,.27f),new Color(.45f,.22f,.54f) };
        public static readonly string[] Details = {
            "普通球碰墙再命中\n这一球算 2 分",
            "普通球首次落地\n贴着地面快速弹出",
            "闪过近身来球\n下次普投多一颗斜球",
            "每回合首次空手中球\n领一个假人替身",
            "每回合首次空手得分\n领一杯裁判奶茶",
            "全队身形缩小 20%\n被球击中的范围也缩小"
        };
        public static bool Owns(int mask, DodgeTactic tactic) => (mask & (1 << (int)tactic)) != 0;
        public static string ListNames(int mask) => string.Join("、", Names.Where((_, i) => (mask & (1 << i)) != 0));

        public static int[] Deal(int owned)
        {
            var pool = new List<int>();
            for (int i = 0; i < Names.Length; i++) if ((owned & (1 << i)) == 0) pool.Add(i);
            if (pool.Count < 3) return Deal(0);
            // Draw without replacement. Common cards appear more often per slot.
            int[] weights = { 3, 5, 1, 3, 5, 2 };
            for (int i = 0; i < 3; i++)
            {
                int total = 0;
                for (int j = i; j < pool.Count; j++) total += weights[pool[j]];
                int roll = Random.Range(0, total);
                int chosen = i;
                while (roll >= weights[pool[chosen]])
                {
                    roll -= weights[pool[chosen]];
                    chosen++;
                }
                (pool[i], pool[chosen]) = (pool[chosen], pool[i]);
            }
            return pool.GetRange(0, 3).ToArray();
        }
    }

    public sealed partial class DodgeApp
    {
        public const int SeriesLength = 3;
        public bool Drafting => Page == DodgePage.Staging && Room.leg >= 1;
        public bool HasTactic(DodgeActor actor, DodgeTactic tactic)
        {
            int slot = SeatOf(actor);
            return slot >= 0 && DodgeTactics.Owns(Room.seats[slot].tactics, tactic);
        }
        public bool ContinueSeries()
        {
            if (!IsHost || Page != DodgePage.Results || Room.leg < 1 || Room.leg >= SeriesLength) return false;
            if (!Room.seats.Any(s => s.kind != DodgeSeatKind.Empty && s.team == 0) ||
                !Room.seats.Any(s => s.kind != DodgeSeatKind.Empty && s.team == 1)) return false;
            BeginLeg(true);
            return true;
        }

        public bool PickTactic(DodgeActor actor, int card)
        {
            if (!IsHost || !Drafting || card < 0 || card >= 3 || !actor) return false;
            int slot = SeatOf(actor);
            if (slot < 0) return false;
            var seat = Room.seats[slot];
            Vector3 position = actor.transform.position;
            if (seat.kind == DodgeSeatKind.Empty || seat.pick >= 0 || seat.offer == null || seat.offer.Length != 3 ||
                !DodgeCourt.InRest(position) || (position.z > 0f ? 1 : 0) != seat.team) return false;
            LockTeam(seat.team, card);
            Clubhouse.SetPhase(Page);
            Net.BroadcastLobby();
            return true;
        }
        static bool OwnRest(DodgeActor actor, int team) => DodgeCourt.InRest(actor.transform.position) && (actor.transform.position.z > 0 ? 1 : 0) == team;
        void LockTeam(int team, int card)
        {
            foreach (var seat in Room.seats.Where(s => s.team == team && s.kind != DodgeSeatKind.Empty))
            {
                seat.pick = card;
                seat.tactics = 1 << seat.offer[card];
            }
        }
        void OfferTeam(int team, int[] offer)
        {
            foreach (var seat in Room.seats.Where(s => s.team == team && s.kind != DodgeSeatKind.Empty))
            {
                seat.offer = offer;
                seat.pick = -1;
                seat.tactics = 0;
            }
        }
        public bool Reroll(DodgeActor actor)
        {
            int slot = SeatOf(actor);
            if (!IsHost || !Drafting || slot < 0) return false;
            var seat = Room.seats[slot];
            int team = seat.team;
            if (team < 0 || !OwnRest(actor,team) ||
                seat.pick >= 0 || Room.rerolled[team] || Room.funds[team] < 1) return false;
            int[] offer;
            do { offer = DodgeTactics.Deal(0); } while (offer.All(x => seat.offer.Contains(x)));
            OfferTeam(team, offer);
            Room.funds[team]--; Room.rerolled[team] = true;
            Clubhouse.SetPhase(Page); Net.BroadcastLobby(); return true;
        }
        public static int Price(DodgeWeapon weapon) => weapon == DodgeWeapon.Rocket || weapon == DodgeWeapon.Coil ? 4 : 3;
        public bool Shop(DodgeActor actor, DodgeWeapon weapon, DodgeItem item)
        {
            int slot = SeatOf(actor);
            if (!IsHost || !Drafting || slot < 0) return false;
            int team = Room.seats[slot].team;
            if (team < 0 || !OwnRest(actor,team)) return false;
            bool medicine = weapon == DodgeWeapon.Standard;
            int price = medicine ? 2 : Price(weapon);
            if (Room.funds[team] < price) return false;
            if (medicine)
            {
                if (!DodgePartyKit.Regulated(item) || actor.Kit.Item != DodgeItem.None) return false;
                actor.Kit.Grant(item);
            }
            else if (weapon < DodgeWeapon.Burst || weapon > DodgeWeapon.Coil || !DodgePartyEffects.Instance.IssueWeapon(actor, weapon)) return false;
            Room.funds[team] -= price;
            Clubhouse.SetPhase(Page); Net.BroadcastLobby(); return true;
        }
        void BeginLeg(bool carryTraining)
        {
            System.Array.Clear(leavingRoom,0,leavingRoom.Length);
            Room.round++;
            Room.leg++;
            foreach (var seat in Room.seats) { seat.score = seat.hits = 0; seat.inCourt = false; }
            for (int team = 0; team < 2; team++)
            {
                Room.funds[team] = 6 + (carryTraining ? Mathf.Min(1, Room.funds[team]) : 0);
                Room.rerolled[team] = false;
                OfferTeam(team, DodgeTactics.Deal(0));
                if (!Room.seats.Any(s => s.team == team && s.kind == DodgeSeatKind.Human)) LockTeam(team,0);
            }
            TimeLeft = Room.duration;
            Countdown = 3f;
            Page = DodgePage.Staging;
            Paused = settingsOpen = helpOpen = false;
            Message = "";
            ResetWorld(carryTraining);
            ConfigureActors();
            DodgePartyEffects.Instance.StockRooms();
            game.CaptureInput();
            Net.BroadcastLobby();
        }
        void ClearSeries()
        {
            Room.leg = Room.mintWins = Room.peachWins = 0;
            foreach (var seat in Room.seats)
            {
                seat.tactics = 0;
                seat.offer = null;
                seat.pick = -1;
            }
        }
        public string SeriesStanding => DodgeColleges.Cape+"  " + Room.mintWins + "  —  " + Room.peachWins + "  "+DodgeColleges.Ridge;
        string SeriesWinner => Room.mintWins == Room.peachWins ? "系列赛战平" :
            DodgeColleges.Name(Room.mintWins>Room.peachWins?0:1) + "夺冠";
    }
}
