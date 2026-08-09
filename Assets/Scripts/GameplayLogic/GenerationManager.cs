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

    private const string OBSTICLE_SETS_FOLDER = "ObsticleSets";
    private const string TRACK_APPERENCE_FOLDER = "TrackApperences";

    private List<ObsticleSetSO> _obsticleSets;
    private List<TrackApperenceSO> _trackApperences;
    private int _lastObsticleIndex = -1;
    private int _lastApperenceIndex = -1;

    public GenerationManager()
    {
        _obsticleSets = new List<ObsticleSetSO>(Resources.LoadAll<ObsticleSetSO>(OBSTICLE_SETS_FOLDER));
        _trackApperences = new List<TrackApperenceSO>(Resources.LoadAll<TrackApperenceSO>(TRACK_APPERENCE_FOLDER));
    }

    private T GetRandomFromList<T>(List<T> list, ref int lastIndex) where T : class
    {
        if (list == null || list.Count == 0) return null;
        if (list.Count == 1) return list[0];

        int count = list.Count;
        int randomIndex = Random.Range(0, count - 1);

        if (randomIndex >= lastIndex && lastIndex != -1)
        {
            randomIndex++;
        }

        lastIndex = randomIndex;
        return list[randomIndex];
    }

    public ObsticleSetSO GetRandomObsticles() => GetRandomFromList(_obsticleSets, ref _lastObsticleIndex);

    public TrackApperenceSO GetRandomApperence() => GetRandomFromList(_trackApperences, ref _lastApperenceIndex);

}
