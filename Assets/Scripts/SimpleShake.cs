using UnityEngine;

/// <summary>
/// A one-shot positional shake that decays back to rest. Trigger it with
/// <see cref="Shake()"/> for impacts such as the elevator slamming to a stop.
/// The offset it applies each frame is removed on the next frame so it never
/// accumulates or drifts the transform from its base position.
/// </summary>
public class SimpleShake : MonoBehaviour
{
    [Tooltip("Shake strength (local units) at the start of a shake.")]
    public float ShakeIntensity = 0.1f;
    [Tooltip("How long a triggered shake lasts before fully settling.")]
    public float ShakeDuration = 0.6f;

    private Vector3 _shakeOffset;
    private float _startIntensity;
    private float _duration;
    private float _elapsed;
    private bool _shaking;

    /// <summary>Trigger a shake using the inspector defaults.</summary>
    public void Shake()
    {
        Shake(ShakeIntensity, ShakeDuration);
    }

    /// <summary>Trigger a shake with explicit strength and length.</summary>
    public void Shake(float intensity, float duration)
    {
        _startIntensity = intensity;
        _duration = Mathf.Max(0.0001f, duration);
        _elapsed = 0f;
        _shaking = true;
    }

    private void LateUpdate()
    {
        if (!_shaking)
        {
            return;
        }

        var strength = _startIntensity * (1f - _elapsed / _duration);
        _shakeOffset = Random.insideUnitSphere * strength;
        transform.localPosition += _shakeOffset;
    }

    private void Update()
    {
        // Undo last frame's displacement so the shake never accumulates.
        transform.localPosition -= _shakeOffset;
        _shakeOffset = Vector3.zero;

        if (!_shaking)
        {
            return;
        }

        _elapsed += Time.deltaTime;
        if (_elapsed >= _duration)
        {
            _shaking = false;
        }
    }
}
