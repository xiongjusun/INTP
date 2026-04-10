// Copyright (c) 2026 Dreamcore XR Labs, adapted and revised from gsplat-unity

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Gsplat.Editor
{
    [ScriptedImporter(1, "ply")]
    public class GsplatImporter : ScriptedImporter
    {
        // Helper to map properties to byte offsets
        class PlyProperty
        {
            public string Name;
            public int Offset;
            public int Size;
        }

        static int GetTypeSize(string type)
        {
            switch (type)
            {
                case "char": case "uchar": case "int8": case "uint8": return 1;
                case "short": case "ushort": case "int16": case "uint16": return 2;
                case "int": case "uint": case "int32": case "uint32": case "float": case "float32": return 4;
                case "double": case "float64": return 8;
                default: return 0;
            }
        }

        public override void OnImportAsset(AssetImportContext ctx)
        {
            GsplatAsset gsplatAsset = ScriptableObject.CreateInstance<GsplatAsset>();
            Bounds bounds = new Bounds();

            FileStream fs = null;
            try
            {
                fs = new FileStream(ctx.assetPath, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024); // 64KB buffer

                List<PlyProperty> properties = new List<PlyProperty>();
                uint vertexCount = 0;
                int vertexStride = 0;
                
                StringBuilder lineBuilder = new StringBuilder();
                bool readingHeader = true;
                bool readingVertexElement = false;
                int currentOffset = 0;

                while (readingHeader && fs.Position < fs.Length)
                {
                    int b = fs.ReadByte();
                    if (b == -1) break;

                    char c = (char)b;
                    if (c == '\n')
                    {
                        string line = lineBuilder.ToString().Trim();
                        lineBuilder.Clear();

                        if (line == "end_header")
                        {
                            readingHeader = false;
                            break;
                        }

                        string[] parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 2)
                        {
                            if (parts[0] == "element")
                            {
                                if (parts[1] == "vertex")
                                {
                                    vertexCount = uint.Parse(parts[2]);
                                    readingVertexElement = true;
                                }
                                else
                                {
                                    readingVertexElement = false;
                                }
                            }
                            else if (parts[0] == "property" && readingVertexElement)
                            {
                                string type = parts[1];
                                string name = parts[parts.Length - 1];
                                int size = GetTypeSize(type);
                                
                                properties.Add(new PlyProperty { Name = name, Offset = currentOffset, Size = size });
                                currentOffset += size;
                            }
                        }
                    }
                    else
                    {
                        lineBuilder.Append(c);
                    }
                }
                vertexStride = currentOffset;

                if (vertexCount == 0 || vertexStride == 0)
                {
                    Debug.LogError($"[GsplatImporter] Invalid PLY header in {ctx.assetPath}");
                    return;
                }

                long expectedDataBytes = (long)vertexCount * vertexStride;
                long remainingFileBytes = fs.Length - fs.Position;
                
                if (remainingFileBytes < expectedDataBytes)
                {
                    Debug.LogError($"[GsplatImporter] File truncated. Expected {expectedDataBytes} bytes of data, found {remainingFileBytes}.");
                    return;
                }

                gsplatAsset.Positions = new Vector3[vertexCount];
                gsplatAsset.Colors = new Vector4[vertexCount];
                gsplatAsset.Scales = new Vector3[vertexCount];
                gsplatAsset.Rotations = new Vector4[vertexCount];

                int GetOff(string name) => properties.FirstOrDefault(p => p.Name == name)?.Offset ?? -1;

                int offX = GetOff("x");
                int offY = GetOff("y");
                int offZ = GetOff("z");

                int offScale0 = GetOff("scale_0");
                int offScale1 = GetOff("scale_1");
                int offScale2 = GetOff("scale_2");

                int offRot0 = GetOff("rot_0");
                int offRot1 = GetOff("rot_1");
                int offRot2 = GetOff("rot_2");
                int offRot3 = GetOff("rot_3");

                int offOpac = GetOff("opacity");
                int offDc0 = GetOff("f_dc_0");
                int offDc1 = GetOff("f_dc_1");
                int offDc2 = GetOff("f_dc_2");

                var shProps = properties
                    .Where(p => p.Name.StartsWith("f_rest_") || p.Name.StartsWith("sh_"))
                    .OrderBy(p => {
                        string digits = new string(p.Name.Where(char.IsDigit).ToArray());
                        return int.TryParse(digits, out int n) ? n : 9999;
                    }).ToList();

                int shCount = shProps.Count;
                bool shDiv3 = shCount % 3 == 0;
                int shVecCount = shDiv3 ? shCount / 3 : shCount;
                
                if (shCount > 0) 
                    gsplatAsset.SHs = new Vector3[vertexCount * (shDiv3 ? shVecCount : 1)];

                gsplatAsset.SplatCount = vertexCount;
                gsplatAsset.SHBands = GsplatUtils.CalcSHBandsFromCoefficientCount(shCount);


                int batchSize = 4096; 
                byte[] buffer = new byte[batchSize * vertexStride];
                
                int verticesRead = 0;
                while (verticesRead < vertexCount)
                {
                    int toRead = Mathf.Min(batchSize, (int)vertexCount - verticesRead);
                    int bytesToRead = toRead * vertexStride;
                    
                    int bytesRead = fs.Read(buffer, 0, bytesToRead);
                    if (bytesRead < bytesToRead) break; 

                    for (int k = 0; k < toRead; k++)
                    {
                        int globalIndex = verticesRead + k;
                        int offset = k * vertexStride; 

                        float px = BitConverter.ToSingle(buffer, offset + offX);
                        float py = BitConverter.ToSingle(buffer, offset + offY);
                        float pz = BitConverter.ToSingle(buffer, offset + offZ);
                        gsplatAsset.Positions[globalIndex] = new Vector3(px, py, pz);

                        if (globalIndex == 0) bounds = new Bounds(gsplatAsset.Positions[globalIndex], Vector3.zero);
                        else bounds.Encapsulate(gsplatAsset.Positions[globalIndex]);

                        if (offScale0 != -1)
                        {
                            float sx = Mathf.Exp(BitConverter.ToSingle(buffer, offset + offScale0));
                            float sy = Mathf.Exp(BitConverter.ToSingle(buffer, offset + offScale1));
                            float sz = Mathf.Exp(BitConverter.ToSingle(buffer, offset + offScale2));
                            gsplatAsset.Scales[globalIndex] = new Vector3(sx, sy, sz);
                        }

                        if (offRot0 != -1)
                        {
                            float r0 = BitConverter.ToSingle(buffer, offset + offRot0);
                            float r1 = BitConverter.ToSingle(buffer, offset + offRot1);
                            float r2 = BitConverter.ToSingle(buffer, offset + offRot2);
                            float r3 = BitConverter.ToSingle(buffer, offset + offRot3);
                            gsplatAsset.Rotations[globalIndex] = new Vector4(r0, r1, r2, r3).normalized;
                        }

                        float r = (offDc0 != -1) ? BitConverter.ToSingle(buffer, offset + offDc0) : 0;
                        float g = (offDc1 != -1) ? BitConverter.ToSingle(buffer, offset + offDc1) : 0;
                        float b = (offDc2 != -1) ? BitConverter.ToSingle(buffer, offset + offDc2) : 0;
                        float a = (offOpac != -1) ? GsplatUtils.Sigmoid(BitConverter.ToSingle(buffer, offset + offOpac)) : 1;
                        gsplatAsset.Colors[globalIndex] = new Vector4(r, g, b, a);

                        if (shCount > 0)
                        {
                            if (shDiv3)
                            {
                                for (int j = 0; j < shVecCount; j++)
                                {
                                    float shr = BitConverter.ToSingle(buffer, offset + shProps[j * 3].Offset);
                                    float shg = BitConverter.ToSingle(buffer, offset + shProps[j * 3 + 1].Offset);
                                    float shb = BitConverter.ToSingle(buffer, offset + shProps[j * 3 + 2].Offset);
                                    gsplatAsset.SHs[globalIndex * shVecCount + j] = new Vector3(shr, shg, shb);
                                }
                            }
                            else
                            {
                                for (int j = 0; j < shCount; j++)
                                {
                                    float v = BitConverter.ToSingle(buffer, offset + shProps[j].Offset);
                                    gsplatAsset.SHs[globalIndex] = new Vector3(v, 0, 0);
                                }
                            }
                        }
                    }

                    verticesRead += toRead;
                    if (verticesRead % 50000 == 0)
                        EditorUtility.DisplayProgressBar("Importing Splats", $"Read {verticesRead}/{vertexCount}", (float)verticesRead / vertexCount);
                }

                gsplatAsset.Bounds = bounds;
                ctx.AddObjectToAsset("gsplat", gsplatAsset);
                ctx.SetMainObject(gsplatAsset);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GsplatImporter] Error: {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                if (fs != null) fs.Dispose();
                EditorUtility.ClearProgressBar();
            }
        }
    }
}