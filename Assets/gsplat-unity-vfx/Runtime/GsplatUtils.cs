// Copyright (c) 2026 Dreamcore XR Labs, adapted and revised from gsplat-unity

using System;
using UnityEngine;

namespace Gsplat
{
    public static class GsplatUtils
    {
        public const string k_PackagePath = "Packages/coco.linux.3dgsvfx/";

        /// <summary>
        /// Sigmoid function for opacity normalization
        /// </summary>
        public static float Sigmoid(float x)
        {
            return 1.0f / (1.0f + Mathf.Exp(-x));
        }

        /// <summary>
        /// Calculate SH bands from a given number of SH coefficients.
        /// </summary>
        /// <param name="coeffCount">Number of SH coefficients (e.g., from f_rest_* properties)</param>
        /// <returns>SH bands (0,1,2,...)</returns>
        public static byte CalcSHBandsFromCoefficientCount(int coeffCount)
        {
            for (byte bands = 0; bands <= 5; bands++)
            {
                if ((bands + 1) * (bands + 1) - 1 == coeffCount)
                    return bands;
            }
            return 0; // No SH
        }

        /// <summary>
        /// Converts SH bands to the total number of SH coefficients
        /// </summary>
        public static int SHBandsToCoefficientCount(byte shBands)
        {
            return (shBands + 1) * (shBands + 1) - 1;
        }

        /// <summary>
        /// Calculate world-space bounds from local bounds and a transform
        /// </summary>
        public static Bounds CalcWorldBounds(Bounds localBounds, Transform transform)
        {
            var localCenter = localBounds.center;
            var localExtents = localBounds.extents;

            var localCorners = new[]
            {
                localCenter + new Vector3(localExtents.x, localExtents.y, localExtents.z),
                localCenter + new Vector3(localExtents.x, localExtents.y, -localExtents.z),
                localCenter + new Vector3(localExtents.x, -localExtents.y, localExtents.z),
                localCenter + new Vector3(localExtents.x, -localExtents.y, -localExtents.z),
                localCenter + new Vector3(-localExtents.x, localExtents.y, localExtents.z),
                localCenter + new Vector3(-localExtents.x, localExtents.y, -localExtents.z),
                localCenter + new Vector3(-localExtents.x, -localExtents.y, localExtents.z),
                localCenter + new Vector3(-localExtents.x, -localExtents.y, -localExtents.z)
            };

            var worldBounds = new Bounds(transform.TransformPoint(localCorners[0]), Vector3.zero);
            for (var i = 1; i < 8; i++)
                worldBounds.Encapsulate(transform.TransformPoint(localCorners[i]));

            return worldBounds;
        }
    }
}
