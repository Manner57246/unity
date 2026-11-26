#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SuperDodgeball.Editor
{
    public sealed class DodgeBlastChecks : MonoBehaviour
    {
        [Serializable] sealed class Report
        {
            public bool passed;
            public string failure;
            public List<string> checks = new();
        }

        readonly Report report = new();
        DodgeApp App => DodgeApp.Instance;
        DodgeActor Source => App.Actors[0];
        GameObject cover;
        readonly Vector3 center = new(0, .05f, 4f);

        [MenuItem("Super Dodgeball/Verify college cup blast damage")]
        public static void Begin() => new GameObject("College cup play check").AddComponent<DodgeBlastChecks>().StartCoroutine("Run");

        void Update()
        {
            if (!App || !App.Initialized || !App.Walkable) return;
            App.SetPaused(false);
            App.Controllers[0].RemoteControlled = true;
            for (int i = 1; i < App.Actors.Length; i++)
            {
                App.Actors[i].GetComponent<DodgeBot>().enabled = false;
                App.Controllers[i].enabled = false;
            }
        }

        void Require(bool ok, string name)
        {
            if (!ok) throw new InvalidOperationException(name);
            report.checks.Add(name);
        }

        IEnumerator Run()
        {
            var checks = CheckMatch();
            while (true)
            {
                try { if (!checks.MoveNext()) { report.passed = true; break; } }
                catch (Exception exception) { report.failure = exception.ToString(); break; }
                yield return checks.Current;
            }
            if (cover) Destroy(cover);
            App.ReturnHome("");
            Directory.CreateDirectory("work");
            File.WriteAllText("work/campus-blast-check.json", JsonUtility.ToJson(report, true));
            Debug.Log("[College cup] " + report.passed + " " + report.failure);
            Destroy(gameObject);
        }

        IEnumerator CheckMatch()
        {
            while (!App || !App.Initialized) yield return null;
            App.ReturnHome("");
            App.BeginParty();
            App.ChooseSide(0, 0);
            App.Room.crowd = App.Room.drops = false;
            Require(App.StartMatch(), "Enter offline team match");
            for (int i = 0; i < 5; i++)
                App.PlaceActor(i, App.Clubhouse.CourtSpawn(App.Room.seats[i].team, i), 0);
            yield return new WaitForSeconds(3.5f);
            Require(App.Page == DodgePage.Playing, "Court is live");
            typeof(DodgeReferee).GetProperty("Phase").SetValue(App.Referee, DodgeRefereePhase.Slacking);
            typeof(DodgeReferee).GetProperty("Remaining").SetValue(App.Referee, 1000f);
            for (int i = 0; i < 5; i++) App.Room.seats[i].team = i == 0 || i == 4 ? 0 : 1;
            cover = new GameObject("Solid test screen", typeof(BoxCollider), typeof(DodgeSurface));
            cover.transform.position = center + new Vector3(2.2f, 1.8f, 0);
            cover.GetComponent<BoxCollider>().size = new Vector3(.24f, 4.3f, 2.9f);

            var weapons = new[] { DodgeWeapon.Thunderbolt, DodgeWeapon.Thunderbolt, DodgeWeapon.Rocket, DodgeWeapon.Bomb, DodgeWeapon.Vest };
            for (int trial = 0; trial < weapons.Length; trial++)
            {
                var weapon = weapons[trial];
                bool weak = trial == 0;
                bool storm = weapon == DodgeWeapon.Thunderbolt;
                DodgeGame.Instance.ClearBalls();
                DodgePartyEffects.Instance.ResetEffects();
                var spots = new[] { weapon == DodgeWeapon.Vest ? center : new Vector3(0,.05f,-6),
                    center + Vector3.left * 1.35f, center + Vector3.forward * (storm ? 4.85f : 2.55f),
                    center + Vector3.right * 3.12f, center + Vector3.back * 1.7f };
                for (int i = 0; i < 5; i++)
                {
                    App.Actors[i].ResetActor();
                    App.PlaceActor(i, spots[i], i == 0 ? 0 : 180);
                }
                yield return new WaitForSeconds(.18f);
                int before = App.TeamScore(0);
                if (weapon == DodgeWeapon.Vest)
                {
                    Source.Arsenal.Equip(weapon);
                    Require(Source.Kit.TryPopVest(), "Vest starts its approach fuse");
                    yield return new WaitForSeconds(1.45f);
                }
                else if (weapon == DodgeWeapon.Bomb)
                {
                    DodgePartyEffects.Instance.Spawn(DodgeEffect.Bomb, center + Vector3.up * .36f, Vector3.zero, Source, 3f);
                    yield return new WaitForSeconds(2.4f);
                    Require(App.TeamScore(0) == before && DodgePartyEffects.Instance.Explosions == 0, "Timer waits before bursting");
                    yield return new WaitForSeconds(.84f);
                }
                else
                {
                    var shot = DodgeBall.Spawn(center + Vector3.up * .8f, Vector3.down * 17f, Source, false, DodgeGame.Instance.ballMaterial, weapon);
                    shot.SetPower(weak ? 0 : 1);
                    yield return new WaitForSeconds(.26f);
                }
                string label = weapon + (weak ? " tap" : "");
                int expected = weak ? 1 : 2;
                string counts = $" ({App.Actors[1].HitCount},{App.Actors[2].HitCount},{App.Actors[3].HitCount},{App.Actors[4].HitCount})";
                Require(App.TeamScore(0) == before + expected && App.Actors[1].HitCount == 1 && App.Actors[2].HitCount == expected - 1,
                    label + " hits and scores once per exposed enemy" + counts);
                Require(App.Actors[3].HitCount == 0 && App.Actors[4].HitCount == 0 && Source.HitCount == 0,
                    label + " protects cover, ally and owner");
                if (storm) Require(App.Actors[1].Zeus.ShockLeft > 0 && (App.Actors[2].Zeus.ShockLeft > 0) == !weak,
                    label + " electrifies each reached target");
                if (weapon == DodgeWeapon.Vest) Require(Source.Kit.ReturnTime > 0, "Wearer enters existing return sequence");
            }
        }
    }
}
#endif
