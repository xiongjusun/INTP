﻿using UnityEngine;
using INTP.Foundation;
using INTP.Core.StateMachine;

namespace INTP.Core.Input
{
    /// <summary>
    /// 模式切换守卫 - 防止在非法条件下进行模式切换
    /// 参考plan.mb第2.3章节
    /// </summary>
    public class ModeSwitchGuard : MonoBehaviour
    {
        private GameFlowStateMachine _gameFlowStateMachine;
        private PlayerModeStateMachine _playerModeStateMachine;
        private InteractionStateMachine _interactionStateMachine;
        private EventBus _eventBus;
        private PlayerModeState _last3DMode = PlayerModeState.FPS3DWalk;

        // 可配置的条件检查（留给之后的系统扩展）
        private bool _isVehicleAvailable = true;  // 飞行器是否可用（未损坏、有能量等）
        private bool _isAnimationPlaying = false; // 是否有动画正在播放

        private void Awake()
        {
            _gameFlowStateMachine = FindObjectOfType<GameFlowStateMachine>();
            _playerModeStateMachine = FindObjectOfType<PlayerModeStateMachine>();
            _interactionStateMachine = FindObjectOfType<InteractionStateMachine>();
            _eventBus = EventBus.Instance;
        }

        // 防止 Awake 时序导致内部引用为空。
        private bool EnsureReferences()
        {
            if (_gameFlowStateMachine == null)
                _gameFlowStateMachine = FindObjectOfType<GameFlowStateMachine>();
            if (_playerModeStateMachine == null)
                _playerModeStateMachine = FindObjectOfType<PlayerModeStateMachine>();
            if (_interactionStateMachine == null)
                _interactionStateMachine = FindObjectOfType<InteractionStateMachine>();
            if (_eventBus == null)
                _eventBus = EventBus.Instance;

            return _gameFlowStateMachine != null &&
                   _playerModeStateMachine != null &&
                   _interactionStateMachine != null;
        }

        /// <summary>
        /// 检查是否可以切换模式
        /// </summary>
        public bool CanSwitchMode()
        {
            if (!EnsureReferences())
            {
                Debug.LogWarning("Cannot switch mode: ModeSwitchGuard dependencies are not ready.");
                return false;
            }

            // 检查游戏流程状态
            if (!IsGameFlowAllowsSwitch())
            {
                Debug.LogWarning("Cannot switch mode: Game is paused or in dream overlay");
                return false;
            }

            // 检查交互状态
            if (!IsInteractionStateAllowsSwitch())
            {
                Debug.LogWarning("Cannot switch mode: Currently in interaction (backpack/dragging/dream selecting)");
                return false;
            }

            // 检查飞行器可用性（当前模式为步行，要切换到飞行时）
            // 注：在新系统中不再需要这个检查，因为有三个独立的 Player
            // if (_playerModeStateMachine.IsWalkMode && !_isVehicleAvailable)
            // {
            //     Debug.LogWarning("Cannot switch mode: Vehicle is not available");
            //     return false;
            // }

            // 检查动画状态
            if (_isAnimationPlaying)
            {
                Debug.LogWarning("Cannot switch mode: Animation is playing");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 尝试通过Tab在两种3D模式间切换（Walk <-> Ship）。
        /// </summary>
        public bool TryToggleTabMode()
        {
            if (!EnsureReferences())
            {
                Debug.LogWarning("TryToggleTabMode ignored: dependencies are not ready.");
                return false;
            }

            if (!CanSwitchMode())
                return false;

            var currentMode = _playerModeStateMachine.CurrentMode;

            // Tab仅在两种3D模式之间切换；2D模式下按Tab不做切换。
            var nextMode = currentMode switch
            {
                PlayerModeState.FPS3DWalk => PlayerModeState.FPS3DShip,
                PlayerModeState.FPS3DShip => PlayerModeState.FPS3DWalk,
                _ => currentMode
            };

            if (nextMode == currentMode)
                return false;

            _last3DMode = nextMode;

            if (_playerModeStateMachine.TryTransitionMode(nextMode))
            {
                Debug.Log($"Successfully switched player mode with Tab: {currentMode} -> {nextMode}");
                return true;
            }

            return false;
        }

        /// <summary>
        /// 尝试通过Q进入/退出2D模式。
        /// </summary>
        public bool TryToggle2DMode()
        {
            if (!EnsureReferences())
            {
                Debug.LogWarning("TryToggle2DMode ignored: dependencies are not ready.");
                return false;
            }

            if (!CanSwitchMode())
                return false;

            var currentMode = _playerModeStateMachine.CurrentMode;
            PlayerModeState nextMode;

            if (currentMode == PlayerModeState.Plane2DCharacter)
            {
                nextMode = _last3DMode;
            }
            else
            {
                _last3DMode = currentMode;
                nextMode = PlayerModeState.Plane2DCharacter;
            }

            if (_playerModeStateMachine.TryTransitionMode(nextMode))
            {
                Debug.Log($"Successfully switched player mode with Q: {currentMode} -> {nextMode}");
                return true;
            }

            return false;
        }

        /// <summary>
        /// 兼容旧调用：默认沿用Tab切换逻辑。
        /// </summary>
        public bool TryTogglePlayerMode()
        {
            return TryToggleTabMode();
        }

        /// <summary>
        /// 游戏流程状态是否允许切换
        /// </summary>
        private bool IsGameFlowAllowsSwitch()
        {
            var gameState = _gameFlowStateMachine.CurrentState;
            return gameState == GameFlowState.Playing;
        }

        /// <summary>
        /// 交互状态是否允许切换
        /// </summary>
        private bool IsInteractionStateAllowsSwitch()
        {
            var interactionState = _interactionStateMachine.CurrentState;
            
            // 允许在普通模式或仅打开背包时切换
            return interactionState == InteractionState.Normal ||
                   interactionState == InteractionState.BackpackOpen;
        }

        /// <summary>
        /// 设置飞行器可用性
        /// </summary>
        public void SetVehicleAvailable(bool available)
        {
            _isVehicleAvailable = available;
        }

        /// <summary>
        /// 设置动画播放状态
        /// </summary>
        public void SetAnimationPlaying(bool playing)
        {
            _isAnimationPlaying = playing;
        }

        /// <summary>
        /// 重置所有条件到默认状态
        /// </summary>
        public void ResetConditions()
        {
            _isVehicleAvailable = true;
            _isAnimationPlaying = false;
        }
    }
}