using UnityEngine;

[RequireComponent(typeof(MovmentController))]
public class CoinMagnetEffect : PowerUpEffect
{
    private const int MAX_COINS_IN_RANGE = 32;

    [SerializeField] private LayerMask _coinMask = 1 << 9;

    private readonly Collider[] _coinsInRange = new Collider[MAX_COINS_IN_RANGE];
    private MovmentController _movement;
    private Vector3 _halfExtents;

    public override PowerUpType Type => PowerUpType.CoinMagnet;

    private void Awake()
    {
        _movement = GetComponent<MovmentController>();
    }

    private void Start()
    {
        float cellRange = _movement.LaneWidth * PowerUpSettings.MAGNET_RANGE_IN_CELLS + PowerUpSettings.MAGNET_RANGE_MARGIN;
        _halfExtents = new Vector3(cellRange, PowerUpSettings.MAGNET_VERTICAL_RANGE, cellRange);
    }

    private void FixedUpdate()
    {
        if (!IsActive) return;

        int count = Physics.OverlapBoxNonAlloc(transform.position, _halfExtents, _coinsInRange,
            Quaternion.identity, _coinMask, QueryTriggerInteraction.Collide);

        float pullSpeed = Mathf.Max(PowerUpSettings.MAGNET_MIN_PULL_SPEED,
            SegmentMover.MoveSpeed * PowerUpSettings.MAGNET_PULL_SPEED_FACTOR);
        float step = pullSpeed * Time.fixedDeltaTime;

        for (int i = 0; i < count; i++)
        {
            if (!_coinsInRange[i].TryGetComponent(out Coin coin)) continue;

            Vector3 coinPosition = coin.transform.position;
            if (Vector3.Distance(coinPosition, transform.position) <= PowerUpSettings.MAGNET_COLLECT_DISTANCE)
            {
                coin.Collect();
                continue;
            }

            coin.transform.position = Vector3.MoveTowards(coinPosition, transform.position, step);
        }
    }
}
