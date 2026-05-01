using UnityEngine;

/// <summary>
/// Optional helper for existing projects.
/// Add this component to the object/collider that should receive bag placement clicks.
/// This avoids needing to create or change project layers.
/// </summary>
[RequireComponent(typeof(Collider))]
public class BagPlacementSurface : MonoBehaviour
{
}
