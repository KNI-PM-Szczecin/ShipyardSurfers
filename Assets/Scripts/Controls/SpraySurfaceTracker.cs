using UnityEngine;

[RequireComponent(typeof(Animator))]
public class SpraySurfaceTracker : MonoBehaviour
{
    public const string SURFACE_PARAMETER = "Surface";
    public const float WATER = 0f;
    public const float SOLID = 1f;

    [SerializeField, Tooltip("Emitters live under this anchor; it is kept at the height of whatever the boat rides on, even while the player crouches")]
    private Transform _anchor;

    [SerializeField, Tooltip("World-space emitters whose drops must travel with the moving track")]
    private ParticleSystem[] _emitters;

    [SerializeField, Tooltip("Layers the boat can ride on: the track floor plus obstacle roofs and ramps")]
    private LayerMask _surfaceMask = (1 << 0) | (1 << 6) | (1 << 8);

    [SerializeField, Tooltip("Layer of the water floor; anything else the boat rides on throws sparks instead of spray")]
    private int _waterLayer;

    [SerializeField] private float _probeStartHeight = 2f;
    [SerializeField] private float _probeLength = 6f;
    [SerializeField] private float _anchorLift = 0.03f;

    private Animator _animator;
    private RideSurfaceProbe _probe;
    private Transform _player;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _player = transform.parent != null ? transform.parent : transform;
        _probe = new RideSurfaceProbe(_surfaceMask.value, _waterLayer, _probeStartHeight, _probeLength);
    }

    private void LateUpdate()
    {
        float trackVelocity = -SegmentMover.MoveSpeed;
        foreach (ParticleSystem emitter in _emitters)
        {
            if (emitter == null) continue;

            ParticleSystem.VelocityOverLifetimeModule velocity = emitter.velocityOverLifetime;
            velocity.z = trackVelocity;
        }

        if (_anchor == null || !_probe.TryProbe(_player.position, out RideSurface surface)) return;

        Vector3 position = _anchor.position;
        _anchor.position = new Vector3(position.x, surface.Height + _anchorLift, position.z);
        _animator.SetFloat(SURFACE_PARAMETER, surface.IsWater ? WATER : SOLID);
    }
}
