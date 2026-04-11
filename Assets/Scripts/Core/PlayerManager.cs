﻿using UnityEngine;
using INTP.Foundation;
using INTP.Core.StateMachine;
using INTP.Gameplay.Player;

namespace INTP.Core
{
    /// <summary>
    /// 玩家管理器 - 管理多个独立的 Player 对象（FPS3D步行、FPS3D飞船、2D平面小人）
    /// 根据 PlayerModeState 激活和禁用相应的 Player 对象
    /// </summary>
    public class PlayerManager : MonoBehaviour
    {
        [Header("Scene Player References (optional)")]
        [SerializeField] private GameObject _fps3DWalkPlayer;
        [SerializeField] private GameObject _fps3DShipPlayer;
        [SerializeField] private GameObject _plane2DCharacterPlayer;

        [Header("Prefab Fallback (optional)")]
        [SerializeField] private GameObject _fps3DWalkPlayerPrefab;      // FPS3D步行 Player
        [SerializeField] private GameObject _fps3DShipPlayerPrefab;      // FPS3D飞船 Player
        [SerializeField] private GameObject _plane2DCharacterPrefab;    // 2D平面小人 Player

        private GameObject _activeFPS3DWalkPlayer;
        private GameObject _activeFPS3DShipPlayer;
        private GameObject _activePlane2DCharacterPlayer;

        private PlayerModeStateMachine _playerModeStateMachine;
        private EventBus _eventBus;
        private bool _initialized;
        private bool _subscribedToModeChanged;

        private void Awake()
        {
            _eventBus = EventBus.Instance;
            EnsureModeStateMachineReference();
        }

        private void Start()
        {
            // 防止忘记在外部显式调用初始化，导致模式切换成功但对象未激活。
            InitializeAllPlayers();

            if (!_initialized)
                InvokeRepeating(nameof(TryInitializeUntilReady), 0.2f, 0.2f);
        }

        private void TryInitializeUntilReady()
        {
            if (_initialized)
            {
                CancelInvoke(nameof(TryInitializeUntilReady));
                return;
            }

            InitializeAllPlayers();

            if (_initialized)
                CancelInvoke(nameof(TryInitializeUntilReady));
        }

        private void OnDestroy()
        {
            if (_subscribedToModeChanged && _playerModeStateMachine != null)
                _playerModeStateMachine.OnModeChanged -= OnPlayerModeChanged;
        }

        /// <summary>
        /// 初始化所有 Player 对象（在游戏启动时调用）
        /// </summary>
        public void InitializeAllPlayers()
        {
            if (!EnsureModeStateMachineReference())
            {
                Debug.LogWarning("PlayerManager initialization deferred: PlayerModeStateMachine not ready yet.");
                return;
            }

            if (_initialized)
            {
                SwitchToMode(_playerModeStateMachine.CurrentMode);
                return;
            }

            // 1) 优先使用 Inspector 显式绑定（可绑定未激活对象）
            _activeFPS3DWalkPlayer = _fps3DWalkPlayer;
            _activeFPS3DShipPlayer = _fps3DShipPlayer;
            _activePlane2DCharacterPlayer = _plane2DCharacterPlayer;

            // 2) 回退：在场景中查找（包含未激活对象）
            if (_activeFPS3DWalkPlayer == null)
                _activeFPS3DWalkPlayer = FindInSceneIncludingInactive<FPS3DWalkController>()?.gameObject;
            if (_activeFPS3DShipPlayer == null)
                _activeFPS3DShipPlayer = FindInSceneIncludingInactive<FPS3DShipController>()?.gameObject;
            if (_activePlane2DCharacterPlayer == null)
                _activePlane2DCharacterPlayer = FindInSceneIncludingInactive<Plane2DCharacterController>()?.gameObject;

            // 如果场景中没有，则从 Prefab 创建
            if (_activeFPS3DWalkPlayer == null && _fps3DWalkPlayerPrefab != null)
            {
                _activeFPS3DWalkPlayer = Instantiate(_fps3DWalkPlayerPrefab, transform);
            }
            if (_activeFPS3DShipPlayer == null && _fps3DShipPlayerPrefab != null)
            {
                _activeFPS3DShipPlayer = Instantiate(_fps3DShipPlayerPrefab, transform);
            }
            if (_activePlane2DCharacterPlayer == null && _plane2DCharacterPrefab != null)
            {
                _activePlane2DCharacterPlayer = Instantiate(_plane2DCharacterPrefab, transform);
            }

            _initialized = true;

            // 根据初始 Mode 激活相应的 Player
            SwitchToMode(_playerModeStateMachine.CurrentMode);
        }

        private bool EnsureModeStateMachineReference()
        {
            if (_playerModeStateMachine == null)
                _playerModeStateMachine = FindObjectOfType<PlayerModeStateMachine>();

            if (_playerModeStateMachine == null)
                return false;

            if (!_subscribedToModeChanged)
            {
                _playerModeStateMachine.OnModeChanged += OnPlayerModeChanged;
                _subscribedToModeChanged = true;
            }

            return true;
        }

        private static T FindInSceneIncludingInactive<T>() where T : Component
        {
            T[] all = Resources.FindObjectsOfTypeAll<T>();
            foreach (var item in all)
            {
                if (item == null)
                    continue;

                // 过滤掉非场景对象（如 prefab 资源）
                if (!item.gameObject.scene.IsValid())
                    continue;

                return item;
            }

            return null;
        }

        /// <summary>
        /// 当 Mode 改变时调用
        /// </summary>
        private void OnPlayerModeChanged(PlayerModeState newMode, PlayerModeState oldMode)
        {
            SwitchToMode(newMode);
        }

        /// <summary>
        /// 切换到指定 Mode 的 Player
        /// </summary>
        private void SwitchToMode(PlayerModeState targetMode)
        {
            // 禁用所有 Player
            if (_activeFPS3DWalkPlayer != null)
                _activeFPS3DWalkPlayer.SetActive(false);
            if (_activeFPS3DShipPlayer != null)
                _activeFPS3DShipPlayer.SetActive(false);
            if (_activePlane2DCharacterPlayer != null)
                _activePlane2DCharacterPlayer.SetActive(false);

            // 启用目标 Player
            switch (targetMode)
            {
                case PlayerModeState.FPS3DWalk:
                    if (_activeFPS3DWalkPlayer != null)
                    {
                        _activeFPS3DWalkPlayer.SetActive(true);
                        Debug.Log("✓ FPS3D Walk Player activated");
                    }
                    else
                    {
                        Debug.LogError("FPS3D Walk Player not found!");
                    }
                    break;

                case PlayerModeState.FPS3DShip:
                    if (_activeFPS3DShipPlayer != null)
                    {
                        _activeFPS3DShipPlayer.SetActive(true);
                        Debug.Log("✓ FPS3D Ship Player activated");
                    }
                    else
                    {
                        Debug.LogError("FPS3D Ship Player not found!");
                    }
                    break;

                case PlayerModeState.Plane2DCharacter:
                    if (_activePlane2DCharacterPlayer != null)
                    {
                        _activePlane2DCharacterPlayer.SetActive(true);
                        Debug.Log("✓ 2D Character Player activated");
                    }
                    else
                    {
                        Debug.LogError("2D Character Player not found!");
                    }
                    break;
            }
        }

        /// <summary>
        /// 获取当前活跃的 Player GameObject
        /// </summary>
        public GameObject GetActivePlayer()
        {
            if (!EnsureModeStateMachineReference())
                return null;

            return _playerModeStateMachine.CurrentMode switch
            {
                PlayerModeState.FPS3DWalk => _activeFPS3DWalkPlayer,
                PlayerModeState.FPS3DShip => _activeFPS3DShipPlayer,
                PlayerModeState.Plane2DCharacter => _activePlane2DCharacterPlayer,
                _ => null
            };
        }

        /// <summary>
        /// 获取指定 Mode 的 Player GameObject
        /// </summary>
        public GameObject GetPlayerByMode(PlayerModeState mode)
        {
            return mode switch
            {
                PlayerModeState.FPS3DWalk => _activeFPS3DWalkPlayer,
                PlayerModeState.FPS3DShip => _activeFPS3DShipPlayer,
                PlayerModeState.Plane2DCharacter => _activePlane2DCharacterPlayer,
                _ => null
            };
        }
    }
}
