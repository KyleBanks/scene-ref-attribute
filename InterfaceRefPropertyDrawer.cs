#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

#if UNITY_2022_2_OR_NEWER
using System;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace KBCore.Refs
{
    [CustomPropertyDrawer(typeof(InterfaceRef<>))]
    public class InterfaceRefPropertyDrawer : PropertyDrawer
    {
        private const string IMPLEMENTER_PROP = "_implementer";

// unity 2022.2 makes UIToolkit the default for inspectors
#if UNITY_2022_2_OR_NEWER
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var field = new PropertyField(property.FindPropertyRelative(IMPLEMENTER_PROP), property.displayName);
            field.RegisterCallback<DragPerformEvent, SerializedProperty>(DragPerformEvent, property,
                TrickleDown.TrickleDown);
            field.RegisterCallback<DragUpdatedEvent, SerializedProperty>(DragUpdatedEvent, property,
                TrickleDown.TrickleDown);

            return field;
        }

        private void DragUpdatedEvent(DragUpdatedEvent evt, SerializedProperty serializedProperty)
        {
            evt.StopImmediatePropagation();
            if (!TryGetDraggedComponent(serializedProperty, out _))
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                return;
            }

            DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
        }

        private void DragPerformEvent(DragPerformEvent evt, SerializedProperty serializedProperty)
        {
            if (!TryGetDraggedComponent(serializedProperty, out var component))
                return;

            DragAndDrop.AcceptDrag();

            serializedProperty.FindPropertyRelative(IMPLEMENTER_PROP).objectReferenceValue = component;
            serializedProperty.serializedObject.ApplyModifiedProperties();

            evt.StopImmediatePropagation();
        }
        
#endif

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            EditorGUI.ObjectField(position, property.FindPropertyRelative(IMPLEMENTER_PROP), label);
            HandleIMGUIObjectDrag(position, property);

            EditorGUI.EndProperty();
        }

        private void HandleIMGUIObjectDrag(Rect position, SerializedProperty property)
        {
            var evt = Event.current;
            
            if ((evt.type != EventType.DragUpdated && evt.type != EventType.DragPerform) || !position.Contains(evt.mousePosition))
                return;
            
            evt.Use();
            
            if (!TryGetDraggedComponent(property, out var component))
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                return;
            }

            DragAndDrop.visualMode = DragAndDropVisualMode.Generic;

            if (evt.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                
                property.FindPropertyRelative(IMPLEMENTER_PROP).objectReferenceValue = component;
                property.serializedObject.ApplyModifiedProperties();

                GUI.changed = true;
            }

            evt.Use();
        }
        
        private static bool TryGetDraggedComponent(SerializedProperty property, out Component component)
        {
            component = null;

            if (DragAndDrop.objectReferences.Length != 1)
                return false;

            var targetType = ((ISerializableRef)property.boxedValue).RefType;
            Object checkedObject = DragAndDrop.objectReferences[0];

            if (checkedObject is Component draggedComponent && targetType.IsAssignableFrom(draggedComponent.GetType()))
            {
                component = draggedComponent;
                return true;
            }

            if (checkedObject is GameObject go && go.TryGetComponent(targetType, out component))
                return true;

            return false;
        }
    }
}
#endif
