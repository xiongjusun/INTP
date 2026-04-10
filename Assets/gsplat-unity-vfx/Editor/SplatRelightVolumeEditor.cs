//Copyright © 2026 Dreamcore XR Labs
using UnityEngine;
using UnityEditor;

namespace Gsplat
{
    [CustomEditor(typeof(SplatRelightVolume))]
    [CanEditMultipleObjects]
    public class SplatRelightVolumeEditor : UnityEditor.Editor
    {
        SerializedProperty shape;
        SerializedProperty blendMode;
        SerializedProperty color;
        SerializedProperty intensity;
        SerializedProperty softEdge;
        
        // Shape specific params
        SerializedProperty radius;
        SerializedProperty boxSize;
        SerializedProperty height;
        SerializedProperty torusThickness;

        void OnEnable()
        {
            shape = serializedObject.FindProperty("shape");
            blendMode = serializedObject.FindProperty("blendMode");
            color = serializedObject.FindProperty("color");
            intensity = serializedObject.FindProperty("intensity");
            softEdge = serializedObject.FindProperty("softEdge");
            
            radius = serializedObject.FindProperty("radius");
            boxSize = serializedObject.FindProperty("boxSize");
            height = serializedObject.FindProperty("height");
            torusThickness = serializedObject.FindProperty("torusThickness");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            RelightShape currentShape = (RelightShape)shape.enumValueIndex;

            EditorGUILayout.LabelField("General Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(shape);
            EditorGUILayout.PropertyField(blendMode);
            
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Appearance", EditorStyles.boldLabel);

            if (currentShape != RelightShape.RainbowStrip)
            {
                EditorGUILayout.PropertyField(color);
            }

            EditorGUILayout.PropertyField(intensity);
            EditorGUILayout.PropertyField(softEdge);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Shape Dimensions", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            

            switch (currentShape)
            {
                case RelightShape.Sphere:
                    EditorGUILayout.PropertyField(radius);
                    break;

                case RelightShape.Box:
                    EditorGUILayout.PropertyField(boxSize);
                    break;

                case RelightShape.Cylinder:
                    EditorGUILayout.PropertyField(radius);
                    EditorGUILayout.PropertyField(height);
                    break;

                case RelightShape.Capsule:
                    EditorGUILayout.PropertyField(radius);
                    EditorGUILayout.PropertyField(height);
                    break;

                case RelightShape.Torus:
                    EditorGUILayout.PropertyField(radius, new GUIContent("Main Radius"));
                    EditorGUILayout.PropertyField(torusThickness, new GUIContent("Thickness"));
                    break;

                case RelightShape.Plane:
                    EditorGUILayout.HelpBox("The Plane is infinite. Rotate the Transform to orient the floor/ceiling.", MessageType.Info);
                    break;
                    
                case RelightShape.Heart:
                    EditorGUILayout.PropertyField(radius, new GUIContent("Size"));
                    EditorGUILayout.PropertyField(height, new GUIContent("Depth Thickness"));
                    break;

                case RelightShape.RainbowStrip:
                    EditorGUILayout.PropertyField(boxSize, new GUIContent("Strip Dimensions"));
                    EditorGUILayout.HelpBox("Rainbow mode overrides the single Color selection.", MessageType.None);
                    EditorGUILayout.HelpBox("The Rainbow gradient flows along the Local X Axis (Red axis).", MessageType.Info);
                    break;

            }

            EditorGUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();
        }
    }
}