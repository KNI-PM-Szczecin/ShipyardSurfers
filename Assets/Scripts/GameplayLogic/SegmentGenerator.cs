using System.Collections.Generic;
using UnityEngine;

public class SegmentGenerator : MonoBehaviour
{
    public List<Transform> LaneCenters;
    [SerializeField] private GameObject _segmentPrefab;
    [SerializeField] private Renderer _trackRenderer;

    private bool _spawned;

    private void OnTriggerExit(Collider other)
    {
        if (_spawned || !other.CompareTag("Player")) return;
        _spawned = true;

        float currentFarZ = _trackRenderer.bounds.max.z;

        GameObject seg = Instantiate(_segmentPrefab, new Vector3(0, -1, 0), Quaternion.identity);

        var init = seg.GetComponent<TrackInitiator>();
        if (init != null) init.LaneCenters = LaneCenters;

        var nextGen = seg.GetComponentInChildren<SegmentGenerator>();
        if (nextGen != null) nextGen.LaneCenters = LaneCenters;

        float newNearZ = nextGen != null ? nextGen._trackRenderer.bounds.min.z
                                         : seg.GetComponentInChildren<Renderer>().bounds.min.z;

        Vector3 p = seg.transform.position;
        seg.transform.position = new Vector3(p.x, p.y, p.z + (currentFarZ - newNearZ));
    }
}