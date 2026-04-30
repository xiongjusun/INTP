using UnityEngine;

public class BagPlacedObject : MonoBehaviour
{
    public BagItemKind kind;
    public bool countsAsSource;

    private bool registered;

    public void Initialize(BagItemKind itemKind, bool source)
    {
        kind = itemKind;
        countsAsSource = source;

        if (countsAsSource && SimManager.HasInstance && !registered)
        {
            SimManager.Instance.ChangeBagSource(kind, 1);
            registered = true;
        }
    }

    public void RemoveFromBag()
    {
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (countsAsSource && registered && SimManager.HasInstance)
        {
            SimManager.Instance.ChangeBagSource(kind, -1);
            registered = false;
        }
    }
}
