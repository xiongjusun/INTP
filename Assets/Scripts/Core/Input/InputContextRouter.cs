﻿using UnityEngine;
using UnityEngine.InputSystem;
using INTP.Foundation;
using INTP.Core.StateMachine;

namespace INTP.Core.Input
{
    /// <summary>
    /// 输入上下文路由器 - 根据当前状态切换输入上下文
    /// </summary>
    public class InputContextRouter : MonoBehaviour
    {
        [SerializeField]
        private InputActionAsset _inputActionAsset;

        private PlayerInput _playerInput;
        private GameFlowStateMachine _gameFlowStateMachine;
        private PlayerModeStateMachine _playerModeStateMachine;
        private InteractionStateMachine _interactionStateMachine;
        private EventBus _eventBus;

        private void Awake()
        {
            // 尝试获取当前 GameObject 上的 PlayerInput
            _playerInput = GetComponent<PlayerInput>();
            
            // 如果当前 GameObject 上没有 PlayerInput，则尝试在场景中查找
            if (_playerInput == null)
            {
                _playerInput = FindObjectOfType<PlayerInput>();
            }
            
            // 如果场景中也没有 PlayerInput，则在当前 GameObject 上添加
            if (_playerInput == null)
            {
                _playerInput = gameObject.AddComponent<PlayerInput>();
                Debug.Log("PlayerInput component added to InputContextRouter GameObject");
            }
            
            // 立即配置 InputActionAsset
            ConfigurePlayerInput();
            
            _gameFlowStateMachine = FindObjectOfType<GameFlowStateMachine>();
            _playerModeStateMachine = FindObjectOfType<PlayerModeStateMachine>();
            _interactionStateMachine = FindObjectOfType<InteractionStateMachine>();
            _eventBus = EventBus.Instance;
        }
        
        /// <summary>
        /// 配置 PlayerInput 的 InputActionAsset
        /// </summary>
        private void ConfigurePlayerInput()
        {
            if (_playerInput == null)
            {
                Debug.LogError("[InputContextRouter] PlayerInput is null!");
                return;
            }
            
            // 如果已经有配置，则不覆盖
            if (_playerInput.actions != null)
            {
                Debug.Log("[InputContextRouter] PlayerInput already has actions configured");
                return;
            }
            
            // 优先使用 Inspector 中分配的
            if (_inputActionAsset != null)
            {
                _playerInput.actions = _inputActionAsset;
                Debug.Log("[InputContextRouter] Assigned InputActionAsset from Inspector to PlayerInput");
                return;
            }
            
            // 使用 InputActionAssetLoader 进行多方法加载
            var inputActions = InputActionAssetLoader.GetOrLoadInputActionAsset();
            if (inputActions != null)
            {
                _playerInput.actions = inputActions;
                Debug.Log("[InputContextRouter] Successfully loaded and assigned InputSystem_Actions to PlayerInput");
                return;
            }
            
            // 失败提示
            Debug.LogError("[InputContextRouter] CRITICAL: Could not find InputSystem_Actions! " +
                "\n解决方案:" +
                "\n1) 在 Inspector 中: 拖动 InputSystem_Actions.inputactions 到 '_inputActionAsset' 字段" +
                "\n2) 创建 'Assets/Resources' 文件夹，将 InputSystem_Actions.inputactions 放入其中" +
                "\n3) 或者在场景中添加 InputActionAssetLoader 组件并在其中分配文件");
            
            // 禁用此组件以防止后续崩溃
            enabled = false;
        }

        private void OnEnable()
        {
            if (_gameFlowStateMachine != null)
                _gameFlowStateMachine.OnStateChanged += HandleGameFlowStateChanged;

            if (_playerModeStateMachine != null)
                _playerModeStateMachine.OnModeChanged += HandlePlayerModeChanged;

            if (_interactionStateMachine != null)
                _interactionStateMachine.OnStateChanged += HandleInteractionStateChanged;
        }

        private void OnDisable()
        {
            if (_gameFlowStateMachine != null)
                _gameFlowStateMachine.OnStateChanged -= HandleGameFlowStateChanged;

            if (_playerModeStateMachine != null)
                _playerModeStateMachine.OnModeChanged -= HandlePlayerModeChanged;

            if (_interactionStateMachine != null)
                _interactionStateMachine.OnStateChanged -= HandleInteractionStateChanged;
        }

        /// <summary>
        /// 应用针对特定模式的输入上下文
        /// </summary>
        public void ApplyInputForGameFlow(GameFlowState state)
        {
            if (_playerInput == null) return;

            switch (state)
            {
                case GameFlowState.Playing:
                    SwitchActionMap("Player");
                    break;
                case GameFlowState.Paused:
                case GameFlowState.DreamOverlay:
                    SwitchActionMap("UI");
                    break;
                case GameFlowState.Boot:
                    SwitchActionMap("UI");
                    break;
            }
        }

        /// <summary>
        /// 应用针对玩家模式的输入上下文
        /// </summary>
        public void ApplyInputForMode(PlayerModeState mode)
        {
            if (_playerInput == null) return;

            // 这里可以在同一个ActionMap内部根据模式启用/禁用特定的Action
            // 也可以使用不同的ActionMap
            Debug.Log($"Applying input context for player mode: {mode}");
        }

        /// <summary>
        /// 应用针对交互模式的输入上下文
        /// </summary>
        public void ApplyInputForInteraction(InteractionState state)
        {
            if (_playerInput == null) return;

            switch (state)
            {
                case InteractionState.Normal:
                    Cursor.visible = false;
                    break;
                case InteractionState.BackpackOpen:
                case InteractionState.DraggingPlaceable:
                case InteractionState.DreamSelecting:
                    Cursor.visible = true;
                    break;
            }
        }

        /// <summary>
        /// 切换Action Map
        /// </summary>
        private void SwitchActionMap(string mapName)
        {
            if (_playerInput == null)
            {
                Debug.LogWarning("[InputContextRouter] Cannot switch action map - PlayerInput is null");
                return;
            }
            
            if (_playerInput.actions == null)
            {
                Debug.LogWarning("[InputContextRouter] Cannot switch action map - PlayerInput.actions is null");
                return;
            }
            
            if (_playerInput.currentActionMap == null || _playerInput.currentActionMap.name == mapName)
                return;

            _playerInput.SwitchCurrentActionMap(mapName);
            Debug.Log($"Switched to action map: {mapName}");
        }

        private void HandleGameFlowStateChanged(GameFlowState newState)
        {
            ApplyInputForGameFlow(newState);
        }

        private void HandlePlayerModeChanged(PlayerModeState newMode, PlayerModeState oldMode)
        {
            ApplyInputForMode(newMode);
        }

        private void HandleInteractionStateChanged(InteractionState newState)
        {
            ApplyInputForInteraction(newState);
        }
    }
}
