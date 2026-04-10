using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BagItemSlotUI : MonoBehaviour, IPointerDownHandler
{
    [SerializeField] private Image iconImage;

    private PlaceableItemData itemData;
    private BagUIController owner;

    public void Setup(PlaceableItemData data, BagUIController bagUI)
    {
        itemData = data;
        owner = bagUI;

        if (iconImage != null)
            iconImage.sprite = data != null ? data.icon : null;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (itemData == null || owner == null)
            return;

        owner.BeginItemPlacement(itemData);
    }
}