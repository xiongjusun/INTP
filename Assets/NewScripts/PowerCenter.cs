using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PowerCenter : MonoBehaviour
{
    public PowerCenterType type = PowerCenterType.Religion;

    [Header("Life / Region")]
    public float life = 100f;
    public float baseRadius = 12f;
    public float radiusPerLife = 0.025f;
    public float visualScalePerRadius = 0.08f;
    public float createrExtraExitDistance = 5f;

    [Header("Capture")]
    public float captureScanInterval = 2f;
    public float religionCaptureChance = 0.30f;
    public float politicalCaptureChance = 0.40f;
    public float politicalToReligionChance = 0.40f;
    public float politicalToPoliticalChance = 0.20f;
    public float religionToReligionChance = 0.10f;
    public float religionToPoliticalChance = 0.30f;

    [Header("Religion Movement")]
    public float religionMoveSpeed = 2f;
    public float religionWanderRetargetSeconds = 4f;
    public float religionFollowerOrbitRadius = 5f;

    public float CurrentRadius
    {
        get
        {
            if (type == PowerCenterType.Political) return baseRadius + life * radiusPerLife;
            return baseRadius;
        }
    }

    private readonly List<CreatureAgent> followers = new List<CreatureAgent>();
    private float nextCaptureScanTime;
    private Vector3 religionTarget;
    private float nextReligionTargetTime;
    private Renderer cachedRenderer;

    private void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = false;
        cachedRenderer = GetComponentInChildren<Renderer>();
    }

    private void Start()
    {
        if (SimManager.HasInstance) SimManager.Instance.RegisterPowerCenter(this);
        PickReligionTarget();
        RefreshVisual();
    }

    private void Update()
    {
        if (!SimManager.HasInstance) return;

        if (type == PowerCenterType.Religion)
        {
            ReligionMoveUpdate();
        }
        else
        {
            UpdatePoliticalSize();
        }

        if (Time.time >= nextCaptureScanTime)
        {
            nextCaptureScanTime = Time.time + captureScanInterval;
            CaptureScan();
        }
    }

    private void ReligionMoveUpdate()
    {
        if (Time.time >= nextReligionTargetTime || Vector3.Distance(transform.position, religionTarget) <= 0.4f)
        {
            PickReligionTarget();
        }

        Vector3 next = Vector3.MoveTowards(transform.position, religionTarget, religionMoveSpeed * Time.deltaTime);
        transform.position = SimManager.Instance.ClampToWorld(next);
    }

    private void PickReligionTarget()
    {
        if (!SimManager.HasInstance) return;
        religionTarget = SimManager.Instance.ClampToWorld(transform.position + SimManager.Instance.RandomOffset(baseRadius));
        nextReligionTargetTime = Time.time + religionWanderRetargetSeconds;
    }

    private void UpdatePoliticalSize()
    {
        float size = Mathf.Max(1f, CurrentRadius * visualScalePerRadius);
        transform.localScale = new Vector3(size, size, size);
    }

    private void CaptureScan()
    {
        float radius = CurrentRadius;
        List<CreatureAgent> near = SimManager.Instance.GetLivingAgentsWithin(transform.position, radius, null);

        for (int i = 0; i < near.Count; i++)
        {
            CreatureAgent agent = near[i];
            if (agent == null || agent.currentPower == this) continue;

            float chance = GetChanceToCapture(agent.currentPower);
            if (chance <= 0f) continue;
            if (UnityEngine.Random.value <= chance) agent.SetPowerCenter(this);
        }
    }

    private float GetChanceToCapture(PowerCenter from)
    {
        if (from == null)
        {
            return type == PowerCenterType.Religion ? religionCaptureChance : politicalCaptureChance;
        }

        if (from.type == PowerCenterType.Political && type == PowerCenterType.Religion) return politicalToReligionChance;
        if (from.type == PowerCenterType.Political && type == PowerCenterType.Political) return politicalToPoliticalChance;
        if (from.type == PowerCenterType.Religion && type == PowerCenterType.Religion) return religionToReligionChance;
        if (from.type == PowerCenterType.Religion && type == PowerCenterType.Political) return religionToPoliticalChance;
        return 0f;
    }

    public Vector3 GetRandomPointInRegion(bool allowCreaterExtra)
    {
        float radius = CurrentRadius + (allowCreaterExtra ? createrExtraExitDistance : 0f);
        Vector3 point = transform.position + SimManager.Instance.RandomOffset(radius);
        return SimManager.Instance.ClampToWorld(point);
    }

    public void ReceiveLife(float amount)
    {
        life += Mathf.Max(0f, amount);
        RefreshVisual();
    }

    public void AddFollower(CreatureAgent agent)
    {
        if (agent != null && !followers.Contains(agent)) followers.Add(agent);
    }

    public void RemoveFollower(CreatureAgent agent)
    {
        followers.Remove(agent);
    }

    private void RefreshVisual()
    {
        if (cachedRenderer == null) cachedRenderer = GetComponentInChildren<Renderer>();
        if (cachedRenderer == null) return;
        cachedRenderer.material.color = type == PowerCenterType.Religion ? new Color(1f, 0.8f, 0.1f) : new Color(0.9f, 0.2f, 0.2f);
    }

    private void OnDestroy()
    {
        if (SimManager.HasInstance) SimManager.Instance.UnregisterPowerCenter(this);
    }
}
