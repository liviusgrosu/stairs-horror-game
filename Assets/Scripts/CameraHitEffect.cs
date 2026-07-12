using System.Collections;
using UnityEngine;

public class CameraHitEffect : MonoBehaviour
{
    public static CameraHitEffect Instance;

    [SerializeField]
    private float maxYawOffset = 30f;
    [SerializeField]
    private float maxPitchOffset = 15f;
    [SerializeField]
    private float recoverySpeed = 2f;
    [SerializeField]
    private float downwardPitchOffset = 25f;

    [Header("Death Collapse")]
    [Tooltip("Roll around the camera's forward axis when the player dies. Negative is clockwise.")]
    [SerializeField] private float deathRollAngle = -90f;
    [SerializeField] private float deathRollDuration = 0.6f;

    private Vector3 _rotationOffset;
    private float _deathRoll;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void LateUpdate()
    {
        if (_rotationOffset != Vector3.zero)
        {
            _rotationOffset = Vector3.Lerp(_rotationOffset, Vector3.zero, recoverySpeed * Time.deltaTime);

            if (_rotationOffset.sqrMagnitude < 0.01f)
            {
                _rotationOffset = Vector3.zero;
            }
        }

        if (_rotationOffset != Vector3.zero || _deathRoll != 0f)
        {
            transform.localRotation *= Quaternion.Euler(_rotationOffset.x, _rotationOffset.y, _rotationOffset.z + _deathRoll);
        }
    }

    public IEnumerator PlayDeathCollapse()
    {
        _deathRoll = 0f;

        var elapsed = 0f;
        while (elapsed < deathRollDuration)
        {
            elapsed += Time.deltaTime;
            _deathRoll = Mathf.Lerp(0f, deathRollAngle, elapsed / deathRollDuration);
            yield return null;
        }
        _deathRoll = deathRollAngle;
    }

    public void ResetDeathCollapse()
    {
        _deathRoll = 0f;
        _rotationOffset = Vector3.zero;
    }

    public void ApplyHitRotation()
    {
        var yaw = Random.Range(-maxYawOffset, maxYawOffset);
        var pitch = Random.Range(-maxPitchOffset, maxPitchOffset);
        _rotationOffset = new Vector3(pitch, yaw, 0f);
    }

    public void ApplyDownwardHit()
    {
        _rotationOffset = new Vector3(downwardPitchOffset, 0f, 0f);
    }
}
