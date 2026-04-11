﻿using UnityEngine;
using UnityEngine.InputSystem;
using INTP.Foundation;
using INTP.Core.StateMachine;
using INTP.Core.Input;

namespace INTP.Core
{
    /// <summary>
    /// 游戏流程控制器 - 处理主要的游戏流程输入和状态转换
    /// </summary>
    public class GameFlowController : MonoBehaviour
    {
        private GameFlowStateMachine _gameFlowStateMachine;
        private PlayerModeStateMachine _playerModeStateMachine;
        private InteractionStateMachine _interactionStateMachine;
        private ModeSwitchGuard _modeSwitchGuard;
        private EventBus _eventBus;

        // 输入组件
        private PlayerInput _playerInput;
        private InputAction _pauseAction;
        private InputAction _switchModeAction;
        private InputAction _backpackAction;
        private InputAction _dreamAction;

        private void Awake()
        {
            InitializeReferences();
            SetupInputActions();
        }

        private void OnEnable()
        {
            if (_pauseAction != null)
                _pauseAction.performed += OnPausePressed;
            if (_switchModeAction != null)
                _switchModeAction.performed += OnSwitchModePressed;
            if (_backpackAction != null)
                _backpackAction.performed += OnBackpackPressed;
            if (_dreamAction != null)
                _dreamAction.performed += OnDreamPressed;
        }

        private void OnDisable()
        {
            if (_pauseAction != null)
                _pauseAction.performed -= OnPausePressed;
            if (_switchModeAction != null)
                _switchModeAction.performed -= OnSwitchModePressed;
            if (_backpackAction != null)
                _backpackAction.performed -= OnBackpackPressed;
            if (_dreamAction != null)
                _dreamAction.performed -= OnDreamPressed;
        }

        private void InitializeReferences()
        {
            _gameFlowStateMachine = FindObjectOfType<GameFlowStateMachine>();
            _playerModeStateMachine = FindObjectOfType<PlayerModeStateMachine>();
            _interactionStateMachine = FindObjectOfType<InteractionStateMachine>();
            _modeSwitchGuard = FindObjectOfType<ModeSwitchGuard>();
            _eventBus = EventBus.Instance;

            if (_gameFlowStateMachine == null)
                Debug.LogError("GameFlowStateMachine not found!");
            if (_playerModeStateMachine == null)
                Debug.LogError("PlayerModeStateMachine not found!");
            if (_interactionStateMachine == null)
                Debug.LogError("InteractionStateMachine not found!");
            if (_modeSwitchGuard == null)
                Debug.LogError("ModeSwitchGuard not found!");
        }

        // 输入回调可能先于某些系统初始化完成，按需补齐引用。
        private bool EnsureReferencesForInput()
        {
            if (_gameFlowStateMachine == null)
                _gameFlowStateMachine = FindObjectOfType<GameFlowStateMachine>();
            if (_playerModeStateMachine == null)
                _playerModeStateMachine = FindObjectOfType<PlayerModeStateMachine>();
            if (_interactionStateMachine == null)
                _interactionStateMachine = FindObjectOfType<InteractionStateMachine>();
            if (_modeSwitchGuard == null)
                _modeSwitchGuard = FindObjectOfType<ModeSwitchGuard>();
            if (_eventBus == null)
                _eventBus = EventBus.Instance;

            return _gameFlowStateMachine != null &&
                   _playerModeStateMachine != null &&
                   _interactionStateMachine != null &&
                   _modeSwitchGuard != null &&
                   _eventBus != null;
        }

        private void SetupInputActions()
        {
            _playerInput = GetComponent<PlayerInput>();
            if (_playerInput == null)
            {
                Debug.LogError("PlayerInput component not found on this GameObject!");
                return;
            }

            // 尝试获取输入动作（需要InputSystem_Actions.inputactions配置）
            try
            {
                _pauseAction = _playerInput.actions["Pause"];
                _switchModeAction = _playerInput.actions["SwitchMode"];
                _backpackAction = _playerInput.actions["OpenBackpack"];
                _dreamAction = _playerInput.actions["DreamHold"];
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Some input actions not configured: {ex.Message}");
                // 这是正常的，如果InputSystem_Actions还没有配置
            }
        }

        /// <summary>
        /// 处理暂停输入（ESC键）
        /// </summary>
        private void OnPausePressed(InputAction.CallbackContext context)
        {
            if (!EnsureReferencesForInput())
            {
                Debug.LogWarning("OnPausePressed ignored: required systems are not ready.");
                return;
            }

            var currentState = _gameFlowStateMachine.CurrentState;

            if (currentState == GameFlowState.Playing)
            {
                _gameFlowStateMachine.TransitionTo(GameFlowState.Paused);
                Debug.Log("Game paused");
            }
            else if (currentState == GameFlowState.Paused)
            {
                _gameFlowStateMachine.TransitionTo(GameFlowState.Playing);
                Debug.Log("Game resumed");
            }
        }

        /// <summary>
        /// 处理模式切换输入（Tab键）
        /// </summary>
        private void OnSwitchModePressed(InputAction.CallbackContext context)
        {
            if (!EnsureReferencesForInput())
            {
                Debug.LogWarning("OnSwitchModePressed ignored: ModeSwitchGuard or state machines not ready.");
                return;
            }

            if (_modeSwitchGuard.TryTogglePlayerMode())
            {
                Debug.Log("Player mode switched successfully");
            }
        }

        /// <summary>
        /// 处理背包输入（E键）
        /// </summary>
        private void OnBackpackPressed(InputAction.CallbackContext context)
        {
            if (!EnsureReferencesForInput())
            {
                Debug.LogWarning("OnBackpackPressed ignored: required systems are not ready.");
                return;
            }

            var currentInteractionState = _interactionStateMachine.CurrentState;

            if (currentInteractionState == InteractionState.Normal)
            {
                _interactionStateMachine.TransitionTo(InteractionState.BackpackOpen);
                Debug.Log("Backpack opened");
                _eventBus.Publish(new BackpackOpenedEvent { });
            }
            else if (currentInteractionState == InteractionState.BackpackOpen)
            {
                _interactionStateMachine.TransitionTo(InteractionState.Normal);
                Debug.Log("Backpack closed");
                _eventBus.Publish(new BackpackClosedEvent { });
            }
        }

        /// <summary>
        /// 处理梦境输入（F键按住）
        /// </summary>
        private void OnDreamPressed(InputAction.CallbackContext context)
        {
            if (!EnsureReferencesForInput())
            {
                Debug.LogWarning("OnDreamPressed ignored: required systems are not ready.");
                return;
            }

            // 这里暂时简化处理，实际梦境系统会更复杂
            if (context.started)
            {
                var currentGameState = _gameFlowStateMachine.CurrentState;
                if (currentGameState == GameFlowState.Playing)
                {
                    _gameFlowStateMachine.TransitionTo(GameFlowState.DreamOverlay);
                    Debug.Log("Dream overlay opened");
                    _eventBus.Publish(new DreamStartedEvent { });
                }
            }
            else if (context.canceled)
            {
                var currentGameState = _gameFlowStateMachine.CurrentState;
                if (currentGameState == GameFlowState.DreamOverlay)
                {
                    _gameFlowStateMachine.TransitionTo(GameFlowState.Playing);
                    Debug.Log("Dream overlay closed");
                    _eventBus.Publish(new DreamEndedEvent { });
                }
            }
        }
    }

    // ============ 事件定义 ============

    public class BackpackOpenedEvent : IGameEvent { }
    public class BackpackClosedEvent : IGameEvent { }
    public class DreamStartedEvent : IGameEvent { }
    public class DreamEndedEvent : IGameEvent { }
}