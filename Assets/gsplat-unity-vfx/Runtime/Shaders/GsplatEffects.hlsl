// Copyright (c) 2026 Dreamcore XR Labs

#ifndef GSPLAT_EFFECTS_INCLUDED
#define GSPLAT_EFFECTS_INCLUDED

// Utility helpers
inline float2x2 rot2(float a) {
    float s = sin(a);
    float c = cos(a);
    return float2x2(c, -s, s, c);
}

inline float3 frac3(float3 v) { return frac(v); }

// Quaternion math helpers
inline float4 quatMul(float4 q1, float4 q2)
{
    // Quaternion multiplication: q = q1 * q2
    return float4(
        q1.w * q2.x + q1.x * q2.w + q1.y * q2.z - q1.z * q2.y,
        q1.w * q2.y - q1.x * q2.z + q1.y * q2.w + q1.z * q2.x,
        q1.w * q2.z + q1.x * q2.y - q1.y * q2.x + q1.z * q2.w,
        q1.w * q2.w - q1.x * q2.x - q1.y * q2.y - q1.z * q2.z
    );
}

// Hash functions
inline float3 hash3(float3 p) {
    // similar to GLSL fract(sin(p*123.456)*123.456)
    return frac3(sin(p * 123.456f) * 123.456f);
}

inline float3 hash2_3(float3 p) {
    p = frac3(p * 0.3183099f + 0.1f);
    p *= 17.0f;
    return frac3(float3(p.x * p.y * p.z, p.x + p.y * p.z, p.x * p.y + p.z));
}

// Smooth perlin-style noise (vector-valued & scalar)
inline float3 noise2_vec(float3 p) {
    float3 i = floor(p);
    float3 f = frac(p);
    f = f * f * (3.0f - 2.0f * f);

    float3 n000 = hash2_3(i + float3(0,0,0));
    float3 n100 = hash2_3(i + float3(1,0,0));
    float3 n010 = hash2_3(i + float3(0,1,0));
    float3 n110 = hash2_3(i + float3(1,1,0));
    float3 n001 = hash2_3(i + float3(0,0,1));
    float3 n101 = hash2_3(i + float3(1,0,1));
    float3 n011 = hash2_3(i + float3(0,1,1));
    float3 n111 = hash2_3(i + float3(1,1,1));

    float3 x0 = lerp(n000, n100, f.x);
    float3 x1 = lerp(n010, n110, f.x);
    float3 x2 = lerp(n001, n101, f.x);
    float3 x3 = lerp(n011, n111, f.x);

    float3 y0 = lerp(x0, x1, f.y);
    float3 y1 = lerp(x2, x3, f.y);

    return lerp(y0, y1, f.z);
}

inline float noise_scalar(float3 p) {
    float3 i = floor(p);
    float3 f = frac(p);
    float3 u = f * f * (3.0f - 2.0f * f);

    float3 h000 = hash3(i + float3(0,0,0));
    float3 h100 = hash3(i + float3(1,0,0));
    float3 h010 = hash3(i + float3(0,1,0));
    float3 h110 = hash3(i + float3(1,1,0));
    float3 h001 = hash3(i + float3(0,0,1));
    float3 h101 = hash3(i + float3(1,0,1));
    float3 h011 = hash3(i + float3(0,1,1));
    float3 h111 = hash3(i + float3(1,1,1));

    float n000 = dot(h000, f - float3(0,0,0));
    float n100 = dot(h100, f - float3(1,0,0));
    float n010 = dot(h010, f - float3(0,1,0));
    float n110 = dot(h110, f - float3(1,1,0));
    float n001 = dot(h001, f - float3(0,0,1));
    float n101 = dot(h101, f - float3(1,0,1));
    float n011 = dot(h011, f - float3(0,1,1));
    float n111 = dot(h111, f - float3(1,1,1));

    float nx00 = lerp(n000, n100, u.x);
    float nx10 = lerp(n010, n110, u.x);
    float nx01 = lerp(n001, n101, u.x);
    float nx11 = lerp(n011, n111, u.x);

    float nxy0 = lerp(nx00, nx10, u.y);
    float nxy1 = lerp(nx01, nx11, u.y);

    return lerp(nxy0, nxy1, u.z);
}

// Motion helpers
inline float3 perlinMotion(float3 pos, float t, float intensity) {
    pos += float3(
        noise_scalar(pos + t * 0.3f),
        noise_scalar(pos + t * 0.4f + 5.0f),
        noise_scalar(pos + t * 0.5f + 10.0f)
    ) * intensity * 0.2f;
    return pos;
}

inline float3 windMotion(float3 pos, float t, float intensity, float3 windDir) {
    float3 dir = normalize(windDir);
    float sway = sin(t + dot(pos, dir) * 0.5f) * 0.1f;
    pos += dir * intensity * 0.5f + dir * sway * intensity;
    return pos;
}

inline float4 twister_effect(float3 pos, float3 scale, float t) {
    float h = hash2_3(pos).x ;
    float s = smoothstep(0.0f, 8.0f, t * t * 0.1f - length(pos.xz) * 2.0f + 2.0f);
    if (length(scale) < 0.05f) pos.y = lerp(-10.0f, pos.y, pow(s, 2.0f * h));
    pos.xz = lerp(pos.xz * 0.5f, pos.xz, pow(s, 2.0f * h));
    float rotationTime = t * (1.0f - s) * 0.2f;
    float ang = rotationTime + pos.y * 20.0f * (1.0f - s) * exp(-length(pos.xz));
    pos.xz = mul(pos.xz, rot2(ang));  
    return float4(pos, s * s * s * s);
}

inline float4 rain_effect(float3 pos, float3 scale, float t) {
    float3 h = hash2_3(pos);
    float s = pow(smoothstep(0.0f, 5.0f, t * t * 0.1f - length(pos.xz) * 2.0f + 1.0f), 0.5f + h.x);
    float y = pos.y;
    pos.y = min(-10.0f + s * 15.0f, pos.y);
    pos.xz = lerp(pos.xz * 0.3f, pos.xz, s);
    //pos.xz = mul(pos.xz, rot2(t * 0.3f)); // uncomment this line if you want the scene to rotate automatically
    return float4(pos, smoothstep(-10.0f, y, pos.y));
}

// Fractals (rough port)
inline float4 fractal1_effect(float3 pos, float t, float intensity) {
    float m = 100.0f;
    float3 p = pos * 0.1f;
    p.y += 0.5f;
    for (int i = 0; i < 8; ++i) {
        p = abs(p) / clamp(abs(p.x * p.y), 0.3f, 3.0f) - 1.0f;
        p.xy = mul(p.xy, rot2(radians(90.0f)));
        if (i > 1) m = min(m, length(p.xy) + step(0.3f, frac(p.z * 0.5f + t * 0.5f + (float)i * 0.2f)));
    }
    m = step(m, 0.5f) * 1.3f * intensity;
    return float4(-pos.y * 0.3f, 0.5f, 0.7f, 0.3f) * intensity + float4(m, m, m, 0.0f);
}

inline float4 fractal2_effect(float3 center, float3 scales, float4 rgba, float t, float intensity) {
    float3 pos = center;
    float splatSize = length(scales);
    float pattern = exp(-50.0f * splatSize);
    float3 p = pos * 0.65f;
    pos.y += 2.0f;
    float c = 0.0f;
    float l2 = length(p);
    float m = 100.0f;
    for (int i = 0; i < 10; ++i) {
        p = abs(p) / dot(p, p) - 0.8f;
        float l = length(p);
        c += exp(-1.0f * abs(l - l2) * (1.0f + sin(t * 1.5f + pos.y)));
        l2 = length(p);
        m = min(m, length(p));
    }
    c = smoothstep(0.3f, 0.5f, m + sin(t * 1.5f + pos.y * 0.5f)) + c * 0.1f;
    float alpha = rgba.a * exp(-20.0f * splatSize) * m * intensity;
    float3 outc = float3(length(rgba.rgb), length(rgba.rgb), length(rgba.rgb)) * float3(c, c * c, c * c * c) * intensity;
    return float4(outc, alpha);
}

inline float4 sin3D_light_effect(float3 p, float t, float amplitude, float frequency, float speed) {
    float m = exp(amplitude * length(sin(p *frequency + t * speed))) * 5.0f;
    return float4(m, m, m, 0.3f);
}

inline float4 disintegrate_effect(float3 pos, float t, float intensity) {
    float3 p = pos + (hash3(pos) * 2.0f - 1.0f) * intensity;
    float tt = smoothstep(-1.0f, 0.5f, -sin(t + -pos.y * 0.5f));
    p.xz = mul(p.xz, rot2(tt * 2.0f + p.y * 2.0f * tt));
    return float4(lerp(p, pos, tt), tt);
}

inline float4 flare_effect(float3 pos, float t) {
    float3 p = float3(0.0f, -1.5f, 0.0f);
    float tt = smoothstep(-1.0f, 0.5f, sin(t + hash3(pos).x));
    tt *= tt;
    p.x += sin(t * 2.0f) * tt; p.z += sin(t * 2.0f) * tt; p.y += sin(t) * tt;
    return float4(lerp(pos, p, tt), tt);
}

inline float4 perlinWave_effect(float3 pos, float3 scales, float4 rgba, float t, float intensity) {
    float3 p = pos + float3(
        noise_scalar(pos + t * 0.3f),
        noise_scalar(pos + t * 0.4f + 5.0f),
        noise_scalar(pos + t * 0.5f + 10.0f)
    ) * intensity * 0.2f;

    float splatSize = length(scales);
    float l = length(p.xy);
    float c = sin(l * 10.0f + t * 1.5f) * 0.5f + 0.5f;
    float fade = exp(-5.0f * splatSize);
    float3 color = rgba.rgb * (0.5f + 0.5f * c) * fade * intensity;
    float alpha = rgba.a * fade;
    return float4(color, alpha);
}


// Main API: modify center, scales, rgba in-place
inline void ApplyGsplatEffect(inout float3 center, inout float3 scales, inout float4 rgba, 
                              int effectType, float t, float intensity, float3 windDir,
                              float waveAmplitude, float waveFrequency, float waveSpeed, float blendScale,
                              float lightWaveAmplitude, float lightWaveFrequency, float lightWaveSpeed, 
                              float glitterDensity, float dissolveDriftSpeed, float burnDuration)
{
    float3 localPos = center;
    float3 splatScales = scales;
    float4 splatColor = rgba;
    float4 quaternion = float4(0.0f, 0.0f, 0.0f, 1.0f);

    if (effectType == 1) {
        // deep meditation, as in spark 
        float4 e = fractal2_effect(localPos, splatScales, splatColor, t, intensity);
        rgba = lerp(splatColor, e, intensity);
        // breath animation (subtle scale + y offset)
        float b = sin(t * 1.5f);
        center.y += b * 0.02f * intensity;
    }
    else if (effectType == 2) {
        float4 e = sin3D_light_effect(localPos, t, lightWaveAmplitude, lightWaveFrequency, lightWaveSpeed);
        rgba = lerp(splatColor, float4(splatColor.rgb * e.rgb, splatColor.a), intensity);
    }
    else if (effectType == 3) {
        // flare effect, as in spark
        float4 e = flare_effect(localPos, t);
        center = e.xyz;
        rgba.rgb = lerp(splatColor.rgb, float3(1.0f,1.0f,1.0f), abs(e.w));
        rgba.a = lerp(splatColor.a, 0.3f, abs(e.w));
    }
    else if (effectType == 4) {
        // disintegrate effect, as in spark
        float4 e = disintegrate_effect(localPos, t, intensity);
        center = e.xyz;
        scales = lerp(float3(0.01f,0.01f,0.01f), scales, e.w);
    }
    else if (effectType == 5) {
        //  wind effect, new extension
        float3 dir = windDir;
        center = windMotion(localPos, t, intensity, dir);
        rgba.rgb = lerp(splatColor.rgb, splatColor.rgb + float3(0.02f,0.05f,0.08f) * intensity, 0.3f);
    }
    else if (effectType == 6) {
        // perlin wave effect, new extension
        // Point cloud + blending + 3DGS + wave + perlin noise
        center = localPos + waveAmplitude * noise2_vec(localPos * waveFrequency + t * waveSpeed) ;
        float4 e = sin3D_light_effect(localPos, t, lightWaveAmplitude, lightWaveFrequency, lightWaveSpeed);
        rgba = lerp(splatColor, float4(splatColor.rgb * e.rgb, splatColor.a), intensity);
        
        float3 finalScales; 

        if(blendScale<0.1){
            finalScales = float3(0.001, 0.001, 0.001);
        }else{
            finalScales = splatScales* blendScale;
        }
        scales = finalScales;
    }
    else if (effectType == 7) {
        // Magic reveal effect, as in spark
        float l = length(localPos.xz);
        float s = smoothstep(0.0f, 10.0f, t - 4.5f) * 10.0f;
        float border = abs(s - l - 0.5f);
        localPos *= 1.0f - 0.2f * exp(-20.0f * border);
        float3 finalScales = lerp(splatScales, float3(0.001f,0.001f,0.001f), smoothstep(s - 0.5f, s, l + 0.5f));
        center = localPos + 0.1f * noise2_vec(localPos * 2.0f + t * 0.5f) * smoothstep(s - 0.5f, s, l + 0.5f);
        scales = finalScales;
        float at = atan2(localPos.x, localPos.z) / 3.14159265f;
        rgba *= step(at, t - 3.14159265f);
        rgba += float4(exp(-20.0f * border), exp(-20.0f * border), exp(-20.0f * border), 0.0f) + float4(exp(-50.0f * abs(t - at - 3.14159265f)) * 0.5f,                                                                                        exp(-50.0f * abs(t - at - 3.14159265f)) * 0.5f,                                                                          exp(-50.0f * abs(t - at - 3.14159265f)) * 0.5f, 0.0f);
    }

    else if(effectType==8){
        // spread effect, as in spark
        float tt = t * t * 0.4f + 0.5f;
        float mulFactor = min(1.0f, 0.3f + max(0.0f, tt * 0.05f));
        localPos.xz *= mulFactor;
        float l = length(localPos.xz);
        float m1 = min(tt - 7.0f - l * 2.5f, 1.0f);
        float m2 = min(tt - 1.0f - l * 2.0f, 1.0f);
        float3 sA = lerp(float3(0.0f, 0.0f, 0.0f), splatScales, saturate(m1));
        float3 sB = lerp(float3(0.0f, 0.0f, 0.0f), splatScales * 0.2f, saturate(m2));
        scales = max(sA, sB);
        float blend = saturate(tt - l * 2.5f - 3.0f);
        rgba = lerp(float4(0.3f, 0.3f, 0.3f, 0.3f), splatColor, blend);
        center = localPos;
    }
    else if(effectType==9){

        // unroll effect, as in spark
        float ang = (localPos.y * 50.0f - 20.0f) * exp(-t);
        localPos.xz = mul(localPos.xz, rot2(ang));
        center = localPos * (1.0f - exp(-t) * 2.0f);
        float ss = smoothstep(0.3f, 0.7f, t + localPos.y - 2.0f);
        scales = lerp(float3(0.002f, 0.002f, 0.002f), splatScales, ss);
        float mask = (t * 0.5f + localPos.y - 0.5f) >= 0.0f ? 1.0f : 0.0f;
        rgba = splatColor * mask;
    }
    else if(effectType==10){
        // twister effect, as in spark

        float4 e = sin3D_light_effect(localPos, t, lightWaveAmplitude, lightWaveFrequency, lightWaveSpeed);
        rgba = lerp(splatColor, float4(splatColor.rgb * e.rgb, splatColor.a), intensity);
        // Twister Effect: swirling weather reveal
        float4 effectResult = twister_effect(localPos, splatScales, t);
        center = effectResult.xyz;
        scales = lerp(float3(0.002f, 0.002f, 0.002f), splatScales, pow(effectResult.w, 12.0f));

        float s = effectResult.w;
        float spin = -t * 0.3f * (1.0f - s);
        float4 spinQ = float4(0.0f, sin(spin * 0.5f), 0.0f, cos(spin * 0.5f));
        quaternion = quatMul(spinQ, quaternion); 
    }
    else if (effectType == 11)
    {
        // Rain Effect, as in spark
        
        float4 e = sin3D_light_effect(localPos, t, lightWaveAmplitude, lightWaveFrequency, lightWaveSpeed);
        rgba = lerp(splatColor, float4(splatColor.rgb * e.rgb, splatColor.a), intensity);
        float4 effectResult = rain_effect(localPos, splatScales, t);
        center = effectResult.xyz;
        scales = lerp(float3(0.005f, 0.005f, 0.005f), splatScales, pow(effectResult.w, 30.0f));
        quaternion = float4(1.0f, 0.0f, 0.0f, 0.0f);
        rgba.rgb = lerp(rgba.rgb, rgba.rgb * 0.85f + float3(0.05f, 0.07f, 0.10f), 0.25f * effectResult.w);
        rgba.a *= saturate(effectResult.w);
    }
    else if (effectType == 12){
        // basic glitter effects, new design and implementation
        float3 hashVal = hash3(localPos);
        if (hashVal.z < glitterDensity) 
        {
            float glow = 0.0f;
            glow += sin(t * (5.0 + hashVal.x * 10.0) + hashVal.x * 6.28318) * 0.5 + 0.5;
            glow += sin(t * (3.0 + hashVal.y * 8.0) + hashVal.y * 6.28318) * 0.5 + 0.5;
            glow += sin(t * (2.0 + hashVal.z * 6.0) + hashVal.z * 6.28318) * 0.5 + 0.5;
            glow /= 3.0f;          // average
            glow = pow(glow, 2.0f); // sharpen peaks

            float3 glitterEmissiveColor = float3(1.5f, 1.8f, 2.0f); // Bright, cool white/blue
            float3 finalGlitterColor = glitterEmissiveColor * glow;
            scales = float3(0.002f,0.002f, 0.002f);
            rgba.rgb = lerp(rgba.rgb, finalGlitterColor, glow); // The 'glow' (0-1) now controls the strength/twinkle
        }
    }
    else if (effectType == 13){
        // Glitter Galaxy with Fading Particles, new design and implementations
        float3 hashVal = hash3(localPos);

        // Use the same density control
        if (hashVal.z < glitterDensity) 
        {
            // --- 1. Size Logic ---
            float isLarge = step(0.9, hashVal.y); 
            float starSize = lerp(0.0008f, 0.002f, isLarge);
            scales = float3(starSize, starSize, starSize);

            // --- 2. Galaxy Color Palette ---
            float3 colPurple = float3(0.15, 0.05, 0.35);
            float3 colCyan   = float3(0.0, 0.7, 1.0);
            float3 colPink   = float3(1.0, 0.2, 0.6);
            float3 colWhite  = float3(1.0, 0.95, 0.8);

            float3 starColor;
            float h = hashVal.x;

            if (h < 0.33)       starColor = lerp(colPurple, colCyan, h * 3.0);
            else if (h < 0.66)  starColor = lerp(colCyan, colPink, (h - 0.33) * 3.0);
            else                starColor = lerp(colPink, colWhite, (h - 0.66) * 3.0);

            // --- 3. Twinkle Animation ---
            float glow = 0.0f;
            float speed = 2.0 + (hashVal.z * 4.0); 

            glow += sin(t * speed + hashVal.x * 6.28318) * 0.5 + 0.5;
            glow += sin(t * (speed * 0.5) + hashVal.y * 6.28318) * 0.5 + 0.5;
            glow *= 0.5; // Average

            float sharpness = lerp(4.0, 2.0, isLarge); 
            glow = pow(glow, sharpness);

            float brightness = 1.0 + (isLarge * 2.0); 
            float3 finalGalaxyColor = starColor * brightness;

            // --- 4. Fading Logic ---
            // Assign a random lifetime per particle
            float lifetime = 2.0 + hashVal.z * 3.0; // 2s to 5s
            float ageOffset = hashVal.y * lifetime; // random start offset
            float age = fmod(t + ageOffset, lifetime); 
            float alpha = 1.0 - (age / lifetime); // fade from 1 to 0 over lifetime

            // Optional: make small stars fade faster
            alpha *= lerp(1.0, 0.6, isLarge); 
            float4 e = sin3D_light_effect(localPos, t, lightWaveAmplitude, lightWaveFrequency, lightWaveSpeed);
            rgba = lerp(splatColor, float4(splatColor.rgb * e.rgb, splatColor.a), intensity);

            // --- 5. Final Output ---
            rgba.rgb = lerp(rgba.rgb, finalGalaxyColor, glow);
            rgba.a   = alpha; // apply fade
        }
    }
    else if (effectType == 14){
        // flying dissolve with glitter, new design and implementations
        float _driftSpeed = 0.1f; 
        float4 e = sin3D_light_effect(localPos, t, lightWaveAmplitude, lightWaveFrequency, lightWaveSpeed);
        rgba = lerp(splatColor, float4(splatColor.rgb * e.rgb, splatColor.a), intensity);
        float3 hashVal = hash3(localPos);

        float startTime = hashVal.x * 100.0f;

        float shouldOscillate = (t >= startTime) ? 1.0f : 0.0f;

        float oscillation = sin(t * 2.0f + hashVal.y * 6.2831853f) * 0.5f + 0.5f;

        float3 moveDirection = normalize(float3(
            (hashVal.x - 0.5f) * 0.6f,  // small random left–right
            -1.0f,                        // strong upward bias
            (hashVal.z - 0.5f) * 0.6f    // small random forward–back
        ));

        float randomSpeed = frac(sin(dot(center, float3(12.0f, 78.0f, 45.0f))) * 43758.0f);

        float moveAmount = t * dissolveDriftSpeed * randomSpeed * shouldOscillate;
        center += moveDirection * moveAmount;
        float shrinkFactor = saturate(moveAmount * 0.8f);

        shrinkFactor = smoothstep(0.0f, 1.0f, shrinkFactor);

        float3 minScale = float3(0.003f, 0.003f, 0.003f);
        scales = lerp(splatScales, minScale, shrinkFactor);

        if (hashVal.z < glitterDensity) // 0.3f is 30%
        {
            float glow = 0.0f;
            glow += sin(t * (5.0 + hashVal.x * 10.0) + hashVal.x * 6.28318) * 0.5 + 0.5;
            glow += sin(t * (3.0 + hashVal.y * 8.0) + hashVal.y * 6.28318) * 0.5 + 0.5;
            glow += sin(t * (2.0 + hashVal.z * 6.0) + hashVal.z * 6.28318) * 0.5 + 0.5;
            glow /= 3.0f;          
            glow = pow(glow, 2.0f); // sharpen peaks
            float3 glitterEmissiveColor = float3(1.5f, 1.8f, 2.0f); // Bright, cool white/blue
            float3 finalGlitterColor = glitterEmissiveColor * glow;
            rgba.rgb = lerp(rgba.rgb, finalGlitterColor, shrinkFactor);
            float fadeOut = smoothstep(0.7f, 1.0f, shrinkFactor);
            rgba.a *= lerp(1.0f, 1.0f - fadeOut, shouldOscillate);

        }else{
            
            rgba.a *= lerp(1.0f, 1.0f - clamp(moveAmount * 0.5f, 0.0f, 1.0f), shouldOscillate);
        }

    }
    else if (effectType == 15){
        // glow dissolve , new design and implementations
        // --- CONFIGURATION ---
        //float burnDuration = 0.5f;      // How long the particle takes to burn away
        float maxGlowIntensity = intensity*2.0f;  // Max emissive brightness
        float3 glowColor = float3(2.0f, 1.0f, 0.2f); // Hot white/yellow-orange color

        // --- 1. HASH & TIMING ---
        float3 hashVal = hash3(localPos); 

        // Time randomization: Tighter window than slow melts, but still varied.
        float startTime = hashVal.y * 0.8f; 

        // Calculate local time for this specific splat
        float localT = t - startTime;
        float shouldBurn = (localT >= 0.0f) ? 1.0f : 0.0f;

        // "burnProgress" goes from 0.0 to 1.0 over the duration
        float burnProgress = saturate(localT / burnDuration) * shouldBurn;

        if (shouldBurn > 0.5f)
        {
            // --- 2. EMISSION & GLOW (The "Burn") ---
            
            // Use a power curve (x^4) to make the glow rapidly spike, then fade
            // This is the core of the burning effect: sudden brightness then decay.
            float glowCurve = pow(sin(burnProgress * 3.14159f), 4.0f); 
            
            // Calculate the final emissive color
            float3 glowEmission = glowColor * maxGlowIntensity * glowCurve;

            // Apply the emissive glow to the splat's color (additive)
            rgba.rgb += glowEmission;

            // --- 3. SHRINK & DISSOLVE ---
            
            // Use a smoothstep curve to ensure a clean visual fade-out.
            // Shrink the splat aggressively as the burn peaks (0.5 to 1.0 progress)
            float shrinkFactor = smoothstep(0.5f, 1.0f, burnProgress);
            
            // Lerp scales from splatScales (original) to minScale (near zero)
            float3 minScale = float3(0.005f, 0.005f, 0.005f);
            scales = lerp(splatScales, minScale, shrinkFactor);

            // --- 4. ALPHA FADE-OUT ---
            
            // Fade the alpha out slightly slower than the scale to let the glow linger
            // Fade starts around 0.6 progress, ends at 1.0
            float fadeOut = smoothstep(0.6f, 1.0f, burnProgress);
            rgba.a *= lerp(1.0f, 0.0f, fadeOut);

            // Apply a small random upward/outward drift during the burn
            // Use a different hash for subtle movement during the burn
            float3 randomDir = normalize(hash3(center) - 0.5f);
            center += randomDir * burnProgress * 0.05f;
        }
    }
    else if (effectType == 16){
        //radial expansion
        // new effect and implementation
                // 1. Setup Data
        float3 originalPos = center;
        float dist = length(originalPos);
        float3 dir = (dist > 0.0001f) ? normalize(originalPos) : float3(0, 1, 0);
        float3 hashVal = hash3(originalPos * 10.0f); // Random seed per splat

        // 2. Expansion Logic
        // Moves the particles outwards from the center (0,0,0).
        // Max expansion distance is 2.0 units multiplied by intensity.
        float expansionAmount = intensity * 2.0f; 

        // Add some noise to the direction so it doesn't look like a perfect sphere expansion
        // We use the intensity in the noise lookup so the turbulence evolves as hands move
        float3 noiseDir = hashVal * 2.0f - 1.0f; 
        center += (dir + noiseDir * 0.2f) * expansionAmount;

        // --- NEW: Motion after expansion threshold ---
        float expansionThreshold = 1.2f; // Threshold for motion
        if (expansionAmount > expansionThreshold)
        {
            // Subtle Brownian / swirl motion based on time T
            float motionStrength = 0.05f; // Small drift
            float3 swirl = float3(-dir.z, dir.y, dir.x); // gentle swirl perpendicular to direction
            float3 drift = swirl * sin(t + dot(originalPos, float3(12.34, 56.78, 90.12))) * motionStrength;
            
            // Add small random Brownian motion
            float3 brownian = (hashVal * 2.0f - 1.0f) * motionStrength * 0.5f * sin(t * 3.0 + hashVal.x * 6.28);
            
            center += drift + brownian;
            
            // Slightly vary the scale of the particle
            float scaleVariation = 0.85f + 0.15f * hashVal.y; // 0.85 - 1.0x original
            scales *= scaleVariation;
        }

        // 3. Shrink Logic
        // As intensity increases, splats shrink to dust (tiny points).
        // We keep them slightly visible (0.001) rather than deleting them entirely.
        float3 targetScale = float3(0.001f, 0.001f, 0.001f);

        // Non-linear interpolation for scale (shrinks fast at the beginning)
        float shrinkT = smoothstep(0.0f, 0.8f, intensity);
        scales = lerp(scales, targetScale, shrinkT);

        // 4. Glitter/Glow Logic
        // Only affect X% of particles based on density (e.g., 40%)
        float density = 0.0f;

        if (expansionAmount > expansionThreshold)
        {
            density = 0.3f;

        }
        if (hashVal.z < density) 
        {
            // Create a sparkle that changes based on how far expanded the object is.
            float sparkleFreq = 20.0f;
            float sparklePhase = dot(originalPos, float3(12.9898, 78.233, 45.164));
            
            // Sine wave for twinkling
            float blink = sin(intensity * sparkleFreq + sparklePhase);
            blink = smoothstep(0.5f, 1.0f, blink);

            // Brighter as it expands
            float brightness = 2.0f * intensity; 
            
            // Diamond/White glitter color
            float3 glitterColor = float3(1.2f, 1.5f, 2.0f); 

            // Mix original color with glitter based on shrink amount and blink
            rgba.rgb = lerp(rgba.rgb,  rgba.rgb* brightness, blink * shrinkT);
            
            // Keep alpha high for glitter particles, fade others slightly
            rgba.a = max(rgba.a, blink);
        }
        else 
        {
            // For non-glitter particles, we fade them out slightly as they expand
            rgba.a *= (1.0f - intensity * 0.5f);
        }
    }
}

#endif // GSPLAT_EFFECTS_INCLUDED
