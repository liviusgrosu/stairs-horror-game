using UnityEngine;

[RequireComponent(typeof(Collider))]
public class EncounterAccelerationZone : MonoBehaviour
{
    [SerializeField] private float _timeMultiplier = 30f;
    [SerializeField] private int _activeRayCount = 12;
    [SerializeField] private float _activeRadius = 20f;

    public static float Multiplier { get; private set; } = 1f;
    public static bool PlayerInside { get; private set; }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        Multiplier = _timeMultiplier;
        PlayerInside = true;
        if (FirstEncounter.Instance) FirstEncounter.Instance.SetRays(_activeRayCount, _activeRadius);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        Multiplier = 1f;
        PlayerInside = false;
        if (FirstEncounter.Instance) FirstEncounter.Instance.ResetRays();
    }

    private void OnDisable()
    {
        Multiplier = 1f;
        PlayerInside = false;
    }
}
