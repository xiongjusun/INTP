using UnityEngine;
using UnityEngine.Video;
using UnityEngine.InputSystem;
using INTP.Foundation;
using INTP.Core.StateMachine;

namespace INTP.Gameplay
{
    /// <summary>
    /// 示例视频交互物品
    /// 需要在场景中有一个 VideoPlayer 和 RenderTexture
    /// </summary>
    public class ExampleVideoInteractable : VideoInteractable
    {
        [Header("Video Settings")]
        [SerializeField] private VideoClip videoClip;
        [SerializeField] private VideoPlayer videoPlayer;
        [SerializeField] private RenderTexture renderTexture;
        [SerializeField] private GameObject videoScreen;
        [SerializeField] private Material screenMaterial;

        [Header("Settings")]
        [SerializeField] private float fadeInDuration = 0.5f;
        [SerializeField] private bool pauseGameWhilePlaying = true;

        [Header("Exit Settings")]
        [Tooltip("长按多久退出视频（秒）")]
        [SerializeField] private float longPressDuration = 1.0f;
        [Tooltip("长按退出按键 (Input System)")]
        [SerializeField] private InputActionProperty exitAction;
        [Tooltip("是否启用长按退出")]
        [SerializeField] private bool enableLongPressExit = true;

        private bool _isPlaying = false;
        private float _longPressTimer = 0f;
        private bool _isLongPressing = false;
        private InteractionStateMachine _interactionStateMachine;
        private InputAction _cachedExitAction;

        private void Start()
        {
            _interactionStateMachine = FindAnyObjectByType<InteractionStateMachine>();

            // 优先使用 Inspector 中设置的 Reference
            if (exitAction.reference != null)
            {
                _cachedExitAction = exitAction.action;
            }

            // 如果没有设置有效的 action，从 Player ActionMap 获取 VideoExit
            if (_cachedExitAction == null || string.IsNullOrEmpty(_cachedExitAction.name))
            {
                var playerInput = FindAnyObjectByType<UnityEngine.InputSystem.PlayerInput>();
                if (playerInput != null && playerInput.actions != null)
                {
                    var playerMap = playerInput.actions.FindActionMap("Player");
                    if (playerMap != null)
                    {
                        _cachedExitAction = playerMap.FindAction("VideoExit");
                    }
                }
            }

            if (_cachedExitAction == null || string.IsNullOrEmpty(_cachedExitAction.name))
            {
                Debug.LogError("[Video] VideoExit action not found!");
            }
            else if (!_cachedExitAction.enabled)
            {
                _cachedExitAction.Enable();
            }
        }

        private void OnEnable()
        {
            if (_cachedExitAction != null)
            {
                _cachedExitAction.performed += OnExitActionPerformed;
                _cachedExitAction.canceled += OnExitActionCanceled;
            }
        }

        private void OnDisable()
        {
            if (_cachedExitAction != null)
            {
                _cachedExitAction.performed -= OnExitActionPerformed;
                _cachedExitAction.canceled -= OnExitActionCanceled;
            }
        }

        private void OnExitActionPerformed(InputAction.CallbackContext context)
        {
            if (_isPlaying && enableLongPressExit)
            {
                _isLongPressing = true;
            }
        }

        private void OnExitActionCanceled(InputAction.CallbackContext context)
        {
            _isLongPressing = false;
            _longPressTimer = 0f;
        }

        private void Update()
        {
            if (_isPlaying && enableLongPressExit && _isLongPressing)
            {
                _longPressTimer += Time.unscaledDeltaTime;
                if (_longPressTimer >= longPressDuration)
                {
                    StopVideo();
                }
            }
        }

        protected override void OnVideoPlay()
        {
            if (_isPlaying) return;
            _isPlaying = true;
            _longPressTimer = 0f;

            // 强制启用 Player ActionMap
            var playerInput = FindAnyObjectByType<UnityEngine.InputSystem.PlayerInput>();
            if (playerInput != null)
            {
                var playerMap = playerInput.actions?.FindActionMap("Player");
                if (playerMap != null && !playerMap.enabled)
                {
                    playerMap.Enable();
                }
            }

            // 确保 VideoExit Action 被启用
            if (_cachedExitAction != null && !_cachedExitAction.enabled)
            {
                _cachedExitAction.Enable();
            }

            // 设置视频材质
            if (screenMaterial != null && renderTexture != null)
            {
                screenMaterial.mainTexture = renderTexture;
            }

            // 播放视频
            if (videoPlayer != null && videoClip != null)
            {
                videoPlayer.clip = videoClip;
                videoPlayer.targetTexture = renderTexture;
                videoPlayer.Play();

                if (videoScreen != null)
                {
                    videoScreen.SetActive(true);
                }

                videoPlayer.loopPointReached += OnVideoEnded;
            }

            // 锁定玩家交互
            if (_interactionStateMachine != null)
            {
                _interactionStateMachine.TransitionTo(InteractionState.VideoPlaying);
            }
        }

        private void OnVideoEnded(VideoPlayer vp)
        {
            StopVideo();
        }

        /// <summary>
        /// 停止视频播放（外部调用）
        /// </summary>
        public void StopVideo()
        {
            if (videoPlayer != null)
            {
                videoPlayer.Stop();
                videoPlayer.loopPointReached -= OnVideoEnded;
            }

            if (videoScreen != null)
            {
                videoScreen.SetActive(false);
            }

            // 恢复玩家交互
            if (_interactionStateMachine != null)
            {
                _interactionStateMachine.TransitionTo(InteractionState.Normal);
            }

            _isPlaying = false;
            _longPressTimer = 0f;
            _isLongPressing = false;
        }

        /// <summary>
        /// 获取当前播放状态
        /// </summary>
        public bool IsPlaying => _isPlaying;

        /// <summary>
        /// 获取长按进度（0-1）
        /// </summary>
        public float LongPressProgress => Mathf.Clamp01(_longPressTimer / longPressDuration);
    }
}
