using UnityEngine;

namespace INTP.Foundation
{
    /// <summary>
    /// 可交互物品类型
    /// </summary>
    public enum InteractableType
    {
        Collectible,  // 可收集物品，进入背包
        Video         // 视频物品，播放视频
    }

    /// <summary>
    /// 交互事件 - 当玩家与可交互物品交互时触发
    /// </summary>
    public class InteractEvent : IGameEvent
    {
        public GameObject InteractableObject { get; private set; }
        public InteractableType InteractableType { get; private set; }
        public string ItemId { get; private set; }

        public InteractEvent(GameObject interactable, InteractableType type, string itemId = "")
        {
            InteractableObject = interactable;
            InteractableType = type;
            ItemId = itemId;
        }
    }

    /// <summary>
    /// 背包开关事件 - 控制背包UI的打开/关闭
    /// </summary>
    public class BackpackToggleEvent : IGameEvent
    {
        public bool IsOpen { get; private set; }

        public BackpackToggleEvent(bool isOpen)
        {
            IsOpen = isOpen;
        }
    }

    /// <summary>
    /// 物品添加到背包事件
    /// </summary>
    public class ItemAddedToBackpackEvent : IGameEvent
    {
        public string ItemId { get; private set; }
        public string ItemName { get; private set; }
        public Sprite Icon { get; private set; }

        public ItemAddedToBackpackEvent(string itemId, string itemName, Sprite icon = null)
        {
            ItemId = itemId;
            ItemName = itemName;
            Icon = icon;
        }
    }

    /// <summary>
    /// 可交互物品变更事件 - 通知UI显示交互提示
    /// </summary>
    public class InteractableChangedEvent : IGameEvent
    {
        public string PromptText { get; private set; }
        public InteractableType InteractableType { get; private set; }
        public bool HasInteractable { get; private set; }

        public InteractableChangedEvent(string promptText, InteractableType type, bool hasInteractable)
        {
            PromptText = promptText;
            InteractableType = type;
            HasInteractable = hasInteractable;
        }
    }
}
