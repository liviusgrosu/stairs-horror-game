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

    [Header("Statue")]
    [SerializeField] private Statue _statue;

    [Header("Ice Cracks")]
    [SerializeField] private Material _iceMaterial;
    [SerializeField] private float _crackIntensityUnlit = 0.2f;
    [SerializeField] private float _crackIntensityLit = 1f;

    [Header("Guidance")]
    [Tooltip("Guide object attached to the player, disabled until it's needed.")]
    [SerializeField] private FurnaceGuide _furnaceGuide;
    [Tooltip("How long the player can go without lighting a furnace before the guide first appears.")]
    [SerializeField] private float _guideDelay = 180f;
    [Tooltip("How often the guide reappears after the first time, while a furnace is still unlit.")]
    [SerializeField] private float _guideRepeatInterval = 300f;
    [Tooltip("Skip the guide if the player is already this close to the nearest unlit furnace.")]
    [SerializeField] private float _guideSkipDistance = 30f;

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

    private void Start()
    {
        StartCoroutine(GuideRoutine());
    }

    private IEnumerator GuideRoutine()
    {
        // Don't start counting until the player is actually playing (past the menu).
        while (GameManager.Instance == null || !GameManager.Instance.GameStarted)
        {
            yield return null;
        }

        yield return new WaitForSeconds(_guideDelay);

        while (true)
        {
            SpawnGuide();
            yield return new WaitForSeconds(_guideRepeatInterval);
        }
    }

    private void SpawnGuide()
    {
        if (!_furnaceGuide) return;

        var player = GameObject.Find("Player");
        if (!player) return;

        Furnace target = FindNearestUnlitFurnace(player.transform.position);
        if (!target) return;

        float distance = Vector3.Distance(player.transform.position, target.transform.position);
        if (distance <= _guideSkipDistance) return;

        _furnaceGuide.Activate(player.transform, target.transform);
    }

    private static Furnace FindNearestUnlitFurnace(Vector3 from)
    {
        Furnace nearest = null;
        float best = float.MaxValue;

        foreach (var furnace in FindObjectsByType<Furnace>(FindObjectsSortMode.None))
        {
            if (furnace.Used) continue;

            float distance = (furnace.transform.position - from).sqrMagnitude;
            if (distance < best)
            {
                best = distance;
                nearest = furnace;
            }
        }

        return nearest;
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

            if (_statue)
            {
                _statue.Pray();
            }
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
