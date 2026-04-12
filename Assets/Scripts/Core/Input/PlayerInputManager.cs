﻿﻿using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.EventSystems;
using INTP.Foundation;

namespace INTP.Core.Input
{
    /// <summary>
    /// PlayerInput 管理器 - 统一管理所有输入相关的初始化和配置
    /// 在场景中添加此脚本，确保InputSystem正确工作
    /// </summary>
    public class PlayerInputManager : MonoBehaviour
    {
        [SerializeField]
        private InputActionAsset _inputActions;

        [SerializeField]
        private bool _useUIInputModule = true;

        [SerializeField]
        private bool _autoSwitchActionMaps = true;

        private PlayerInput _playerInput;
        private InputSystemUIInputModule _uiInputModule;

        private void Awake()
        {
            // 查找或创建PlayerInput组件
            _playerInput = GetComponent<PlayerInput>();
            if (_playerInput == null)
            {
                Debug.LogError("[PlayerInputManager] PlayerInput component not found! Please add it to this GameObject.");
                return;
            }

            // 配置PlayerInput
            SetupPlayerInput();

            // 配置UI输入模块
            if (_useUIInputModule)
            {
                SetupUIInputModule();
            }
        }

        private void SetupPlayerInput()
        {
            if (_playerInput == null) return;

            // 验证InputActions资源
            if (_playerInput.actions == null)
            {
                Debug.LogWarning("[PlayerInputManager] No InputActionAsset assigned to PlayerInput!");
                return;
            }

            // 验证ActionMaps存在
            if (_playerInput.actions.FindActionMap("Player") == null)
            {
                Debug.LogError("[PlayerInputManager] 'Player' ActionMap not found in InputActionAsset!");
                return;
            }

            if (_playerInput.actions.FindActionMap("UI") == null)
            {
                Debug.LogError("[PlayerInputManager] 'UI' ActionMap not found in InputActionAsset!");
                return;
            }

            // 启用默认ActionMap
            _playerInput.actions.FindActionMap("Player").Enable();

            Debug.Log("[PlayerInputManager] PlayerInput configured successfully!");
        }

        private void SetupUIInputModule()
        {
            // 查找Canvas中的GraphicRaycaster
            Canvas mainCanvas = FindObjectOfType<Canvas>();
            if (mainCanvas == null)
            {
                Debug.LogWarning("[PlayerInputManager] No Canvas found in scene. UI input may not work properly.");
                return;
            }

            // 查找或创建InputSystemUIInputModule
            _uiInputModule = FindObjectOfType<InputSystemUIInputModule>();
            if (_uiInputModule == null)
            {
                // 在EventSystem上添加InputSystemUIInputModule
                EventSystem eventSystem = FindObjectOfType<EventSystem>();
                if (eventSystem == null)
                {
                    Debug.LogWarning("[PlayerInputManager] No EventSystem found! Creating one...");
                    GameObject eventSystemGO = new GameObject("EventSystem");
                    eventSystem = eventSystemGO.AddComponent<EventSystem>();
                    eventSystemGO.AddComponent<StandaloneInputModule>();
                }

                _uiInputModule = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            }

            // 配置UI输入
            if (_uiInputModule != null)
            {
                _uiInputModule.actionsAsset = _playerInput.actions;
                Debug.Log("[PlayerInputManager] UI Input Module configured!");
            }
        }

        /// <summary>
        /// 切换ActionMap (例如从Player到UI)
        /// </summary>
        public void SwitchActionMap(string actionMapName)
        {
            if (_playerInput == null || _playerInput.actions == null) return;

            // 禁用所有ActionMap
            foreach (var actionMap in _playerInput.actions.actionMaps)
            {
                actionMap.Disable();
            }

            // 启用指定的ActionMap
            var targetMap = _playerInput.actions.FindActionMap(actionMapName);
            if (targetMap != null)
            {
                targetMap.Enable();
                Debug.Log($"[PlayerInputManager] Switched to '{actionMapName}' ActionMap");
            }
            else
            {
                Debug.LogWarning($"[PlayerInputManager] ActionMap '{actionMapName}' not found!");
            }
        }

        /// <summary>
        /// 获取特定的InputAction
        /// </summary>
        public InputAction GetAction(string actionName)
        {
            if (_playerInput == null || _playerInput.actions == null) return null;

            try
            {
                return _playerInput.actions[actionName];
            }
            catch
            {
                Debug.LogWarning($"[PlayerInputManager] Action '{actionName}' not found!");
                return null;
            }
        }

        /// <summary>
        /// 订阅Action事件 (简化版)
        /// </summary>
        public void SubscribeToAction(string actionName, System.Action<InputAction.CallbackContext> callback)
        {
            var action = GetAction(actionName);
            if (action != null)
            {
                action.performed += callback;
                action.Enable();
            }
        }

        /// <summary>
        /// 取消订阅Action事件
        /// </summary>
        public void UnsubscribeFromAction(string actionName, System.Action<InputAction.CallbackContext> callback)
        {
            var action = GetAction(actionName);
            if (action != null)
            {
                action.performed -= callback;
            }
        }

        /// <summary>
        /// 获取PlayerInput组件
        /// </summary>
        public PlayerInput GetPlayerInput()
        {
            return _playerInput;
        }

        /// <summary>
        /// 启用所有输入
        /// </summary>
        public void EnableAllInput()
        {
            if (_playerInput != null && _playerInput.actions != null)
            {
                _playerInput.actions.Enable();
            }
        }

        /// <summary>
        /// 禁用所有输入
        /// </summary>
        public void DisableAllInput()
        {
            if (_playerInput != null && _playerInput.actions != null)
            {
                _playerInput.actions.Disable();
            }
        }

        /// <summary>
        /// 获取当前活跃的ActionMap
        /// </summary>
        public string GetCurrentActionMapName()
        {
            if (_playerInput == null || _playerInput.currentActionMap == null)
                return "None";

            return _playerInput.currentActionMap.name;
        }

        /// <summary>
        /// 验证所有必要的Actions是否存在
        /// </summary>
        public bool ValidateAllActions()
        {
            if (_playerInput == null || _playerInput.actions == null)
                return false;

            // 检查Player ActionMap的Actions
            string[] playerActions = 
            {
                "Move", "Look", "Attack", "Interact", "Jump",
                "Sprint", "Crouch", "Previous", "Next",
                "SwitchMode", "Toggle2DMode", "OpenBackpack", "DreamHold", "Pause"
            };

            foreach (var actionName in playerActions)
            {
                if (_playerInput.actions[actionName] == null)
                {
                    Debug.LogError($"[PlayerInputManager] Missing action: '{actionName}'");
                    return false;
                }
            }

            // 检查UI ActionMap的Actions
            string[] uiActions = { "Navigate", "Submit", "Cancel", "Point", "Click" };

            foreach (var actionName in uiActions)
            {
                if (_playerInput.actions[actionName] == null)
                {
                    Debug.LogError($"[PlayerInputManager] Missing UI action: '{actionName}'");
                    return false;
                }
            }

            Debug.Log("[PlayerInputManager] All required actions are present!");
            return true;
        }

        /// <summary>
        /// 打印所有可用的ActionMap和Actions (调试用)
        /// </summary>
        public void PrintActionsDebugInfo()
        {
            if (_playerInput == null || _playerInput.actions == null)
            {
                Debug.LogWarning("[PlayerInputManager] PlayerInput or Actions not available!");
                return;
            }

            Debug.Log("=== PlayerInput Debug Info ===");
            foreach (var actionMap in _playerInput.actions.actionMaps)
            {
                Debug.Log($"ActionMap: {actionMap.name}");
                foreach (var action in actionMap.actions)
                {
                    Debug.Log($"  └─ Action: {action.name} ({action.type})");
                    foreach (var binding in action.bindings)
                    {
                        Debug.Log($"      └─ {binding.path} [{binding.groups}]");
                    }
                }
            }
        }
    }
}
