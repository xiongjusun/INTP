using UnityEngine;

[DefaultExecutionOrder(-10000)]
public class CreatureSimulationBootstrap : MonoBehaviour
{
    public bool createRuntimeSetup = true;
    public bool createPlaneIfMissing = true;
    public bool createCameraIfMissing = true;
    public bool createLightIfMissing = true;
    public bool createManagerIfMissing = true;
    public bool createBagIfMissing = true;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreateBootstrap()
    {
        // If the setup menu was not used, this guarantees the simulation still has a manager and a visible bag.
        if (FindObjectOfType<CreatureSimulationBootstrap>() != null) return;

        bool needsBootstrap = FindObjectOfType<SimManager>() == null || FindObjectOfType<BagUI>() == null;
        if (!needsBootstrap) return;

        GameObject obj = new GameObject("Creature Simulation Bootstrap");
        obj.AddComponent<CreatureSimulationBootstrap>();
    }

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
        }

        if (createCameraIfMissing && cam == null)
        {
            GameObject cameraObj = new GameObject("Main Camera");
            cam = cameraObj.AddComponent<Camera>();
            cameraObj.tag = "MainCamera";
            cam.transform.position = new Vector3(0f, 55f, -55f);
            cam.transform.rotation = Quaternion.Euler(55f, 0f, 0f);
            cam.orthographic = true;
            cam.orthographicSize = 48f;
        }
        else if (cam != null)
        {
            // Put the camera in a useful view only if it looks unconfigured for this top-down prototype.
            if (cam.transform.position == Vector3.zero)
            {
                cam.transform.position = new Vector3(0f, 55f, -55f);
                cam.transform.rotation = Quaternion.Euler(55f, 0f, 0f);
                cam.orthographic = true;
                cam.orthographicSize = 48f;
            }
        }

        if (createLightIfMissing && FindObjectOfType<Light>() == null)
        {
            GameObject lightObj = new GameObject("Directional Light");
            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        SimManager manager = FindObjectOfType<SimManager>();
        if (createManagerIfMissing && manager == null)
        {
            GameObject managerObj = new GameObject("Simulation Manager");
            manager = managerObj.AddComponent<SimManager>();
            manager.worldSize = new Vector2(80f, 80f);
            manager.startingCreatureCount = 12;
        }

        if (createBagIfMissing && FindObjectOfType<BagUI>() == null)
        {
            GameObject bagHost = manager != null ? manager.gameObject : new GameObject("Bag UI Host");
            BagUI bag = bagHost.AddComponent<BagUI>();
            bag.worldCamera = cam;
            bag.groundMask = ~0;
            bag.createUIAutomatically = true;
        }
    }
}
