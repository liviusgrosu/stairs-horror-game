using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PickaxeUI : MonoBehaviour
{
    private List<PickaxeUIGemSlot> _pickaxeGemUISlots;
    public int GemSlotCount => _pickaxeGemUISlots.Count;

    private void Awake()
    {
        _pickaxeGemUISlots = transform.GetComponentsInChildren<PickaxeUIGemSlot>().ToList();
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

    private void Refresh()
    {
        foreach (var gemSlot in _pickaxeGemUISlots)
        {
            gemSlot.Clear();
        }

        for (var i = 0; i < Inventory.Instance.PickaxeGems.Count && i < _pickaxeGemUISlots.Count; i++)
        {
            var gem = Inventory.Instance.PickaxeGems[i];
            _pickaxeGemUISlots[i].Icon.sprite = gem.Icon;
            _pickaxeGemUISlots[i].Item = gem;
        }
    }
}
