using System.Collections;
using UnityEngine;

public class FurnaceManager : MonoBehaviour
{
    public static FurnaceManager Instance;

    [SerializeField] private GameObject _door;
    [SerializeField] private int _requiredFurnaces = 3;
    [Header("Furnace Spawns")]
    [SerializeField] private GameObject _zombiePrefab;
    [Tooltip("After the 2nd furnace, how long before the first engaged chase begins.")]
    [SerializeField] private float _secondFurnaceSpawnDelay = 60f;
    [Tooltip("Between the 2nd and 3rd furnace, how often a dormant (non-engaged) zombie can spawn.")]
    [SerializeField] private float _nonEngagedRecurringInterval = 90f;
    [Tooltip("After the 3rd furnace, how often a zombie spawns already engaged (chasing).")]
    [SerializeField] private float _engagedRecurringInterval = 60f;

    [Header("Statue")]
    [SerializeField] private Statue _statue;

    [Header("Ice Cracks")]
    [SerializeField] private Material _iceMaterial;
    [Tooltip("Crack intensity before any furnace is lit.")]
    [SerializeField] private float _crackIntensityUnlit = 0.2f;
    [Tooltip("Crack intensity after the 1st furnace is lit.")]
    [SerializeField] private float _firstCrackIntensity = 0.5f;
    [Tooltip("Crack intensity after the 2nd furnace is lit. The 3rd furnace deactivates the door, so this is the last crack state the player sees.")]
    [SerializeField] private float _secondCrackIntensity = 0.75f;

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

    public int UsedFurnaceCount => _usedCount;

    private int _furnaceOccupancy;
    public bool PlayerInsideFurnace => _furnaceOccupancy > 0;

    public void SetPlayerInsideFurnace(bool inside)
    {
        _furnaceOccupancy = Mathf.Max(0, _furnaceOccupancy + (inside ? 1 : -1));

        if (inside && EncounterAccelerationZone.PlayerInside && FirstEncounter.Instance)
        {
            FirstEncounter.Instance.OnPlayerEnteredFurnace();
        }
    }

    private int _encounterLevel;
    private bool _firstEncounterBodyDropped;
    private bool _advanceQueued;

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

        Transform target;
        if (_usedCount >= _requiredFurnaces)
        {
            if (!_door) return;
            target = _door.transform;
        }
        else
        {
            Furnace furnace = FindNearestUnlitFurnace(player.transform.position);
            if (!furnace) return;
            target = furnace.transform;
        }

        float distance = Vector3.Distance(player.transform.position, target.position);
        if (distance <= _guideSkipDistance) return;

        _furnaceGuide.Activate(player.transform, target);
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
            SetCrackIntensity(_firstCrackIntensity);

            if (_statue)
            {
                _statue.Pray();
            }
        }
        else if (_usedCount == 2)
        {
            SetCrackIntensity(_secondCrackIntensity);
        }

        RequestAdvanceEncounter();

        if (_usedCount >= _requiredFurnaces && _door)
        {
            _door.SetActive(false);
            SpawnGuide();
        }
    }

    public void RequestAdvanceEncounter()
    {
        if (_encounterLevel >= 3) return;

        if (_encounterLevel == 1 && !_firstEncounterBodyDropped)
        {
            _advanceQueued = true;
            return;
        }

        _encounterLevel++;
        ApplyEncounterLevel(_encounterLevel);
    }

    public void NotifyFirstEncounterBodyDropped()
    {
        _firstEncounterBodyDropped = true;

        if (_encounterLevel == 1 && _advanceQueued)
        {
            _advanceQueued = false;
            _encounterLevel = 2;
            ApplyEncounterLevel(2);
        }
    }

    private void ApplyEncounterLevel(int level)
    {
        if (level == 1)
        {
            if (FirstEncounter.Instance)
            {
                FirstEncounter.Instance.ArmFirstEncounter();
            }
        }
        else if (level == 2)
        {
            StartCoroutine(SpawnEngagedZombieDelayed(_secondFurnaceSpawnDelay));
            if (FirstEncounter.Instance)
            {
                FirstEncounter.Instance.StartRecurringSpawns(_nonEngagedRecurringInterval, false);
            }
        }
        else if (level == 3)
        {
            SpawnEngagedZombie();
            if (FirstEncounter.Instance)
            {
                FirstEncounter.Instance.StartRecurringSpawns(_engagedRecurringInterval, true);
            }
        }
    }

    private IEnumerator SpawnEngagedZombieDelayed(float delay)
    {
        var elapsed = 0f;
        while (elapsed < delay)
        {
            elapsed += Time.deltaTime * EncounterAccelerationZone.Multiplier;
            yield return null;
        }
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
