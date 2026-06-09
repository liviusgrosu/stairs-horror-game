using UnityEngine;

public class PlayerVisibility : MonoBehaviour
{
    public static PlayerVisibility Instance { get; private set; }

    [Header("Sampling")]
    [SerializeField] private float _updateInterval = 0.1f;
    [SerializeField] private LayerMask _occlusionMask = ~0;

    [Header("Ambient")]
    [SerializeField] [Range(0f, 0.5f)] private float _ambientBase = 0.1f;

    [Header("Light Contribution")]
    [SerializeField] private float _smoothSpeed = 5f;

    public float VisibilityValue { get; private set; } = 1f;
    public float VisibilityMultiplier { get; set; } = 1f;

    private float _targetVisibility = 1f;
    private float _timeSinceLastUpdate;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        _timeSinceLastUpdate += Time.deltaTime;
        if (_timeSinceLastUpdate >= _updateInterval)
        {
            _timeSinceLastUpdate = 0f;
            _targetVisibility = ComputeVisibility();
        }

        VisibilityValue = Mathf.Lerp(VisibilityValue, _targetVisibility, _smoothSpeed * Time.deltaTime) * Mathf.Clamp01(VisibilityMultiplier);
    }

    private float ComputeVisibility()
    {
        var total = _ambientBase;
        var playerPos = transform.position;

        foreach (var stealthLight in StealthLight.Active)
        {
            if (!stealthLight)
            {
                continue;
            }

            var light = stealthLight.Source;
            if (!light.enabled || !light.gameObject.activeInHierarchy)
            {
                continue;
            }

            var effectivePos = GetEffectiveLightPoint(light, playerPos);
            if (effectivePos == null)
            {
                continue;
            }

            var distance = Vector3.Distance(playerPos, effectivePos.Value);
            if (distance > stealthLight.DetectionRange)
            {
                continue;
            }

            var coneFactor = 1f;
            if (light.type == LightType.Spot)
            {
                var lightToPlayer = (playerPos - light.transform.position).normalized;
                var angleToPlayer = Vector3.Angle(light.transform.forward, lightToPlayer);
                var halfSpotAngle = light.spotAngle * 0.5f;
                if (angleToPlayer > halfSpotAngle)
                {
                    continue;
                }

                coneFactor = 1f - (angleToPlayer / halfSpotAngle);
            }

            var direction = (effectivePos.Value - playerPos).normalized;
            if (Physics.Raycast(playerPos, direction, distance, _occlusionMask))
            {
                continue;
            }

            var t = 1f - (distance / stealthLight.DetectionRange);
            total += stealthLight.MaxContribution * stealthLight.FalloffCurve.Evaluate(t) * coneFactor;
        }

        return Mathf.Clamp01(total);
    }

    private static Vector3? GetEffectiveLightPoint(Light light, Vector3 playerPos)
    {
        if (light.type is not (LightType.Rectangle or LightType.Disc))
        {
            return light.transform.position;
        }
        
        var localPlayer = light.transform.InverseTransformPoint(playerPos);
        if (localPlayer.z < 0f)
        {
            return null;
        }

        Vector3 closest;
        if (light.type == LightType.Rectangle)
        {
            closest = new Vector3(
                Mathf.Clamp(localPlayer.x, -light.areaSize.x * 0.5f, light.areaSize.x * 0.5f),
                Mathf.Clamp(localPlayer.y, -light.areaSize.y * 0.5f, light.areaSize.y * 0.5f),
                0f
            );
        }
        else
        {
            var onPlane = new Vector2(localPlayer.x, localPlayer.y);
            var radius = light.areaSize.x * 0.5f;
            if (onPlane.magnitude > radius)
            {
                onPlane = onPlane.normalized * radius;
            }
            closest = new Vector3(onPlane.x, onPlane.y, 0f);
        }

        var worldPoint = light.transform.TransformPoint(closest);
        return worldPoint + (playerPos - worldPoint).normalized * 0.05f;
    }
}
