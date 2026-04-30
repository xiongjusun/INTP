using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PredatorAgent : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float wanderRadius = 12f;
    public float retargetSeconds = 2f;
    public float arriveDistance = 0.4f;

    private Vector3 target;
    private float nextTargetTime;
    private Rigidbody body;
    private Renderer cachedRenderer;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        if (body == null) body = gameObject.AddComponent<Rigidbody>();
        body.useGravity = false;
        body.isKinematic = true;

        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;

        cachedRenderer = GetComponentInChildren<Renderer>();
        if (cachedRenderer != null) cachedRenderer.material.color = Color.black;
    }

    private void Start()
    {
        if (SimManager.HasInstance) SimManager.Instance.RegisterPredator(this);
        PickTarget();
    }

    private void Update()
    {
        if (!SimManager.HasInstance) return;
        if (Time.time >= nextTargetTime || Vector3.Distance(transform.position, target) <= arriveDistance)
        {
            PickTarget();
        }

        Vector3 next = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
        transform.position = SimManager.Instance.ClampToWorld(next);
    }

    private void PickTarget()
    {
        if (!SimManager.HasInstance) return;
        target = SimManager.Instance.ClampToWorld(transform.position + SimManager.Instance.RandomOffset(wanderRadius));
        nextTargetTime = Time.time + retargetSeconds;
    }

    private void OnTriggerEnter(Collider other)
    {
        CreatureAgent agent = other.GetComponentInParent<CreatureAgent>();
        if (agent == null || agent.isRemain) return;
        agent.KillByPredator();
    }

    private void OnDestroy()
    {
        if (SimManager.HasInstance) SimManager.Instance.UnregisterPredator(this);
    }
}
