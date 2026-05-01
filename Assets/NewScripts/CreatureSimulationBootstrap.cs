using UnityEngine;

/// <summary>
/// Safe bootstrap for existing projects.
/// It does NOT auto-run, does NOT change your camera, and does NOT change your existing UI/EventSystem.
/// Use it only if you want a component-based setup instead of the editor menu.
/// </summary>
[DefaultExecutionOrder(-10000)]
public class CreatureSimulationBootstrap : MonoBehaviour
{
    [Header("Safe Runtime Setup")]
    public bool createRuntimeSetup = false;
    public bool createManagerIfMissing = true;
    public bool createBagIfMissing = true;
    public bool assignMainCameraToBagIfAvailable = true;

    [Header("Optional Helpers - Off By Default")]
    public bool createPlaneIfMissing = false;
    public bool createCameraIfMissing = false;
    public bool createLightIfMissing = false;

    private void Awake()
    {
        if (createRuntimeSetup) CreateRuntimeSetup();
    }

    public void CreateRuntimeSetup()
    {
        Camera cam = Camera.main;

        if (createPlaneIfMissing && GameObject.Find("Simulation Plane") == null)
        {
            GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            plane.name = "Simulation Plane";
            plane.transform.position = Vector3.zero;
            plane.transform.localScale = new Vector3(8f, 1f, 8f);
            if (plane.GetComponent<BagPlacementSurface>() == null) plane.AddComponent<BagPlacementSurface>();
        }

        if (createCameraIfMissing && cam == null)
        {
            GameObject cameraObj = new GameObject("Main Camera");
            cam = cameraObj.AddComponent<Camera>();
            cameraObj.tag = "MainCamera";
            // No forced position/rotation here. Configure this camera yourself if you enable this option.
        }

        if (createLightIfMissing && FindObjectOfType<Light>() == null)
        {
            GameObject lightObj = new GameObject("Directional Light");
            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
        }

        SimManager manager = FindObjectOfType<SimManager>();
        if (createManagerIfMissing && manager == null)
        {
            GameObject managerObj = new GameObject("Simulation Manager");
            manager = managerObj.AddComponent<SimManager>();
        }

        if (createBagIfMissing && FindObjectOfType<BagUI>() == null)
        {
            GameObject bagHost = manager != null ? manager.gameObject : new GameObject("Bag UI Host");
            BagUI bag = bagHost.AddComponent<BagUI>();
            if (assignMainCameraToBagIfAvailable) bag.worldCamera = cam;
            bag.groundMask = ~0;
            bag.createUIAutomatically = true;
            bag.blockPlacementWhenPointerOverOtherUI = true;
            bag.drawImmediateModeBag = false;
        }
    }
}
