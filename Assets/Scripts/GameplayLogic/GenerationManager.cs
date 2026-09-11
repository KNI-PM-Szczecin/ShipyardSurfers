using System.Collections.Generic;
using UnityEngine;

public class GenerationManager
{
    private static GenerationManager _instance;

    public static GenerationManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new GenerationManager();
            }
            return _instance;
        }
    }

    public const string OBSTICLE_SETS_FOLDER = "ObsticleSets";
    public const string TRACK_APPERENCE_FOLDER = "TrackApperences";

    private List<ObsticleSetSO> _obsticleSets;
    private List<TrackApperenceSO> _trackApperences;
    private int _lastObsticleIndex = -1;
    private int _lastApperenceIndex = -1;

    public ISegmentContentOverride ContentOverride { get; set; }

    public bool IsShipped(ObsticleSetSO set) => _obsticleSets.Contains(set);

    public bool IsShipped(TrackApperenceSO look) => _trackApperences.Contains(look);

    public GenerationManager()
    {
        _obsticleSets = new List<ObsticleSetSO>(Resources.LoadAll<ObsticleSetSO>(OBSTICLE_SETS_FOLDER));
        _trackApperences = new List<TrackApperenceSO>(Resources.LoadAll<TrackApperenceSO>(TRACK_APPERENCE_FOLDER));
    }

    private T GetRandomFromList<T>(List<T> list, ref int lastIndex) where T : class
    {
        if (list == null || list.Count == 0) return null;
        if (list.Count == 1) return list[0];

        bool hasPrevious = lastIndex >= 0;
        int randomIndex = Random.Range(0, hasPrevious ? list.Count - 1 : list.Count);

        if (hasPrevious && randomIndex >= lastIndex)
        {
            randomIndex++;
        }

        lastIndex = randomIndex;
        return list[randomIndex];
    }

    public SegmentContent NextSegmentContent()
    {
        var randomPick = new SegmentContent(
            GetRandomFromList(_obsticleSets, ref _lastObsticleIndex),
            GetRandomFromList(_trackApperences, ref _lastApperenceIndex));

        return ContentOverride != null ? ContentOverride.Apply(randomPick) : randomPick;
    }
}
