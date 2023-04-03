#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
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
    [CustomPropertyDrawer(typeof(SceneAttribute))]
    public class SceneRefAttributePropertyDrawer : PropertyDrawer
    {

        private bool _isInitialized;
        private bool _canValidateType;
        private Type _elementType;
        private string _typeName;

        private SceneRefAttribute _sceneRefAttribute => (SceneRefAttribute) attribute;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!this._isInitialized)
                this.Initialize(property);

            if (!this.IsSatisfied(property))
            {
                Rect helpBoxPos = position;
                helpBoxPos.height = EditorGUIUtility.singleLineHeight * 2;
                string message = $"Missing {property.propertyPath} ({this._typeName}) reference on {this._sceneRefAttribute.Loc}!";
                EditorGUI.HelpBox(helpBoxPos, message, MessageType.Error);
                position.height = EditorGUI.GetPropertyHeight(property, label);
                position.y += helpBoxPos.height;
            }

            bool wasEnabled = GUI.enabled;
            GUI.enabled = false;
            EditorGUI.PropertyField(position, property, label, true);
            GUI.enabled = wasEnabled;
        }

        private void Initialize(SerializedProperty property)
        {
            this._isInitialized = true;

            // the type won't change, so we only need to initialize these values once
            this._elementType = this.fieldInfo.FieldType;
            if (typeof(ISerializableRef).IsAssignableFrom(this._elementType))
            {
                Type interfaceType = this._elementType.GetInterfaces().FirstOrDefault(type =>
                    type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ISerializableRef<>));
                if (interfaceType != null)
                    this._elementType = interfaceType.GetGenericArguments()[0];
            }

            this._canValidateType = typeof(Component).IsAssignableFrom(this._elementType)
                                     && property.propertyType == SerializedPropertyType.ObjectReference;

            this._typeName = this.fieldInfo.FieldType.Name;
            if (this.fieldInfo.FieldType.IsGenericType && this.fieldInfo.FieldType.GenericTypeArguments.Length >= 1)
                this._typeName = this._typeName.Replace("`1", $"<{this.fieldInfo.FieldType.GenericTypeArguments[0].Name}>");
        }

        /// <summary>Is this field Satisfied with a value or optional</summary>
        private bool IsSatisfied(SerializedProperty property)
        {
            if (!this._canValidateType || this._sceneRefAttribute.HasFlags(Flag.Optional))
                return true;
            return property.objectReferenceValue != null;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float helpBoxHeight = 0;
            if (!this.IsSatisfied(property))
                helpBoxHeight = EditorGUIUtility.singleLineHeight * 2;
            return EditorGUI.GetPropertyHeight(property, label) + helpBoxHeight;
        }
    }
}
#endif
