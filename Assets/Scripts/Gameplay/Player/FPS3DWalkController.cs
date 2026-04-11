using UnityEngine;
using UnityEngine.InputSystem;
using INTP.Foundation;

namespace INTP.Gameplay.Player
{
    /// <summary>
    /// FPS3D 步行控制器 - 第一人称3D模式
    /// 鼠标控制镜头旋转，WASD 移动
    /// </summary>
    public class FPS3DWalkController : MonoBehaviour
    {
        [Header("移动设置")]
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private float _sprintSpeed = 10f;
        [SerializeField] private float _acceleration = 10f;
        [SerializeField] private CharacterController _characterController;

        [Header("视角设置")]
        [SerializeField] private Camera _mainCamera;
        [SerializeField] private float _mouseSensitivity = 2f;
        [SerializeField] private float _maxLookAngle = 90f;

        private Vector3 _moveDirection = Vector3.zero;
        private float _currentSpeed = 0f;
        private float _verticalLookRotation = 0f;
        private bool _isSprinting = false;
        private EventBus _eventBus;

        private void Awake()
        {
            if (_characterController == null)
                _characterController = GetComponent<CharacterController>();
            if (_mainCamera == null)
                _mainCamera = GetComponentInChildren<Camera>();
            _eventBus = EventBus.Instance;
        }

        private void OnEnable()
        {
            // 游戏开始时可以解锁并隐藏光标
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void OnDisable()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void Update()
        {
            HandleMouseLook();
            HandleMovement();
        }

        /// <summary>
        /// 处理鼠标看视角
        /// </summary>
        private void HandleMouseLook()
        {
            if (_mainCamera == null)
                return;

            Vector2 mouseDelta = Mouse.current.delta.ReadValue();

            // 绕Y轴旋转（左右看）
            transform.Rotate(Vector3.up * (mouseDelta.x * _mouseSensitivity));

            // 绕X轴旋转（上下看）
            _verticalLookRotation -= mouseDelta.y * _mouseSensitivity;
            _verticalLookRotation = Mathf.Clamp(_verticalLookRotation, -_maxLookAngle, _maxLookAngle);

            _mainCamera.transform.localRotation = Quaternion.Euler(_verticalLookRotation, 0, 0);
        }

        /// <summary>
        /// 处理移动
        /// </summary>
        private void HandleMovement()
        {
            if (_characterController == null || !_characterController.enabled)
                return;

            Keyboard keyboard = Keyboard.current;
            Vector3 inputDirection = Vector3.zero;

            // 获取输入方向
            if (keyboard.wKey.isPressed)
                inputDirection += transform.forward;
            if (keyboard.sKey.isPressed)
                inputDirection -= transform.forward;
            if (keyboard.aKey.isPressed)
                inputDirection -= transform.right;
            if (keyboard.dKey.isPressed)
                inputDirection += transform.right;

            // 冲刺
            _isSprinting = keyboard.leftShiftKey.isPressed;
            float targetSpeed = _isSprinting ? _sprintSpeed : _moveSpeed;

            // 平滑速度变化
            if (inputDirection.magnitude > 0)
            {
                _currentSpeed = Mathf.Lerp(_currentSpeed, targetSpeed, _acceleration * Time.deltaTime);
            }
            else
            {
                _currentSpeed = Mathf.Lerp(_currentSpeed, 0, _acceleration * Time.deltaTime);
            }

            _moveDirection = inputDirection.normalized * _currentSpeed;

            // 应用重力
            _moveDirection.y -= 9.81f * Time.deltaTime;

            // 移动角色
            _characterController.Move(_moveDirection * Time.deltaTime);

            Debug.Log($"FPS3D Walk - Speed: {_currentSpeed:F2}, Sprint: {_isSprinting}");
        }

        /// <summary>
        /// 停止移动
        /// </summary>
        public void Stop()
        {
            _moveDirection = Vector3.zero;
            _currentSpeed = 0f;
        }

        /// <summary>
        /// 获取当前速度
        /// </summary>
        public float GetCurrentSpeed() => _currentSpeed;

        /// <summary>
        /// 是否正在冲刺
        /// </summary>
        public bool IsSprinting => _isSprinting;
    }
}
