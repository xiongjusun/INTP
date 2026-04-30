using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class RelationLine : MonoBehaviour
{
    public CreatureAgent a;
    public CreatureAgent b;
    public int resolution = 24;
    public float skyBendHeight = 4f;
    public float lineWidth = 0.08f;

    private LineRenderer line;
    private Vector3[] points;

    private void Awake()
    {
        line = GetComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.positionCount = resolution;
        line.startWidth = lineWidth;
        line.endWidth = lineWidth;

        if (line.sharedMaterial == null)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader != null) line.material = new Material(shader);
        }

        points = new Vector3[resolution];
    }

    public void Initialize(CreatureAgent first, CreatureAgent second)
    {
        a = first;
        b = second;
        if (line == null) Awake();
        UpdateLine();
    }

    private void LateUpdate()
    {
        if (a == null || b == null)
        {
            Destroy(gameObject);
            return;
        }

        UpdateLine();
    }

    private void UpdateLine()
    {
        if (line == null) return;
        if (a == null || b == null) return;

        if (points == null || points.Length != resolution)
        {
            points = new Vector3[resolution];
            line.positionCount = resolution;
        }

        Vector3 start = a.transform.position + Vector3.up * 0.6f;
        Vector3 end = b.transform.position + Vector3.up * 0.6f;
        Vector3 control = (start + end) * 0.5f + Vector3.up * skyBendHeight;

        for (int i = 0; i < resolution; i++)
        {
            float t = resolution <= 1 ? 0f : i / (float)(resolution - 1);
            points[i] = QuadraticBezier(start, control, end, t);
        }

        line.SetPositions(points);
    }

    private Vector3 QuadraticBezier(Vector3 p0, Vector3 p1, Vector3 p2, float t)
    {
        float u = 1f - t;
        return u * u * p0 + 2f * u * t * p1 + t * t * p2;
    }
}
