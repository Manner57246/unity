using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SuperDodgeball
{
    public enum DodgePage { Home, Join, Lobby, Countdown, Playing, Results, Staging }
    public enum DodgeSeatKind { Empty, Human, Bot }
    [Serializable] public sealed class DodgeSeatInfo
    {
        public string name = "空位";
        public DodgeSeatKind kind;
        public bool ready;
        public int score, hits;
        public int team = -1;
        public int outfit;
        public bool inCourt;
        public int tactics, pick = -1;
        public int[] offer;
    }
    [Serializable] public sealed class DodgeRoom
    {
        public string name = "软糖躲避球派对";
        public int[] funds = new int[2];
        public bool[] rerolled = new bool[2];
        public DodgeSeatInfo[] seats = new DodgeSeatInfo[5];
        public float duration = 180f;
        public int difficulty = 1;
        public DodgeVenue venue = DodgeVenue.Tour;
        public bool crowd = true, drops = true;
        public int round; // Network epoch; increases even across rematches in the same room.
        public int leg, mintWins, peachWins;
        public DodgeRoom() { for (int i = 0; i < 5; i++) seats[i] = new DodgeSeatInfo(); }
    }

    [DefaultExecutionOrder(-300)]
    public sealed partial class DodgeApp : MonoBehaviour
    {
        public static DodgeApp Instance { get; private set; }
        public static bool BypassForChecks;
        public static bool Active => Instance && !BypassForChecks;
        public DodgePage Page { get; private set; } = DodgePage.Home;
        public DodgeRoom Room { get; private set; } = new DodgeRoom();
        public DodgeActor[] Actors { get; private set; }
        public DodgePlayer[] Controllers { get; private set; }
        public int LocalSlot { get; private set; }
        public bool IsClient => Net && Net.IsClient;
        public bool Online => Net && Net.Online;
        public bool IsHost => !IsClient;
        public bool Paused { get; private set; }
        public bool Walkable => Page == DodgePage.Lobby || Page == DodgePage.Staging || Page == DodgePage.Countdown || Page == DodgePage.Playing;
        public bool SimulationRunning => initialized && Walkable && (!Paused || Online);
        public bool MatchRunning => initialized && Page == DodgePage.Playing;
        public bool ShowReticle => Walkable && !Paused && !settingsOpen && !helpOpen;
        public DodgeClubhouse Clubhouse { get; private set; }
        public DodgeReferee Referee { get; private set; }
        public DodgeStadium Stadium {get;private set;}
        public DodgeVenueDirector Venue {get;private set;}
        public DodgeVenue CurrentVenue => Room.venue!=DodgeVenue.Tour?Room.venue:Room.leg==2?DodgeVenue.Flume:Room.leg==3?DodgeVenue.Gym:DodgeVenue.Warehouse;
        public float TimeLeft { get; private set; }
        public float Countdown { get; private set; }
        public DodgeNet Net { get; private set; }
        public string Message = "";
        public string PlayerName = "";
        public string JoinAddress = "127.0.0.1";
        public string PortText = "7777";
        public bool Connecting { get; private set; }
        public string WinnerText { get; private set; }
        public bool Initialized => initialized;
        DodgeGame game;
        DodgeHud hud;
        bool initialized, settingsOpen, helpOpen;
        Vector3[] homes;
        Quaternion[] rotations;
        float volume, sensitivity;
        bool fullscreen;
        int quality;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (!FindFirstObjectByType<DodgeGame>() || Instance) return;
            new GameObject("06 - MVP menus and room", typeof(DodgeApp), typeof(DodgeNet));
        }

        void Awake() { Instance = this; }
        IEnumerator Start()
        {
            game = FindFirstObjectByType<DodgeGame>(); Net = GetComponent<DodgeNet>();
            hud = game.GetComponent<DodgeHud>();
            // Let every physical character finish building its skeleton first.
            yield return null;
            Actors = new DodgeActor[5]; Controllers = new DodgePlayer[5];
            Actors[0] = game.player.GetComponent<DodgeActor>();
            for (int i = 1; i < 5; i++) Actors[i] = GameObject.Find("Target " + i).GetComponent<DodgeActor>();
            homes = new Vector3[5]; rotations = new Quaternion[5];
            for (int i = 0; i < 5; i++)
            {
                homes[i] = Actors[i].transform.position; rotations[i] = Actors[i].transform.rotation;
                Controllers[i] = Actors[i].GetComponent<DodgePlayer>() ?? Actors[i].gameObject.AddComponent<DodgePlayer>();
                Controllers[i].view = game.view;
                Controllers[i].enabled = false;
                var bot = Actors[i].GetComponent<DodgeBot>(); if (bot) bot.enabled = false;
            }
            PlayerName = PlayerPrefs.GetString("MVP.PlayerName", "软糖玩家");
            volume = PlayerPrefs.GetFloat("MVP.Volume", .7f);
            sensitivity = PlayerPrefs.GetFloat("MVP.Sensitivity", .1f);
            fullscreen = PlayerPrefs.GetInt("MVP.Fullscreen", 0) == 1;
            quality = PlayerPrefs.GetInt("MVP.Quality", Mathf.Min(2, QualitySettings.names.Length - 1));
            ApplySettings(false);
            initialized = true;
            Clubhouse = gameObject.AddComponent<DodgeClubhouse>();
            Clubhouse.Build(this, game);
            var crowd=FindFirstObjectByType<DodgeCrowd>();if(crowd)crowd.InitializeSignage(Clubhouse);
            Referee = gameObject.AddComponent<DodgeReferee>();
            Referee.Build(this, game);
            gameObject.AddComponent<DodgePartyEffects>();
            Stadium=FindFirstObjectByType<DodgeStadium>();
            if(Stadium)
            {
                Stadium.Initialize(this);
                Venue=gameObject.AddComponent<DodgeVenueDirector>();
                Venue.Initialize(this);
            }
            game.endlessPractice = true; game.practiceLauncher = false;
            ReturnHome("");
            ApplyAutomationArguments();
        }

        void Update()
        {
            if (!initialized || BypassForChecks) return;
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (settingsOpen || helpOpen) { settingsOpen = helpOpen = false; }
                else if (Walkable) SetPaused(!Paused);
                else if (Page == DodgePage.Join) CancelJoin();
            }
            if (Walkable && !DodgeGame.InputCaptured && !Paused) Paused = true;
            if (IsClient) return;
            UpdateClubhouse();
            if (Page == DodgePage.Countdown)
            {
                if (!AllOnCourt) { Page = DodgePage.Staging; Countdown = 3f; Clubhouse.SetPhase(Page); Net.BroadcastLobby(); return; }
                if (SimulationRunning) Countdown = Mathf.Max(0f, Countdown - Time.deltaTime);
                if (Countdown <= 0f) { Page = DodgePage.Playing; Clubhouse.SetPhase(Page); game.CaptureInput(); Net.BroadcastLobby(); }
            }
            if (SimulationRunning && Page == DodgePage.Playing)
            {
                TimeLeft = Mathf.Max(0f, TimeLeft - Time.deltaTime);
                if (TimeLeft <= 0f) FinishMatch();
            }
        }

        public int SeatOf(DodgeActor actor) => Actors == null ? -1 : Array.IndexOf(Actors, actor);
        public void CreditHit(DodgeActor attacker, DodgeActor victim, bool bankShot = false)
        {
            if (IsClient || Page != DodgePage.Playing || !attacker || attacker == victim) return;
            int source = SeatOf(attacker), target = SeatOf(victim);
            if (source < 0 || target < 0 || Room.seats[source].kind == DodgeSeatKind.Empty || Room.seats[target].kind == DodgeSeatKind.Empty || SameTeam(attacker, victim)) return;
            attacker.Kit.Earn();
            Room.seats[source].score += bankShot && HasTactic(attacker, DodgeTactic.BankShot) ? 2 : 1;
            Room.seats[target].hits++;
            PresentHit(Room.round, source, target);
            Net.SendHit(source, target);
            attacker.Kit.RewardSupply(DodgeTactic.TeaSponsor);
            victim.Kit.RewardSupply(DodgeTactic.PocketDecoy);
            if(Stadium)Stadium.DrawScreens();
        }

        internal void PresentHit(int round, int source, int target)
        {
            if (!MatchRunning || round != Room.round || (LocalSlot != source && LocalSlot != target)) return;
            if (target >= 0 && target < Actors.Length && hud) hud.ShowHit(target);
        }

        public bool CreateRoom(bool online)
        {
            if (!initialized) return false;
            Net.Close();
            if (online && (!int.TryParse(PortText, out int port) || port < 1024 || port > 65535)) { Message = "端口请填写 1024 到 65535 之间的数字。"; return false; }
            Room = new DodgeRoom(); LocalSlot = 0;
            Room.seats[0] = new DodgeSeatInfo { name = CleanName(PlayerName), kind = DodgeSeatKind.Human, outfit = PlayerPrefs.GetInt("MVP.Outfit",0) };
            Room.name = Room.seats[0].name + "的派对";
            if (online)
            {
                try { Net.Host(int.Parse(PortText)); }
                catch (Exception) { Net.Close(); Message = "开房失败，端口可能正在使用。请换一个端口重试。"; return false; }
            }
            else for (int i = 1; i < 5; i++) SetBot(i, true, false);
            Page = DodgePage.Lobby; Message = ""; Connecting = false; Paused = false;
            PlayerPrefs.SetString("MVP.PlayerName", CleanName(PlayerName)); PlayerPrefs.Save();
            ResetWorld(); ConfigureActors(); game.CaptureInput(); Net.BroadcastLobby();
            return true;
        }

        public void JoinRoom()
        {
            if (Connecting) return;
            if (!int.TryParse(PortText, out int port) || port < 1024 || port > 65535 || string.IsNullOrWhiteSpace(JoinAddress))
            { Message = "请填写房主地址和有效端口。"; return; }
            Net.Close(); Message = "正在连接房间…"; Connecting = true; Page = DodgePage.Join; game.UnlockInput();
            Net.Join(JoinAddress.Trim(), port);
        }

        public static string CleanName(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "软糖玩家";
            var clean = new string(value.Where(c => !char.IsControl(c) && c != '<' && c != '>').Take(16).ToArray()).Trim();
            return clean.Length == 0 ? "软糖玩家" : clean;
        }

        public void SetBot(int slot, bool add, bool publish = true)
        {
            if (!IsHost || slot < 1 || slot >= 5 || (publish && Page != DodgePage.Lobby)) return;
            if (Room.seats[slot].kind == DodgeSeatKind.Human) return;
            Room.seats[slot] = add ? new DodgeSeatInfo { name = new[] { "", "冲冲豆", "捡漏豆", "杂技豆", "投手豆" }[slot], kind = DodgeSeatKind.Bot, ready = true, team = SmallerTeam() } : new DodgeSeatInfo();
            if (publish) { if (add) PlaceActor(slot, Clubhouse.LobbySpawn(slot), 0f); ConfigureActors(); Net.BroadcastLobby(); }
        }

        public void RulesChanged()
        {
            if (!IsHost || Page != DodgePage.Lobby) return;
            Room.duration = Mathf.Clamp(Room.duration, 30f, 600f); Room.difficulty = Mathf.Clamp(Room.difficulty, 0, 2);
            Net.BroadcastLobby();
        }
        public void InvalidateReady()
        {
            // A selected side is readiness; rule changes are visible on the board.
            foreach (var seat in Room.seats) seat.ready = seat.kind != DodgeSeatKind.Empty && seat.team >= 0;
        }
        public bool CanStart => IsHost && Page == DodgePage.Lobby && Room.seats.Count(s => s.kind != DodgeSeatKind.Empty) >= 2
            && Room.seats.Where(s => s.kind != DodgeSeatKind.Empty).All(s => s.team >= 0)
            && Room.seats.Any(s => s.kind != DodgeSeatKind.Empty && s.team == 0) && Room.seats.Any(s => s.kind != DodgeSeatKind.Empty && s.team == 1);

        public bool StartMatch()
        {
            if (!CanStart) { Message = "走到左右两侧选队；双方至少一人，所有真人选队后由房主砸开始。"; return false; }
            ClearSeries();
            BeginLeg(false);
            return true;
        }

        public void FinishMatch()
        {
            if (IsClient || Page != DodgePage.Playing) return;
            Page = DodgePage.Results; Paused = false; game.UnlockInput(); game.ClearBalls();
            int margin = TeamScore(0) - TeamScore(1);
            if (margin > 0) Room.mintWins++;
            if (margin < 0) Room.peachWins++;
            UpdateWinner(); if(Stadium)Stadium.DrawScreens(); Net.BroadcastLobby();
        }
        void UpdateWinner()
        {
            int blue = TeamScore(0), peach = TeamScore(1);
            WinnerText = blue == peach ? "本回合平局" : DodgeColleges.Name(blue>peach?0:1) + "赢了！";
        }
        public void BackToLobby()
        {
            if (!IsHost) return;
            ClearSeries();
            Page = DodgePage.Lobby; Paused = false; settingsOpen = helpOpen = false;
            foreach(var seat in Room.seats) {seat.inCourt=false;seat.score=seat.hits=0;}
            InvalidateReady(); ResetWorld(); ConfigureActors(); game.CaptureInput(); Net.BroadcastLobby();
        }
        public void ReturnHome(string message)
        {
            if (!initialized) return;
            Net.Close(); Connecting = false; Paused = false; settingsOpen = helpOpen = false;
            Page = DodgePage.Home; LocalSlot = 0; Message = message; Room = new DodgeRoom();
            for (int i = 0; i < 5; i++) { Room.seats[i].kind = DodgeSeatKind.Bot; Room.seats[i].name = ""; }
            ResetWorld(); ConfigureActors(); game.UnlockInput();
        }

        public void ReceiveRoom(DodgeRoom room, DodgePage page, int localSlot, float left, float countdown)
        {
            if (room == null || (uint)room.venue>(uint)DodgeVenue.Gym || room.seats == null || room.seats.Length != 5 || localSlot < 1 || localSlot >= 5) throw new System.IO.InvalidDataException("Invalid room");
            bool fresh = room.round != Room.round || (Page != page && page == DodgePage.Lobby);
            bool entering = Page != page;
            Room = room; Page = page; LocalSlot = localSlot; TimeLeft = left; Countdown = countdown; Connecting = false; Message = "";
            if (fresh) ResetWorld(page == DodgePage.Staging && room.leg > 1);
            ConfigureActors();
            if (entering)
            {
                Paused = false; settingsOpen = helpOpen = false;
                if (Walkable) game.CaptureInput(); else game.UnlockInput();
            }
            if (page == DodgePage.Results) UpdateWinner();
        }
        public void ApplyClock(float left, float countdown) { TimeLeft = left; Countdown = countdown; }
        public void SetPaused(bool value)
        {
            if (!Walkable) return;
            Paused = value;
            if (value) game.UnlockInput(); else game.CaptureInput();
        }

        void ResetWorld(bool carryTraining = false)
        {
            if (hud) hud.ClearHits();
            DodgeRestTarget.ResetBots();
            if (Referee) Referee.ResetRound();
            foreach (var actor in Actors) { actor.gameObject.SetActive(true); actor.Puppet.SetVisible(true); actor.Puppet.SetNetworkReplica(false); }
            game.player = Controllers[LocalSlot]; game.view.player = game.player;
            if(Stadium)Stadium.DrawScreens();
            game.ResetRound(carryTraining);
            for (int i = 0; i < 5; i++)
            {
                Controllers[i].RemoteInput = default; Controllers[i].ResetPlayer(); Actors[i].ResetActor(carryTraining);
                var motor = Actors[i].GetComponent<CharacterController>(); motor.enabled = false;
                Vector3 destination = Page == DodgePage.Lobby ? Clubhouse.LobbySpawn(i) : Page == DodgePage.Staging ? Clubhouse.RestSpawn(Room.seats[i].team,i) : homes[i];
                float yaw = Page == DodgePage.Lobby ? 0f : Page == DodgePage.Staging ? (Room.seats[i].team == 1 ? 180f : 0f) : rotations[i].eulerAngles.y;
                if (Drafting && Room.seats[i].kind == DodgeSeatKind.Human) yaw += 180f;
                Actors[i].transform.SetPositionAndRotation(destination, Quaternion.Euler(0f,yaw,0f));
                if(Actors[i].Puppet.Wardrobe) Actors[i].Puppet.Wardrobe.SetStyle(Room.seats[i].outfit);
                Actors[i].Puppet.SnapToPose();
            }
            game.view.SetView(Actors[LocalSlot].transform.eulerAngles.y, Drafting ? 0f : 9f);
            // The match clock is owned by the application; this marks world objects as pausable.
            game.CaptureInput(); game.UnlockInput();
        }

        public void ConfigureActors()
        {
            bool menu = Page == DodgePage.Home || Page == DodgePage.Join;
            game.airdropsEnabled = Room.drops; game.crowdEnabled = Room.crowd; game.botsEnabled = true;
            Clubhouse.SetPhase(Page);
            for (int i = 0; i < 5; i++)
            {
                var actor = Actors[i]; var controller = Controllers[i]; var bot = actor.GetComponent<DodgeBot>();
                bool occupied = menu || Room.seats[i].kind != DodgeSeatKind.Empty;
                actor.gameObject.SetActive(occupied); actor.Puppet.SetVisible(occupied);
                actor.isPlayer = i == LocalSlot; actor.animatedTarget = false;
                actor.Puppet.SetNetworkReplica(IsClient);
                if(!menu && actor.Puppet.Wardrobe)
                {
                    actor.Puppet.Wardrobe.SetCollege(Room.seats[i].team,i);
                    actor.Puppet.Wardrobe.SetStyle(Room.seats[i].outfit);
                }
                controller.Replica = IsClient; controller.RemoteControlled = !IsClient && i != LocalSlot;
                controller.enabled = occupied && !IsClient && !menu && Room.seats[i].kind == DodgeSeatKind.Human;
                actor.GetComponent<CharacterController>().enabled = occupied && !IsClient;
                if (bot)
                {
                    bot.enabled = occupied && !IsClient && !menu && Room.seats[i].kind == DodgeSeatKind.Bot;
                    float skill = Room.difficulty;
                    bot.moveSpeed = Mathf.Lerp(3.4f, 4.8f, skill / 2f);
                    bot.reactionSeconds = Mathf.Lerp(.5f, .23f, skill / 2f);
                    bot.aimError = Mathf.Lerp(1.15f, .42f, skill / 2f);
                }
            }
            game.player = Controllers[LocalSlot]; game.view.player = game.player;
            if(Stadium)Stadium.DrawScreens();
        }

        void ApplySettings(bool save)
        {
            AudioListener.volume = volume; game.view.sensitivity = sensitivity;
            QualitySettings.SetQualityLevel(Mathf.Clamp(quality, 0, QualitySettings.names.Length - 1), false);
            if (!Application.isEditor) Screen.fullScreenMode = fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
            if (!save) return;
            PlayerPrefs.SetFloat("MVP.Volume", volume); PlayerPrefs.SetFloat("MVP.Sensitivity", sensitivity);
            PlayerPrefs.SetInt("MVP.Fullscreen", fullscreen ? 1 : 0); PlayerPrefs.SetInt("MVP.Quality", quality); PlayerPrefs.Save();
        }
        void OnApplicationFocus(bool focused)
        {
            if (!focused && initialized && Walkable) { Paused = true; game.UnlockInput(); }
        }
        void OnDestroy() { Net?.Close(); if (Instance == this) Instance = null; BypassForChecks = false; DestroyUi(); }
        void OnApplicationQuit() => Net?.Close();
    }
}
