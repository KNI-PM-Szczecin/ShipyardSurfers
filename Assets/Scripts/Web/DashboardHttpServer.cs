using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

public class DashboardHttpServer
{
    private const string HTML_CONTENT_TYPE = "text/html; charset=utf-8";
    private const string JSON_CONTENT_TYPE = "application/json";
    private const string ROOT_PATH = "/";

    private class Route
    {
        public Func<string> Body;
        public string ContentType;
    }

    private readonly int _port;
    private readonly Dictionary<string, Route> _routes = new Dictionary<string, Route>();

    private HttpListener _listener;
    private Thread _thread;
    private volatile bool _running;

    public DashboardHttpServer(int port)
    {
        _port = port;
    }

    public void AddPage(string path, string filePath)
    {
        AddRoute(path, () => LoadFile(filePath), HTML_CONTENT_TYPE);
    }

    public void AddJson(string path, LiveJsonStore store)
    {
        AddRoute(path, store.Get, JSON_CONTENT_TYPE);
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

    private void AddRoute(string path, Func<string> body, string contentType)
    {
        if (_running) throw new InvalidOperationException("Routes must be registered before the server starts.");

        _routes[WebSocketProtocol.NormalizePath(path)] = new Route { Body = body, ContentType = contentType };
    }

    private void Loop()
    {
        while (_running)
        {
            HttpListenerContext ctx;
            try { ctx = _listener.GetContext(); }
            catch { break; }

            ThreadPool.QueueUserWorkItem(_ => Handle(ctx));
        }
    }

    private void Handle(HttpListenerContext ctx)
    {
        try
        {
            string path = WebSocketProtocol.NormalizePath(ctx.Request.Url.AbsolutePath);
            if (!_routes.TryGetValue(path, out Route route)) _routes.TryGetValue(ROOT_PATH, out route);

            if (route == null)
            {
                ctx.Response.StatusCode = (int)HttpStatusCode.NotFound;
                ctx.Response.OutputStream.Close();
                return;
            }

            byte[] bytes = Encoding.UTF8.GetBytes(route.Body());
            ctx.Response.ContentType = route.ContentType;
            ctx.Response.Headers["Cache-Control"] = "no-store";
            ctx.Response.ContentLength64 = bytes.Length;
            ctx.Response.OutputStream.Write(bytes, 0, bytes.Length);
            ctx.Response.OutputStream.Close();
        }
        catch { }
    }

    private static string LoadFile(string filePath)
    {
        return File.Exists(filePath)
            ? File.ReadAllText(filePath)
            : $"<h1>{Path.GetFileName(filePath)} not found</h1>";
    }
}
