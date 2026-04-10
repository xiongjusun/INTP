using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class WorldPlacementController : MonoBehaviour
{
    [SerializeField] private Camera sceneCamera;
    [SerializeField] private LayerMask placementMask;
    [SerializeField] private Transform placementUpReference;
    [SerializeField] private Transform placedObjectsParent;
    [SerializeField] private TiledWorldIllusion tiledWorldIllusion;

    [Header("Placement")]
    [SerializeField] private float maxRayDistance = 500f;
    [SerializeField] private float surfaceOffset = 0.02f;
    [SerializeField] private float positionSmoothTime = 0.03f;
    [SerializeField] private float rotationSmoothSharpness = 18f;
    [SerializeField, Range(0f, 1f)] private float minUpDot = 0.8f;

    private PlaceableItemData currentItem;
    private GameObject previewInstance;
    private Vector3 previewVelocity;
    private Vector3 targetPosition;
    private Quaternion targetRotation;

    private bool isPlacing;
    private bool hasValidSurface;
    private bool previewInitialized;
    private bool ignoreCurrentClick;

    private void Awake()
    {
        if (sceneCamera == null)
            sceneCamera = Camera.main;
    }

    private void Update()
    {
        if (!isPlacing)
            return;

        UpdatePreviewPosition();

        if (WasCancelPressed())
        {
            CancelPlacement();
            return;
        }

        if (ignoreCurrentClick)
        {
            if (!IsLeftMouseHeld())
                ignoreCurrentClick = false;

            return;
        }

        if (WasLeftMousePressed())
        {
            if (IsPointerOverUI())
                return;

            if (hasValidSurface)
                PlaceCurrent();
        }
    }

    public void BeginPlacement(PlaceableItemData item)
    {
        if (item == null || item.placedPrefab == null)
            return;

        CancelPlacement();

        currentItem = item;
        isPlacing = true;
        ignoreCurrentClick = IsLeftMouseHeld();

        GameObject previewPrefab = item.previewPrefab != null ? item.previewPrefab : item.placedPrefab;
        previewInstance = Instantiate(previewPrefab);
        previewInstance.name = previewPrefab.name + "_Preview";

        targetRotation = Quaternion.Euler(item.rotationEuler);
        previewInstance.transform.rotation = targetRotation;

        int ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");
        if (ignoreRaycastLayer >= 0)
            SetLayerRecursively(previewInstance, ignoreRaycastLayer);

        DisablePhysics(previewInstance);

        previewVelocity = Vector3.zero;
        previewInitialized = false;
        hasValidSurface = false;
    }

    private void UpdatePreviewPosition()
    {
        if (sceneCamera == null)
            sceneCamera = Camera.main;

        if (sceneCamera == null || previewInstance == null || currentItem == null)
            return;

        Vector2 pointerScreenPosition = ReadPointerScreenPosition();

        if (!IsWithinScreen(pointerScreenPosition))
        {
            hasValidSurface = false;
            return;
        }

        Ray ray = sceneCamera.ScreenPointToRay(pointerScreenPosition);

        if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, placementMask, QueryTriggerInteraction.Ignore))
        {
            Vector3 requiredUp = placementUpReference != null ? placementUpReference.up : Vector3.up;
            hasValidSurface = Vector3.Dot(hit.normal, requiredUp) >= minUpDot;

            if (!hasValidSurface)
                return;

            targetPosition = hit.point + hit.normal * surfaceOffset + currentItem.placementOffset;

            if (!previewInitialized)
            {
                previewInstance.transform.SetPositionAndRotation(targetPosition, targetRotation);
                previewInitialized = true;
            }
            else
            {
                previewInstance.transform.position = Vector3.SmoothDamp(
                    previewInstance.transform.position,
                    targetPosition,
                    ref previewVelocity,
                    positionSmoothTime);

                float t = 1f - Mathf.Exp(-rotationSmoothSharpness * Time.deltaTime);
                previewInstance.transform.rotation = Quaternion.Slerp(
                    previewInstance.transform.rotation,
                    targetRotation,
                    t);
            }
        }
        else
        {
            hasValidSurface = false;
        }
    }

    private void PlaceCurrent()
    {
        Transform parent = placedObjectsParent != null ? placedObjectsParent : placementUpReference;

        GameObject placed = Instantiate(
            currentItem.placedPrefab,
            previewInstance.transform.position,
            Quaternion.Euler(currentItem.rotationEuler),
            parent
        );

        if (tiledWorldIllusion != null)
            tiledWorldIllusion.RegisterPlacedObject(placed.transform);

        CancelPlacement();
    }

    public void CancelPlacement()
    {
        if (previewInstance != null)
            Destroy(previewInstance);

        previewInstance = null;
        currentItem = null;
        previewVelocity = Vector3.zero;
        targetPosition = Vector3.zero;
        targetRotation = Quaternion.identity;
        isPlacing = false;
        hasValidSurface = false;
        previewInitialized = false;
        ignoreCurrentClick = false;
    }

    private Vector2 ReadPointerScreenPosition()
    {
        if (Mouse.current != null)
            return Mouse.current.position.ReadValue();

        if (Pointer.current != null)
            return Pointer.current.position.ReadValue();

        return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
    }

    private bool IsWithinScreen(Vector2 screenPos)
    {
        return screenPos.x >= 0f &&
               screenPos.x <= Screen.width &&
               screenPos.y >= 0f &&
               screenPos.y <= Screen.height;
    }

    private bool WasLeftMousePressed()
    {
        return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
    }

    private bool IsLeftMouseHeld()
    {
        return Mouse.current != null && Mouse.current.leftButton.isPressed;
    }

    private bool WasCancelPressed()
    {
        bool esc = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
        bool rightMouse = Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;
        return esc || rightMouse;
    }

    private bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    private void DisablePhysics(GameObject root)
    {
        Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
            colliders[i].enabled = false;

        Rigidbody[] rigidbodies = root.GetComponentsInChildren<Rigidbody>(true);
        for (int i = 0; i < rigidbodies.Length; i++)
        {
            rigidbodies[i].isKinematic = true;
            rigidbodies[i].detectCollisions = false;
        }
    }

    private void SetLayerRecursively(GameObject root, int layer)
    {
        root.layer = layer;

        for (int i = 0; i < root.transform.childCount; i++)
            SetLayerRecursively(root.transform.GetChild(i).gameObject, layer);
    }
}