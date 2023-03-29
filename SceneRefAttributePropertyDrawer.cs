#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace KBCore.Refs
{
    /// <summary>
    /// Custom property drawer for the reference attributes, making them read-only.
    /// 
    /// Note: Does not apply to the Anywhere attribute as that needs to remain editable. 
    /// </summary>
    [CustomPropertyDrawer(typeof(SelfAttribute))]
    [CustomPropertyDrawer(typeof(ChildAttribute))]
    [CustomPropertyDrawer(typeof(ParentAttribute))]
    public class SceneRefAttributePropertyDrawer : PropertyDrawer
    {

        bool isInitialized = false;
        bool isValidFieldType;
        Type elementType;
        string typeName;

        SceneRefAttribute sceneRefAttribute => (SceneRefAttribute)attribute;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!isInitialized)
            {
                Initialize(property);
            }

            if (isValidFieldType)
            {
                bool isSatisfied = IsSatisfied(property);
                if (!isSatisfied)
                {
                    Rect helpBoxPos = position;
                    helpBoxPos.height = EditorGUIUtility.singleLineHeight * 2;
                    string message = $"{property.propertyPath} missing {typeName} reference on {sceneRefAttribute.Loc}!";
                    EditorGUI.HelpBox(helpBoxPos, message, MessageType.Error);
                    position.height = EditorGUI.GetPropertyHeight(property, label);
                    position.y += helpBoxPos.height;
                }
            }

            bool wasEnabled = GUI.enabled;
            GUI.enabled = false;
            EditorGUI.PropertyField(position, property, label, true);
            GUI.enabled = wasEnabled;
        }

        void Initialize(SerializedProperty property)
        {
            isInitialized = true;

            // the type wont change, so we only need to initialize these values once

            elementType = fieldInfo.FieldType;
            if (typeof(ISerializableRef).IsAssignableFrom(elementType))
            {
                var interfaceType = elementType.GetInterfaces().FirstOrDefault(type =>
                    type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ISerializableRef<>));
                if (interfaceType != null)
                {
                    elementType = interfaceType.GetGenericArguments()[0];
                }
            }

            isValidFieldType = typeof(Component).IsAssignableFrom(elementType)
                && (property.propertyType == SerializedPropertyType.ObjectReference);

            typeName = fieldInfo.FieldType.Name;
            if (fieldInfo.FieldType.IsGenericType && fieldInfo.FieldType.GenericTypeArguments.Length >= 1)
            {
                typeName = typeName.Replace("`1", $"<{fieldInfo.FieldType.GenericTypeArguments[0].Name}>");
            }
        }

        /// <summary>Is this field Satisfied with a value or optional</summary>
        private bool IsSatisfied(SerializedProperty property)
        {
            if (sceneRefAttribute.HasFlags(Flag.Optional))
            {
                return true;
            }
            return CheckValue(property);
        }

        private bool CheckValue(SerializedProperty property)
        {
            bool hasValue = false;
            if (property.isArray)
            {
                //property.isArray includes lists and arrays
                hasValue = property.arraySize > 0;
                for (int i = 0; i < property.arraySize; i++)
                {
                    // all array elements must have a value
                    hasValue &= property.GetArrayElementAtIndex(i).objectReferenceValue != null;
                }
            } else if (property.propertyType == SerializedPropertyType.ObjectReference)
            {
                hasValue = property.objectReferenceValue != null;
            }
            return hasValue;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float helpboxHeight = 0;
            if (!IsSatisfied(property))
            {
                helpboxHeight = EditorGUIUtility.singleLineHeight * 2;
            }
            return EditorGUI.GetPropertyHeight(property, label) + helpboxHeight;
        }
    }
}
#endif