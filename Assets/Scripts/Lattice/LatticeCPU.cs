using UnityEngine;

public class LatticeCPU : MonoBehaviour
{
    public Transform spherePrefab;

    [Header("Grid")]
    public int countX = 30;
    public int countZ = 30;
    public float spacing = 1f;

    [Header("Influence")]
    public float radius = 2f;
    public float maxHeight = 1.5f;
    public float falloff = 2f;
    public float smoothSpeed = 12f;

    Transform[] spheres;
    Vector3[] baseLocalPos;
    float[] heights;
    float radiusSqr;

    void Start()
    {
        radiusSqr = radius * radius;

        int total = countX * countZ;
        spheres = new Transform[total];
        baseLocalPos = new Vector3[total];
        heights = new float[total];

        float halfX = (countX - 1) * spacing * 0.5f;
        float halfZ = (countZ - 1) * spacing * 0.5f;

        int i = 0;
        for (int z = 0; z < countZ; z++)
        {
            for (int x = 0; x < countX; x++)
            {
                Vector3 local = new Vector3(x * spacing - halfX, 0f, z * spacing - halfZ);
                Transform t = Instantiate(spherePrefab, transform);
                t.localPosition = local;
                t.localRotation = Quaternion.identity;

                spheres[i] = t;
                baseLocalPos[i] = local;
                i++;
            }
        }
    }

    void Update()
    {
        float lerpT = 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime);
        var influencers = LatticeInfluencer.Active;

        for (int i = 0; i < spheres.Length; i++)
        {
            Vector3 worldPos = transform.TransformPoint(baseLocalPos[i]);

            float best = 0f;
            for (int p = 0; p < influencers.Count; p++)
            {
                Vector3 c = influencers[p].position;

                float dx = worldPos.x - c.x;
                float dz = worldPos.z - c.z;
                float d2 = dx * dx + dz * dz;

                if (d2 > radiusSqr) continue;

                float t01 = 1f - Mathf.Sqrt(d2) / radius;
                float h = Mathf.Pow(t01, falloff) * maxHeight;
                if (h > best) best = h;
            }

            heights[i] = Mathf.Lerp(heights[i], best, lerpT);

            Vector3 local = baseLocalPos[i];
            local.y += heights[i];
            spheres[i].localPosition = local;
        }
    }
}