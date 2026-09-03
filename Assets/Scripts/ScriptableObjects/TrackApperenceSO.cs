using UnityEngine;

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
    public GameObject RampPrefab;
    [Space]
    [Header("Pickups")]
    public GameObject CoinPrefab;
    [Space]
    [Header("Power ups")]
    public GameObject CoinMagnetPrefab;
    public GameObject SuperJumpPrefab;
    public GameObject DoublePointsPrefab;

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
                return BlockadePrefab;
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
}
