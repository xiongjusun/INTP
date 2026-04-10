using System.Collections.Generic;
using UnityEngine;

public class TiledWorldIllusion : MonoBehaviour
{
    [Header("Tile Definition")]
    [SerializeField] private Transform planeTransform;
    [SerializeField] private MeshFilter planeMeshFilter;
    [SerializeField] private Transform sourceTileRoot;
    [SerializeField] private Transform sourcePlacedRoot;

    [Header("Grid Size")]
    [SerializeField, Min(1)] private int tileRadius = 2; // 2 = 5x5

    [Header("Clone Settings")]
    [SerializeField] private bool makeTileClonesVisualOnly = true;
    [SerializeField] private bool makePlacedMirrorsVisualOnly = true;

    private Vector2Int[] tileOffsets;
    private Transform[] tileClones;
    private Transform[] clonePlacedRoots;
    private Bounds planeLocalBounds;

    private readonly List<MirrorEntry> mirrorEntries = new();

    private class MirrorEntry
    {
        public Transform source;
        public Transform[] mirrors;
    }

    private void Awake()
    {
        if (planeTransform == null || sourceTileRoot == null)
        {
            Debug.LogError("TiledWorldIllusion: Assign Plane Transform and Source Tile Root.", this);
            enabled = false;
            return;
        }

        if (planeMeshFilter == null)
            planeMeshFilter = planeTransform.GetComponent<MeshFilter>();

        if (planeMeshFilter == null || planeMeshFilter.sharedMesh == null)
        {
            Debug.LogError("TiledWorldIllusion: Plane needs a MeshFilter with a mesh.", this);
            enabled = false;
            return;
        }

        if (sourcePlacedRoot == null)
            sourcePlacedRoot = sourceTileRoot;

        if (sourcePlacedRoot != sourceTileRoot && !sourcePlacedRoot.IsChildOf(sourceTileRoot))
        {
            Debug.LogError("TiledWorldIllusion: Source Placed Root must be Source Tile Root or its child.", this);
            enabled = false;
            return;
        }

        planeLocalBounds = planeMeshFilter.sharedMesh.bounds;

        GenerateTileOffsets();
        BuildClones();
        SyncTileClones();
    }

    private void LateUpdate()
    {
        SyncTileClones();
        SyncPlacedMirrors();
    }

    private void OnDestroy()
    {
        DestroyAllTileClones();
        DestroyAllPlacedMirrors();
    }

    private void OnValidate()
    {
        if (tileRadius < 1)
            tileRadius = 1;
    }

    private void GenerateTileOffsets()
    {
        List<Vector2Int> offsets = new List<Vector2Int>();

        for (int z = -tileRadius; z <= tileRadius; z++)
        {
            for (int x = -tileRadius; x <= tileRadius; x++)
            {
                if (x == 0 && z == 0)
                    continue;

                offsets.Add(new Vector2Int(x, z));
            }
        }

        tileOffsets = offsets.ToArray();
    }

    public void RegisterPlacedObject(Transform sourceObject)
    {
        if (sourceObject == null)
            return;

        if (sourceObject.parent != sourcePlacedRoot)
        {
            Debug.LogWarning(
                $"TiledWorldIllusion: Placed object '{sourceObject.name}' should be a direct child of '{sourcePlacedRoot.name}' for clean mirroring.",
                sourceObject);
        }

        Transform[] mirrors = new Transform[tileOffsets.Length];

        for (int i = 0; i < tileOffsets.Length; i++)
        {
            if (clonePlacedRoots[i] == null)
                continue;

            GameObject mirrorGO = Instantiate(sourceObject.gameObject, clonePlacedRoots[i]);
            mirrorGO.name = $"{sourceObject.name}_Mirror_{tileOffsets[i].x}_{tileOffsets[i].y}";

            Transform mirror = mirrorGO.transform;
            mirror.localPosition = sourceObject.localPosition;
            mirror.localRotation = sourceObject.localRotation;
            mirror.localScale = sourceObject.localScale;

            if (makePlacedMirrorsVisualOnly)
                MakeVisualOnly(mirrorGO);

            mirrors[i] = mirror;
        }

        mirrorEntries.Add(new MirrorEntry
        {
            source = sourceObject,
            mirrors = mirrors
        });
    }

    private void BuildClones()
    {
        tileClones = new Transform[tileOffsets.Length];
        clonePlacedRoots = new Transform[tileOffsets.Length];

        string placedRootPath = GetRelativePath(sourceTileRoot, sourcePlacedRoot);

        for (int i = 0; i < tileOffsets.Length; i++)
        {
            GameObject cloneGO = Instantiate(sourceTileRoot.gameObject, sourceTileRoot.parent);
            cloneGO.name = $"{sourceTileRoot.name}_Clone_{tileOffsets[i].x}_{tileOffsets[i].y}";

            if (makeTileClonesVisualOnly)
                MakeVisualOnly(cloneGO);

            Transform clone = cloneGO.transform;
            tileClones[i] = clone;

            clonePlacedRoots[i] = string.IsNullOrEmpty(placedRootPath)
                ? clone
                : clone.Find(placedRootPath);

            if (clonePlacedRoots[i] == null)
            {
                Debug.LogError(
                    $"TiledWorldIllusion: Could not find placed root path '{placedRootPath}' on clone '{cloneGO.name}'.",
                    cloneGO);
            }
        }
    }

    private void SyncTileClones()
    {
        if (tileClones == null || tileOffsets == null)
            return;

        float tileSizeX = planeLocalBounds.size.x;
        float tileSizeZ = planeLocalBounds.size.z;

        Vector3 planeOriginWorld = planeTransform.TransformPoint(Vector3.zero);

        for (int i = 0; i < tileClones.Length; i++)
        {
            Vector2Int offset = tileOffsets[i];

            Vector3 localOffset = new Vector3(offset.x * tileSizeX, 0f, offset.y * tileSizeZ);
            Vector3 worldOffset = planeTransform.TransformPoint(localOffset) - planeOriginWorld;

            Transform clone = tileClones[i];
            if (clone == null)
                continue;

            clone.position = sourceTileRoot.position + worldOffset;
            clone.rotation = sourceTileRoot.rotation;
            clone.localScale = sourceTileRoot.localScale;
            clone.gameObject.SetActive(sourceTileRoot.gameObject.activeSelf);
        }
    }

    private void SyncPlacedMirrors()
    {
        for (int entryIndex = mirrorEntries.Count - 1; entryIndex >= 0; entryIndex--)
        {
            MirrorEntry entry = mirrorEntries[entryIndex];

            if (entry.source == null)
            {
                if (entry.mirrors != null)
                {
                    for (int i = 0; i < entry.mirrors.Length; i++)
                    {
                        if (entry.mirrors[i] != null)
                            Destroy(entry.mirrors[i].gameObject);
                    }
                }

                mirrorEntries.RemoveAt(entryIndex);
                continue;
            }

            for (int i = 0; i < entry.mirrors.Length; i++)
            {
                Transform mirror = entry.mirrors[i];
                if (mirror == null)
                    continue;

                mirror.localPosition = entry.source.localPosition;
                mirror.localRotation = entry.source.localRotation;
                mirror.localScale = entry.source.localScale;
                mirror.gameObject.SetActive(entry.source.gameObject.activeSelf);
            }
        }
    }

    private void DestroyAllTileClones()
    {
        if (tileClones == null)
            return;

        for (int i = 0; i < tileClones.Length; i++)
        {
            if (tileClones[i] != null)
                Destroy(tileClones[i].gameObject);
        }
    }

    private void DestroyAllPlacedMirrors()
    {
        for (int entryIndex = 0; entryIndex < mirrorEntries.Count; entryIndex++)
        {
            MirrorEntry entry = mirrorEntries[entryIndex];
            if (entry.mirrors == null)
                continue;

            for (int i = 0; i < entry.mirrors.Length; i++)
            {
                if (entry.mirrors[i] != null)
                    Destroy(entry.mirrors[i].gameObject);
            }
        }

        mirrorEntries.Clear();
    }

    private string GetRelativePath(Transform root, Transform target)
    {
        if (root == target)
            return string.Empty;

        List<string> pathParts = new List<string>();
        Transform current = target;

        while (current != null && current != root)
        {
            pathParts.Add(current.name);
            current = current.parent;
        }

        if (current != root)
            return string.Empty;

        pathParts.Reverse();
        return string.Join("/", pathParts);
    }

    private void MakeVisualOnly(GameObject root)
    {
        int ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");
        if (ignoreRaycastLayer >= 0)
            SetLayerRecursively(root, ignoreRaycastLayer);

        Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
            colliders[i].enabled = false;

        Rigidbody[] rigidbodies = root.GetComponentsInChildren<Rigidbody>(true);
        for (int i = 0; i < rigidbodies.Length; i++)
        {
            rigidbodies[i].isKinematic = true;
            rigidbodies[i].detectCollisions = false;
        }
    }

    private void SetLayerRecursively(GameObject root, int layer)
    {
        root.layer = layer;

        for (int i = 0; i < root.transform.childCount; i++)
            SetLayerRecursively(root.transform.GetChild(i).gameObject, layer);
    }
}