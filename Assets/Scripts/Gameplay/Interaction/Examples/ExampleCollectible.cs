using UnityEngine;
using INTP.Foundation;

namespace INTP.Gameplay
{
    /// <summary>
    /// 示例可收集物品
    /// 创建方式：在 Unity 中右键 Create > INTP > Gameplay > Collectible Item
    /// </summary>
    public class ExampleCollectible : CollectibleItem
    {
        [Header("Example Settings")]
        [SerializeField] private ParticleSystem collectEffect;
        [SerializeField] private AudioClip collectSound;

        protected override void Awake()
        {
            base.Awake();

            if (GetComponent<Collider2D>() == null)
            {
                var col = gameObject.AddComponent<CircleCollider2D>();
                col.radius = 0.5f;
                col.isTrigger = true;
            }
        }

        protected override void OnCollected()
        {
            // 播放收集特效
            if (collectEffect != null)
            {
                var effect = Instantiate(collectEffect, transform.position, Quaternion.identity);
                effect.Play();
                Destroy(effect.gameObject, effect.main.duration);
            }

            // 播放收集音效
            if (collectSound != null)
            {
                AudioSource.PlayClipAtPoint(collectSound, transform.position);
            }

            base.OnCollected();
        }
    }
}
