using System.Collections.Generic;
using UnityEngine;

public class RandomPrefabWanderSpawner : MonoBehaviour
{
    [Header("Spawn")]
    [SerializeField] private GameObject prefab;
    [SerializeField] private int minCount = 5;
    [SerializeField] private int maxCount = 15;
    [SerializeField] private Vector3 regionCenter = Vector3.zero;
    [SerializeField] private Vector2 regionSize = new Vector2(20f, 20f);
    [SerializeField] private float fixedY = 0f;

    [Header("Movement")]
    [SerializeField] private float moveSpeedMin = 1f;
    [SerializeField] private float moveSpeedMax = 3f;
    [SerializeField] private float reachDistance = 0.2f;
    [SerializeField] private float waitAtPointMin = 0.2f;
    [SerializeField] private float waitAtPointMax = 1.2f;

    [Header("Rotation")]
    [SerializeField] private bool rotateTowardMoveDirection = true;
    [SerializeField] private float rotateSpeed = 8f;

    private class Mover
    {
        public Transform transform;
        public Vector3 target;
        public float speed;
        public float waitTimer;
    }

    private readonly List<Mover> movers = new List<Mover>();

    private void Start()
    {
        SpawnObjects();
    }

    private void Update()
    {
        for (int i = 0; i < movers.Count; i++)
        {
            UpdateMover(movers[i]);
        }
    }

    private void SpawnObjects()
    {
        if (prefab == null)
        {
            Debug.LogWarning("RandomPrefabWanderSpawner: Prefab is not assigned.");
            return;
        }

        int count = Random.Range(minCount, maxCount + 1);

        for (int i = 0; i < count; i++)
        {
            Vector3 spawnPos = GetRandomPointInRegion();
            GameObject obj = Instantiate(prefab, spawnPos, Quaternion.identity, transform);

            Mover mover = new Mover
            {
                transform = obj.transform,
                target = GetRandomPointInRegion(),
                speed = Random.Range(moveSpeedMin, moveSpeedMax),
                waitTimer = 0f
            };

            movers.Add(mover);
        }
    }

    private void UpdateMover(Mover mover)
    {
        if (mover.transform == null) return;

        Vector3 currentPos = mover.transform.position;
        currentPos.y = fixedY;
        mover.transform.position = currentPos;

        if (mover.waitTimer > 0f)
        {
            mover.waitTimer -= Time.deltaTime;
            return;
        }

        Vector3 target = mover.target;
        target.y = fixedY;

        Vector3 toTarget = target - mover.transform.position;
        toTarget.y = 0f;

        if (toTarget.sqrMagnitude <= reachDistance * reachDistance)
        {
            mover.target = GetRandomPointInRegion();
            mover.waitTimer = Random.Range(waitAtPointMin, waitAtPointMax);
            return;
        }

        Vector3 moveDir = toTarget.normalized;
        mover.transform.position += moveDir * mover.speed * Time.deltaTime;

        Vector3 fixedPos = mover.transform.position;
        fixedPos.y = fixedY;
        mover.transform.position = fixedPos;

        if (rotateTowardMoveDirection && moveDir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
            mover.transform.rotation = Quaternion.Slerp(
                mover.transform.rotation,
                targetRot,
                rotateSpeed * Time.deltaTime
            );
        }
    }

    private Vector3 GetRandomPointInRegion()
    {
        float x = Random.Range(regionCenter.x - regionSize.x * 0.5f, regionCenter.x + regionSize.x * 0.5f);
        float z = Random.Range(regionCenter.z - regionSize.y * 0.5f, regionCenter.z + regionSize.y * 0.5f);
        return new Vector3(x, fixedY, z);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 size = new Vector3(regionSize.x, 0.1f, regionSize.y);
        Vector3 center = new Vector3(regionCenter.x, fixedY, regionCenter.z);
        Gizmos.DrawWireCube(center, size);
    }
}