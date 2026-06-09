using UnityEngine;

public class TorchManager : MonoBehaviour
{
    public static TorchManager Instance;

    [SerializeField] private GameObject _door;
    [SerializeField] private int _requiredTorches = 2;

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
        if (_usedCount >= _requiredTorches && _door)
        {
            _door.SetActive(false);
        }
    }
}
