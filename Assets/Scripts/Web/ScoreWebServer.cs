using System;
using System.IO;
using UnityEngine;

public class ScoreWebServer : MonoBehaviour
{
    private const int HttpPort = 8080;
    private const int WsPort = 8081;
    private const float SnapshotInterval = 0.2f;
    private const string PageFileName = "scoreboard.html";

    private LiveJsonStore _data;
    private ScorePageHttpServer _httpServer;
    private WebSocketPushServer _wsServer;
    private string _pagePath;
    private float _nextSnapshot;
    private bool _started;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoStart()
    {
        var go = new GameObject("ScoreWebServer");
        go.AddComponent<ScoreWebServer>();
        DontDestroyOnLoad(go);
    }

    private void Start()
    {
        ScoreboardSeeder.SeedIfEmpty();

        _pagePath = Path.Combine(Application.streamingAssetsPath, PageFileName);
        _data = new LiveJsonStore();
        _httpServer = new ScorePageHttpServer(HttpPort, LoadPage, _data);
        _wsServer = new WebSocketPushServer(WsPort, _data);

        try
        {
            _httpServer.Start();
            _wsServer.Start();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"ScoreWebServer: could not start ({e.Message})");
            StopServers();
            return;
        }

        _started = true;
        Debug.Log($"ScoreWebServer running at http://localhost:{HttpPort}/ (ws on {WsPort})");
    }

    private void Update()
    {
        if (!_started || Time.unscaledTime < _nextSnapshot) return;
        _nextSnapshot = Time.unscaledTime + SnapshotInterval;

        _data.Set(ScoreSnapshot.CaptureJson());
    }

    private string LoadPage()
    {
        return File.Exists(_pagePath)
            ? File.ReadAllText(_pagePath)
            : $"<h1>{PageFileName} not found</h1>";
    }

    private void OnDestroy() => StopServers();

    private void StopServers()
    {
        _httpServer?.Stop();
        _wsServer?.Stop();
    }
}
