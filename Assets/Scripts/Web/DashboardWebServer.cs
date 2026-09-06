using System;
using System.IO;
using UnityEngine;

public class DashboardWebServer : MonoBehaviour
{
    private const int HTTP_PORT = 8080;
    private const int WS_PORT = 8081;
    private const float SNAPSHOT_INTERVAL = 0.2f;
    private const string SCOREBOARD_PAGE = "scoreboard.html";
    private const string CAMERA_PAGE = "camera.html";
    private const string SCOREBOARD_PATH = "/";
    private const string SCORE_DATA_PATH = "/data";

    private LiveJsonStore _scoreData;
    private DashboardHttpServer _httpServer;
    private WebSocketPushServer _wsServer;
    private float _nextSnapshot;
    private bool _started;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoStart()
    {
        var go = new GameObject(nameof(DashboardWebServer));
        go.AddComponent<DashboardWebServer>();
        DontDestroyOnLoad(go);
    }

    private void Start()
    {
        ScoreboardSeeder.SeedIfEmpty();

        _scoreData = new LiveJsonStore();

        _httpServer = new DashboardHttpServer(HTTP_PORT);
        _httpServer.AddPage(SCOREBOARD_PATH, PagePath(SCOREBOARD_PAGE));
        _httpServer.AddPage(PoseDebugFeed.CHANNEL_PATH, PagePath(CAMERA_PAGE));
        _httpServer.AddJson(SCORE_DATA_PATH, _scoreData);

        _wsServer = new WebSocketPushServer(WS_PORT);
        _wsServer.RegisterChannel(SCOREBOARD_PATH, _scoreData);
        _wsServer.RegisterChannel(PoseDebugFeed.CHANNEL_PATH, PoseDebugFeed.Pose, PoseDebugFeed.Frames);

        try
        {
            _httpServer.Start();
            _wsServer.Start();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"{nameof(DashboardWebServer)}: could not start ({e.Message})");
            StopServers();
            return;
        }

        _started = true;
        Debug.Log($"{nameof(DashboardWebServer)} running at http://localhost:{HTTP_PORT}/ and http://localhost:{HTTP_PORT}{PoseDebugFeed.CHANNEL_PATH} (ws on {WS_PORT})");
    }

    private void Update()
    {
        if (!_started) return;

        PoseDebugFeed.ViewerCount = _wsServer.ClientCount(PoseDebugFeed.CHANNEL_PATH);

        if (Time.unscaledTime < _nextSnapshot) return;
        _nextSnapshot = Time.unscaledTime + SNAPSHOT_INTERVAL;
        _scoreData.Set(ScoreSnapshot.CaptureJson());
    }

    private static string PagePath(string fileName)
    {
        return Path.Combine(Application.streamingAssetsPath, fileName);
    }

    private void OnDestroy() => StopServers();

    private void StopServers()
    {
        _httpServer?.Stop();
        _wsServer?.Stop();
        PoseDebugFeed.ViewerCount = 0;
    }
}
