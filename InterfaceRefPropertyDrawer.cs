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
    [CustomPropertyDrawer(typeof(InterfaceRef<>))]
    public class InterfaceRefPropertyDrawer : PropertyDrawer
    {
// unity 2022.2 makes UIToolkit the default for inspectors
#if UNITY_2022_2_OR_NEWER
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            return new PropertyField(property.FindPropertyRelative("_implementer"), property.displayName);
        }
#endif

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.PropertyField(position, property.FindPropertyRelative("_implementer"), label, true);
        }
    }
}
#endif
