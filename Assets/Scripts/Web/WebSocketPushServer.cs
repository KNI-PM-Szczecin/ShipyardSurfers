using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

public class WebSocketPushServer
{
    private const int BroadcastIntervalMs = 100;

    private readonly int _port;
    private readonly LiveJsonStore _data;

    private TcpListener _listener;
    private Thread _acceptThread;
    private Thread _broadcastThread;
    private volatile bool _running;

    private readonly object _clientsLock = new object();
    private readonly List<TcpClient> _clients = new List<TcpClient>();

    public WebSocketPushServer(int port, LiveJsonStore data)
    {
        _port = port;
        _data = data;
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
        lock (_clientsLock)
        {
            foreach (var c in _clients) { try { c.Close(); } catch { } }
            _clients.Clear();
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
        try
        {
            NetworkStream stream = client.GetStream();
            if (!WebSocketProtocol.TryHandshake(stream))
            {
                client.Close();
                return;
            }

            byte[] frame = WebSocketProtocol.EncodeTextFrame(_data.Get());
            stream.Write(frame, 0, frame.Length);

            lock (_clientsLock) { _clients.Add(client); }

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
            lock (_clientsLock) { _clients.Remove(client); }
            try { client.Close(); } catch { }
        }
    }

    private void BroadcastLoop()
    {
        int sentVersion = 0;
        while (_running)
        {
            if (_data.TryGetNewer(ref sentVersion, out string json))
            {
                byte[] frame = WebSocketProtocol.EncodeTextFrame(json);
                lock (_clientsLock)
                {
                    for (int i = _clients.Count - 1; i >= 0; i--)
                    {
                        try { _clients[i].GetStream().Write(frame, 0, frame.Length); }
                        catch
                        {
                            try { _clients[i].Close(); } catch { }
                            _clients.RemoveAt(i);
                        }
                    }
                }
            }

            Thread.Sleep(BroadcastIntervalMs);
        }
    }
}
