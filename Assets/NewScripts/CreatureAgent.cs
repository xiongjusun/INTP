using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CreatureAgent : MonoBehaviour
{
    [Header("Identity")]
    public int simId;
    public bool isCreater;
    public bool isRemain;
    public readonly HashSet<int> relationIds = new HashSet<int>();

    [Header("Visual Prefab Swap")]
    public bool useCreaterPrefabVisualWhenConverted = true;
    public bool copyCreaterPrefabBehaviorWhenConverted = true;

    [Tooltip("Keep this off when you want Creature/Creater prefab materials to show exactly as you made them.")]
    public bool overrideNormalAndCreaterMaterialColor = false;

    [Tooltip("Usually keep this on so infected/remain states are visible even when using custom prefabs.")]
    public bool overrideRemainAndInfectedMaterialColor = true;

    [Header("Physics")]
    public bool useGravityAndEnvironment = true;
    public bool makeOwnedCollidersSolid = true;
    public bool freezePhysicsRotation = true;
    public float rigidbodyMass = 1f;
    public float horizontalStopVelocity = 0f;

    [Header("Life")]
    public float life = 100f;
    public float remainAtLife = 50f;
    public float movementLifeDrainPerSecond = 0.8f;

    [Header("Movement")]
    public float moveSpeed = 3f;
    public float wanderRetargetSeconds = 2.5f;
    public float wanderTargetRadius = 10f;
    public float arriveDistance = 0.25f;

    [Header("Creater Ability")]
    public float eatRemainMultiplier = 1f;
    public float giveLifeThreshold = 200f;
    public float giveLifeRate = 2f;
    public float giveLifeRadius = 8f;

    [Header("Political Behavior")]
    public float politicalTributeInterval = 5f;
    public float politicalTributeAmount = 10f;

    [Header("Infection")]
    public bool infected;
    public float infectionDeathDelay = 5f;
    public float infectionSpreadDistance = 3f;
    public float infectionSpreadInterval = 0.5f;
    public float infectionSpreadChance = 1f;

    [HideInInspector] public PowerCenter currentPower;
    [HideInInspector] public float nextReproduceTime;
    [HideInInspector] public float nextSelfProduceTime;

    public bool IsLivingCreature { get { return !isRemain && life > remainAtLife && gameObject.activeInHierarchy; } }
    public bool IsReligionFollower { get { return currentPower != null && currentPower.type == PowerCenterType.Religion; } }
    public bool IsPoliticalFollower { get { return currentPower != null && currentPower.type == PowerCenterType.Political; } }

    private Vector3 targetPoint;
    private float nextWanderTime;
    private float nextTributeTime;
    private float infectionDeathTime;
    private float nextInfectionSpreadTime;
    private bool hasRemainCargo;
    private float remainCargoValue;
    private CreatureAgent targetRemain;
    private Rigidbody body;
    private Renderer cachedRenderer;
    private bool initialized;

    private GameObject convertedCreaterVisual;
    private readonly List<Renderer> hiddenOriginalRenderers = new List<Renderer>();

    private void Awake()
    {
        ConfigurePhysics();
        cachedRenderer = GetComponentInChildren<Renderer>();
    }

    private void Start()
    {
        ConfigurePhysics();

        if (SimManager.HasInstance) SimManager.Instance.RegisterAgent(this);
        if (!initialized) RefreshVisual();

        PickNewWanderTarget();
    }

    private void Update()
    {
        if (!SimManager.HasInstance)
        {
            StopHorizontalMovement();
            return;
        }

        if (isRemain)
        {
            StopHorizontalMovement();
            return;
        }

        if (infected)
        {
            SpreadInfection();

            if (Time.time >= infectionDeathTime)
            {
                BecomeRemain();
                return;
            }
        }

        if (life <= remainAtLife)
        {
            BecomeRemain();
            return;
        }

        if (currentPower != null && currentPower.type == PowerCenterType.Political)
        {
            PoliticalUpdate();
        }
        else if (currentPower != null && currentPower.type == PowerCenterType.Religion)
        {
            ReligionFollowerUpdate();
        }
        else
        {
            WanderUpdate();
        }

        if (isCreater)
        {
            GiveLifeToNearbyCreatures();
        }
    }

    public void Initialize(bool creater, float startingLife)
    {
        isCreater = creater;
        isRemain = false;
        infected = false;
        life = startingLife;
        initialized = true;

        ConfigurePhysics();
        RefreshVisual();
    }

    private void ConfigurePhysics()
    {
        body = GetComponent<Rigidbody>();
        if (body == null) body = gameObject.AddComponent<Rigidbody>();

        body.useGravity = useGravityAndEnvironment;
        body.isKinematic = !useGravityAndEnvironment;
        body.mass = Mathf.Max(0.01f, rigidbodyMass);
        body.interpolation = RigidbodyInterpolation.Interpolate;

        if (useGravityAndEnvironment)
        {
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }

        if (freezePhysicsRotation)
        {
            body.constraints = RigidbodyConstraints.FreezeRotation;
        }

        Collider[] colliders = GetComponentsInChildren<Collider>(true);

        if (colliders.Length == 0)
        {
            gameObject.AddComponent<CapsuleCollider>();
            colliders = GetComponentsInChildren<Collider>(true);
        }

        if (makeOwnedCollidersSolid)
        {
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null)
                {
                    colliders[i].isTrigger = false;
                }
            }
        }
    }

    private void WanderUpdate()
    {
        if (Time.time >= nextWanderTime || FlatDistance(transform.position, targetPoint) <= arriveDistance)
        {
            PickNewWanderTarget();
        }

        MoveToward(targetPoint, true, 0f, null);
    }

    private void ReligionFollowerUpdate()
    {
        if (currentPower == null)
        {
            WanderUpdate();
            return;
        }

        if (Time.time >= nextWanderTime || FlatDistance(transform.position, targetPoint) <= arriveDistance)
        {
            Vector3 offset = SimManager.Instance.RandomOffset(currentPower.religionFollowerOrbitRadius);
            targetPoint = SimManager.Instance.ClampToWorld(currentPower.transform.position + offset);
            nextWanderTime = Time.time + wanderRetargetSeconds;
        }

        MoveToward(targetPoint, true, 0f, null);
    }

    private void PoliticalUpdate()
    {
        if (currentPower == null)
        {
            WanderUpdate();
            return;
        }

        PayPoliticalTribute();
        if (!IsLivingCreature) return;

        if (hasRemainCargo)
        {
            MoveToward(currentPower.transform.position, true, currentPower.CurrentRadius, currentPower);

            if (FlatDistance(transform.position, currentPower.transform.position) <= 1.2f)
            {
                float delivered = isCreater ? 30f : remainCargoValue;
                currentPower.ReceiveLife(delivered);
                hasRemainCargo = false;
                remainCargoValue = 0f;
            }

            return;
        }

        if (targetRemain == null || !targetRemain.isRemain)
        {
            float searchRadius = currentPower.CurrentRadius + (isCreater ? currentPower.createrExtraExitDistance : 0f);
            targetRemain = SimManager.Instance.FindClosestRemain(transform.position, searchRadius, currentPower, isCreater);
        }

        if (targetRemain != null && targetRemain.isRemain)
        {
            MoveToward(targetRemain.transform.position, true, currentPower.CurrentRadius, currentPower);

            if (FlatDistance(transform.position, targetRemain.transform.position) <= 1.0f)
            {
                PickUpRemain(targetRemain);
            }
        }
        else
        {
            if (Time.time >= nextWanderTime || FlatDistance(transform.position, targetPoint) <= arriveDistance)
            {
                targetPoint = currentPower.GetRandomPointInRegion(false);
                nextWanderTime = Time.time + wanderRetargetSeconds;
            }

            MoveToward(targetPoint, true, currentPower.CurrentRadius, currentPower);
        }
    }

    private void PayPoliticalTribute()
    {
        if (currentPower == null) return;
        if (Time.time < nextTributeTime) return;

        nextTributeTime = Time.time + politicalTributeInterval;

        float amount = Mathf.Min(politicalTributeAmount, Mathf.Max(0f, life - remainAtLife));
        if (amount <= 0f) return;

        life -= amount;
        currentPower.ReceiveLife(amount);

        if (life <= remainAtLife) BecomeRemain();
    }

    private void PickUpRemain(CreatureAgent remain)
    {
        if (remain == null || !remain.isRemain) return;

        remainCargoValue = Mathf.Max(1f, remain.life);
        hasRemainCargo = true;
        targetRemain = null;

        remain.ConsumeRemain();
    }

    private void PickNewWanderTarget()
    {
        if (!SimManager.HasInstance) return;

        Vector3 offset = SimManager.Instance.RandomOffset(wanderTargetRadius);
        targetPoint = SimManager.Instance.ClampToWorld(transform.position + offset);
        nextWanderTime = Time.time + wanderRetargetSeconds;
    }

    private void MoveToward(Vector3 target, bool drainLife, float politicalRadius, PowerCenter politicalCenter)
    {
        if (!SimManager.HasInstance) return;

        Vector3 currentPosition = body != null ? body.position : transform.position;
        target = SimManager.Instance.ClampToWorld(target);

        if (politicalCenter != null && politicalRadius > 0f)
        {
            target = ClampPointInsidePoliticalArea(target, politicalRadius, politicalCenter);
        }

        Vector3 flatToTarget = new Vector3(
            target.x - currentPosition.x,
            0f,
            target.z - currentPosition.z
        );

        float distance = flatToTarget.magnitude;

        if (distance <= arriveDistance)
        {
            StopHorizontalMovement();
            return;
        }

        Vector3 desiredHorizontalVelocity = flatToTarget / distance * moveSpeed;

        Vector3 predictedNextPosition = currentPosition + desiredHorizontalVelocity * Time.deltaTime;
        Vector3 clampedNextPosition = SimManager.Instance.ClampToWorld(predictedNextPosition);

        if (!Mathf.Approximately(predictedNextPosition.x, clampedNextPosition.x))
        {
            desiredHorizontalVelocity.x = 0f;
        }

        if (!Mathf.Approximately(predictedNextPosition.z, clampedNextPosition.z))
        {
            desiredHorizontalVelocity.z = 0f;
        }

        ApplyHorizontalVelocity(desiredHorizontalVelocity);

        float estimatedMoved = Mathf.Min(distance, moveSpeed * Time.deltaTime);

        if (drainLife && estimatedMoved > 0.001f)
        {
            life -= movementLifeDrainPerSecond * Time.deltaTime;

            if (life <= remainAtLife)
            {
                BecomeRemain();
            }
        }
    }

    private Vector3 ClampPointInsidePoliticalArea(Vector3 point, float politicalRadius, PowerCenter politicalCenter)
    {
        if (politicalCenter == null) return point;

        float extra = isCreater ? politicalCenter.createrExtraExitDistance : 0f;
        float allowed = politicalRadius + extra;

        Vector3 center = politicalCenter.transform.position;
        Vector3 flat = new Vector3(point.x - center.x, 0f, point.z - center.z);

        if (flat.magnitude > allowed)
        {
            flat = flat.normalized * allowed;
            point.x = center.x + flat.x;
            point.z = center.z + flat.z;
        }

        return point;
    }

    private void ApplyHorizontalVelocity(Vector3 horizontalVelocity)
    {
        if (body != null && useGravityAndEnvironment && !body.isKinematic)
        {
            Vector3 velocity = GetBodyVelocity();
            velocity.x = horizontalVelocity.x;
            velocity.z = horizontalVelocity.z;
            SetBodyVelocity(velocity);
        }
        else
        {
            Vector3 current = transform.position;
            Vector3 next = current + horizontalVelocity * Time.deltaTime;
            next = SimManager.HasInstance ? SimManager.Instance.ClampToWorld(next) : next;
            transform.position = next;
        }
    }

    private void StopHorizontalMovement()
    {
        if (body != null && useGravityAndEnvironment && !body.isKinematic)
        {
            Vector3 velocity = GetBodyVelocity();
            velocity.x = horizontalStopVelocity;
            velocity.z = horizontalStopVelocity;
            SetBodyVelocity(velocity);
        }
    }

    private Vector3 GetBodyVelocity()
    {
        if (body == null) return Vector3.zero;

#if UNITY_6000_0_OR_NEWER
        return body.linearVelocity;
#else
        return body.velocity;
#endif
    }

    private void SetBodyVelocity(Vector3 velocity)
    {
        if (body == null) return;

#if UNITY_6000_0_OR_NEWER
        body.linearVelocity = velocity;
#else
        body.velocity = velocity;
#endif
    }

    private float FlatDistance(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x;
        float dz = a.z - b.z;
        return Mathf.Sqrt(dx * dx + dz * dz);
    }

    private void GiveLifeToNearbyCreatures()
    {
        if (life <= giveLifeThreshold) return;

        List<CreatureAgent> receivers = SimManager.Instance.GetLivingAgentsWithin(transform.position, giveLifeRadius, this);
        if (receivers.Count == 0) return;

        float totalGive = Mathf.Min(giveLifeRate * Time.deltaTime, life - giveLifeThreshold);
        if (totalGive <= 0f) return;

        float each = totalGive / receivers.Count;

        for (int i = 0; i < receivers.Count; i++)
        {
            if (receivers[i] == null || !receivers[i].IsLivingCreature) continue;

            receivers[i].life += each;
            receivers[i].RefreshVisual();
            life -= each;
        }
    }

    private void SpreadInfection()
    {
        if (Time.time < nextInfectionSpreadTime) return;

        nextInfectionSpreadTime = Time.time + infectionSpreadInterval;

        List<CreatureAgent> near = SimManager.Instance.GetLivingAgentsWithin(transform.position, infectionSpreadDistance, this);

        for (int i = 0; i < near.Count; i++)
        {
            CreatureAgent other = near[i];
            if (other == null || other.infected) continue;

            float chance = infectionSpreadChance;

            if (other.IsPoliticalFollower)
            {
                chance *= SimManager.Instance.politicalInfectionMultiplier;
            }

            if (UnityEngine.Random.value <= chance)
            {
                other.Infect();
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        HandleCreatureTouch(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision == null) return;
        HandleCreatureTouch(collision.collider);
    }

    private void HandleCreatureTouch(Collider other)
    {
        if (other == null) return;

        CreatureAgent otherAgent = other.GetComponentInParent<CreatureAgent>();
        if (otherAgent == null || otherAgent == this) return;

        if (isCreater && otherAgent.isRemain)
        {
            life += Mathf.Max(1f, otherAgent.life) * eatRemainMultiplier;
            otherAgent.ConsumeRemain();
            RefreshVisual();
        }
    }

    public void SetPowerCenter(PowerCenter newCenter)
    {
        if (currentPower == newCenter) return;

        if (currentPower != null) currentPower.RemoveFollower(this);

        currentPower = newCenter;

        if (currentPower != null) currentPower.AddFollower(this);

        targetRemain = null;
        hasRemainCargo = false;
    }

    public void Infect()
    {
        if (!IsLivingCreature || infected) return;

        infected = true;
        infectionDeathTime = Time.time + infectionDeathDelay;
        nextInfectionSpreadTime = Time.time;

        RefreshVisual();
    }

    public void BecomeCreater()
    {
        if (isRemain) return;
        if (isCreater) return;

        isCreater = true;
        gameObject.name = "Creater";

        if (copyCreaterPrefabBehaviorWhenConverted)
        {
            CopyCreaterBehaviorFromPrefab();
        }

        ConfigurePhysics();

        if (useCreaterPrefabVisualWhenConverted)
        {
            ApplyCreaterPrefabVisual();
        }

        RefreshVisual();
    }

    private void CopyCreaterBehaviorFromPrefab()
    {
        if (!SimManager.HasInstance) return;

        GameObject prefab = SimManager.Instance.createrPrefab;
        if (prefab == null) return;

        CreatureAgent template = prefab.GetComponent<CreatureAgent>();
        if (template == null) return;

        useGravityAndEnvironment = template.useGravityAndEnvironment;
        makeOwnedCollidersSolid = template.makeOwnedCollidersSolid;
        freezePhysicsRotation = template.freezePhysicsRotation;
        rigidbodyMass = template.rigidbodyMass;
        horizontalStopVelocity = template.horizontalStopVelocity;

        movementLifeDrainPerSecond = template.movementLifeDrainPerSecond;
        moveSpeed = template.moveSpeed;
        wanderRetargetSeconds = template.wanderRetargetSeconds;
        wanderTargetRadius = template.wanderTargetRadius;
        arriveDistance = template.arriveDistance;

        eatRemainMultiplier = template.eatRemainMultiplier;
        giveLifeThreshold = template.giveLifeThreshold;
        giveLifeRate = template.giveLifeRate;
        giveLifeRadius = template.giveLifeRadius;

        politicalTributeInterval = template.politicalTributeInterval;
        politicalTributeAmount = template.politicalTributeAmount;

        infectionDeathDelay = template.infectionDeathDelay;
        infectionSpreadDistance = template.infectionSpreadDistance;
        infectionSpreadInterval = template.infectionSpreadInterval;
        infectionSpreadChance = template.infectionSpreadChance;

        overrideNormalAndCreaterMaterialColor = template.overrideNormalAndCreaterMaterialColor;
        overrideRemainAndInfectedMaterialColor = template.overrideRemainAndInfectedMaterialColor;
    }

    private void ApplyCreaterPrefabVisual()
    {
        if (!SimManager.HasInstance) return;
        if (SimManager.Instance.createrPrefab == null) return;

        RemoveConvertedVisualAndShowOriginalRenderers();

        GameObject prefab = SimManager.Instance.createrPrefab;

        convertedCreaterVisual = Instantiate(prefab, transform);
        convertedCreaterVisual.name = "Converted Creater Visual";
        convertedCreaterVisual.transform.localPosition = Vector3.zero;
        convertedCreaterVisual.transform.localRotation = Quaternion.identity;
        convertedCreaterVisual.transform.localScale = prefab.transform.localScale;

        RemoveSimulationComponentsFromVisual(convertedCreaterVisual);
        HideOriginalRenderersExcept(convertedCreaterVisual.transform);

        cachedRenderer = convertedCreaterVisual.GetComponentInChildren<Renderer>(true);
    }

    private void RemoveSimulationComponentsFromVisual(GameObject visualRoot)
    {
        if (visualRoot == null) return;

        DestroyAllComponentsInChildren<CreatureAgent>(visualRoot);
        DestroyAllComponentsInChildren<PredatorAgent>(visualRoot);
        DestroyAllComponentsInChildren<PowerCenter>(visualRoot);
        DestroyAllComponentsInChildren<BagPlacedObject>(visualRoot);
        DestroyAllComponentsInChildren<Rigidbody>(visualRoot);
        DestroyAllComponentsInChildren<Collider>(visualRoot);
    }

    private void DestroyAllComponentsInChildren<T>(GameObject root) where T : Component
    {
        if (root == null) return;

        T[] components = root.GetComponentsInChildren<T>(true);

        for (int i = 0; i < components.Length; i++)
        {
            T component = components[i];
            if (component == null) continue;

            Behaviour behaviour = component as Behaviour;
            if (behaviour != null) behaviour.enabled = false;

            Collider col = component as Collider;
            if (col != null) col.enabled = false;

            Rigidbody rb = component as Rigidbody;
            if (rb != null) rb.detectCollisions = false;

            Destroy(component);
        }
    }

    private void HideOriginalRenderersExcept(Transform visibleRoot)
    {
        hiddenOriginalRenderers.Clear();

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null) continue;

            bool isConvertedVisualRenderer = visibleRoot != null && renderer.transform.IsChildOf(visibleRoot);
            if (isConvertedVisualRenderer) continue;

            if (renderer.enabled)
            {
                renderer.enabled = false;
                hiddenOriginalRenderers.Add(renderer);
            }
        }
    }

    private void RemoveConvertedVisualAndShowOriginalRenderers()
    {
        for (int i = 0; i < hiddenOriginalRenderers.Count; i++)
        {
            if (hiddenOriginalRenderers[i] != null)
            {
                hiddenOriginalRenderers[i].enabled = true;
            }
        }

        hiddenOriginalRenderers.Clear();

        if (convertedCreaterVisual != null)
        {
            Destroy(convertedCreaterVisual);
            convertedCreaterVisual = null;
        }

        cachedRenderer = GetComponentInChildren<Renderer>(true);
    }

    public void BecomeRemain()
    {
        if (isRemain) return;

        RemoveConvertedVisualAndShowOriginalRenderers();

        isRemain = true;
        infected = false;
        hasRemainCargo = false;
        targetRemain = null;

        if (currentPower != null) SetPowerCenter(null);

        BagPlacedObject placed = GetComponent<BagPlacedObject>();
        if (placed != null && placed.kind == BagItemKind.Creater && placed.countsAsSource)
        {
            Destroy(placed);
        }

        gameObject.name = "Remain";
        life = Mathf.Max(1f, life);

        StopHorizontalMovement();

        transform.localScale = new Vector3(0.8f, 0.25f, 0.8f);

        RefreshVisual();
    }

    public void ConsumeRemain()
    {
        if (!isRemain) return;

        if (SimManager.HasInstance) SimManager.Instance.UnregisterAgent(this);

        Destroy(gameObject);
    }

    public void KillByPredator()
    {
        if (SimManager.HasInstance) SimManager.Instance.UnregisterAgent(this);

        Destroy(gameObject);
    }

    public void RefreshVisual()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0) return;
        if (!SimManager.HasInstance) return;

        cachedRenderer = null;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && renderers[i].enabled)
            {
                cachedRenderer = renderers[i];
                break;
            }
        }

        if (cachedRenderer == null) return;

        Color targetColor;
        bool shouldApplyColor;

        if (infected)
        {
            targetColor = SimManager.Instance.infectedColor;
            shouldApplyColor = overrideRemainAndInfectedMaterialColor;
        }
        else if (isRemain)
        {
            targetColor = SimManager.Instance.remainColor;
            shouldApplyColor = overrideRemainAndInfectedMaterialColor;
        }
        else if (isCreater)
        {
            targetColor = SimManager.Instance.createrColor;
            shouldApplyColor = overrideNormalAndCreaterMaterialColor;
        }
        else
        {
            targetColor = SimManager.Instance.creatureColor;
            shouldApplyColor = overrideNormalAndCreaterMaterialColor;
        }

        if (!shouldApplyColor) return;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || !renderer.enabled) continue;

            ApplyColorToRenderer(renderer, targetColor);
        }
    }

    private void ApplyColorToRenderer(Renderer renderer, Color color)
    {
        if (renderer == null) return;

        Material[] materials = renderer.materials;

        for (int i = 0; i < materials.Length; i++)
        {
            Material material = materials[i];
            if (material == null) continue;

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }
            else if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }
            else
            {
                material.color = color;
            }
        }
    }

    private void OnDestroy()
    {
        if (currentPower != null) currentPower.RemoveFollower(this);
        if (SimManager.HasInstance) SimManager.Instance.UnregisterAgent(this);
    }
}