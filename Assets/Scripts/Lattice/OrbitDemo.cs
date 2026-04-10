using UnityEngine;

public class OrbitDemo : MonoBehaviour
{
    public Vector3 center = Vector3.zero;
    public float orbitRadius = 5f;
    public float speed = 1f;
    public float y = 0.2f;

    private void Update()
    {
        float a = Time.time * speed;
        transform.position = center + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * orbitRadius + Vector3.up * y;
    }
}