using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Light))]
public class StealthLight : MonoBehaviour
{
    [SerializeField]
    private float detectionRange = 10f;
    [SerializeField]
    [Range(0f, 1f)]
    private float maxContribution = 1f;
    [SerializeField]
    private AnimationCurve falloffCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    public Light Source { get; private set; }
    public float DetectionRange => detectionRange;
    public float MaxContribution => maxContribution;
    public AnimationCurve FalloffCurve => falloffCurve;

    private static readonly List<StealthLight> _active = new();
    public static IReadOnlyList<StealthLight> Active => _active;

    private void Awake()
    {
        Source = GetComponent<Light>();
    }

    private void OnEnable()
    {
        if (!_active.Contains(this))
        {
            _active.Add(this);
        }
    }

    private void OnDisable()
    {
        _active.Remove(this);
    }
}
