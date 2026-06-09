using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ViewGemUISlot : MonoBehaviour, IPointerClickHandler
{
    public Image Icon;
    public Sprite EmptySprite;

    private ViewGemScreen _screen;
    private int _gemIndex = -1;

    public void Bind(ViewGemScreen screen, int gemIndex, Sprite icon)
    {
        _screen = screen;
        _gemIndex = gemIndex;
        if (Icon)
        {
            Icon.sprite = icon;
            Icon.enabled = true;
        }
    }

    public void Clear()
    {
        _screen = null;
        _gemIndex = -1;
        if (Icon)
        {
            Icon.sprite = EmptySprite;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!_screen || _gemIndex < 0)
        {
            return;
        }
        //_screen.SelectGem(_gemIndex);
    }
}
