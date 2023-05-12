#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

#if UNITY_2022_2_OR_NEWER
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
            return new PropertyField(property.FindPropertyRelative(IMPLEMENTER_PROP), property.displayName);
        }
#endif

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.PropertyField(position, property.FindPropertyRelative(IMPLEMENTER_PROP), label, true);
        }
    }
}
#endif
