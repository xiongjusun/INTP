using UnityEngine;
using UnityEngine.InputSystem;
using INTP.Foundation;

namespace INTP.Gameplay
{
    /// <summary>
    /// 2D玩家交互系统 - 负责检测可交互物品和处理交互输入
    /// 需要添加到与 Plane2DCharacterController 相同的 GameObject 上
    /// </summary>
    public class Plane2DInteractionSystem : MonoBehaviour
    {
        [Header("Interaction Settings")]
        [SerializeField] private float interactionRadius = 2f;
        [SerializeField] private LayerMask interactableLayer = ~0;
        [SerializeField] private Transform interactionPoint;

        [Header("Input")]
        [SerializeField] private InputActionAsset inputActions;

        private InputAction _interactAction;
        private InputAction _openBackpackAction;
        private IInteractable _currentInteractable;
        private Collider2D[] _hitColliders = new Collider2D[10];
        private EventBus _eventBus;
        private bool _inputEnabled = true;

        public IInteractable CurrentInteractable => _currentInteractable;
        public bool InputEnabled
        {
            get => _inputEnabled;
            set => _inputEnabled = value;
        }

        private void Awake()
        {
            _eventBus = EventBus.Instance;

            if (interactionPoint == null)
            {
                interactionPoint = transform;
            }

            SetupInputActions();
        }

        private void OnEnable()
        {
            EnableInput();
        }

        private void OnDisable()
        {
            DisableInput();
        }

        private void SetupInputActions()
        {
            if (inputActions == null)
            {
                inputActions = Resources.Load<InputActionAsset>("InputSystem_Actions");
            }

            if (inputActions != null)
            {
                var playerMap = inputActions.FindActionMap("Player");
                if (playerMap != null)
                {
                    _interactAction = playerMap.FindAction("Interact");
                    _openBackpackAction = playerMap.FindAction("OpenBackpack");
                }
            }

            if (_interactAction != null)
            {
                _interactAction.started += OnInteractStarted;
                _interactAction.performed += OnInteractPerformed;
                _interactAction.canceled += OnInteractCanceled;
            }
            if (_openBackpackAction != null)
            {
                _openBackpackAction.performed += OnOpenBackpackPerformed;
            }
        }

        private void OnDestroy()
        {
            if (_interactAction != null)
            {
                _interactAction.started -= OnInteractStarted;
                _interactAction.performed -= OnInteractPerformed;
                _interactAction.canceled -= OnInteractCanceled;
            }
            if (_openBackpackAction != null)
            {
                _openBackpackAction.performed -= OnOpenBackpackPerformed;
            }
        }

        private void Update()
        {
            if (!_inputEnabled) return;

            DetectInteractables();
        }

        /// <summary>
        /// 检测范围内的可交互物品
        /// </summary>
        private void DetectInteractables()
        {
            _currentInteractable = null;

            int hitCount = Physics2D.OverlapCircleNonAlloc(
                interactionPoint.position,
                interactionRadius,
                _hitColliders,
                interactableLayer);

            IInteractable closestInteractable = null;
            float closestDistance = float.MaxValue;

            for (int i = 0; i < hitCount; i++)
            {
                var collider = _hitColliders[i];
                if (collider == null) continue;

                // 查找可交互组件
                IInteractable interactable = null;

                // 先检查自身
                if (collider.TryGetComponent<IInteractable>(out var component))
                {
                    interactable = component;
                }

                // 检查父物体
                if (interactable == null)
                {
                    var parent = collider.transform.parent;
                    while (parent != null && interactable == null)
                    {
                        if (parent.TryGetComponent<IInteractable>(out var parentComponent))
                        {
                            interactable = parentComponent;
                        }
                        parent = parent.parent;
                    }
                }

                if (interactable != null && interactable.CanInteract())
                {
                    float distance = Vector2.Distance(interactionPoint.position, collider.transform.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestInteractable = interactable;
                    }
                }
            }

            _currentInteractable = closestInteractable;
            var promptText = closestInteractable?.InteractionPrompt ?? "";
            var type = closestInteractable?.InteractionType ?? InteractableType.Collectible;
            EventBus.Instance.Publish(new InteractableChangedEvent(promptText, type, closestInteractable != null));
        }

        private void OnInteractStarted(InputAction.CallbackContext context)
        {
            Debug.Log("[Plane2DInteractionSystem] OnInteractStarted called");
            
            if (!_inputEnabled) return;

            // 背包打开时，不处理交互
            if (BackpackService.Instance != null && BackpackService.Instance.IsOpen)
            {
                Debug.Log("[Plane2DInteractionSystem] Backpack is open, skipping interaction");
                return;
            }

            if (_currentInteractable != null)
            {
                var itemName = (_currentInteractable as MonoBehaviour)?.gameObject.name ?? "Unknown";
                Debug.Log($"[Plane2DInteractionSystem] Calling Interact() on: {itemName}");
                _currentInteractable.Interact();
            }
            else
            {
                Debug.Log("[Plane2DInteractionSystem] No interactable in range");
            }
        }

        private void OnInteractPerformed(InputAction.CallbackContext context)
        {
            // 交互已在 OnInteractStarted 中处理，这里不做重复处理
        }

        private void OnInteractCanceled(InputAction.CallbackContext context)
        {
            // Hold 取消时的处理（如果需要）
        }

        private void OnOpenBackpackPerformed(InputAction.CallbackContext context)
        {
            if (!_inputEnabled) return;

            // 如果正在交互，不切换背包
            if (_currentInteractable != null && _currentInteractable.CanInteract())
            {
                return;
            }

            if (BackpackService.Instance != null)
            {
                BackpackService.Instance.ToggleBackpack();
            }
        }

        /// <summary>
        /// 启用输入
        /// </summary>
        public void EnableInput()
        {
            _inputEnabled = true;
        }

        /// <summary>
        /// 禁用输入
        /// </summary>
        public void DisableInput()
        {
            _inputEnabled = false;
        }

        /// <summary>
        /// 显示交互提示（供UI系统调用）
        /// </summary>
        public string GetInteractionPrompt()
        {
            return _currentInteractable?.InteractionPrompt ?? "";
        }

        private void OnDrawGizmosSelected()
        {
            if (interactionPoint == null) interactionPoint = transform;

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(interactionPoint.position, interactionRadius);

            if (_currentInteractable != null)
            {
                Gizmos.color = Color.yellow;
                var targetPos = _currentInteractable as Component;
                if (targetPos != null)
                {
                    Gizmos.DrawLine(interactionPoint.position, targetPos.transform.position);
                }
            }
        }
    }
}
