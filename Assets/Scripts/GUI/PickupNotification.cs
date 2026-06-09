using System.Collections.Generic;
using UnityEngine;

public class PickupNotification : MonoBehaviour
{
    public static PickupNotification Instance;

    [SerializeField] private GameObject notificationPrefab;
    [SerializeField] private float slideUpAmount = 70f;
    [SerializeField] private float slideSpeed = 8f;

    private readonly List<PickupNotificationEntry> _activeEntries = new();

    private void Awake()
    {
        Instance = this;
    }

    public void ClearAll()
    {
        foreach (var entry in _activeEntries)
        {
            if (entry) Destroy(entry.gameObject);
        }
        _activeEntries.Clear();
    }

    public void Show(InventoryItem item)
    {
        if (GameManager.Instance && GameManager.Instance.IsPaused) return;

        // Push existing entries up
        foreach (var entry in _activeEntries)
        {
            entry.PushUp(slideUpAmount);
        }

        var go = Instantiate(notificationPrefab, transform);
        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = Vector2.zero;

        var entry2 = go.GetComponent<PickupNotificationEntry>();
        entry2.Init(item, slideSpeed, this);
        _activeEntries.Add(entry2);
    }

    public void RemoveEntry(PickupNotificationEntry entry)
    {
        _activeEntries.Remove(entry);
    }
}
