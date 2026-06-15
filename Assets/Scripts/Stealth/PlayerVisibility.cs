using UnityEngine;

public class PlayerVisibility : MonoBehaviour
{
    public static PlayerVisibility Instance { get; private set; }

    [Header("Sampling")]
    [SerializeField] private float _updateInterval = 0.1f;

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

    // Stealth lights were removed, so visibility is just a constant ambient level (still modulated by
    // VisibilityMultiplier in Update).
    private float ComputeVisibility()
    {
        return Mathf.Clamp01(_ambientBase);
    }
}
