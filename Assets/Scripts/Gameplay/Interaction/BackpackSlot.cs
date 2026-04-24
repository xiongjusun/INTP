using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace INTP.Gameplay
{
    /// <summary>
    /// 背包物品槽位组件 - 处理物品显示、选中状态和拖拽
    /// </summary>
    public class BackpackSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
    {
        [Header("UI References")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI quantityText;
        [SerializeField] private Image selectionHighlight;
        
        [Header("Auto Find (Optional)")]
        [Tooltip("如果为空，会自动从子物体查找名为 Icon 的 Image")]
        [SerializeField] private bool autoFindChildUI = true;
        
        [Header("States")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color hoverColor = new Color(1f, 1f, 1f, 0.8f);
        [SerializeField] private Color selectedColor = new Color(1f, 0.9f, 0.5f, 1f);
        
        [Header("Animation")]
        [SerializeField] private float hoverScale = 1.1f;
        [SerializeField] private float scaleSpeed = 8f;
        
        private BackpackItem _item;
        private Transform _transformCache;
        private Vector3 _targetScale = Vector3.one;
        private bool _isSelected = false;
        private bool _isHovered = false;
        private float _baseScale = 1f;
        
        public BackpackItem Item => _item;
        public bool HasItem => _item != null;
        public bool IsSelected => _isSelected;
        
        public event System.Action<BackpackSlot> OnSlotClicked;
        public event System.Action<BackpackSlot> OnSlotDragStarted;
        public event System.Action<BackpackItem, Vector2> OnItemDragged;
        
        private void Awake()
        {
            _transformCache = transform;
            
            // 自动查找子物体 UI
            if (autoFindChildUI)
            {
                AutoFindChildUI();
            }
            
            _baseScale = _transformCache.localScale.x;
            if (_baseScale < 0.001f) _baseScale = 1f;
            _targetScale = Vector3.one * _baseScale;
        }
        
        private void AutoFindChildUI()
        {
            // 查找 Icon
            if (iconImage == null)
            {
                var icon = transform.Find("Icon");
                if (icon != null)
                {
                    iconImage = icon.GetComponent<Image>();
                }
            }
            
            // 查找 SelectionHighlight
            if (selectionHighlight == null)
            {
                var highlight = transform.Find("SelectionHighlight");
                if (highlight != null)
                {
                    selectionHighlight = highlight.GetComponent<Image>();
                }
            }
            
            // 查找 QuantityText
            if (quantityText == null)
            {
                var qty = transform.Find("QuantityText");
                if (qty != null)
                {
                    quantityText = qty.GetComponent<TextMeshProUGUI>();
                }
            }
        }
        
        private void Update()
        {
            // 平滑缩放动画
            Vector3 currentScale = _transformCache.localScale;
            Vector3 newScale = Vector3.Lerp(currentScale, _targetScale, Time.deltaTime * scaleSpeed);
            _transformCache.localScale = newScale;
        }
        
        /// <summary>
        /// 绑定物品数据到槽位
        /// </summary>
        public void BindItem(BackpackItem item)
        {
            _item = item;
            
            if (item != null)
            {
                if (iconImage != null)
                {
                    iconImage.sprite = item.icon;
                    iconImage.enabled = item.icon != null;
                }
                
                if (quantityText != null)
                {
                    if (item.quantity > 1)
                    {
                        quantityText.text = $"x{item.quantity}";
                        quantityText.gameObject.SetActive(true);
                    }
                    else
                    {
                        quantityText.gameObject.SetActive(false);
                    }
                }
                
                SetSlotActive(true);
            }
            else
            {
                ClearSlot();
            }
        }
        
        /// <summary>
        /// 清空槽位
        /// </summary>
        public void ClearSlot()
        {
            _item = null;
            
            if (iconImage != null)
            {
                iconImage.sprite = null;
                iconImage.enabled = false;
            }
            
            if (quantityText != null)
            {
                quantityText.gameObject.SetActive(false);
            }
            
            SetSlotActive(false);
        }
        
        /// <summary>
        /// 设置槽位是否激活（是否有物品）
        /// </summary>
        private void SetSlotActive(bool active)
        {
            gameObject.SetActive(active);
        }
        
        /// <summary>
        /// 设置选中状态
        /// </summary>
        public void SetSelected(bool selected)
        {
            _isSelected = selected;
            
            if (selectionHighlight != null)
            {
                selectionHighlight.gameObject.SetActive(selected);
            }
            
            // 选中时使用特殊颜色
            if (iconImage != null)
            {
                iconImage.color = selected ? selectedColor : normalColor;
            }
        }
        
        /// <summary>
        /// 获取槽位图标的世界坐标（用于拖拽时获取位置）
        /// </summary>
        public Vector3 GetIconWorldPosition()
        {
            if (iconImage != null)
            {
                return iconImage.transform.position;
            }
            return _transformCache.position;
        }
        
        // IPointerEnterHandler
        public void OnPointerEnter(PointerEventData eventData)
        {
            _isHovered = true;
            _targetScale = Vector3.one * (_baseScale * hoverScale);
            
            if (selectionHighlight != null && !_isSelected)
            {
                selectionHighlight.color = hoverColor;
                selectionHighlight.gameObject.SetActive(true);
            }
        }
        
        // IPointerExitHandler
        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovered = false;
            _targetScale = Vector3.one * _baseScale;
            
            if (selectionHighlight != null && !_isSelected)
            {
                selectionHighlight.gameObject.SetActive(false);
            }
        }
        
        // IPointerDownHandler
        public void OnPointerDown(PointerEventData eventData)
        {
            if (_item == null) return;
            
            // 单击选中，双击开始拖拽
            OnSlotClicked?.Invoke(this);
            
            // 如果是右键或双击，开始拖拽
            if (eventData.button == PointerEventData.InputButton.Right || eventData.clickCount >= 2)
            {
                StartDrag();
            }
        }
        
        /// <summary>
        /// 开始拖拽物品
        /// </summary>
        public void StartDrag()
        {
            if (_item == null) return;
            
            OnSlotDragStarted?.Invoke(this);
            OnItemDragged?.Invoke(_item, Input.mousePosition);
            
            Debug.Log($"[BackpackSlot] Started dragging item: {_item.itemName}");
        }
    }
}
