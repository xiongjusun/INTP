using System;
using System.Collections.Generic;
using UnityEngine;

namespace INTP.Foundation
{
    /// <summary>
    /// 全局事件总线 - 用于系统间解耦通讯
    /// </summary>
    public class EventBus : MonoBehaviour
    {
        private static EventBus _instance;
        private static bool _applicationIsQuitting;
        private Dictionary<Type, List<Delegate>> _subscribers = new();

        public static EventBus Instance
        {
            get
            {
                if (_applicationIsQuitting)
                {
                    return null;
                }

                if (_instance == null)
                {
                    var obj = new GameObject("EventBus");
                    _instance = obj.AddComponent<EventBus>();
                    DontDestroyOnLoad(obj);
                }
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _subscribers.Clear();
                _instance = null;
            }
        }

        private void OnApplicationQuit()
        {
            _applicationIsQuitting = true;
        }

        /// <summary>
        /// 订阅事件
        /// </summary>
        public void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : IGameEvent
        {
            var eventType = typeof(TEvent);
            if (!_subscribers.ContainsKey(eventType))
            {
                _subscribers[eventType] = new List<Delegate>();
            }
            _subscribers[eventType].Add(handler);
        }

        /// <summary>
        /// 取消订阅事件
        /// </summary>
        public void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : IGameEvent
        {
            var eventType = typeof(TEvent);
            if (_subscribers.ContainsKey(eventType))
            {
                _subscribers[eventType].Remove(handler);
            }
        }

        /// <summary>
        /// 发布事件
        /// </summary>
        public void Publish<TEvent>(TEvent gameEvent) where TEvent : IGameEvent
        {
            var eventType = typeof(TEvent);
            if (_subscribers.ContainsKey(eventType))
            {
                foreach (var subscriber in _subscribers[eventType])
                {
                    try
                    {
                        ((Action<TEvent>)subscriber)?.Invoke(gameEvent);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error publishing event {eventType.Name}: {ex}");
                    }
                }
            }
        }
    }

    /// <summary>
    /// 所有游戏事件的标记接口
    /// </summary>
    public interface IGameEvent { }
}
