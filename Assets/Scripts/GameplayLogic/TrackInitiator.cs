using System.Collections.Generic;
using UnityEngine;

public class TrackInitiator : MonoBehaviour
{
    private const int COINS_PER_CELL = 3;
    private const float COIN_BASE_Y_OFFSET = 1f;
    private const float COIN_SURFACE_Y_OFFSET = 0.5f;
    private const float OBSTACLE_PIVOT_Y_OFFSET = 1f;
    private const float RAMP_OVERLAP = 0.15f;

    public List<Transform> LaneCenters;
    public Renderer TrackRenderer;

    private void Start() => BuildTrack();

    public void BuildTrack()
    {
        var set = GenerationManager.Instance.GetRandomObsticles();
        var look = GenerationManager.Instance.GetRandomApperence();

        if (set == null || look == null)
        {
            Debug.LogError("WARNING: Null obsticle set or track apperence", this);
            return;
        }
        if (LaneCenters == null || LaneCenters.Count == 0)
        {
            Debug.LogError("WARNING: track indicators not defined", this);
            return;
        }

        if (TrackRenderer == null) TrackRenderer = GetComponentInChildren<Renderer>();

        float zNearLocal = ToLocalZ(TrackRenderer.bounds.min.z);
        float zFarLocal = ToLocalZ(TrackRenderer.bounds.max.z);
        float cellDepth = (zFarLocal - zNearLocal) / ObsticleSetSO.ROWS;
        int cols = Mathf.Min(ObsticleSetSO.COLUMNS, LaneCenters.Count);

        CoinPath[,] coinPaths = PlaceObstacles(set, look, cols, cellDepth, zNearLocal);
        PlaceCoins(set, look, cols, cellDepth, zNearLocal, coinPaths);
    }

    private CoinPath[,] PlaceObstacles(ObsticleSetSO set, TrackApperenceSO look, int cols, float cellDepth, float zNearLocal)
    {
        CoinPath[,] coinPaths = CreateGroundCoinPaths(cols);

        for (int y = 0; y < ObsticleSetSO.ROWS; y++)
            for (int x = 0; x < cols; x++)
            {
                ObstacleCell cell = set.GetCell(x, y);
                if (cell.Type == ObsticleType.Empty) continue;

                GameObject prefab = look.GetObsticlePrefab(cell.Type);
                if (prefab == null || LaneCenters[x] == null) continue;

                Vector3 laneLocal = transform.InverseTransformPoint(LaneCenters[x].position);
                float cellNearZ = zNearLocal + y * cellDepth;

                GameObject obstacle = Instantiate(prefab, transform);
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

                RegisterCoinPath(coinPaths, cell, x, y, obstacle, laneLocal.y, cellNearZ, cellDepth);
            }

        return coinPaths;
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
        StretchToDepth(blockade, depth);

        Vector3 position = blockade.transform.localPosition;
        blockade.transform.localPosition = new Vector3(position.x, position.y, cellNearZ + depth * 0.5f);

        if (ObsticleSetAnalyzer.ContinuesPreviousObstacle(set, x, y))
        {
            DisableFrontWalls(blockade);
        }
    }

    private void AlignRampToCellEnd(GameObject ramp, float cellFarZ)
    {
        if (!RendererBoundsUtility.TryGetVisibleBounds(ramp, out Bounds bounds)) return;

        float rampFarZ = ToLocalZ(bounds.max.z);
        ramp.transform.localPosition += new Vector3(0f, 0f, cellFarZ + RAMP_OVERLAP - rampFarZ);
    }

    private void RegisterCoinPath(CoinPath[,] coinPaths, ObstacleCell cell, int x, int y, GameObject obstacle, float laneY, float cellNearZ, float cellDepth)
    {
        float groundY = laneY + COIN_BASE_Y_OFFSET;
        float topY = GetTopLocalY(obstacle) + COIN_SURFACE_Y_OFFSET;

        switch (cell.Type)
        {
            case ObsticleType.Blockade:
                for (int l = 0; l < cell.BlockadeLength && y + l < ObsticleSetSO.ROWS; l++)
                {
                    coinPaths[x, y + l] = CoinPath.Flat(topY);
                }
                break;

            case ObsticleType.Jump:
                coinPaths[x, y] = CoinPath.Arc(groundY, topY);
                break;

            case ObsticleType.Ramp:
                coinPaths[x, y] = CoinPath.Slope(groundY, topY, GetRampStartT(obstacle, cellNearZ, cellDepth), 1f);
                break;
        }
    }

    private float GetRampStartT(GameObject ramp, float cellNearZ, float cellDepth)
    {
        if (!RendererBoundsUtility.TryGetVisibleBounds(ramp, out Bounds bounds)) return 0f;

        return Mathf.Clamp01((ToLocalZ(bounds.min.z) - cellNearZ) / cellDepth);
    }

    private void PlaceCoins(ObsticleSetSO set, TrackApperenceSO look, int cols, float cellDepth, float zNearLocal, CoinPath[,] coinPaths)
    {
        if (look.CoinPrefab == null) return;

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

                    GameObject coin = Instantiate(isPowerUp ? powerUpPrefab : look.CoinPrefab, transform);
                    coin.transform.localPosition = new Vector3(
                        laneLocal.x,
                        path.EvaluateY(t),
                        zNearLocal + (y + path.EvaluateT(t)) * cellDepth);
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
        if (!RendererBoundsUtility.TryGetVisibleBounds(obstacle, out Bounds bounds)) return obstacle.transform.localPosition.y;

        return transform.InverseTransformPoint(new Vector3(bounds.center.x, bounds.max.y, bounds.center.z)).y;
    }

    private static void StretchToDepth(GameObject obstacle, float targetDepth)
    {
        if (targetDepth <= 0f || !RendererBoundsUtility.TryGetVisibleBounds(obstacle, out Bounds bounds)) return;

        float current = bounds.size.z;
        if (current <= 0.0001f) return;

        Vector3 scale = obstacle.transform.localScale;
        obstacle.transform.localScale = new Vector3(scale.x, scale.y, scale.z * (targetDepth / current));
    }
}
