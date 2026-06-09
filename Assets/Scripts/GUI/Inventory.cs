using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class Inventory : MonoBehaviour
{
    public static Inventory Instance;

    [Header("Pickaxe Gem Slot UI")]
    public Transform pickaxeUIParent;
    private PickaxeUI _currentPickaxeUI;
    [Header("--- This is all temp ---")]
    [SerializeField] private GameObject bronzePickaxeUI;
    [SerializeField] private GameObject ironPickaxeUI;
    [SerializeField] private GameObject goldPickaxeUI;
    
    [Header("Light Gem")]
    [SerializeField] private InventoryItem lightGemItem;
    [SerializeField] private Light playerLight;

    [Header("Sound Effects")]
    [SerializeField] private AudioClip gemAttachSound;
    [SerializeField] private AudioClip gemDetachSound;
    private AudioSource _audioSource;

    [Serializable]
    public class DebugStartingItem
    {
        public InventoryItem Item;
        public int Quantity = 1;
    }

    [Header("Debug")]
    [SerializeField] private List<DebugStartingItem> _debugStartingItems = new();

    public event Action OnChanged;

    private int _inventoryCapacity = 8;
    private int _pickaxeGemCapacity => _currentPickaxeUI ? _currentPickaxeUI.GemSlotCount : 0;
    
    private readonly Dictionary<InventoryItem, int> _items = new();
    
    private readonly List<InventoryItem> _pickaxeGems = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        _audioSource = GetComponent<AudioSource>();
        if (playerLight) playerLight.enabled = false;
    }

    public IReadOnlyDictionary<InventoryItem, int> Items => _items;
    public IReadOnlyList<InventoryItem> PickaxeGems => _pickaxeGems;

    /*private void Start()
    {
        _currentPickaxeUI = Instantiate(bronzePickaxeUI, pickaxeUIParent).GetComponent<PickaxeUI>();
        ApplyDebugStartingItems();
    }*/

    private void ApplyDebugStartingItems()
    {
        var added = false;
        foreach (var entry in _debugStartingItems)
        {
            if (!entry.Item || entry.Quantity <= 0)
            {
                continue;
            }

            _items[entry.Item] = _items.GetValueOrDefault(entry.Item, 0) + entry.Quantity;
            added = true;
        }

        if (added)
        {
            OnChanged?.Invoke();
        }
    }

    public void SwitchPickaxe(string newPickaxe)
    {
        RemoveAllGems();
        Destroy(_currentPickaxeUI.gameObject);
        switch (newPickaxe)
        {
            case "Bronze Pickaxe":
                _currentPickaxeUI = Instantiate(bronzePickaxeUI, pickaxeUIParent).GetComponent<PickaxeUI>();
                break;
            case "Iron Pickaxe":
                _currentPickaxeUI = Instantiate(ironPickaxeUI, pickaxeUIParent).GetComponent<PickaxeUI>();
                break;
            case "Gold Pickaxe":
                _currentPickaxeUI = Instantiate(goldPickaxeUI, pickaxeUIParent).GetComponent<PickaxeUI>();
                break;
        }
    }

    public void Add(InventoryItem item)
    {
        if (!_items.TryAdd(item, 1))
        {
            _items[item]++;
        }

        if (PickupNotification.Instance)
        {
            PickupNotification.Instance.Show(item);
        }

        OnChanged?.Invoke();
    }


    public void EquipGem(InventoryItem item)
    {
        if (_pickaxeGems.Count >= _pickaxeGemCapacity)
        {
            return;
        }

        if (_items.ContainsKey(item))
        {
            _items[item]--;
            if (_items[item] <= 0)
            {
                _items.Remove(item);
            }
        }

        _pickaxeGems.Add(item);
        if (item == lightGemItem && playerLight)
        {
            playerLight.enabled = true;
        }
        if (_audioSource && gemAttachSound) _audioSource.PlayOneShot(gemAttachSound);
        OnChanged?.Invoke();
    }

    public void RemoveGem(InventoryItem item)
    {
        if (_pickaxeGems.Count >= _inventoryCapacity)
        {
            return;
        }

        if (_pickaxeGems.Contains(item))
        {
            _pickaxeGems.Remove(item);
        }
        if (item == lightGemItem && playerLight) playerLight.enabled = false;
        Add(item);
        if (_audioSource && gemDetachSound) _audioSource.PlayOneShot(gemDetachSound);
        OnChanged?.Invoke();
    }

    private void RemoveAllGems()
    {
        foreach (var item in _pickaxeGems.ToList())
        {
            if (_pickaxeGems.Contains(item))
            {
                _pickaxeGems.Remove(item);
            }

            if (item == lightGemItem && playerLight)
            {
                playerLight.enabled = false;
            }
            Add(item);
        }
    }

    public int GetCount(InventoryItem item)
    {
        return _items.GetValueOrDefault(item, 0);
    }

    public void Remove(InventoryItem item, int amount)
    {
        if (!_items.ContainsKey(item)) return;

        _items[item] -= amount;
        if (_items[item] <= 0)
        {
            _items.Remove(item);
        }

        OnChanged?.Invoke();
    }


}
