using System.Collections.Generic;
using UnityEngine;
using INTP.Foundation;

namespace INTP.Gameplay.Inventory
{
    /// <summary>
    /// 物品定义
    /// </summary>
    [System.Serializable]
    public class ItemDef
    {
        public string id;
        public string name;
        public Sprite icon;
        public int maxStackSize = 1;
        public float maxPlacementDistance = 50f;
    }

    /// <summary>
    /// 背包槽位数据
    /// </summary>
    [System.Serializable]
    public class InventorySlot
    {
        public ItemDef itemDef;
        public int quantity;

        public InventorySlot(ItemDef item, int count = 1)
        {
            itemDef = item;
            quantity = count;
        }
    }

    /// <summary>
    /// 背包服务 - 管理玩家物品和槽位
    /// 参考plan.mb第4章节
    /// </summary>
    public class InventoryService : MonoBehaviour
    {
        private static InventoryService _instance;
        
        [SerializeField] private int _slotCount = 12;
        private List<InventorySlot> _slots = new();
        private EventBus _eventBus;

        public static InventoryService Instance
        {
            get
            {
                if (_instance == null)
                {
                    var obj = new GameObject("InventoryService");
                    _instance = obj.AddComponent<InventoryService>();
                    DontDestroyOnLoad(obj);
                }
                return _instance;
            }
        }

        public delegate void InventoryChangedDelegate();
        public event InventoryChangedDelegate OnInventoryChanged;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            _eventBus = EventBus.Instance;
            InitializeSlots();
        }

        /// <summary>
        /// 初始化背包槽位
        /// </summary>
        private void InitializeSlots()
        {
            _slots.Clear();
            for (int i = 0; i < _slotCount; i++)
            {
                _slots.Add(null);
            }
        }

        /// <summary>
        /// 添加物品到背包
        /// </summary>
        public bool AddItem(ItemDef itemDef, int quantity = 1)
        {
            if (itemDef == null || quantity <= 0)
                return false;

            // 优先寻找已有同物品的槽位
            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i] != null && _slots[i].itemDef.id == itemDef.id)
                {
                    if (_slots[i].quantity < itemDef.maxStackSize)
                    {
                        int canAdd = itemDef.maxStackSize - _slots[i].quantity;
                        int toAdd = Mathf.Min(canAdd, quantity);
                        _slots[i].quantity += toAdd;
                        quantity -= toAdd;

                        if (quantity == 0)
                        {
                            OnInventoryChanged?.Invoke();
                            _eventBus.Publish(new ItemAddedEvent { itemDef = itemDef, quantity = toAdd });
                            return true;
                        }
                    }
                }
            }

            // 寻找空槽位
            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i] == null)
                {
                    _slots[i] = new InventorySlot(itemDef, Mathf.Min(quantity, itemDef.maxStackSize));
                    quantity -= _slots[i].quantity;

                    if (quantity == 0)
                    {
                        OnInventoryChanged?.Invoke();
                        _eventBus.Publish(new ItemAddedEvent { itemDef = itemDef, quantity = _slots[i].quantity });
                        return true;
                    }
                }
            }

            return false; // 背包已满
        }

        /// <summary>
        /// 消耗物品（拖拽摆放时使用）
        /// </summary>
        public bool ConsumeItem(string itemId, int quantity = 1)
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i] != null && _slots[i].itemDef.id == itemId)
                {
                    if (_slots[i].quantity >= quantity)
                    {
                        _slots[i].quantity -= quantity;
                        if (_slots[i].quantity == 0)
                        {
                            _slots[i] = null;
                        }
                        OnInventoryChanged?.Invoke();
                        _eventBus.Publish(new ItemConsumedEvent { itemId = itemId, quantity = quantity });
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 获取物品数量
        /// </summary>
        public int GetItemQuantity(string itemId)
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i] != null && _slots[i].itemDef.id == itemId)
                {
                    return _slots[i].quantity;
                }
            }
            return 0;
        }

        /// <summary>
        /// 获取所有槽位
        /// </summary>
        public List<InventorySlot> GetAllSlots() => new List<InventorySlot>(_slots);

        /// <summary>
        /// 获取槽位总数
        /// </summary>
        public int GetSlotCount() => _slotCount;

        /// <summary>
        /// 清空背包
        /// </summary>
        public void Clear()
        {
            _slots.Clear();
            InitializeSlots();
            OnInventoryChanged?.Invoke();
        }
    }

    // ============ 背包事件 ============

    public class ItemAddedEvent : IGameEvent
    {
        public ItemDef itemDef;
        public int quantity;
    }

    public class ItemConsumedEvent : IGameEvent
    {
        public string itemId;
        public int quantity;
    }
}
