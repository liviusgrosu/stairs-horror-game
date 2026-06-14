using System.Collections;
using UnityEngine;

public class TorchManager : MonoBehaviour
{
    public static TorchManager Instance;

    [SerializeField] private GameObject _door;
    [SerializeField] private int _requiredTorches = 2;
    [Header("Torch Spawns")]
    [SerializeField] private GameObject _zombiePrefab;
    [SerializeField] private float _secondTorchSpawnDelay = 60f;

    private int _usedCount;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void NotifyTorchUsed()
    {
        _usedCount++;

        if (_usedCount == 1)
        {
            SpawnEngagedZombie();
        }
        else if (_usedCount == 2)
        {
            StartCoroutine(SpawnEngagedZombieDelayed(_secondTorchSpawnDelay));
        }

        if (_usedCount >= _requiredTorches && _door)
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

        // If a dormant zombie is already in the scene, wake it instead of spawning another.
        EnemyAI idle = FindIdleZombie();
        if (idle)
        {
            idle.Engage();
            return;
        }

        if (!FirstEncounter.Instance) return;

        GameObject instance = FirstEncounter.Instance.SpawnAtRandomStair(_zombiePrefab);
        if (!instance) return;

        var ai = instance.GetComponent<EnemyAI>();
        if (ai) ai.SetStartEngaged(true);
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
