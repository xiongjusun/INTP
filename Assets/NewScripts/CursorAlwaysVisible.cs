using UnityEngine;

[DefaultExecutionOrder(32000)]
public class CursorAlwaysVisible : MonoBehaviour
{
    [Header("Cursor")]
    public bool alwaysShowCursor = true;
    public bool unlockCursor = true;

    private void Awake()
    {
        ApplyCursorState();
    }

    private void OnEnable()
    {
        ApplyCursorState();
    }

    private void Start()
    {
        ApplyCursorState();
    }

    private void Update()
    {
        ApplyCursorState();
    }

    private void LateUpdate()
    {
        ApplyCursorState();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            ApplyCursorState();
        }
    }

    private void OnApplicationPause(bool isPaused)
    {
        if (!isPaused)
        {
            ApplyCursorState();
        }
    }

    private void ApplyCursorState()
    {
        if (unlockCursor)
        {
            Cursor.lockState = CursorLockMode.None;
        }

        if (alwaysShowCursor)
        {
            Cursor.visible = true;
        }
    }
}