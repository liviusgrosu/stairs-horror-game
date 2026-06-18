using System.Collections;
using UnityEngine;

public class FurnaceManager : MonoBehaviour
{
    public static FurnaceManager Instance;

    [SerializeField] private GameObject _door;
    [SerializeField] private int _requiredFurnaces = 2;
    [Header("Furnace Spawns")]
    [SerializeField] private GameObject _zombiePrefab;
    [SerializeField] private float _secondFurnaceSpawnDelay = 60f;

    [Header("Ice Cracks")]
    [SerializeField] private Material _iceMaterial;
    [SerializeField] private float _crackIntensityUnlit = 0.2f;
    [SerializeField] private float _crackIntensityLit = 1f;

    private static readonly int CrackIntensityID = Shader.PropertyToID("_CrackIntensity");

    private int _usedCount;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        SetCrackIntensity(_crackIntensityUnlit);
    }

    private void SetCrackIntensity(float value)
    {
        if (_iceMaterial)
        {
            _iceMaterial.SetFloat(CrackIntensityID, value);
        }
    }

    public void NotifyFurnaceUsed()
    {
        _usedCount++;

        if (_usedCount == 1)
        {
            SetCrackIntensity(_crackIntensityLit);
            SpawnEngagedZombie();
        }
        else if (_usedCount == 2)
        {
            StartCoroutine(SpawnEngagedZombieDelayed(_secondFurnaceSpawnDelay));
        }

        if (_usedCount >= _requiredFurnaces && _door)
        {
            _door.SetActive(false);
        }
    }

    private IEnumerator SpawnEngagedZombieDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);
        SpawnEngagedZombie();
    }

    private void SpawnEngagedZombie()
    {
        if (!_zombiePrefab) return;

        // Prefer waking a zombie from the recurring wave; this also despawns the rest of that wave.
        if (FirstEncounter.Instance && FirstEncounter.Instance.EngageRecurringWave())
        {
            return;
        }

        // Otherwise wake any other dormant zombie already in the scene.
        EnemyAI idle = FindIdleZombie();
        if (idle)
        {
            idle.Engage();
            return;
        }

        if (!FirstEncounter.Instance) return;

        FirstEncounter.Instance.SpawnAtRandomStair(_zombiePrefab, true);
    }

    private static EnemyAI FindIdleZombie()
    {
        foreach (var ai in FindObjectsByType<EnemyAI>(FindObjectsSortMode.None))
        {
            if (ai.IsIdle) return ai;
        }
        return null;
    }
}
