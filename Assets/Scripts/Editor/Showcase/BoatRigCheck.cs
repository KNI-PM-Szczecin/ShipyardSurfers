using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class BoatRigCheck
{
    private const string PENDING_MARKER = "Library/BoatRigCheckPending";
    private const string ENTRY_METHOD = "BoatRigCheck.RunBatch";
    private const int JUMP_SAMPLES = 20;
    private const float JUMP_SPACING = 0.9f;
    private const float MIN_FAN_DEGREES = 30f;
    private const float MIN_BANK_DEGREES = 6f;
    private const float MIN_NOSE_UP_DEGREES = 6f;
    private const float SETTLE_TOLERANCE = 2f;

    private static readonly List<string> Problems = new List<string>();
    private static readonly List<(float at, Action action)> Steps = new List<(float, Action)>();
    private static double _startedAt;
    private static bool _armed;
    private static int _nextStep;
    private static Transform _root;
    private static Transform _hull;
    private static Transform _fan;
    private static Animator _animator;
    private static ParticleSystem _stern;
    private static ParticleSystem _sides;
    private static ParticleSystem _sparks;
    private static Transform _anchor;
    private static SpraySurfaceTracker _tracker;
    private static CapsuleCollider _capsule;
    private static Vector3 _capsuleCenter;
    private static float _capsuleHeight;
    private static float _capsuleRadius;
    private static float _fanSample;
    private static float _hullRestY;
    private static int _hops;
    private static int _backflips;

    static BoatRigCheck()
    {
        if (!IsBatchRun() || !File.Exists(PENDING_MARKER)) return;

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

    private static bool IsBatchRun()
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
        if (_armed) return;

        _armed = true;
        _startedAt = EditorApplication.timeSinceStartup;
        Application.logMessageReceived += OnLog;
        FreezeWorld();
        if (!FindRig()) { Finish(); return; }

        PlanSteps();
        EditorApplication.update += Tick;
        Debug.Log($"{nameof(BoatRigCheck)}: armed, {Steps.Count} steps");
    }

    private static void OnLog(string condition, string stackTrace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) Problems.Add($"{type}: {condition}");
    }

    private static void FreezeWorld()
    {
        GameManager manager = Object.FindFirstObjectByType<GameManager>();
        if (manager != null) manager.enabled = false;
        SegmentMover.MoveSpeed = 0f;
    }

    private static bool FindRig()
    {
        GameObject player = GameObject.FindWithTag("Player");
        _root = player != null ? player.transform : null;
        Transform rig = _root != null ? _root.Find(BoatRigBuilder.RIG_NAME) : null;
        _hull = rig != null ? rig.Find(BoatRigBuilder.HULL_NAME) : null;
        _animator = rig != null ? rig.GetComponent<Animator>() : null;
        _capsule = player != null ? player.GetComponent<CapsuleCollider>() : null;

        if (_hull != null)
        {
            foreach (Transform node in _hull.GetComponentsInChildren<Transform>())
            {
                if (string.Equals(node.name, BoatRigBuilder.FAN_NODE_NAME, StringComparison.OrdinalIgnoreCase)) _fan = node;
            }
        }

        if (_root == null || rig == null || _hull == null || _fan == null || _animator == null || _capsule == null)
        {
            Problems.Add("player is missing BoatRig/Hull/fan/Animator/CapsuleCollider");
            return false;
        }

        _capsuleCenter = _capsule.center;
        _capsuleHeight = _capsule.height;
        _capsuleRadius = _capsule.radius;
        _hullRestY = _hull.localPosition.y;
        BoatAnimator boat = rig.GetComponent<BoatAnimator>();
        if (boat == null) Problems.Add("BoatRig has no BoatAnimator");

        _anchor = rig.Find(BoatRigBuilder.SPRAY_ANCHOR_NAME);
        _stern = Emitter(_anchor, BoatRigBuilder.STERN_SPRAY_NAME);
        _sides = Emitter(_anchor, BoatRigBuilder.SIDES_SPRAY_NAME);
        _sparks = Emitter(_anchor, BoatRigBuilder.SPARKS_NAME);
        _tracker = rig.GetComponent<SpraySurfaceTracker>();
        if (_anchor == null || _stern == null || _sides == null || _sparks == null || _tracker == null) Problems.Add("BoatRig is missing the spray anchor, an emitter or SpraySurfaceTracker");
        if (_animator.layerCount < 3) Problems.Add("Boat animator has no Spray layer");
        return true;
    }

    private static ParticleSystem Emitter(Transform anchor, string name)
    {
        Transform child = anchor != null ? anchor.Find(name) : null;
        return child != null ? child.GetComponent<ParticleSystem>() : null;
    }

    private static void ReportSpray(string label, float minRate, float maxRate, float minSparks = 0f, float maxSparks = 1f)
    {
        if (_stern == null || _sparks == null) return;

        float rate = _stern.emission.rateOverTime.constant;
        float sparks = _sparks.emission.rateOverTime.constant;
        float speed = _stern.main.startSpeed.constantMax;
        string state = _animator.layerCount >= 3 ? StateName(_animator.GetCurrentAnimatorStateInfo(2)) : "?";
        Debug.Log($"{nameof(BoatRigCheck)}: spray {label}: state={state} surface={_animator.GetFloat(SpraySurfaceTracker.SURFACE_PARAMETER):0.0} sternRate={rate:0} sternSpeedMax={speed:0.0} " +
                  $"sidesRate={_sides.emission.rateOverTime.constant:0} sparkRate={sparks:0} anchorY={_anchor.position.y:0.00} " +
                  $"particles stern={_stern.particleCount} sides={_sides.particleCount} sparks={_sparks.particleCount}");
        if (rate < minRate || rate > maxRate) Problems.Add($"spray {label}: stern rate {rate:0} outside {minRate:0}..{maxRate:0}");
        if (sparks < minSparks || sparks > maxSparks) Problems.Add($"spray {label}: spark rate {sparks:0} outside {minSparks:0}..{maxSparks:0}");
    }

    private static void ExpectAnchorOnFloor()
    {
        if (_anchor == null || _root == null) return;

        float floorTop = _root.position.y - 1f + 0.1f;
        float offset = _anchor.position.y - floorTop;
        Debug.Log($"{nameof(BoatRigCheck)}: anchor sits {offset:0.00} above the floor top (anchorY={_anchor.position.y:0.00})");
        if (Mathf.Abs(offset) > 0.15f) Problems.Add($"spray anchor is {offset:0.00} off the floor surface");
    }

    private static string StateName(AnimatorStateInfo info)
    {
        foreach (string name in new[] { BoatRigBuilder.SPRAY_GROUND_STATE, BoatRigBuilder.SPRAY_AIR_STATE, BoatRigBuilder.SPRAY_ROLL_STATE, BoatRigBuilder.SPRAY_LANDING_STATE, BoatRigBuilder.SPRAY_DEAD_STATE })
        {
            if (info.IsName(name)) return name;
        }

        return "other";
    }

    private static void PlanSteps()
    {
        float t = 0.5f;
        Steps.Add((t, () => _fanSample = _fan.localEulerAngles.z));
        Steps.Add((t += 0.1f, () =>
        {
            float spun = Mathf.Abs(Mathf.DeltaAngle(_fanSample, _fan.localEulerAngles.z));
            Debug.Log($"{nameof(BoatRigCheck)}: fan turned {spun:0.0} degrees in 0.1 s, controller={_animator.runtimeAnimatorController.name}, layers={_animator.layerCount}");
            if (spun < MIN_FAN_DEGREES) Problems.Add($"fan barely turns ({spun:0.0} degrees in 0.1 s)");
        }));

        Steps.Add((t += 0.2f, () => EventBus.PlayerLaneChangeStarted(1)));
        Steps.Add((t += 0.15f, () => Expect("bank right", Roll(), -MIN_BANK_DEGREES, true)));
        Steps.Add((t += 0.6f, () => ExpectSettled("bank right settles", Roll())));
        Steps.Add((t += 0.1f, () => EventBus.PlayerLaneChangeStarted(-1)));
        Steps.Add((t += 0.15f, () => Expect("bank left", Roll(), MIN_BANK_DEGREES, false)));
        Steps.Add((t += 0.6f, () => ExpectSettled("bank left settles", Roll())));

        Steps.Add((t += 0.1f, () => ReportSpray("at rest", 80f, 100f)));
        Steps.Add((t += 0.05f, ExpectAnchorOnFloor));
        Steps.Add((t += 0.1f, () => EventBus.PlayerGroundedChanged(false)));
        Steps.Add((t += 0.2f, () => ReportSpray("airborne", 0f, 1f)));
        Steps.Add((t += 0.1f, () => EventBus.PlayerGroundedChanged(true)));
        Steps.Add((t += 0.03f, () => ReportSpray("landing splash", 400f, 1000f)));
        Steps.Add((t += 0.35f, () => ReportSpray("after landing", 80f, 100f)));

        Steps.Add((t += 0.1f, EventBus.PlayerRollStarted));
        Steps.Add((t += 0.35f, () => Expect("nose up while rolling", Pitch(), -MIN_NOSE_UP_DEGREES, true)));
        Steps.Add((t += 0.01f, () => ExpectHullLift("water roll keeps the hull at rest height", 0f)));
        Steps.Add((t += 0.05f, () => ReportSpray("while rolling", 300f, 340f)));
        Steps.Add((t += 0.3f, EventBus.PlayerRollEnded));
        Steps.Add((t += 0.4f, () => ExpectSettled("nose up settles", Pitch())));
        Steps.Add((t += 0.05f, () => ReportSpray("after rolling", 80f, 100f)));

        Steps.Add((t += 0.05f, () =>
        {
            _tracker.enabled = false;
            _animator.SetFloat(SpraySurfaceTracker.SURFACE_PARAMETER, SpraySurfaceTracker.SOLID);
        }));
        Steps.Add((t += 0.2f, () => ReportSpray("on a solid roof", 0f, 1f, 60f, 80f)));
        Steps.Add((t += 0.05f, EventBus.PlayerRollStarted));
        Steps.Add((t += 0.3f, () => ReportSpray("rolling on a roof", 0f, 1f, 200f, 240f)));
        Steps.Add((t += 0.01f, () => ExpectHullLift("grind lifts the hull out of the roof", 1.5f)));
        Steps.Add((t += 0.01f, () => Expect("grind yaw", Mathf.Abs(Mathf.DeltaAngle(0f, _hull.localEulerAngles.y)), BoatRigBuilder.GRIND_YAW_DEGREES - 8f, false)));
        Steps.Add((t += 0.05f, EventBus.PlayerRollEnded));
        Steps.Add((t += 0.4f, () => ExpectHullLift("grind settles", 0f)));
        Steps.Add((t += 0.01f, () => ExpectSettled("grind yaw settles", Mathf.DeltaAngle(0f, _hull.localEulerAngles.y))));
        Steps.Add((t += 0.05f, () =>
        {
            _animator.SetFloat(SpraySurfaceTracker.SURFACE_PARAMETER, SpraySurfaceTracker.WATER);
            _tracker.enabled = true;
        }));
        Steps.Add((t += 0.2f, () => ReportSpray("back on water", 80f, 100f)));

        Steps.Add((t += 0.1f, () => _animator.SetTrigger(BoatAnimator.BACKFLIP_TRIGGER)));
        Steps.Add((t += 0.3f, () =>
        {
            bool flipping = _animator.GetCurrentAnimatorStateInfo(0).IsName(BoatRigBuilder.BACKFLIP_STATE);
            float pitch = Pitch();
            Debug.Log($"{nameof(BoatRigCheck)}: forced backflip state={flipping} pitch={pitch:0.0}");
            if (!flipping || Mathf.Abs(pitch) < 30f) Problems.Add("forced backflip did not rotate the hull");
        }));
        Steps.Add((t += 0.8f, () => ExpectSettled("backflip settles", Pitch())));

        for (int i = 0; i < JUMP_SAMPLES; i++)
        {
            Steps.Add((t += JUMP_SPACING, EventBus.PlayerJumpStarted));
            Steps.Add((t += 0.25f, () =>
            {
                AnimatorStateInfo info = _animator.GetCurrentAnimatorStateInfo(0);
                if (info.IsName(BoatRigBuilder.BACKFLIP_STATE)) _backflips++;
                else if (info.IsName(BoatRigBuilder.HOP_STATE)) _hops++;
            }));
        }

        Steps.Add((t += 0.9f, Finish));
    }

    private static float Roll() => Mathf.DeltaAngle(0f, _hull.localEulerAngles.z);

    private static float Pitch() => Mathf.DeltaAngle(0f, _hull.localEulerAngles.x);

    private static void Expect(string label, float value, float threshold, bool below)
    {
        bool ok = below ? value <= threshold : value >= threshold;
        Debug.Log($"{nameof(BoatRigCheck)}: {label}: {value:0.0} degrees (expected {(below ? "<=" : ">=")} {threshold:0.0})");
        if (!ok) Problems.Add($"{label} failed ({value:0.0} degrees)");
    }

    private static void ExpectHullLift(string label, float expectedLift)
    {
        float lift = _hull.localPosition.y - _hullRestY;
        Debug.Log($"{nameof(BoatRigCheck)}: {label}: hull lift {lift:0.00} (expected {expectedLift:0.00})");
        if (Mathf.Abs(lift - expectedLift) > 0.2f) Problems.Add($"{label} failed (lift {lift:0.00})");
    }

    private static void ExpectSettled(string label, float value)
    {
        Debug.Log($"{nameof(BoatRigCheck)}: {label}: {value:0.0} degrees");
        if (Mathf.Abs(value) > SETTLE_TOLERANCE) Problems.Add($"{label} failed ({value:0.0} degrees)");
    }

    private static void Tick()
    {
        float elapsed = (float)(EditorApplication.timeSinceStartup - _startedAt);

        while (_nextStep < Steps.Count && elapsed >= Steps[_nextStep].at)
        {
            Steps[_nextStep].action();
            _nextStep++;
            if (!_armed) return;
        }
    }

    private static void Finish()
    {
        _armed = false;
        EditorApplication.update -= Tick;
        Application.logMessageReceived -= OnLog;

        if (_root != null && _capsule != null)
        {
            bool rotated = Quaternion.Angle(_root.rotation, Quaternion.identity) > 0.01f;
            bool capsuleChanged = _capsule.center != _capsuleCenter || !Mathf.Approximately(_capsule.height, _capsuleHeight) || !Mathf.Approximately(_capsule.radius, _capsuleRadius);
            Debug.Log($"{nameof(BoatRigCheck)}: hitbox untouched: rootRotated={rotated} capsuleChanged={capsuleChanged} (center={_capsule.center}, height={_capsule.height}, radius={_capsule.radius})");
            if (rotated || capsuleChanged) Problems.Add("the boat animation touched the player root or the capsule");
        }

        Debug.Log($"{nameof(BoatRigCheck)}: jumps={JUMP_SAMPLES} hops={_hops} backflips={_backflips} problems={Problems.Count}");
        if (_hops + _backflips == 0) Problems.Add("jumps did not animate the boat at all");
        foreach (string problem in Problems) Debug.Log($"{nameof(BoatRigCheck)}: {problem}");

        bool healthy = Problems.Count == 0;
        EditorApplication.isPlaying = false;
        EditorApplication.Exit(healthy ? 0 : 2);
    }
}
