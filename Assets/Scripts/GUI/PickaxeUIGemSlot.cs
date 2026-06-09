using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PickaxeUIGemSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public InventoryItem Item;
    public Image Icon;
    public Sprite EmptySprite;

    public void Clear()
    {
        Item = null;
        Icon.sprite = EmptySprite;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!Item)
        {
            return;
        }

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

        Inventory.Instance.RemoveGem(Item);
        InventoryUI.Instance.HideItemDescription();
    }
}
