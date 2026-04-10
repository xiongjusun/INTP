// Copyright (c) 2026 Dreamcore XR Labs

using UnityEditor;
using UnityEngine;
using Gsplat;

namespace Gsplat.EditorTools // 👈 your own namespace for editor code
{
    [CustomEditor(typeof(GsplatRenderer))]
    public class GsplatRendererEditor : UnityEditor.Editor 
    {
        // Serialized properties
        SerializedProperty GsplatAssetProp;
        SerializedProperty SHDegreeProp;
        SerializedProperty GammaToLinearProp;
        SerializedProperty effectTypeProp;
        SerializedProperty intensityProp;
        SerializedProperty waveAmplitudeProp;
        SerializedProperty waveFrequencyProp;
        SerializedProperty waveSpeedProp;
        SerializedProperty blendScaleProp;
        SerializedProperty lightWaveAmplitudeProp;
        SerializedProperty lightWaveFrequencyProp;
        SerializedProperty lightWaveSpeedProp;
        SerializedProperty glitterDensityProp;
        SerializedProperty dissolveDriftSpeedProp;

        SerializedProperty burnDurationProp;

        void OnEnable()
        {
            GsplatAssetProp = serializedObject.FindProperty("GsplatAsset");
            SHDegreeProp = serializedObject.FindProperty("SHDegree");
            GammaToLinearProp = serializedObject.FindProperty("GammaToLinear");
            effectTypeProp = serializedObject.FindProperty("effectType");
            intensityProp = serializedObject.FindProperty("intensity");
            waveAmplitudeProp = serializedObject.FindProperty("waveAmplitude");
            waveFrequencyProp = serializedObject.FindProperty("waveFrequency");
            waveSpeedProp = serializedObject.FindProperty("waveSpeed");
            blendScaleProp = serializedObject.FindProperty("blendScale");
            lightWaveAmplitudeProp = serializedObject.FindProperty("lightWaveAmplitude");
            lightWaveFrequencyProp = serializedObject.FindProperty("lightWaveFrequency");
            lightWaveSpeedProp = serializedObject.FindProperty("lightWaveSpeed");
            glitterDensityProp = serializedObject.FindProperty("glitterDensity");
            burnDurationProp = serializedObject.FindProperty("burnDuration");

        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            GsplatRenderer renderer = (GsplatRenderer)target;

            // === Base settings ===
            EditorGUILayout.PropertyField(GsplatAssetProp);
            EditorGUILayout.PropertyField(SHDegreeProp);
            EditorGUILayout.PropertyField(GammaToLinearProp);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Effect Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(effectTypeProp);

            // === Conditionally show intensity ===
            GsplatEffectType currentType = (GsplatEffectType)effectTypeProp.enumValueIndex;
            if (currentType == GsplatEffectType.DeepMeditation ||
                currentType == GsplatEffectType.Waves ||
                currentType == GsplatEffectType.Disintegrate ||
                currentType == GsplatEffectType.Wind ||
                currentType == GsplatEffectType.PerlinWave)
            {
                EditorGUILayout.PropertyField(intensityProp, new GUIContent("Intensity", "Controls overall effect strength."));
            }

            // === Show wave and perlinWave parameters ===
            if (currentType == GsplatEffectType.Waves || currentType == GsplatEffectType.PerlinWave)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Light Wave Parameters", EditorStyles.boldLabel);

                EditorGUILayout.PropertyField(lightWaveAmplitudeProp);
                EditorGUILayout.PropertyField(lightWaveFrequencyProp);
                EditorGUILayout.PropertyField(lightWaveSpeedProp);
            }
            // === PerlinWave special controls ===
            if (currentType == GsplatEffectType.PerlinWave)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Perlin Wave Parameters", EditorStyles.boldLabel);

                EditorGUILayout.PropertyField(waveAmplitudeProp);
                EditorGUILayout.PropertyField(waveFrequencyProp);
                EditorGUILayout.PropertyField(waveSpeedProp);
                EditorGUILayout.PropertyField(blendScaleProp);

                EditorGUILayout.Space();

                if (GUILayout.Button("Blend Appear"))
                    renderer.StartBlendAppear();

                if (GUILayout.Button("Blend Disappear"))
                    renderer.StartBlendDisappear();

                if (GUILayout.Button("Shock Wave Animation"))
                    renderer.StartShockWaveEffect();
            }

            if(currentType == GsplatEffectType.Glitter)
            {
                EditorGUILayout.PropertyField(glitterDensityProp, new GUIContent("Glitter Density", "Controls overall glittering particle density."));
            }
            if(currentType == GsplatEffectType.GalaxyGlitter)
            {
                EditorGUILayout.PropertyField(glitterDensityProp, new GUIContent("Glitter Density", "Controls overall glittering particle density."));
            }
            if(currentType == GsplatEffectType.FlyDissolve)
            {
                EditorGUILayout.PropertyField(glitterDensityProp, new GUIContent("Glitter Density", "Controls overall glittering particle density."));
            }
            if(currentType == GsplatEffectType.GlowDissolve)
            {
                EditorGUILayout.PropertyField(intensityProp, new GUIContent("Intensity", "Controls overall effect strength."));
                EditorGUILayout.PropertyField(burnDurationProp, new GUIContent("burnDuration", "Controls overall over how long the object burns and fade away."));
            }
            if(currentType == GsplatEffectType.RadialExpansion)
            {
                EditorGUILayout.PropertyField(intensityProp, new GUIContent("Intensity", "Controls overall effect strength (how much the particle expands)."));
            }
            // === Always show Reset Except for None ===
            EditorGUILayout.Space();

            GUIContent resetButtonContent = new GUIContent(
                "Reset Effect Time",
                "Resets the animation timer (baseTime) to 0.\nUse this if an effect seems out of sync."
            );

            // Disable button if effectType == None
            using (new EditorGUI.DisabledScope(currentType == GsplatEffectType.None || currentType == GsplatEffectType.RadialExpansion))
            {
                if (GUILayout.Button(resetButtonContent))
                    renderer.resetAnimationTime();
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}