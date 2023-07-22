using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

#if UNITY_EDITOR

using UnityEditor;

#endif

namespace KBCore.Refs
{
    public static class SceneRefAttributeValidator
    {
        private static readonly List<ReflectionUtil.AttributedField<SceneRefAttribute>> ATTRIBUTED_FIELDS_CACHE = new List<ReflectionUtil.AttributedField<SceneRefAttribute>>();

#if UNITY_EDITOR

        /// <summary>
        /// Validate all references for every script and every game object in the scene.
        /// </summary>
        [MenuItem("Tools/KBCore/Validate All Refs")]
        private static void ValidateAllRefs()
        {
            MonoScript[] scripts = MonoImporter.GetAllRuntimeMonoScripts();
            for (int i = 0; i < scripts.Length; i++)
            {
                MonoScript runtimeMonoScript = scripts[i];
                Type scriptType = runtimeMonoScript.GetClass();
                if (scriptType == null)
                    continue;

                try
                {
                    ReflectionUtil.GetFieldsWithAttributeFromType(
                        scriptType,
                        ATTRIBUTED_FIELDS_CACHE,
                        BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance
                    );
                    if (ATTRIBUTED_FIELDS_CACHE.Count == 0)
                        continue;

                    Object[] objects = Object.FindObjectsOfType(scriptType, true);
                    if (objects.Length == 0)
                        continue;

                    Debug.Log($"Validating {ATTRIBUTED_FIELDS_CACHE.Count} field(s) on {objects.Length} {objects[0].GetType().Name} instance(s)");
                    for (int o = 0; o < objects.Length; o++)
                        Validate(objects[o] as MonoBehaviour, ATTRIBUTED_FIELDS_CACHE, false);
                }
                finally
                {
                    ATTRIBUTED_FIELDS_CACHE.Clear();
                }
            }
        }

        /// <summary>
        /// Validate a single components references, attempting to assign missing references
        /// and logging errors as necessary.
        /// </summary>
        [MenuItem("CONTEXT/Component/Validate Refs")]
        private static void ValidateRefs(MenuCommand menuCommand)
            => Validate(menuCommand.context as Component);

        /// <summary>
        /// Clean and validate a single components references. Useful in instances where (for example) Unity has
        /// incorrectly serialized a scene reference within a prefab.
        /// </summary>
        [MenuItem("CONTEXT/Component/Clean and Validate Refs")]
        private static void CleanValidateRefs(MenuCommand menuCommand)
            => CleanValidate(menuCommand.context as Component);

#endif

        /// <summary>
        /// Validate a single components references, attempting to assign missing references
        /// and logging errors as necessary.
        /// </summary>
        public static void ValidateRefs(this Component c, bool updateAtRuntime = false)
            => Validate(c, updateAtRuntime);

        /// <summary>
        /// Validate a single components references, attempting to assign missing references
        /// and logging errors as necessary.
        /// </summary>
        public static void Validate(Component c, bool updateAtRuntime = false)
        {
            try
            {
                ReflectionUtil.GetFieldsWithAttributeFromType(
                    c.GetType(),
                    ATTRIBUTED_FIELDS_CACHE,
                    BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance
                );
                Validate(c, ATTRIBUTED_FIELDS_CACHE, updateAtRuntime);
            }
            finally
            {
                ATTRIBUTED_FIELDS_CACHE.Clear();
            }
        }

        /// <summary>
        /// Clean and validate a single components references. Useful in instances where (for example) Unity has
        /// incorrectly serialized a scene reference within a prefab.
        /// </summary>
        public static void CleanValidate(Component c, bool updateAtRuntime = false)
        {
            try
            {
                ReflectionUtil.GetFieldsWithAttributeFromType(
                    c.GetType(),
                    ATTRIBUTED_FIELDS_CACHE,
                    BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance
                );
                Clean(c, ATTRIBUTED_FIELDS_CACHE);
                Validate(c, ATTRIBUTED_FIELDS_CACHE, updateAtRuntime);
            }
            finally
            {
                ATTRIBUTED_FIELDS_CACHE.Clear();
            }
        }

        private static void Validate(
            Component c,
            List<ReflectionUtil.AttributedField<SceneRefAttribute>> requiredFields,
            bool updateAtRuntime
        )
        {
            if (requiredFields.Count == 0)
            {
                Debug.LogWarning($"{c.GetType().Name} has no required fields", c.gameObject);
                return;
            }

            bool isUninstantiatedPrefab = PrefabUtil.IsUninstantiatedPrefab(c.gameObject);
            for (int i = 0; i < requiredFields.Count; i++)
            {
                ReflectionUtil.AttributedField<SceneRefAttribute> attributedField = requiredFields[i];
                SceneRefAttribute attribute = attributedField.Attribute;
                FieldInfo field = attributedField.FieldInfo;

                if (field.FieldType.IsInterface)
                    throw new Exception($"{c.GetType().Name} cannot serialize interface {field.Name} directly, use InterfaceRef instead");

                object fieldValue = field.GetValue(c);
                if (updateAtRuntime || !Application.isPlaying)
                    fieldValue = UpdateRef(attribute, c, field, fieldValue);

                if (isUninstantiatedPrefab)
                    continue;

                ValidateRef(attribute, c, field, fieldValue);
            }
        }

        private static void Clean(
            Component c,
            List<ReflectionUtil.AttributedField<SceneRefAttribute>> requiredFields
        )
        {
            for (int i = 0; i < requiredFields.Count; i++)
            {
                ReflectionUtil.AttributedField<SceneRefAttribute> attributedField = requiredFields[i];
                SceneRefAttribute attribute = attributedField.Attribute;
                if (attribute.Loc == RefLoc.Anywhere)
                    continue;

                FieldInfo field = attributedField.FieldInfo;
                field.SetValue(c, null);
#if UNITY_EDITOR
                EditorUtility.SetDirty(c);
#endif
            }
        }

        private static object UpdateRef(SceneRefAttribute attr, Component c, FieldInfo field, object existingValue)
        {
            Type fieldType = field.FieldType;
            bool isArray = fieldType.IsArray;
            bool excludeSelf = attr.HasFlags(Flag.ExcludeSelf);
            bool includeInactive = attr.HasFlags(Flag.IncludeInactive);

            ISerializableRef iSerializable = null;
            if (typeof(ISerializableRef).IsAssignableFrom(fieldType))
            {
                iSerializable = (ISerializableRef)(existingValue ?? Activator.CreateInstance(fieldType));
                fieldType = iSerializable.RefType;
                existingValue = iSerializable.SerializedObject;
            }

            if (attr.HasFlags(Flag.Editable))
            {
                bool isFilledArray = isArray && (existingValue as Object[])?.Length > 0;
                if (isFilledArray || existingValue as Object != null)
                {
                    // If the field is editable and the value has already been set, keep it.
                    return existingValue;
                }
            }

            Type elementType = fieldType;
            if (isArray)
            {
                elementType = fieldType.GetElementType();
                if (typeof(ISerializableRef).IsAssignableFrom(elementType))
                {
                    Type interfaceType = elementType?.GetInterfaces().FirstOrDefault(type =>
                        type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ISerializableRef<>));
                    if (interfaceType != null)
                        elementType = interfaceType.GetGenericArguments()[0];
                }
            }

            object value = null;
            switch (attr.Loc)
            {
                case RefLoc.Anywhere:
                    if (isArray ? typeof(ISerializableRef).IsAssignableFrom(fieldType.GetElementType()) : iSerializable != null)
                        value = isArray
                            ? (existingValue as ISerializableRef[])?.Select(existingRef => GetComponentIfWrongType(existingRef.SerializedObject, elementType)).ToArray()
                            : GetComponentIfWrongType(existingValue, elementType);
                    break;

                case RefLoc.Self:
                    value = isArray
                        ? c.GetComponents(elementType)
                        : c.GetComponent(elementType);
                    break;

                case RefLoc.Parent:
                    bool includeInactiveWhenSingle = includeInactive;
#if UNITY_2020
                    includeInactiveWhenSingle = false;
#endif
                    value = isArray
                        ? GetComponentsInParent(c, elementType, includeInactive, excludeSelf)
                        : GetComponentInParent(c, elementType, includeInactiveWhenSingle, excludeSelf);
                    break;

                case RefLoc.Child:
                    value = isArray
                        ? GetComponentsInChildren(c, elementType, includeInactive, excludeSelf)
                        : GetComponentInChildren(c, elementType, includeInactive, excludeSelf);
                    break;

                case RefLoc.Scene:
#if UNITY_2020
                    value = isArray
                        ? (object)Object.FindObjectsOfType(elementType, includeInactive)
                        : (object)Object.FindObjectOfType(elementType, includeInactive);
#else
                    FindObjectsInactive includeInactiveObjects = includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude;
                    value = isArray
                        ? Object.FindObjectsByType(elementType, includeInactiveObjects, FindObjectsSortMode.None)
                        : Object.FindAnyObjectByType(elementType, includeInactiveObjects);
#endif
                    break;

                default:
                    throw new Exception($"Unhandled Loc={attr.Loc}");
            }

            if (value == null)
                return existingValue;

            if (isArray)
            {
                Type realElementType = fieldType.GetElementType();

                Array componentArray = (Array)value;
                Array typedArray = Array.CreateInstance(
                    realElementType ?? throw new InvalidOperationException(),
                    componentArray.Length
                );

                if (elementType == realElementType)
                {
                    Array.Copy(componentArray, typedArray, typedArray.Length);
                    value = typedArray;
                }
                else if (typeof(ISerializableRef).IsAssignableFrom(realElementType))
                {
                    for (int i = 0; i < typedArray.Length; i++)
                    {
                        ISerializableRef elementValue = Activator.CreateInstance(realElementType) as ISerializableRef;
                        elementValue?.OnSerialize(componentArray.GetValue(i));
                        typedArray.SetValue(elementValue, i);
                    }
                    value = typedArray;
                }
            }

            if (iSerializable != null)
            {
                if (!iSerializable.OnSerialize(value))
                    return existingValue;
            }
            else
            {
                bool valuesAreEqual = existingValue != null && (isArray ? Enumerable.SequenceEqual((object[])value, (object[])existingValue) : value.Equals(existingValue));
                if (valuesAreEqual)
                    return existingValue;
                field.SetValue(c, value);
            }

#if UNITY_EDITOR
            EditorUtility.SetDirty(c);
#endif
            return value;
        }

        private static object GetComponentIfWrongType(object existingValue, Type elementType)
        {
            if (existingValue is Component existingComponent && existingComponent && !elementType.IsInstanceOfType(existingValue))
                return existingComponent.GetComponent(elementType);
            return existingValue;
        }

        private static void ValidateRef(SceneRefAttribute attr, Component c, FieldInfo field, object value)
        {
            Type fieldType = field.FieldType;
            bool isArray = fieldType.IsArray;

            if (value is ISerializableRef ser)
                value = ser.SerializedObject;

            if (IsEmptyOrNull(value, isArray))
            {
                if (attr.HasFlags(Flag.Optional)) return;
                
                var elementType = isArray ? fieldType.GetElementType() : fieldType;
                elementType = typeof(ISerializableRef).IsAssignableFrom(elementType) ? elementType?.GetGenericArguments()[0] : elementType;
                Debug.LogError($"{c.GetType().Name} missing required {elementType?.Name}{(isArray ? "[]" : "")} ref '{field.Name}'", c.gameObject);
                return;
            }

            if (attr.HasFlags(Flag.Optional))
            {
                if (isArray)
                {
                    Array a = (Array)value;
                    for (int i = 0; i < a.Length; ++i)
                    {
                        object o = UnpackValue(a.GetValue(i));
                        if (o == null)
                            continue;

                        if (o is MonoBehaviour m && m.gameObject == c.gameObject)
                        {
                            var elementType = fieldType.GetElementType();
                            Debug.LogError($"{c.GetType().Name} {elementType?.Name}[] ref '{field.Name}' cannot contain component from the same GameObject (i={i})", c.gameObject);
                        }
                    }
                }
                else
                {
                    object o = UnpackValue(value);
                    //value is on the same object as current component
                    if (o is MonoBehaviour m && m.gameObject == c.gameObject)
                    {
                        var elementType = typeof(ISerializableRef).IsAssignableFrom(fieldType)
                            ? fieldType.GetGenericArguments()[0]
                            : fieldType;
                        Debug.LogError($"{c.GetType().Name} {elementType.Name} ref '{field.Name}' cannot be on the same GameObject", c.gameObject);
                    }
                }
            }

            if (isArray)
            {
                Array a = (Array)value;
                for (int i = 0; i < a.Length; i++)
                {
                    object o = UnpackValue(a.GetValue(i));
                    if (o != null)
                        ValidateRefLocation(attr.Loc, c, field, o);
                    else
                        Debug.LogError($"{c.GetType().Name} missing required element ref in array '{field.Name}'", c.gameObject);
                }
            }
            else
            {
                ValidateRefLocation(attr.Loc, c, field, value);
            }
        }

        private static void ValidateRefLocation(RefLoc loc, Component c, FieldInfo field, object refObj)
        {
            switch (refObj)
            {
                case Component valueC:
                    ValidateRefLocation(loc, c, field, valueC);
                    break;

                case ScriptableObject valueSO:
                    ValidateRefLocationAnywhere(loc, c, field, valueSO);
                    break;

                case GameObject valueGO:
                    ValidateRefLocationAnywhere(loc, c, field, valueGO);
                    break;

                default:
                    throw new Exception($"{c.GetType().Name} has unexpected reference type {refObj?.GetType().Name}");
            }
        }

        private static void ValidateRefLocation(RefLoc loc, Component c, FieldInfo field, Component refObj)
        {
            switch (loc)
            {
                case RefLoc.Anywhere:
                    break;

                case RefLoc.Self:
                    if (refObj.gameObject != c.gameObject)
                        Debug.LogError($"{c.GetType().Name} requires {field.FieldType.Name} ref '{field.Name}' to be on Self", c.gameObject);
                    break;

                case RefLoc.Parent:
                    if (!c.transform.IsChildOf(refObj.transform))
                        Debug.LogError($"{c.GetType().Name} requires {field.FieldType.Name} ref '{field.Name}' to be a Parent", c.gameObject);
                    break;

                case RefLoc.Child:
                    if (!refObj.transform.IsChildOf(c.transform))
                        Debug.LogError($"{c.GetType().Name} requires {field.FieldType.Name} ref '{field.Name}' to be a Child", c.gameObject);
                    break;

                case RefLoc.Scene:
                    if (c == null)
                        Debug.LogError($"{c.GetType().Name} requires {field.FieldType.Name} ref '{field.Name}' to be in the scene", c.gameObject);
                    break;

                default:
                    throw new Exception($"Unhandled Loc={loc}");
            }
        }

        // ReSharper disable once UnusedParameter.Local
        private static void ValidateRefLocationAnywhere(RefLoc loc, Component c, FieldInfo field, Object refObj)
        {
            switch (loc)
            {
                case RefLoc.Anywhere:
                    break;

                case RefLoc.Self:
                case RefLoc.Parent:
                case RefLoc.Child:
                case RefLoc.Scene:
                    Debug.LogError($"{c.GetType().Name} requires {field.FieldType.Name} ref '{field.Name}' to be Anywhere", c.gameObject);
                    break;

                default:
                    throw new Exception($"Unhandled Loc={loc}");
            }
        }

        private static bool IsEmptyOrNull(object obj, bool isArray)
        {
            if (obj is ISerializableRef ser)
                return !ser.HasSerializedObject;

            return obj == null || obj.Equals(null) || (isArray && ((Array)obj).Length == 0);
        }

        private static object UnpackValue(object obj)
        {
            if (obj is ISerializableRef ser)
                return ser.SerializedObject;

            return obj;
        }
        
        private static Component[] GetComponentsInParent(Component c, Type elementType, bool includeInactive, bool excludeSelf)
        {
            var element = c;
            if (excludeSelf)
                element = c.transform.parent;

            return element == null
                ? Array.Empty<Component>()
                : element.GetComponentsInParent(elementType, includeInactive);
        }

        private static Component GetComponentInParent(Component c, Type elementType, bool includeInactive,
            bool excludeSelf)
        {
            var element = c;
            if (excludeSelf)
                element = c.transform.parent;

            return element == null
                ? null
                : element.GetComponentInParent(elementType, includeInactive);
        }

        private static Component[] GetComponentsInChildren(Component c, Type elementType, bool includeInactive,
            bool excludeSelf)
        {
            if (!excludeSelf)
                return c.GetComponentsInChildren(elementType, includeInactive);

            List<Component> components = new List<Component>();

            var transform = c.transform;
            int childCount = transform.childCount;

            for (int i = 0; i < childCount; ++i)
            {
                var child = transform.GetChild(i);
                components.AddRange(child.GetComponentsInChildren(elementType, includeInactive));
            }
            
            return components.ToArray();
        }
        
        private static Component GetComponentInChildren(Component c, Type elementType, bool includeInactive,
            bool excludeSelf)
        {
            if (!excludeSelf)
                return c.GetComponentInChildren(elementType, includeInactive);

            var transform = c.transform;
            int childCount = transform.childCount;

            for (int i = 0; i < childCount; ++i)
            {
                var child = transform.GetChild(i);
                var component = child.GetComponentInChildren(elementType, includeInactive);
                
                if (component != null)
                    return component;
            }
            
            return null;
        }
    }
}