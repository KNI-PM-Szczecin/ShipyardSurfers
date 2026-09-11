using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

public class ObstacleSetDebugger : MonoBehaviour, ISegmentContentOverride
{
    [SerializeField, Tooltip("Every new segment takes the next layout from this list, in order; leave empty to keep the game's random pick")]
    private ObsticleSetSO[] _obstacleSets;

    [SerializeField, Tooltip("Every new segment takes the next appearance from this list, in order; leave empty to keep the game's random pick")]
    private TrackApperenceSO[] _appearances;

    private int _nextObstacleSet;
    private int _nextAppearance;
    private int _segments;

    public bool ForcesObstacleSets => HasAny(_obstacleSets);
    public bool ForcesAppearances => HasAny(_appearances);

    public bool Contains(ObsticleSetSO set) => _obstacleSets != null && Array.IndexOf(_obstacleSets, set) >= 0;
    public bool Contains(TrackApperenceSO look) => _appearances != null && Array.IndexOf(_appearances, look) >= 0;

    public void Configure(ObsticleSetSO[] obstacleSets, TrackApperenceSO[] appearances)
    {
        _obstacleSets = obstacleSets;
        _appearances = appearances;
        _nextObstacleSet = 0;
        _nextAppearance = 0;
    }

    private void OnEnable()
    {
        if (!Debug.isDebugBuild)
        {
            Debug.LogWarning($"{nameof(ObstacleSetDebugger)}: ignored outside the editor and development builds", this);
            return;
        }

        GenerationManager.Instance.ContentOverride = this;
        Debug.LogWarning($"{nameof(ObstacleSetDebugger)}: active, layouts [{Names(_obstacleSets)}], appearances [{Names(_appearances)}]", this);
        WarnAboutUnshippedAssets();
    }

    private void WarnAboutUnshippedAssets()
    {
        GenerationManager manager = GenerationManager.Instance;

        foreach (ObsticleSetSO set in _obstacleSets ?? Array.Empty<ObsticleSetSO>())
        {
            if (set != null && !manager.IsShipped(set)) WarnUnshipped(set, GenerationManager.OBSTICLE_SETS_FOLDER);
        }

        foreach (TrackApperenceSO look in _appearances ?? Array.Empty<TrackApperenceSO>())
        {
            if (look != null && !manager.IsShipped(look)) WarnUnshipped(look, GenerationManager.TRACK_APPERENCE_FOLDER);
        }
    }

    private void WarnUnshipped(Object asset, string folder)
    {
        Debug.LogWarning($"{nameof(ObstacleSetDebugger)}: '{asset.name}' is not in Resources/{folder}, so the game never picks it; " +
                         "legacy assets may reference prefabs that predate the current track pipeline", asset);
    }

    private void OnDisable()
    {
        if (GenerationManager.Instance.ContentOverride == this) GenerationManager.Instance.ContentOverride = null;
    }

    public SegmentContent Apply(SegmentContent randomPick)
    {
        SegmentContent content = randomPick;
        if (ForcesObstacleSets) content = content.WithObsticles(Next(_obstacleSets, ref _nextObstacleSet));
        if (ForcesAppearances) content = content.WithApperence(Next(_appearances, ref _nextAppearance));

        _segments++;
        Debug.Log($"{nameof(ObstacleSetDebugger)}: segment {_segments} -> layout '{Name(content.Obsticles)}' ({Source(ForcesObstacleSets)}), " +
                  $"appearance '{Name(content.Apperence)}' ({Source(ForcesAppearances)})", this);

        return content;
    }

    private static T Next<T>(T[] items, ref int cursor) where T : Object
    {
        for (int i = 0; i < items.Length; i++)
        {
            T item = items[cursor];
            cursor = (cursor + 1) % items.Length;
            if (item != null) return item;
        }

        return null;
    }

    private static bool HasAny<T>(T[] items) where T : Object
    {
        if (items == null) return false;

        foreach (T item in items)
        {
            if (item != null) return true;
        }

        return false;
    }

    private static string Names<T>(T[] items) where T : Object
    {
        if (!HasAny(items)) return "random";

        var names = new List<string>();
        foreach (T item in items)
        {
            if (item != null) names.Add(item.name);
        }

        return string.Join(", ", names);
    }

    private static string Name(Object asset) => asset != null ? asset.name : "none";

    private static string Source(bool forced) => forced ? "forced" : "random";
}
