﻿using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

namespace INTP.Core.Input
{
    /// <summary>
    /// InputActions 使用示例 - 展示如何在实际项目中使用各种 Actions
    /// 这是一个参考脚本，不直接用于生产环境
    /// </summary>
    public class InputActionsExample : MonoBehaviour
    {
        private PlayerInput _playerInput;
        private InputActionMap _playerActionMap;
        private InputActionMap _uiActionMap;

        // 各个Action的引用
        private InputAction _moveAction;
        private InputAction _lookAction;
        private InputAction _jumpAction;
        private InputAction _sprintAction;
        private InputAction _switchModeAction;
        private InputAction _openBackpackAction;
        private InputAction _dreamHoldAction;
        private InputAction _pauseAction;

        private void Awake()
        {
            InitializePlayerInput();
        }

        private void OnEnable()
        {
            SubscribeToActions();
        }

        private void OnDisable()
        {
            UnsubscribeFromActions();
        }

        /// <summary>
        /// 初始化 PlayerInput 和所有 ActionMaps
        /// </summary>
        private void InitializePlayerInput()
        {
            _playerInput = GetComponent<PlayerInput>();
            if (_playerInput == null)
            {
                Debug.LogError("[InputActionsExample] PlayerInput component not found!");
                return;
            }

            if (_playerInput.actions == null)
            {
                Debug.LogError("[InputActionsExample] No InputActionAsset assigned!");
                return;
            }

            // 获取 ActionMaps
            _playerActionMap = _playerInput.actions.FindActionMap("Player");
            _uiActionMap = _playerInput.actions.FindActionMap("UI");

            if (_playerActionMap == null || _uiActionMap == null)
            {
                Debug.LogError("[InputActionsExample] Required ActionMaps not found!");
                return;
            }

            // 获取所有需要的 Actions
            try
            {
                _moveAction = _playerInput.actions["Move"];
                _lookAction = _playerInput.actions["Look"];
                _jumpAction = _playerInput.actions["Jump"];
                _sprintAction = _playerInput.actions["Sprint"];
                _switchModeAction = _playerInput.actions["SwitchMode"];
                _openBackpackAction = _playerInput.actions["OpenBackpack"];
                _dreamHoldAction = _playerInput.actions["DreamHold"];
                _pauseAction = _playerInput.actions["Pause"];

                Debug.Log("[InputActionsExample] All actions initialized successfully!");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[InputActionsExample] Failed to initialize actions: {ex.Message}");
            }
        }

        /// <summary>
        /// 订阅所有 Action 事件
        /// </summary>
        private void SubscribeToActions()
        {
            if (_moveAction != null)
                _moveAction.performed += OnMove;

            if (_lookAction != null)
                _lookAction.performed += OnLook;

            if (_jumpAction != null)
                _jumpAction.performed += OnJump;

            if (_sprintAction != null)
            {
                _sprintAction.performed += OnSprintStarted;
                _sprintAction.canceled += OnSprintEnded;
            }

            if (_switchModeAction != null)
                _switchModeAction.performed += OnSwitchMode;

            if (_openBackpackAction != null)
            {
                _openBackpackAction.performed += OnBackpackToggle;
            }

            if (_dreamHoldAction != null)
            {
                _dreamHoldAction.started += OnDreamStarted;
                _dreamHoldAction.canceled += OnDreamEnded;
            }

            if (_pauseAction != null)
                _pauseAction.performed += OnPause;
        }

        /// <summary>
        /// 取消订阅所有 Action 事件
        /// </summary>
        private void UnsubscribeFromActions()
        {
            if (_moveAction != null)
                _moveAction.performed -= OnMove;

            if (_lookAction != null)
                _lookAction.performed -= OnLook;

            if (_jumpAction != null)
                _jumpAction.performed -= OnJump;

            if (_sprintAction != null)
            {
                _sprintAction.performed -= OnSprintStarted;
                _sprintAction.canceled -= OnSprintEnded;
            }

            if (_switchModeAction != null)
                _switchModeAction.performed -= OnSwitchMode;

            if (_openBackpackAction != null)
            {
                _openBackpackAction.performed -= OnBackpackToggle;
            }

            if (_dreamHoldAction != null)
            {
                _dreamHoldAction.started -= OnDreamStarted;
                _dreamHoldAction.canceled -= OnDreamEnded;
            }

            if (_pauseAction != null)
                _pauseAction.performed -= OnPause;
        }

        // ============ Action 回调方法 ============

        /// <summary>
        /// 移动 Action 回调 - Move 是 Value 类型，持续监听
        /// </summary>
        private void OnMove(InputAction.CallbackContext context)
        {
            Vector2 moveInput = context.ReadValue<Vector2>();
            
            // 检查是否真的有输入
            if (moveInput.sqrMagnitude > 0.01f)
            {
                Debug.Log($"[Move] Direction: {moveInput.x:F2}, {moveInput.y:F2}");
                // 应用移动到角色控制器
                // characterController.Move(moveInput);
            }
        }

        /// <summary>
        /// 视角 Action 回调 - Look 是 Value 类型，持续监听
        /// </summary>
        private void OnLook(InputAction.CallbackContext context)
        {
            Vector2 lookInput = context.ReadValue<Vector2>();
            
            if (lookInput.sqrMagnitude > 0.01f)
            {
                Debug.Log($"[Look] Direction: {lookInput.x:F2}, {lookInput.y:F2}");
                // 应用到摄像机控制
                // cameraController.Rotate(lookInput);
            }
        }

        /// <summary>
        /// 跳跃 Action 回调 - 按键按下时触发一次
        /// </summary>
        private void OnJump(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                Debug.Log("[Jump] Jump executed!");
                // 执行跳跃逻辑
                // characterController.Jump();
            }
        }

        /// <summary>
        /// 冲刺 Action 回调 - 追踪开始和结束
        /// </summary>
        private void OnSprintStarted(InputAction.CallbackContext context)
        {
            Debug.Log("[Sprint] Sprint started");
            // characterController.StartSprint();
        }

        private void OnSprintEnded(InputAction.CallbackContext context)
        {
            Debug.Log("[Sprint] Sprint ended");
            // characterController.EndSprint();
        }

        /// <summary>
        /// 切换模式 Action 回调
        /// 新添加的 Action
        /// </summary>
        private void OnSwitchMode(InputAction.CallbackContext context)
        {
            Debug.Log("[SwitchMode] Mode switched! (Tab pressed)");
            // 调用模式切换逻辑
            // modeSwitcher.ToggleMode();
        }

        /// <summary>
        /// 打开背包 Action 回调
        /// 新添加的 Action
        /// </summary>
        private void OnBackpackToggle(InputAction.CallbackContext context)
        {
            Debug.Log("[OpenBackpack] Backpack toggled! (B pressed)");
            // 切换背包UI
            // backpackUI.Toggle();
        }

        /// <summary>
        /// 梦境 Action 回调 - 长按 F 键
        /// 新添加的 Action，使用 Hold 交互
        /// </summary>
        private void OnDreamStarted(InputAction.CallbackContext context)
        {
            Debug.Log("[DreamHold] Dream overlay started (F pressed and held)");
            // 打开梦境叠加层
            // dreamOverlay.Show();
        }

        private void OnDreamEnded(InputAction.CallbackContext context)
        {
            Debug.Log("[DreamHold] Dream overlay ended (F released)");
            // 关闭梦境叠加层
            // dreamOverlay.Hide();
        }

        /// <summary>
        /// 暂停 Action 回调
        /// 新添加的 Action
        /// </summary>
        private void OnPause(InputAction.CallbackContext context)
        {
            Debug.Log("[Pause] Game paused! (Escape pressed)");
            // 暂停游戏或打开暂停菜单
            // gameManager.TogglePause();
        }

        // ============ 辅助方法 ============

        /// <summary>
        /// 切换 ActionMap (例如从 Player 到 UI)
        /// </summary>
        public void SwitchToUIActionMap()
        {
            if (_playerActionMap != null)
                _playerActionMap.Disable();

            if (_uiActionMap != null)
                _uiActionMap.Enable();

            Debug.Log("[ActionMap] Switched to UI");
        }

        public void SwitchToPlayerActionMap()
        {
            if (_uiActionMap != null)
                _uiActionMap.Disable();

            if (_playerActionMap != null)
                _playerActionMap.Enable();

            Debug.Log("[ActionMap] Switched to Player");
        }

        /// <summary>
        /// 获取当前的移动输入（Value类型）
        /// 在 Update 中持续获取（不通过事件）
        /// </summary>
        public Vector2 GetMoveInput()
        {
            if (_moveAction != null)
                return _moveAction.ReadValue<Vector2>();
            return Vector2.zero;
        }

        /// <summary>
        /// 获取当前的视角输入（Value类型）
        /// </summary>
        public Vector2 GetLookInput()
        {
            if (_lookAction != null)
                return _lookAction.ReadValue<Vector2>();
            return Vector2.zero;
        }

        /// <summary>
        /// 检查是否正在冲刺
        /// </summary>
        public bool IsSprintPressed()
        {
            if (_sprintAction != null)
                return _sprintAction.IsPressed();
            return false;
        }

        /// <summary>
        /// 临时禁用特定 Action
        /// </summary>
        public void DisableAction(string actionName)
        {
            var action = _playerInput.actions[actionName];
            if (action != null)
            {
                action.Disable();
                Debug.Log($"[Action] Disabled: {actionName}");
            }
        }

        /// <summary>
        /// 启用特定 Action
        /// </summary>
        public void EnableAction(string actionName)
        {
            var action = _playerInput.actions[actionName];
            if (action != null)
            {
                action.Enable();
                Debug.Log($"[Action] Enabled: {actionName}");
            }
        }

        /// <summary>
        /// 检查 Action 是否启用
        /// </summary>
        public bool IsActionEnabled(string actionName)
        {
            var action = _playerInput.actions[actionName];
            if (action != null)
                return action.enabled;
            return false;
        }

        /// <summary>
        /// 打印所有 Action 的当前值（调试用）
        /// </summary>
        public void DebugPrintAllActions()
        {
            Debug.Log("=== Current Action Values ===");
            Debug.Log($"Move: {GetMoveInput()}");
            Debug.Log($"Look: {GetLookInput()}");
            Debug.Log($"Sprint: {IsSprintPressed()}");
            
            Debug.Log("=== All ActionMaps ===");
            if (_playerInput != null && _playerInput.actions != null)
            {
                foreach (var actionMap in _playerInput.actions.actionMaps)
                {
                    Debug.Log($"ActionMap: {actionMap.name} (Enabled: {actionMap.enabled})");
                    foreach (var action in actionMap.actions)
                    {
                        // 使用 activeControl 获取值，避免 ReadValue<object> 的泛型限制
                        object actionValue = action.activeControl != null ? action.activeControl.ReadValueAsObject() : "N/A";
                        Debug.Log($"  └─ {action.name}: {actionValue} (Enabled: {action.enabled})");
                    }
                }
            }
        }
    }
}
