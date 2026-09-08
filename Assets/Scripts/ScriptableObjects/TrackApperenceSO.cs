using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct BlockadeVariant
{
    public GameObject Prefab;

    [Tooltip("Smallest blockade length (in cells) this look is allowed for; long models like ships should require 2+")]
    [Range(1, ObsticleSetSO.MAX_BLOCKADE_LENGTH)]
    public int MinLength;
}

[System.Serializable]
public struct BackdropProp
{
    public GameObject Prefab;

    [Tooltip("Distance from the outer face of the side wall to the prop, picked at random between min and max")]
    public float MinDistance;
    public float MaxDistance;

    [Tooltip("Relative chance of being picked")]
    [Min(0f)] public float Weight;

    [Tooltip("Rotate around Y by a random angle (rocks, plants); ships and buildings keep their orientation")]
    public bool RandomYaw;

    [Tooltip("Vertical offset from the floor, negative sinks the prop (hulls in water)")]
    public float YOffset;

    [Tooltip("Random uniform scale applied on top of the prefab size")]
    public float ScaleMin;
    public float ScaleMax;
}

[CreateAssetMenu(menuName = "ScriptableObjects/TrackApperenceSO", order = 2)]
public class TrackApperenceSO : ScriptableObject
{
    [Header("Track")]
    public GameObject Track;
    [Space]
    [Header("Obsticles")]
    public GameObject JumpPrefab;
    public GameObject SlidePrefab;
    public GameObject BlockadePrefab;
    [Tooltip("Optional extra blockade looks picked at random per blockade; BlockadePrefab is used when none fits the length")]
    public BlockadeVariant[] BlockadeVariants;
    public GameObject RampPrefab;
    [Space]
    [Header("Pickups")]
    public GameObject CoinPrefab;
    [Space]
    [Header("Power ups")]
    public GameObject CoinMagnetPrefab;
    public GameObject SuperJumpPrefab;
    public GameObject DoublePointsPrefab;
    [Space]
    [Header("Dressing")]
    [Tooltip("Visual wall pieces placed back to back along both sides of the track; the default cube walls are hidden when any are set")]
    public GameObject[] WallSegmentPrefabs;
    [Tooltip("Distance from the track center to the inner face of the wall segments")]
    public float WallOffset = 5.5f;
    public BackdropProp[] BackdropProps;
    [Tooltip("How many times the whole backdrop is scattered along each side; more passes means denser surroundings")]
    [Range(1, 8)] public int BackdropPasses = 3;
    [Tooltip("Gap along the track between consecutive backdrop props within one pass")]
    public float BackdropSpacingMin = 3f;
    public float BackdropSpacingMax = 6f;

    private readonly List<GameObject> _candidates = new List<GameObject>();

    public bool HasWallSegments => WallSegmentPrefabs != null && WallSegmentPrefabs.Length > 0;

    public GameObject GetPowerUpPrefab(PowerUpType type)
    {
        switch (type)
        {
            case PowerUpType.CoinMagnet:
                return CoinMagnetPrefab;
            case PowerUpType.SuperJump:
                return SuperJumpPrefab;
            case PowerUpType.DoublePoints:
                return DoublePointsPrefab;
            default:
                Debug.LogError("Unknown PowerUpType used in GetPowerUpPrefab!");
                return null;
        }
    }

    public GameObject GetObsticlePrefab(ObsticleType Type)
    {
        switch (Type)
        {
            case ObsticleType.Blockade:
                return GetBlockadePrefab(1);
            case ObsticleType.Ramp:
                return RampPrefab;
            case ObsticleType.Slide:
                return SlidePrefab;
            case ObsticleType.Jump:
                return JumpPrefab;
            default:
                Debug.LogError("Unknown ObstacleType used in GetObsticlePrefab!");
                return null;
        }
    }

    public GameObject GetBlockadePrefab(int length)
    {
        _candidates.Clear();
        if (BlockadePrefab != null) _candidates.Add(BlockadePrefab);

        if (BlockadeVariants != null)
        {
            foreach (BlockadeVariant variant in BlockadeVariants)
            {
                if (variant.Prefab != null && variant.MinLength <= length) _candidates.Add(variant.Prefab);
            }
        }

        return _candidates.Count > 0 ? _candidates[Random.Range(0, _candidates.Count)] : null;
    }
}
