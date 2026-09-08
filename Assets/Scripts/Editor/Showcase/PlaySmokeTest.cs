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
    private const string ENTRY_METHOD = "PlaySmokeTest.RunBatch";
    private const float DEFAULT_RUN_SECONDS = 12f;
    private const float COIN_INSIDE_MARGIN = 0.15f;
    private const float SLIDE_COIN_CLEARANCE = 0.6f;
    private const float CAPSULE_REACH = 0.9f;
    private const float ROOF_GAP_TOLERANCE = 0.35f;
    private const float OTHER_OBSTACLE_MARGIN = 1f;
    private const float RAMP_LANE_TOLERANCE = 1.5f;
    private const float RAMP_SEARCH_DISTANCE = 12f;

    private static readonly List<string> Problems = new List<string>();
    private static double _startedAt;
    private static float _runSeconds;
    private static bool _armed;

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
        if (EditorApplication.timeSinceStartup - _startedAt < _runSeconds) return;

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
        ReportCoins(solids);
        ReportRamps(solids);

        foreach (string problem in Problems) Debug.Log($"{nameof(PlaySmokeTest)}: {problem}");
        if (segments == 0 || solids.Count == 0 || walls == 0 || backdrop == 0) Problems.Add("world is missing pieces");
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
        float speed = LayoutSpeed();
        float rise = arc.RiseDistance(speed);
        float fall = arc.FallDistance(speed);
        float groundY = PlayerGroundY();

        int inFootprint = 0;
        int outOfReach = 0;
        int ownedByOthers = 0;
        float worstGap = 0f;
        string worst = "none";

        Coin[] coins = Object.FindObjectsByType<Coin>(FindObjectsSortMode.None);
        List<Collider> bodies = ObstacleBodies();

        foreach (Collider body in bodies)
        {
            if (body.transform.parent == null || !body.transform.parent.name.StartsWith("JumpBlock", StringComparison.Ordinal)) continue;

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
                worst = $"coin y={position.y:0.00} while jumping player is at y={playerY:0.00} (gap {gap:0.00})";
            }
        }

        Debug.Log($"{nameof(PlaySmokeTest)}: jumpCoins={inFootprint} outOfJumpReach={outOfReach} sharedWithOtherObstacles={ownedByOthers} " +
                  $"speed={speed:0.0} rise={rise:0.0} fall={fall:0.0} worst={worst}");
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

    private static float LayoutSpeed()
    {
        if (SegmentMover.MoveSpeed > 0.01f) return SegmentMover.MoveSpeed;

        return GameManager.instance != null ? GameManager.instance.StartGameSpeed : 15f;
    }

    private static float PlayerGroundY()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        return player != null ? player.transform.position.y : 0f;
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
        string[] arguments = Environment.GetCommandLineArgs();
        for (int i = 0; i < arguments.Length - 1; i++)
        {
            if (arguments[i] == RUN_SECONDS_ARGUMENT && float.TryParse(arguments[i + 1], out float seconds)) return seconds;
        }

        return DEFAULT_RUN_SECONDS;
    }
}
