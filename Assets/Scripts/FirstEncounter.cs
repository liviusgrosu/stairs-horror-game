using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FirstEncounter : MonoBehaviour
{
    public static FirstEncounter Instance;

    [SerializeField] private Transform player;
    [SerializeField] private float distance = 40f;
    [SerializeField] private float verticalHalfLength = 20f;
    [SerializeField] private Vector3 gridSnap = new Vector3(8f, 4f, 4f);
    [SerializeField] private Color rayColor = Color.red;
    [SerializeField] private GameObject zombieHangingPrefab;
    [SerializeField] private string stairsTag = "Stair";
    [SerializeField] private float spawnDelay = 120f;

    [Header("Recurring Spawn")]
    [SerializeField] private GameObject zombiePrefab;
    [Tooltip("When the player comes within this range of a dormant recurring zombie, the rest of the wave despawns. Set larger than the enemy's engage distance so the player can move around a found zombie without waking it.")]
    [SerializeField] private float recurringCommitDistance = 20f;
    [Tooltip("If the player leaves a still-dormant recurring zombie farther than this, it despawns (freeing the next wave to spawn). Set larger than the commit distance.")]
    [SerializeField] private float recurringAbandonDistance = 60f;

    private readonly Vector3[] _offsets = new Vector3[8];
    private readonly List<BodyEncounter> _spawned = new List<BodyEncounter>();
    private readonly List<EnemyAI> _recurringWave = new List<EnemyAI>();
    private float _timer;
    private bool _hasSpawned;
    private bool _hasCommitted;
    private bool _recurringCommitted;

    private bool _firstEncounterArmed;
    private bool _recurringActive;
    private float _recurringInterval;
    private bool _recurringEngaged;
    private float _recurringTimer;

    private void Awake()
    {
        Instance = this;

        if (!player)
        {
            var playerGO = GameObject.FindGameObjectWithTag("Player");
            if (playerGO) player = playerGO.transform;
        }

        for (int i = 0; i < 8; i++)
        {
            float angle = i * Mathf.PI * 2f / 8f;
            float x = Mathf.Cos(angle) * distance;
            float z = Mathf.Sin(angle) * distance;
            _offsets[i] = new Vector3(Snap(x, gridSnap.x), 0f, Snap(z, gridSnap.z));
        }
    }

    private void Update()
    {
        if (!DebugManager.SpawningEnabled) return;
        if (!player) return;

        Vector3 playerPos = player.position;
        Vector3 up = Vector3.up * verticalHalfLength;
        for (int i = 0; i < _offsets.Length; i++)
        {
            Vector3 point = playerPos + _offsets[i];
            Debug.DrawLine(point + up, point - up, rayColor);
        }

        if (_firstEncounterArmed && !_hasSpawned)
        {
            _timer += Time.deltaTime;
            if (_timer >= spawnDelay)
            {
                SpawnEncounters(playerPos, up);
            }
        }

        if (_recurringActive)
        {
            TickRecurring();
        }

        if (_recurringWave.Count > 0)
        {
            if (!_recurringCommitted)
                TryCommitRecurringWave(playerPos);
            else
                TryAbandonRecurringWave(playerPos);
        }
    }

    public void ArmFirstEncounter()
    {
        _firstEncounterArmed = true;
    }

    public void StartRecurringSpawns(float interval, bool engaged)
    {
        _recurringActive = true;
        _recurringInterval = Mathf.Max(0.01f, interval);
        _recurringEngaged = engaged;
        _recurringTimer = 0f;
    }

    private void TickRecurring()
    {
        _recurringTimer += Time.deltaTime;
        if (_recurringTimer < _recurringInterval) return;
        _recurringTimer = 0f;

        if (!zombiePrefab || IsZombieActive()) return;

        if (_recurringEngaged)
            SpawnAtRandomStair(zombiePrefab, true);
        else
            SpawnRecurringWave();
    }

    private void SpawnEncounters(Vector3 playerPos, Vector3 up)
    {
        if (!zombieHangingPrefab) return;

        foreach (GameObject stair in FindStairs(playerPos, up))
        {
            GameObject instance = Instantiate(zombieHangingPrefab, stair.transform.position, Quaternion.identity);
            var encounter = instance.GetComponent<BodyEncounter>();
            if (encounter)
            {
                encounter.PlayerEntered += OnEncounterEntered;
                _spawned.Add(encounter);
            }
        }

        _hasSpawned = _spawned.Count > 0;
    }

    private List<GameObject> FindStairs(Vector3 playerPos, Vector3 up)
    {
        var stairs = new List<GameObject>();
        var seen = new HashSet<GameObject>();
        float rayLen = verticalHalfLength * 2f;

        for (int i = 0; i < _offsets.Length; i++)
        {
            Vector3 origin = playerPos + _offsets[i] + up;
            if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, rayLen)) continue;
            if (!hit.collider.CompareTag(stairsTag)) continue;

            GameObject stair = hit.collider.gameObject;
            if (!seen.Add(stair)) continue;

            stairs.Add(stair);
        }

        return stairs;
    }

    public void SpawnAtRandomStair(GameObject prefab, bool startEngaged)
    {
        if (!DebugManager.SpawningEnabled || !prefab || !player) return;

        Vector3 up = Vector3.up * verticalHalfLength;
        List<GameObject> stairs = FindStairs(player.position, up);
        if (stairs.Count == 0) return;

        GameObject stair = stairs[Random.Range(0, stairs.Count)];
        GameObject instance = CreateZombie(prefab, stair.transform.position, startEngaged);
        instance.SetActive(true);
    }

    private static GameObject CreateZombie(GameObject prefab, Vector3 position, bool startEngaged)
    {
        GameObject instance = Instantiate(prefab, position, Quaternion.identity);
        var ai = instance.GetComponent<EnemyAI>();
        if (ai) ai.SetStartEngaged(startEngaged);
        return instance;
    }

    private void OnEncounterEntered(BodyEncounter entered)
    {
        if (_hasCommitted) return;
        _hasCommitted = true;

        for (int i = 0; i < _spawned.Count; i++)
        {
            var enc = _spawned[i];
            if (!enc) continue;
            enc.PlayerEntered -= OnEncounterEntered;
            if (enc != entered) Destroy(enc.gameObject);
        }
    }

    private void SpawnRecurringWave()
    {
        if (!player) return;

        Vector3 up = Vector3.up * verticalHalfLength;

        _recurringWave.Clear();
        _recurringCommitted = false;
        foreach (GameObject stair in FindStairs(player.position, up))
        {
            GameObject instance = CreateZombie(zombiePrefab, stair.transform.position, false);
            var ai = instance.GetComponent<EnemyAI>();
            if (ai) _recurringWave.Add(ai);
            instance.SetActive(true);
        }
    }

    private void TryCommitRecurringWave(Vector3 playerPos)
    {
        float sqrCommit = recurringCommitDistance * recurringCommitDistance;

        EnemyAI found = null;
        for (int i = 0; i < _recurringWave.Count; i++)
        {
            var ai = _recurringWave[i];
            if (!ai) continue;
            if ((ai.transform.position - playerPos).sqrMagnitude <= sqrCommit)
            {
                found = ai;
                break;
            }
        }

        if (!found) return;

        for (int i = 0; i < _recurringWave.Count; i++)
        {
            var ai = _recurringWave[i];
            if (!ai) continue;
            if (ai != found) Destroy(ai.gameObject);
        }
        _recurringWave.Clear();
        _recurringWave.Add(found);
        _recurringCommitted = true;
    }
    
    private void TryAbandonRecurringWave(Vector3 playerPos)
    {
        float sqrAbandon = recurringAbandonDistance * recurringAbandonDistance;

        for (int i = _recurringWave.Count - 1; i >= 0; i--)
        {
            var ai = _recurringWave[i];
            if (!ai)
            {
                _recurringWave.RemoveAt(i);
                continue;
            }
            if (!ai.IsIdle) continue;
            if ((ai.transform.position - playerPos).sqrMagnitude < sqrAbandon) continue;

            Destroy(ai.gameObject);
            _recurringWave.RemoveAt(i);
        }
    }
    
    public EnemyAI EngageRecurringWave()
    {
        if (_recurringWave.Count == 0 || !player) return null;

        for (int i = 0; i < _recurringWave.Count; i++)
        {
            var ai = _recurringWave[i];
            if (ai && !ai.IsIdle) return ai;
        }

        EnemyAI chosen = null;
        float bestSqr = float.PositiveInfinity;
        for (int i = 0; i < _recurringWave.Count; i++)
        {
            var ai = _recurringWave[i];
            if (!ai || !ai.IsIdle) continue;

            float sqr = (ai.transform.position - player.position).sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                chosen = ai;
            }
        }

        if (!chosen) return null;

        for (int i = 0; i < _recurringWave.Count; i++)
        {
            var ai = _recurringWave[i];
            if (ai && ai != chosen) Destroy(ai.gameObject);
        }
        _recurringWave.Clear();
        _recurringWave.Add(chosen);

        chosen.Engage();
        return chosen;
    }

    private static bool IsZombieActive()
    {
        return FindObjectsByType<EnemyAI>(FindObjectsSortMode.None).Length > 0;
    }

    private static float Snap(float value, float step)
    {
        if (step <= 0f) return value;
        return Mathf.Round(value / step) * step;
    }
}
