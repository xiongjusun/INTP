using UnityEngine;
using INTP.Foundation;

namespace INTP.Gameplay.Inventory
{
    /// <summary>
    /// 星球射线检测服务 - 检测射线与星球地表的交点
    /// 参考plan.mb第4.2章节
    /// </summary>
    public class PlanetRaycastService : MonoBehaviour
    {
        [SerializeField] private LayerMask _planetMask = LayerMask.GetMask("Planet");
        [SerializeField] private float _maxRayDistance = 1000f;

        private Camera _mainCamera;
        private EventBus _eventBus;

        private void Awake()
        {
            _mainCamera = Camera.main;
            _eventBus = EventBus.Instance;
        }

        /// <summary>
        /// 从鼠标位置射出射线并检测星球地表
        /// </summary>
        public RaycastHit? RaycastFromCursor(LayerMask mask)
        {
            if (_mainCamera == null)
                return null;

            Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, _maxRayDistance, mask))
            {
                return hit;
            }

            return null;
        }

        /// <summary>
        /// 从屏幕中心射出射线并检测星球地表
        /// （用于飞行器模式的拖拽摆放）
        /// </summary>
        public RaycastHit? RaycastFromScreenCenter(LayerMask mask)
        {
            if (_mainCamera == null)
                return null;

            Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, 0);
            Ray ray = _mainCamera.ScreenPointToRay(screenCenter);
            if (Physics.Raycast(ray, out RaycastHit hit, _maxRayDistance, mask))
            {
                return hit;
            }

            return null;
        }

        /// <summary>
        /// 从指定位置和方向射出射线
        /// </summary>
        public RaycastHit? Raycast(Vector3 origin, Vector3 direction, LayerMask mask)
        {
            if (Physics.Raycast(origin, direction, out RaycastHit hit, _maxRayDistance, mask))
            {
                return hit;
            }

            return null;
        }

        /// <summary>
        /// 设置射线最大距离
        /// </summary>
        public void SetMaxRayDistance(float distance)
        {
            _maxRayDistance = distance;
        }
    }
}
