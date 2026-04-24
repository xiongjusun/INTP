using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using INTP.Foundation;

namespace INTP.Gameplay
{
    /// <summary>
    /// 2D背包UI控制器 - 管理背包UI的显示和数据同步
    /// </summary>
    public class BackpackUIController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject backpackPanel;
        [SerializeField] private RectTransform slotContainer;
        [SerializeField] private GameObject slotPrefab;
        [SerializeField] private TextMeshProUGUI itemCountText;

        [Header("Animation")]
        [SerializeField] private float openAnimationDuration = 0.3f;
        [SerializeField] private AnimationCurve openAnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Settings")]
        [SerializeField] private int maxDisplaySlots = 8;

        private List<GameObject> _slotInstances = new();
        private bool _isAnimating = false;

        private void Awake()
        {
            Debug.Log($"[BackpackUIController] Awake called.");
            if (backpackPanel != null)
            {
                backpackPanel.SetActive(false);
            }
        }

        private void Start()
        {
            Debug.Log($"[BackpackUIController] Start - EventBus.Instance = {EventBus.Instance != null}");
            if (EventBus.Instance != null)
            {
                EventBus.Instance.Subscribe<BackpackToggleEvent>(OnBackpackToggle);
                EventBus.Instance.Subscribe<InteractEvent>(OnInteract);
            }
        }

        private void OnDestroy()
        {
            if (EventBus.Instance != null)
            {
                EventBus.Instance.Unsubscribe<BackpackToggleEvent>(OnBackpackToggle);
                EventBus.Instance.Unsubscribe<InteractEvent>(OnInteract);
            }
        }

        private void OnEnable()
        {
            Debug.Log("[BackpackUIController] OnEnable called - refreshing UI");
            if (BackpackService.Instance != null)
            {
                RefreshUI();
            }
            else
            {
                Debug.Log("[BackpackUIController] BackpackService not ready yet, skipping RefreshUI");
            }
        }

        private void OnDisable()
        {
        }

        private void OnBackpackToggle(BackpackToggleEvent evt)
        {
            Debug.Log($"[BackpackUIController] OnBackpackToggle received: {evt.IsOpen}");
            SetBackpackOpen(evt.IsOpen);
        }

        private void OnInteract(InteractEvent evt)
        {
            // 交互时如果背包打开则关闭
            if (BackpackService.Instance != null && BackpackService.Instance.IsOpen)
            {
                BackpackService.Instance.SetBackpackOpen(false);
            }
        }

        /// <summary>
        /// 设置背包开关状态
        /// </summary>
        public void SetBackpackOpen(bool open)
        {
            if (backpackPanel == null) return;

            if (open)
            {
                backpackPanel.SetActive(true);
                RefreshUI();
            }

            StopAllCoroutines();
            StartCoroutine(AnimateBackpack(open));
        }

        /// <summary>
        /// 切换背包状态
        /// </summary>
        public void ToggleBackpack()
        {
            if (BackpackService.Instance != null)
            {
                BackpackService.Instance.ToggleBackpack();
            }
        }

        /// <summary>
        /// 刷新背包UI
        /// </summary>
        public void RefreshUI()
        {
            Debug.Log($"[BackpackUIController] RefreshUI called. slotContainer={slotContainer != null}, slotPrefab={slotPrefab != null}, BackpackService={BackpackService.Instance != null}");
            
            if (slotContainer == null || slotPrefab == null)
            {
                Debug.LogError("[BackpackUIController] Missing slotContainer or slotPrefab reference!");
                return;
            }
            
            if (BackpackService.Instance == null)
            {
                Debug.LogError("[BackpackUIController] BackpackService.Instance is null!");
                return;
            }

            // 清除旧插槽
            foreach (var slot in _slotInstances)
            {
                if (slot != null)
                {
                    Destroy(slot);
                }
            }
            _slotInstances.Clear();

            // 创建新插槽
            var items = BackpackService.Instance.Items;
            for (int i = 0; i < Mathf.Min(items.Count, maxDisplaySlots); i++)
            {
                var item = items[i];
                var slotObj = Instantiate(slotPrefab, slotContainer);
                _slotInstances.Add(slotObj);

                // 设置插槽内容（根据实际UI实现调整）
                var iconImage = slotObj.GetComponentInChildren<UnityEngine.UI.Image>();
                if (iconImage != null && item.icon != null)
                {
                    iconImage.sprite = item.icon;
                }

                var nameText = slotObj.GetComponentInChildren<TextMeshProUGUI>();
                if (nameText != null)
                {
                    nameText.text = item.quantity > 1 ? $"{item.itemName} x{item.quantity}" : item.itemName;
                }
            }

            // 更新物品数量
            if (itemCountText != null)
            {
                itemCountText.text = $"{items.Count}/{BackpackService.Instance.MaxSlots}";
            }
        }

        private System.Collections.IEnumerator AnimateBackpack(bool open)
        {
            _isAnimating = true;
            float elapsed = 0f;

            while (elapsed < openAnimationDuration)
            {
                elapsed += Time.deltaTime;
                float t = openAnimationCurve.Evaluate(elapsed / openAnimationDuration);

                if (backpackPanel != null)
                {
                    var scale = backpackPanel.transform.localScale;
                    float targetScale = open ? 1f : 0.01f;
                    scale.x = Mathf.Lerp(backpackPanel.transform.localScale.x, targetScale, t);
                    scale.y = Mathf.Lerp(backpackPanel.transform.localScale.y, targetScale, t);
                    scale.z = Mathf.Lerp(backpackPanel.transform.localScale.z, targetScale, t);
                    backpackPanel.transform.localScale = scale;
                }

                yield return null;
            }

            if (!open && backpackPanel != null)
            {
                backpackPanel.SetActive(false);
                backpackPanel.transform.localScale = Vector3.one;
            }

            _isAnimating = false;
        }
    }
}
