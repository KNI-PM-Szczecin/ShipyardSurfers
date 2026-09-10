using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrackDestroyer : MonoBehaviour
{
    private const int SCAN_INTERVAL = 15;
    private const int PIECES_PER_STEP = 32;

    private readonly HashSet<TrackInitiator> _removing = new HashSet<TrackInitiator>();

    private void Update()
    {
        if (Time.frameCount % SCAN_INTERVAL != 0) return;

        foreach (TrackInitiator segment in FindObjectsByType<TrackInitiator>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (_removing.Contains(segment) || !IsFullyBehind(segment)) continue;

            _removing.Add(segment);
            StartCoroutine(RemoveSegment(segment));
        }
    }

    private bool IsFullyBehind(TrackInitiator segment) =>
        segment.TrackRenderer != null && segment.TrackRenderer.bounds.max.z < transform.position.z;

    private IEnumerator RemoveSegment(TrackInitiator segment)
    {
        Transform root = segment.transform;
        int removed = 0;

        for (int partIndex = root.childCount - 1; partIndex >= 0; partIndex--)
        {
            Transform part = root.GetChild(partIndex);

            for (int pieceIndex = part.childCount - 1; pieceIndex >= 0; pieceIndex--)
            {
                Destroy(part.GetChild(pieceIndex).gameObject);
                removed++;

                if (removed % PIECES_PER_STEP == 0) yield return null;
            }

            Destroy(part.gameObject);
            removed++;

            if (removed % PIECES_PER_STEP == 0) yield return null;
        }

        _removing.Remove(segment);
        Destroy(root.gameObject);
    }
}
