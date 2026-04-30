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

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        if (body == null) body = gameObject.AddComponent<Rigidbody>();
        body.useGravity = false;
        body.isKinematic = true;

        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;

        cachedRenderer = GetComponentInChildren<Renderer>();
    }

    private void Start()
    {
        if (SimManager.HasInstance) SimManager.Instance.RegisterAgent(this);
        if (!initialized) RefreshVisual();
        PickNewWanderTarget();
    }

    public void Initialize(bool creater, float startingLife)
    {
        isCreater = creater;
        isRemain = false;
        infected = false;
        life = startingLife;
        initialized = true;
        RefreshVisual();
    }

    private void Update()
    {
        if (!SimManager.HasInstance) return;
        if (isRemain) return;

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

    private void WanderUpdate()
    {
        if (Time.time >= nextWanderTime || Vector3.Distance(transform.position, targetPoint) <= arriveDistance)
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

        if (Time.time >= nextWanderTime || Vector3.Distance(transform.position, targetPoint) <= arriveDistance)
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
            if (Vector3.Distance(transform.position, currentPower.transform.position) <= 1.2f)
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
            if (Vector3.Distance(transform.position, targetRemain.transform.position) <= 1.0f)
            {
                PickUpRemain(targetRemain);
            }
        }
        else
        {
            if (Time.time >= nextWanderTime || Vector3.Distance(transform.position, targetPoint) <= arriveDistance)
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
        Vector3 offset = SimManager.Instance.RandomOffset(wanderTargetRadius);
        targetPoint = SimManager.Instance.ClampToWorld(transform.position + offset);
        nextWanderTime = Time.time + wanderRetargetSeconds;
    }

    private void MoveToward(Vector3 target, bool drainLife, float politicalRadius, PowerCenter politicalCenter)
    {
        target.y = SimManager.Instance.groundY;

        Vector3 oldPosition = transform.position;
        Vector3 newPosition = Vector3.MoveTowards(oldPosition, target, moveSpeed * Time.deltaTime);

        if (politicalCenter != null && politicalRadius > 0f)
        {
            float extra = isCreater ? politicalCenter.createrExtraExitDistance : 0f;
            Vector3 center = politicalCenter.transform.position;
            Vector3 flat = new Vector3(newPosition.x - center.x, 0f, newPosition.z - center.z);
            float allowed = politicalRadius + extra;
            if (flat.magnitude > allowed)
            {
                flat = flat.normalized * allowed;
                newPosition = new Vector3(center.x + flat.x, SimManager.Instance.groundY, center.z + flat.z);
            }
        }

        newPosition = SimManager.Instance.ClampToWorld(newPosition);
        transform.position = newPosition;

        float moved = Vector3.Distance(oldPosition, newPosition);
        if (drainLife && moved > 0.001f)
        {
            life -= movementLifeDrainPerSecond * Time.deltaTime;
            if (life <= remainAtLife) BecomeRemain();
        }
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
            if (other.IsPoliticalFollower) chance *= SimManager.Instance.politicalInfectionMultiplier;
            if (UnityEngine.Random.value <= chance) other.Infect();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
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
        isCreater = true;
        gameObject.name = "Creater";
        RefreshVisual();
    }

    public void BecomeRemain()
    {
        if (isRemain) return;
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
        if (cachedRenderer == null) cachedRenderer = GetComponentInChildren<Renderer>();
        if (cachedRenderer == null || !SimManager.HasInstance) return;

        if (infected)
        {
            cachedRenderer.material.color = SimManager.Instance.infectedColor;
        }
        else if (isRemain)
        {
            cachedRenderer.material.color = SimManager.Instance.remainColor;
        }
        else if (isCreater)
        {
            cachedRenderer.material.color = SimManager.Instance.createrColor;
        }
        else
        {
            cachedRenderer.material.color = SimManager.Instance.creatureColor;
        }
    }

    private void OnDestroy()
    {
        if (currentPower != null) currentPower.RemoveFollower(this);
        if (SimManager.HasInstance) SimManager.Instance.UnregisterAgent(this);
    }
}
