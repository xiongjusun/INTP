using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

[DefaultExecutionOrder(-5000)]
public class BagUI : MonoBehaviour
{
    private class BagButtonEntry
    {
        public BagItemKind kind;
        public RectTransform rect;
        public Image image;
        public Text text;
        public string label;
    }

    [Header("Scene References")]
    public Camera worldCamera;
    public LayerMask groundMask = ~0;

    [Header("UI")]
    public bool createUIAutomatically = true;
    public Vector2 buttonSize = new Vector2(180f, 36f);
    public float doubleClickWindow = 0.30f;
    public bool showDebugMessages = true;
    public bool blockPlacementWhenPointerOverOtherUI = false;

    [Header("Emergency Visible Bag")]
    public bool drawImmediateModeBag = true;
    public float guiScale = 1f;

    private static BagUI activeBag;

    private bool hasSelectedItem;
    private BagItemKind selectedItem;
    private BagPlacedObject lastClickedPlacedObject;
    private float lastClickTime;

    private Canvas bagCanvas;
    private RectTransform panelRect;
    private Text statusText;
    private Font runtimeFont;
    private bool uiCreated;
    private readonly List<BagButtonEntry> buttonEntries = new List<BagButtonEntry>();

    private readonly BagItemKind[] itemOrder =
    {
        BagItemKind.Religion,
        BagItemKind.Political,
        BagItemKind.Infection,
        BagItemKind.Predator,
        BagItemKind.Creater,
        BagItemKind.Merriage,
        BagItemKind.SelfProduce
    };

    private void Awake()
    {
        if (activeBag != null && activeBag != this)
        {
            enabled = false;
            return;
        }

        activeBag = this;
        if (worldCamera == null) worldCamera = Camera.main;
    }

    private void Start()
    {
        EnsureReady();
    }

    private void OnEnable()
    {
        if (activeBag == null) activeBag = this;
    }

    private void EnsureReady()
    {
        if (worldCamera == null) worldCamera = Camera.main;
        if (createUIAutomatically && !uiCreated) CreateBagUI();
    }

    private void Update()
    {
        if (!enabled) return;
        if (worldCamera == null) worldCamera = Camera.main;
        if (createUIAutomatically && !uiCreated) CreateBagUI();
        if (worldCamera == null) return;

        if (LeftMousePressedThisFrame())
        {
            Vector3 screenPosition = GetPointerScreenPosition();

            // Block placement below the emergency IMGUI bag.
            // The actual button click is handled by OnGUI, which is more reliable in projects
            // where the normal UI/EventSystem has been changed.
            if (drawImmediateModeBag && PointerIsInsideImmediateBag(screenPosition)) return;

            // This makes the canvas bag clickable even if Unity Button.onClick is not firing.
            if (TryHandleManualCanvasBagClick(screenPosition)) return;

            HandleWorldMouseClick(screenPosition);
        }
    }

    private void OnGUI()
    {
        if (!drawImmediateModeBag || !enabled) return;

        Rect panel = GetImmediateBagRect();
        Rect oldPanel = panel;

        Matrix4x4 oldMatrix = GUI.matrix;
        if (!Mathf.Approximately(guiScale, 1f) && guiScale > 0f)
        {
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(guiScale, guiScale, 1f));
            panel = new Rect(panel.x / guiScale, panel.y / guiScale, panel.width / guiScale, panel.height / guiScale);
        }

        GUI.Box(panel, "BAG");

        float x = panel.x + 10f;
        float y = panel.y + 28f;
        float w = panel.width - 20f;
        float h = 28f;

        for (int i = 0; i < itemOrder.Length; i++)
        {
            BagItemKind kind = itemOrder[i];
            string label = GetBagLabel(kind);
            if (hasSelectedItem && selectedItem == kind) label = "✓ " + label;

            if (GUI.Button(new Rect(x, y, w, h), label))
            {
                SelectBagItem((int)kind);
            }

            y += h + 6f;
        }

        string status = hasSelectedItem
            ? "Selected: " + selectedItem + "\nClick the plane to place."
            : "Click item, then click plane.";
        GUI.Label(new Rect(x, y + 4f, w, 55f), status);

        GUI.matrix = oldMatrix;

        // Prevent IMGUI clicks from falling through to world placement when this method receives the event.
        if (Event.current != null && Event.current.type == EventType.MouseDown && oldPanel.Contains(Event.current.mousePosition))
        {
            Event.current.Use();
        }
    }

    public void SelectBagItem(int itemIndex)
    {
        selectedItem = (BagItemKind)itemIndex;
        hasSelectedItem = true;
        RefreshButtonVisuals();

        if (statusText != null)
        {
            statusText.text = "Selected: " + selectedItem + "\nClick the plane to place it.";
        }

        if (showDebugMessages) Debug.Log("Creature Simulation Bag: selected " + selectedItem);
    }

    private bool LeftMousePressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return true;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetMouseButtonDown(0);
#else
        return false;
#endif
    }

    private Vector3 GetPointerScreenPosition()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            Vector2 position = Mouse.current.position.ReadValue();
            return new Vector3(position.x, position.y, 0f);
        }

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            Vector2 position = Touchscreen.current.primaryTouch.position.ReadValue();
            return new Vector3(position.x, position.y, 0f);
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.mousePosition;
#else
        return new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
#endif
    }

    private bool PointerIsInsideImmediateBag(Vector3 screenPosition)
    {
        Rect panel = GetImmediateBagRect();

        // Update and Physics use bottom-left screen coordinates.
        // IMGUI uses top-left screen coordinates.
        Vector2 guiPoint = new Vector2(screenPosition.x, Screen.height - screenPosition.y);
        return panel.Contains(guiPoint);
    }

    private Rect GetImmediateBagRect()
    {
        float scale = guiScale > 0f ? guiScale : 1f;
        float width = 210f * scale;
        float height = 330f * scale;
        return new Rect(16f, Screen.height - height - 16f, width, height);
    }

    private bool TryHandleManualCanvasBagClick(Vector3 screenPosition)
    {
        if (panelRect == null) return false;
        if (!RectTransformUtility.RectangleContainsScreenPoint(panelRect, screenPosition, null)) return false;

        for (int i = 0; i < buttonEntries.Count; i++)
        {
            BagButtonEntry entry = buttonEntries[i];
            if (entry == null || entry.rect == null) continue;

            if (RectTransformUtility.RectangleContainsScreenPoint(entry.rect, screenPosition, null))
            {
                SelectBagItem((int)entry.kind);
                return true;
            }
        }

        // The click was on the bag panel but not on a button. Do not place things under the UI.
        return true;
    }

    private void HandleWorldMouseClick(Vector3 screenPosition)
    {
        if (blockPlacementWhenPointerOverOtherUI && PointerIsOverAnyOtherUI(screenPosition)) return;

        BagPlacedObject clickedPlaced = RaycastBagPlacedObject(screenPosition);
        if (clickedPlaced != null)
        {
            if (clickedPlaced == lastClickedPlacedObject && Time.time - lastClickTime <= doubleClickWindow)
            {
                clickedPlaced.RemoveFromBag();
                lastClickedPlacedObject = null;
                hasSelectedItem = false;
                RefreshButtonVisuals();
                SetStatus("Deleted placed " + clickedPlaced.kind + ".");
                return;
            }

            lastClickedPlacedObject = clickedPlaced;
            lastClickTime = Time.time;
        }

        if (!hasSelectedItem) return;

        Vector3 point;
        if (GetGroundPoint(screenPosition, out point))
        {
            PlaceSelectedItem(point);
            SetStatus("Placed: " + selectedItem + ".\nSelect another item or double-click placed objects to delete.");
            hasSelectedItem = false;
            RefreshButtonVisuals();
        }
        else
        {
            SetStatus("Could not find the plane. Make sure the scene has a ground plane/collider.");
        }
    }

    private bool PointerIsOverAnyOtherUI(Vector3 screenPosition)
    {
        if (panelRect != null && RectTransformUtility.RectangleContainsScreenPoint(panelRect, screenPosition, null)) return true;
        if (EventSystem.current == null) return false;
        return EventSystem.current.IsPointerOverGameObject();
    }

    private BagPlacedObject RaycastBagPlacedObject(Vector3 screenPosition)
    {
        Ray ray = worldCamera.ScreenPointToRay(screenPosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, 500f, ~0, QueryTriggerInteraction.Collide);
        float bestDistance = float.MaxValue;
        BagPlacedObject best = null;

        for (int i = 0; i < hits.Length; i++)
        {
            BagPlacedObject placed = hits[i].collider.GetComponentInParent<BagPlacedObject>();
            if (placed != null && hits[i].distance < bestDistance)
            {
                bestDistance = hits[i].distance;
                best = placed;
            }
        }

        return best;
    }

    private bool GetGroundPoint(Vector3 screenPosition, out Vector3 point)
    {
        Ray ray = worldCamera.ScreenPointToRay(screenPosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 500f, groundMask, QueryTriggerInteraction.Ignore))
        {
            point = hit.point;
            if (SimManager.HasInstance) point = SimManager.Instance.ClampToWorld(point);
            return true;
        }

        float groundY = SimManager.HasInstance ? SimManager.Instance.groundY : 0f;
        Plane plane = new Plane(Vector3.up, new Vector3(0f, groundY, 0f));
        float enter;
        if (plane.Raycast(ray, out enter))
        {
            point = ray.GetPoint(enter);
            if (SimManager.HasInstance) point = SimManager.Instance.ClampToWorld(point);
            return true;
        }

        point = Vector3.zero;
        return false;
    }

    private void PlaceSelectedItem(Vector3 point)
    {
        if (!SimManager.HasInstance)
        {
            SetStatus("No SimManager found. Add SimManager or use the bootstrap/setup menu.");
            return;
        }

        switch (selectedItem)
        {
            case BagItemKind.Religion:
            {
                PowerCenter center = SimManager.Instance.SpawnPowerCenter(PowerCenterType.Religion, point);
                AddPlacedComponent(center.gameObject, BagItemKind.Religion, false);
                break;
            }
            case BagItemKind.Political:
            {
                PowerCenter center = SimManager.Instance.SpawnPowerCenter(PowerCenterType.Political, point);
                AddPlacedComponent(center.gameObject, BagItemKind.Political, false);
                break;
            }
            case BagItemKind.Predator:
            {
                PredatorAgent predator = SimManager.Instance.SpawnPredator(point);
                AddPlacedComponent(predator.gameObject, BagItemKind.Predator, false);
                break;
            }
            case BagItemKind.Creater:
            {
                CreatureAgent creater = SimManager.Instance.SpawnCreature(point, 100f, true);
                AddPlacedComponent(creater.gameObject, BagItemKind.Creater, true);
                break;
            }
            case BagItemKind.Infection:
            case BagItemKind.Merriage:
            case BagItemKind.SelfProduce:
            {
                GameObject marker = CreateSourceMarker(selectedItem, point);
                AddPlacedComponent(marker, selectedItem, true);
                break;
            }
        }
    }

    private void AddPlacedComponent(GameObject obj, BagItemKind kind, bool countsAsSource)
    {
        if (obj == null) return;
        BagPlacedObject placed = obj.GetComponent<BagPlacedObject>();
        if (placed == null) placed = obj.AddComponent<BagPlacedObject>();
        placed.Initialize(kind, countsAsSource);
    }

    private GameObject CreateSourceMarker(BagItemKind kind, Vector3 point)
    {
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        marker.name = kind + " Source";
        marker.transform.position = point + Vector3.up * 0.15f;
        marker.transform.localScale = new Vector3(1.2f, 0.3f, 1.2f);

        Renderer renderer = marker.GetComponent<Renderer>();
        if (renderer != null)
        {
            if (kind == BagItemKind.Infection) renderer.material.color = new Color(0.6f, 0.0f, 0.7f);
            if (kind == BagItemKind.Merriage) renderer.material.color = new Color(1f, 0.45f, 0.8f);
            if (kind == BagItemKind.SelfProduce) renderer.material.color = new Color(0.2f, 1f, 0.7f);
        }

        return marker;
    }

    private void CreateBagUI()
    {
        EnsureCompatibleEventSystem();

        GameObject canvasObj = new GameObject("Creature Simulation Bag Canvas");
        canvasObj.transform.SetParent(null, false);
        canvasObj.transform.SetAsLastSibling();

        bagCanvas = canvasObj.AddComponent<Canvas>();
        bagCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        bagCanvas.overrideSorting = true;
        bagCanvas.sortingOrder = 30000;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        GameObject panel = new GameObject("Bag Panel - CLICK THESE BUTTONS");
        panel.transform.SetParent(canvasObj.transform, false);
        panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 0f);
        panelRect.anchorMax = new Vector2(0f, 0f);
        panelRect.pivot = new Vector2(0f, 0f);
        panelRect.anchoredPosition = new Vector2(16f, 16f);
        panelRect.sizeDelta = new Vector2(buttonSize.x + 20f, 340f);

        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.03f, 0.03f, 0.03f, 0.88f);
        panelImage.raycastTarget = true;

        buttonEntries.Clear();

        float y = 10f;
        AddLabel(panel.transform, "Bag Title", "BAG", 10f, y, buttonSize.x, 26f, 18, TextAnchor.MiddleCenter, FontStyle.Bold);
        y += 34f;

        AddButton(panel.transform, "Religion", BagItemKind.Religion, 10f, y);
        y += buttonSize.y + 6f;
        AddButton(panel.transform, "Political", BagItemKind.Political, 10f, y);
        y += buttonSize.y + 6f;
        AddButton(panel.transform, "Infection", BagItemKind.Infection, 10f, y);
        y += buttonSize.y + 6f;
        AddButton(panel.transform, "Predator", BagItemKind.Predator, 10f, y);
        y += buttonSize.y + 6f;
        AddButton(panel.transform, "Creater", BagItemKind.Creater, 10f, y);
        y += buttonSize.y + 6f;
        AddButton(panel.transform, "Merriage", BagItemKind.Merriage, 10f, y);
        y += buttonSize.y + 6f;
        AddButton(panel.transform, "SelfProduce", BagItemKind.SelfProduce, 10f, y);
        y += buttonSize.y + 8f;

        statusText = AddLabel(panel.transform, "Bag Status", "Click a bag item, then click the plane.", 10f, y, buttonSize.x, 48f, 13, TextAnchor.UpperLeft, FontStyle.Normal);

        uiCreated = true;
        RefreshButtonVisuals();

        if (showDebugMessages)
        {
            Debug.Log("Creature Simulation: Bag UI created with manual positions. You should see actual buttons now.");
        }
    }

    private Text AddLabel(Transform parent, string name, string message, float x, float yFromTop, float width, float height, int fontSize, TextAnchor alignment, FontStyle style)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.AddComponent<RectTransform>();
        SetTopLeftRect(rect, x, yFromTop, width, height);

        Text text = obj.AddComponent<Text>();
        text.text = message;
        text.alignment = alignment;
        text.color = Color.white;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.font = GetRuntimeFont();
        text.raycastTarget = false;
        return text;
    }

    private void EnsureCompatibleEventSystem()
    {
        EventSystem eventSystem = FindObjectOfType<EventSystem>();
        if (eventSystem == null)
        {
            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystem = eventSystemObject.AddComponent<EventSystem>();
        }

#if ENABLE_INPUT_SYSTEM
        StandaloneInputModule oldModule = eventSystem.GetComponent<StandaloneInputModule>();
        if (oldModule != null)
        {
            oldModule.enabled = false;
            Destroy(oldModule);
        }

        if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
        {
            eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
        }
#elif ENABLE_LEGACY_INPUT_MANAGER
        if (eventSystem.GetComponent<StandaloneInputModule>() == null)
        {
            eventSystem.gameObject.AddComponent<StandaloneInputModule>();
        }
#endif
    }

    private void AddButton(Transform parent, string label, BagItemKind kind, float x, float yFromTop)
    {
        GameObject buttonObj = new GameObject(label + " Button");
        buttonObj.transform.SetParent(parent, false);
        RectTransform rect = buttonObj.AddComponent<RectTransform>();
        SetTopLeftRect(rect, x, yFromTop, buttonSize.x, buttonSize.y);

        Image image = buttonObj.AddComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.95f);
        image.raycastTarget = true;

        Button button = buttonObj.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(delegate { SelectBagItem((int)kind); });

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Text text = textObj.AddComponent<Text>();
        text.text = label;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.black;
        text.fontSize = 15;
        text.fontStyle = FontStyle.Bold;
        text.font = GetRuntimeFont();
        text.raycastTarget = false;

        buttonEntries.Add(new BagButtonEntry
        {
            kind = kind,
            rect = rect,
            image = image,
            text = text,
            label = label
        });
    }

    private void SetTopLeftRect(RectTransform rect, float x, float yFromTop, float width, float height)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(x, -yFromTop);
        rect.sizeDelta = new Vector2(width, height);
    }

    private void RefreshButtonVisuals()
    {
        for (int i = 0; i < buttonEntries.Count; i++)
        {
            BagButtonEntry entry = buttonEntries[i];
            if (entry == null) continue;

            bool selected = hasSelectedItem && entry.kind == selectedItem;
            if (entry.image != null)
            {
                entry.image.color = selected ? new Color(0.35f, 0.9f, 1f, 1f) : new Color(1f, 1f, 1f, 0.95f);
            }

            if (entry.text != null)
            {
                entry.text.text = selected ? "✓ " + entry.label : entry.label;
            }
        }
    }

    private void SetStatus(string message)
    {
        if (statusText != null) statusText.text = message;
        if (showDebugMessages) Debug.Log("Creature Simulation Bag: " + message.Replace("\n", " "));
    }

    private string GetBagLabel(BagItemKind kind)
    {
        switch (kind)
        {
            case BagItemKind.Religion: return "Religion";
            case BagItemKind.Political: return "Political";
            case BagItemKind.Infection: return "Infection";
            case BagItemKind.Predator: return "Predator";
            case BagItemKind.Creater: return "Creater";
            case BagItemKind.Merriage: return "Merriage";
            case BagItemKind.SelfProduce: return "SelfProduce";
            default: return kind.ToString();
        }
    }

    private Font GetRuntimeFont()
    {
        if (runtimeFont != null) return runtimeFont;

        string[] builtinCandidates = { "LegacyRuntime.ttf", "Arial.ttf" };
        for (int i = 0; i < builtinCandidates.Length; i++)
        {
            try
            {
                runtimeFont = Resources.GetBuiltinResource<Font>(builtinCandidates[i]);
                if (runtimeFont != null) return runtimeFont;
            }
            catch
            {
                // Some Unity versions throw if the old built-in font name is requested.
            }
        }

        try
        {
            runtimeFont = Font.CreateDynamicFontFromOSFont(new[] { "Arial", "Helvetica", "Liberation Sans", "DejaVu Sans" }, 14);
        }
        catch
        {
            runtimeFont = null;
        }

        return runtimeFont;
    }

    private void OnDestroy()
    {
        if (activeBag == this) activeBag = null;
    }
}
