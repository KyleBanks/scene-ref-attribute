#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

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
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            bool wasEnabled = GUI.enabled;
            GUI.enabled = false;
            EditorGUI.PropertyField(position, property, label, true);
            GUI.enabled = wasEnabled;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) 
            => EditorGUI.GetPropertyHeight(property, label);
    }
}
#endif