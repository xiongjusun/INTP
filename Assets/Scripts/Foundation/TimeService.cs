using UnityEngine;

namespace INTP.Foundation
{
    /// <summary>
    /// 时间服务 - 管理游戏速度和时间流逝
    /// </summary>
    public class TimeService : MonoBehaviour
    {
        private static TimeService _instance;
        private float _baseTimeScale = 1f;
        private bool _isPaused = false;

        public static TimeService Instance
        {
            get
            {
                if (_instance == null)
                {
                    var obj = new GameObject("TimeService");
                    _instance = obj.AddComponent<TimeService>();
                    DontDestroyOnLoad(obj);
                }
                return _instance;
            }
        }

        /// <summary>
        /// 获取当前时间缩放
        /// </summary>
        public float TimeScale => Time.timeScale;

        /// <summary>
        /// 设置时间缩放（不影响暂停状态）
        /// </summary>
        public void SetTimeScale(float scale)
        {
            _baseTimeScale = Mathf.Max(0, scale);
            UpdateTimeScale();
        }

        /// <summary>
        /// 暂停游戏
        /// </summary>
        public void Pause()
        {
            _isPaused = true;
            UpdateTimeScale();
        }

        /// <summary>
        /// 恢复游戏
        /// </summary>
        public void Resume()
        {
            _isPaused = false;
            UpdateTimeScale();
        }

        /// <summary>
        /// 切换暂停状态
        /// </summary>
        public void TogglePause()
        {
            if (_isPaused)
                Resume();
            else
                Pause();
        }

        /// <summary>
        /// 获取暂停状态
        /// </summary>
        public bool IsPaused => _isPaused;

        private void UpdateTimeScale()
        {
            Time.timeScale = _isPaused ? 0f : _baseTimeScale;
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f;
        }
    }
}
