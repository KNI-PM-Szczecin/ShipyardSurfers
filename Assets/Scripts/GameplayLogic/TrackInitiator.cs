using System.Collections.Generic;
using UnityEngine;

public class TrackInitiator : MonoBehaviour
{
    private const int CoinsPerCell = 3;
    private const float CoinBaseYOffset = 1f;
    private const float CoinSurfaceYOffset = 0.5f;

    public List<Transform> LaneCenters;
    public Renderer TrackRenderer;

    private void Start() => BuildTrack();

    public void BuildTrack()
    {
        print("building track");

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

        float zNearLocal = transform.InverseTransformPoint(
            new Vector3(0, 0, TrackRenderer.bounds.min.z)).z;
        float zFarLocal = transform.InverseTransformPoint(
            new Vector3(0, 0, TrackRenderer.bounds.max.z)).z;

        float cellDepth = (zFarLocal - zNearLocal) / ObsticleSetSO.Rows; 

        PlaceObstacles(set, look, cellDepth, zNearLocal);
    }

    private void PlaceObstacles(ObsticleSetSO set, TrackApperenceSO look, float cellDepth, float zNearLocal)
    {
        int cols = Mathf.Min(ObsticleSetSO.Columns, LaneCenters.Count);

        float[,] coinYStart = new float[cols, ObsticleSetSO.Rows];
        float[,] coinYEnd = new float[cols, ObsticleSetSO.Rows];
        float[,] coinYArc = new float[cols, ObsticleSetSO.Rows];
        for (int x = 0; x < cols; x++)
            for (int y = 0; y < ObsticleSetSO.Rows; y++)
                coinYStart[x, y] = coinYEnd[x, y] = float.NaN;

        for (int y = 0; y < ObsticleSetSO.Rows; y++)
            for (int x = 0; x < cols; x++)
            {
                ObstacleCell cell = set.GetCell(x, y);
                if (cell.Type == ObsticleType.Empty) continue;

                GameObject prefab = look.GetObsticlePrefab(cell.Type);
                if (prefab == null || LaneCenters[x] == null) continue;

                Vector3 laneLocal = transform.InverseTransformPoint(LaneCenters[x].position);
                float zLocal = zNearLocal + (y + 0.5f) * cellDepth;

                GameObject go = Instantiate(prefab, transform);

                if (cell.Type == ObsticleType.Blockade && cell.BlockadeLength > 1)
                {
                    int len = cell.BlockadeLength;
                    zLocal = zNearLocal + (y + len * 0.5f) * cellDepth;
                    StretchToDepth(go, len * cellDepth);
                }
                else if (cell.Type == ObsticleType.Blockade)
                {
                    StretchToDepth(go, cellDepth);
                }

                go.transform.localPosition = new Vector3(laneLocal.x, laneLocal.y + 1, zLocal);

                float topY = GetTopLocalY(go) + CoinSurfaceYOffset;

                switch (cell.Type)
                {
                    case ObsticleType.Blockade:
                        for (int l = 0; l < cell.BlockadeLength && y + l < ObsticleSetSO.Rows; l++)
                        {
                            coinYStart[x, y + l] = topY;
                            coinYEnd[x, y + l] = topY;
                        }
                        break;

                    case ObsticleType.Jump:
                        // łuk nad barierką: start i koniec przy ziemi, szczyt nad przeszkodą
                        float jumpBaseY = laneLocal.y + CoinBaseYOffset;
                        coinYStart[x, y] = jumpBaseY;
                        coinYEnd[x, y] = jumpBaseY;
                        coinYArc[x, y] = Mathf.Max(0f, topY - jumpBaseY);
                        break;

                    case ObsticleType.Ramp:
                        coinYStart[x, y] = laneLocal.y + CoinBaseYOffset;
                        coinYEnd[x, y] = topY;
                        break;
                }
            }

        PlaceCoins(set, look, cols, cellDepth, zNearLocal, coinYStart, coinYEnd, coinYArc);
    }

    private void PlaceCoins(ObsticleSetSO set, TrackApperenceSO look, int cols, float cellDepth, float zNearLocal, float[,] coinYStart, float[,] coinYEnd, float[,] coinYArc)
    {
        if (look.CoinPrefab == null) return;

        GameObject powerUpPrefab = null;
        if (PowerUpSpawnRoller.TryRoll(CountCoins(set, cols), out int powerUpIndex, out PowerUpType type))
        {
            powerUpPrefab = look.GetPowerUpPrefab(type);
        }

        int coinIndex = 0;

        for (int y = 0; y < ObsticleSetSO.Rows; y++)
            for (int x = 0; x < cols; x++)
            {
                if (!set.GetCell(x, y).HasCoin || LaneCenters[x] == null) continue;

                Vector3 laneLocal = transform.InverseTransformPoint(LaneCenters[x].position);
                float baseY = laneLocal.y + CoinBaseYOffset;

                float yStart = float.IsNaN(coinYStart[x, y]) ? baseY : coinYStart[x, y];
                float yEnd = float.IsNaN(coinYEnd[x, y]) ? baseY : coinYEnd[x, y];

                for (int i = 0; i < CoinsPerCell; i++)
                {
                    float t = (i + 0.5f) / CoinsPerCell;
                    float yLocal = Mathf.Lerp(yStart, yEnd, t) + coinYArc[x, y] * 4f * t * (1f - t);

                    bool isPowerUp = powerUpPrefab != null && coinIndex == powerUpIndex;
                    coinIndex++;

                    GameObject coin = Instantiate(isPowerUp ? powerUpPrefab : look.CoinPrefab, transform);
                    coin.transform.localPosition = new Vector3(
                        laneLocal.x,
                        yLocal,
                        zNearLocal + (y + t) * cellDepth);
                }
            }
    }

    private int CountCoins(ObsticleSetSO set, int cols)
    {
        int count = 0;

        for (int y = 0; y < ObsticleSetSO.Rows; y++)
            for (int x = 0; x < cols; x++)
            {
                if (set.GetCell(x, y).HasCoin && LaneCenters[x] != null) count += CoinsPerCell;
            }

        return count;
    }

    private float GetTopLocalY(GameObject go)
    {
        var r = go.GetComponentInChildren<Renderer>();
        if (r == null) return go.transform.localPosition.y;

        Bounds b = r.bounds;
        return transform.InverseTransformPoint(new Vector3(b.center.x, b.max.y, b.center.z)).y;
    }

    private void StretchToDepth(GameObject go, float targetDepth)
    {
        var r = go.GetComponentInChildren<Renderer>();
        if (r == null || targetDepth <= 0f) return;

        float current = r.bounds.size.z;
        if (current <= 0.0001f) return;

        Vector3 s = go.transform.localScale;
        go.transform.localScale = new Vector3(s.x, s.y, s.z * (targetDepth / current));
    }
}