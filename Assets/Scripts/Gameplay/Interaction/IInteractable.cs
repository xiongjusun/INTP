using UnityEngine;
using INTP.Foundation;

namespace INTP.Gameplay
{
    /// <summary>
    /// 可交互物品接口
    /// 实现此接口的物体可以被玩家交互
    /// </summary>
    public interface IInteractable
    {
        /// <summary>
        /// 交互提示文本（显示在UI上）
        /// </summary>
        string InteractionPrompt { get; }

        /// <summary>
        /// 交互类型
        /// </summary>
        InteractableType InteractionType { get; }

        /// <summary>
        /// 物品ID（用于背包系统）
        /// </summary>
        string ItemId { get; }

        /// <summary>
        /// 是否可以交互
        /// </summary>
        bool CanInteract();

        /// <summary>
        /// 执行交互
        /// </summary>
        void Interact();
    }

    /// <summary>
    /// 可收集物品基类 - 实现此类的物体可以进入背包
    /// </summary>
    public abstract class CollectibleItem : MonoBehaviour, IInteractable
    {
        [Header("Collectible Settings")]
        [SerializeField] protected string itemId;
        [SerializeField] protected string itemName;
        [SerializeField] protected Sprite icon;
        [SerializeField] protected string promptText = "Press E to collect";

        public string InteractionPrompt => promptText;
        public InteractableType InteractionType => InteractableType.Collectible;
        public string ItemId => itemId;

        protected virtual void Awake()
        {
        }

        public virtual bool CanInteract()
        {
            return true;
        }

        public virtual void Interact()
        {
            Debug.Log($"[CollectibleItem] Interact called: {itemName} ({itemId})");
            
            // 直接添加到背包（双重保险：事件系统 + 直接调用）
            if (BackpackService.Instance != null)
            {
                BackpackService.Instance.AddItem(itemId, itemName, icon);
            }
            else
            {
                Debug.LogError("[CollectibleItem] BackpackService.Instance is null!");
            }
            
            // 发布事件（用于其他系统监听）
            EventBus.Instance.Publish(new InteractEvent(gameObject, InteractableType.Collectible, itemId));
            EventBus.Instance.Publish(new ItemAddedToBackpackEvent(itemId, itemName, icon));
            
            OnCollected();
            Destroy(gameObject);
        }

        /// <summary>
        /// 收集后执行的逻辑（可重写）
        /// </summary>
        protected virtual void OnCollected()
        {
            Debug.Log($"Collected item: {itemName}");
        }
    }

    /// <summary>
    /// 视频交互物品 - 实现此类的物体可以播放视频
    /// </summary>
    public abstract class VideoInteractable : MonoBehaviour, IInteractable
    {
        [Header("Video Settings")]
        [SerializeField] protected string itemId;
        [SerializeField] protected string videoTitle;
        [SerializeField] protected string promptText = "Press E to play video";

        public string InteractionPrompt => promptText;
        public InteractableType InteractionType => InteractableType.Video;
        public string ItemId => itemId;

        protected virtual void Awake()
        {
        }

        public virtual bool CanInteract()
        {
            return true;
        }

        public virtual void Interact()
        {
            EventBus.Instance.Publish(new InteractEvent(gameObject, InteractableType.Video, itemId));
            OnVideoPlay();
        }

        /// <summary>
        /// 视频播放逻辑（可重写）
        /// </summary>
        protected abstract void OnVideoPlay();
    }
}
