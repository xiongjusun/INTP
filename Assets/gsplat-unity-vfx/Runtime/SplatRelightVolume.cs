using UnityEngine;

namespace Gsplat
{
    public enum RelightShape
    {
        Sphere = 0,
        Box = 1,
        Cylinder = 2,
        Capsule = 3,
        Torus = 4,
        Plane = 5,
        Heart = 6,
        RainbowStrip = 7,
    }

    public enum RelightBlendMode
    {
        Multiply = 0,
        Set = 1,
        Add = 2
    }

    public struct RelightData
    {
        public Matrix4x4 worldToLocalMatrix;
        public Vector4 params1; // x: Type, y: Radius/Density, z: Softness, w: BlendMode
        public Vector4 params2; // x: Height/Speed, yzw: Box Extents
        public Vector4 color;   // rgb: Color, a: Intensity
    }

    [ExecuteAlways]
    public class SplatRelightVolume : MonoBehaviour
    {
        public RelightShape shape = RelightShape.Sphere;
        public RelightBlendMode blendMode = RelightBlendMode.Add;
        [ColorUsage(false, true)] public Color color = Color.white;
        [Range(0, 10)] public float intensity = 1.0f;
        
        [Header("Common Settings")]
        [Range(0.001f, 1f)] public float softEdge = 0.1f;

        [Header("Shape Specifics")]
        [Tooltip("Sphere Radius, Heart Size, or Sparkle Density")]
        public float radius = 0.5f;
        
        [Tooltip("Box Size for Box and Rainbow")]
        public Vector3 boxSize = Vector3.one;
        
        [Tooltip("Cylinder Height, Heart Thickness, or Sparkle Speed")]
        public float height = 1.0f;
        
        [Range(0f, 1f)] public float torusThickness = 0.2f;

        void OnDrawGizmos()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(color.r, color.g, color.b, 0.5f);
            
            switch (shape)
            {
                case RelightShape.Sphere: Gizmos.DrawWireSphere(Vector3.zero, radius); break;
                case RelightShape.Box: Gizmos.DrawWireCube(Vector3.zero, boxSize); break;
                case RelightShape.Cylinder: DrawWireCylinder(radius, height); break;
                case RelightShape.Capsule: DrawWireCapsule(radius, height); break;
                case RelightShape.Torus: 
                    Gizmos.DrawWireSphere(Vector3.left * radius, torusThickness);
                    Gizmos.DrawWireSphere(Vector3.right * radius, torusThickness);
                    break;
                case RelightShape.Plane: Gizmos.DrawWireCube(new Vector3(0, -0.05f, 0), new Vector3(10, 0.1f, 10)); break;
                case RelightShape.Heart: DrawWireHeart(radius, height); break;
                
                case RelightShape.RainbowStrip:
                    Gizmos.DrawWireCube(Vector3.zero, boxSize);
                    Gizmos.DrawLine(new Vector3(-boxSize.x/2, 0, 0), new Vector3(boxSize.x/2, 0, 0));
                    break;
            }
        }

        // ... Existing DrawWireHeart / Cylinder / Capsule ...
        void DrawWireHeart(float r, float thickness)
        {
            float step = 0.1f;
            Vector3 prev = CalculateHeartPoint(0, r);
            float halfThick = thickness * 0.5f;
            for (float t = step; t <= Mathf.PI * 2; t += step)
            {
                Vector3 curr = CalculateHeartPoint(t, r);
                Gizmos.DrawLine(prev + Vector3.forward * halfThick, curr + Vector3.forward * halfThick);
                Gizmos.DrawLine(prev - Vector3.forward * halfThick, curr - Vector3.forward * halfThick);
                prev = curr;
            }
        }

        Vector3 CalculateHeartPoint(float t, float r)
        {
            float x = 16 * Mathf.Pow(Mathf.Sin(t), 3);
            float y = 13 * Mathf.Cos(t) - 5 * Mathf.Cos(2 * t) - 2 * Mathf.Cos(3 * t) - Mathf.Cos(4 * t);
            return new Vector3(x, y, 0) * (r * 0.05f); 
        }

        void DrawWireCylinder(float r, float h) { /*...*/}
        void DrawWireCapsule(float r, float h) { /*...*/}

        public RelightData GetData()
        {
            RelightData d = new RelightData();
            d.worldToLocalMatrix = transform.worldToLocalMatrix;
            
            d.params1 = new Vector4((float)shape, radius, softEdge, (float)blendMode);
            
            // Logic mapper for secondary params
            float secondaryParam = height; // Default behavior
            
            if (shape == RelightShape.Torus) secondaryParam = torusThickness;
            if (shape == RelightShape.Heart) secondaryParam = height;

            d.params2 = new Vector4(secondaryParam, boxSize.x, boxSize.y, boxSize.z);
            d.color = new Vector4(color.r, color.g, color.b, intensity);
            
            return d;
        }
    }
}