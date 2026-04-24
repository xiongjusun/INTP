using UnityEngine;
using INTP.Foundation;

namespace INTP.Gameplay
{
    /// <summary>
    /// 测试用收集物品 - 快速创建用于测试交互系统
    /// </summary>
    public class DebugCollectibleItem : CollectibleItem
    {
        [Header("Debug Settings")]
        [SerializeField] private Color itemColor = Color.yellow;
        [SerializeField] private float spinSpeed = 50f;
        [SerializeField] private float hoverHeight = 0.5f;
        [SerializeField] private float hoverSpeed = 2f;

        private Vector3 _startPos;

        protected override void Awake()
        {
            base.Awake();
            _startPos = transform.position;

            // 自动设置外观
            var renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = itemColor;
            }

            // 确保有碰撞器
            if (GetComponent<Collider2D>() == null)
            {
                gameObject.AddComponent<CircleCollider2D>().radius = 0.5f;
            }
        }

        private void Update()
        {
            // 旋转动画
            transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime);

            // 悬浮动画
            float heightOffset = Mathf.Sin(Time.time * hoverSpeed) * hoverHeight;
            transform.position = _startPos + Vector3.up * heightOffset;
        }

        protected override void OnCollected()
        {
            Debug.Log($"[Debug] Collected: {itemName} (ID: {ItemId})");
            base.OnCollected();
        }
    }

    /// <summary>
    /// 测试用视频交互物品 - 快速创建用于测试视频播放
    /// </summary>
    public class DebugVideoInteractable : VideoInteractable
    {
        [Header("Debug Settings")]
        [SerializeField] private Color screenColor = Color.cyan;
        [SerializeField] private GameObject screenPrefab;

        private GameObject _screenInstance;

        protected override void Awake()
        {
            base.Awake();

            // 自动设置外观
            var renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = screenColor;
            }

            // 确保有碰撞器
            if (GetComponent<Collider2D>() == null)
            {
                gameObject.AddComponent<CircleCollider2D>().radius = 0.8f;
            }
        }

        protected override void OnVideoPlay()
        {
            // 创建测试屏幕
            if (screenPrefab != null && _screenInstance == null)
            {
                _screenInstance = Instantiate(screenPrefab, transform.position + Vector3.up * 2f, Quaternion.identity);
                Debug.Log($"[Debug] Video screen created for: {videoTitle}");
            }

            Debug.Log($"[Debug] Video playing: {videoTitle}");
        }

        private void OnDestroy()
        {
            if (_screenInstance != null)
            {
                Destroy(_screenInstance);
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 0.8f);
            Gizmos.DrawIcon(transform.position + Vector3.up, "d_SceneViewFx.psd", true);
        }
    }
}
