using System.Collections.Generic;
using UnityEngine;

public class TrackSpawner : MonoBehaviour
{
    public List<Transform> LaneCenters;
    [SerializeField] private GameObject _segmentPrefab;

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Segment")) return;

        TrackInitiator currentInit = other.GetComponentInParent<TrackInitiator>();
        if (currentInit == null) return;
        float currentFarZ = currentInit.TrackRenderer.bounds.max.z;

        GameObject seg = Instantiate(_segmentPrefab, new Vector3(0, -1, 0), Quaternion.identity);

        var init = seg.GetComponent<TrackInitiator>();
        if (init != null) init.LaneCenters = LaneCenters;
        float newNearZ = init.TrackRenderer.bounds.min.z;

        Vector3 p = seg.transform.position;
        seg.transform.position = new Vector3(p.x, p.y, p.z + (currentFarZ - newNearZ));
    }
}