using UnityEngine;
using UnityEngine.InputSystem;
using INTP.Foundation;

namespace INTP.Gameplay.Player
{
    /// <summary>
    /// 2D 平面小人控制器 - 2D俯视图模式
    /// WASD 控制上下左右移动
    /// </Dsummary>
    public class Plane2DCharacterController : MonoBehaviour
    {
        [Header("移动设置")]
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private float _sprintSpeed = 10f;
        [SerializeField] private float _acceleration = 10f;
        [SerializeField] private Rigidbody2D _rigidbody2D;

        [Header("动画设置")]
        [SerializeField] private Animator _animator;

        [Header("交互设置")]
        [SerializeField] private Plane2DInteractionSystem _interactionSystem;

        private Vector2 _moveInput = Vector2.zero;
        private Vector2 _currentVelocity = Vector2.zero;
        private bool _isSprinting = false;
        private EventBus _eventBus;

        // 动画参数
        private int _moveXHash = Animator.StringToHash("MoveX");
        private int _moveYHash = Animator.StringToHash("MoveY");
        private int _speedHash = Animator.StringToHash("Speed");

        private void Awake()
        {
            if (_rigidbody2D == null)
                _rigidbody2D = GetComponent<Rigidbody2D>();
            if (_animator == null)
                _animator = GetComponent<Animator>();
            if (_interactionSystem == null)
                _interactionSystem = GetComponent<Plane2DInteractionSystem>();
            _eventBus = EventBus.Instance;
        }

        private void Update()
        {
            HandleInput();
            UpdateAnimations();
        }

        private void FixedUpdate()
        {
            ApplyMovement();
        }

        /// <summary>
        /// 处理输入
        /// </summary>
        private void HandleInput()
        {
            Keyboard keyboard = Keyboard.current;

            _moveInput = Vector2.zero;

            // WASD 输入
            if (keyboard.wKey.isPressed)
                _moveInput.y += 1f;
            if (keyboard.sKey.isPressed)
                _moveInput.y -= 1f;
            if (keyboard.aKey.isPressed)
                _moveInput.x -= 1f;
            if (keyboard.dKey.isPressed)
                _moveInput.x += 1f;

            // 冲刺（Shift 键）
            _isSprinting = keyboard.leftShiftKey.isPressed && _moveInput.magnitude > 0;

            // 标准化输入方向
            if (_moveInput.magnitude > 1f)
                _moveInput = _moveInput.normalized;

            // 移动时关闭背包
            if (_moveInput.magnitude > 0.1f && BackpackService.Instance != null && BackpackService.Instance.IsOpen)
            {
                BackpackService.Instance.SetBackpackOpen(false);
            }
        }

        /// <summary>
        /// 应用移动
        /// </summary>
        private void ApplyMovement()
        {
            if (_rigidbody2D == null)
                return;

            float targetSpeed = _isSprinting ? _sprintSpeed : _moveSpeed;
            Vector2 targetVelocity = _moveInput * targetSpeed;

            // 平滑速度过渡
            _currentVelocity = Vector2.Lerp(_currentVelocity, targetVelocity, _acceleration * Time.fixedDeltaTime);

            // 应用速度
            if (_rigidbody2D.isKinematic)
            {
                _rigidbody2D.linearVelocity = _currentVelocity;
            }
            else
            {
                _rigidbody2D.linearVelocity = _currentVelocity;
            }
        }

        /// <summary>
        /// 更新动画
        /// </summary>
        private void UpdateAnimations()
        {
            if (_animator == null)
                return;

            // 设置移动方向动画参数
            _animator.SetFloat(_moveXHash, _moveInput.x);
            _animator.SetFloat(_moveYHash, _moveInput.y);

            // 设置速度动画参数
            float speed = _currentVelocity.magnitude;
            _animator.SetFloat(_speedHash, speed);
        }

        /// <summary>
        /// 停止移动
        /// </summary>
        public void Stop()
        {
            _moveInput = Vector2.zero;
            _currentVelocity = Vector2.zero;
            _isSprinting = false;

            if (_rigidbody2D != null)
                _rigidbody2D.linearVelocity = Vector2.zero;
        }

        /// <summary>
        /// 获取当前速度
        /// </summary>
        public float GetCurrentSpeed() => _currentVelocity.magnitude;

        /// <summary>
        /// 是否正在冲刺
        /// </summary>
        public bool IsSprinting => _isSprinting;

        /// <summary>
        /// 是否正在移动
        /// </summary>
        public bool IsMoving => _moveInput.magnitude > 0.1f;

        /// <summary>
        /// 获取移动方向
        /// </summary>
        public Vector2 MoveDirection => _moveInput;
    }
}
