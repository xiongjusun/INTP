using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BagUIController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button bagButton;
    [SerializeField] private RectTransform bagPanel;
    [SerializeField] private CanvasGroup bagCanvasGroup;
    [SerializeField] private Transform contentRoot;
    [SerializeField] private BagItemSlotUI slotPrefab;

    [Header("Animation")]
    [SerializeField] private float openOffsetY = 220f;
    [SerializeField] private float moveSmoothTime = 0.08f;
    [SerializeField] private float fadeSpeed = 8f;

    [Header("Data")]
    [SerializeField] private List<PlaceableItemData> items = new();

    [Header("Placement")]
    [SerializeField] private WorldPlacementController placementController;
    [SerializeField] private bool closeBagWhenSelectingItem = true;

    private bool isOpen;
    private Vector2 closedPosition;
    private Vector2 openPosition;
    private Vector2 panelVelocity;
    private float currentAlpha;

    private void Awake()
    {
        if (bagPanel == null)
        {
            Debug.LogError("BagUIController: Assign Bag Panel.", this);
            enabled = false;
            return;
        }

        if (bagCanvasGroup == null)
        {
            bagCanvasGroup = bagPanel.GetComponent<CanvasGroup>();
            if (bagCanvasGroup == null)
                bagCanvasGroup = bagPanel.gameObject.AddComponent<CanvasGroup>();
        }

        if (bagButton != null)
        {
            bagButton.onClick.RemoveAllListeners();
            bagButton.onClick.AddListener(ToggleBag);
        }

        closedPosition = bagPanel.anchoredPosition;
        openPosition = closedPosition + Vector2.up * openOffsetY;

        BuildUI();
        SetBagOpenInstant(false);
    }

    private void Update()
    {
        AnimatePanel();
    }

    public void ToggleBag()
    {
        SetBagOpen(!isOpen);
    }

    public void SetBagOpen(bool open)
    {
        isOpen = open;

        if (isOpen)
            bagPanel.gameObject.SetActive(true);

        bagCanvasGroup.interactable = isOpen;
        bagCanvasGroup.blocksRaycasts = isOpen;
    }

    private void SetBagOpenInstant(bool open)
    {
        isOpen = open;

        bagPanel.gameObject.SetActive(true);
        bagPanel.anchoredPosition = open ? openPosition : closedPosition;

        currentAlpha = open ? 1f : 0f;
        bagCanvasGroup.alpha = currentAlpha;
        bagCanvasGroup.interactable = open;
        bagCanvasGroup.blocksRaycasts = open;

        if (!open)
            bagPanel.gameObject.SetActive(false);
    }

    private void AnimatePanel()
    {
        if (bagPanel == null || bagCanvasGroup == null)
            return;

        Vector2 targetPos = isOpen ? openPosition : closedPosition;
        float targetAlpha = isOpen ? 1f : 0f;

        bagPanel.anchoredPosition = Vector2.SmoothDamp(
            bagPanel.anchoredPosition,
            targetPos,
            ref panelVelocity,
            moveSmoothTime);

        currentAlpha = Mathf.MoveTowards(
            currentAlpha,
            targetAlpha,
            fadeSpeed * Time.unscaledDeltaTime);

        bagCanvasGroup.alpha = currentAlpha;

        if (!isOpen &&
            currentAlpha <= 0.001f &&
            (bagPanel.anchoredPosition - targetPos).sqrMagnitude < 0.25f)
        {
            bagPanel.gameObject.SetActive(false);
        }
    }

    public void BuildUI()
    {
        if (contentRoot == null || slotPrefab == null)
            return;

        for (int i = contentRoot.childCount - 1; i >= 0; i--)
            Destroy(contentRoot.GetChild(i).gameObject);

        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] == null)
                continue;

            BagItemSlotUI slot = Instantiate(slotPrefab, contentRoot);
            slot.Setup(items[i], this);
        }
    }

    public void BeginItemPlacement(PlaceableItemData item)
    {
        if (placementController == null || item == null)
            return;

        placementController.BeginPlacement(item);

        if (closeBagWhenSelectingItem)
            SetBagOpen(false);
    }
}