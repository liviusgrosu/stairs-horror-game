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

    private readonly Vector3[] _offsets = new Vector3[8];
    private readonly List<BodyEncounter> _spawned = new List<BodyEncounter>();
    private float _timer;
    private bool _hasSpawned;
    private bool _hasCommitted;

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
        if (!player) return;

        Vector3 playerPos = player.position;
        Vector3 up = Vector3.up * verticalHalfLength;
        for (int i = 0; i < _offsets.Length; i++)
        {
            Vector3 point = playerPos + _offsets[i];
            Debug.DrawLine(point + up, point - up, rayColor);
        }

        if (!_hasSpawned)
        {
            _timer += Time.deltaTime;
            if (_timer >= spawnDelay)
            {
                SpawnEncounters(playerPos, up);
            }
        }
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

    // Raycasts down from each surrounding offset and returns the unique stairs found around the player.
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

    // Spawns the given prefab on a random stair found around the player. Returns the instance, or null if none found.
    public GameObject SpawnAtRandomStair(GameObject prefab)
    {
        if (!prefab || !player) return null;

        Vector3 up = Vector3.up * verticalHalfLength;
        List<GameObject> stairs = FindStairs(player.position, up);
        if (stairs.Count == 0) return null;

        GameObject stair = stairs[Random.Range(0, stairs.Count)];
        return Instantiate(prefab, stair.transform.position, Quaternion.identity);
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

    private static float Snap(float value, float step)
    {
        if (step <= 0f) return value;
        return Mathf.Round(value / step) * step;
    }
}
