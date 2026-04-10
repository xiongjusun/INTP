// Copyright (c) 2026 Dreamcore XR Labs
// GsplatRelighting.hlsl

struct RelightData
{
    float4x4 worldToLocalMatrix;
    float4 params1; // x: Type, y: Radius, z: Softness, w: BlendMode
    float4 params2; // x: Height/Thick, yzw: Box Extents
    float4 color;   // rgb: Color, a: Intensity
};

StructuredBuffer<RelightData> _RelightBuffer;
int _RelightCount;

// --- SDF Primitives ---

float sdSphere(float3 p, float s) { return length(p) - s; }

float sdBox(float3 p, float3 b) {
    float3 q = abs(p) - b / 2.0;
    return length(max(q, 0.0)) + min(max(q.x, max(q.y, q.z)), 0.0);
}

float sdCylinder(float3 p, float h, float r) {
    float2 d = abs(float2(length(p.xz), p.y)) - float2(r, h / 2.0);
    return min(max(d.x, d.y), 0.0) + length(max(d, 0.0));
}

float sdCapsule(float3 p, float h, float r) {
    p.y -= clamp(p.y, -h / 2.0, h / 2.0);
    return length(p) - r;
}

float sdTorus(float3 p, float2 t) {
    float2 q = float2(length(p.xz) - t.x, p.y);
    return length(q) - t.y;
}

float sdPlane(float3 p) { return p.y; }

float dot2(float2 v) { return dot(v,v); }
float sdHeart2D(float2 p)
{
    p.x = abs(p.x);
    if( p.y+p.x>1.0 )
        return sqrt(dot2(p-float2(0.25,0.75))) - sqrt(2.0)/4.0;
    return sqrt(min(dot2(p-float2(0.00,1.00)),
                    dot2(p-0.5*max(p.x+p.y,0.0)))) * sign(p.x-p.y);
}

float sdHeart(float3 p, float size, float thickness)
{
    float2 q = p.xy / size; 
    
    float d2d = sdHeart2D(q) * size; 
    
    float zDist = abs(p.z) - thickness / 2.0;
    
    return min(max(d2d, zDist), 0.0) + length(max(float2(d2d, zDist), 0.0));
}

// Helper: HSV to RGB for Rainbow
float3 hsv2rgb(float3 c)
{
    float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
    return c.z * lerp(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
}

float hash13(float3 p3)
{
    p3  = frac(p3 * .1031);
    p3 += dot(p3, p3.yzx + 33.33);
    return frac((p3.x + p3.y) * p3.z);
}

void ApplyRelighting(float3 localPos, inout float4 rgba, float4x4 modelMatrix)
{
    float3 worldPos = mul(modelMatrix, float4(localPos, 1.0)).xyz;

    for (int i = 0; i < _RelightCount; i++)
    {
        RelightData light = _RelightBuffer[i];
        float3 p = mul(light.worldToLocalMatrix, float4(worldPos, 1.0)).xyz;

        uint type = (uint)light.params1.x;
        float radius = light.params1.y;  
        float softness = light.params1.z;
        uint blendMode = (uint)light.params1.w;
        
        float height = light.params2.x;   
        float3 boxSize = light.params2.yzw;
        float3 lightColor = light.color.rgb;
        float intensity = light.color.a;

        float dist = 100.0;
        
        // Flags
        bool isRainbow = (type == 7);
        bool isScintillation = (type == 8);

        switch(type)
        {
            case 0: dist = sdSphere(p, radius); break;
            case 1: dist = sdBox(p, boxSize); break;
            case 2: dist = sdCylinder(p, height, radius); break;
            case 3: dist = sdCapsule(p, height, radius); break;
            case 4: dist = sdTorus(p, float2(radius, height)); break;
            case 5: dist = sdPlane(p); break;
            case 6: dist = sdHeart(p, radius, height); break; 
            case 7: dist = sdBox(p, boxSize); break; // Rainbow uses Box
        }

        float effectFactor = 1.0 - smoothstep(0.0, softness, dist); 
        
        if (effectFactor <= 0.0) continue;

        // --- Rainbow Logic ---
        if (isRainbow)
        {
            float normalizedX = (p.x / boxSize.x) + 0.5;
            lightColor = hsv2rgb(float3(normalizedX, 1.0, 1.0));
        }
        
        if (isScintillation)
        {
            float density = max(1.0, radius * 10.0); 
            float3 gridId = floor(p * density);
            
            float rnd = hash13(gridId);
            
            float speed = height * 5.0;
            float blink = sin(_Time.y * speed + rnd * 6.2831); // -1 to 1
            
            blink = pow(max(0.0, blink), 8.0); 
            float mask = step(0.8, rnd); 
            
            float sparkleParams = blink * mask;

            lightColor += (lightColor * sparkleParams * 5.0);
            
        }

        float3 finalColor = lightColor * intensity;

        if (blendMode == 0) // Multiply
        {
            rgba.rgb = lerp(rgba.rgb, rgba.rgb * finalColor, effectFactor);
        }
        else if (blendMode == 1) // Set
        {
            rgba.rgb = lerp(rgba.rgb, finalColor, effectFactor);
        }
        else if (blendMode == 2) // Add
        {
            rgba.rgb += (finalColor * effectFactor);
        }
    }
}