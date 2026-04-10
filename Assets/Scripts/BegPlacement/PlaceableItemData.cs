using UnityEngine;

[CreateAssetMenu(menuName = "Bag/Placeable Item")]
public class PlaceableItemData : ScriptableObject
{
    public string itemName;
    public Sprite icon;
    public GameObject placedPrefab;
    public GameObject previewPrefab;
    public Vector3 rotationEuler;
    public Vector3 placementOffset;
}