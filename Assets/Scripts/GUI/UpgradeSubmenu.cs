
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class Requirement
{
    public InventoryItem Item;
    public int Amount;
    public TextMeshProUGUI Text;
}

public class UpgradeSubmenu : MonoBehaviour
{
    [SerializeField] private List<Requirement> _requirements;
    [SerializeField] private Button _upgradeButton;
    private bool _meetsRequirements;
    [SerializeField] private string _upgradeItemName;
    [SerializeField] private GameObject _nextUpgradeItem;
    [SerializeField] private GameObject _noFurtherUpgradesText;
    
    public void OnEnable()
    {
        _meetsRequirements = true;
        foreach (var requirement in _requirements)
        {
            var currentAmount = Inventory.Instance.GetCount(requirement.Item);
            if (currentAmount < requirement.Amount)
            {
                _meetsRequirements = false;
            }
            requirement.Text.text = $"{currentAmount} / {requirement.Amount}";
        }
        
        _upgradeButton.interactable = _meetsRequirements;
    }

    public void Upgrade()
    {
        /*PickaxeHand.Instance.SwitchPickaxe(_upgradeItemName);
        PickaxeHand.Instance.PlayUpgradePickupSound();
        
        foreach (var requirement in _requirements)
        {
            Inventory.Instance.Remove(requirement.Item, requirement.Amount);
        }
        
        gameObject.SetActive(false);
        if (_nextUpgradeItem)
        {
            _nextUpgradeItem.SetActive(true);
        }
        else if (_noFurtherUpgradesText)
        {
            _noFurtherUpgradesText.SetActive(true);
        }
        
        Inventory.Instance.SwitchPickaxe(_upgradeItemName);*/
    }
}
