// Copyright (c) 2026 Dreamcore XR Labs

using System;
using UnityEngine;
using System.Collections;

namespace Gsplat
{
    public enum GsplatEffectType
    {
        None = 0,
        DeepMeditation = 1, // visualization effect
        Waves = 2, // visualization effect
        Flare = 3, // reveal effects
        Disintegrate = 4,  // reveal effects
        Wind = 5, // visualization effect
        PerlinWave = 6, // visualization effect
        Magic = 7, // reveal effects

        Spread = 8, // reveal effects

        Unroll = 9, // reveal effects

        Twister = 10, // reveal effects

        Rain = 11, // reveal effects
        Glitter = 12,  // visualization effect
        GalaxyGlitter = 13, // visualization effect

        FlyDissolve = 14, // dissolve effect
        GlowDissolve = 15,  // glow dissolve effect

        RadialExpansion = 16, // radial expansion effect
    }

    [ExecuteAlways]
    public class GsplatRenderer : MonoBehaviour, IGsplat
    {
        public GsplatAsset GsplatAsset;
        [Range(0, 3)] public int SHDegree = 3;
        public bool GammaToLinear;

        GsplatAsset m_prevAsset;
        GsplatRendererImpl m_renderer;

        public bool Valid => GsplatAsset;
        public uint SplatCount => GsplatAsset ? GsplatAsset.SplatCount : 0;
        public ISorterResource SorterResource => m_renderer.SorterResource;
        public GsplatEffectType effectType = GsplatEffectType.PerlinWave;
        [Range(0f, 1f)] public float intensity = 0.5f;
        [Range(-0.2f, 0.2f)] public float waveAmplitude = 0.1f;
        [Range(0.1f, 6f)] public float waveFrequency = 2f;
        [Range(0f, 2f)] public float waveSpeed = 0.5f;
        [Range(0f, 1f)] public float blendScale = 0.5f;
        [Range(-5f, -1f)] public float lightWaveAmplitude = -2.0f;
        [Range(-5f, 5f)] public float lightWaveFrequency = 2.0f;
        [Range(0.1f, 10f)] public float lightWaveSpeed = 2.0f;
        [Range(0.0f, 1.0f)] public float glitterDensity = 0.2f;
        [Range(0.1f, 3.0f)] public float burnDuration = 0.5f;

        float baseTime = 0;

        void SetBufferData()
        {
            m_renderer.PositionBuffer.SetData(GsplatAsset.Positions);
            m_renderer.ScaleBuffer.SetData(GsplatAsset.Scales);
            m_renderer.RotationBuffer.SetData(GsplatAsset.Rotations);
            m_renderer.ColorBuffer.SetData(GsplatAsset.Colors);

            if (GsplatAsset.SHBands > 0)
                m_renderer.SHBuffer.SetData(GsplatAsset.SHs);
        }

        void OnEnable()
        {
            GsplatSorter.Instance.RegisterGsplat(this);
            if (!GsplatAsset)
                return;
            m_renderer = new GsplatRendererImpl(GsplatAsset.SplatCount, GsplatAsset.SHBands);
            SetBufferData();
        }

        void OnDisable()
        {
            GsplatSorter.Instance.UnregisterGsplat(this);
            m_renderer?.Dispose();
            m_renderer = null;
        }
        void Update()
        {


            if (m_prevAsset != GsplatAsset)
            {
                m_prevAsset = GsplatAsset;
                if (GsplatAsset)
                {
                    if (m_renderer == null)
                        m_renderer = new GsplatRendererImpl(GsplatAsset.SplatCount, GsplatAsset.SHBands);
                    else
                        m_renderer.RecreateResources(GsplatAsset.SplatCount, GsplatAsset.SHBands);
                    SetBufferData();
                }
            }

            if (Valid)
            {
                baseTime = baseTime + Time.deltaTime;
                m_renderer.SetEffectParameters(
                    (int)effectType,
                    intensity,
                    baseTime,
                    new Vector3(1, 0, 0),
                    waveAmplitude,
                    waveFrequency,
                    waveSpeed,
                    blendScale,
                    lightWaveAmplitude,
                    lightWaveFrequency,
                    lightWaveSpeed,
                    glitterDensity,
                    0.3f,
                    burnDuration
                );
                m_renderer.UpdateRelighting(); 

                m_renderer.Render(GsplatAsset.SplatCount, transform, GsplatAsset.Bounds, gameObject.layer,
                    GammaToLinear, SHDegree);
            }

        }

        public void resetAnimationTime()
        {
            baseTime = 0;
        }


        #region visual effects

        public IEnumerator BlendReveal(float duration)
        {
            float startValue = 0.001f;
            float endValue = 1f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                blendScale = Mathf.Lerp(startValue, endValue, elapsed / duration);
                yield return null;
            }

            blendScale = endValue; // ensure it ends exactly at 1
        }

        public void SetMagicEffect()
        {
            effectType = GsplatEffectType.Magic;
            resetAnimationTime();
            StartCoroutine(WaitUntilRevealFinish());
        }
        public void SetRainEffect()
        {
            effectType = GsplatEffectType.Rain;
            resetAnimationTime();
            StartCoroutine(WaitUntilRevealFinish());
        }
        public void SetTwisterEffect()
        {
            effectType = GsplatEffectType.Twister;
            resetAnimationTime();
            StartCoroutine(WaitUntilRevealFinish());
        }
        public IEnumerator WaitUntilRevealFinish()
        {
            yield return new WaitForSeconds(10.5f);
            effectType = GsplatEffectType.PerlinWave;

        }
        public void StartBlendAppear(float effectTime=2.0f)
        {
            StartCoroutine(BlendAppear(effectTime));
        }
        public void StartBlendDisappear(float effectTime = 2.0f)
        {
            StartCoroutine(BlendDisappear(effectTime));
        }

        public void StartShockWaveEffect(float effectTime = 1.0f)
        {
            StartCoroutine(ShockWaveEffect(effectTime));
        }

        public IEnumerator BlendAppear(float duration)
        {
            float startValue = 0.001f;
            float endValue = 1f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                blendScale = Mathf.Lerp(startValue, endValue, elapsed / duration);
                yield return null;
            }

            blendScale = endValue; 
        }

        public IEnumerator BlendDisappear(float duration)
        {
            float startValue = 1f;
            float endValue = 0.001f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                blendScale = Mathf.Lerp(startValue, endValue, elapsed / duration);
                yield return null;
            }

            blendScale = endValue; 
        }


        public IEnumerator ShockWaveEffect(float duration)
        {
            yield return new WaitForSeconds(1.0f);

            float startValue = waveFrequency;
            float startWaveSpeed = waveSpeed;
            float targetValue = 1.5f;
            float elapsed = 0f;

            while (elapsed < duration / 2f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (duration / 2f);
                waveFrequency = Mathf.Lerp(startValue, targetValue, t);
                waveSpeed = Mathf.Lerp(startValue, targetValue, t);
                yield return null;
            }

            elapsed = 0f;

            while (elapsed < duration / 2f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (duration / 2f);
                waveFrequency = Mathf.Lerp(targetValue, startValue, t);
                waveSpeed = Mathf.Lerp(targetValue, startWaveSpeed, t);
                yield return null;
            }

            waveFrequency = startValue; // ensure exact reset
        }
    }
    #endregion

}