using System;
using System.Net;
using System.Text;
using System.Threading;

public class ScorePageHttpServer
{
    private readonly int _port;
    private readonly Func<string> _pageProvider;
    private readonly LiveJsonStore _data;

    private HttpListener _listener;
    private Thread _thread;
    private volatile bool _running;

    public ScorePageHttpServer(int port, Func<string> pageProvider, LiveJsonStore data)
    {
        _port = port;
        _pageProvider = pageProvider;
        _data = data;
    }

    public void Start()
    {
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://localhost:{_port}/");
        _listener.Start();

        _running = true;
        _thread = new Thread(Loop) { IsBackground = true };
        _thread.Start();
    }

    public void Stop()
    {
        _running = false;
        try { _listener?.Stop(); _listener?.Close(); } catch { }
    }

    private void Loop()
    {
        while (_running)
        {
            HttpListenerContext ctx;
            try { ctx = _listener.GetContext(); }
            catch { break; }

            try { Handle(ctx); }
            catch { }
        }
    }

    private void Handle(HttpListenerContext ctx)
    {
        bool isData = ctx.Request.Url.AbsolutePath == "/data";
        string body = isData ? _data.Get() : _pageProvider();
        string contentType = isData ? "application/json" : "text/html; charset=utf-8";

        byte[] bytes = Encoding.UTF8.GetBytes(body);
        ctx.Response.ContentType = contentType;
        ctx.Response.Headers["Cache-Control"] = "no-store";
        ctx.Response.ContentLength64 = bytes.Length;
        ctx.Response.OutputStream.Write(bytes, 0, bytes.Length);
        ctx.Response.OutputStream.Close();
    }
}
