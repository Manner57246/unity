using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace SuperDodgeball
{
    // LAN MVP transport. A single writer per connection; snapshots replace queued
    // snapshots rather than building an ever-growing backlog behind a slow peer.
    // All Unity objects and game decisions stay on the main thread.
    public sealed class DodgeWire : IDisposable
    {
        public const int MaxFrame = 131072;
        public sealed class Notice
        {
            public Peer peer;
            public byte[] packet;
            public string error;
            public bool connected;
        }
        public sealed class Peer : IDisposable
        {
            readonly DodgeWire wire;
            readonly TcpClient client;
            readonly NetworkStream stream;
            readonly AutoResetEvent wake = new AutoResetEvent(false);
            readonly ConcurrentQueue<byte[]> outgoing = new ConcurrentQueue<byte[]>();
            byte[] snapshot;
            int stopped;
            public int Slot = -1;
            public float AcceptedAt, LastInputAt;
            public int Messages;
            public float WindowAt;
            public bool Closed => Volatile.Read(ref stopped) != 0;
            public Peer(DodgeWire owner, TcpClient socket)
            {
                wire = owner; client = socket;
                client.NoDelay = true;
                client.ReceiveTimeout = 10000; client.SendTimeout = 3000;
                stream = client.GetStream();
            }
            public void Start()
            {
                new Thread(ReadLoop) { IsBackground = true, Name = "Dodgeball receive" }.Start();
                new Thread(WriteLoop) { IsBackground = true, Name = "Dodgeball send" }.Start();
            }
            public void Send(byte[] packet, bool latest = false)
            {
                if (Closed) return;
                if (latest) Interlocked.Exchange(ref snapshot, packet);
                else { if (outgoing.Count >= 64) { Dispose(); return; } outgoing.Enqueue(packet); }
                wake.Set();
            }
            void ReadExact(byte[] buffer)
            {
                int offset = 0;
                while (offset < buffer.Length)
                {
                    int count = stream.Read(buffer, offset, buffer.Length - offset);
                    if (count == 0) throw new EndOfStreamException();
                    offset += count;
                }
            }
            void ReadLoop()
            {
                try
                {
                    var header = new byte[4];
                    while (!Closed)
                    {
                        ReadExact(header);
                        int length = header[0] | header[1] << 8 | header[2] << 16 | header[3] << 24;
                        if (length < 1 || length > MaxFrame) throw new InvalidDataException("Invalid frame");
                        var packet = new byte[length]; ReadExact(packet);
                        wire.Enqueue(new Notice { peer = this, packet = packet });
                    }
                }
                catch (Exception) { }
                finally { Dispose(); }
            }
            void WriteLoop()
            {
                try
                {
                    while (!Closed)
                    {
                        while (outgoing.TryDequeue(out var command)) Write(command);
                        var state = Interlocked.Exchange(ref snapshot, null);
                        if (state != null) Write(state);
                        wake.WaitOne(50);
                    }
                }
                catch (Exception) { }
                finally { Dispose(); }
            }
            void Write(byte[] packet)
            {
                int n = packet.Length;
                stream.Write(new[] { (byte)n, (byte)(n >> 8), (byte)(n >> 16), (byte)(n >> 24) }, 0, 4);
                stream.Write(packet, 0, n);
            }
            public void Dispose()
            {
                if (Interlocked.Exchange(ref stopped, 1) != 0) return;
                client.Close(); wake.Set();
                wire.Enqueue(new Notice { peer = this, error = "连接已断开" });
            }
        }

        readonly ConcurrentQueue<Notice> incoming = new ConcurrentQueue<Notice>();
        readonly List<Peer> peers = new List<Peer>();
        TcpListener listener;
        TcpClient connecting;
        volatile bool stopped;
        public bool TryRead(out Notice notice) => incoming.TryDequeue(out notice);
        void Enqueue(Notice notice)
        {
            if (stopped) return;
            if (incoming.Count >= 256) { notice.peer?.Dispose(); return; }
            incoming.Enqueue(notice);
        }
        void Register(TcpClient socket)
        {
            lock (peers)
            {
                if (stopped || peers.FindAll(p => !p.Closed).Count >= 8) { socket.Close(); return; }
                var peer = new Peer(this, socket); peers.Add(peer);
                Enqueue(new Notice { peer = peer, connected = true }); peer.Start();
            }
        }
        public void Listen(int port)
        {
            listener = new TcpListener(IPAddress.Any, port);
            listener.Start(8);
            new Thread(() => {
                try { while (!stopped) Register(listener.AcceptTcpClient()); }
                catch (Exception) { }
            }) { IsBackground = true, Name = "Dodgeball room" }.Start();
        }
        public void Connect(string host, int port)
        {
            new Thread(() => {
                try
                {
                    var socket = new TcpClient(); connecting = socket;
                    var pending = socket.ConnectAsync(host, port);
                    if (!pending.Wait(5000)) { socket.Close(); throw new TimeoutException(); }
                    pending.GetAwaiter().GetResult();
                    if (stopped) socket.Close(); else Register(socket);
                }
                catch (Exception) { Enqueue(new Notice { error = "无法连接房间。请检查地址、端口，确认房主已开房且双方网络可互通。" }); }
            }) { IsBackground = true, Name = "Dodgeball join" }.Start();
        }
        public void Dispose()
        {
            stopped = true; listener?.Stop(); connecting?.Close();
            lock (peers) foreach (var peer in peers) peer.Dispose();
        }
    }
}
