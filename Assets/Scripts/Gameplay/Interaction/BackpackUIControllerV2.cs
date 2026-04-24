using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using INTP.Foundation;

namespace INTP.Gameplay
{
    /// <summary>
    /// 新版背包UI控制器 V2
    /// - 动画由用户在Unity Animation窗口手动制作
    /// - Slot位置在Editor中预设
    /// - ExpandedPanel 常驻 Active，通过动画控制显隐
    /// </summary>
    public class BackpackUIControllerV2 : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("References")]
        [SerializeField] private RectTransform expandedPanel;
        [SerializeField] private Image scrollIndicatorUp;
        [SerializeField] private Image scrollIndicatorDown;
        [SerializeField] private TextMeshProUGUI pageIndicatorText;
        [SerializeField] private TextMeshProUGUI itemCountText;
        
        [Header("Slot Configuration - Editor Preset")]
        [Tooltip("在Editor中预设所有Slot的位置和数量")]
        [SerializeField] private List<BackpackSlot> presetSlots = new();
        
        [Header("Animation Triggers (for Animation Clips)")]
        [Tooltip("播放展开动画的触发器")]
        public string expandTrigger = "Expand";
        [Tooltip("播放收缩动画的触发器")]
        public string collapseTrigger = "Collapse";
        
        private Animator _animator;
        
        private int _currentPage = 0;
        private int _totalPages = 1;
        private bool _isExpanded = false;
        private bool _isMouseOver = false;
        private bool _useNewInputSystem = false;
        
        private List<BackpackItem> _displayItems = new();
        
        public bool IsExpanded => _isExpanded;

        private void Awake()
        {
            Debug.Log("[BackpackUIControllerV2] Awake called.");
            _useNewInputSystem = Mouse.current != null;
            Debug.Log($"[BackpackUIControllerV2] Using new Input System: {_useNewInputSystem}");
            
            _animator = expandedPanel?.GetComponent<Animator>();
            
            InitializeSlots();
        }

        private void Start()
        {
            Debug.Log($"[BackpackUIControllerV2] Start - EventBus.Instance = {EventBus.Instance != null}");
            
            if (EventBus.Instance != null)
            {
                EventBus.Instance.Subscribe<BackpackToggleEvent>(OnBackpackToggle);
                EventBus.Instance.Subscribe<InteractEvent>(OnInteract);
                EventBus.Instance.Subscribe<ItemAddedToBackpackEvent>(OnItemAdded);
            }
            
            // 初始状态：关闭
            SetBackpackOpen(false);
        }

        private void OnDestroy()
        {
            if (EventBus.Instance != null)
            {
                EventBus.Instance.Unsubscribe<BackpackToggleEvent>(OnBackpackToggle);
                EventBus.Instance.Unsubscribe<InteractEvent>(OnInteract);
                EventBus.Instance.Unsubscribe<ItemAddedToBackpackEvent>(OnItemAdded);
            }
        }

        private void Update()
        {
            if (_isExpanded)
            {
                HandleScrollInput();
            }
        }

        private void InitializeSlots()
        {
            foreach (var slot in presetSlots)
            {
                if (slot != null)
                {
                    slot.ClearSlot();
                    slot.gameObject.SetActive(false);
                    
                    slot.OnSlotClicked += OnSlotClicked;
                    slot.OnItemDragged += OnSlotItemDragged;
                }
            }
        }

        private void OnBackpackToggle(BackpackToggleEvent evt)
        {
            Debug.Log($"[BackpackUIControllerV2] OnBackpackToggle received: {evt.IsOpen}");
            SetBackpackOpen(evt.IsOpen);
        }

        private void OnInteract(InteractEvent evt)
        {
            if (_isExpanded)
            {
                SetBackpackOpen(false);
            }
        }

        private void OnItemAdded(ItemAddedToBackpackEvent evt)
        {
            if (_isExpanded)
            {
                RefreshUI();
            }
        }

        /// <summary>
        /// 设置背包开关状态
        /// </summary>
        public void SetBackpackOpen(bool open)
        {
            // 如果状态没变，不做处理
            if (_isExpanded == open) return;
            
            _isExpanded = open;
            
            if (open)
            {
                RefreshUI();
            }
            else
            {
                ClearAllSlots();
                _currentPage = 0;
            }
            
            // 触发 Animation 动画
            if (_animator != null)
            {
                _animator.SetBool(expandTrigger, open);
            }
            // ExpandedPanel 保持常驻 Active，由动画控制显隐
        }

        /// <summary>
        /// 切换背包状态
        /// </summary>
        public void ToggleBackpack()
        {
            SetBackpackOpen(!_isExpanded);
        }

        /// <summary>
        /// 刷新背包UI
        /// </summary>
        public void RefreshUI()
        {
            Debug.Log("[BackpackUIControllerV2] RefreshUI called.");
            
            if (BackpackService.Instance == null)
            {
                Debug.LogError("[BackpackUIControllerV2] BackpackService.Instance is null!");
                return;
            }

            _displayItems = BackpackService.Instance.Items.ToList();
            
            int maxVisibleSlots = GetMaxVisibleSlots();
            _totalPages = Mathf.CeilToInt((float)_displayItems.Count / maxVisibleSlots);
            _totalPages = Mathf.Max(1, _totalPages);
            _currentPage = Mathf.Clamp(_currentPage, 0, _totalPages - 1);
            
            UpdateSlotsDisplay();
            UpdatePageIndicator();
            UpdateScrollIndicators();
            UpdateItemCount();
        }

        private int GetMaxVisibleSlots()
        {
            return presetSlots.Count(slot => slot != null);
        }

        private void UpdateSlotsDisplay()
        {
            int maxSlots = GetMaxVisibleSlots();
            int startIndex = _currentPage * maxSlots;
            
            for (int i = 0; i < presetSlots.Count; i++)
            {
                var slot = presetSlots[i];
                if (slot == null) continue;
                
                int itemIndex = startIndex + i;
                
                if (itemIndex < _displayItems.Count)
                {
                    slot.BindItem(_displayItems[itemIndex]);
                    slot.gameObject.SetActive(true);
                }
                else
                {
                    slot.ClearSlot();
                    slot.gameObject.SetActive(false);
                }
            }
        }

        private void UpdatePageIndicator()
        {
            if (pageIndicatorText != null)
            {
                pageIndicatorText.gameObject.SetActive(_totalPages > 1);
                pageIndicatorText.text = $"{_currentPage + 1}/{_totalPages}";
            }
        }

        private void UpdateScrollIndicators()
        {
            if (scrollIndicatorUp != null)
            {
                scrollIndicatorUp.gameObject.SetActive(_currentPage > 0);
            }
            
            if (scrollIndicatorDown != null)
            {
                scrollIndicatorDown.gameObject.SetActive(_currentPage < _totalPages - 1);
            }
        }

        private void UpdateItemCount()
        {
            if (itemCountText != null && BackpackService.Instance != null)
            {
                var items = BackpackService.Instance.Items;
                itemCountText.text = $"{items.Count}/{BackpackService.Instance.MaxSlots}";
            }
        }

        private void HandleScrollInput()
        {
            float scrollInput = 0f;

            if (_useNewInputSystem)
            {
                scrollInput = Mouse.current?.scroll.ReadValue().y * 0.01f ?? 0f;
            }
            else
            {
                scrollInput = Input.GetAxis("Mouse ScrollWheel");
            }

            if (scrollInput > 0.01f)
            {
                if (_currentPage > 0)
                {
                    _currentPage--;
                    RefreshUI();
                }
            }
            else if (scrollInput < -0.01f)
            {
                if (_currentPage < _totalPages - 1)
                {
                    _currentPage++;
                    RefreshUI();
                }
            }
        }

        private void OnSlotClicked(BackpackSlot slot)
        {
            if (slot == null || !slot.HasItem) return;
            Debug.Log($"[BackpackUIControllerV2] Slot clicked: {slot.Item.itemName}");
        }

        private void OnSlotItemDragged(BackpackItem item, Vector2 position)
        {
            if (item == null) return;
            
            if (ItemDragHandler.Instance != null)
            {
                ItemDragHandler.Instance.StartDrag(item, position);
            }
        }

        #region Animation Callback
        
        /// <summary>
        /// Animation Event - 展开动画播放完毕时调用
        /// 在 Animation Clip 中添加 Animation Event 来触发
        /// </summary>
        public void OnExpandAnimationComplete()
        {
            Debug.Log("[BackpackUIControllerV2] Expand animation complete");
        }
        
        /// <summary>
        /// Animation Event - 收缩动画播放完毕时调用
        /// </summary>
        public void OnCollapseAnimationComplete()
        {
            Debug.Log("[BackpackUIControllerV2] Collapse animation complete");
            ClearAllSlots();
            _currentPage = 0;
        }
        
        #endregion

        private void ClearAllSlots()
        {
            foreach (var slot in presetSlots)
            {
                if (slot != null)
                {
                    slot.ClearSlot();
                    slot.gameObject.SetActive(false);
                }
            }
        }

        #region Public Methods

        public void NextPage()
        {
            if (_currentPage < _totalPages - 1)
            {
                _currentPage++;
                RefreshUI();
            }
        }

        public void PreviousPage()
        {
            if (_currentPage > 0)
            {
                _currentPage--;
                RefreshUI();
            }
        }

        public void OnBackgroundClick()
        {
            if (_isExpanded)
            {
                SetBackpackOpen(false);
            }
        }

        // IPointerEnterHandler / IPointerExitHandler
        public void OnPointerEnter(PointerEventData eventData)
        {
            _isMouseOver = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isMouseOver = false;
        }

        #endregion

        #region Editor Helpers

#if UNITY_EDITOR
        [ContextMenu("Auto-Find Slots")]
        private void AutoFindSlots()
        {
            if (expandedPanel == null) return;
            
            presetSlots.Clear();
            var slots = expandedPanel.GetComponentsInChildren<BackpackSlot>(true);
            presetSlots.AddRange(slots);
            
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"[BackpackUIControllerV2] Auto-found {presetSlots.Count} slots.");
        }
#endif

        #endregion
    }
}
