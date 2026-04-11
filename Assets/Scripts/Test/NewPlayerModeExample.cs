﻿using UnityEngine;
using INTP.Core.StateMachine;
using INTP.Core.Input;
using INTP.Core;
using INTP.Foundation;
using INTP.Gameplay.Player;

namespace INTP.Example
{
    /// <summary>
    /// 新 Player Mode 系统示例 - 展示如何使用三种 Player 模式
    /// </summary>
    public class NewPlayerModeExample : MonoBehaviour
    {
        private PlayerModeStateMachine _playerModeStateMachine;
        private ModeSwitchGuard _modeSwitchGuard;
        private InteractionStateMachine _interactionStateMachine;
        private PlayerManager _playerManager;
        private EventBus _eventBus;

        private void Start()
        {
            _playerModeStateMachine = FindObjectOfType<PlayerModeStateMachine>();
            _modeSwitchGuard = FindObjectOfType<ModeSwitchGuard>();
            _interactionStateMachine = FindObjectOfType<InteractionStateMachine>();
            _playerManager = FindObjectOfType<PlayerManager>();
            _eventBus = EventBus.Instance;

            if (_playerModeStateMachine == null)
            {
                Debug.LogError("PlayerModeStateMachine not found!");
                return;
            }

            // 初始化 PlayerManager 中的所有 Player
            if (_playerManager != null)
            {
                _playerManager.InitializeAllPlayers();
            }

            // 订阅事件
            _playerModeStateMachine.OnModeChanged += OnPlayerModeChanged;

            Debug.Log("NewPlayerModeExample initialized");
        }

        private void OnDestroy()
        {
            if (_playerModeStateMachine != null)
                _playerModeStateMachine.OnModeChanged -= OnPlayerModeChanged;
        }

        /// <summary>
        /// 【示例1】快速理解三种 Mode
        /// </summary>
        public void Example1_UnderstandModes()
        {
            Debug.Log("=== Example 1: Understand Three Modes ===");
            
            Debug.Log("1. FPS3D Walk - 第一人称步行");
            Debug.Log("   - 鼠标控制镜头");
            Debug.Log("   - WASD 移动");
            Debug.Log("   - 用于探索环境");
            
            Debug.Log("2. FPS3D Ship - 飞船模拟器");
            Debug.Log("   - WS 朝镜头前进后退");
            Debug.Log("   - AD 左右移动");
            Debug.Log("   - Space/Ctrl 上升/下降");
            Debug.Log("   - 用于驾驶飞行器");
            
            Debug.Log("3. 2D Character - 平面小人");
            Debug.Log("   - WASD 上下左右移动");
            Debug.Log("   - 用于2D地牢/解谜");
        }

        /// <summary>
        /// 【示例2】循环切换 Mode
        /// </summary>
        public void Example2_ToggleMode()
        {
            Debug.Log("=== Example 2: Toggle Mode ===");
            
            var currentMode = _playerModeStateMachine.CurrentMode;
            Debug.Log($"Current Mode: {currentMode}");

            if (_modeSwitchGuard.TryTogglePlayerMode())
            {
                Debug.Log($"✓ Switched to: {_playerModeStateMachine.CurrentMode}");
            }
            else
            {
                Debug.LogWarning("✗ Cannot switch mode!");
            }
        }

        /// <summary>
        /// 【示例3】直接切换到特定 Mode
        /// </summary>
        public void Example3_SwitchToSpecificMode(PlayerModeState targetMode)
        {
            Debug.Log($"=== Example 3: Switch to {targetMode} ===");

            if (_playerModeStateMachine.TryTransitionMode(targetMode))
            {
                Debug.Log($"✓ Switched to {targetMode}");
            }
            else
            {
                Debug.LogWarning($"✗ Failed to switch to {targetMode}");
            }
        }

        /// <summary>
        /// 【示例4】快速切换到 FPS3D 飞船模式（用于驾驶飞行器）
        /// </summary>
        public void Example4_EnterShipMode()
        {
            Debug.Log("=== Example 4: Enter Ship Mode (Drive Spaceship) ===");
            
            // 首先检查是否允许切换
            if (!_modeSwitchGuard.CanSwitchMode())
            {
                Debug.LogWarning("Cannot switch now!");
                return;
            }

            // 直接切换到飞船模式
            _playerModeStateMachine.TryTransitionMode(PlayerModeState.FPS3DShip);
            Debug.Log("✓ Entered Ship Mode - Ready to fly!");
        }

        /// <summary>
        /// 【示例5】快速切换到 2D 小人模式（进入地牢）
        /// </summary>
        public void Example5_Enter2DMode()
        {
            Debug.Log("=== Example 5: Enter 2D Mode (Dungeon) ===");

            if (!_modeSwitchGuard.CanSwitchMode())
            {
                Debug.LogWarning("Cannot switch now!");
                return;
            }

            _playerModeStateMachine.TryTransitionMode(PlayerModeState.Plane2DCharacter);
            Debug.Log("✓ Entered 2D Mode - Dungeon exploration!");
        }

        /// <summary>
        /// 【示例6】进入 UI 交互模式（打开菜单时禁用玩家移动）
        /// </summary>
        public void Example6_EnterUIMode()
        {
            Debug.Log("=== Example 6: Enter UI Interaction Mode ===");

            // 切换到 UI 交互模式，禁用玩家控制
            var currentState = _interactionStateMachine.CurrentState;
            
            if (currentState == InteractionState.Normal)
            {
                _interactionStateMachine.TransitionTo(InteractionState.UIInteraction);
                Debug.Log("✓ UI Mode enabled - Player control disabled");
                Debug.Log("  (当前 Mode 仍保持，但 Player 对象会被禁用)");
            }
            else if (currentState == InteractionState.UIInteraction)
            {
                _interactionStateMachine.TransitionTo(InteractionState.Normal);
                Debug.Log("✓ Exited UI Mode - Player control restored");
            }
        }

        /// <summary>
        /// 【示例7】检查当前 Mode 状态
        /// </summary>
        public void Example7_CheckCurrentMode()
        {
            Debug.Log("=== Example 7: Check Current Mode ===");

            var mode = _playerModeStateMachine.CurrentMode;
            
            Debug.Log($"Current Mode: {mode}");
            Debug.Log($"Is FPS3D Walk: {_playerModeStateMachine.IsFPS3DWalkMode}");
            Debug.Log($"Is FPS3D Ship: {_playerModeStateMachine.IsFPS3DShipMode}");
            Debug.Log($"Is 2D Character: {_playerModeStateMachine.IsPlane2DCharacterMode}");

            // 获取当前活跃的 Player
            if (_playerManager != null)
            {
                GameObject activePlayer = _playerManager.GetActivePlayer();
                Debug.Log($"Active Player: {activePlayer?.name ?? "None"}");
            }
        }

        /// <summary>
        /// 【示例8】Mode 变化时执行特定逻辑
        /// </summary>
        private void OnPlayerModeChanged(PlayerModeState newMode, PlayerModeState oldMode)
        {
            Debug.Log($"[Event] Mode changed: {oldMode} → {newMode}");

            // 根据新 Mode 执行相应的初始化逻辑
            switch (newMode)
            {
                case PlayerModeState.FPS3DWalk:
                    OnEnterFPS3DWalkMode();
                    break;

                case PlayerModeState.FPS3DShip:
                    OnEnterFPS3DShipMode();
                    break;

                case PlayerModeState.Plane2DCharacter:
                    OnEnterPlane2DMode();
                    break;
            }
        }

        private void OnEnterFPS3DWalkMode()
        {
            Debug.Log("  → Initializing FPS3D Walk Mode");
            Debug.Log("  → Unlocking cursor");
            // 例如：启用 FPS3D 相关的 UI、音乐等
        }

        private void OnEnterFPS3DShipMode()
        {
            Debug.Log("  → Initializing FPS3D Ship Mode");
            Debug.Log("  → Enabling flight HUD");
            // 例如：显示飞行器 HUD、改变音乐等
        }

        private void OnEnterPlane2DMode()
        {
            Debug.Log("  → Initializing 2D Character Mode");
            Debug.Log("  → Switching to 2D camera");
            // 例如：切换摄像头、改变光照等
        }

        /// <summary>
        /// 【示例9】实际场景：从步行切换到飞行
        /// </summary>
        public void Example9_RealScenario_WalkToFly()
        {
            Debug.Log("=== Example 9: Real Scenario - Walk to Fly ===");
            Debug.Log("场景：玩家走到飞行器，按下按钮开始飞行");

            // 步骤1：检查是否在正确的 Mode
            if (!_playerModeStateMachine.IsFPS3DWalkMode)
            {
                Debug.LogWarning("Player is not in Walk mode!");
                return;
            }

            // 步骤2：切换到飞船模式
            if (_modeSwitchGuard.TryTogglePlayerMode())
            {
                Debug.Log("✓ Player entered the spaceship!");
                Debug.Log("✓ Now in Ship Mode - Ready to fly!");
                
                // 步骤3：可选 - 显示飞行器 UI
                Debug.Log("✓ Flight HUD displayed");
            }
            else
            {
                Debug.LogWarning("Cannot enter spaceship now!");
            }
        }

        /// <summary>
        /// 【示例10】实际场景：进入地下城
        /// </summary>
        public void Example10_RealScenario_EnterDungeon()
        {
            Debug.Log("=== Example 10: Real Scenario - Enter Dungeon ===");
            Debug.Log("场景：玩家进入地下城入口，模式切换到2D");

            // 检查当前 Mode
            var currentMode = _playerModeStateMachine.CurrentMode;
            Debug.Log($"Current Mode: {currentMode}");

            // 切换到 2D 模式
            _playerModeStateMachine.TryTransitionMode(PlayerModeState.Plane2DCharacter);
            
            Debug.Log("✓ Entered dungeon!");
            Debug.Log("✓ Switched to 2D Character Mode");
            Debug.Log("✓ Use WASD to explore");
        }
    }

    /// <summary>
    /// 快速测试工具类 - 在编辑器 Console 中使用
    /// </summary>
    public class QuickPlayerModeTest
    {
        /// <summary>
        /// 快速切换 Mode
        /// 用法：QuickPlayerModeTest.QuickToggle();
        /// </summary>
        public static void QuickToggle()
        {
            var guard = Object.FindObjectOfType<ModeSwitchGuard>();
            if (guard != null && guard.TryTogglePlayerMode())
            {
                var sm = Object.FindObjectOfType<PlayerModeStateMachine>();
                Debug.Log($"✓ Toggled to: {sm.CurrentMode}");
            }
        }

        /// <summary>
        /// 快速切换到 FPS3D Walk
        /// </summary>
        public static void QuickToWalk()
        {
            var sm = Object.FindObjectOfType<PlayerModeStateMachine>();
            if (sm != null && sm.TryTransitionMode(PlayerModeState.FPS3DWalk))
            {
                Debug.Log("✓ Switched to FPS3D Walk");
            }
        }

        /// <summary>
        /// 快速切换到 FPS3D Ship
        /// </summary>
        public static void QuickToShip()
        {
            var sm = Object.FindObjectOfType<PlayerModeStateMachine>();
            if (sm != null && sm.TryTransitionMode(PlayerModeState.FPS3DShip))
            {
                Debug.Log("✓ Switched to FPS3D Ship");
            }
        }

        /// <summary>
        /// 快速切换到 2D Character
        /// </summary>
        public static void QuickTo2D()
        {
            var sm = Object.FindObjectOfType<PlayerModeStateMachine>();
            if (sm != null && sm.TryTransitionMode(PlayerModeState.Plane2DCharacter))
            {
                Debug.Log("✓ Switched to 2D Character");
            }
        }

        /// <summary>
        /// 打印当前 Mode 信息
        /// </summary>
        public static void PrintCurrentMode()
        {
            var sm = Object.FindObjectOfType<PlayerModeStateMachine>();
            if (sm != null)
            {
                Debug.Log($"Current Mode: {sm.CurrentMode}");
                Debug.Log($"  Walk: {sm.IsFPS3DWalkMode}");
                Debug.Log($"  Ship: {sm.IsFPS3DShipMode}");
                Debug.Log($"  2D: {sm.IsPlane2DCharacterMode}");
            }

            var manager = Object.FindObjectOfType<PlayerManager>();
            if (manager != null)
            {
                var activePlayer = manager.GetActivePlayer();
                Debug.Log($"Active Player: {activePlayer?.name ?? "None"}");
            }
        }

        /// <summary>
        /// 进入 UI 交互模式
        /// </summary>
        public static void EnterUIMode()
        {
            var interaction = Object.FindObjectOfType<InteractionStateMachine>();
            if (interaction != null)
            {
                interaction.TransitionTo(InteractionState.UIInteraction);
                Debug.Log("✓ Entered UI Mode");
            }
        }

        /// <summary>
        /// 退出 UI 交互模式
        /// </summary>
        public static void ExitUIMode()
        {
            var interaction = Object.FindObjectOfType<InteractionStateMachine>();
            if (interaction != null)
            {
                interaction.TransitionTo(InteractionState.Normal);
                Debug.Log("✓ Exited UI Mode");
            }
        }
    }
}
