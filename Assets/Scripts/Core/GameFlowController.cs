﻿﻿﻿﻿﻿﻿using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
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
        private InputAction _toggle2DModeAction;
        private InputAction _cancelAction;
        private InputAction _backpackAction;
        private InputAction _dreamAction;

        [Header("2D Mode Blink Transition")]
        [SerializeField] private Image _blinkOverlayImage;
        [SerializeField] private string _blinkShaderProgressProperty = "_CloseAmount";
        [SerializeField] private float _blinkCloseDuration = 0.12f;
        [SerializeField] private float _blinkOpenDuration = 0.12f;

        [Header("2D Mode Eye Icon Animation")]
        [SerializeField] private Animator _blinkIconAnimator;
        [SerializeField] private string _iconCloseTrigger = "Close";
        [SerializeField] private string _iconOpenTrigger = "Open";

        private bool _is2DTransitionPlaying;
        private Material _blinkMaterialInstance;

        private void Awake()
        {
            InitializeReferences();
            SetupInputActions();
            SetupBlinkOverlay();
        }

        private void OnDestroy()
        {
            if (_blinkMaterialInstance != null)
                Destroy(_blinkMaterialInstance);
        }

        private void OnEnable()
        {
            if (_pauseAction != null)
                _pauseAction.performed += OnPausePressed;
            if (_switchModeAction != null)
                _switchModeAction.performed += OnSwitchModePressed;
            if (_toggle2DModeAction != null)
                _toggle2DModeAction.performed += OnToggle2DModePressed;
            if (_cancelAction != null)
                _cancelAction.performed += OnCancelPressed;
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
            if (_toggle2DModeAction != null)
                _toggle2DModeAction.performed -= OnToggle2DModePressed;
            if (_cancelAction != null)
                _cancelAction.performed -= OnCancelPressed;
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
                _toggle2DModeAction = _playerInput.actions["Toggle2DMode"];
                _cancelAction = _playerInput.actions["Cancel"];
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
        /// 处理模式切换输入（Tab键，仅第一人称<->飞行器）
        /// </summary>
        private void OnSwitchModePressed(InputAction.CallbackContext context)
        {
            if (!EnsureReferencesForInput())
            {
                Debug.LogWarning("OnSwitchModePressed ignored: ModeSwitchGuard or state machines not ready.");
                return;
            }

            if (_modeSwitchGuard.TryToggleTabMode())
            {
                Debug.Log("Player mode switched successfully");
            }
        }

        /// <summary>
        /// 处理2D模式切换输入（Q键）
        /// </summary>
        private void OnToggle2DModePressed(InputAction.CallbackContext context)
        {
            if (!EnsureReferencesForInput())
            {
                Debug.LogWarning("OnToggle2DModePressed ignored: ModeSwitchGuard or state machines not ready.");
                return;
            }

            if (_is2DTransitionPlaying)
                return;

            StartCoroutine(Toggle2DModeWithBlinkRoutine());
        }

        private IEnumerator Toggle2DModeWithBlinkRoutine()
        {
            _is2DTransitionPlaying = true;

            bool isIn2DMode = _playerModeStateMachine.CurrentMode == PlayerModeState.Plane2DCharacter;

            if (!isIn2DMode)
            {
                // 进入2D：先闭眼并保持黑屏，再切换模式。
                PlayBlinkIconClose();
                _modeSwitchGuard.SetAnimationPlaying(true);
                yield return PlayBlinkPhase(0f, 1f, _blinkCloseDuration);
                _modeSwitchGuard.SetAnimationPlaying(false);

                bool switchedTo2D = _modeSwitchGuard.TryToggle2DMode();
                if (!switchedTo2D)
                {
                    Debug.LogWarning("Enter 2D blocked by guard conditions.");

                    // 切换失败时恢复睁眼，避免卡黑屏。
                    PlayBlinkIconOpen();
                    _modeSwitchGuard.SetAnimationPlaying(true);
                    yield return PlayBlinkPhase(1f, 0f, _blinkOpenDuration);
                    _modeSwitchGuard.SetAnimationPlaying(false);
                }
                else
                {
                    ApplyBlinkProgress(1f);
                    if (_blinkOverlayImage != null)
                        _blinkOverlayImage.enabled = true;
                    Debug.Log("Entered 2D mode with eyes closed");
                }
            }
            else
            {
                // 退出2D：先切模式，再睁眼。
                bool switchedTo3D = _modeSwitchGuard.TryToggle2DMode();
                if (!switchedTo3D)
                {
                    Debug.LogWarning("Exit 2D blocked by guard conditions.");
                }
                else
                {
                    _modeSwitchGuard.SetAnimationPlaying(true);
                    PlayBlinkIconOpen();
                    yield return PlayBlinkPhase(1f, 0f, _blinkOpenDuration);
                    _modeSwitchGuard.SetAnimationPlaying(false);
                    Debug.Log("Exited 2D mode and eyes opened");
                }
            }

            _is2DTransitionPlaying = false;
        }

        private IEnumerator PlayBlinkPhase(float from, float to, float duration)
        {
            if (_blinkOverlayImage == null)
                yield break;

            if (!_blinkOverlayImage.gameObject.activeSelf)
                _blinkOverlayImage.gameObject.SetActive(true);

            _blinkOverlayImage.enabled = true;

            if (duration <= 0f)
            {
                ApplyBlinkProgress(to);
            }
            else
            {
                float elapsed = 0f;
                while (elapsed < duration)
                {
                    float t = elapsed / duration;
                    ApplyBlinkProgress(Mathf.Lerp(from, to, t));
                    elapsed += Time.unscaledDeltaTime;
                    yield return null;
                }

                ApplyBlinkProgress(to);
            }

            if (to <= 0.001f)
            {
                _blinkOverlayImage.enabled = false;
                _blinkOverlayImage.gameObject.SetActive(false);
            }
        }

        private void SetupBlinkOverlay()
        {
            if (_blinkOverlayImage == null)
                return;

            _blinkOverlayImage.raycastTarget = false;

            if (_blinkOverlayImage.material != null)
            {
                _blinkMaterialInstance = new Material(_blinkOverlayImage.material);
                _blinkOverlayImage.material = _blinkMaterialInstance;
            }

            ApplyBlinkProgress(0f);
            _blinkOverlayImage.enabled = false;
            _blinkOverlayImage.gameObject.SetActive(false);
        }

        private void ApplyBlinkProgress(float closeAmount)
        {
            if (_blinkOverlayImage == null)
                return;

            float clamped = Mathf.Clamp01(closeAmount);

            // 无Shader时使用Image透明度作为兜底效果。
            var color = _blinkOverlayImage.color;
            color.a = clamped;
            _blinkOverlayImage.color = color;

            if (_blinkMaterialInstance != null && _blinkMaterialInstance.HasProperty(_blinkShaderProgressProperty))
            {
                _blinkMaterialInstance.SetFloat(_blinkShaderProgressProperty, clamped);
            }
        }

        private void PlayBlinkIconClose()
        {
            if (_blinkIconAnimator == null || string.IsNullOrEmpty(_iconCloseTrigger))
                return;

            if (!string.IsNullOrEmpty(_iconOpenTrigger))
                _blinkIconAnimator.ResetTrigger(_iconOpenTrigger);

            _blinkIconAnimator.SetTrigger(_iconCloseTrigger);
        }

        private void PlayBlinkIconOpen()
        {
            if (_blinkIconAnimator == null || string.IsNullOrEmpty(_iconOpenTrigger))
                return;

            if (!string.IsNullOrEmpty(_iconCloseTrigger))
                _blinkIconAnimator.ResetTrigger(_iconCloseTrigger);

            _blinkIconAnimator.SetTrigger(_iconOpenTrigger);
        }

        /// <summary>
        /// 处理UI取消输入（在暂停界面按Esc返回游戏）
        /// </summary>
        private void OnCancelPressed(InputAction.CallbackContext context)
        {
            if (!EnsureReferencesForInput())
            {
                Debug.LogWarning("OnCancelPressed ignored: required systems are not ready.");
                return;
            }

            if (_gameFlowStateMachine.CurrentState == GameFlowState.Paused)
            {
                _gameFlowStateMachine.TransitionTo(GameFlowState.Playing);
                Debug.Log("Game resumed from UI cancel");
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