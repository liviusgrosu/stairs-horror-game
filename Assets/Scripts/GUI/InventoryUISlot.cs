using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryUISlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public InventoryItem Item = null;
    public Image Icon;
    public TextMeshProUGUI Quantity;
    public Sprite EmptySprite;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!Item) return;

        InventoryUI.Instance.ShowItemDescription(Item.Name, Item.Description);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        InventoryUI.Instance.HideItemDescription();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!Item)
        {
            return;
        }

        if (Item is GemEssenceDustItem dustItem)
        {
            GemSelectionUI.Instance.RestoreUse(dustItem.GemType);
            Inventory.Instance.Remove(Item, 1);
        }
        else if (Item.Type == ItemType.GemSlot)
        {
            Inventory.Instance.EquipGem(Item);
        }
        else if (Item.Type == ItemType.Consumable)
        {
            PlayerHealth.Instance.UseHealthBottle();
        }
        InventoryUI.Instance.HideItemDescription();
    }

    public void Clear()
    {
        Item = null;
        Icon.sprite = EmptySprite;
        if (Quantity)
        {
            Quantity.text = "";
        }
    }
}
