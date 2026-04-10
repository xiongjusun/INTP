using System.Collections.Generic;
using UnityEngine;

public class LatticeInfluencer : MonoBehaviour
{
    public static readonly List<Transform> Active = new();

    private void OnEnable()
    {
        if (!Active.Contains(transform))
            Active.Add(transform);
    }

    private void OnDisable()
    {
        Active.Remove(transform);
    }
}