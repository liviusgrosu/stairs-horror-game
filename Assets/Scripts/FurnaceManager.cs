using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FurnaceManager : MonoBehaviour
{
    public static FurnaceManager Instance;

    [SerializeField] private GameObject _door;
    [SerializeField] private int _requiredFurnaces = 3;
    [Header("Furnace Spawns")]
    [SerializeField] private GameObject _zombiePrefab;
    [Tooltip("Era 1: after the 2nd furnace, how long before the one-time instant-death zombie spawns.")]
    [SerializeField] private float _instantDeathDelay = 60f;
    [Tooltip("Spawned at the respawn point on death. While the player is inside it, encounter timers pause and no zombies spawn.")]
    [SerializeField] private GameObject _safeAreaPrefab;
    [Tooltip("Spawned at the same spot as the safe area on death. Unlike the safe area, this persists after the player walks away.")]
    [SerializeField] private GameObject _handTablePrefab;
    [Tooltip("Era 2 with 1 furnace lit: how often a dormant (forgiving) zombie spawns.")]
    [SerializeField] private float _dormantForgivingInterval = 90f;
    [Tooltip("Era 2 with 2 furnaces lit: how often a dormant (wider engage range) zombie spawns.")]
    [SerializeField] private float _dormantAlertInterval = 45f;
    [Tooltip("Era 2 with 3 furnaces lit: how often an engaged zombie spawns during the final chase.")]
    [SerializeField] private float _engagedRecurringInterval = 60f;

    [Header("Spawn Profiles")]
    [SerializeField] private EnemySpawnProfile _instantDeathProfile = new EnemySpawnProfile { StartEngaged = true, RunSpeed = 12f, EngageDistanceScale = 1f, InstantKill = true };
    [SerializeField] private EnemySpawnProfile _dormantForgivingProfile = new EnemySpawnProfile { StartEngaged = false, RunSpeed = 6f, EngageDistanceScale = 0.8f, InstantKill = false };
    [SerializeField] private EnemySpawnProfile _dormantAlertProfile = new EnemySpawnProfile { StartEngaged = false, RunSpeed = 6f, EngageDistanceScale = 1.6f, InstantKill = false };
    [SerializeField] private EnemySpawnProfile _engagedProfile = new EnemySpawnProfile { StartEngaged = true, RunSpeed = 6f, EngageDistanceScale = 1f, InstantKill = false };

    [Header("Statue")]
    [SerializeField] private Statue _statue;

    [Header("Ice Cracks")]
    [SerializeField] private Material _iceMaterial;
    [Tooltip("Crack intensity before any furnace is lit.")]
    [SerializeField] private float _crackIntensityUnlit = 0.2f;
    [Tooltip("Crack intensity after the 1st furnace is lit.")]
    [SerializeField] private float _firstCrackIntensity = 0.5f;
    [Tooltip("Crack intensity after the 2nd furnace is lit.")]
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

    private int _activeFurnaceCount;

    public int UsedFurnaceCount => _activeFurnaceCount;

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

    private bool _instantDeathTriggered;
    private int _era2Mode = -1;
    private SafeArea _activeSafeArea;

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

        Transform target = FindNearestEmberBall(player.transform.position);
        if (!target)
        {
            if (_activeFurnaceCount >= _requiredFurnaces)
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
        }

        float distance = Vector3.Distance(player.transform.position, target.position);
        if (distance <= _guideSkipDistance) return;

        _furnaceGuide.Activate(player.transform, target);
    }

    private static Transform FindNearestEmberBall(Vector3 from)
    {
        Transform nearest = null;
        float best = float.MaxValue;

        foreach (var ember in FindObjectsByType<EmberBall>(FindObjectsSortMode.None))
        {
            float distance = (ember.transform.position - from).sqrMagnitude;
            if (distance < best)
            {
                best = distance;
                nearest = ember.transform;
            }
        }

        return nearest;
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

    private void ApplyCrackForCount(int count)
    {
        float intensity = count switch
        {
            <= 0 => _crackIntensityUnlit,
            1 => _firstCrackIntensity,
            _ => _secondCrackIntensity,
        };
        SetCrackIntensity(intensity);
    }

    public void NotifyFurnaceUsed()
    {
        _activeFurnaceCount++;
        ApplyCrackForCount(_activeFurnaceCount);

        if (_activeFurnaceCount == 1 && _statue)
        {
            _statue.Pray();
        }

        if (_activeFurnaceCount >= _requiredFurnaces && _door)
        {
            _door.SetActive(false);
            SpawnGuide();
        }

        if (!_instantDeathTriggered)
        {
            if (_activeFurnaceCount == 1)
            {
                if (FirstEncounter.Instance) FirstEncounter.Instance.ArmFirstEncounter();
            }
            else if (_activeFurnaceCount == 2)
            {
                StartCoroutine(SpawnInstantDeathDelayed(_instantDeathDelay));
            }
        }
        else
        {
            RefreshEra2Encounter();
        }
    }

    private IEnumerator SpawnInstantDeathDelayed(float delay)
    {
        var elapsed = 0f;
        while (elapsed < delay)
        {
            if (!SafeArea.PlayerInside)
            {
                elapsed += Time.deltaTime * EncounterAccelerationZone.Multiplier;
            }
            yield return null;
        }

        if (_instantDeathTriggered) yield break;

        if (FirstEncounter.Instance)
        {
            FirstEncounter.Instance.SpawnAtRandomStair(_zombiePrefab, _instantDeathProfile);
        }
    }

    public void NotifyInstantDeathOccurred()
    {
        if (_instantDeathTriggered) return;
        _instantDeathTriggered = true;
    }

    public void RequestAdvanceEncounter()
    {
        if (!_instantDeathTriggered) return;
        if (!FirstEncounter.Instance) return;
        FirstEncounter.Instance.SpawnAtRandomStair(_zombiePrefab, _engagedProfile);
    }

    public void OnPlayerRespawned(bool deactivateFurnace)
    {
        if (deactivateFurnace)
        {
            DeactivateRandomFurnace();
        }
        RefreshEra2Encounter();
    }

    public SafeArea SpawnSafeArea(Vector3 position, bool spawnHandTable)
    {
        if (!_safeAreaPrefab) return null;

        if (_activeSafeArea)
        {
            Destroy(_activeSafeArea.gameObject);
        }

        GameObject instance = Instantiate(_safeAreaPrefab, position, Quaternion.identity);
        _activeSafeArea = instance.GetComponent<SafeArea>();

        if (spawnHandTable && _handTablePrefab)
        {
            Instantiate(_handTablePrefab, position, Quaternion.identity);
        }

        return _activeSafeArea;
    }

    private void DeactivateRandomFurnace()
    {
        var lit = new List<Furnace>();
        foreach (var furnace in FindObjectsByType<Furnace>(FindObjectsSortMode.None))
        {
            if (furnace.Used) lit.Add(furnace);
        }

        if (lit.Count == 0) return;

        Furnace chosen = lit[Random.Range(0, lit.Count)];
        chosen.Deactivate();

        _activeFurnaceCount = Mathf.Max(0, _activeFurnaceCount - 1);
        ApplyCrackForCount(_activeFurnaceCount);

        if (_door && _activeFurnaceCount < _requiredFurnaces)
        {
            _door.SetActive(true);
        }
    }

    private void RefreshEra2Encounter()
    {
        if (!_instantDeathTriggered) return;
        if (!FirstEncounter.Instance) return;

        int mode = Mathf.Clamp(_activeFurnaceCount, 0, 3);
        if (mode == _era2Mode) return;
        _era2Mode = mode;

        switch (mode)
        {
            case 0:
                FirstEncounter.Instance.StopRecurring();
                FirstEncounter.Instance.DespawnAllZombies();
                break;
            case 1:
                FirstEncounter.Instance.StartRecurringSpawns(_dormantForgivingInterval, _dormantForgivingProfile);
                break;
            case 2:
                FirstEncounter.Instance.StartRecurringSpawns(_dormantAlertInterval, _dormantAlertProfile);
                break;
            default:
                FirstEncounter.Instance.SpawnAtRandomStair(_zombiePrefab, _engagedProfile);
                FirstEncounter.Instance.StartRecurringSpawns(_engagedRecurringInterval, _engagedProfile);
                break;
        }
    }
}
