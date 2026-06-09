using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    public static InventoryUI Instance;

    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _descriptionText;
    [SerializeField] private TextMeshProUGUI _placeholderText;
    private List<InventoryUISlot> _itemUISlots;

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        if (Inventory.Instance != null)
        {
            Inventory.Instance.OnChanged += Refresh;
        }
        Refresh();
    }

    private void OnDisable()
    {
        if (Inventory.Instance != null)
        {
            Inventory.Instance.OnChanged -= Refresh;
        }
    }

    public void ShowItemDescription(string itemName, string itemDescription)
    {
        _nameText.gameObject.SetActive(true);
        _descriptionText.gameObject.SetActive(true);
        _placeholderText.gameObject.SetActive(false);
        _nameText.text = itemName;
        _descriptionText.text = itemDescription;
    }

    public void HideItemDescription()
    {
        _nameText.gameObject.SetActive(false);
        _descriptionText.gameObject.SetActive(false);
        _placeholderText.gameObject.SetActive(true);
    }

    private void Refresh()
    {
        _itemUISlots = transform.GetComponentsInChildren<InventoryUISlot>().ToList();

        // Clear all inventory slots first
        foreach (var slot in _itemUISlots)
        {
            slot.Clear();
        }

        // Populate inventory slots
        foreach (var (item, quantity) in Inventory.Instance.Items)
        {
            var slot = _itemUISlots.Find(s => s.Item == item);
            if (slot && slot.Quantity)
            {
                slot.Quantity.text = quantity.ToString();
                continue;
            }

            var nextEmptySlot = _itemUISlots.Find(s => !s.Item);
            nextEmptySlot.Icon.sprite = item.Icon;
            nextEmptySlot.Item = item;
            nextEmptySlot.Quantity.text = quantity.ToString();
        }
    }
}
