using System.Collections.Generic;
using UnityEngine;

public class TrackSpawner : MonoBehaviour
{
    private const int MAX_SPAWNS_PER_FRAME = 8;

    public List<Transform> LaneCenters;
    [SerializeField] private GameObject _segmentPrefab;
    [SerializeField, Tooltip("Segment already placed in the scene that the generated track continues from.")]
    private TrackInitiator _firstSegment;
    [SerializeField, Min(1), Tooltip("Generated track always reaches at least this many segment lengths past this object.")]
    private int _segmentsAhead = 2;

    private TrackInitiator _lastSegment;

    private void Start()
    {
        _lastSegment = _firstSegment != null ? _firstSegment : FindFirstObjectByType<TrackInitiator>();
        FillAhead();
    }

    private void Update() => FillAhead();

    private void FillAhead()
    {
        if (_segmentPrefab == null) return;

        for (int i = 0; i < MAX_SPAWNS_PER_FRAME && NeedsSegment(); i++)
        {
            SpawnNext();
        }
    }

    private bool NeedsSegment()
    {
        if (!HasTrackEnd()) return true;

        Bounds bounds = _lastSegment.TrackRenderer.bounds;
        return bounds.max.z < transform.position.z + _segmentsAhead * bounds.size.z;
    }

    private void SpawnNext()
    {
        float startZ = HasTrackEnd() ? _lastSegment.TrackRenderer.bounds.max.z : transform.position.z;

        GameObject segment = Instantiate(_segmentPrefab);
        var initiator = segment.GetComponent<TrackInitiator>();
        initiator.LaneCenters = LaneCenters;

        float nearZ = initiator.TrackRenderer.bounds.min.z;
        segment.transform.position += Vector3.forward * (startZ - nearZ);

        _lastSegment = initiator;
    }

    private bool HasTrackEnd() => _lastSegment != null && _lastSegment.TrackRenderer != null;
}
