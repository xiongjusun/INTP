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

    [Header("Political Area Visual")]
    public bool showPoliticalAreaCylinder = true;
    public float politicalAreaHeight = 0.12f;
    public float politicalAreaYOffset = 0.02f;
    public Color politicalAreaColor = new Color(0.9f, 0.2f, 0.2f, 0.25f);
    public Material politicalAreaMaterial;

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

    private Vector3 originalLocalScale;
    private bool originalScaleStored;

    private GameObject politicalAreaVisual;
    private Renderer politicalAreaRenderer;
    private Material runtimePoliticalAreaMaterial;

    private void Awake()
    {
        StoreOriginalScale();

        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = false;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();

        rb.useGravity = true;
        rb.isKinematic = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        cachedRenderer = GetComponentInChildren<Renderer>();
    }

    private void Start()
    {
        if (SimManager.HasInstance) SimManager.Instance.RegisterPowerCenter(this);
        PickReligionTarget();
        RefreshVisual();

        if (type == PowerCenterType.Political)
        {
            UpdatePoliticalSize();
        }
    }

    private void Update()
    {
        if (!SimManager.HasInstance) return;

        if (type == PowerCenterType.Religion)
        {
            HidePoliticalAreaVisual();
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

    private void StoreOriginalScale()
    {
        if (originalScaleStored) return;
        originalLocalScale = transform.localScale;
        originalScaleStored = true;
    }

    private void ReligionMoveUpdate()
    {
        if (Time.time >= nextReligionTargetTime || FlatDistance(transform.position, religionTarget) <= 0.4f)
        {
            PickReligionTarget();
        }

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Vector3 next = Vector3.MoveTowards(transform.position, religionTarget, religionMoveSpeed * Time.deltaTime);
            transform.position = SimManager.Instance.ClampToWorld(next);
            return;
        }

        Vector3 current = rb.position;

        Vector3 flatToTarget = new Vector3(
            religionTarget.x - current.x,
            0f,
            religionTarget.z - current.z
        );

        if (flatToTarget.magnitude <= 0.4f)
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
#else
        rb.velocity = new Vector3(0f, rb.velocity.y, 0f);
#endif
            return;
        }

        Vector3 horizontalVelocity = flatToTarget.normalized * religionMoveSpeed;

#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = new Vector3(horizontalVelocity.x, rb.linearVelocity.y, horizontalVelocity.z);
#else
    rb.velocity = new Vector3(horizontalVelocity.x, rb.velocity.y, horizontalVelocity.z);
#endif
    }

    private float FlatDistance(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x;
        float dz = a.z - b.z;
        return Mathf.Sqrt(dx * dx + dz * dz);
    }

    private void PickReligionTarget()
    {
        if (!SimManager.HasInstance) return;
        religionTarget = SimManager.Instance.ClampToWorld(transform.position + SimManager.Instance.RandomOffset(baseRadius));
        religionTarget.y = transform.position.y;
        nextReligionTargetTime = Time.time + religionWanderRetargetSeconds;
    }

    private void UpdatePoliticalSize()
    {
        StoreOriginalScale();

        // Keep the power center prefab/model at its original size.
        // Only the cylinder under it shows the political affected area.
        transform.localScale = originalLocalScale;

        if (!showPoliticalAreaCylinder)
        {
            HidePoliticalAreaVisual();
            return;
        }

        EnsurePoliticalAreaVisual();
        UpdatePoliticalAreaVisual();
    }

    private void EnsurePoliticalAreaVisual()
    {
        if (politicalAreaVisual != null)
        {
            if (!politicalAreaVisual.activeSelf) politicalAreaVisual.SetActive(true);
            return;
        }

        politicalAreaVisual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        politicalAreaVisual.name = "Political Affected Area Cylinder";
        politicalAreaVisual.transform.SetParent(transform, false);

        Collider areaCollider = politicalAreaVisual.GetComponent<Collider>();
        if (areaCollider != null)
        {
            Destroy(areaCollider);
        }

        politicalAreaRenderer = politicalAreaVisual.GetComponent<Renderer>();
        if (politicalAreaRenderer != null)
        {
            if (politicalAreaMaterial != null)
            {
                politicalAreaRenderer.sharedMaterial = politicalAreaMaterial;
            }
            else
            {
                runtimePoliticalAreaMaterial = CreateTransparentMaterial(politicalAreaColor);
                politicalAreaRenderer.sharedMaterial = runtimePoliticalAreaMaterial;
            }
        }
    }

    private void UpdatePoliticalAreaVisual()
    {
        if (politicalAreaVisual == null) return;

        float radius = Mathf.Max(0.05f, CurrentRadius);
        float diameter = radius * 2f;
        float height = Mathf.Max(0.01f, politicalAreaHeight);

        float groundY = SimManager.HasInstance ? SimManager.Instance.groundY : transform.position.y;

        politicalAreaVisual.transform.position = new Vector3(
            transform.position.x,
            groundY + politicalAreaYOffset + height * 0.5f,
            transform.position.z
        );

        politicalAreaVisual.transform.rotation = Quaternion.identity;

        Vector3 parentScale = transform.lossyScale;

        politicalAreaVisual.transform.localScale = new Vector3(
            SafeDivide(diameter, parentScale.x),
            SafeDivide(height * 0.5f, parentScale.y),
            SafeDivide(diameter, parentScale.z)
        );

        if (politicalAreaRenderer == null)
        {
            politicalAreaRenderer = politicalAreaVisual.GetComponent<Renderer>();
        }

        if (politicalAreaRenderer != null && politicalAreaMaterial == null)
        {
            if (politicalAreaRenderer.sharedMaterial != null)
            {
                politicalAreaRenderer.sharedMaterial.color = politicalAreaColor;
            }
        }
    }

    private float SafeDivide(float value, float divisor)
    {
        if (Mathf.Abs(divisor) <= 0.0001f) return value;
        return value / divisor;
    }

    private void HidePoliticalAreaVisual()
    {
        if (politicalAreaVisual != null && politicalAreaVisual.activeSelf)
        {
            politicalAreaVisual.SetActive(false);
        }
    }

    private Material CreateTransparentMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Diffuse");

        Material material = shader != null ? new Material(shader) : null;
        if (material == null) return null;

        material.color = color;

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

    private Renderer FindMainRenderer()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (politicalAreaVisual != null && renderers[i].gameObject == politicalAreaVisual) continue;
            return renderers[i];
        }

        return null;
    }

    private void RefreshVisual()
    {
        if (cachedRenderer == null || (politicalAreaVisual != null && cachedRenderer.gameObject == politicalAreaVisual))
        {
            cachedRenderer = FindMainRenderer();
        }

        if (cachedRenderer == null) return;
        cachedRenderer.material.color = type == PowerCenterType.Religion ? new Color(1f, 0.8f, 0.1f) : new Color(0.9f, 0.2f, 0.2f);
    }

    private void OnDestroy()
    {
        if (SimManager.HasInstance) SimManager.Instance.UnregisterPowerCenter(this);

        if (runtimePoliticalAreaMaterial != null)
        {
            Destroy(runtimePoliticalAreaMaterial);
        }
    }
}
