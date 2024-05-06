#if UNITY_EDITOR

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KBCore.Refs
{
    [InitializeOnLoad]
    public static class SceneRefValidatorOnSave
    {
        private const string PrefsKey = "KBCore/ValidateRefsOnSave";
        private const string MenuItemText = "Tools/KBCore/Validate Refs on Save";
        
        public static bool ValidateRefsOnSave
        {
            get => EditorPrefs.GetBool(PrefsKey, false);
            private set => EditorPrefs.SetBool(PrefsKey, value);
        }
        
        static SceneRefValidatorOnSave()
        {
            EditorSceneManager.sceneSaving += OnSceneSaving;
        }

        [MenuItem(MenuItemText, false, 1000)]
        public static void ToggleValidateRefsOnSave()
        {
            ValidateRefsOnSave = !ValidateRefsOnSave;
            Menu.SetChecked(MenuItemText, ValidateRefsOnSave);
        }
        
        [MenuItem(MenuItemText, true)]
        public static bool ToggleValidateRefsOnSaveValidate()
        {
            Menu.SetChecked(MenuItemText, ValidateRefsOnSave);
            
            return true;
        }

        private static void OnSceneSaving(Scene scene, string path)
        {
            if (!ValidateRefsOnSave) return;
            
            SceneRefAttributeValidator.ValidateAllRefs();
        }
    }
}

#endif