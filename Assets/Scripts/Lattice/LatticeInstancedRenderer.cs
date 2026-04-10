using UnityEngine;

public class LatticeInstancedRenderer : MonoBehaviour
{
    [Header("Grid")]
    public int countX = 60;
    public int countZ = 60;
    public float spacing = 0.5f;
    public float sphereScale = 0.35f;

    [Header("Influence")]
    public float radius = 3f;
    public float maxHeight = 1.5f;
    public float falloff = 2f;
    public int maxParticles = 32;   // must match shader array size exactly

    [Header("Rendering")]
    public Mesh sphereMesh;
    public Material sphereMaterial;

    [Header("Debug")]
    public bool logStatus = true;
    public bool debugFakeInfluenceIfNone = false;

    const int BatchSize = 1023;

    Matrix4x4[] matrices;
    Vector4[] particlePositions;
    Bounds worldBounds;
    MaterialPropertyBlock props;

    static readonly int ParticlePositionsId = Shader.PropertyToID("_ParticlePositions");
    static readonly int ParticleCountId = Shader.PropertyToID("_ParticleCount");
    static readonly int RadiusId = Shader.PropertyToID("_Radius");
    static readonly int MaxHeightId = Shader.PropertyToID("_MaxHeight");
    static readonly int FalloffId = Shader.PropertyToID("_Falloff");

    void Start()
    {
        if (sphereMesh == null || sphereMaterial == null)
        {
            Debug.LogError("Assign sphereMesh and sphereMaterial.");
            enabled = false;
            return;
        }

        sphereMaterial.enableInstancing = true;
        props = new MaterialPropertyBlock();

        BuildGrid();
    }

    void BuildGrid()
    {
        matrices = new Matrix4x4[countX * countZ];
        particlePositions = new Vector4[maxParticles];

        Vector3 origin = transform.position;
        float halfX = (countX - 1) * spacing * 0.5f;
        float halfZ = (countZ - 1) * spacing * 0.5f;

        int i = 0;
        for (int z = 0; z < countZ; z++)
        {
            for (int x = 0; x < countX; x++)
            {
                Vector3 p = new Vector3(x * spacing - halfX, 0f, z * spacing - halfZ);
                matrices[i++] = Matrix4x4.TRS(
                    origin + p,
                    Quaternion.identity,
                    Vector3.one * sphereScale
                );
            }
        }

        worldBounds = new Bounds(
            origin + new Vector3(0f, maxHeight * 0.5f, 0f),
            new Vector3(
                countX * spacing + radius * 2f,
                maxHeight + sphereScale * 2f + 2f,
                countZ * spacing + radius * 2f
            )
        );
    }

    void LateUpdate()
    {
        // clean null entries
        for (int i = LatticeInfluencer.Active.Count - 1; i >= 0; i--)
        {
            if (LatticeInfluencer.Active[i] == null)
                LatticeInfluencer.Active.RemoveAt(i);
        }

        int count = Mathf.Min(LatticeInfluencer.Active.Count, maxParticles);

        for (int i = 0; i < count; i++)
        {
            Vector3 p = LatticeInfluencer.Active[i].position;
            particlePositions[i] = new Vector4(p.x, p.y, p.z, 1f);
        }

        for (int i = count; i < particlePositions.Length; i++)
            particlePositions[i] = Vector4.zero;

        if (count == 0 && debugFakeInfluenceIfNone)
        {
            Vector3 fake = transform.position;
            particlePositions[0] = new Vector4(fake.x, fake.y, fake.z, 1f);
            count = 1;
        }

        props.Clear();
        props.SetFloat(ParticleCountId, count);
        props.SetVectorArray(ParticlePositionsId, particlePositions);
        props.SetFloat(RadiusId, radius);
        props.SetFloat(MaxHeightId, maxHeight);
        props.SetFloat(FalloffId, falloff);

        if (logStatus && Time.frameCount % 60 == 0)
        {
            string first = count > 0 ? particlePositions[0].ToString() : "none";
            Debug.Log($"Lattice influencers: {count}, first: {first}");
        }

        var rp = new RenderParams(sphereMaterial)
        {
            worldBounds = worldBounds,
            matProps = props
        };

        int total = matrices.Length;
        for (int start = 0; start < total; start += BatchSize)
        {
            int batchCount = Mathf.Min(BatchSize, total - start);
            Graphics.RenderMeshInstanced(rp, sphereMesh, 0, matrices, batchCount, start);
        }
    }
}