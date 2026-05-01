using System.Collections.Generic;
using UnityEngine;

public class SimManager : MonoBehaviour
{
    public static SimManager Instance { get; private set; }
    public static bool HasInstance { get { return Instance != null; } }

    [Header("Prefab References")]
    public GameObject creaturePrefab;
    public GameObject createrPrefab;
    public GameObject religionPrefab;
    public GameObject politicalPrefab;
    public GameObject predatorPrefab;
    public GameObject relationLinePrefab;

    [Header("Bag Source Prefabs")]
    public GameObject defaultBagSourcePrefab;
    public GameObject infectionSourcePrefab;
    public GameObject createrSourcePrefab;
    public GameObject merriageSourcePrefab;
    public GameObject selfProduceSourcePrefab;

    [Header("World")]
    public Vector3 spawnCenter = new Vector3(70f, 0f, 70f);
    public Vector2 worldSize = new Vector2(80f, 80f);
    public float groundY = 0f;
    public Transform spawnedRoot;
    public int startingCreatureCount = 12;

    [Header("Physics Spawn")]
    public float agentSpawnYOffset = 1f;
    public bool addAgentSpawnYOffset = true;

    [Header("Creature Visual Colors")]
    public Color creatureColor = new Color(0.1f, 0.8f, 0.2f);
    public Color createrColor = new Color(0.2f, 0.6f, 1f);
    public Color remainColor = new Color(0.35f, 0.35f, 0.35f);
    public Color infectedColor = new Color(0.65f, 0.1f, 0.8f);

    [Header("Reproduce Settings")]
    public float marriageDistance = 4f;
    public float baseMarriageChance = 0.60f;
    public float marriageCheckInterval = 1.5f;
    public float reproduceCooldown = 8f;
    public float religionReproductionMultiplier = 1.5f;
    public float selfProduceCheckInterval = 4f;
    public float selfProduceChancePerCheck = 0.15f;
    public float selfProduceMinimumLife = 75f;

    [Header("Disease / Bag Timers")]
    public float infectionSourceInterval = 50f;
    public float createrSourceUpgradeInterval = 5f;
    public float politicalInfectionMultiplier = 0.35f;

    [Header("Spawn Offsets")]
    public float childSpawnRadius = 2f;
    public float selfProduceSpawnRadius = 1.5f;
    public float fallbackSourceMarkerYOffset = 0.15f;

    public readonly List<CreatureAgent> agents = new List<CreatureAgent>();
    public readonly List<PowerCenter> powerCenters = new List<PowerCenter>();
    public readonly List<PredatorAgent> predators = new List<PredatorAgent>();

    private int nextId = 1;
    private readonly Dictionary<string, RelationLine> relationLines = new Dictionary<string, RelationLine>();

    private int infectionSourceCount;
    private int createrUpgradeSourceCount;
    private int marriageSourceCount;
    private int selfProduceSourceCount;

    private float nextMarriageScanTime;
    private float nextSelfProduceScanTime;
    private float nextInfectionSourceTime;
    private float nextCreaterUpgradeTime;

    public bool MarriageEnabled { get { return marriageSourceCount > 0; } }
    public bool SelfProduceEnabled { get { return selfProduceSourceCount > 0; } }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (spawnedRoot == null) spawnedRoot = transform;
        if (createrSourceUpgradeInterval <= 0f) createrSourceUpgradeInterval = 5f;
    }

    private void Start()
    {
        for (int i = 0; i < startingCreatureCount; i++)
        {
            SpawnCreature(RandomWorldPoint(), 100f, false);
        }
    }

    private void Update()
    {
        if (MarriageEnabled && Time.time >= nextMarriageScanTime)
        {
            nextMarriageScanTime = Time.time + marriageCheckInterval;
            ScanForMarriage();
        }

        if (SelfProduceEnabled && Time.time >= nextSelfProduceScanTime)
        {
            nextSelfProduceScanTime = Time.time + selfProduceCheckInterval;
            ScanForSelfProduce();
        }

        if (infectionSourceCount > 0 && Time.time >= nextInfectionSourceTime)
        {
            nextInfectionSourceTime = Time.time + infectionSourceInterval;
            for (int i = 0; i < infectionSourceCount; i++) InfectRandomAgent();
        }

        if (createrUpgradeSourceCount > 0 && Time.time >= nextCreaterUpgradeTime)
        {
            nextCreaterUpgradeTime = Time.time + createrSourceUpgradeInterval;

            for (int i = 0; i < createrUpgradeSourceCount; i++)
            {
                UpgradeRandomCreatureToCreater();
            }
        }
    }

    public void ChangeBagSource(BagItemKind kind, int delta)
    {
        switch (kind)
        {
            case BagItemKind.Infection:
                {
                    int oldCount = infectionSourceCount;
                    infectionSourceCount = Mathf.Max(0, infectionSourceCount + delta);
                    if (oldCount == 0 && infectionSourceCount > 0) nextInfectionSourceTime = Time.time + infectionSourceInterval;
                    break;
                }
            case BagItemKind.Creater:
                {
                    int oldCount = createrUpgradeSourceCount;
                    createrUpgradeSourceCount = Mathf.Max(0, createrUpgradeSourceCount + delta);
                    if (oldCount == 0 && createrUpgradeSourceCount > 0) nextCreaterUpgradeTime = Time.time + createrSourceUpgradeInterval;
                    break;
                }
            case BagItemKind.Merriage:
                marriageSourceCount = Mathf.Max(0, marriageSourceCount + delta);
                break;
            case BagItemKind.SelfProduce:
                selfProduceSourceCount = Mathf.Max(0, selfProduceSourceCount + delta);
                break;
        }
    }

    public void RegisterAgent(CreatureAgent agent)
    {
        if (agent == null) return;
        if (agent.simId == 0) agent.simId = nextId++;
        if (!agents.Contains(agent)) agents.Add(agent);
    }

    public void UnregisterAgent(CreatureAgent agent)
    {
        if (agent == null) return;
        agents.Remove(agent);
        RemoveRelationsOf(agent);
    }

    public void RegisterPowerCenter(PowerCenter center)
    {
        if (center != null && !powerCenters.Contains(center)) powerCenters.Add(center);
    }

    public void UnregisterPowerCenter(PowerCenter center)
    {
        if (center == null) return;
        powerCenters.Remove(center);

        for (int i = agents.Count - 1; i >= 0; i--)
        {
            if (agents[i] != null && agents[i].currentPower == center)
            {
                agents[i].SetPowerCenter(null);
            }
        }
    }

    public void RegisterPredator(PredatorAgent predator)
    {
        if (predator != null && !predators.Contains(predator)) predators.Add(predator);
    }

    public void UnregisterPredator(PredatorAgent predator)
    {
        predators.Remove(predator);
    }

    public CreatureAgent SpawnCreature(Vector3 position, float life, bool asCreater)
    {
        GameObject prefab = asCreater && createrPrefab != null ? createrPrefab : creaturePrefab;
        GameObject obj;
        bool usedPrefab = prefab != null;
        Vector3 spawnPosition = GetAgentSpawnPosition(position);

        if (usedPrefab)
        {
            obj = Instantiate(prefab, spawnPosition, Quaternion.identity, spawnedRoot);
        }
        else
        {
            obj = GameObject.CreatePrimitive(asCreater ? PrimitiveType.Capsule : PrimitiveType.Sphere);
            obj.transform.SetParent(spawnedRoot);
            obj.transform.position = spawnPosition;
        }

        obj.name = asCreater ? "Creater" : "Creature";

        CreatureAgent agent = obj.GetComponent<CreatureAgent>();
        if (agent == null) agent = obj.AddComponent<CreatureAgent>();

        if (!usedPrefab)
        {
            agent.overrideNormalAndCreaterMaterialColor = true;
        }

        agent.Initialize(asCreater, life);
        RegisterAgent(agent);
        return agent;
    }

    public PowerCenter SpawnPowerCenter(PowerCenterType type, Vector3 position)
    {
        GameObject prefab = type == PowerCenterType.Religion ? religionPrefab : politicalPrefab;
        GameObject obj;
        Vector3 spawnPosition = ClampToWorld(position);

        if (prefab != null)
        {
            obj = Instantiate(prefab, spawnPosition, Quaternion.identity, spawnedRoot);
        }
        else
        {
            obj = GameObject.CreatePrimitive(type == PowerCenterType.Religion ? PrimitiveType.Sphere : PrimitiveType.Cube);
            obj.transform.SetParent(spawnedRoot);
            obj.transform.position = spawnPosition;
        }

        obj.name = type.ToString();

        PowerCenter center = obj.GetComponent<PowerCenter>();
        if (center == null) center = obj.AddComponent<PowerCenter>();
        center.type = type;

        RegisterPowerCenter(center);
        return center;
    }

    public PredatorAgent SpawnPredator(Vector3 position)
    {
        GameObject obj;
        Vector3 spawnPosition = GetAgentSpawnPosition(position);

        if (predatorPrefab != null)
        {
            obj = Instantiate(predatorPrefab, spawnPosition, Quaternion.identity, spawnedRoot);
        }
        else
        {
            obj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            obj.transform.SetParent(spawnedRoot);
            obj.transform.position = spawnPosition;
        }

        obj.name = "Predator";

        PredatorAgent predator = obj.GetComponent<PredatorAgent>();
        if (predator == null) predator = obj.AddComponent<PredatorAgent>();

        RegisterPredator(predator);
        return predator;
    }

    public GameObject SpawnBagSourceMarker(BagItemKind kind, Vector3 position)
    {
        GameObject prefab = GetBagSourcePrefab(kind);
        GameObject obj;
        Vector3 clampedPosition = ClampToWorld(position);

        if (prefab != null)
        {
            obj = Instantiate(prefab, clampedPosition, Quaternion.identity, spawnedRoot);
            obj.name = kind + " Source";
        }
        else
        {
            obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            obj.name = kind + " Source";
            obj.transform.SetParent(spawnedRoot);
            obj.transform.position = clampedPosition + Vector3.up * fallbackSourceMarkerYOffset;
            obj.transform.localScale = new Vector3(1.2f, 0.3f, 1.2f);
            ApplyDefaultSourceMarkerVisual(obj, kind);
        }

        EnsureSourceMarkerCanBeClicked(obj);
        return obj;
    }

    private GameObject GetBagSourcePrefab(BagItemKind kind)
    {
        switch (kind)
        {
            case BagItemKind.Infection:
                return infectionSourcePrefab != null ? infectionSourcePrefab : defaultBagSourcePrefab;
            case BagItemKind.Creater:
                return createrSourcePrefab != null ? createrSourcePrefab : defaultBagSourcePrefab;
            case BagItemKind.Merriage:
                return merriageSourcePrefab != null ? merriageSourcePrefab : defaultBagSourcePrefab;
            case BagItemKind.SelfProduce:
                return selfProduceSourcePrefab != null ? selfProduceSourcePrefab : defaultBagSourcePrefab;
            default:
                return defaultBagSourcePrefab;
        }
    }

    private void EnsureSourceMarkerCanBeClicked(GameObject obj)
    {
        if (obj == null) return;

        Collider existingCollider = obj.GetComponentInChildren<Collider>();
        if (existingCollider == null)
        {
            obj.AddComponent<BoxCollider>();
        }
    }

    private void ApplyDefaultSourceMarkerVisual(GameObject obj, BagItemKind kind)
    {
        if (obj == null) return;

        Renderer renderer = obj.GetComponentInChildren<Renderer>();
        if (renderer == null) return;

        switch (kind)
        {
            case BagItemKind.Infection:
                renderer.material.color = new Color(0.6f, 0.0f, 0.7f);
                break;
            case BagItemKind.Creater:
                renderer.material.color = new Color(0.2f, 0.6f, 1f);
                break;
            case BagItemKind.Merriage:
                renderer.material.color = new Color(1f, 0.45f, 0.8f);
                break;
            case BagItemKind.SelfProduce:
                renderer.material.color = new Color(0.2f, 1f, 0.7f);
                break;
            default:
                renderer.material.color = Color.white;
                break;
        }
    }

    public Vector3 RandomWorldPoint()
    {
        float halfX = worldSize.x * 0.5f;
        float halfZ = worldSize.y * 0.5f;

        float x = UnityEngine.Random.Range(spawnCenter.x - halfX, spawnCenter.x + halfX);
        float z = UnityEngine.Random.Range(spawnCenter.z - halfZ, spawnCenter.z + halfZ);

        return new Vector3(x, spawnCenter.y, z);
    }

    public Vector3 RandomOffset(float radius)
    {
        Vector2 v = UnityEngine.Random.insideUnitCircle * radius;
        return new Vector3(v.x, 0f, v.y);
    }

    public Vector3 GetAgentSpawnPosition(Vector3 position)
    {
        Vector3 result = ClampToWorld(position);

        if (addAgentSpawnYOffset)
        {
            result.y += agentSpawnYOffset;
        }

        return result;
    }

    public Vector3 ClampToWorld(Vector3 position)
    {
        float halfX = worldSize.x * 0.5f;
        float halfZ = worldSize.y * 0.5f;

        position.x = Mathf.Clamp(position.x, spawnCenter.x - halfX, spawnCenter.x + halfX);
        position.z = Mathf.Clamp(position.z, spawnCenter.z - halfZ, spawnCenter.z + halfZ);

        // Important:
        // Do NOT force position.y to groundY here.
        // Creature, Creater, and Predator now use gravity and environment colliders for height.
        return position;
    }

    public Vector3 ClampToWorldAtGroundY(Vector3 position)
    {
        position = ClampToWorld(position);
        position.y = groundY;
        return position;
    }

    public CreatureAgent FindClosestRemain(Vector3 from, float maxDistance, PowerCenter politicalCenter, bool createrMayLeaveSlightly)
    {
        CreatureAgent best = null;
        float bestSqr = maxDistance * maxDistance;

        for (int i = 0; i < agents.Count; i++)
        {
            CreatureAgent candidate = agents[i];
            if (candidate == null || !candidate.isRemain) continue;

            if (politicalCenter != null)
            {
                float allowedRadius = politicalCenter.CurrentRadius;
                if (createrMayLeaveSlightly) allowedRadius += politicalCenter.createrExtraExitDistance;

                float fromCenter = FlatDistance(candidate.transform.position, politicalCenter.transform.position);
                if (fromCenter > allowedRadius) continue;
            }

            float sqr = FlatSqrDistance(candidate.transform.position, from);
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                best = candidate;
            }
        }

        return best;
    }

    public List<CreatureAgent> GetLivingAgentsWithin(Vector3 center, float radius, CreatureAgent ignore = null)
    {
        List<CreatureAgent> result = new List<CreatureAgent>();
        float sqrRadius = radius * radius;

        for (int i = 0; i < agents.Count; i++)
        {
            CreatureAgent agent = agents[i];
            if (agent == null || agent == ignore || !agent.IsLivingCreature) continue;

            if (FlatSqrDistance(agent.transform.position, center) <= sqrRadius)
            {
                result.Add(agent);
            }
        }

        return result;
    }

    public float FlatDistance(Vector3 a, Vector3 b)
    {
        return Mathf.Sqrt(FlatSqrDistance(a, b));
    }

    public float FlatSqrDistance(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x;
        float dz = a.z - b.z;
        return dx * dx + dz * dz;
    }

    public bool HaveRelation(CreatureAgent a, CreatureAgent b)
    {
        if (a == null || b == null) return false;
        return a.relationIds.Contains(b.simId) || b.relationIds.Contains(a.simId);
    }

    public void AddRelation(CreatureAgent a, CreatureAgent b)
    {
        if (a == null || b == null || a == b) return;

        RegisterAgent(a);
        RegisterAgent(b);

        a.relationIds.Add(b.simId);
        b.relationIds.Add(a.simId);

        string key = RelationKey(a.simId, b.simId);
        if (relationLines.ContainsKey(key) && relationLines[key] != null) return;

        GameObject lineObj;

        if (relationLinePrefab != null)
        {
            lineObj = Instantiate(relationLinePrefab, Vector3.zero, Quaternion.identity, spawnedRoot);
        }
        else
        {
            lineObj = new GameObject("RelationLine");
            lineObj.transform.SetParent(spawnedRoot);
        }

        RelationLine line = lineObj.GetComponent<RelationLine>();
        if (line == null) line = lineObj.AddComponent<RelationLine>();

        line.Initialize(a, b);
        relationLines[key] = line;
    }

    private void RemoveRelationsOf(CreatureAgent agent)
    {
        if (agent == null || agent.simId == 0) return;

        for (int i = 0; i < agents.Count; i++)
        {
            CreatureAgent other = agents[i];
            if (other != null) other.relationIds.Remove(agent.simId);
        }

        List<string> keysToRemove = new List<string>();

        foreach (KeyValuePair<string, RelationLine> pair in relationLines)
        {
            if (pair.Key.StartsWith(agent.simId + ":") || pair.Key.EndsWith(":" + agent.simId))
            {
                if (pair.Value != null) Destroy(pair.Value.gameObject);
                keysToRemove.Add(pair.Key);
            }
        }

        for (int i = 0; i < keysToRemove.Count; i++)
        {
            relationLines.Remove(keysToRemove[i]);
        }
    }

    private string RelationKey(int a, int b)
    {
        int min = Mathf.Min(a, b);
        int max = Mathf.Max(a, b);
        return min + ":" + max;
    }

    private void ScanForMarriage()
    {
        List<CreatureAgent> snapshot = new List<CreatureAgent>(agents);
        float distanceSqr = marriageDistance * marriageDistance;

        for (int i = 0; i < snapshot.Count; i++)
        {
            CreatureAgent a = snapshot[i];
            if (!CanReproduce(a)) continue;

            for (int j = i + 1; j < snapshot.Count; j++)
            {
                CreatureAgent b = snapshot[j];
                if (!CanReproduce(b)) continue;
                if (a == b) continue;

                if (FlatSqrDistance(a.transform.position, b.transform.position) > distanceSqr) continue;

                float chance = baseMarriageChance;
                if (a.IsReligionFollower || b.IsReligionFollower) chance *= religionReproductionMultiplier;
                chance = Mathf.Clamp01(chance);

                if (UnityEngine.Random.value <= chance)
                {
                    DoMarriage(a, b);
                    a.nextReproduceTime = Time.time + reproduceCooldown;
                    b.nextReproduceTime = Time.time + reproduceCooldown;
                    break;
                }
            }
        }
    }

    private bool CanReproduce(CreatureAgent agent)
    {
        return agent != null && agent.IsLivingCreature && Time.time >= agent.nextReproduceTime;
    }

    private void DoMarriage(CreatureAgent a, CreatureAgent b)
    {
        bool alreadyRelated = HaveRelation(a, b);

        Vector3 childBasePosition = (a.transform.position + b.transform.position) * 0.5f + RandomOffset(childSpawnRadius);
        CreatureAgent child = SpawnCreature(childBasePosition, 100f, false);

        if (!alreadyRelated)
        {
            float totalLife = Mathf.Max(1f, a.life + b.life);
            float share = totalLife / 3f;

            a.life = share;
            b.life = share;
            child.life = share;

            a.RefreshVisual();
            b.RefreshVisual();
            child.RefreshVisual();
        }
        else
        {
            child.life = 100f;
        }

        AddRelation(a, b);
        AddRelation(a, child);
        AddRelation(b, child);

        child.nextReproduceTime = Time.time + reproduceCooldown;
    }

    private void ScanForSelfProduce()
    {
        List<CreatureAgent> snapshot = new List<CreatureAgent>(agents);

        for (int i = 0; i < snapshot.Count; i++)
        {
            CreatureAgent agent = snapshot[i];
            if (agent == null || !agent.IsLivingCreature) continue;
            if (agent.life <= selfProduceMinimumLife) continue;
            if (Time.time < agent.nextSelfProduceTime) continue;
            if (UnityEngine.Random.value > selfProduceChancePerCheck) continue;

            DoSelfProduce(agent);
            agent.nextSelfProduceTime = Time.time + reproduceCooldown;
        }
    }

    private void DoSelfProduce(CreatureAgent original)
    {
        float share = original.life * 0.5f;
        original.life = share;

        Vector3 childBasePosition = original.transform.position + RandomOffset(selfProduceSpawnRadius);
        CreatureAgent child = SpawnCreature(childBasePosition, share, original.isCreater);

        AddRelation(original, child);

        child.nextSelfProduceTime = Time.time + reproduceCooldown;
        child.nextReproduceTime = Time.time + reproduceCooldown;

        original.RefreshVisual();
        child.RefreshVisual();
    }

    private void InfectRandomAgent()
    {
        List<CreatureAgent> candidates = new List<CreatureAgent>();

        for (int i = 0; i < agents.Count; i++)
        {
            CreatureAgent agent = agents[i];
            if (agent != null && agent.IsLivingCreature && !agent.infected)
            {
                candidates.Add(agent);
            }
        }

        if (candidates.Count == 0) return;

        CreatureAgent target = candidates[UnityEngine.Random.Range(0, candidates.Count)];
        target.Infect();
    }

    private void UpgradeRandomCreatureToCreater()
    {
        List<CreatureAgent> candidates = new List<CreatureAgent>();

        for (int i = 0; i < agents.Count; i++)
        {
            CreatureAgent agent = agents[i];
            if (agent != null && agent.IsLivingCreature && !agent.isCreater)
            {
                candidates.Add(agent);
            }
        }

        if (candidates.Count == 0) return;

        CreatureAgent target = candidates[UnityEngine.Random.Range(0, candidates.Count)];
        target.BecomeCreater();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}