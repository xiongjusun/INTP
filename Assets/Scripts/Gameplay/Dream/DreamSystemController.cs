﻿using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using INTP.Foundation;

namespace INTP.Gameplay.Dream
{
    /// <summary>
    /// 梦境视频定义 - 配置梦境信息
    /// 参考plan.mb第5.2章节
    /// </summary>
    [CreateAssetMenu(fileName = "DreamVideoDef", menuName = "INTP/Dream/DreamVideoDef")]
    public class DreamVideoDefSO : ScriptableObject
    {
        public string dreamId;
        public string dreamName;
        [TextArea(2, 4)]
        public string description;
        
        [SerializeField] private VideoClip _videoClip;
        [SerializeField] private List<CaptureWindow> _captureWindows = new();
        [SerializeField] private List<CapturableItem> _captureCandidates = new();

        [SerializeField] private bool _unlocked = false;

        public VideoClip VideoClip => _videoClip;
        public List<CaptureWindow> CaptureWindows => _captureWindows;
        public List<CapturableItem> CaptureCandidates => _captureCandidates;
        public bool IsUnlocked => _unlocked;

        /// <summary>
        /// 检查在指定时间是否可以捕捉元素
        /// </summary>
        public bool CanCaptureAtTime(float currentTime, out CapturableItem item)
        {
            item = null;

            // 检查是否在任何捕捉窗口内
            foreach (var window in _captureWindows)
            {
                if (currentTime >= window.startTime && currentTime <= window.endTime)
                {
                    // 获取该窗口内可捕捉的元素
                    foreach (var candidate in _captureCandidates)
                    {
                        if (candidate.availableInWindow == window)
                        {
                            item = candidate;
                            return true;
                        }
                    }
                    return false;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// 捕捉窗口 - 定义在梦境中何时可以捕捉元素
    /// </summary>
    [System.Serializable]
    public class CaptureWindow
    {
        public string windowName;
        public float startTime;
        public float endTime;
    }

    /// <summary>
    /// 可捕捉物品
    /// </summary>
    [System.Serializable]
    public class CapturableItem
    {
        public string itemId;
        public string itemName;
        public Sprite itemIcon;
        public int quantity = 1;
        public CaptureWindow availableInWindow;
    }

    /// <summary>
    /// 梦境系统控制器 - 管理梦境视频播放和元素捕捉
    /// 参考plan.mb第5.3章节
    /// </summary>
    public class DreamSystemController : MonoBehaviour
    {
        [SerializeField] private VideoPlayer _videoPlayer;
        [SerializeField] private List<DreamVideoDefSO> _availableDreams = new();

        private DreamVideoDefSO _currentDream;
        private EventBus _eventBus;
        private bool _isPlaying = false;

        private void Awake()
        {
            _eventBus = EventBus.Instance;
            
            if (_videoPlayer == null)
                _videoPlayer = GetComponent<VideoPlayer>();
        }

        /// <summary>
        /// 开始播放梦境视频
        /// </summary>
        public void PlayDream(DreamVideoDefSO dreamDef)
        {
            if (dreamDef == null || !dreamDef.IsUnlocked)
                return;

            _currentDream = dreamDef;
            _isPlaying = true;

            if (_videoPlayer != null && dreamDef.VideoClip != null)
            {
                _videoPlayer.clip = dreamDef.VideoClip;
                _videoPlayer.Play();
                Debug.Log($"Playing dream: {dreamDef.dreamName}");
                _eventBus.Publish(new DreamVideoStartedEvent { dreamDef = dreamDef });
            }
        }

        /// <summary>
        /// 停止播放梦境
        /// </summary>
        public void StopDream()
        {
            if (_videoPlayer != null)
                _videoPlayer.Stop();

            _isPlaying = false;
            _eventBus.Publish(new DreamVideoStoppedEvent { });
            Debug.Log("Dream stopped");
        }

        /// <summary>
        /// 尝试在当前时间捕捉元素
        /// </summary>
        public bool TryCaptureElement()
        {
            if (_currentDream == null || !_isPlaying)
                return false;

            float currentTime = (float)_videoPlayer.time;

            if (_currentDream.CanCaptureAtTime(currentTime, out CapturableItem item))
            {
                _eventBus.Publish(new DreamElementCapturedEvent { itemId = item.itemId, quantity = item.quantity });
                Debug.Log($"Captured: {item.itemName}");
                return true;
            }

            return false;
        }

        /// <summary>
        /// 获取当前梦境
        /// </summary>
        public DreamVideoDefSO GetCurrentDream() => _currentDream;

        /// <summary>
        /// 获取可用梦境列表
        /// </summary>
        public List<DreamVideoDefSO> GetAvailableDreams()
        {
            var available = new List<DreamVideoDefSO>();
            foreach (var dream in _availableDreams)
            {
                if (dream.IsUnlocked)
                    available.Add(dream);
            }
            return available;
        }

        /// <summary>
        /// 检查是否正在播放梦境
        /// </summary>
        public bool IsPlayingDream => _isPlaying;
    }

    // ============ 梦境事件 ============

    public class DreamVideoStartedEvent : IGameEvent
    {
        public DreamVideoDefSO dreamDef;
    }

    public class DreamVideoStoppedEvent : IGameEvent { }

    public class DreamElementCapturedEvent : IGameEvent
    {
        public string itemId;
        public int quantity;
    }
}
