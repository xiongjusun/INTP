using System;
using System.Collections.Generic;
using UnityEngine;
using INTP.Foundation;

namespace INTP.Core.StateMachine
{
    /// <summary>
    /// 玩家模式状态定义
    /// </summary>
    public enum PlayerModeState
    {
        FPS3DWalk,       // 第一人称3D步行 - 鼠标控制镜头，WASD移动
        FPS3DShip,       // 第一人称3D飞船 - WS朝屏幕中心前进后退，AD左右移动
        Plane2DCharacter // 2D平面小人 - WASD控制上下左右移动
    }

    /// <summary>
    /// 交互模式状态定义
    /// </summary>
    public enum InteractionState
    {
        Normal,              // 正常游戏 - 使用当前 PlayerMode 的控制
        UIInteraction,       // UI交互模式 - 禁用玩家移动，只允许UI交互
        BackpackOpen,        // 背包打开
        DraggingPlaceable,   // 拖拽放置物品
        DreamSelecting,      // 梦境选择
        VideoPlaying         // 视频播放中 - 禁用玩家移动
    }

    /// <summary>
    /// 玩家模式状态机
    /// </summary>
    public class PlayerModeStateMachine : MonoBehaviour
    {
        private PlayerModeState _currentMode = PlayerModeState.FPS3DWalk;
        private EventBus _eventBus;

        public PlayerModeState CurrentMode => _currentMode;

        public delegate void PlayerModeChangedDelegate(PlayerModeState newMode, PlayerModeState oldMode);
        public event PlayerModeChangedDelegate OnModeChanged;

        private void Awake()
        {
            _eventBus = EventBus.Instance;
        }

        /// <summary>
        /// 初始化初始模式（在系统启动时调用一次）
        /// </summary>
        public void InitializeMode(PlayerModeState initialMode)
        {
            if (initialMode != _currentMode)
            {
                _currentMode = initialMode;
                Debug.Log($"PlayerMode initialized to: {initialMode}");
                OnModeChanged?.Invoke(initialMode, initialMode);
                _eventBus.Publish(new PlayerModeChangedEvent { newMode = initialMode, oldMode = initialMode });
            }
        }

        /// <summary>
        /// 尝试切换玩家模式
        /// </summary>
        public bool TryTransitionMode(PlayerModeState newMode)
        {
            if (newMode == _currentMode)
                return false;

            var oldMode = _currentMode;
            _currentMode = newMode;

            Debug.Log($"PlayerMode: {oldMode} -> {newMode}");
            
            OnModeChanged?.Invoke(newMode, oldMode);
            _eventBus.Publish(new PlayerModeChangedEvent { newMode = newMode, oldMode = oldMode });

            return true;
        }

        /// <summary>
        /// 获取是否为FPS3D步行模式
        /// </summary>
        public bool IsFPS3DWalkMode => _currentMode == PlayerModeState.FPS3DWalk;

        /// <summary>
        /// 获取是否为FPS3D飞船模式
        /// </summary>
        public bool IsFPS3DShipMode => _currentMode == PlayerModeState.FPS3DShip;

        /// <summary>
        /// 获取是否为2D平面小人模式
        /// </summary>
        public bool IsPlane2DCharacterMode => _currentMode == PlayerModeState.Plane2DCharacter;
    }

    /// <summary>
    /// 交互模式状态机
    /// </summary>
    public class InteractionStateMachine : MonoBehaviour
    {
        private InteractionState _currentState = InteractionState.Normal;
        private EventBus _eventBus;

        public InteractionState CurrentState => _currentState;

        public delegate void InteractionStateChangedDelegate(InteractionState newState);
        public event InteractionStateChangedDelegate OnStateChanged;

        private void Awake()
        {
            _eventBus = EventBus.Instance;
        }

        /// <summary>
        /// 转换交互状态
        /// </summary>
        public bool TransitionTo(InteractionState newState)
        {
            if (newState == _currentState)
                return false;

            var oldState = _currentState;
            _currentState = newState;

            Debug.Log($"InteractionState: {oldState} -> {newState}");

            OnStateChanged?.Invoke(newState);
            _eventBus.Publish(new InteractionStateChangedEvent { newState = newState, oldState = oldState });

            return true;
        }

        /// <summary>
        /// 检查是否在普通模式（不在任何交互中）
        /// </summary>
        public bool IsNormal => _currentState == InteractionState.Normal;

        /// <summary>
        /// 检查是否可以切换模式（用于阻止非法切换）
        /// </summary>
        public bool CanSwitchPlayerMode => 
            _currentState == InteractionState.Normal || 
            _currentState == InteractionState.BackpackOpen;
    }

    /// <summary>
    /// 玩家模式改变事件
    /// </summary>
    public class PlayerModeChangedEvent : IGameEvent
    {
        public PlayerModeState oldMode;
        public PlayerModeState newMode;
    }

    /// <summary>
    /// 交互状态改变事件
    /// </summary>
    public class InteractionStateChangedEvent : IGameEvent
    {
        public InteractionState oldState;
        public InteractionState newState;
    }
}
