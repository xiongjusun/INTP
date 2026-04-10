using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class InfiniteWrapFlyCamera : MonoBehaviour
{
    [Header("Wrap Area")]
    [SerializeField] private Transform planeTransform;
    [SerializeField] private MeshFilter planeMeshFilter;

    [Header("Move")]
    [SerializeField] private float moveSpeed = 12f;
    [SerializeField] private float fastMoveSpeed = 24f;
    [SerializeField] private float verticalSpeed = 8f;

    [Header("Look")]
    [SerializeField] private float mouseSensitivity = 0.08f;
    [SerializeField] private float lookSmoothTime = 0.04f;
    [SerializeField] private float minPitch = -80f;
    [SerializeField] private float maxPitch = 80f;
    [SerializeField] private bool rotateWhilePointerOverUI = true;

    [Header("World Height Bounds (Y Axis)")]
    [SerializeField] private float minWorldY = 0f;
    [SerializeField] private float maxWorldY = 7f;

    [Header("Cursor")]
    [SerializeField] private bool keepCursorVisible = true;

    private Bounds planeLocalBounds;

    private float targetYaw;
    private float targetPitch;
    private float currentYaw;
    private float currentPitch;
    private float yawVelocity;
    private float pitchVelocity;

    private void Awake()
    {
        if (planeTransform == null)
        {
            Debug.LogError("InfiniteWrapFlyCamera: Assign Plane Transform.", this);
            enabled = false;
            return;
        }

        if (planeMeshFilter == null)
            planeMeshFilter = planeTransform.GetComponent<MeshFilter>();

        if (planeMeshFilter == null || planeMeshFilter.sharedMesh == null)
        {
            Debug.LogError("InfiniteWrapFlyCamera: Plane needs a MeshFilter with a mesh.", this);
            enabled = false;
            return;
        }

        planeLocalBounds = planeMeshFilter.sharedMesh.bounds;
        CacheStartAngles();
    }

    private void Start()
    {
        ApplyCursorMode();
    }

    private void OnEnable()
    {
        ApplyCursorMode();
    }

    private void OnDisable()
    {
        ReleaseCursor();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
            ApplyCursorMode();
        else
            ReleaseCursor();
    }

    private void Update()
    {
        UpdateLook();
        UpdateMovement();
    }

    private void UpdateLook()
    {
        if (!rotateWhilePointerOverUI &&
            EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        Vector2 look = ReadLookInput();

        targetYaw += look.x * mouseSensitivity;
        targetPitch -= look.y * mouseSensitivity;
        targetPitch = Mathf.Clamp(targetPitch, minPitch, maxPitch);

        currentYaw = Mathf.SmoothDampAngle(currentYaw, targetYaw, ref yawVelocity, lookSmoothTime);
        currentPitch = Mathf.SmoothDampAngle(currentPitch, targetPitch, ref pitchVelocity, lookSmoothTime);

        Quaternion yawRotation = Quaternion.AngleAxis(currentYaw, planeTransform.up);
        Vector3 rightAxis = yawRotation * planeTransform.right;
        Quaternion pitchRotation = Quaternion.AngleAxis(currentPitch, rightAxis);

        transform.rotation = pitchRotation * yawRotation;
    }

    private void UpdateMovement()
    {
        Vector3 input = ReadMoveInput();

        if (input.sqrMagnitude > 1f)
            input.Normalize();

        Vector3 planeUp = planeTransform.up;

        Vector3 forwardOnPlane = Vector3.ProjectOnPlane(transform.forward, planeUp);
        if (forwardOnPlane.sqrMagnitude < 0.0001f)
            forwardOnPlane = Vector3.ProjectOnPlane(planeTransform.forward, planeUp);

        forwardOnPlane.Normalize();

        Vector3 rightOnPlane = Vector3.Cross(planeUp, forwardOnPlane).normalized;
        float speed = IsFastMoveHeld() ? fastMoveSpeed : moveSpeed;

        Vector3 planarMove =
            (rightOnPlane * input.x + forwardOnPlane * input.z) * speed;

        Vector3 verticalMove = Vector3.up * (input.y * verticalSpeed);

        Vector3 nextWorldPos = transform.position + (planarMove + verticalMove) * Time.deltaTime;

        Vector3 nextLocalPos = planeTransform.InverseTransformPoint(nextWorldPos);
        nextLocalPos.x = Wrap(nextLocalPos.x, planeLocalBounds.min.x, planeLocalBounds.max.x);
        nextLocalPos.z = Wrap(nextLocalPos.z, planeLocalBounds.min.z, planeLocalBounds.max.z);

        Vector3 wrappedWorldPos = planeTransform.TransformPoint(nextLocalPos);
        wrappedWorldPos.y = Mathf.Clamp(nextWorldPos.y, minWorldY, maxWorldY);

        transform.position = wrappedWorldPos;
    }

    private void CacheStartAngles()
    {
        Vector3 planeUp = planeTransform.up;

        Vector3 flatForward = Vector3.ProjectOnPlane(transform.forward, planeUp);
        if (flatForward.sqrMagnitude < 0.0001f)
            flatForward = Vector3.ProjectOnPlane(planeTransform.forward, planeUp);

        flatForward.Normalize();

        float startYaw = Vector3.SignedAngle(planeTransform.forward, flatForward, planeUp);
        Vector3 rightAxis = Vector3.Cross(planeUp, flatForward).normalized;
        float startPitch = Vector3.SignedAngle(flatForward, transform.forward, rightAxis);
        startPitch = Mathf.Clamp(startPitch, minPitch, maxPitch);

        targetYaw = currentYaw = startYaw;
        targetPitch = currentPitch = startPitch;
    }

    private void ApplyCursorMode()
    {
#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX
        Cursor.lockState = (!Application.isEditor)
            ? CursorLockMode.Confined
            : CursorLockMode.None;
#else
        Cursor.lockState = CursorLockMode.None;
#endif
        Cursor.visible = keepCursorVisible;
    }

    private void ReleaseCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private float Wrap(float value, float min, float max)
    {
        float size = max - min;
        if (size <= 0f)
            return min;

        float wrapped = (value - min) % size;
        if (wrapped < 0f)
            wrapped += size;

        return min + wrapped;
    }

    private Vector2 ReadLookInput()
    {
        Mouse mouse = Mouse.current;
        return mouse != null ? mouse.delta.ReadValue() : Vector2.zero;
    }

    private Vector3 ReadMoveInput()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null)
            return Vector3.zero;

        float x = 0f;
        float y = 0f;
        float z = 0f;

        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) x -= 1f;
        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) x += 1f;
        if (kb.sKey.isPressed || kb.downArrowKey.isPressed) z -= 1f;
        if (kb.wKey.isPressed || kb.upArrowKey.isPressed) z += 1f;

        if (kb.spaceKey.isPressed || kb.eKey.isPressed) y += 1f;
        if (kb.leftCtrlKey.isPressed || kb.qKey.isPressed || kb.cKey.isPressed) y -= 1f;

        return new Vector3(x, y, z);
    }

    private bool IsFastMoveHeld()
    {
        Keyboard kb = Keyboard.current;
        return kb != null && (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed);
    }
}