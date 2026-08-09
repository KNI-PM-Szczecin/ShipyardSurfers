using System.Collections.Generic;
using UnityEngine;

public class TrackInitiator : MonoBehaviour
{
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

        float cellDepth = (zFarLocal - zNearLocal) / ObsticleSetSO.Rows;   // teraz LOKALNIE, spójnie

        PlaceObstacles(set, look, cellDepth, zNearLocal);
    }

    private void PlaceObstacles(ObsticleSetSO set, TrackApperenceSO look, float cellDepth, float zNearLocal)
    {
        int cols = Mathf.Min(ObsticleSetSO.Columns, LaneCenters.Count);

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
            }
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