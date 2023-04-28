#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using System.Linq;
#if UNITY_2022_2_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

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
        private bool _editable => this._sceneRefAttribute.HasFlags(Flag.Editable);

// unity 2022.2 makes UIToolkit the default for inspectors
#if UNITY_2022_2_OR_NEWER
        private const string SCENE_REF_CLASS = "kbcore-refs-sceneref";

        private PropertyField _propertyField;
        private HelpBox _helpBox;
        private InspectorElement _inspectorElement;
        private SerializedProperty _serializedProperty;

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            this._serializedProperty = property;
            this.Initialize(property);

            VisualElement root = new();
            root.AddToClassList(SCENE_REF_CLASS);

            this._helpBox = new HelpBox("", HelpBoxMessageType.Error);
            this._helpBox.style.display = DisplayStyle.None;
            root.Add(this._helpBox);

            this._propertyField = new PropertyField(property);
            this._propertyField.SetEnabled(this._editable);
            root.Add(this._propertyField);

            if (this._canValidateType)
            {
                this.UpdateHelpBox();
                this._propertyField.RegisterCallback<AttachToPanelEvent>(this.OnAttach);
            }
            return root;
        }

        private void OnAttach(AttachToPanelEvent attachToPanelEvent)
        {
            this._propertyField.UnregisterCallback<AttachToPanelEvent>(this.OnAttach);
            this._inspectorElement = this._propertyField.GetFirstAncestorOfType<InspectorElement>();
            if (this._inspectorElement == null)
                // not in an inspector, invalid
                return;

            // subscribe to SerializedPropertyChangeEvent so we can update when the property changes
            this._inspectorElement.RegisterCallback<SerializedPropertyChangeEvent>(this.OnSerializedPropertyChangeEvent);
            this._propertyField.RegisterCallback<DetachFromPanelEvent>(this.OnDetach);
        }

        private void OnDetach(DetachFromPanelEvent detachFromPanelEvent)
        {
            // unregister from all callbacks
            this._propertyField.UnregisterCallback<DetachFromPanelEvent>(this.OnDetach);
            this._inspectorElement.UnregisterCallback<SerializedPropertyChangeEvent>(this.OnSerializedPropertyChangeEvent);
            this._serializedProperty = null;
        }

        private void OnSerializedPropertyChangeEvent(SerializedPropertyChangeEvent changeEvent)
        {
            if (changeEvent.changedProperty != this._serializedProperty)
                return;
            this.UpdateHelpBox();
        }

        private void UpdateHelpBox()
        {
            bool isSatisfied = this.IsSatisfied(this._serializedProperty);
            this._helpBox.style.display = isSatisfied ? DisplayStyle.None : DisplayStyle.Flex;
            string message = $"Missing {this._serializedProperty.propertyPath} ({this._typeName}) reference on {this._sceneRefAttribute.Loc}!";
            this._helpBox.text = message;
        }
#endif

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
            GUI.enabled = this._editable;
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
