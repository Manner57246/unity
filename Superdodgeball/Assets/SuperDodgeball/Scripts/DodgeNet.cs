using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

namespace SuperDodgeball
{
    [DefaultExecutionOrder(-250)]
    public sealed class DodgeNet : MonoBehaviour
    {
        const int Protocol = 20261125;
        enum Packet : byte { Hello = 1, Lobby, Snapshot, Input, Ready, Ping, Reject }
        [Serializable] sealed class LobbyPacket
        {
            public DodgeRoom room;
            public DodgePage page;
            public int slot;
            public float left, countdown;
        }
        public bool IsClient { get; private set; }
        public bool Online { get; private set; }
        public int Port { get; private set; }
        public int ConnectedHumans => peers.FindAll(p => p.Slot > 0 && !p.Closed).Count + (Online && !IsClient ? 1 : 0);
        public int SnapshotsReceived { get; private set; }
        public string LocalAddress
        {
            get
            {
                if (cachedAddress != null) return cachedAddress;
                try
                {
                    foreach (var ip in Dns.GetHostAddresses(Dns.GetHostName()))
                        if (ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip)) return cachedAddress = ip.ToString();
                }
                catch (SocketException) { }
                return cachedAddress = "127.0.0.1";
            }
        }
        DodgeApp App => DodgeApp.Instance;
        DodgeGame Game => DodgeGame.Instance;
        DodgeWire wire;
        DodgeWire.Peer server;
        readonly List<DodgeWire.Peer> peers = new List<DodgeWire.Peer>();
        readonly Dictionary<int, DodgeNetView> balls = new Dictionary<int, DodgeNetView>();
        readonly Dictionary<int, DodgeNetView> drops = new Dictionary<int, DodgeNetView>();
        readonly HashSet<int> seen = new HashSet<int>();
        readonly List<int> removed = new List<int>();
        float nextLobby, nextSnapshot, nextInput, nextPing;
        DodgeInput bufferedInput;
        int nextId;
        DodgeCrowd crowd;
        string cachedAddress;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public Func<DodgeInput> AutomationInput;
#endif

        public void Host(int port)
        {
            Close(); wire = new DodgeWire(); wire.Listen(port); Port = port; Online = true; IsClient = false;
        }
        public void Join(string host, int port)
        {
            Close(); wire = new DodgeWire(); Port = port; Online = IsClient = true; wire.Connect(host, port);
        }
        public void Close()
        {
            wire?.Dispose(); wire = null; server = null; peers.Clear(); Online = IsClient = false;
            bufferedInput = default; nextInput = nextLobby = nextSnapshot = nextPing = 0f;
            ClearViews(balls); ClearViews(drops); SnapshotsReceived = 0;
        }
        static void ClearViews(Dictionary<int, DodgeNetView> map)
        {
            foreach (var value in map.Values) if (value) Destroy(value.gameObject);
            map.Clear();
        }

        void Update()
        {
            if (!DodgeApp.Active || !App.Initialized || wire == null) return;
            for (int i = 0; i < 128 && wire != null && wire.TryRead(out var notice); i++)
            {
                try { Handle(notice); }
                catch (Exception error)
                {
                    Debug.LogWarning("[Dodgeball room] Rejected invalid message: " + error.GetType().Name);
                    if (IsClient) App.ReturnHome("房间数据不兼容或连接中断，请使用同一版本重新加入。");
                    else notice.peer?.Dispose();
                }
            }
            if (wire == null) return;
            if (IsClient)
            {
                if (server != null)
                {
                    var frame = DodgeInput.Read(Game.view);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    if (AutomationInput != null) frame = AutomationInput();
#endif
                    bufferedInput.pressed |= frame.pressed;
                    bufferedInput.held = frame.held; bufferedInput.move = frame.move;
                    bufferedInput.yaw = frame.yaw; bufferedInput.pitch = frame.pitch; bufferedInput.aim = frame.aim;
                    if (Time.unscaledTime >= nextInput)
                    {
                        nextInput = Time.unscaledTime + 1f / 30f;
                        bool sending = App.Walkable && !App.Paused;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                        sending |= App.Walkable && AutomationInput != null;
#endif
                        SendInput(sending ? bufferedInput : default);
                        bufferedInput.pressed = 0;
                    }
                    if (Time.unscaledTime >= nextPing) { nextPing = Time.unscaledTime + 1f; server.Send(new[] { (byte)Packet.Ping }); }
                }
            }
            else
            {
                foreach (var peer in peers.ToArray())
                {
                    if (peer.Closed) continue;
                    if (peer.Slot < 0 && Time.unscaledTime - peer.AcceptedAt > 5f) peer.Dispose();
                    if (peer.Slot > 0 && Time.unscaledTime - peer.LastInputAt > .35f) App.Controllers[peer.Slot].RemoteInput = default;
                }
                if (Time.unscaledTime >= nextLobby) { nextLobby = Time.unscaledTime + .5f; BroadcastLobby(); }
                if (Time.unscaledTime >= nextSnapshot && App.Page != DodgePage.Home && App.Page != DodgePage.Join)
                {
                    nextSnapshot = Time.unscaledTime + .05f;
                    var snapshot = Snapshot();
                    foreach (var peer in peers) if (peer.Slot > 0 && !peer.Closed) peer.Send(snapshot, true);
                }
            }
        }

        void Handle(DodgeWire.Notice notice)
        {
            if (notice.connected)
            {
                notice.peer.AcceptedAt = Time.unscaledTime;
                if (IsClient)
                {
                    server = notice.peer;
                    server.Send(Make(Packet.Hello, w => { w.Write(Protocol); w.Write(DodgeApp.CleanName(App.PlayerName)); w.Write(Mathf.Clamp(PlayerPrefs.GetInt("MVP.Outfit",0),0,2)); }));
                }
                else peers.Add(notice.peer);
                return;
            }
            if (notice.error != null)
            {
                if (IsClient) { App.ReturnHome(App.Connecting ? "连接房间失败，请确认地址、端口及双方网络。" : "与房主的连接已断开，已返回主菜单。"); return; }
                if (notice.peer != null && notice.peer.Slot > 0)
                {
                    int slot = notice.peer.Slot; notice.peer.Slot = -1;
                    App.Room.seats[slot] = new DodgeSeatInfo();
                    App.InvalidateReady(); App.ConfigureActors(); App.Message = "一位玩家离开了房间。"; BroadcastLobby();
                }
                peers.Remove(notice.peer); return;
            }
            using (var r = new BinaryReader(new MemoryStream(notice.packet), Encoding.UTF8))
            {
                var type = (Packet)r.ReadByte();
                if (!IsClient)
                {
                    var peer = notice.peer;
                    if (Time.unscaledTime - peer.WindowAt > 1f) { peer.WindowAt = Time.unscaledTime; peer.Messages = 0; }
                    if (++peer.Messages > 150) throw new InvalidDataException("Rate exceeded");
                    if (type == Packet.Hello)
                    {
                        if (peer.Slot != -1) throw new InvalidDataException("Duplicate hello");
                        if (r.ReadInt32() != Protocol) { Reject(peer, "游戏版本不一致，请使用同一份游戏文件。"); return; }
                        string name = DodgeApp.CleanName(r.ReadString());
                        int outfit=Mathf.Clamp(r.ReadInt32(),0,2);
                        int free = Array.FindIndex(App.Room.seats, 1, s => s.kind == DodgeSeatKind.Empty);
                        if (App.Page != DodgePage.Lobby || free < 1) { Reject(peer, App.Page != DodgePage.Lobby ? "比赛已经开始，请等房主返回大厅再加入。" : "房间已满，请让房主移除一个人机再加入。"); return; }
                        peer.Slot = free; peer.LastInputAt = Time.unscaledTime;
                        App.Room.seats[free] = new DodgeSeatInfo { name = name, kind = DodgeSeatKind.Human, outfit=outfit };
                        App.PlaceActor(free,App.Clubhouse.LobbySpawn(free),0f);
                        App.InvalidateReady(); App.ConfigureActors(); BroadcastLobby();
                        return;
                    }
                    if (type == Packet.Ping) return;
                    if (peer.Slot < 1) throw new InvalidDataException("Handshake required");
                    if (type == Packet.Ready)
                    {
                        bool ready = r.ReadBoolean();
                        if (App.Page == DodgePage.Lobby) { App.Room.seats[peer.Slot].ready = ready; BroadcastLobby(); }
                    }
                    else if (type == Packet.Input)
                    {
                        int round = r.ReadInt32(); var input = ReadInput(r);
                        if (!App.Walkable || round != App.Room.round) return;
                        var controller = App.Controllers[peer.Slot];
                        input.pressed |= controller.RemoteInput.pressed;
                        controller.RemoteInput = input; peer.LastInputAt = Time.unscaledTime;
                    }
                    else throw new InvalidDataException("Unexpected client packet");
                }
                else
                {
                    if (type == Packet.Reject) { App.ReturnHome(r.ReadString()); return; }
                    if (type == Packet.Lobby)
                    {
                        if (r.ReadInt32() != Protocol) throw new InvalidDataException("Protocol");
                        var lobby = JsonUtility.FromJson<LobbyPacket>(r.ReadString());
                        App.ReceiveRoom(lobby.room, lobby.page, lobby.slot, lobby.left, lobby.countdown);
                    }
                    else if (type == Packet.Snapshot) ReadSnapshot(r);
                    else if (type != Packet.Ping) throw new InvalidDataException("Unexpected server packet");
                }
            }
        }
        void Reject(DodgeWire.Peer peer, string message)
        {
            peer.Send(Make(Packet.Reject, w => w.Write(message)));
            // Leave enough time for the single sender to flush the rejection.
            peer.AcceptedAt = Time.unscaledTime - 4.6f;
        }
        public void Kick(int slot)
        {
            if (IsClient || App.Page != DodgePage.Lobby) return;
            var peer = peers.Find(p => p.Slot == slot); peer?.Dispose();
        }
        public void SetReady(bool ready)
        {
            if (IsClient && server != null) server.Send(Make(Packet.Ready, w => w.Write(ready)));
        }
        public void BroadcastLobby()
        {
            if (!Online || IsClient || !App.Initialized) return;
            foreach (var peer in peers)
            {
                if (peer.Slot < 1 || peer.Closed) continue;
                var lobby = new LobbyPacket { room = App.Room, page = App.Page, slot = peer.Slot, left = App.TimeLeft, countdown = App.Countdown };
                peer.Send(Make(Packet.Lobby, w => { w.Write(Protocol); w.Write(JsonUtility.ToJson(lobby)); }));
            }
        }
        static byte[] Make(Packet type, Action<BinaryWriter> write)
        {
            using (var stream = new MemoryStream())
            {
                using (var w = new BinaryWriter(stream, Encoding.UTF8, true)) { w.Write((byte)type); write(w); }
                return stream.ToArray();
            }
        }
        void SendInput(DodgeInput input)
        {
            server.Send(Make(Packet.Input, w => {
                w.Write(App.Room.round); w.Write(input.move.x); w.Write(input.move.y);
                w.Write(input.yaw); w.Write(input.pitch); WriteVector(w, input.aim);
                w.Write((int)input.held); w.Write((int)input.pressed);
            }));
        }
        static DodgeInput ReadInput(BinaryReader r)
        {
            var input = new DodgeInput { move = new Vector2(Finite(r.ReadSingle()), Finite(r.ReadSingle())),
                yaw = Finite(r.ReadSingle()), pitch = Finite(r.ReadSingle()), aim = ReadVector(r),
                held = (DodgeButtons)(r.ReadInt32() & 31), pressed = (DodgeButtons)(r.ReadInt32() & 2016) };
            input.move = Vector2.ClampMagnitude(input.move, 1f); input.yaw = Mathf.Repeat(input.yaw, 360f); input.pitch = Mathf.Clamp(input.pitch, -32f, 65f);
            input.aim = Vector3.ClampMagnitude(input.aim, 100f);
            return input;
        }
        public static void WriteVector(BinaryWriter w, Vector3 v) { w.Write(v.x); w.Write(v.y); w.Write(v.z); }
        public static Vector3 ReadVector(BinaryReader r) => new Vector3(Finite(r.ReadSingle()), Finite(r.ReadSingle()), Finite(r.ReadSingle()));
        public static void WriteRotation(BinaryWriter w, Quaternion q) { w.Write(q.x); w.Write(q.y); w.Write(q.z); w.Write(q.w); }
        public static Quaternion ReadRotation(BinaryReader r)
        {
            var q = new Quaternion(Finite(r.ReadSingle()), Finite(r.ReadSingle()), Finite(r.ReadSingle()), Finite(r.ReadSingle()));
            return Quaternion.Normalize(q);
        }
        static float Finite(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || Mathf.Abs(value) > 100000f) throw new InvalidDataException("Non-finite value");
            return value;
        }

        byte[] Snapshot()
        {
            return Make(Packet.Snapshot, w => {
                w.Write(App.Room.round); w.Write(App.TimeLeft); w.Write(App.Countdown);
                App.Referee.WriteState(w);
                DodgePartyEffects.Instance.Write(w);
                for (int i = 0; i < 5; i++)
                {
                    var actor = App.Actors[i]; bool active = actor.gameObject.activeSelf;
                    w.Write(active); w.Write(App.Room.seats[i].score); w.Write(App.Room.seats[i].hits);
                    if (!active) continue;
                    WriteVector(w, actor.transform.position); WriteRotation(w, actor.transform.rotation);
                    w.Write(actor.Lean); w.Write(actor.DiveAmount); w.Write(actor.LShape); w.Write(actor.OShape);
                    w.Write(actor.Windup); w.Write(actor.MotionAmount); w.Write(actor.ThrowPulse);
                    w.Write(actor.IsStunned); w.Write(actor.HitCount);
                    w.Write(actor.CrowdStatus.EggRemaining); w.Write(actor.CrowdStatus.SoapRemaining);
                    w.Write((byte)actor.Arsenal.Weapon); w.Write((byte)actor.Arsenal.Charges); w.Write(actor.Arsenal.SpecialSelected);
                    actor.Kit.Write(w);
                    var controller = App.Controllers[i]; var bot = actor.GetComponent<DodgeBot>();
                    w.Write((byte)(controller.enabled ? controller.VisualWeapon : bot ? bot.VisualWeapon : actor.Arsenal.Selected));
                    w.Write(controller.enabled ? controller.ShotsFired : bot ? bot.ShotsFired : 0);
                    w.Write((byte)actor.Puppet.Wardrobe.Style); w.Write(actor.heldBall.gameObject.activeSelf);
                    actor.Puppet.WritePose(w);
                }
                var activeBalls = new List<DodgeBall>(DodgeBall.Active);
                w.Write((ushort)activeBalls.Count);
                foreach (var ball in activeBalls)
                {
                    if (ball.NetworkId == 0) ball.NetworkId = ++nextId;
                    w.Write(ball.NetworkId); w.Write(ball.IsEgg); w.Write((byte)ball.Weapon); w.Write((sbyte)App.SeatOf(ball.owner));
                    WriteVector(w, ball.transform.position); WriteRotation(w, ball.transform.rotation); WriteVector(w, ball.velocity);
                }
                var activeDrops = new List<DodgeAirdrop>(DodgeAirdrop.Active);
                w.Write((byte)activeDrops.Count);
                foreach (var drop in activeDrops)
                {
                    if (drop.NetworkId == 0) drop.NetworkId = ++nextId;
                    w.Write(drop.NetworkId); w.Write((byte)drop.Kind); w.Write((byte)drop.Reward); w.Write((byte)drop.Item); w.Write(drop.FromCrowd); w.Write(drop.Landed);
                    WriteVector(w, drop.transform.position); WriteRotation(w, drop.transform.rotation);
                }
                if (!crowd) crowd = FindFirstObjectByType<DodgeCrowd>();
                w.Write(crowd != null); if (crowd) crowd.WritePose(w);
            });
        }

        void ReadSnapshot(BinaryReader r)
        {
            int round = r.ReadInt32();
            if (round != App.Room.round) return;
            App.ApplyClock(r.ReadSingle(), r.ReadSingle()); SnapshotsReceived++;
            App.Referee.ReadState(r);
            DodgePartyEffects.Instance.Read(r);
            for (int i = 0; i < 5; i++)
            {
                bool active = r.ReadBoolean(); App.Room.seats[i].score = r.ReadInt32(); App.Room.seats[i].hits = r.ReadInt32();
                var actor = App.Actors[i]; actor.gameObject.SetActive(active); actor.Puppet.SetVisible(active);
                if (!active) continue;
                var position = ReadVector(r); var rotation = ReadRotation(r);
                var view = actor.GetComponent<DodgeNetView>() ?? actor.gameObject.AddComponent<DodgeNetView>(); view.Target(position, rotation);
                float lean = r.ReadSingle(), dive = r.ReadSingle(), l = r.ReadSingle(), o = r.ReadSingle(), charge = r.ReadSingle(), move = r.ReadSingle(), throwing = r.ReadSingle();
                bool stunned = r.ReadBoolean(); int hitCount = r.ReadInt32();
                actor.ApplyReplica(lean, dive, l, o, charge, move, throwing);
                actor.ApplyReplicaHits(stunned, hitCount);
                actor.CrowdStatus.ApplyReplica(r.ReadSingle(), r.ReadSingle());
                actor.Arsenal.ApplyReplica((DodgeWeapon)r.ReadByte(), r.ReadByte(), r.ReadBoolean());
                actor.Kit.Read(r);
                actor.ReplicaVisualWeapon = (DodgeWeapon)r.ReadByte();
                actor.ApplyReplicaShots(r.ReadInt32());
                int style = r.ReadByte(); if (actor.Puppet.Wardrobe.Style != style) actor.Puppet.Wardrobe.SetStyle(style);
                actor.SetReplicaHeld(r.ReadBoolean());
                actor.Puppet.ReadPose(r);
                App.Controllers[i].ReplicaCharge = charge; App.Controllers[i].ReplicaMovement = move;
            }
            seen.Clear(); int count = r.ReadUInt16(); if (count > 256) throw new InvalidDataException("Too many balls");
            for (int i = 0; i < count; i++)
            {
                int id = r.ReadInt32(); bool egg = r.ReadBoolean(); var weapon = (DodgeWeapon)r.ReadByte(); int owner = r.ReadSByte();
                Vector3 position = ReadVector(r); Quaternion rotation = ReadRotation(r); Vector3 velocity = ReadVector(r);
                if (!balls.TryGetValue(id, out var view) || !view)
                {
                    var actor = owner >= 0 && owner < 5 ? App.Actors[owner] : null;
                    var ball = egg ? DodgeBall.SpawnEgg(position, velocity, null) : DodgeBall.Spawn(position, velocity, actor, false, Game.ballMaterial, weapon);
                    ball.Replica = true; view = ball.gameObject.AddComponent<DodgeNetView>(); balls[id] = view;
                }
                view.Target(position, rotation); seen.Add(id);
            }
            Prune(balls);
            seen.Clear(); count = r.ReadByte(); if (count > 8) throw new InvalidDataException("Too many gifts");
            for (int i = 0; i < count; i++)
            {
                int id = r.ReadInt32(); var kind = (DodgeGiftKind)r.ReadByte(); var weapon = (DodgeWeapon)r.ReadByte(); var item=(DodgeItem)r.ReadByte(); bool tossed = r.ReadBoolean(), landed = r.ReadBoolean();
                Vector3 position = ReadVector(r); Quaternion rotation = ReadRotation(r);
                if (!drops.TryGetValue(id, out var view) || !view)
                {
                    var drop = tossed ? DodgeAirdrop.Toss(Game, position, position, kind, weapon,item) : DodgeAirdrop.Spawn(Game, position, weapon, landed, kind,item);
                    drop.Replica = true; view = drop.gameObject.AddComponent<DodgeNetView>(); drops[id] = view;
                }
                view.GetComponent<DodgeAirdrop>().ApplyReplica(landed); view.Target(position, rotation); seen.Add(id);
            }
            Prune(drops);
            if (r.ReadBoolean()) { if (!crowd) crowd = FindFirstObjectByType<DodgeCrowd>(); crowd.ReadPose(r); }
        }
        void Prune(Dictionary<int, DodgeNetView> map)
        {
            removed.Clear(); foreach (var pair in map) if (!seen.Contains(pair.Key)) removed.Add(pair.Key);
            foreach (int id in removed) { if (map[id]) Destroy(map[id].gameObject); map.Remove(id); }
        }
        void OnDestroy() => Close();
    }

    [DefaultExecutionOrder(15)]
    public sealed class DodgeNetView : MonoBehaviour
    {
        Vector3 position; Quaternion rotation; bool received;
        public void Target(Vector3 p, Quaternion q)
        {
            position = p; rotation = q;
            if (!received || (transform.position - p).sqrMagnitude > 9f) transform.SetPositionAndRotation(p, q);
            received = true;
        }
        void Update()
        {
            if (!DodgeGame.IsReplica || !received) return;
            float blend = 1f - Mathf.Exp(-32f * Time.deltaTime);
            transform.SetPositionAndRotation(Vector3.Lerp(transform.position, position, blend), Quaternion.Slerp(transform.rotation, rotation, blend));
        }
    }
}
