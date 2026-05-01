#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class SimulationPrefabCreator
{
    private const string RootFolder = "Assets/CreatureSimulation";
    private const string PrefabFolder = RootFolder + "/Prefabs";
    private const string MaterialFolder = RootFolder + "/Materials";

    private class PrefabSet
    {
        public GameObject creaturePrefab;
        public GameObject createrPrefab;
        public GameObject religionPrefab;
        public GameObject politicalPrefab;
        public GameObject predatorPrefab;
        public GameObject relationLinePrefab;
        public GameObject defaultBagSourcePrefab;
        public GameObject infectionSourcePrefab;
        public GameObject createrSourcePrefab;
        public GameObject merriageSourcePrefab;
        public GameObject selfProduceSourcePrefab;
    }

    [MenuItem("Tools/Creature Simulation/Safe Install In Current Scene (No Camera Or UI Settings Change)")]
    public static void SafeInstallInCurrentScene()
    {
        PrefabSet prefabs = CreateOrUpdateDefaultPrefabs();
        CreateOrUpdateSceneSafely(prefabs);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Creature Simulation safe install complete. Existing camera, existing Canvas UI, EventSystem, Input settings, and project layers were not changed.");
    }

    [MenuItem("Tools/Creature Simulation/Create Default Prefabs Only")]
    public static void CreateDefaultPrefabsOnly()
    {
        CreateOrUpdateDefaultPrefabs();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Creature Simulation default prefabs created/updated only. Scene, camera, UI, EventSystem, Input settings, and layers were not changed.");
    }

    private static PrefabSet CreateOrUpdateDefaultPrefabs()
    {
        EnsureFolder("Assets", "CreatureSimulation");
        EnsureFolder(RootFolder, "Prefabs");
        EnsureFolder(RootFolder, "Materials");

        Material creatureMat = MakeMaterial("Creature", new Color(0.1f, 0.8f, 0.2f));
        Material createrMat = MakeMaterial("Creater", new Color(0.2f, 0.6f, 1f));
        Material religionMat = MakeMaterial("Religion", new Color(1f, 0.8f, 0.1f));
        Material politicalMat = MakeMaterial("Political", new Color(0.9f, 0.2f, 0.2f));
        Material politicalAreaMat = MakeTransparentMaterial("PoliticalArea", new Color(0.9f, 0.2f, 0.2f, 0.25f));
        Material predatorMat = MakeMaterial("Predator", Color.black);
        Material lineMat = MakeMaterial("RelationLine", Color.white);

        Material infectionSourceMat = MakeMaterial("InfectionSource", new Color(0.6f, 0.0f, 0.7f));
        Material createrSourceMat = MakeMaterial("CreaterSource", new Color(0.2f, 0.6f, 1f));
        Material merriageSourceMat = MakeMaterial("MerriageSource", new Color(1f, 0.45f, 0.8f));
        Material selfProduceSourceMat = MakeMaterial("SelfProduceSource", new Color(0.2f, 1f, 0.7f));
        Material defaultSourceMat = MakeMaterial("DefaultBagSource", Color.white);

        PrefabSet prefabs = new PrefabSet();
        prefabs.creaturePrefab = CreateCreaturePrefab("Creature", false, creatureMat);
        prefabs.createrPrefab = CreateCreaturePrefab("Creater", true, createrMat);
        prefabs.religionPrefab = CreatePowerCenterPrefab("Religion", PowerCenterType.Religion, PrimitiveType.Sphere, religionMat, null);
        prefabs.politicalPrefab = CreatePowerCenterPrefab("Political", PowerCenterType.Political, PrimitiveType.Cube, politicalMat, politicalAreaMat);
        prefabs.predatorPrefab = CreatePredatorPrefab(predatorMat);
        prefabs.relationLinePrefab = CreateRelationLinePrefab(lineMat);

        prefabs.defaultBagSourcePrefab = CreateBagSourcePrefab("DefaultBagSource", defaultSourceMat);
        prefabs.infectionSourcePrefab = CreateBagSourcePrefab("InfectionSource", infectionSourceMat);
        prefabs.createrSourcePrefab = CreateBagSourcePrefab("CreaterSource", createrSourceMat);
        prefabs.merriageSourcePrefab = CreateBagSourcePrefab("MerriageSource", merriageSourceMat);
        prefabs.selfProduceSourcePrefab = CreateBagSourcePrefab("SelfProduceSource", selfProduceSourceMat);

        return prefabs;
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
        agent.overrideNormalAndCreaterMaterialColor = false;
        agent.overrideRemainAndInfectedMaterialColor = true;
        agent.useCreaterPrefabVisualWhenConverted = true;

        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb == null) rb = obj.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true;

        Collider col = obj.GetComponent<Collider>();
        if (col != null) col.isTrigger = true;

        return SavePrefab(obj, name);
    }

    private static GameObject CreatePowerCenterPrefab(string name, PowerCenterType type, PrimitiveType primitive, Material material, Material politicalAreaMaterial)
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
        center.showPoliticalAreaCylinder = type == PowerCenterType.Political;
        center.politicalAreaMaterial = politicalAreaMaterial;

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
        predator.overrideMaterialColor = false;

        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb == null) rb = obj.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true;

        Collider col = obj.GetComponent<Collider>();
        if (col != null) col.isTrigger = true;

        return SavePrefab(obj, "Predator");
    }

    private static GameObject CreateBagSourcePrefab(string name, Material material)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        obj.name = name;
        obj.transform.localScale = new Vector3(1.2f, 0.3f, 1.2f);

        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null) renderer.sharedMaterial = material;

        Collider col = obj.GetComponent<Collider>();
        if (col == null) obj.AddComponent<BoxCollider>();

        return SavePrefab(obj, name);
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

    private static void CreateOrUpdateSceneSafely(PrefabSet prefabs)
    {
        bool createdManager = false;
        SimManager manager = Object.FindObjectOfType<SimManager>();
        if (manager == null)
        {
            GameObject managerObj = new GameObject("Simulation Manager");
            manager = managerObj.AddComponent<SimManager>();
            createdManager = true;
        }

        if (manager.creaturePrefab == null) manager.creaturePrefab = prefabs.creaturePrefab;
        if (manager.createrPrefab == null) manager.createrPrefab = prefabs.createrPrefab;
        if (manager.religionPrefab == null) manager.religionPrefab = prefabs.religionPrefab;
        if (manager.politicalPrefab == null) manager.politicalPrefab = prefabs.politicalPrefab;
        if (manager.predatorPrefab == null) manager.predatorPrefab = prefabs.predatorPrefab;
        if (manager.relationLinePrefab == null) manager.relationLinePrefab = prefabs.relationLinePrefab;

        if (manager.defaultBagSourcePrefab == null) manager.defaultBagSourcePrefab = prefabs.defaultBagSourcePrefab;
        if (manager.infectionSourcePrefab == null) manager.infectionSourcePrefab = prefabs.infectionSourcePrefab;
        if (manager.createrSourcePrefab == null) manager.createrSourcePrefab = prefabs.createrSourcePrefab;
        if (manager.merriageSourcePrefab == null) manager.merriageSourcePrefab = prefabs.merriageSourcePrefab;
        if (manager.selfProduceSourcePrefab == null) manager.selfProduceSourcePrefab = prefabs.selfProduceSourcePrefab;

        manager.createrSourceUpgradeInterval = 5f;
        if (createdManager)
        {
            manager.worldSize = new Vector2(80f, 80f);
            manager.startingCreatureCount = 12;
        }

        BagUI bag = Object.FindObjectOfType<BagUI>();
        if (bag == null)
        {
            bag = manager.gameObject.AddComponent<BagUI>();
        }

        if (bag.worldCamera == null)
        {
            bag.worldCamera = Camera.main;
        }

        bag.createUIAutomatically = true;
        bag.blockPlacementWhenPointerOverOtherUI = true;
        bag.drawImmediateModeBag = false;
        bag.screenMargin = new Vector2(16f, 16f);
        bag.groundMask = ~0;
        bag.preferBagPlacementSurface = true;

        EditorUtility.SetDirty(manager);
        EditorUtility.SetDirty(bag);

        Debug.Log("Safe scene setup done. For best placement, add BagPlacementSurface to your existing ground/click surface. No camera transform, Canvas, EventSystem, Input settings, layer settings, or ProjectSettings were modified.");
    }

    private static Material MakeMaterial(string name, Color color)
    {
        string path = MaterialFolder + "/" + name + ".mat";
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
        {
            SetMaterialColor(existing, color);
            return existing;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Sprites/Default");

        Material material = new Material(shader);
        SetMaterialColor(material, color);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static Material MakeTransparentMaterial(string name, Color color)
    {
        Material material = MakeMaterial(name, color);

        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
        if (material.HasProperty("_Mode")) material.SetFloat("_Mode", 3f);
        if (material.HasProperty("_SrcBlend")) material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (material.HasProperty("_DstBlend")) material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        if (material.HasProperty("_ZWrite")) material.SetInt("_ZWrite", 0);
        material.EnableKeyword("_ALPHABLEND_ON");
        material.renderQueue = 3000;

        return material;
    }

    private static void SetMaterialColor(Material material, Color color)
    {
        if (material == null) return;
        material.color = color;
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
    }

    private static void EnsureFolder(string parent, string folderName)
    {
        string path = parent + "/" + folderName;
        if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, folderName);
    }
}
#endif
