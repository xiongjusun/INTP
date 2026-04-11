﻿﻿using UnityEngine;
using UnityEngine.InputSystem;
using INTP.Foundation;

namespace INTP.Gameplay.Player
{
    /// <summary>
    /// FPS3D 飞船模拟器控制器 - 第一人称飞船模式
    /// WS 朝屏幕中心方向前进后退
    /// AD 左右移动
    /// Space/Ctrl 上升/下降
    /// 鼠标控制视角方向
    /// </summary>
    public class FPS3DShipController : MonoBehaviour
    {
        [Header("飞行设置")]
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private float _verticalSpeed = 3f;
        [SerializeField] private float _acceleration = 10f;
        [SerializeField] private CharacterController _characterController;
        [SerializeField] private bool _useHorizontalPlaneForMove = false;

        [Header("视角设置")]
        [SerializeField] private Camera _mainCamera;
        [SerializeField] private float _mouseSensitivity = 2f;
        [SerializeField] private float _maxLookAngle = 90f;

        private Vector3 _moveDirection = Vector3.zero;
        private float _currentForwardSpeed = 0f;
        private float _currentRightSpeed = 0f;
        private float _currentVerticalSpeed = 0f;
        private float _verticalLookRotation = 0f;
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
            HandleShipMovement();
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
        /// 处理飞船移动（类似飞行器）
        /// W/S - 朝屏幕中心方向前进后退（相对于镜头方向）
        /// A/D - 左右移动
        /// Space/Ctrl - 上升/下降
        /// </summary>
        private void HandleShipMovement()
        {
            if (_characterController == null || !_characterController.enabled || _mainCamera == null)
                return;

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            float forwardInput = 0f;
            float rightInput = 0f;

            // WS - 朝镜头前方移动（相对于视角中心）
            if (keyboard.wKey.isPressed)
                forwardInput += 1f;
            if (keyboard.sKey.isPressed)
                forwardInput -= 1f;

            // AD - 左右移动
            if (keyboard.aKey.isPressed)
                rightInput -= 1f;
            if (keyboard.dKey.isPressed)
                rightInput += 1f;

            // Space/Ctrl - 上升/下降
            float verticalInput = 0f;
            if (keyboard.spaceKey.isPressed)
                verticalInput = 1f;
            if (keyboard.ctrlKey.isPressed)
                verticalInput = -1f;

            // 平滑速度变化
            float targetForwardSpeed = forwardInput * _moveSpeed;
            float targetRightSpeed = rightInput * _moveSpeed;
            float targetVerticalSpeed = verticalInput * _verticalSpeed;

            _currentForwardSpeed = Mathf.Lerp(_currentForwardSpeed, targetForwardSpeed, _acceleration * Time.deltaTime);
            _currentRightSpeed = Mathf.Lerp(_currentRightSpeed, targetRightSpeed, _acceleration * Time.deltaTime);
            _currentVerticalSpeed = Mathf.Lerp(_currentVerticalSpeed, targetVerticalSpeed, _acceleration * Time.deltaTime);

            // 计算最终移动方向（相对于世界空间）
            Vector3 forward = _mainCamera.transform.forward;
            Vector3 right = _mainCamera.transform.right;

            if (_useHorizontalPlaneForMove)
            {
                forward.y = 0f;
                right.y = 0f;
                forward.Normalize();
                right.Normalize();
            }

            _moveDirection = forward * _currentForwardSpeed +
                             right * _currentRightSpeed +
                             Vector3.up * _currentVerticalSpeed;

            // 移动角色
            _characterController.Move(_moveDirection * Time.deltaTime);
        }

        /// <summary>
        /// 停止移动
        /// </summary>
        public void Stop()
        {
            _moveDirection = Vector3.zero;
            _currentForwardSpeed = 0f;
            _currentRightSpeed = 0f;
            _currentVerticalSpeed = 0f;
        }

        /// <summary>
        /// 获取当前速度大小
        /// </summary>
        public float GetCurrentSpeed() => _moveDirection.magnitude;
    }
}
