// Copyright (c) 2025 Yize Wu
// SPDX-License-Identifier: MIT

using UnityEngine;

namespace Gsplat
{
    public class GsplatRendererImpl
    {
        public uint SplatCount { get; private set; }
        public byte SHBands { get; private set; }

        MaterialPropertyBlock m_propertyBlock;
        public GraphicsBuffer PositionBuffer { get; private set; }
        public GraphicsBuffer ScaleBuffer { get; private set; }
        public GraphicsBuffer RotationBuffer { get; private set; }
        public GraphicsBuffer ColorBuffer { get; private set; }
        public GraphicsBuffer SHBuffer { get; private set; }
        public GraphicsBuffer OrderBuffer { get; private set; }
        public ISorterResource SorterResource { get; private set; }
        public bool Valid =>
            PositionBuffer != null &&
            ScaleBuffer != null &&
            RotationBuffer != null &&
            ColorBuffer != null &&
            (SHBands == 0 || SHBuffer != null);

        static readonly int k_orderBuffer = Shader.PropertyToID("_OrderBuffer");
        static readonly int k_positionBuffer = Shader.PropertyToID("_PositionBuffer");
        static readonly int k_scaleBuffer = Shader.PropertyToID("_ScaleBuffer");
        static readonly int k_rotationBuffer = Shader.PropertyToID("_RotationBuffer");
        static readonly int k_colorBuffer = Shader.PropertyToID("_ColorBuffer");
        static readonly int k_shBuffer = Shader.PropertyToID("_SHBuffer");
        static readonly int k_matrixM = Shader.PropertyToID("_MATRIX_M");
        static readonly int k_splatInstanceSize = Shader.PropertyToID("_SplatInstanceSize");
        static readonly int k_splatCount = Shader.PropertyToID("_SplatCount");
        static readonly int k_gammaToLinear = Shader.PropertyToID("_GammaToLinear");
        static readonly int k_shDegree = Shader.PropertyToID("_SHDegree");
        static readonly int k_effectType = Shader.PropertyToID("_EffectType");
        static readonly int k_effectIntensity = Shader.PropertyToID("_EffectIntensity");
        static readonly int k_effectTime = Shader.PropertyToID("_EffectTime");
        static readonly int k_effectWindDir = Shader.PropertyToID("_EffectWindDir");
        static readonly int k_WaveAmplitude = Shader.PropertyToID("_WaveAmplitude");
        static readonly int k_WaveFrequency = Shader.PropertyToID("_WaveFrequency");
        static readonly int k_waveSpeed = Shader.PropertyToID("_waveSpeed");
        static readonly int k_blendScale = Shader.PropertyToID("_blendScale");
        static readonly int k_lightWaveAmplitude = Shader.PropertyToID("_lightWaveAmplitude");
        static readonly int k_lightWaveFrequency = Shader.PropertyToID("_lightWaveFrequency");
        static readonly int k_lightWaveSpeed = Shader.PropertyToID("_lightWaveSpeed");
        static readonly int k_glitterDensity = Shader.PropertyToID("_GlitterDensity");
        static readonly int k_dissolveDriftSpeed = Shader.PropertyToID("_DissolveDriftSpeed");

        static readonly int k_burnDuration = Shader.PropertyToID("_burnDuration");

        // Parameters for ---------------- Relighting --------------------------    
        public GraphicsBuffer RelightBuffer { get; private set; }
        static readonly int k_relightBuffer = Shader.PropertyToID("_RelightBuffer");
        static readonly int k_relightCount = Shader.PropertyToID("_RelightCount");
        public GsplatRendererImpl(uint splatCount, byte shBands)
        {
            SplatCount = splatCount;
            SHBands = shBands;
            CreateResources(splatCount);
            CreatePropertyBlock();
        }

        public void RecreateResources(uint splatCount, byte shBands)
        {
            if (SplatCount == splatCount && SHBands == shBands)
                return;
            Dispose();
            SplatCount = splatCount;
            SHBands = shBands;
            CreateResources(splatCount);
            CreatePropertyBlock();
        }

        void CreateResources(uint splatCount)
        {
            PositionBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, (int)splatCount,
                System.Runtime.InteropServices.Marshal.SizeOf(typeof(Vector3)));
            ScaleBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, (int)splatCount,
                System.Runtime.InteropServices.Marshal.SizeOf(typeof(Vector3)));
            RotationBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, (int)splatCount,
                System.Runtime.InteropServices.Marshal.SizeOf(typeof(Vector4)));
            ColorBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, (int)splatCount,
                System.Runtime.InteropServices.Marshal.SizeOf(typeof(Vector4)));
            if (SHBands > 0)
                SHBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured,
                    GsplatUtils.SHBandsToCoefficientCount(SHBands) * (int)splatCount,
                    System.Runtime.InteropServices.Marshal.SizeOf(typeof(Vector3)));
            OrderBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, (int)splatCount, sizeof(uint));

            SorterResource = GsplatSorter.Instance.CreateSorterResource(splatCount, PositionBuffer, OrderBuffer);
        }

        void CreatePropertyBlock()
        {
            m_propertyBlock ??= new MaterialPropertyBlock();
            m_propertyBlock.SetBuffer(k_orderBuffer, OrderBuffer);
            m_propertyBlock.SetBuffer(k_positionBuffer, PositionBuffer);
            m_propertyBlock.SetBuffer(k_scaleBuffer, ScaleBuffer);
            m_propertyBlock.SetBuffer(k_rotationBuffer, RotationBuffer);
            m_propertyBlock.SetBuffer(k_colorBuffer, ColorBuffer);
            if (SHBands > 0)
                m_propertyBlock.SetBuffer(k_shBuffer, SHBuffer);
        }

        public void Dispose()
        {
            PositionBuffer?.Dispose();
            ScaleBuffer?.Dispose();
            RotationBuffer?.Dispose();
            ColorBuffer?.Dispose();
            SHBuffer?.Dispose();
            OrderBuffer?.Dispose();
            SorterResource?.Dispose();

            RelightBuffer?.Dispose(); 

            PositionBuffer = null;
            ScaleBuffer = null;
            RotationBuffer = null;
            ColorBuffer = null;
            SHBuffer = null;
            OrderBuffer = null;
            RelightBuffer = null;
        }
        public void SetEffectParameters(int type, float intensity, float time, Vector3 windDir, float waveAmplitude,
                                        float waveFrequency, float waveSpeed, float blendScale = 1.0f,
                                        float lightWaveAmplitude = -2.0f, float lightWaveFrequency = 2.0f, float lightWaveSpeed = 2.0f,
                                        float glitterDensity = 0.3f, float dissolveDriftSpeed =0.3f, float burnDuration =2.0f)
        {
            if (m_propertyBlock == null)
                m_propertyBlock = new MaterialPropertyBlock();
            m_propertyBlock.SetInt(k_effectType, type);
            m_propertyBlock.SetFloat(k_effectIntensity, intensity);
            m_propertyBlock.SetFloat(k_effectTime, time);
            m_propertyBlock.SetVector(k_effectWindDir, windDir);
            m_propertyBlock.SetFloat(k_WaveAmplitude, waveAmplitude);
            m_propertyBlock.SetFloat(k_WaveFrequency, waveFrequency);
            m_propertyBlock.SetFloat(k_waveSpeed, waveSpeed);
            m_propertyBlock.SetFloat(k_blendScale, blendScale);
            m_propertyBlock.SetFloat(k_lightWaveAmplitude, lightWaveAmplitude);
            m_propertyBlock.SetFloat(k_lightWaveFrequency, lightWaveFrequency);
            m_propertyBlock.SetFloat(k_lightWaveSpeed, lightWaveSpeed);
            m_propertyBlock.SetFloat(k_glitterDensity, glitterDensity);
            m_propertyBlock.SetFloat(k_dissolveDriftSpeed, dissolveDriftSpeed);
            m_propertyBlock.SetFloat(k_burnDuration, burnDuration);
        }
        public void UpdateRelighting()
        {
            // Find all volume objects
            var volumes = Object.FindObjectsOfType<SplatRelightVolume>();
            int count = volumes.Length;

            if (m_propertyBlock == null) m_propertyBlock = new MaterialPropertyBlock();

            // DX12 FIX: We must ALWAYS have a buffer bound, even if it's empty.
            // If count is 0, we allocate a buffer of size 1 just to satisfy the API.
            int bufferSize = (count > 0) ? count : 1;

            // Check if we need to create or resize the buffer
            if (RelightBuffer == null || RelightBuffer.count != bufferSize)
            {
                RelightBuffer?.Dispose();
                // Stride is 112 bytes
                RelightBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, bufferSize, 112);
            }

            // If we have actual lights, update the data
            if (count > 0)
            {
                var dataArray = new RelightData[count];
                for (int i = 0; i < count; i++)
                {
                    dataArray[i] = volumes[i].GetData();
                }
                RelightBuffer.SetData(dataArray);
            }

            // Always bind the buffer and the count
            m_propertyBlock.SetBuffer(k_relightBuffer, RelightBuffer);
            m_propertyBlock.SetInt(k_relightCount, count);
        }

        /// <summary>
        /// Render the splats.
        /// </summary>
        /// <param name="splatCount">It can be less than or equal to the SplatCount property.</param> 
        /// <param name="transform">Object transform.</param>
        /// <param name="localBounds">Bounding box in object space.</param>
        /// <param name="layer">Layer used for rendering.</param>
        /// <param name="gammaToLinear">Covert color space from Gamma to Linear.</param>
        /// <param name="shDegree">Order of SH coefficients used for rendering. The final value is capped by the SHBands property.</param>
        public void Render(uint splatCount, Transform transform, Bounds localBounds, int layer,
            bool gammaToLinear = false, int shDegree = 3)
        {
            if (!Valid || !GsplatSettings.Instance.Valid || !GsplatSorter.Instance.Valid)
                return;

            m_propertyBlock.SetInteger(k_splatCount, (int)splatCount);
            m_propertyBlock.SetInteger(k_gammaToLinear, gammaToLinear ? 1 : 0);
            m_propertyBlock.SetInteger(k_splatInstanceSize, (int)GsplatSettings.Instance.SplatInstanceSize);
            m_propertyBlock.SetInteger(k_shDegree, shDegree);
            m_propertyBlock.SetMatrix(k_matrixM, transform.localToWorldMatrix);

            var rp = new RenderParams(GsplatSettings.Instance.Materials[SHBands])
            {
                worldBounds = GsplatUtils.CalcWorldBounds(localBounds, transform),
                matProps = m_propertyBlock,
                layer = layer
            };

            Graphics.RenderMeshPrimitives(rp, GsplatSettings.Instance.Mesh, 0,
                Mathf.CeilToInt(splatCount / (float)GsplatSettings.Instance.SplatInstanceSize));
        }
    }
}