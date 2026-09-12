using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

public static class BoatRigBuilder
{
    public const string RIG_NAME = "BoatRig";
    public const string HULL_NAME = "Hull";
    public const string FAN_NODE_NAME = "fan";
    public const string SPRAY_ANCHOR_NAME = "SprayAnchor";
    public const string STERN_SPRAY_NAME = "SprayStern";
    public const string SIDES_SPRAY_NAME = "SpraySides";
    public const string SPARKS_NAME = "SparksStern";
    public const string ANIMATION_FOLDER = "Assets/Animations/Player";
    public const string CONTROLLER_PATH = ANIMATION_FOLDER + "/Boat.controller";
    public const string IDLE_STATE = "Idle";
    public const string BANK_LEFT_STATE = "BankLeft";
    public const string BANK_RIGHT_STATE = "BankRight";
    public const string NOSE_UP_STATE = "NoseUp";
    public const string GRIND_STATE = "Grind";
    public const string ROLL_STATE = "Roll";
    public const float GRIND_YAW_DEGREES = 40f;
    public const float GRIND_TILT_DEGREES = 14f;
    public const string HOP_STATE = "Hop";
    public const string BACKFLIP_STATE = "Backflip";
    public const string FAN_SPIN_STATE = "FanSpin";
    public const string FAN_STOPPED_STATE = "FanStopped";
    public const string SPRAY_GROUND_STATE = "SprayGround";
    public const string SPRAY_AIR_STATE = "SprayAir";
    public const string SPRAY_ROLL_STATE = "SprayRoll";
    public const string SPRAY_LANDING_STATE = "SprayLanding";
    public const string SPRAY_DEAD_STATE = "SprayDead";
    public const float WATER_STERN_RATE = 90f;
    public const float WATER_SIDES_RATE = 40f;
    public const float WATER_ROLL_RATE = 320f;
    public const float SPARK_RATE = 70f;
    public const float SPARK_ROLL_RATE = 220f;

    private const string SPRAY_MATERIAL_PATH = MastTrimmer.GENERATED_FOLDER + "/Player_Spray.mat";
    private const string SPARKS_MATERIAL_PATH = MastTrimmer.GENERATED_FOLDER + "/Player_Sparks.mat";
    private const string DROPLET_TEXTURE_PATH = MastTrimmer.GENERATED_FOLDER + "/Player_Droplet.png";
    private const string EULER_X = "localEulerAnglesRaw.x";
    private const string EULER_Y = "localEulerAnglesRaw.y";
    private const string EULER_Z = "localEulerAnglesRaw.z";
    private const string POSITION_X = "localPosition.x";
    private const string POSITION_Y = "localPosition.y";
    private const string POSITION_Z = "localPosition.z";
    private const float GRIND_TURN_SECONDS = 0.1f;
    private const string EMISSION_RATE = "EmissionModule.rateOverTime.scalar";
    private const string START_SPEED_MIN = "InitialModule.startSpeed.minScalar";
    private const string START_SPEED_MAX = "InitialModule.startSpeed.scalar";
    private const int DEFAULT_LAYER = 0;
    private const int WATER_LAYER = 4;
    private const float BANK_DEGREES = 18f;
    private const float BANK_PEAK_SECONDS = 0.12f;
    private const float BANK_SECONDS = 0.5f;
    private const float NOSE_UP_DEGREES = 14f;
    private const float HOP_NOSE_UP_DEGREES = 10f;
    private const float HOP_NOSE_DOWN_DEGREES = 8f;
    private const float FAN_REVOLUTIONS_PER_SECOND = 4f;
    private const float QUICK_BLEND = 0.05f;
    private const float SOFT_BLEND = 0.12f;
    private const float FAN_STOP_BLEND = 0.6f;
    private const float WATERLINE_ABOVE_KEEL = 0.12f;
    private const float WATER_ROLL_SPEED_MULTIPLIER = 1.6f;
    private const float SPARK_ROLL_SPEED_MULTIPLIER = 1.5f;
    private const float WATER_LANDING_RATE = 900f;
    private const float SPARK_LANDING_RATE = 600f;
    private const float LANDING_SPLASH_SECONDS = 0.07f;
    private const float LANDING_CLIP_SECONDS = 0.2f;
    private const float SPRAY_HOLD_SECONDS = 0.5f;
    private static readonly Vector3 SternSprayDirection = new Vector3(0f, 0.6f, -0.8f);
    private static readonly Vector2 SternSpeed = new Vector2(4f, 8f);
    private static readonly Vector2 SternSize = new Vector2(0.2f, 0.45f);
    private static readonly Vector2 SternLifetime = new Vector2(0.6f, 1f);
    private static readonly Vector2 SidesSpeed = new Vector2(1.5f, 3f);
    private static readonly Vector2 SidesSize = new Vector2(0.12f, 0.25f);
    private static readonly Vector2 SidesLifetime = new Vector2(0.4f, 0.7f);
    private static readonly Vector2 SparkSpeed = new Vector2(5f, 10f);
    private static readonly Vector2 SparkSize = new Vector2(0.05f, 0.11f);
    private static readonly Vector2 SparkLifetime = new Vector2(0.35f, 0.8f);
    private static readonly Color WaterColor = new Color(0.86f, 0.95f, 1f, 0.95f);
    private static readonly Color SparkHot = new Color(1f, 0.96f, 0.7f, 1f);
    private static readonly Color SparkWarm = new Color(1f, 0.55f, 0.15f, 1f);
    private static readonly Color SparkCold = new Color(0.55f, 0.12f, 0.02f, 0f);

    public static void Build(GameObject player, float airTimeSeconds, RollProfile roll, float rollTransitionSeconds, float rollHoldSeconds)
    {
        Transform rig = EnsureRig(player.transform, out Transform hull, out Transform fan);
        StripNestedAnimators(hull);
        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(rig.gameObject);

        AssetFolders.Ensure(ANIMATION_FOLDER);
        string fanPath = RelativePath(fan, rig);
        BuildSpray(rig, hull);
        var grind = new GrindTiming(hull.localPosition, roll, rollTransitionSeconds, rollHoldSeconds);
        AnimatorController controller = BuildController(HULL_NAME, fanPath, airTimeSeconds, grind);

        Animator animator = UiFactory.Ensure<Animator>(rig.gameObject);
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        UiFactory.Ensure<BoatAnimator>(rig.gameObject);

        Debug.Log($"{nameof(BoatRigBuilder)}: '{RIG_NAME}' under '{player.name}' — hull pivot {hull.localPosition:0.00}, fan at '{fanPath}', backflip lasts {airTimeSeconds:0.00} s, " +
                  "spray/sparks emitters rebuilt under the surface anchor and driven by the Spray animator layer");
    }

    private readonly struct GrindTiming
    {
        public GrindTiming(Vector3 hullRest, RollProfile roll, float transitionSeconds, float holdSeconds)
        {
            HullRest = hullRest;
            Roll = roll;
            TransitionSeconds = Mathf.Max(0.01f, transitionSeconds);
            HoldSeconds = Mathf.Max(0f, holdSeconds);
        }

        public Vector3 HullRest { get; }
        public RollProfile Roll { get; }
        public float TransitionSeconds { get; }
        public float HoldSeconds { get; }
        public float TotalSeconds => Roll.TotalDuration;
    }

    private static Transform EnsureRig(Transform player, out Transform hull, out Transform fan)
    {
        Transform rig = player.Find(RIG_NAME);
        Transform model;

        if (rig != null)
        {
            hull = rig.Find(HULL_NAME);
            if (hull == null || hull.childCount == 0) throw new InvalidOperationException($"'{RIG_NAME}' exists but has no '{HULL_NAME}' with the boat model inside — delete it and re-run");
            model = hull.GetChild(0);
        }
        else
        {
            model = FindBoatModel(player);
            rig = new GameObject(RIG_NAME).transform;
            rig.SetParent(player, false);
            rig.SetPositionAndRotation(model.position, model.rotation);

            if (!ModelLibrary.TryGetBounds(model.gameObject, out Bounds bounds)) throw new InvalidOperationException($"'{model.name}' has no renderers");
            hull = new GameObject(HULL_NAME).transform;
            hull.SetParent(rig, false);
            hull.position = bounds.center;
            model.SetParent(hull, true);
        }

        fan = FindDeep(model, FAN_NODE_NAME);
        if (fan == null) throw new InvalidOperationException($"boat model '{model.name}' has no '{FAN_NODE_NAME}' node to spin");
        return rig;
    }

    private static Transform FindBoatModel(Transform player)
    {
        foreach (Transform child in player)
        {
            if (child.name == RIG_NAME) continue;
            if (child.GetComponentInChildren<Renderer>(true) != null) return child;
        }

        throw new InvalidOperationException($"'{player.name}' has no boat model child; drop the Kenney boat under the player first");
    }

    private static Transform FindDeep(Transform root, string name)
    {
        foreach (Transform node in root.GetComponentsInChildren<Transform>(true))
        {
            if (node != root && string.Equals(node.name, name, StringComparison.OrdinalIgnoreCase)) return node;
        }

        return null;
    }

    private static string RelativePath(Transform node, Transform root)
    {
        string path = node.name;
        for (Transform parent = node.parent; parent != null && parent != root; parent = parent.parent) path = $"{parent.name}/{path}";
        return path;
    }

    private static void StripNestedAnimators(Transform root)
    {
        foreach (Animator animator in root.GetComponentsInChildren<Animator>(true)) Object.DestroyImmediate(animator, true);
        foreach (Animation animation in root.GetComponentsInChildren<Animation>(true)) Object.DestroyImmediate(animation, true);
    }

    private static void BuildSpray(Transform rig, Transform hull)
    {
        if (!ModelLibrary.TryGetBounds(hull.gameObject, out Bounds bounds)) throw new InvalidOperationException("boat hull has no renderers to size the spray from");

        Vector3 min = rig.InverseTransformPoint(bounds.min);
        Vector3 max = rig.InverseTransformPoint(bounds.max);
        float hullWidth = max.x - min.x;
        float hullLength = max.z - min.z;

        foreach (string stale in new[] { SPRAY_ANCHOR_NAME, STERN_SPRAY_NAME, SIDES_SPRAY_NAME, SPARKS_NAME })
        {
            Transform old = rig.Find(stale);
            if (old != null) Object.DestroyImmediate(old.gameObject);
        }

        Transform anchor = new GameObject(SPRAY_ANCHOR_NAME).transform;
        anchor.SetParent(rig, false);
        anchor.localPosition = new Vector3(0f, min.y + WATERLINE_ABOVE_KEEL, 0f);

        Material water = EnsureParticleMaterial(SPRAY_MATERIAL_PATH);
        Material sparksMaterial = EnsureParticleMaterial(SPARKS_MATERIAL_PATH);

        ParticleSystem stern = Emitter(anchor, STERN_SPRAY_NAME, new Vector3(0f, 0f, min.z + 0.1f), Quaternion.LookRotation(SternSprayDirection), water,
            SternSpeed, SternSize, SternLifetime, 1f, WATER_STERN_RATE, WaterColor);
        Cone(stern, 32f, Mathf.Max(0.1f, hullWidth * 0.3f));
        Stretch(stern, 1.6f, 0.04f);
        Splash(stern, 0.6f, 0.1f);
        Trails(stern, water);

        ParticleSystem sides = Emitter(anchor, SIDES_SPRAY_NAME, new Vector3(0f, 0f, (min.z + max.z) * 0.5f), Quaternion.Euler(-90f, 0f, 0f), water,
            SidesSpeed, SidesSize, SidesLifetime, 0.9f, WATER_SIDES_RATE, WaterColor);
        ParticleSystem.ShapeModule sidesShape = sides.shape;
        sidesShape.shapeType = ParticleSystemShapeType.Box;
        sidesShape.scale = new Vector3(hullWidth + 0.3f, hullLength * 0.8f, 0.05f);
        sidesShape.randomDirectionAmount = 0.45f;
        Splash(sides, 0.6f, 0.1f);

        ParticleSystem sparks = Emitter(anchor, SPARKS_NAME, new Vector3(0f, 0f, min.z + 0.2f), Quaternion.LookRotation(SternSprayDirection), sparksMaterial,
            SparkSpeed, SparkSize, SparkLifetime, 1.3f, 0f, SparkHot);
        Cone(sparks, 40f, 0.15f);
        Stretch(sparks, 2.5f, 0.06f);
        Splash(sparks, 0.1f, 0.6f);
        SparkColor(sparks);

        SpraySurfaceTracker tracker = UiFactory.Ensure<SpraySurfaceTracker>(rig.gameObject);
        var serialized = new SerializedObject(tracker);
        serialized.FindProperty("_anchor").objectReferenceValue = anchor;
        SerializedProperty emitters = serialized.FindProperty("_emitters");
        emitters.arraySize = 3;
        emitters.GetArrayElementAtIndex(0).objectReferenceValue = stern;
        emitters.GetArrayElementAtIndex(1).objectReferenceValue = sides;
        emitters.GetArrayElementAtIndex(2).objectReferenceValue = sparks;
        serialized.FindProperty("_waterLayer").intValue = DEFAULT_LAYER;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static ParticleSystem Emitter(Transform anchor, string name, Vector3 localPosition, Quaternion localRotation, Material material,
        Vector2 speed, Vector2 size, Vector2 lifetime, float gravity, float rate, Color color)
    {
        var emitter = new GameObject(name);
        emitter.transform.SetParent(anchor, false);
        emitter.transform.localPosition = localPosition;
        emitter.transform.localRotation = localRotation;

        var system = emitter.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = system.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startSpeed = new ParticleSystem.MinMaxCurve(speed.x, speed.y);
        main.startSize = new ParticleSystem.MinMaxCurve(size.x, size.y);
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime.x, lifetime.y);
        main.startColor = color;
        main.gravityModifier = gravity;
        main.maxParticles = 600;

        ParticleSystem.EmissionModule emission = system.emission;
        emission.enabled = true;
        emission.rateOverTime = rate;

        ParticleSystem.VelocityOverLifetimeModule velocity = system.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = 0f;
        velocity.y = 0f;
        velocity.z = 0f;

        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = system.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.4f));

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = system.colorOverLifetime;
        colorOverLifetime.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.9f, 0.6f), new GradientAlphaKey(0f, 1f) });
        colorOverLifetime.color = gradient;

        var renderer = emitter.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortMode = ParticleSystemSortMode.Distance;
        renderer.sharedMaterial = material;
        return system;
    }

    private static void Cone(ParticleSystem system, float angle, float radius)
    {
        ParticleSystem.ShapeModule shape = system.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = angle;
        shape.radius = radius;
    }

    private static void Stretch(ParticleSystem system, float lengthScale, float velocityScale)
    {
        var renderer = system.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.lengthScale = lengthScale;
        renderer.velocityScale = velocityScale;
    }

    private static void Splash(ParticleSystem system, float lifetimeLoss, float bounce)
    {
        ParticleSystem.CollisionModule collision = system.collision;
        collision.enabled = true;
        collision.type = ParticleSystemCollisionType.World;
        collision.mode = ParticleSystemCollisionMode.Collision3D;
        collision.quality = ParticleSystemCollisionQuality.High;
        collision.collidesWith = (1 << DEFAULT_LAYER) | (1 << WATER_LAYER) | (1 << ObstacleSetBuilder.OBSTACLE_LAYER) | (1 << ObstacleSetBuilder.BAD_OBSTACLE_LAYER);
        collision.dampen = 0.4f;
        collision.bounce = bounce;
        collision.lifetimeLoss = lifetimeLoss;
        collision.radiusScale = 0.5f;
    }

    private static void Trails(ParticleSystem system, Material material)
    {
        ParticleSystem.TrailModule trails = system.trails;
        trails.enabled = true;
        trails.mode = ParticleSystemTrailMode.PerParticle;
        trails.ratio = 0.6f;
        trails.lifetime = 0.18f;
        trails.dieWithParticles = true;
        trails.inheritParticleColor = true;
        trails.widthOverTrail = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.25f, 1f, 0f));
        system.GetComponent<ParticleSystemRenderer>().trailMaterial = material;
    }

    private static void SparkColor(ParticleSystem system)
    {
        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = system.colorOverLifetime;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(SparkHot, 0f), new GradientColorKey(SparkWarm, 0.45f), new GradientColorKey(SparkCold, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
        colorOverLifetime.color = gradient;
    }

    private static Material EnsureParticleMaterial(string path)
    {
        Material template = GraphicsSettings.currentRenderPipeline != null
            ? GraphicsSettings.currentRenderPipeline.defaultParticleMaterial
            : AssetDatabase.GetBuiltinExtraResource<Material>("Default-ParticleSystem.mat");

        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            AssetFolders.Ensure(MastTrimmer.GENERATED_FOLDER);
            material = new Material(template);
            AssetDatabase.CreateAsset(material, path);
        }

        var droplet = AssetDatabase.LoadAssetAtPath<Texture2D>(DROPLET_TEXTURE_PATH);
        if (droplet == null) Debug.LogWarning($"{nameof(BoatRigBuilder)}: {DROPLET_TEXTURE_PATH} missing (run Tools/ui-sprites/droplet.py); using the default particle texture");
        else
        {
            material.SetTexture("_BaseMap", droplet);
            material.SetTexture("_MainTex", droplet);
        }

        EditorUtility.SetDirty(material);
        return material;
    }

    private static AnimatorController BuildController(string hullPath, string fanPath, float airTime, GrindTiming grind)
    {
        foreach (string asset in AssetDatabase.FindAssets("", new[] { ANIMATION_FOLDER }))
        {
            string path = AssetDatabase.GUIDToAssetPath(asset);
            if (path.EndsWith(".anim", StringComparison.Ordinal) || path.EndsWith(".controller", StringComparison.Ordinal)) AssetDatabase.DeleteAsset(path);
        }

        AnimationClip idle = Save(HullClip(IDLE_STATE, hullPath, true, Constant(0f, BANK_SECONDS), Constant(0f, BANK_SECONDS)), IDLE_STATE);
        AnimationClip bankLeft = Save(HullClip(BANK_LEFT_STATE, hullPath, false, Constant(0f, BANK_SECONDS), Keys((0f, 0f), (BANK_PEAK_SECONDS, BANK_DEGREES), (BANK_SECONDS, 0f))), BANK_LEFT_STATE);
        AnimationClip bankRight = Save(HullClip(BANK_RIGHT_STATE, hullPath, false, Constant(0f, BANK_SECONDS), Keys((0f, 0f), (BANK_PEAK_SECONDS, -BANK_DEGREES), (BANK_SECONDS, 0f))), BANK_RIGHT_STATE);
        AnimationClip noseUp = Save(HullClip(NOSE_UP_STATE, hullPath, true, Constant(-NOSE_UP_DEGREES, BANK_SECONDS), Constant(0f, BANK_SECONDS)), NOSE_UP_STATE);
        AnimationClip grindClip = Save(GrindClip(hullPath, grind), GRIND_STATE);
        AnimationClip hop = Save(HullClip(HOP_STATE, hullPath, false,
            Keys((0f, 0f), (airTime * 0.22f, -HOP_NOSE_UP_DEGREES), (airTime * 0.7f, HOP_NOSE_DOWN_DEGREES), (airTime, 0f)), Constant(0f, airTime)), HOP_STATE);
        AnimationClip backflip = Save(HullClip(BACKFLIP_STATE, hullPath, false, Keys((0f, 0f), (airTime, -360f)), Constant(0f, airTime)), BACKFLIP_STATE);

        float spinSeconds = 1f / FAN_REVOLUTIONS_PER_SECOND;
        AnimationClip fanSpin = Save(FanClip(FAN_SPIN_STATE, fanPath, Linear(0f, 360f, spinSeconds), true), FAN_SPIN_STATE);
        AnimationClip fanStopped = Save(FanClip(FAN_STOPPED_STATE, fanPath, Constant(0f, spinSeconds), true), FAN_STOPPED_STATE);

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(CONTROLLER_PATH);
        foreach (string trigger in new[] { BoatAnimator.BANK_LEFT_TRIGGER, BoatAnimator.BANK_RIGHT_TRIGGER, BoatAnimator.JUMP_TRIGGER, BoatAnimator.BACKFLIP_TRIGGER })
        {
            controller.AddParameter(trigger, AnimatorControllerParameterType.Trigger);
        }

        controller.AddParameter(BoatAnimator.ROLLING_PARAMETER, AnimatorControllerParameterType.Bool);
        controller.AddParameter(BoatAnimator.DEAD_PARAMETER, AnimatorControllerParameterType.Bool);
        controller.AddParameter(BoatAnimator.GROUNDED_PARAMETER, AnimatorControllerParameterType.Bool);
        controller.AddParameter(SpraySurfaceTracker.SURFACE_PARAMETER, AnimatorControllerParameterType.Float);

        BuildHullLayer(controller, idle, bankLeft, bankRight, noseUp, grindClip, hop, backflip);
        BuildFanLayer(controller, fanSpin, fanStopped);
        BuildSprayLayer(controller);
        RemoveParameter(controller, "Blend");
        return controller;
    }

    private static void BuildHullLayer(AnimatorController controller, AnimationClip idleClip, AnimationClip bankLeftClip, AnimationClip bankRightClip,
        AnimationClip noseUpClip, AnimationClip grindClip, AnimationClip hopClip, AnimationClip backflipClip)
    {
        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        AnimatorState idle = State(machine, IDLE_STATE, idleClip);
        AnimatorState bankLeft = State(machine, BANK_LEFT_STATE, bankLeftClip);
        AnimatorState bankRight = State(machine, BANK_RIGHT_STATE, bankRightClip);
        AnimatorState noseUp = SurfaceState(controller, 0, ROLL_STATE, noseUpClip, grindClip);
        AnimatorState hop = State(machine, HOP_STATE, hopClip);
        AnimatorState backflip = State(machine, BACKFLIP_STATE, backflipClip);
        machine.defaultState = idle;

        foreach (AnimatorState from in new[] { idle, bankRight, noseUp, hop }) Trigger(from, bankLeft, BoatAnimator.BANK_LEFT_TRIGGER, QUICK_BLEND);
        foreach (AnimatorState from in new[] { idle, bankLeft, noseUp, hop }) Trigger(from, bankRight, BoatAnimator.BANK_RIGHT_TRIGGER, QUICK_BLEND);
        foreach (AnimatorState from in new[] { idle, bankLeft, bankRight, noseUp })
        {
            Trigger(from, hop, BoatAnimator.JUMP_TRIGGER, QUICK_BLEND);
            Trigger(from, backflip, BoatAnimator.BACKFLIP_TRIGGER, QUICK_BLEND);
        }

        Bool(idle, noseUp, BoatAnimator.ROLLING_PARAMETER, true, SOFT_BLEND);
        Bool(noseUp, idle, BoatAnimator.ROLLING_PARAMETER, false, SOFT_BLEND);
        foreach (AnimatorState finished in new[] { bankLeft, bankRight, hop, backflip }) ExitTo(finished, idle, QUICK_BLEND);
    }

    private static void BuildFanLayer(AnimatorController controller, AnimationClip spinClip, AnimationClip stoppedClip)
    {
        AnimatorStateMachine machine = AddLayer(controller, "Propeller");
        AnimatorState spin = State(machine, FAN_SPIN_STATE, spinClip);
        AnimatorState stopped = State(machine, FAN_STOPPED_STATE, stoppedClip);
        machine.defaultState = spin;
        Bool(spin, stopped, BoatAnimator.DEAD_PARAMETER, true, FAN_STOP_BLEND);
    }

    private static void BuildSprayLayer(AnimatorController controller)
    {
        AnimationClip groundWater = Save(SprayClip($"{SPRAY_GROUND_STATE}Water", WATER_STERN_RATE, WATER_SIDES_RATE, 1f, 0f, 1f), $"{SPRAY_GROUND_STATE}Water");
        AnimationClip groundSparks = Save(SprayClip($"{SPRAY_GROUND_STATE}Sparks", 0f, 0f, 1f, SPARK_RATE, 1f), $"{SPRAY_GROUND_STATE}Sparks");
        AnimationClip rollWater = Save(SprayClip($"{SPRAY_ROLL_STATE}Water", WATER_ROLL_RATE, WATER_SIDES_RATE, WATER_ROLL_SPEED_MULTIPLIER, 0f, 1f), $"{SPRAY_ROLL_STATE}Water");
        AnimationClip rollSparks = Save(SprayClip($"{SPRAY_ROLL_STATE}Sparks", 0f, 0f, 1f, SPARK_ROLL_RATE, SPARK_ROLL_SPEED_MULTIPLIER), $"{SPRAY_ROLL_STATE}Sparks");
        AnimationClip landingWater = Save(LandingClip($"{SPRAY_LANDING_STATE}Water", WATER_LANDING_RATE, WATER_STERN_RATE, WATER_SIDES_RATE, 0f, 0f), $"{SPRAY_LANDING_STATE}Water");
        AnimationClip landingSparks = Save(LandingClip($"{SPRAY_LANDING_STATE}Sparks", 0f, 0f, 0f, SPARK_LANDING_RATE, SPARK_RATE), $"{SPRAY_LANDING_STATE}Sparks");
        AnimationClip air = Save(SprayClip(SPRAY_AIR_STATE, 0f, 0f, 1f, 0f, 1f), SPRAY_AIR_STATE);
        AnimationClip dead = Save(SprayClip(SPRAY_DEAD_STATE, 0f, 0f, 1f, 0f, 1f), SPRAY_DEAD_STATE);

        AnimatorStateMachine machine = AddLayer(controller, "Spray");
        int layerIndex = controller.layers.Length - 1;
        AnimatorState groundState = SurfaceState(controller, layerIndex, SPRAY_GROUND_STATE, groundWater, groundSparks);
        AnimatorState rollState = SurfaceState(controller, layerIndex, SPRAY_ROLL_STATE, rollWater, rollSparks);
        AnimatorState landingState = SurfaceState(controller, layerIndex, SPRAY_LANDING_STATE, landingWater, landingSparks);
        AnimatorState airState = State(machine, SPRAY_AIR_STATE, air);
        AnimatorState deadState = State(machine, SPRAY_DEAD_STATE, dead);
        machine.defaultState = groundState;

        Bool(groundState, airState, BoatAnimator.GROUNDED_PARAMETER, false, QUICK_BLEND);
        Bool(rollState, airState, BoatAnimator.GROUNDED_PARAMETER, false, QUICK_BLEND);
        Bool(landingState, airState, BoatAnimator.GROUNDED_PARAMETER, false, QUICK_BLEND);
        Bool(airState, landingState, BoatAnimator.GROUNDED_PARAMETER, true, 0f);
        ExitTo(landingState, groundState, QUICK_BLEND);
        Bool(groundState, rollState, BoatAnimator.ROLLING_PARAMETER, true, QUICK_BLEND);
        Bool(rollState, groundState, BoatAnimator.ROLLING_PARAMETER, false, SOFT_BLEND);
        foreach (AnimatorState alive in new[] { groundState, airState, rollState, landingState }) Bool(alive, deadState, BoatAnimator.DEAD_PARAMETER, true, QUICK_BLEND);
    }

    private static AnimatorStateMachine AddLayer(AnimatorController controller, string name)
    {
        controller.AddLayer(name);
        AnimatorControllerLayer[] layers = controller.layers;
        int index = layers.Length - 1;
        layers[index].defaultWeight = 1f;
        controller.layers = layers;
        return controller.layers[index].stateMachine;
    }

    private static AnimatorState SurfaceState(AnimatorController controller, int layerIndex, string name, AnimationClip water, AnimationClip sparks)
    {
        AnimatorState state = controller.CreateBlendTreeInController(name, out BlendTree tree, layerIndex);
        tree.name = name;
        tree.blendType = BlendTreeType.Simple1D;
        tree.blendParameter = SpraySurfaceTracker.SURFACE_PARAMETER;
        tree.useAutomaticThresholds = false;
        tree.AddChild(water, SpraySurfaceTracker.WATER);
        tree.AddChild(sparks, SpraySurfaceTracker.SOLID);
        state.writeDefaultValues = true;
        return state;
    }

    private static void RemoveParameter(AnimatorController controller, string name)
    {
        AnimatorControllerParameter[] parameters = controller.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].name == name) controller.RemoveParameter(i);
        }
    }

    private static AnimatorState State(AnimatorStateMachine machine, string name, AnimationClip clip)
    {
        AnimatorState state = machine.AddState(name);
        state.motion = clip;
        state.writeDefaultValues = true;
        return state;
    }

    private static void Trigger(AnimatorState from, AnimatorState to, string trigger, float blend)
    {
        AnimatorStateTransition transition = from.AddTransition(to);
        transition.hasExitTime = false;
        transition.duration = blend;
        transition.AddCondition(AnimatorConditionMode.If, 0f, trigger);
    }

    private static void Bool(AnimatorState from, AnimatorState to, string parameter, bool value, float blend)
    {
        AnimatorStateTransition transition = from.AddTransition(to);
        transition.hasExitTime = false;
        transition.duration = blend;
        transition.AddCondition(value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, parameter);
    }

    private static void ExitTo(AnimatorState from, AnimatorState to, float blend)
    {
        AnimatorStateTransition transition = from.AddTransition(to);
        transition.hasExitTime = true;
        transition.exitTime = 1f;
        transition.duration = blend;
    }

    private static AnimationClip HullClip(string name, string hullPath, bool loop, AnimationCurve pitch, AnimationCurve roll)
    {
        var clip = new AnimationClip { name = name, frameRate = 60f };
        clip.SetCurve(hullPath, typeof(Transform), EULER_X, pitch);
        clip.SetCurve(hullPath, typeof(Transform), EULER_Y, Constant(0f, Mathf.Max(LastKeyTime(pitch), LastKeyTime(roll))));
        clip.SetCurve(hullPath, typeof(Transform), EULER_Z, roll);
        SetLoop(clip, loop);
        return clip;
    }

    private static AnimationClip GrindClip(string hullPath, GrindTiming grind)
    {
        float total = grind.TotalSeconds;
        float riseStart = grind.TransitionSeconds + grind.HoldSeconds;
        var lift = new AnimationCurve(
            new Keyframe(0f, grind.HullRest.y),
            new Keyframe(grind.TransitionSeconds, grind.HullRest.y - grind.Roll.Evaluate(grind.TransitionSeconds)),
            new Keyframe(riseStart, grind.HullRest.y - grind.Roll.Evaluate(riseStart)),
            new Keyframe(total, grind.HullRest.y));
        for (int i = 0; i < lift.length; i++)
        {
            AnimationUtility.SetKeyLeftTangentMode(lift, i, AnimationUtility.TangentMode.Linear);
            AnimationUtility.SetKeyRightTangentMode(lift, i, AnimationUtility.TangentMode.Linear);
        }

        float turnOut = Mathf.Max(GRIND_TURN_SECONDS, total - grind.TransitionSeconds);
        var clip = new AnimationClip { name = GRIND_STATE, frameRate = 60f };
        clip.SetCurve(hullPath, typeof(Transform), POSITION_X, Constant(grind.HullRest.x, total));
        clip.SetCurve(hullPath, typeof(Transform), POSITION_Y, lift);
        clip.SetCurve(hullPath, typeof(Transform), POSITION_Z, Constant(grind.HullRest.z, total));
        clip.SetCurve(hullPath, typeof(Transform), EULER_X, Constant(0f, total));
        clip.SetCurve(hullPath, typeof(Transform), EULER_Y, Keys((0f, 0f), (GRIND_TURN_SECONDS, GRIND_YAW_DEGREES), (turnOut, GRIND_YAW_DEGREES), (total, 0f)));
        clip.SetCurve(hullPath, typeof(Transform), EULER_Z, Keys((0f, 0f), (GRIND_TURN_SECONDS, GRIND_TILT_DEGREES), (turnOut, GRIND_TILT_DEGREES), (total, 0f)));
        SetLoop(clip, false);
        return clip;
    }

    private static AnimationClip FanClip(string name, string fanPath, AnimationCurve spin, bool loop)
    {
        var clip = new AnimationClip { name = name, frameRate = 60f };
        clip.SetCurve(fanPath, typeof(Transform), EULER_X, Constant(0f, LastKeyTime(spin)));
        clip.SetCurve(fanPath, typeof(Transform), EULER_Y, Constant(0f, LastKeyTime(spin)));
        clip.SetCurve(fanPath, typeof(Transform), EULER_Z, spin);
        SetLoop(clip, loop);
        return clip;
    }

    private static AnimationClip SprayClip(string name, float sternRate, float sidesRate, float waterSpeedMultiplier, float sparkRate, float sparkSpeedMultiplier)
    {
        var clip = new AnimationClip { name = name, frameRate = 60f };
        EmitterCurves(clip, STERN_SPRAY_NAME, Constant(sternRate, SPRAY_HOLD_SECONDS), SternSpeed * waterSpeedMultiplier, SPRAY_HOLD_SECONDS);
        EmitterCurves(clip, SIDES_SPRAY_NAME, Constant(sidesRate, SPRAY_HOLD_SECONDS), SidesSpeed, SPRAY_HOLD_SECONDS);
        EmitterCurves(clip, SPARKS_NAME, Constant(sparkRate, SPRAY_HOLD_SECONDS), SparkSpeed * sparkSpeedMultiplier, SPRAY_HOLD_SECONDS);
        SetLoop(clip, true);
        return clip;
    }

    private static AnimationClip LandingClip(string name, float waterSpike, float waterRest, float sidesRate, float sparkSpike, float sparkRest)
    {
        var clip = new AnimationClip { name = name, frameRate = 60f };
        EmitterCurves(clip, STERN_SPRAY_NAME, Spike(waterSpike, waterRest), SternSpeed, LANDING_CLIP_SECONDS);
        EmitterCurves(clip, SIDES_SPRAY_NAME, Constant(sidesRate, LANDING_CLIP_SECONDS), SidesSpeed, LANDING_CLIP_SECONDS);
        EmitterCurves(clip, SPARKS_NAME, Spike(sparkSpike, sparkRest), SparkSpeed, LANDING_CLIP_SECONDS);
        SetLoop(clip, false);
        return clip;
    }

    private static void EmitterCurves(AnimationClip clip, string emitter, AnimationCurve rate, Vector2 speed, float seconds)
    {
        string path = $"{SPRAY_ANCHOR_NAME}/{emitter}";
        clip.SetCurve(path, typeof(ParticleSystem), EMISSION_RATE, rate);
        clip.SetCurve(path, typeof(ParticleSystem), START_SPEED_MIN, Constant(speed.x, seconds));
        clip.SetCurve(path, typeof(ParticleSystem), START_SPEED_MAX, Constant(speed.y, seconds));
    }

    private static AnimationCurve Spike(float peak, float rest)
    {
        var curve = new AnimationCurve(
            new Keyframe(0f, peak),
            new Keyframe(LANDING_SPLASH_SECONDS, peak),
            new Keyframe(LANDING_SPLASH_SECONDS + 0.01f, rest),
            new Keyframe(LANDING_CLIP_SECONDS, rest));
        for (int i = 0; i < curve.length; i++)
        {
            AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Constant);
            AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Constant);
        }

        return curve;
    }

    private static void SetLoop(AnimationClip clip, bool loop)
    {
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
    }

    private static AnimationClip Save(AnimationClip clip, string name)
    {
        AssetDatabase.CreateAsset(clip, $"{ANIMATION_FOLDER}/Boat{name}.anim");
        return clip;
    }

    private static AnimationCurve Constant(float value, float seconds) => AnimationCurve.Constant(0f, seconds, value);

    private static AnimationCurve Linear(float from, float to, float seconds)
    {
        float slope = (to - from) / seconds;
        return new AnimationCurve(new Keyframe(0f, from, slope, slope), new Keyframe(seconds, to, slope, slope));
    }

    private static AnimationCurve Keys(params (float time, float value)[] points)
    {
        var keys = new List<Keyframe>();
        foreach ((float time, float value) in points) keys.Add(new Keyframe(time, value));

        var curve = new AnimationCurve(keys.ToArray());
        for (int i = 0; i < curve.length; i++)
        {
            AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
            AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
        }

        return curve;
    }

    private static float LastKeyTime(AnimationCurve curve) => curve.length > 0 ? curve[curve.length - 1].time : 0f;
}
