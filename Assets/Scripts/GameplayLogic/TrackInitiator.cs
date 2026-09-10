using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrackInitiator : MonoBehaviour
{
    private const int PIECES_PER_STEP = 16;
    private const int COINS_PER_CELL = 3;
    private const float COIN_BASE_Y_OFFSET = 1f;
    private const float COIN_SURFACE_Y_OFFSET = 0.5f;
    private const float COIN_UNDER_Y_OFFSET = 0.85f;
    private const float OBSTACLE_PIVOT_Y_OFFSET = 1f;
    private const float RAMP_OVERLAP = 0.15f;
    private const float DEFAULT_TRACK_SPEED = 15f;

    public List<Transform> LaneCenters;
    public Renderer TrackRenderer;

    [SerializeField, Tooltip("Plain wall renderers hidden when the picked appearance brings its own wall segments")]
    private Renderer[] _defaultWallRenderers;

    private readonly TrackDresser _dresser = new TrackDresser();

    private void Start() => StartCoroutine(BuildTrackRoutine());

    public void BuildTrack()
    {
        IEnumerator steps = BuildTrackRoutine();
        while (steps.MoveNext()) { }
    }

    private IEnumerator BuildTrackRoutine()
    {
        var set = GenerationManager.Instance.GetRandomObsticles();
        var look = GenerationManager.Instance.GetRandomApperence();

        if (set == null || look == null)
        {
            Debug.LogError("WARNING: Null obsticle set or track apperence", this);
            yield break;
        }
        if (LaneCenters == null || LaneCenters.Count == 0)
        {
            Debug.LogError("WARNING: track indicators not defined", this);
            yield break;
        }

        if (TrackRenderer == null) TrackRenderer = GetComponentInChildren<Renderer>();

        float zNearLocal = ToLocalZ(TrackRenderer.bounds.min.z);
        float zFarLocal = ToLocalZ(TrackRenderer.bounds.max.z);
        float cellDepth = (zFarLocal - zNearLocal) / ObsticleSetSO.ROWS;
        int cols = Mathf.Min(ObsticleSetSO.COLUMNS, LaneCenters.Count);

        CoinPath[,] coinPaths = CreateGroundCoinPaths(cols);

        foreach (object step in PlaceObstacles(set, look, cols, cellDepth, zNearLocal, coinPaths)) yield return step;
        foreach (object step in PlaceCoins(set, look, cols, cellDepth, zNearLocal, coinPaths)) yield return step;
        foreach (object step in DressTrack(look, zNearLocal, zFarLocal)) yield return step;
    }

    private IEnumerable DressTrack(TrackApperenceSO look, float zNearLocal, float zFarLocal)
    {
        HideDefaultWalls(look);

        float floorLocalY = transform.InverseTransformPoint(new Vector3(0f, TrackRenderer.bounds.max.y, 0f)).y;

        foreach (object step in _dresser.DressSteps(transform, look, floorLocalY, zNearLocal, zFarLocal)) yield return step;
    }

    private void HideDefaultWalls(TrackApperenceSO look)
    {
        if (!look.HasWallSegments || _defaultWallRenderers == null) return;

        foreach (Renderer wall in _defaultWallRenderers)
        {
            if (wall != null) wall.enabled = false;
        }
    }

    private IEnumerable PlaceObstacles(ObsticleSetSO set, TrackApperenceSO look, int cols, float cellDepth, float zNearLocal,
        CoinPath[,] coinPaths)
    {
        var claimed = new bool[cols, ObsticleSetSO.ROWS];
        var obstacles = new GameObject[cols, ObsticleSetSO.ROWS];
        int placed = 0;

        for (int y = 0; y < ObsticleSetSO.ROWS; y++)
            for (int x = 0; x < cols; x++)
            {
                ObstacleCell cell = set.GetCell(x, y);
                if (cell.Type == ObsticleType.Empty) continue;

                GameObject prefab = cell.Type == ObsticleType.Blockade
                    ? look.GetBlockadePrefab(cell.BlockadeLength)
                    : look.GetObsticlePrefab(cell.Type);
                if (prefab == null || LaneCenters[x] == null) continue;

                Vector3 laneLocal = transform.InverseTransformPoint(LaneCenters[x].position);
                float cellNearZ = zNearLocal + y * cellDepth;

                GameObject obstacle = Instantiate(prefab, transform);
                obstacles[x, y] = obstacle;
                obstacle.transform.localPosition = new Vector3(laneLocal.x, laneLocal.y + OBSTACLE_PIVOT_Y_OFFSET, cellNearZ + cellDepth * 0.5f);

                switch (cell.Type)
                {
                    case ObsticleType.Blockade:
                        PlaceBlockade(obstacle, set, cell, x, y, cellNearZ, cellDepth);
                        break;

                    case ObsticleType.Ramp:
                        AlignRampToCellEnd(obstacle, cellNearZ + cellDepth);
                        break;
                }

                RegisterCoinPath(coinPaths, claimed, cell, x, y, obstacle, laneLocal.y, cellNearZ, cellDepth, zNearLocal);

                placed++;
                if (placed % PIECES_PER_STEP == 0) yield return null;
            }

        OpenJoinedSideWalls(set, obstacles, cols);
    }

    private static void OpenJoinedSideWalls(ObsticleSetSO set, GameObject[,] obstacles, int cols)
    {
        for (int y = 0; y < ObsticleSetSO.ROWS; y++)
            for (int x = 1; x < cols; x++)
            {
                if (obstacles[x - 1, y] == null || obstacles[x, y] == null) continue;
                if (!ObsticleSetAnalyzer.ContinuesLeftObstacle(set, x, y)) continue;

                DisableSideWall(obstacles[x - 1, y], true);
                DisableSideWall(obstacles[x, y], false);
            }
    }

    private static void DisableSideWall(GameObject obstacle, bool onRight)
    {
        foreach (SideWallCollisionDetector wall in obstacle.GetComponentsInChildren<SideWallCollisionDetector>())
        {
            if (wall.IsOnRight == onRight) wall.gameObject.SetActive(false);
        }
    }

    private CoinPath[,] CreateGroundCoinPaths(int cols)
    {
        CoinPath[,] coinPaths = new CoinPath[cols, ObsticleSetSO.ROWS];

        for (int x = 0; x < cols; x++)
        {
            if (LaneCenters[x] == null) continue;

            float groundY = transform.InverseTransformPoint(LaneCenters[x].position).y + COIN_BASE_Y_OFFSET;

            for (int y = 0; y < ObsticleSetSO.ROWS; y++)
            {
                coinPaths[x, y] = CoinPath.Flat(groundY);
            }
        }

        return coinPaths;
    }

    private void PlaceBlockade(GameObject blockade, ObsticleSetSO set, ObstacleCell cell, int x, int y, float cellNearZ, float cellDepth)
    {
        float depth = cell.BlockadeLength * cellDepth;

        if (blockade.TryGetComponent(out BlockadeFitter fitter)) fitter.Fit(depth);
        else StretchToDepth(blockade, depth);

        Vector3 position = blockade.transform.localPosition;
        blockade.transform.localPosition = new Vector3(position.x, position.y, cellNearZ + depth * 0.5f);

        if (ObsticleSetAnalyzer.ContinuesPreviousObstacle(set, x, y))
        {
            DisableFrontWalls(blockade);
        }
    }

    private void AlignRampToCellEnd(GameObject ramp, float cellFarZ)
    {
        if (!ObstacleBoundsUtility.TryGetGameplayBounds(ramp, out Bounds bounds)) return;

        float rampFarZ = ToLocalZ(bounds.max.z);
        ramp.transform.localPosition += new Vector3(0f, 0f, cellFarZ + RAMP_OVERLAP - rampFarZ);
    }

    private void RegisterCoinPath(CoinPath[,] coinPaths, bool[,] claimed, ObstacleCell cell, int x, int y, GameObject obstacle,
        float laneY, float cellNearZ, float cellDepth, float zNearLocal)
    {
        float groundY = laneY + COIN_BASE_Y_OFFSET;

        switch (cell.Type)
        {
            case ObsticleType.Blockade:
                RegisterBlockadeTop(coinPaths, claimed, cell, x, y, obstacle);
                break;

            case ObsticleType.Jump:
                RegisterJumpArc(coinPaths, claimed, x, y, obstacle, groundY, cellDepth, zNearLocal);
                break;

            case ObsticleType.Slide:
                coinPaths[x, y] = CoinPath.Flat(GetBottomLocalY(obstacle) - COIN_UNDER_Y_OFFSET);
                claimed[x, y] = true;
                break;

            case ObsticleType.Ramp:
                RegisterRampSlope(coinPaths, claimed, x, y, obstacle, groundY, cellNearZ, cellDepth);
                break;
        }
    }

    private void RegisterBlockadeTop(CoinPath[,] coinPaths, bool[,] claimed, ObstacleCell cell, int x, int y, GameObject obstacle)
    {
        CoinPath path = CoinPath.Flat(GetTopLocalY(obstacle) + COIN_SURFACE_Y_OFFSET);

        for (int length = 0; length < cell.BlockadeLength && y + length < ObsticleSetSO.ROWS; length++)
        {
            coinPaths[x, y + length] = path;
            claimed[x, y + length] = true;
        }
    }

    private void RegisterRampSlope(CoinPath[,] coinPaths, bool[,] claimed, int x, int y, GameObject obstacle, float groundY,
        float cellNearZ, float cellDepth)
    {
        claimed[x, y] = true;
        if (!ObstacleBoundsUtility.TryGetGameplayBounds(obstacle, out Bounds bounds)) return;

        float startZ = ToLocalZ(bounds.min.z);
        float startT = Mathf.Clamp01((startZ - cellNearZ) / cellDepth);
        coinPaths[x, y] = CoinPath.Slope(startZ, groundY, ToLocalZ(bounds.max.z), GetTopLocalY(obstacle) + COIN_SURFACE_Y_OFFSET, startT);
    }

    private void RegisterJumpArc(CoinPath[,] coinPaths, bool[,] claimed, int x, int y, GameObject obstacle, float groundY,
        float cellDepth, float zNearLocal)
    {
        claimed[x, y] = true;
        if (!ObstacleBoundsUtility.TryGetGameplayBounds(obstacle, out Bounds bounds)) return;

        JumpArc arc = MovmentController.Arc;
        float speed = TrackSpeed();
        float peakZ = ToLocalZ(bounds.min.z);
        float startZ = peakZ - arc.RiseDistance(speed);
        float endZ = peakZ + arc.FallDistance(speed);

        CoinPath path = CoinPath.Jump(startZ, groundY, peakZ, groundY + arc.Height, endZ);
        int firstRow = Mathf.Max(0, Mathf.FloorToInt((startZ - zNearLocal) / cellDepth));
        int lastRow = Mathf.Min(ObsticleSetSO.ROWS - 1, Mathf.FloorToInt((endZ - zNearLocal) / cellDepth));

        for (int row = firstRow; row <= lastRow; row++)
        {
            if (row != y && claimed[x, row]) continue;

            coinPaths[x, row] = path;
        }
    }

    private static float TrackSpeed()
    {
        if (SegmentMover.MoveSpeed > 0.01f) return SegmentMover.MoveSpeed;

        return GameManager.instance != null ? GameManager.instance.StartGameSpeed : DEFAULT_TRACK_SPEED;
    }

    private IEnumerable PlaceCoins(ObsticleSetSO set, TrackApperenceSO look, int cols, float cellDepth, float zNearLocal,
        CoinPath[,] coinPaths)
    {
        if (look.CoinPrefab == null) yield break;

        GameObject powerUpPrefab = null;
        if (PowerUpSpawnRoller.TryRoll(CountCoins(set, cols), out int powerUpIndex, out PowerUpType type))
        {
            powerUpPrefab = look.GetPowerUpPrefab(type);
        }

        int coinIndex = 0;

        for (int y = 0; y < ObsticleSetSO.ROWS; y++)
            for (int x = 0; x < cols; x++)
            {
                if (!set.GetCell(x, y).HasCoin || LaneCenters[x] == null) continue;

                Vector3 laneLocal = transform.InverseTransformPoint(LaneCenters[x].position);
                CoinPath path = coinPaths[x, y];

                for (int i = 0; i < COINS_PER_CELL; i++)
                {
                    float t = (i + 0.5f) / COINS_PER_CELL;

                    bool isPowerUp = powerUpPrefab != null && coinIndex == powerUpIndex;
                    coinIndex++;

                    float coinZ = zNearLocal + (y + path.EvaluateT(t)) * cellDepth;

                    GameObject coin = Instantiate(isPowerUp ? powerUpPrefab : look.CoinPrefab, transform);
                    coin.transform.localPosition = new Vector3(laneLocal.x, path.EvaluateY(coinZ), coinZ);

                    if (coinIndex % PIECES_PER_STEP == 0) yield return null;
                }
            }
    }

    private int CountCoins(ObsticleSetSO set, int cols)
    {
        int count = 0;

        for (int y = 0; y < ObsticleSetSO.ROWS; y++)
            for (int x = 0; x < cols; x++)
            {
                if (set.GetCell(x, y).HasCoin && LaneCenters[x] != null) count += COINS_PER_CELL;
            }

        return count;
    }

    private static void DisableFrontWalls(GameObject obstacle)
    {
        foreach (FrontWallCollisionDetector wall in obstacle.GetComponentsInChildren<FrontWallCollisionDetector>())
        {
            wall.gameObject.SetActive(false);
        }
    }

    private float ToLocalZ(float worldZ) => transform.InverseTransformPoint(new Vector3(0f, 0f, worldZ)).z;

    private float GetTopLocalY(GameObject obstacle)
    {
        if (!ObstacleBoundsUtility.TryGetGameplayBounds(obstacle, out Bounds bounds)) return obstacle.transform.localPosition.y;

        return transform.InverseTransformPoint(new Vector3(bounds.center.x, bounds.max.y, bounds.center.z)).y;
    }

    private float GetBottomLocalY(GameObject obstacle)
    {
        if (!ObstacleBoundsUtility.TryGetGameplayBounds(obstacle, out Bounds bounds)) return obstacle.transform.localPosition.y;

        return transform.InverseTransformPoint(new Vector3(bounds.center.x, bounds.min.y, bounds.center.z)).y;
    }

    private static void StretchToDepth(GameObject obstacle, float targetDepth)
    {
        if (targetDepth <= 0f || !ObstacleBoundsUtility.TryGetGameplayBounds(obstacle, out Bounds bounds)) return;

        float current = bounds.size.z;
        if (current <= 0.0001f) return;

        Vector3 scale = obstacle.transform.localScale;
        obstacle.transform.localScale = new Vector3(scale.x, scale.y, scale.z * (targetDepth / current));
    }
}
