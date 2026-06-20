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

    private Vector3 _rotationOffset;

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
        if (_rotationOffset == Vector3.zero)
        {
            return;
        }

        _rotationOffset = Vector3.Lerp(_rotationOffset, Vector3.zero, recoverySpeed * Time.deltaTime);

        if (_rotationOffset.sqrMagnitude < 0.01f)
        {
            _rotationOffset = Vector3.zero;
        }

        transform.localRotation *= Quaternion.Euler(_rotationOffset);
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
