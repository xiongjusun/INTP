using UnityEngine;
using UnityEngine.EventSystems;
using INTP.Foundation;

namespace INTP.Gameplay
{
    /// <summary>
    /// 物品放置请求事件 - 从背包拖拽物品到3D场景
    /// </summary>
    public class ItemPlacementRequestEvent : IGameEvent
    {
        public BackpackItem Item { get; private set; }
        public Vector3 WorldPosition { get; private set; }
        public Vector2 ScreenPosition { get; private set; }
        
        public ItemPlacementRequestEvent(BackpackItem item, Vector3 worldPosition)
        {
            Item = item;
            WorldPosition = worldPosition;
            ScreenPosition = RectTransformUtility.WorldToScreenPoint(Camera.main, worldPosition);
        }
        
        public ItemPlacementRequestEvent(BackpackItem item, Vector2 screenPosition)
        {
            Item = item;
            ScreenPosition = screenPosition;
            WorldPosition = Vector3.zero;
        }
    }
    
    /// <summary>
    /// 物品拖拽状态变更事件
    /// </summary>
    public class ItemDragStateChangedEvent : IGameEvent
    {
        public BackpackItem Item { get; private set; }
        public bool IsDragging { get; private set; }
        public Vector2 CurrentPosition { get; private set; }
        
        public ItemDragStateChangedEvent(BackpackItem item, bool isDragging, Vector2 position)
        {
            Item = item;
            IsDragging = isDragging;
            CurrentPosition = position;
        }
    }
    
    /// <summary>
    /// 物品拖拽处理器 - 管理从背包到3D场景的物品拖拽
    /// </summary>
    public class ItemDragHandler : MonoBehaviour
    {
        public static ItemDragHandler Instance { get; private set; }
        
        [Header("Drag Visual")]
        [SerializeField] private RectTransform dragIconPrefab;
        [SerializeField] private Canvas parentCanvas;
        
        private RectTransform _dragIcon;
        private BackpackItem _currentDragItem;
        private bool _isDragging = false;
        private Camera _uiCamera;
        
        public bool IsDragging => _isDragging;
        public BackpackItem CurrentDragItem => _currentDragItem;
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }
        
        private void Start()
        {
            if (parentCanvas != null)
            {
                _uiCamera = parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay 
                    ? null 
                    : parentCanvas.worldCamera;
            }
            
            // 订阅事件
            if (EventBus.Instance != null)
            {
                EventBus.Instance.Subscribe<ItemPlacementRequestEvent>(OnPlacementRequest);
            }
            
            // 创建拖拽图标
            CreateDragIcon();
        }
        
        private void OnDestroy()
        {
            if (EventBus.Instance != null)
            {
                EventBus.Instance.Unsubscribe<ItemPlacementRequestEvent>(OnPlacementRequest);
            }
        }
        
        private void Update()
        {
            if (_isDragging)
            {
                UpdateDragIconPosition();
                HandleDragInput();
            }
        }
        
        /// <summary>
        /// 开始拖拽物品
        /// </summary>
        public void StartDrag(BackpackItem item, Vector2 startPosition)
        {
            if (_isDragging) return;
            
            _currentDragItem = item;
            _isDragging = true;
            
            if (_dragIcon != null)
            {
                _dragIcon.gameObject.SetActive(true);
                UpdateDragIconPosition();
                
                // 设置拖拽图标内容
                var iconImage = _dragIcon.GetComponentInChildren<UnityEngine.UI.Image>();
                if (iconImage != null && item.icon != null)
                {
                    iconImage.sprite = item.icon;
                }
            }
            
            EventBus.Instance?.Publish(new ItemDragStateChangedEvent(item, true, startPosition));
            Debug.Log($"[ItemDragHandler] Started dragging: {item.itemName}");
        }
        
        /// <summary>
        /// 结束拖拽
        /// </summary>
        public void EndDrag()
        {
            if (!_isDragging) return;
            
            if (_dragIcon != null)
            {
                _dragIcon.gameObject.SetActive(false);
            }
            
            EventBus.Instance?.Publish(new ItemDragStateChangedEvent(_currentDragItem, false, Input.mousePosition));
            Debug.Log($"[ItemDragHandler] Ended dragging: {_currentDragItem?.itemName}");
            
            _currentDragItem = null;
            _isDragging = false;
        }
        
        /// <summary>
        /// 取消拖拽（物品放回背包）
        /// </summary>
        public void CancelDrag()
        {
            if (!_isDragging) return;
            
            Debug.Log($"[ItemDragHandler] Cancelled dragging: {_currentDragItem?.itemName}");
            
            if (_dragIcon != null)
            {
                _dragIcon.gameObject.SetActive(false);
            }
            
            _currentDragItem = null;
            _isDragging = false;
        }
        
        /// <summary>
        /// 获取当前拖拽的屏幕位置
        /// </summary>
        public Vector2 GetCurrentScreenPosition()
        {
            return Input.mousePosition;
        }
        
        /// <summary>
        /// 获取当前拖拽的UI位置（相对于Canvas）
        /// </summary>
        public Vector3 GetCurrentUIPosition()
        {
            Vector2 localPoint;
            if (parentCanvas != null)
            {
                RectTransform canvasRect = parentCanvas.transform as RectTransform;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect, Input.mousePosition, _uiCamera, out localPoint))
                {
                    return localPoint;
                }
            }
            return _dragIcon?.localPosition ?? Vector3.zero;
        }
        
        /// <summary>
        /// 检查当前拖拽位置是否在3D场景区域（不在UI上）
        /// </summary>
        public bool IsOver3DScene()
        {
            // 使用 EventSystem.current 来判断当前是否hover在UI上
            if (EventSystem.current == null) return true;
            
            PointerEventData pointerData = new PointerEventData(EventSystem.current)
            {
                position = Input.mousePosition
            };
            
            var raycastResults = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, raycastResults);
            
            // 如果没有任何UI元素被命中，说明在3D场景上
            return raycastResults.Count == 0;
        }
        
        private void CreateDragIcon()
        {
            if (dragIconPrefab == null || parentCanvas == null) return;
            
            _dragIcon = Instantiate(dragIconPrefab, parentCanvas.transform);
            _dragIcon.gameObject.SetActive(false);
            
            // 设置初始位置
            _dragIcon.position = Input.mousePosition;
        }
        
        private void UpdateDragIconPosition()
        {
            if (_dragIcon == null) return;
            
            Vector2 screenPos = Input.mousePosition;
            _dragIcon.position = screenPos;
        }
        
        private void HandleDragInput()
        {
            // 左键释放 - 尝试放置物品
            if (Input.GetMouseButtonUp(0))
            {
                TryPlaceItem();
            }
            
            // 右键或ESC取消拖拽
            if (Input.GetMouseButtonUp(1) || Input.GetKeyDown(KeyCode.Escape))
            {
                CancelDrag();
            }
        }
        
        private void TryPlaceItem()
        {
            if (_currentDragItem == null) return;
            
            // 检查是否在3D场景区域
            if (IsOver3DScene())
            {
                // 发布放置请求事件
                EventBus.Instance?.Publish(new ItemPlacementRequestEvent(_currentDragItem, Input.mousePosition));
                EndDrag();
            }
            else
            {
                // 在UI上释放，取消拖拽
                Debug.Log("[ItemDragHandler] Dropped on UI, cancelling...");
                CancelDrag();
            }
        }
        
        private void OnPlacementRequest(ItemPlacementRequestEvent evt)
        {
            Debug.Log($"[ItemDragHandler] Placement requested for: {evt.Item.itemName} at {evt.WorldPosition}");
            // 这里可以添加实际的3D放置逻辑
            // 例如：从BackpackService移除物品，实例化3D物体
            
            // 通知BackpackService移除物品（如果放置成功）
            // BackpackService.Instance?.RemoveItem(evt.Item.itemId);
        }
    }
}
