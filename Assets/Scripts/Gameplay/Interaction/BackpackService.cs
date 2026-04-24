using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using INTP.Foundation;

namespace INTP.Gameplay
{
    /// <summary>
    /// 背包物品数据
    /// </summary>
    [System.Serializable]
    public class BackpackItem
    {
        public string itemId;
        public string itemName;
        public Sprite icon;
        public int quantity = 1;

        public BackpackItem(string id, string name, Sprite sprite, int qty = 1)
        {
            itemId = id;
            itemName = name;
            icon = sprite;
            quantity = qty;
        }
    }

    /// <summary>
    /// 背包服务 - 管理玩家背包数据
    /// </summary>
    public class BackpackService : MonoBehaviour
    {
        public static BackpackService Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private int maxSlots = 20;

        [Header("UI Reference")]
        [SerializeField] private GameObject backpackPanel;

        private List<BackpackItem> _items = new();
        private bool _isOpen = false;

        public bool IsOpen => _isOpen;
        public int ItemCount => _items.Count;
        public int MaxSlots => maxSlots;
        public IReadOnlyList<BackpackItem> Items => _items;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (backpackPanel != null)
            {
                backpackPanel.SetActive(false);
            }
        }

        private void OnEnable()
        {
            EventBus.Instance.Subscribe<BackpackToggleEvent>(OnBackpackToggle);
            EventBus.Instance.Subscribe<ItemAddedToBackpackEvent>(OnItemAdded);
        }

        private void OnDisable()
        {
            if (EventBus.Instance != null)
            {
                EventBus.Instance.Unsubscribe<BackpackToggleEvent>(OnBackpackToggle);
                EventBus.Instance.Unsubscribe<ItemAddedToBackpackEvent>(OnItemAdded);
            }
        }

        private void OnBackpackToggle(BackpackToggleEvent evt)
        {
            SetBackpackOpen(evt.IsOpen);
        }

        private void OnItemAdded(ItemAddedToBackpackEvent evt)
        {
            AddItem(evt.ItemId, evt.ItemName, evt.Icon);
        }

        /// <summary>
        /// 切换背包开关状态
        /// </summary>
        public void ToggleBackpack()
        {
            SetBackpackOpen(!_isOpen);
        }

        /// <summary>
        /// 设置背包开关状态
        /// </summary>
        public void SetBackpackOpen(bool open)
        {
            _isOpen = open;

            if (backpackPanel != null)
            {
                backpackPanel.SetActive(open);
            }
        }

        /// <summary>
        /// 添加物品到背包
        /// </summary>
        public bool AddItem(string itemId, string itemName, Sprite icon = null, int quantity = 1)
        {
            Debug.Log($"[BackpackService] AddItem called: {itemId}, {itemName}");
            
            if (_items.Count >= maxSlots)
            {
                Debug.LogWarning($"Backpack is full! Max slots: {maxSlots}");
                return false;
            }

            // 检查是否已存在同类物品（可堆叠逻辑）
            var existingItem = _items.Find(item => item.itemId == itemId);
            if (existingItem != null)
            {
                existingItem.quantity += quantity;
                Debug.Log($"Stacked item: {itemName} x{quantity}");
                return true;
            }

            var newItem = new BackpackItem(itemId, itemName, icon, quantity);
            _items.Add(newItem);
            Debug.Log($"Added item to backpack: {itemName}, Total items: {_items.Count}");
            return true;
        }

        /// <summary>
        /// 移除物品
        /// </summary>
        public bool RemoveItem(string itemId, int quantity = 1)
        {
            var item = _items.Find(i => i.itemId == itemId);
            if (item == null)
            {
                Debug.LogWarning($"Item not found in backpack: {itemId}");
                return false;
            }

            item.quantity -= quantity;
            if (item.quantity <= 0)
            {
                _items.Remove(item);
            }
            return true;
        }

        /// <summary>
        /// 检查是否包含物品
        /// </summary>
        public bool HasItem(string itemId)
        {
            return _items.Exists(item => item.itemId == itemId);
        }

        /// <summary>
        /// 获取物品数量
        /// </summary>
        public int GetItemCount(string itemId)
        {
            var item = _items.Find(i => i.itemId == itemId);
            return item?.quantity ?? 0;
        }

        /// <summary>
        /// 清空背包
        /// </summary>
        public void ClearBackpack()
        {
            _items.Clear();
        }
    }
}
