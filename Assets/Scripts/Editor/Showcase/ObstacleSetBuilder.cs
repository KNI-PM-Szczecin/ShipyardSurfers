using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

public static class ObstacleSetBuilder
{
    public const string PREFAB_ROOT = "Assets/Prefabs/ObsticleSets";
    public const string APPEARANCE_FOLDER = "Assets/Scripts/ScriptableObjects/Resources/TrackApperences";
    public const string LEGACY_APPEARANCE_FOLDER = "Assets/Scripts/ScriptableObjects/LegacyTrackApperences";
    public const string PIRATE_APPEARANCE_PATH = APPEARANCE_FOLDER + "/TrackApperencePirate.asset";
    public const string PORT_APPEARANCE_PATH = APPEARANCE_FOLDER + "/TrackApperencePort.asset";
    public const string TRACK_PART_PREFAB_PATH = "Assets/Prefabs/Sections/TrackPartBP.prefab";
    private const string TEMPLATE_APPEARANCE_NAME = "TrackApperence1";
    private const string MENU_PATH = "Shipyard Surfers/World/Rebuild Obstacle Sets";

    public const int OBSTACLE_LAYER = 6;
    public const int BAD_OBSTACLE_LAYER = 8;
    public const float FLOOR_Y = -1f;
    public const float CELL_DEPTH = 5.6f;
    public const float OBSTACLE_WIDTH = 2.4f;
    public const float JUMP_HEIGHT = 1.6f;
    public const float SLIDE_CLEARANCE = 1.2f;
    public const float SLIDE_DECK_THICKNESS = 0.3f;
    public const float BLOCKADE_HEIGHT = 3.7f;
    public const float MIN_BLOCKADE_HEIGHT = 2.4f;
    public const float LANE_PITCH = 3f;
    public const float BLOCKADE_VISUAL_WIDTH = 2.6f;
    public const float RAMP_RUN = 4.4f;
    public const float RAMP_RISE = BLOCKADE_HEIGHT;
    public const float WALL_OFFSET = 5.5f;
    private const float TRIGGER_THICKNESS = 0.1f;
    private const float SIDE_WALL_HEIGHT = 1.6f;
    private const float RAMP_BOARD_THICKNESS = 0.35f;
    private const float RAMP_MIN_THICKNESS = 0.15f;
    private const int SHIP_MIN_LENGTH = 2;
    private const int BACKDROP_PASSES = 3;
    private const float BACKDROP_SPACING_MIN = 3f;
    private const float BACKDROP_SPACING_MAX = 6f;
    private static readonly string[] SailNodePrefixes = { "sail", "flag" };

    [MenuItem(MENU_PATH)]
    public static void BuildAll()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        TrackApperenceSO template = LoadTemplate();

        BuildPirateSet(template);
        BuildPortSet(template);
        RetireLegacyAppearances();
        WireDefaultWalls();

        AssetDatabase.SaveAssets();
        Debug.Log($"{nameof(ObstacleSetBuilder)}: obstacle sets rebuilt");
    }

    private static void BuildPirateSet(TrackApperenceSO template)
    {
        const KenneyKit kit = KenneyKit.Pirate;
        string folder = $"{PREFAB_ROOT}/PirateSet";
        string dressing = $"{folder}/Dressing";
        EnsureFolder(dressing);

        GameObject jump = NewRoot("JumpBlock");
        GameObject cannon = SpawnFitted(kit, "cannon", jump.transform, OBSTACLE_WIDTH, JUMP_HEIGHT, 2.6f);
        cannon.transform.Rotate(Vector3.up, 90f, Space.World);
        PlaceOnFloor(cannon, jump.transform);
        FinalizeJump(jump);
        GameObject jumpPrefab = SavePrefab(jump, $"{folder}/JumpBlock.prefab");

        GameObject slide = NewRoot("SlideBlock");
        BuildDeckSlide(slide.transform, kit, "structure-platform");
        FinalizeSlide(slide);
        GameObject slidePrefab = SavePrefab(slide, $"{folder}/SlideBlock.prefab");

        GameObject ramp = NewRoot("RampBlock");
        BuildBoardRamp(ramp.transform, kit, "platform");
        foreach (float side in new[] { -1f, 1f })
        {
            GameObject barrel = SpawnFitted(kit, "barrel", ramp.transform, 0.8f, 1.2f, 0.8f);
            PlaceOnFloor(barrel, ramp.transform, new Vector3(side * 1.4f, 0f, RAMP_RUN * 0.15f));
        }
        FinalizeRamp(ramp);
        GameObject rampPrefab = SavePrefab(ramp, $"{folder}/RampBlock.prefab");

        GameObject blockadePrefab = Blockade($"{folder}/BlockadeBlock_1.prefab", tile =>
        {
            GameObject piece = ModelLibrary.Spawn(kit, "castle-wall", tile);
            ModelLibrary.OrientLongAxisAlongX(piece);
            ModelLibrary.FitHeight(piece, BLOCKADE_HEIGHT);
            NarrowTo(piece, BLOCKADE_VISUAL_WIDTH);
            PlaceOnFloor(piece, tile);
        });

        var variants = new List<BlockadeVariant>
        {
            ShipBlockade(kit, "ship-pirate-small", $"{folder}/BlockadeBlock_2.prefab"),
            ShipBlockade(kit, "ship-small", $"{folder}/BlockadeBlock_3.prefab"),
            ShipBlockade(kit, "ship-wreck", $"{folder}/BlockadeBlock_4.prefab"),
            ShipBlockade(kit, "boat-row-large", $"{folder}/BlockadeBlock_5.prefab"),
        };

        var walls = new List<GameObject>();
        (string model, float height)[] wallPieces =
        {
            ("rocks-a", 3.2f), ("rocks-b", 3.6f), ("rocks-c", 2.8f),
            ("rocks-sand-a", 3f), ("rocks-sand-b", 3.4f), ("rocks-sand-c", 2.6f),
        };
        foreach ((string model, float height) in wallPieces)
        {
            walls.Add(DressingPrefab($"{dressing}/Wall_{model}.prefab", root => Fit(kit, model, root, height)));
        }

        var props = new List<BackdropProp>
        {
            Prop(dressing, kit, "patch-sand", 0.3f, 0f, 8f, 3f),
            Prop(dressing, kit, "patch-grass", 0.3f, 0f, 8f, 3f),
            Prop(dressing, kit, "patch-sand-foliage", 0.5f, 0f, 8f, 2.5f),
            Prop(dressing, kit, "patch-grass-foliage", 0.5f, 0f, 8f, 2.5f),
            Prop(dressing, kit, "grass-patch", 0.7f, 0f, 9f, 3f),
            Prop(dressing, kit, "grass-plant", 0.9f, 0f, 9f, 2f),
            Prop(dressing, kit, "grass", 0.6f, 0f, 9f, 2f),
            Prop(dressing, kit, "rocks-sand-a", 1.3f, 0f, 8f, 3f),
            Prop(dressing, kit, "rocks-sand-b", 1.6f, 0f, 8f, 3f),
            Prop(dressing, kit, "rocks-sand-c", 1.1f, 0f, 8f, 2.5f),
            Prop(dressing, kit, "rocks-a", 3f, 1f, 13f, 2.5f),
            Prop(dressing, kit, "rocks-b", 3.6f, 1f, 13f, 2.5f),
            Prop(dressing, kit, "rocks-c", 2.4f, 1f, 13f, 2.5f),
            Prop(dressing, kit, "barrel", 1f, 0.3f, 6f, 1.5f),
            Prop(dressing, kit, "crate", 1f, 0.3f, 6f, 1.5f),
            Prop(dressing, kit, "crate-bottles", 1f, 0.3f, 6f, 1f),
            Prop(dressing, kit, "chest", 0.9f, 0.3f, 6f, 1f),
            Prop(dressing, kit, "cannon-mobile", 1.4f, 0.5f, 7f, 0.8f),
            Prop(dressing, kit, "tool-shovel", 1.2f, 0.5f, 6f, 0.6f),
            Prop(dressing, kit, "palm-straight", 6.5f, 1f, 14f, 3f),
            Prop(dressing, kit, "palm-bend", 6f, 1f, 14f, 3f),
            Prop(dressing, kit, "palm-detailed-straight", 7f, 2f, 16f, 2.5f),
            Prop(dressing, kit, "palm-detailed-bend", 6.5f, 2f, 16f, 2.5f),
            Prop(dressing, kit, "structure-fence", 2.2f, 0.3f, 8f, 1.5f, false),
            Prop(dressing, kit, "structure-platform-dock", 1.6f, 0.5f, 7f, 1.2f, false, -0.4f),
            Prop(dressing, kit, "flag-pirate-high", 6f, 2f, 13f, 0.9f, false),
            Prop(dressing, kit, "tower-watch", 5.5f, 4f, 18f, 0.9f, false),
            Prop(dressing, kit, "tower-complete-small", 7f, 6f, 24f, 0.7f, false),
            Prop(dressing, kit, "tower-complete-large", 10f, 8f, 28f, 0.7f, false),
            Prop(dressing, kit, "structure", 4f, 6f, 22f, 0.9f, false),
            Prop(dressing, kit, "structure-roof", 4.5f, 6f, 22f, 0.7f, false),
            ShipProp(dressing, kit, "boat-row-small", 1.2f, 3f, 16f, 1f, -0.5f),
            ShipProp(dressing, kit, "ship-medium", 10f, 10f, 32f, 0.9f, -1.2f),
            ShipProp(dressing, kit, "ship-pirate-medium", 10f, 10f, 32f, 0.9f, -1.2f),
            ShipProp(dressing, kit, "ship-large", 12f, 12f, 36f, 0.8f, -1.5f),
            ShipProp(dressing, kit, "ship-pirate-large", 14f, 12f, 36f, 0.8f, -1.5f),
            ShipProp(dressing, kit, "ship-ghost", 11f, 16f, 38f, 0.4f, -1.5f),
        };

        SaveAppearance(PIRATE_APPEARANCE_PATH, template, jumpPrefab, slidePrefab, rampPrefab, blockadePrefab, variants, walls, props);
    }

    private static void BuildPortSet(TrackApperenceSO template)
    {
        const KenneyKit sea = KenneyKit.Watercraft;
        const KenneyKit factory = KenneyKit.Factory;
        string folder = $"{PREFAB_ROOT}/PortSet";
        string dressing = $"{folder}/Dressing";
        EnsureFolder(dressing);

        GameObject jump = NewRoot("JumpBlock");
        GameObject pile = SpawnFitted(sea, "cargo-pile-a", jump.transform, OBSTACLE_WIDTH, JUMP_HEIGHT, 2.8f);
        PlaceOnFloor(pile, jump.transform);
        FinalizeJump(jump);
        GameObject jumpPrefab = SavePrefab(jump, $"{folder}/JumpBlock.prefab");

        GameObject slide = NewRoot("SlideBlock");
        GameObject gate = ModelLibrary.Spawn(sea, "gate", slide.transform);
        ModelLibrary.FitWidth(gate, OBSTACLE_WIDTH + 0.2f);
        ModelLibrary.ScaleHeightTo(gate, SLIDE_CLEARANCE + 0.7f);
        PlaceOnFloor(gate, slide.transform);
        FinalizeSlide(slide);
        GameObject slidePrefab = SavePrefab(slide, $"{folder}/SlideBlock.prefab");

        GameObject ramp = NewRoot("RampBlock");
        BuildBoardRamp(ramp.transform, factory, "conveyor-long");
        foreach (float side in new[] { -1f, 1f })
        {
            GameObject cone = SpawnFitted(factory, "warning-orange", ramp.transform, 0.6f, 0.8f, 0.6f);
            PlaceOnFloor(cone, ramp.transform, new Vector3(side * 1.4f, 0f, RAMP_RUN * 0.1f));
        }
        FinalizeRamp(ramp);
        GameObject rampPrefab = SavePrefab(ramp, $"{folder}/RampBlock.prefab");

        GameObject blockadePrefab = Blockade($"{folder}/BlockadeBlock_1.prefab", tile =>
        {
            StackedContainer(tile, sea, "cargo-container-a", 0f);
            StackedContainer(tile, sea, "cargo-container-b", BLOCKADE_HEIGHT * 0.5f);
        });

        var variants = new List<BlockadeVariant>
        {
            ShipBlockade(sea, "ship-cargo-a", $"{folder}/BlockadeBlock_2.prefab"),
            ShipBlockade(sea, "ship-cargo-b", $"{folder}/BlockadeBlock_3.prefab"),
            ShipBlockade(sea, "ship-cargo-c", $"{folder}/BlockadeBlock_4.prefab"),
            ShipBlockade(sea, "boat-tug-a", $"{folder}/BlockadeBlock_5.prefab"),
        };

        var walls = new List<GameObject>();
        string[] containers = { "cargo-container-a", "cargo-container-b", "cargo-container-c" };
        for (int i = 0; i < containers.Length; i++)
        {
            string lower = containers[i];
            string upper = containers[(i + 1) % containers.Length];
            walls.Add(DressingPrefab($"{dressing}/Wall_containers-{(char)('a' + i)}.prefab", root =>
            {
                StackedContainer(root.transform, sea, lower, 0f);
                StackedContainer(root.transform, sea, upper, BLOCKADE_HEIGHT * 0.5f);
            }));
        }

        var props = new List<BackdropProp>
        {
            Prop(dressing, factory, "warning-orange", 0.8f, 0f, 7f, 2.5f),
            Prop(dressing, factory, "warning-traffic", 1f, 0f, 7f, 2f),
            Prop(dressing, factory, "cone", 0.7f, 0f, 7f, 2f),
            Prop(dressing, factory, "box-small", 0.6f, 0f, 7f, 2f),
            Prop(dressing, factory, "box-large", 1.4f, 0f, 8f, 2.5f),
            Prop(dressing, factory, "box-long", 0.8f, 0f, 8f, 2f),
            Prop(dressing, factory, "box-wide", 0.9f, 0f, 8f, 2f),
            Prop(dressing, factory, "cog-a", 1.2f, 0.3f, 7f, 0.8f),
            Prop(dressing, sea, "cargo-pile-a", 1.6f, 0.3f, 9f, 2f),
            Prop(dressing, sea, "cargo-pile-b", 1.8f, 0.3f, 9f, 2f),
            Prop(dressing, sea, "cargo-container-c", 2.2f, 0f, 10f, 3f, false),
            Prop(dressing, sea, "cargo-container-a", 2.2f, 0f, 10f, 2.5f, false),
            Prop(dressing, factory, "structure-yellow-tall", 6.4f, 0f, 4f, 2f, false),
            Prop(dressing, factory, "structure-yellow-high", 5f, 1f, 9f, 1.5f, false),
            Prop(dressing, factory, "structure-yellow-medium", 4f, 1f, 9f, 1.5f, false),
            Prop(dressing, factory, "structure-yellow-short", 2.5f, 0.5f, 9f, 1.5f, false),
            Prop(dressing, factory, "structure-wall", 4f, 1f, 11f, 1.2f, false),
            Prop(dressing, factory, "structure-window", 4f, 1f, 11f, 1.2f, false),
            Prop(dressing, factory, "pipe-large-long", 1.1f, 0.3f, 9f, 1.5f, false),
            Prop(dressing, factory, "pipe-large-bend", 1.6f, 0.3f, 9f, 1.2f),
            Prop(dressing, factory, "pipe-large-valve", 1.6f, 0.3f, 9f, 1f),
            Prop(dressing, factory, "conveyor-long", 1.2f, 0.5f, 9f, 1.2f, false),
            Prop(dressing, factory, "catwalk-straight", 1.4f, 0.5f, 9f, 1f, false),
            Prop(dressing, factory, "machine", 3.5f, 2f, 13f, 1.2f, false),
            Prop(dressing, factory, "machine-bed", 2.5f, 2f, 13f, 1f, false),
            Prop(dressing, factory, "scanner-high", 3.5f, 2f, 13f, 0.8f, false),
            Prop(dressing, factory, "hopper-round", 5f, 3f, 16f, 1f, false),
            Prop(dressing, factory, "hopper-square", 5f, 3f, 16f, 1f, false),
            Prop(dressing, factory, "structure-high", 6f, 8f, 26f, 1f, false),
            Prop(dressing, factory, "structure-tall", 8f, 8f, 26f, 1f, false),
            Prop(dressing, factory, "crane", 10f, 5f, 22f, 1f, false),
            Prop(dressing, factory, "crane-lift", 9f, 5f, 22f, 0.8f, false),
            Prop(dressing, factory, "crane-magnet", 9f, 5f, 22f, 0.6f, false),
            ShipProp(dressing, sea, "buoy", 1.2f, 2f, 14f, 1f, -0.4f),
            ShipProp(dressing, sea, "buoy-flag", 2.2f, 2f, 14f, 1f, -0.4f),
            ShipProp(dressing, sea, "boat-tug-b", 4f, 8f, 26f, 0.8f, -0.6f),
            ShipProp(dressing, sea, "boat-house-b", 4f, 8f, 26f, 0.6f, -0.6f),
            ShipProp(dressing, sea, "ship-cargo-a", 9f, 12f, 34f, 0.9f, -1f),
            ShipProp(dressing, sea, "ship-cargo-b", 9f, 12f, 34f, 0.9f, -1f),
            ShipProp(dressing, sea, "ship-cargo-c", 9f, 12f, 34f, 0.8f, -1f),
            ShipProp(dressing, sea, "ship-ocean-liner-small", 11f, 14f, 36f, 0.6f, -1.2f),
        };

        if (AssetDatabase.LoadAssetAtPath<GameObject>(ModelLibrary.CRANE_GLB_PATH) != null)
        {
            GameObject polyCrane = DressingPrefab($"{dressing}/Prop_crane-polypizza.prefab", root =>
            {
                GameObject crane = ModelLibrary.SpawnAsset(ModelLibrary.CRANE_GLB_PATH, root.transform);
                ModelLibrary.FitHeight(crane, 11f);
                PlaceOnFloor(crane, root.transform);
            });
            props.Add(MakeProp(polyCrane, 8f, 22f, 0.5f, false, 0f, 0.9f, 1.1f));
        }

        SaveAppearance(PORT_APPEARANCE_PATH, template, jumpPrefab, slidePrefab, rampPrefab, blockadePrefab, variants, walls, props);
    }

    private static void StackedContainer(Transform parent, KenneyKit kit, string model, float y)
    {
        GameObject container = ModelLibrary.Spawn(kit, model, parent);
        ModelLibrary.OrientLongAxisAlongX(container);
        ModelLibrary.FitHeight(container, BLOCKADE_HEIGHT * 0.5f);
        NarrowTo(container, BLOCKADE_VISUAL_WIDTH);
        PlaceOnFloor(container, parent, new Vector3(0f, y, 0f));
    }

    private static void BuildDeckSlide(Transform parent, KenneyKit kit, string model)
    {
        GameObject deck = ModelLibrary.Spawn(kit, model, parent);
        ModelLibrary.FitWidth(deck, OBSTACLE_WIDTH + 0.2f);
        ModelLibrary.ScaleHeightTo(deck, SLIDE_CLEARANCE + SLIDE_DECK_THICKNESS);
        PlaceOnFloor(deck, parent);
    }

    private static void BuildBoardRamp(Transform parent, KenneyKit kit, string model)
    {
        GameObject board = ModelLibrary.Spawn(kit, model, parent);
        ModelLibrary.OrientLongAxisAlongZ(board);
        ModelLibrary.FitWidth(board, OBSTACLE_WIDTH);

        float thickness = ModelLibrary.Size(board).y;
        if (thickness < RAMP_MIN_THICKNESS)
        {
            throw new InvalidOperationException($"Ramp model '{model}' is a flat sheet ({thickness:0.000} thick); pick a model with volume");
        }

        if (thickness > RAMP_BOARD_THICKNESS) ModelLibrary.ScaleHeightTo(board, RAMP_BOARD_THICKNESS);
        ModelLibrary.StretchDepth(board, SlopeLength());
        board.transform.Rotate(Vector3.right, -SlopeAngle(), Space.World);
        PlaceOnFloor(board, parent);
    }

    private static BlockadeVariant ShipBlockade(KenneyKit kit, string model, string path)
    {
        GameObject prefab = Blockade(path, tile =>
        {
            GameObject ship = ModelLibrary.Spawn(kit, model, tile);
            ModelLibrary.DeactivateChildrenNamed(ship, SailNodePrefixes);
            MastTrimmer.TrimAboveDeck(ship, $"{kit}-{model}-hull");
            ModelLibrary.OrientLongAxisAlongZ(ship);
            ModelLibrary.FitWidth(ship, BLOCKADE_VISUAL_WIDTH);

            float deckHeight = ModelLibrary.Size(ship).y;
            if (deckHeight > BLOCKADE_HEIGHT) ModelLibrary.FitHeight(ship, BLOCKADE_HEIGHT);
            else if (deckHeight < MIN_BLOCKADE_HEIGHT) ModelLibrary.ScaleHeightTo(ship, MIN_BLOCKADE_HEIGHT);

            PlaceOnFloor(ship, tile);
        });

        return new BlockadeVariant { Prefab = prefab, MinLength = SHIP_MIN_LENGTH };
    }

    private static GameObject Blockade(string path, Action<Transform> fillTile)
    {
        GameObject root = NewRoot(Path.GetFileNameWithoutExtension(path));

        var tile = new GameObject("Tile");
        tile.transform.SetParent(root.transform, false);
        fillTile(tile.transform);

        FinalizeBlockade(root, tile.transform);
        return SavePrefab(root, path);
    }

    private static void FinalizeBlockade(GameObject root, Transform tile)
    {
        Bounds bounds = VisualBounds(root);
        float width = LANE_PITCH;
        float depth = Mathf.Max(0.3f, bounds.size.z);
        float height = Mathf.Clamp(bounds.max.y - FLOOR_Y, MIN_BLOCKADE_HEIGHT, BLOCKADE_HEIGHT);
        float centerY = FLOOR_Y + height * 0.5f;

        UiFactory.SetLayerRecursive(root.transform, OBSTACLE_LAYER);

        GameObject body = AddBox(root, "Body", new Vector3(0f, centerY, 0f), new Vector3(width, height, depth), false, null);
        GameObject front = AddBox(root, "FrontWall", new Vector3(0f, centerY, -(depth + TRIGGER_THICKNESS) * 0.5f),
            new Vector3(width, height, TRIGGER_THICKNESS), true, typeof(FrontWallCollisionDetector));

        GameObject left = AddBox(root, "LeftWall", new Vector3(-(width + TRIGGER_THICKNESS) * 0.5f, centerY, 0f),
            new Vector3(TRIGGER_THICKNESS, height, depth), true, typeof(SideWallCollisionDetector));
        left.GetComponent<SideWallCollisionDetector>().IsOnRight = false;

        GameObject right = AddBox(root, "RightWall", new Vector3((width + TRIGGER_THICKNESS) * 0.5f, centerY, 0f),
            new Vector3(TRIGGER_THICKNESS, height, depth), true, typeof(SideWallCollisionDetector));
        right.GetComponent<SideWallCollisionDetector>().IsOnRight = true;

        var fitter = root.AddComponent<BlockadeFitter>();
        var so = new SerializedObject(fitter);
        so.FindProperty("_tile").objectReferenceValue = tile;
        so.FindProperty("_tileDepth").floatValue = depth;
        so.FindProperty("_frontWall").objectReferenceValue = front.GetComponent<BoxCollider>();

        SerializedProperty colliders = so.FindProperty("_depthColliders");
        GameObject[] resized = { body, left, right };
        colliders.arraySize = resized.Length;
        for (int i = 0; i < resized.Length; i++)
        {
            colliders.GetArrayElementAtIndex(i).objectReferenceValue = resized[i].GetComponent<BoxCollider>();
        }

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void FinalizeJump(GameObject root)
    {
        Bounds bounds = VisualBounds(root);
        UiFactory.SetLayerRecursive(root.transform, BAD_OBSTACLE_LAYER);
        AddBox(root, "Body", bounds.center, bounds.size, false, null);
        AddFrontWall(root, bounds.min.y, bounds.max.y, bounds.size.x, bounds.min.z);
        AddSideWalls(root, bounds.min.x, bounds.max.x, bounds.min.y, bounds.max.y, bounds.center.z, bounds.size.z);
    }

    private static void FinalizeSlide(GameObject root)
    {
        Bounds bounds = VisualBounds(root);
        UiFactory.SetLayerRecursive(root.transform, BAD_OBSTACLE_LAYER);
        float barBottom = FLOOR_Y + SLIDE_CLEARANCE;
        AddBox(root, "Body", new Vector3(bounds.center.x, (barBottom + bounds.max.y) * 0.5f, bounds.center.z),
            new Vector3(bounds.size.x, bounds.max.y - barBottom, bounds.size.z), false, null);
        AddFrontWall(root, barBottom, bounds.max.y, bounds.size.x, bounds.min.z);
        AddSideWalls(root, bounds.min.x, bounds.max.x, barBottom, bounds.max.y, bounds.center.z, bounds.size.z);
    }

    private static void FinalizeRamp(GameObject root)
    {
        UiFactory.SetLayerRecursive(root.transform, OBSTACLE_LAYER);
        float slopeLength = SlopeLength();
        Quaternion tilt = Quaternion.Euler(-SlopeAngle(), 0f, 0f);
        var slopeCenter = new Vector3(0f, FLOOR_Y + RAMP_RISE * 0.5f, 0f);

        GameObject slope = AddBox(root, "Slope", slopeCenter, new Vector3(OBSTACLE_WIDTH, RAMP_BOARD_THICKNESS, slopeLength), false, null);
        slope.transform.localRotation = tilt;
        slope.transform.localPosition = slopeCenter - tilt * new Vector3(0f, RAMP_BOARD_THICKNESS * 0.5f, 0f);

        foreach (float side in new[] { -1f, 1f })
        {
            Vector3 offset = tilt * new Vector3(side * (OBSTACLE_WIDTH * 0.5f + TRIGGER_THICKNESS * 0.5f), SIDE_WALL_HEIGHT * 0.5f, 0f);
            GameObject wall = AddBox(root, side > 0f ? "RightWall" : "LeftWall", slopeCenter + offset,
                new Vector3(TRIGGER_THICKNESS, SIDE_WALL_HEIGHT, slopeLength), true, typeof(SideWallCollisionDetector));
            wall.transform.localRotation = tilt;
            wall.GetComponent<SideWallCollisionDetector>().IsOnRight = side > 0f;
        }
    }

    private static void AddFrontWall(GameObject root, float bottomY, float topY, float width, float frontZ)
    {
        var center = new Vector3(0f, (bottomY + topY) * 0.5f, frontZ - TRIGGER_THICKNESS * 0.5f);
        AddBox(root, "FrontWall", center, new Vector3(width, topY - bottomY, TRIGGER_THICKNESS), true, typeof(FrontWallCollisionDetector));
    }

    private static void AddSideWalls(GameObject root, float minX, float maxX, float bottomY, float topY, float centerZ, float depth)
    {
        float centerY = (bottomY + topY) * 0.5f;
        float height = topY - bottomY;

        GameObject left = AddBox(root, "LeftWall", new Vector3(minX - TRIGGER_THICKNESS * 0.5f, centerY, centerZ),
            new Vector3(TRIGGER_THICKNESS, height, depth), true, typeof(SideWallCollisionDetector));
        left.GetComponent<SideWallCollisionDetector>().IsOnRight = false;

        GameObject right = AddBox(root, "RightWall", new Vector3(maxX + TRIGGER_THICKNESS * 0.5f, centerY, centerZ),
            new Vector3(TRIGGER_THICKNESS, height, depth), true, typeof(SideWallCollisionDetector));
        right.GetComponent<SideWallCollisionDetector>().IsOnRight = true;
    }

    private static GameObject AddBox(GameObject root, string name, Vector3 localCenter, Vector3 size, bool isTrigger, Type detector)
    {
        var go = new GameObject(name);
        go.layer = root.layer;
        go.transform.SetParent(root.transform, false);
        go.transform.localPosition = localCenter;

        var box = go.AddComponent<BoxCollider>();
        box.size = size;
        box.isTrigger = isTrigger;
        if (detector != null) go.AddComponent(detector);
        return go;
    }

    private static Bounds VisualBounds(GameObject root)
    {
        if (!ModelLibrary.TryGetBounds(root, out Bounds bounds))
        {
            throw new InvalidOperationException($"{root.name} has no visible renderers");
        }

        return bounds;
    }

    private static GameObject DressingPrefab(string path, Action<GameObject> fill)
    {
        GameObject root = NewRoot(Path.GetFileNameWithoutExtension(path));
        fill(root);
        UiFactory.SetLayerRecursive(root.transform, 0);
        return SavePrefab(root, path);
    }

    private static void Fit(KenneyKit kit, string model, GameObject root, float height)
    {
        GameObject instance = ModelLibrary.Spawn(kit, model, root.transform);
        ModelLibrary.FitHeight(instance, height);
        PlaceOnFloor(instance, root.transform);
    }

    private static void FitShip(KenneyKit kit, string model, GameObject root, float height)
    {
        GameObject instance = ModelLibrary.Spawn(kit, model, root.transform);
        ModelLibrary.OrientLongAxisAlongZ(instance);
        ModelLibrary.FitHeight(instance, height);
        PlaceOnFloor(instance, root.transform);
    }

    private static BackdropProp Prop(string dressing, KenneyKit kit, string model, float height, float minDistance, float maxDistance,
        float weight, bool randomYaw = true, float yOffset = 0f)
    {
        GameObject prefab = DressingPrefab($"{dressing}/Prop_{model}.prefab", root => Fit(kit, model, root, height));
        return MakeProp(prefab, minDistance, maxDistance, weight, randomYaw, yOffset, 0.8f, 1.25f);
    }

    private static BackdropProp ShipProp(string dressing, KenneyKit kit, string model, float height, float minDistance, float maxDistance,
        float weight, float yOffset)
    {
        GameObject prefab = DressingPrefab($"{dressing}/Prop_{model}.prefab", root => FitShip(kit, model, root, height));
        return MakeProp(prefab, minDistance, maxDistance, weight, false, yOffset, 0.9f, 1.15f);
    }

    private static BackdropProp MakeProp(GameObject prefab, float minDistance, float maxDistance, float weight, bool randomYaw,
        float yOffset, float scaleMin, float scaleMax)
    {
        return new BackdropProp
        {
            Prefab = prefab,
            MinDistance = minDistance,
            MaxDistance = maxDistance,
            Weight = weight,
            RandomYaw = randomYaw,
            YOffset = yOffset,
            ScaleMin = scaleMin,
            ScaleMax = scaleMax
        };
    }

    private static void SaveAppearance(string path, TrackApperenceSO template, GameObject jump, GameObject slide, GameObject ramp,
        GameObject blockade, List<BlockadeVariant> variants, List<GameObject> walls, List<BackdropProp> props)
    {
        EnsureFolder(APPEARANCE_FOLDER);
        var look = AssetDatabase.LoadAssetAtPath<TrackApperenceSO>(path);
        bool isNew = look == null;
        if (isNew) look = ScriptableObject.CreateInstance<TrackApperenceSO>();

        look.JumpPrefab = jump;
        look.SlidePrefab = slide;
        look.RampPrefab = ramp;
        look.BlockadePrefab = blockade;
        look.BlockadeVariants = variants.ToArray();
        look.WallSegmentPrefabs = walls.ToArray();
        look.WallOffset = WALL_OFFSET;
        look.BackdropProps = props.ToArray();
        look.BackdropPasses = BACKDROP_PASSES;
        look.BackdropSpacingMin = BACKDROP_SPACING_MIN;
        look.BackdropSpacingMax = BACKDROP_SPACING_MAX;

        if (template != null)
        {
            look.CoinPrefab = template.CoinPrefab;
            look.CoinMagnetPrefab = template.CoinMagnetPrefab;
            look.SuperJumpPrefab = template.SuperJumpPrefab;
            look.DoublePointsPrefab = template.DoublePointsPrefab;
        }

        if (isNew) AssetDatabase.CreateAsset(look, path);
        EditorUtility.SetDirty(look);
        Debug.Log($"{nameof(ObstacleSetBuilder)}: appearance saved to {path} ({props.Count} props, {walls.Count} wall pieces)");
    }

    private static TrackApperenceSO LoadTemplate()
    {
        foreach (string folder in new[] { APPEARANCE_FOLDER, LEGACY_APPEARANCE_FOLDER })
        {
            var template = AssetDatabase.LoadAssetAtPath<TrackApperenceSO>($"{folder}/{TEMPLATE_APPEARANCE_NAME}.asset");
            if (template != null) return template;
        }

        Debug.LogWarning($"{nameof(ObstacleSetBuilder)}: template appearance {TEMPLATE_APPEARANCE_NAME} not found, coins and power ups stay unassigned");
        return null;
    }

    private static void RetireLegacyAppearances()
    {
        EnsureFolder(LEGACY_APPEARANCE_FOLDER);
        foreach (string guid in AssetDatabase.FindAssets("t:TrackApperenceSO", new[] { APPEARANCE_FOLDER }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path == PIRATE_APPEARANCE_PATH || path == PORT_APPEARANCE_PATH) continue;

            string target = $"{LEGACY_APPEARANCE_FOLDER}/{Path.GetFileName(path)}";
            string error = AssetDatabase.MoveAsset(path, target);
            if (!string.IsNullOrEmpty(error)) Debug.LogWarning($"{nameof(ObstacleSetBuilder)}: could not move {path}: {error}");
        }
    }

    private static void WireDefaultWalls()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(TRACK_PART_PREFAB_PATH);
        try
        {
            TrackInitiator initiator = root.GetComponent<TrackInitiator>();
            if (initiator == null) return;

            var renderers = new List<Renderer>();
            foreach (Transform child in root.transform)
            {
                if (child.name.StartsWith("Wall", StringComparison.Ordinal) && child.TryGetComponent(out Renderer renderer)) renderers.Add(renderer);
            }

            var so = new SerializedObject(initiator);
            SerializedProperty walls = so.FindProperty("_defaultWallRenderers");
            walls.arraySize = renderers.Count;
            for (int i = 0; i < renderers.Count; i++) walls.GetArrayElementAtIndex(i).objectReferenceValue = renderers[i];
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, TRACK_PART_PREFAB_PATH);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    public static float SlopeAngle() => Mathf.Atan2(RAMP_RISE, RAMP_RUN) * Mathf.Rad2Deg;

    public static float SlopeLength() => Mathf.Sqrt(RAMP_RUN * RAMP_RUN + RAMP_RISE * RAMP_RISE);

    private static void NarrowTo(GameObject root, float maxWidth)
    {
        Vector3 size = ModelLibrary.Size(root);
        if (size.x <= maxWidth || size.x <= 0.0001f) return;

        Vector3 scale = root.transform.localScale;
        root.transform.localScale = new Vector3(scale.x * (maxWidth / size.x), scale.y, scale.z);
    }

    private static GameObject NewRoot(string name)
    {
        var root = new GameObject(name);
        root.transform.position = Vector3.zero;
        return root;
    }

    private static GameObject SpawnFitted(KenneyKit kit, string model, Transform parent, float maxWidth, float maxHeight, float maxDepth)
    {
        GameObject instance = ModelLibrary.Spawn(kit, model, parent);
        ModelLibrary.FitWithin(instance, maxWidth, maxHeight, maxDepth);
        return instance;
    }

    private static void PlaceOnFloor(GameObject instance, Transform parent) => PlaceOnFloor(instance, parent, Vector3.zero);

    private static void PlaceOnFloor(GameObject instance, Transform parent, Vector3 localOffset)
    {
        ModelLibrary.PlaceBottomCenter(instance, parent.TransformPoint(new Vector3(0f, FLOOR_Y, 0f) + localOffset));
    }

    private static GameObject SavePrefab(GameObject root, string path)
    {
        EnsureFolder(Path.GetDirectoryName(path)?.Replace('\\', '/'));
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static void EnsureFolder(string path)
    {
        if (string.IsNullOrEmpty(path) || AssetDatabase.IsValidFolder(path)) return;

        string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        if (!string.IsNullOrEmpty(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
