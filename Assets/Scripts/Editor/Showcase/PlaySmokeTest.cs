using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class PlaySmokeTest
{
    private const string PENDING_MARKER = "Library/PlaySmokeTestPending";
    private const string RUN_SECONDS_ARGUMENT = "-smokeSeconds";
    private const string SHOT_ARGUMENT = "-smokeShot";
    private const string FORCED_SET_ARGUMENT = "-smokeSet";
    private const string FORCED_LOOK_ARGUMENT = "-smokeLook";
    private const char NAME_SEPARATOR = ',';
    private static readonly Vector2Int ShotSize = new Vector2Int(960, 540);
    private const string ENTRY_METHOD = "PlaySmokeTest.RunBatch";
    private const float DEFAULT_RUN_SECONDS = 12f;
    private const float COIN_INSIDE_MARGIN = 0.15f;
    private const float SLIDE_COIN_CLEARANCE = 0.6f;
    private const float CAPSULE_REACH = 0.9f;
    private const float CAPSULE_WIDTH = 1f;
    private const float ROOF_GAP_TOLERANCE = 0.35f;
    private const float OTHER_OBSTACLE_MARGIN = 1f;
    private const float RAMP_LANE_TOLERANCE = 1.5f;
    private const float RAMP_SEARCH_DISTANCE = 12f;
    private const float RECYCLE_AT_FRACTION = 0.5f;
    private const float RECYCLE_MARGIN = 10f;
    private const float PLAYER_CLEARANCE = 1.5f;
    private const float FALL_THRESHOLD = 0.6f;
    private const double SLOW_FRAME_SECONDS = 0.033;
    private const double VERY_SLOW_FRAME_SECONDS = 0.1;
    private const int OBSTACLE_LAYER = 6;
    private const int BAD_OBSTACLE_LAYER = 8;

    private static readonly List<string> Problems = new List<string>();
    private static double _startedAt;
    private static float _runSeconds;
    private static bool _armed;
    private static bool _shotCaptured;
    private static bool _recycleRequested;
    private static TrackInitiator _recycledSegment;
    private static int _recycledPieces;
    private static Transform _player;
    private static float _playerGroundY;
    private static float _fellAt = -1f;
    private static int _lastFrameCount = -1;
    private static double _lastFrameTime;
    private static int _frames;
    private static int _slowFrames;
    private static int _verySlowFrames;
    private static double _worstFrameSeconds;
    private static float _worstFrameAt;

    static PlaySmokeTest()
    {
        if (!IsBatchSmokeRun() || !File.Exists(PENDING_MARKER)) return;

        EditorApplication.update += ArmWhenPlaying;
    }

    public static void RunBatch()
    {
        try
        {
            EditorSceneManager.OpenScene(GameSceneSetupTool.GAME_SCENE_PATH, OpenSceneMode.Single);
            ConfigureDebugger();
            File.WriteAllText(PENDING_MARKER, DateTime.UtcNow.ToString("O"));
            EditorApplication.update += ArmWhenPlaying;
            EditorApplication.isPlaying = true;
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }

    private static void ConfigureDebugger()
    {
        ObsticleSetSO[] sets = LoadForced<ObsticleSetSO>(FORCED_SET_ARGUMENT, GenerationManager.OBSTICLE_SETS_FOLDER);
        TrackApperenceSO[] looks = LoadForced<TrackApperenceSO>(FORCED_LOOK_ARGUMENT, GenerationManager.TRACK_APPERENCE_FOLDER);
        if (sets.Length == 0 && looks.Length == 0) return;

        ObstacleSetDebugger debugger = Object.FindFirstObjectByType<ObstacleSetDebugger>(FindObjectsInactive.Include);
        if (debugger == null) debugger = new GameObject(nameof(ObstacleSetDebugger)).AddComponent<ObstacleSetDebugger>();

        debugger.Configure(sets, looks);
        debugger.enabled = true;
        Debug.Log($"{nameof(PlaySmokeTest)}: forcing layouts [{Join(sets)}] and appearances [{Join(looks)}]");
    }

    private static T[] LoadForced<T>(string argument, string folder) where T : Object
    {
        string value = ArgumentValue(argument);
        if (string.IsNullOrEmpty(value)) return Array.Empty<T>();

        var loaded = new List<T>();
        foreach (string name in value.Split(NAME_SEPARATOR))
        {
            string trimmed = name.Trim();
            T asset = Resources.Load<T>($"{folder}/{trimmed}");
            if (asset == null) asset = FindAssetByName<T>(trimmed);
            if (asset == null) throw new InvalidOperationException($"{argument}: no {typeof(T).Name} named '{trimmed}' in the project");

            loaded.Add(asset);
        }

        return loaded.ToArray();
    }

    private static T FindAssetByName<T>(string name) where T : Object
    {
        foreach (string guid in AssetDatabase.FindAssets($"t:{typeof(T).Name} {name}"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(path) != name) continue;

            return AssetDatabase.LoadAssetAtPath<T>(path);
        }

        return null;
    }

    private static string Join<T>(T[] assets) where T : Object
    {
        var names = new List<string>();
        foreach (T asset in assets) names.Add(asset.name);

        return names.Count > 0 ? string.Join(", ", names) : "random";
    }

    private static bool IsBatchSmokeRun()
    {
        if (!Application.isBatchMode) return false;

        foreach (string argument in Environment.GetCommandLineArgs())
        {
            if (argument == ENTRY_METHOD) return true;
        }

        return false;
    }

    private static void ArmWhenPlaying()
    {
        if (!EditorApplication.isPlaying) return;

        EditorApplication.update -= ArmWhenPlaying;
        if (File.Exists(PENDING_MARKER)) File.Delete(PENDING_MARKER);
        Arm();
    }

    private static void Arm()
    {
        if (_armed) return;

        _armed = true;
        _runSeconds = RunSecondsFromArguments();
        _startedAt = EditorApplication.timeSinceStartup;
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        _player = player != null ? player.transform : null;
        _playerGroundY = _player != null ? _player.position.y : 0f;
        Application.logMessageReceived += OnLog;
        EditorApplication.update += Tick;
        Debug.Log($"{nameof(PlaySmokeTest)}: armed for {_runSeconds:0} s of play mode");
    }

    private static void OnLog(string condition, string stackTrace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
        {
            Problems.Add($"{type}: {condition}");
        }
    }

    private static void Tick()
    {
        double elapsed = EditorApplication.timeSinceStartup - _startedAt;
        TrackPlayer((float)elapsed);
        TrackFrameTime(elapsed);

        if (elapsed >= _runSeconds * RECYCLE_AT_FRACTION)
        {
            if (!_shotCaptured)
            {
                _shotCaptured = true;
                CaptureShot();
            }

            if (!_recycleRequested) RequestRecycle();
        }

        if (elapsed < _runSeconds) return;

        EditorApplication.update -= Tick;
        Application.logMessageReceived -= OnLog;

        Report();

        bool healthy = Problems.Count == 0;
        EditorApplication.isPlaying = false;
        EditorApplication.Exit(healthy ? 0 : 2);
    }

    private static void Report()
    {
        List<Collider> solids = SolidObstacleColliders();
        int segments = Object.FindObjectsByType<TrackInitiator>(FindObjectsSortMode.None).Length;
        int walls = 0;
        int backdrop = 0;

        foreach (Transform transform in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            if (transform.name == "Walls") walls += transform.childCount;
            if (transform.name == "Backdrop") backdrop += transform.childCount;
        }

        Debug.Log($"{nameof(PlaySmokeTest)}: segments={segments} solidObstacles={solids.Count} wallPieces={walls} backdropProps={backdrop} problems={Problems.Count}");
        ReportPlayer();
        ReportFrameTimes();
        ReportObstaclePlacement();
        ReportCoins(solids);
        ReportRamps(solids);
        ReportRecycling();
        ReportFacingBouncers();
        ReportForcedContent();

        foreach (string problem in Problems) Debug.Log($"{nameof(PlaySmokeTest)}: {problem}");
        if (segments == 0 || solids.Count == 0 || walls == 0 || backdrop == 0) Problems.Add("world is missing pieces");
    }

    private static void TrackPlayer(float elapsed)
    {
        if (_player == null || _fellAt >= 0f) return;
        if (_player.position.y < _playerGroundY - FALL_THRESHOLD) _fellAt = elapsed;
    }

    private static void TrackFrameTime(double elapsed)
    {
        double now = EditorApplication.timeSinceStartup;
        if (Time.frameCount == _lastFrameCount) return;

        if (_lastFrameCount >= 0)
        {
            double seconds = now - _lastFrameTime;
            _frames++;
            if (seconds > SLOW_FRAME_SECONDS) _slowFrames++;
            if (seconds > VERY_SLOW_FRAME_SECONDS) _verySlowFrames++;
            if (seconds > _worstFrameSeconds)
            {
                _worstFrameSeconds = seconds;
                _worstFrameAt = (float)elapsed;
            }
        }

        _lastFrameCount = Time.frameCount;
        _lastFrameTime = now;
    }

    private static void ReportFrameTimes()
    {
        double elapsed = EditorApplication.timeSinceStartup - _startedAt;
        double averageMs = _frames > 0 ? elapsed / _frames * 1000.0 : 0.0;

        Debug.Log($"{nameof(PlaySmokeTest)}: frames={_frames} averageMs={averageMs:0.0} over33ms={_slowFrames} over100ms={_verySlowFrames} " +
                  $"worstMs={_worstFrameSeconds * 1000.0:0} at t={_worstFrameAt:0.0}s");
    }

    private static void ReportObstaclePlacement()
    {
        int cells = 0;
        int placed = 0;
        int misplaced = 0;
        string example = "none";

        foreach (TrackInitiator segment in Object.FindObjectsByType<TrackInitiator>(FindObjectsSortMode.None))
        {
            if (!segment.Content.IsComplete || segment.TrackRenderer == null) continue;

            cells += segment.ObstacleCells;
            placed += segment.ObstaclesPlaced;

            Bounds floor = segment.TrackRenderer.bounds;
            float cellDepth = floor.size.z / ObsticleSetSO.ROWS;

            foreach (Transform child in segment.transform)
            {
                if (child.gameObject.layer != OBSTACLE_LAYER && child.gameObject.layer != BAD_OBSTACLE_LAYER) continue;
                if (!ObstacleBoundsUtility.TryGetGameplayBounds(child.gameObject, out Bounds bounds)) continue;

                int row = Mathf.FloorToInt((bounds.center.z - floor.min.z) / cellDepth);
                int lane = NearestLane(segment.LaneCenters, bounds.center.x);
                if (CellHoldsObstacle(segment.Content.Obsticles, lane, row)) continue;

                misplaced++;
                example = $"{child.name} of {segment.name} sits at lane {lane}, row {row} (z={bounds.center.z:0.0}) where the layout has nothing";
            }
        }

        Debug.Log($"{nameof(PlaySmokeTest)}: obstacleCells={cells} placed={placed} misplaced={misplaced} example={example}");
        if (placed < cells) Problems.Add($"{cells - placed} obstacle cells were skipped (missing prefab or lane)");
        if (misplaced > 0) Problems.Add($"{misplaced} obstacles ended up in cells the layout leaves empty");
    }

    private static int NearestLane(List<Transform> lanes, float x)
    {
        int nearest = -1;
        float nearestDistance = float.MaxValue;

        for (int i = 0; lanes != null && i < lanes.Count; i++)
        {
            if (lanes[i] == null) continue;

            float distance = Mathf.Abs(lanes[i].position.x - x);
            if (distance >= nearestDistance) continue;

            nearestDistance = distance;
            nearest = i;
        }

        return nearest;
    }

    private static bool CellHoldsObstacle(ObsticleSetSO set, int lane, int row)
    {
        if (lane < 0 || !ObsticleSetSO.IsInside(lane, row)) return false;

        for (int back = 0; back < ObsticleSetSO.MAX_BLOCKADE_LENGTH; back++)
        {
            ObstacleCell cell = set.GetCell(lane, row - back);
            if (cell.Type == ObsticleType.Empty) continue;
            if (back == 0) return true;
            if (cell.Type == ObsticleType.Blockade && cell.BlockadeLength > back) return true;
        }

        return false;
    }

    private static void ReportPlayer()
    {
        float y = _player != null ? _player.position.y : float.NaN;
        float speed = SegmentMover.MoveSpeed;

        Debug.Log($"{nameof(PlaySmokeTest)}: playerY={y:0.00} groundY={_playerGroundY:0.00} fellAt={(_fellAt >= 0f ? $"{_fellAt:0.0}s" : "never")} trackSpeed={speed:0.0}");
        if (_fellAt >= 0f) Problems.Add($"player fell below the track {_fellAt:0.0} s into the run");
    }

    private static void CaptureShot()
    {
        string path = ArgumentValue(SHOT_ARGUMENT);
        if (string.IsNullOrEmpty(path)) return;

        Camera camera = Camera.main;
        if (camera == null)
        {
            Debug.Log($"{nameof(PlaySmokeTest)}: no main camera to capture");
            return;
        }

        string directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

        SceneCapture.RenderToPng(camera, ShotSize, path);
    }

    private static void ReportFacingBouncers()
    {
        var walls = new List<SideWallCollisionDetector>();

        foreach (SideWallCollisionDetector wall in Object.FindObjectsByType<SideWallCollisionDetector>(FindObjectsSortMode.None))
        {
            Transform root = wall.transform.parent;
            if (root != null && root.name.StartsWith("RampBlock", StringComparison.Ordinal)) walls.Add(wall);
        }

        int pairs = 0;
        string worst = "none";

        foreach (SideWallCollisionDetector wall in walls)
        {
            if (!wall.IsOnRight) continue;

            foreach (SideWallCollisionDetector other in walls)
            {
                if (other.IsOnRight) continue;

                Vector3 gap = other.transform.position - wall.transform.position;
                if (gap.x <= 0f || gap.x >= CAPSULE_WIDTH || Mathf.Abs(gap.z) > 1f) continue;

                pairs++;
                worst = $"ramp bouncers {gap.x:0.00} apart at x={wall.transform.position.x:0.0} z={wall.transform.position.z:0.0}";
            }
        }

        Debug.Log($"{nameof(PlaySmokeTest)}: rampBouncers={walls.Count} facingPairs={pairs} example={worst}");
        if (pairs > 0) Problems.Add($"{pairs} pairs of ramp bouncers sit closer together than the player capsule");
    }

    private static void ReportForcedContent()
    {
        ObstacleSetDebugger debugger = Object.FindFirstObjectByType<ObstacleSetDebugger>();
        if (debugger == null || !debugger.isActiveAndEnabled) return;

        int built = 0;
        int offList = 0;
        string example = "none";

        foreach (TrackInitiator segment in Object.FindObjectsByType<TrackInitiator>(FindObjectsSortMode.None))
        {
            SegmentContent content = segment.Content;
            if (!content.IsComplete) continue;

            built++;
            bool layoutOk = !debugger.ForcesObstacleSets || debugger.Contains(content.Obsticles);
            bool lookOk = !debugger.ForcesAppearances || debugger.Contains(content.Apperence);
            if (layoutOk && lookOk) continue;

            offList++;
            example = $"{segment.name} got '{content.Obsticles.name}' / '{content.Apperence.name}'";
        }

        Debug.Log($"{nameof(PlaySmokeTest)}: forcedSegments={built} offList={offList} example={example}");
        if (built == 0) Problems.Add("obstacle set debugger was active but no segment reported its content");
        if (offList > 0) Problems.Add($"{offList} segments ignored the obstacle set debugger");
    }

    private static void RequestRecycle()
    {
        TrackDestroyer destroyer = Object.FindFirstObjectByType<TrackDestroyer>();
        if (destroyer == null)
        {
            _recycleRequested = true;
            return;
        }

        TrackInitiator oldest = OldestSegmentBehindPlayer();
        if (oldest == null) return;

        _recycleRequested = true;
        _recycledSegment = oldest;
        _recycledPieces = CountPieces(oldest.transform);

        float behindZ = destroyer.transform.position.z - RECYCLE_MARGIN;
        oldest.transform.position += Vector3.forward * (behindZ - oldest.TrackRenderer.bounds.max.z);
    }

    private static TrackInitiator OldestSegmentBehindPlayer()
    {
        float playerZ = _player != null ? _player.position.z : 0f;
        bool trackStopped = SegmentMover.MoveSpeed <= 0.01f;
        TrackInitiator oldest = null;

        foreach (TrackInitiator segment in Object.FindObjectsByType<TrackInitiator>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!segment.enabled || segment.TrackRenderer == null) continue;

            float farZ = segment.TrackRenderer.bounds.max.z;
            if (!trackStopped && farZ >= playerZ - PLAYER_CLEARANCE) continue;
            if (oldest == null || farZ < oldest.TrackRenderer.bounds.max.z) oldest = segment;
        }

        return oldest;
    }

    private static int CountPieces(Transform parent)
    {
        int count = parent.childCount;

        for (int i = 0; i < parent.childCount; i++) count += CountPieces(parent.GetChild(i));

        return count;
    }

    private static void ReportRecycling()
    {
        if (_recycledSegment == null && _recycledPieces == 0)
        {
            Problems.Add("recycling check could not place a segment behind the track destroyer");
            return;
        }

        bool removed = _recycledSegment == null;

        Debug.Log($"{nameof(PlaySmokeTest)}: recycledSegmentPieces={_recycledPieces} removed={removed}");
        if (!removed) Problems.Add($"a segment behind the track destroyer still holds {CountPieces(_recycledSegment.transform)} pieces");
    }

    private static List<Collider> SolidObstacleColliders()
    {
        var solids = new List<Collider>();
        foreach (Collider collider in Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))
        {
            if (collider.isTrigger) continue;
            if (collider.gameObject.layer != 6 && collider.gameObject.layer != 8) continue;

            solids.Add(collider);
        }

        return solids;
    }

    private static void ReportCoins(List<Collider> solids)
    {
        int coins = 0;
        int inside = 0;
        string worst = "none";

        foreach (Coin coin in Object.FindObjectsByType<Coin>(FindObjectsSortMode.None))
        {
            coins++;
            Vector3 position = coin.transform.position;

            foreach (Collider solid in solids)
            {
                if (!solid.bounds.Contains(position)) continue;
                if ((solid.ClosestPoint(position) - position).sqrMagnitude > COIN_INSIDE_MARGIN * COIN_INSIDE_MARGIN) continue;

                inside++;
                worst = $"{coin.name} at {position.ToString("0.0")} inside {solid.transform.parent?.name}/{solid.name}";
                break;
            }
        }

        Debug.Log($"{nameof(PlaySmokeTest)}: coins={coins} insideObstacles={inside} example={worst}");
        if (inside > 0) Problems.Add($"{inside} coins are buried inside obstacles");

        ReportSlideCoins();
    }

    private static void ReportSlideCoins()
    {
        int checked_ = 0;
        int unreachable = 0;
        string worst = "none";

        foreach (Collider body in Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))
        {
            if (body.isTrigger || body.name != "Body") continue;
            if (body.transform.parent == null || !body.transform.parent.name.StartsWith("SlideBlock", StringComparison.Ordinal)) continue;

            Bounds bar = body.bounds;
            float rollingCeiling = bar.min.y - SLIDE_COIN_CLEARANCE;

            foreach (Coin coin in Object.FindObjectsByType<Coin>(FindObjectsSortMode.None))
            {
                Vector3 position = coin.transform.position;
                if (Mathf.Abs(position.x - bar.center.x) > 1.2f) continue;
                if (position.z < bar.min.z - 2f || position.z > bar.max.z + 2f) continue;

                checked_++;
                if (position.y <= rollingCeiling) continue;

                unreachable++;
                worst = $"coin y={position.y:0.00} needs <= {rollingCeiling:0.00} near {body.transform.parent.name}";
            }
        }

        Debug.Log($"{nameof(PlaySmokeTest)}: slideCoins={checked_} aboveRollingHeight={unreachable} example={worst}");
        if (unreachable > 0) Problems.Add($"{unreachable} coins next to a slide sit too high to grab while rolling");

        ReportJumpCoins();
    }

    private static void ReportJumpCoins()
    {
        JumpArc arc = MovmentController.Arc;
        float groundY = _playerGroundY;

        int inFootprint = 0;
        int outOfReach = 0;
        int ownedByOthers = 0;
        float worstGap = 0f;
        string worst = "none";
        var speeds = new List<string>();

        Coin[] coins = Object.FindObjectsByType<Coin>(FindObjectsSortMode.None);
        List<Collider> bodies = ObstacleBodies();

        foreach (Collider body in bodies)
        {
            if (body.transform.parent == null || !body.transform.parent.name.StartsWith("JumpBlock", StringComparison.Ordinal)) continue;

            float speed = LayoutSpeedOf(body);
            float rise = arc.RiseDistance(speed);
            float fall = arc.FallDistance(speed);
            string speedLabel = $"{speed:0.0}";
            if (!speeds.Contains(speedLabel)) speeds.Add(speedLabel);

            Bounds jump = body.bounds;
            float peakZ = jump.min.z;
            float startZ = peakZ - rise;

            foreach (Coin coin in coins)
            {
                Vector3 position = coin.transform.position;
                if (Mathf.Abs(position.x - jump.center.x) > 1.2f) continue;
                if (position.z < startZ || position.z > peakZ + fall) continue;

                if (SharedWithAnotherObstacle(position, body, bodies))
                {
                    ownedByOthers++;
                    continue;
                }

                inFootprint++;
                float playerY = groundY + arc.HeightAt(position.z - startZ, speed);
                float gap = Mathf.Abs(position.y - playerY);
                if (gap <= CAPSULE_REACH) continue;

                outOfReach++;
                if (gap <= worstGap) continue;

                worstGap = gap;
                worst = $"coin y={position.y:0.00} while jumping player is at y={playerY:0.00} at layout speed {speed:0.0} (gap {gap:0.00})";
            }
        }

        Debug.Log($"{nameof(PlaySmokeTest)}: jumpCoins={inFootprint} outOfJumpReach={outOfReach} sharedWithOtherObstacles={ownedByOthers} " +
                  $"layoutSpeeds={(speeds.Count > 0 ? string.Join("/", speeds) : "none")} worst={worst}");
        if (outOfReach > 0) Problems.Add($"{outOfReach} coins on a jump are outside the real jump trajectory");

        ReportBlockadeRoofs();
    }

    private static void ReportBlockadeRoofs()
    {
        int roofs = 0;
        float worstGap = 0f;
        string worst = "none";

        foreach (Collider body in Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))
        {
            if (body.isTrigger || body.name != "Body") continue;

            Transform root = body.transform.parent;
            if (root == null || !root.name.StartsWith("BlockadeBlock", StringComparison.Ordinal)) continue;
            if (!RendererBoundsUtility.TryGetVisibleBounds(root.gameObject, out Bounds visual)) continue;

            roofs++;
            float gap = body.bounds.max.y - visual.max.y;
            if (Mathf.Abs(gap) <= Mathf.Abs(worstGap)) continue;

            worstGap = gap;
            worst = $"{root.name} walkable roof at {body.bounds.max.y:0.00} but the model tops out at {visual.max.y:0.00}";
        }

        Debug.Log($"{nameof(PlaySmokeTest)}: blockades={roofs} worstRoofGap={worstGap:0.00} ({worst})");
        if (Mathf.Abs(worstGap) > ROOF_GAP_TOLERANCE) Problems.Add($"blockade roof is {worstGap:0.00} off the model top");
    }

    private static List<Collider> ObstacleBodies()
    {
        var bodies = new List<Collider>();
        foreach (Collider collider in Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))
        {
            if (collider.isTrigger || collider.transform.parent == null) continue;
            if (collider.name != "Body" && collider.name != "Slope") continue;

            bodies.Add(collider);
        }

        return bodies;
    }

    private static bool SharedWithAnotherObstacle(Vector3 position, Collider owner, List<Collider> bodies)
    {
        foreach (Collider other in bodies)
        {
            if (other == owner) continue;

            Bounds bounds = other.bounds;
            if (Mathf.Abs(bounds.center.x - position.x) > 1.2f) continue;
            if (position.z < bounds.min.z - OTHER_OBSTACLE_MARGIN || position.z > bounds.max.z + OTHER_OBSTACLE_MARGIN) continue;

            return true;
        }

        return false;
    }

    private static float LayoutSpeedOf(Collider obstacle)
    {
        TrackInitiator segment = obstacle.GetComponentInParent<TrackInitiator>();
        if (segment != null && segment.LayoutSpeed > 0.01f) return segment.LayoutSpeed;
        if (SegmentMover.MoveSpeed > 0.01f) return SegmentMover.MoveSpeed;

        return GameManager.instance != null ? GameManager.instance.StartGameSpeed : 15f;
    }

    private static void ReportRamps(List<Collider> solids)
    {
        int ramps = 0;
        float worstGap = 0f;
        string worst = "none";

        foreach (Collider solid in solids)
        {
            if (solid.name != "Slope") continue;

            ramps++;
            Bounds ramp = solid.bounds;
            float gap = NearestFollowingGap(ramp, solids);
            if (Mathf.Abs(gap) <= Mathf.Abs(worstGap)) continue;

            worstGap = gap;
            worst = $"{solid.transform.parent?.name} topY={ramp.max.y:0.00} endZ={ramp.max.z:0.0} gap={gap:0.00}";
        }

        Debug.Log($"{nameof(PlaySmokeTest)}: ramps={ramps} worstGapToNextObstacle={worstGap:0.00} ({worst})");
        if (Mathf.Abs(worstGap) > 1.5f) Problems.Add($"ramp is {worstGap:0.0} away from the obstacle it should lead onto");
    }

    private static float NearestFollowingGap(Bounds ramp, List<Collider> solids)
    {
        float best = float.MaxValue;
        bool found = false;

        foreach (Collider solid in solids)
        {
            if (solid.name != "Body") continue;

            Bounds other = solid.bounds;
            if (Mathf.Abs(other.center.x - ramp.center.x) > RAMP_LANE_TOLERANCE) continue;

            float gap = other.min.z - ramp.max.z;
            if (gap < -1f || gap > RAMP_SEARCH_DISTANCE) continue;

            if (!found || Mathf.Abs(gap) < Mathf.Abs(best))
            {
                best = gap;
                found = true;
            }
        }

        return found ? best : 0f;
    }

    private static float RunSecondsFromArguments()
    {
        return float.TryParse(ArgumentValue(RUN_SECONDS_ARGUMENT), out float seconds) ? seconds : DEFAULT_RUN_SECONDS;
    }

    private static string ArgumentValue(string name)
    {
        string[] arguments = Environment.GetCommandLineArgs();

        for (int i = 0; i < arguments.Length - 1; i++)
        {
            if (arguments[i] == name) return arguments[i + 1];
        }

        return null;
    }
}
