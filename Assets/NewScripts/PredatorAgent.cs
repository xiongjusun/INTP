using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PredatorAgent : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float wanderRadius = 12f;
    public float retargetSeconds = 2f;
    public float arriveDistance = 0.4f;

    [Header("Physics")]
    public bool useGravityAndEnvironment = true;
    public bool makeOwnedCollidersSolid = true;
    public bool freezePhysicsRotation = true;
    public float rigidbodyMass = 1.5f;
    public float horizontalStopVelocity = 0f;

    [Header("Visual")]
    public bool overrideMaterialColor = false;

    private Vector3 target;
    private float nextTargetTime;
    private Rigidbody body;
    private Renderer cachedRenderer;

    private void Awake()
    {
        ConfigurePhysics();

        cachedRenderer = GetComponentInChildren<Renderer>();

        if (cachedRenderer != null && overrideMaterialColor)
        {
            cachedRenderer.material.color = Color.black;
        }
    }

    private void Start()
    {
        ConfigurePhysics();

        if (SimManager.HasInstance)
        {
            SimManager.Instance.RegisterPredator(this);
        }

        PickTarget();
    }

    private void Update()
    {
        if (!SimManager.HasInstance)
        {
            StopHorizontalMovement();
            return;
        }

        if (Time.time >= nextTargetTime || FlatDistance(transform.position, target) <= arriveDistance)
        {
            PickTarget();
        }

        MoveToward(target);
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

    private void PickTarget()
    {
        if (!SimManager.HasInstance) return;

        target = SimManager.Instance.ClampToWorld(transform.position + SimManager.Instance.RandomOffset(wanderRadius));
        nextTargetTime = Time.time + retargetSeconds;
    }

    private void MoveToward(Vector3 targetPosition)
    {
        if (!SimManager.HasInstance) return;

        Vector3 currentPosition = body != null ? body.position : transform.position;
        targetPosition = SimManager.Instance.ClampToWorld(targetPosition);

        Vector3 flatToTarget = new Vector3(
            targetPosition.x - currentPosition.x,
            0f,
            targetPosition.z - currentPosition.z
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

    private void OnTriggerEnter(Collider other)
    {
        HandleHit(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision == null) return;
        HandleHit(collision.collider);
    }

    private void HandleHit(Collider other)
    {
        if (other == null) return;

        CreatureAgent agent = other.GetComponentInParent<CreatureAgent>();
        if (agent == null || agent.isRemain) return;

        agent.KillByPredator();
    }

    private void OnDestroy()
    {
        if (SimManager.HasInstance)
        {
            SimManager.Instance.UnregisterPredator(this);
        }
    }
}