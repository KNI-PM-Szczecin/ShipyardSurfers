using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

public class WebSocketPushServer
{
    private const int BROADCAST_INTERVAL_MS = 33;
    private const string ROOT_PATH = "/";

    private class Channel
    {
        public LiveJsonStore Json;
        public LiveBytesStore Bytes;
        public int JsonVersion;
        public int BytesVersion;
        public readonly List<TcpClient> Clients = new List<TcpClient>();
    }

    private readonly int _port;
    private readonly Dictionary<string, Channel> _channels = new Dictionary<string, Channel>();

    private TcpListener _listener;
    private Thread _acceptThread;
    private Thread _broadcastThread;
    private volatile bool _running;

    public WebSocketPushServer(int port)
    {
        _port = port;
    }

    public void RegisterChannel(string path, LiveJsonStore json, LiveBytesStore bytes = null)
    {
        if (_running) throw new InvalidOperationException("Channels must be registered before the server starts.");

        _channels[WebSocketProtocol.NormalizePath(path)] = new Channel { Json = json, Bytes = bytes };
    }

    public int ClientCount(string path)
    {
        if (!_channels.TryGetValue(WebSocketProtocol.NormalizePath(path), out Channel channel)) return 0;
        lock (channel.Clients) return channel.Clients.Count;
    }

    public void Start()
    {
        _listener = new TcpListener(IPAddress.Loopback, _port);
        _listener.Start();

        _running = true;
        _acceptThread = new Thread(AcceptLoop) { IsBackground = true };
        _acceptThread.Start();
        _broadcastThread = new Thread(BroadcastLoop) { IsBackground = true };
        _broadcastThread.Start();
    }

    public void Stop()
    {
        _running = false;
        try { _listener?.Stop(); } catch { }

        foreach (Channel channel in _channels.Values)
        {
            lock (channel.Clients)
            {
                foreach (TcpClient client in channel.Clients) { try { client.Close(); } catch { } }
                channel.Clients.Clear();
            }
        }
    }

    private void AcceptLoop()
    {
        while (_running)
        {
            TcpClient client;
            try { client = _listener.AcceptTcpClient(); }
            catch { break; }

            ThreadPool.QueueUserWorkItem(_ => HandleClient(client));
        }
    }

    private void HandleClient(TcpClient client)
    {
        Channel channel = null;
        try
        {
            NetworkStream stream = client.GetStream();
            if (!WebSocketProtocol.TryHandshake(stream, out string path) || !TryResolveChannel(path, out channel))
            {
                client.Close();
                return;
            }

            if (channel.Json != null)
            {
                byte[] snapshot = WebSocketProtocol.EncodeTextFrame(channel.Json.Get());
                stream.Write(snapshot, 0, snapshot.Length);
            }

            lock (channel.Clients) { channel.Clients.Add(client); }

            var buf = new byte[512];
            while (_running)
            {
                int n = stream.Read(buf, 0, buf.Length);
                if (n <= 0 || WebSocketProtocol.IsCloseFrame(buf[0])) break;
            }
        }
        catch { }
        finally
        {
            if (channel != null)
            {
                lock (channel.Clients) { channel.Clients.Remove(client); }
            }
            try { client.Close(); } catch { }
        }
    }

    private bool TryResolveChannel(string path, out Channel channel)
    {
        return _channels.TryGetValue(path, out channel) || _channels.TryGetValue(ROOT_PATH, out channel);
    }

    private void BroadcastLoop()
    {
        while (_running)
        {
            foreach (Channel channel in _channels.Values)
            {
                if (channel.Json != null && channel.Json.TryGetNewer(ref channel.JsonVersion, out string json))
                {
                    Broadcast(channel, WebSocketProtocol.EncodeTextFrame(json));
                }

                if (channel.Bytes != null && channel.Bytes.TryGetNewer(ref channel.BytesVersion, out byte[] bytes))
                {
                    Broadcast(channel, WebSocketProtocol.EncodeBinaryFrame(bytes));
                }
            }

            Thread.Sleep(BROADCAST_INTERVAL_MS);
        }
    }

    private static void Broadcast(Channel channel, byte[] frame)
    {
        lock (channel.Clients)
        {
            for (int i = channel.Clients.Count - 1; i >= 0; i--)
            {
                try { channel.Clients[i].GetStream().Write(frame, 0, frame.Length); }
                catch
                {
                    try { channel.Clients[i].Close(); } catch { }
                    channel.Clients.RemoveAt(i);
                }
            }
        }
    }
}
