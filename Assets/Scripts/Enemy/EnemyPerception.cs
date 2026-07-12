using System;
using UnityEngine;

public enum StimulusKind
{
    Sight,
    Sound
}

public enum StimulusTier
{
    Faint,
    Moderate,
    Strong
}

public readonly struct Stimulus
{
    public readonly StimulusKind Kind;
    public readonly StimulusTier Tier;
    public readonly Vector3 Position;
    public readonly bool FromPlayer;

    public Stimulus(StimulusKind kind, StimulusTier tier, Vector3 position, bool fromPlayer)
    {
        Kind = kind;
        Tier = tier;
        Position = position;
        FromPlayer = fromPlayer;
    }
}

public class EnemyPerception : MonoBehaviour
{
    [Header("Sight")]
    [SerializeField] private float _fov = 45f;
    [SerializeField] private float _verticalSightHeight = 2f;
    [SerializeField] private float _litEngageDistance = 15f;
    [SerializeField] private float _darkEngageDistance = 2f;
    [SerializeField] private float _litInvestigationDistance = 25f;
    [SerializeField] private float _darkInvestigationDistance = 5f;
    [SerializeField] private float _sightSuspicionRatio = 1.5f;

    [Header("Hearing")]
    [SerializeField] private LayerMask _occlusionMask;
    [SerializeField] private float _hardSurfaceAttenuation = 0.5f;
    [SerializeField] [Range(0f, 1f)] private float _noiseEngageRatio = 0.35f;
    [SerializeField] private float _noiseSuspicionRatio = 1.3f;

    [Header("Debug")]
    [SerializeField] private bool _isBlind;
    [SerializeField] private bool _isDeaf;

    public event Action<Stimulus> OnStimulus;

    private float _engageDistanceScale = 1f;

    public Transform Player { get; private set; }
    public float MaxEngageDistance => _litEngageDistance * _engageDistanceScale;

    public void SetEngageDistanceScale(float scale)
    {
        _engageDistanceScale = Mathf.Max(0.01f, scale);
    }

    private bool _gizmoNoiseActive;
    private bool _gizmoNoiseHeard;
    private Vector3 _gizmoNoisePosition;

    private float Visibility => PlayerVisibility.Instance ? PlayerVisibility.Instance.VisibilityValue : 1f;
    private float CurrentEngageDistance => Mathf.Lerp(_darkEngageDistance, _litEngageDistance, Visibility);

    private void OnEnable()
    {
        NoiseEmitter.OnNoise += HandleNoise;
    }

    private void OnDisable()
    {
        NoiseEmitter.OnNoise -= HandleNoise;
    }

    private void Start()
    {
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj)
        {
            Player = playerObj.transform;
        }
    }

    private void Update()
    {
        if (!Player)
        {
            return;
        }
        if (GameManager.Instance && GameManager.Instance.HasDied)
        {
            return;
        }

        TrySightStimulus();
    }

    private void TrySightStimulus()
    {
        if (_isBlind)
        {
            return;
        }
        
        var visibility = Visibility;
        var effectiveEngageDistance = Mathf.Lerp(_darkEngageDistance, _litEngageDistance, visibility) * _engageDistanceScale;
        var effectiveInvestigationDistance = Mathf.Lerp(_darkInvestigationDistance, _litInvestigationDistance, visibility) * _engageDistanceScale;
        var effectiveSuspicionDistance = effectiveInvestigationDistance * _sightSuspicionRatio;
        var maxRange = Mathf.Max(effectiveEngageDistance, effectiveSuspicionDistance);

        var distance = Vector3.Distance(transform.position, Player.position);
        if (distance > maxRange)
        {
            return;
        }

        var verticalDiff = Player.position.y - transform.position.y;
        if (Mathf.Abs(verticalDiff) > _verticalSightHeight)
        {
            return;
        }

        var enemyToPlayer = Player.position - transform.position;
        var flatDirection = new Vector3(enemyToPlayer.x, 0f, enemyToPlayer.z);
        var flatForward = new Vector3(transform.forward.x, 0f, transform.forward.z);
        if (Vector3.Angle(flatDirection, flatForward) > _fov)
        {
            return;
        }

        var eyeOrigin = transform.position + Vector3.up;
        var rayDirection = Player.position - eyeOrigin;
        if (!Physics.Raycast(eyeOrigin, rayDirection, out var hit, maxRange))
        {
            return;
        }

        if (!hit.transform.CompareTag("Player"))
        {
            return;
        }

        StimulusTier tier;
        if (distance <= effectiveEngageDistance)
        {
            tier = StimulusTier.Strong;
        }
        else if (distance <= effectiveInvestigationDistance)
        {
            tier = StimulusTier.Moderate;
        }
        else
        {
            tier = StimulusTier.Faint;
        }

        OnStimulus?.Invoke(new Stimulus(StimulusKind.Sight, tier, Player.position, true));
    }

    private void HandleNoise(Vector3 position, float radius, string sourceTag)
    {
        if (_isDeaf)
        {
            return;
        }

        var direction = position - transform.position;
        if (direction.magnitude > radius * _noiseSuspicionRatio)
        {
            return;
        }

        var effectiveRadius = radius;
        if (Physics.Raycast(transform.position, direction.normalized, out var hit, direction.magnitude, _occlusionMask))
        {
            if (hit.collider.CompareTag("Stone") ||
                hit.collider.CompareTag("Wood") ||
                hit.collider.CompareTag("Grass") ||
                hit.collider.CompareTag("Metal") ||
                hit.collider.CompareTag("Gravel"))
            {
                effectiveRadius *= _hardSurfaceAttenuation;
            }
        }

        var heard = direction.magnitude <= effectiveRadius;
        _gizmoNoiseActive = true;
        _gizmoNoiseHeard = heard;
        _gizmoNoisePosition = position;

        var fromPlayer = sourceTag != "Decoy";

        if (heard)
        {
            var tier = direction.magnitude <= effectiveRadius * _noiseEngageRatio
                ? StimulusTier.Strong
                : StimulusTier.Moderate;
            OnStimulus?.Invoke(new Stimulus(StimulusKind.Sound, tier, position, fromPlayer));
            return;
        }

        if (direction.magnitude > effectiveRadius * _noiseSuspicionRatio)
        {
            return;
        }

        OnStimulus?.Invoke(new Stimulus(StimulusKind.Sound, StimulusTier.Faint, position, fromPlayer));
    }

    private void OnDrawGizmos()
    {
        if (!_gizmoNoiseActive)
        {
            return;
        }
        Gizmos.color = _gizmoNoiseHeard ? Color.green : Color.red;
        Gizmos.DrawLine(_gizmoNoisePosition, transform.position);
    }
}
