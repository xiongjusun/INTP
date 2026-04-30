#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class SimulationPrefabCreator
{
    private const string RootFolder = "Assets/CreatureSimulation";
    private const string PrefabFolder = RootFolder + "/Prefabs";
    private const string MaterialFolder = RootFolder + "/Materials";

    [MenuItem("Tools/Creature Simulation/Create Default Prefabs + Scene Objects")]
    public static void CreateDefaults()
    {
        EnsureFolder("Assets", "CreatureSimulation");
        EnsureFolder(RootFolder, "Prefabs");
        EnsureFolder(RootFolder, "Materials");

        Material creatureMat = MakeMaterial("Creature", new Color(0.1f, 0.8f, 0.2f));
        Material createrMat = MakeMaterial("Creater", new Color(0.2f, 0.6f, 1f));
        Material religionMat = MakeMaterial("Religion", new Color(1f, 0.8f, 0.1f));
        Material politicalMat = MakeMaterial("Political", new Color(0.9f, 0.2f, 0.2f));
        Material predatorMat = MakeMaterial("Predator", Color.black);
        Material lineMat = MakeMaterial("RelationLine", Color.white);

        GameObject creaturePrefab = CreateCreaturePrefab("Creature", false, creatureMat);
        GameObject createrPrefab = CreateCreaturePrefab("Creater", true, createrMat);
        GameObject religionPrefab = CreatePowerCenterPrefab("Religion", PowerCenterType.Religion, PrimitiveType.Sphere, religionMat);
        GameObject politicalPrefab = CreatePowerCenterPrefab("Political", PowerCenterType.Political, PrimitiveType.Cube, politicalMat);
        GameObject predatorPrefab = CreatePredatorPrefab(predatorMat);
        GameObject relationLinePrefab = CreateRelationLinePrefab(lineMat);

        CreateOrUpdateScene(creaturePrefab, createrPrefab, religionPrefab, politicalPrefab, predatorPrefab, relationLinePrefab);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Creature Simulation setup complete. Press Play, then use the bottom-left bag UI to place systems.");
    }

    private static GameObject CreateCreaturePrefab(string name, bool creater, Material material)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        obj.name = name;
        obj.transform.localScale = creater ? new Vector3(1.25f, 1.25f, 1.25f) : Vector3.one;

        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null) renderer.sharedMaterial = material;

        CreatureAgent agent = obj.AddComponent<CreatureAgent>();
        agent.isCreater = creater;
        agent.life = 100f;
        agent.moveSpeed = creater ? 3.4f : 3f;

        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb == null) rb = obj.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true;

        Collider col = obj.GetComponent<Collider>();
        if (col != null) col.isTrigger = true;

        return SavePrefab(obj, name);
    }

    private static GameObject CreatePowerCenterPrefab(string name, PowerCenterType type, PrimitiveType primitive, Material material)
    {
        GameObject obj = GameObject.CreatePrimitive(primitive);
        obj.name = name;
        obj.transform.localScale = type == PowerCenterType.Religion ? Vector3.one * 1.5f : Vector3.one * 2f;

        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null) renderer.sharedMaterial = material;

        PowerCenter center = obj.AddComponent<PowerCenter>();
        center.type = type;
        center.life = 100f;
        center.baseRadius = type == PowerCenterType.Religion ? 12f : 10f;

        return SavePrefab(obj, name);
    }

    private static GameObject CreatePredatorPrefab(Material material)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        obj.name = "Predator";
        obj.transform.localScale = new Vector3(1.2f, 1.4f, 1.2f);

        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null) renderer.sharedMaterial = material;

        PredatorAgent predator = obj.AddComponent<PredatorAgent>();
        predator.moveSpeed = 5f;

        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb == null) rb = obj.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true;

        Collider col = obj.GetComponent<Collider>();
        if (col != null) col.isTrigger = true;

        return SavePrefab(obj, "Predator");
    }

    private static GameObject CreateRelationLinePrefab(Material material)
    {
        GameObject obj = new GameObject("RelationLine");
        LineRenderer line = obj.AddComponent<LineRenderer>();
        line.sharedMaterial = material;
        line.useWorldSpace = true;
        line.positionCount = 24;
        line.startWidth = 0.08f;
        line.endWidth = 0.08f;
        obj.AddComponent<RelationLine>();
        return SavePrefab(obj, "RelationLine");
    }

    private static GameObject SavePrefab(GameObject obj, string name)
    {
        string path = PrefabFolder + "/" + name + ".prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(obj, path);
        Object.DestroyImmediate(obj);
        return prefab;
    }

    private static void CreateOrUpdateScene(GameObject creaturePrefab, GameObject createrPrefab, GameObject religionPrefab, GameObject politicalPrefab, GameObject predatorPrefab, GameObject relationLinePrefab)
    {
        int groundLayer = EnsureLayer("Ground");

        GameObject plane = GameObject.Find("Simulation Plane");
        if (plane == null)
        {
            plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            plane.name = "Simulation Plane";
            plane.transform.position = Vector3.zero;
            plane.transform.localScale = new Vector3(8f, 1f, 8f);
        }
        plane.layer = groundLayer;

        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject cameraObj = new GameObject("Main Camera");
            cam = cameraObj.AddComponent<Camera>();
            cameraObj.tag = "MainCamera";
        }
        cam.transform.position = new Vector3(0f, 55f, -55f);
        cam.transform.rotation = Quaternion.Euler(55f, 0f, 0f);
        cam.orthographic = true;
        cam.orthographicSize = 48f;

        if (Object.FindObjectOfType<Light>() == null)
        {
            GameObject lightObj = new GameObject("Directional Light");
            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        SimManager manager = Object.FindObjectOfType<SimManager>();
        if (manager == null)
        {
            GameObject managerObj = new GameObject("Simulation Manager");
            manager = managerObj.AddComponent<SimManager>();
        }

        manager.creaturePrefab = creaturePrefab;
        manager.createrPrefab = createrPrefab;
        manager.religionPrefab = religionPrefab;
        manager.politicalPrefab = politicalPrefab;
        manager.predatorPrefab = predatorPrefab;
        manager.relationLinePrefab = relationLinePrefab;
        manager.worldSize = new Vector2(80f, 80f);
        manager.startingCreatureCount = 12;

        BagUI bag = Object.FindObjectOfType<BagUI>();
        if (bag == null) bag = manager.gameObject.AddComponent<BagUI>();
        bag.worldCamera = cam;
        bag.groundMask = 1 << groundLayer;
    }

    private static Material MakeMaterial(string name, Color color)
    {
        string path = MaterialFolder + "/" + name + ".mat";
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
        {
            existing.color = color;
            return existing;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Sprites/Default");

        Material material = new Material(shader);
        material.color = color;
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static void EnsureFolder(string parent, string folderName)
    {
        string path = parent + "/" + folderName;
        if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, folderName);
    }

    private static int EnsureLayer(string layerName)
    {
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layers = tagManager.FindProperty("layers");

        for (int i = 0; i < layers.arraySize; i++)
        {
            SerializedProperty layer = layers.GetArrayElementAtIndex(i);
            if (layer.stringValue == layerName) return i;
        }

        for (int i = 8; i < layers.arraySize; i++)
        {
            SerializedProperty layer = layers.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(layer.stringValue))
            {
                layer.stringValue = layerName;
                tagManager.ApplyModifiedProperties();
                return i;
            }
        }

        Debug.LogWarning("No empty user layer found. Using Default layer for placement ground.");
        return 0;
    }
}
#endif
