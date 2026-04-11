using UnityEngine;
using INTP.Foundation;
using INTP.Core.StateMachine;

namespace INTP.Gameplay.Inventory
{
    /// <summary>
    /// 拖拽摆放验证器 - 验证物品是否可以在指定位置放置
    /// 参考plan.mb第4.2章节
    /// </summary>
    public class PlacementValidator : MonoBehaviour
    {
        [SerializeField] private LayerMask _planetMask = LayerMask.GetMask("Planet");
        [SerializeField] private float _maxSurfaceAngle = 45f;  // 最大坡度
        [SerializeField] private float _minDistanceFromOther = 2f; // 与其他物品的最小距离

        private EventBus _eventBus;

        private void Awake()
        {
            _eventBus = EventBus.Instance;
        }

        /// <summary>
        /// 检查位置是否在最大范围内
        /// </summary>
        public bool WithinMaxRange(Vector3 playerPos, Vector3 targetPos, float maxDistance)
        {
            float distance = Vector3.Distance(playerPos, targetPos);
            return distance <= maxDistance;
        }

        /// <summary>
        /// 验证放置位置（地表和碰撞）
        /// </summary>
        public bool ValidateSurfaceAndCollision(RaycastHit hit, ItemDef itemDef)
        {
            // 检查是否击中星球地表
            if ((1 << hit.collider.gameObject.layer & _planetMask) == 0)
                return false;

            // 检查地面坡度（法线与上方向的夹角）
            float angle = Vector3.Angle(hit.normal, Vector3.up);
            if (angle > _maxSurfaceAngle)
            {
                Debug.Log($"Surface too steep: {angle}°");
                return false;
            }

            // 检查是否与其他物体碰撞
            var colliders = Physics.OverlapSphere(hit.point, 1f);
            foreach (var collider in colliders)
            {
                if (collider != hit.collider)
                {
                    Debug.Log("Placement blocked by other object");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 获取最大放置距离
        /// </summary>
        public float GetMaxPlacementDistance(ItemDef itemDef)
        {
            return itemDef?.maxPlacementDistance ?? 50f;
        }
    }
}
