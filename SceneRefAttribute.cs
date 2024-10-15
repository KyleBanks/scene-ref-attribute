using System;
using UnityEngine;

namespace KBCore.Refs
{
    /// <summary>
    /// RefLoc indicates the expected location of the reference.
    /// </summary>
    internal enum RefLoc
    {
        /// <summary>
        /// Anywhere will only validate the reference isn't null, but relies on you to 
        /// manually assign the reference yourself.
        /// </summary>
        Anywhere = -1,
        /// <summary>
        /// Self looks for the reference on the same game object as the attributed component
        /// using GetComponent(s)()
        /// </summary>
        Self = 0,
        /// <summary>
        /// Parent looks for the reference on the parent hierarchy of the attributed components game object
        /// using GetComponent(s)InParent()
        /// </summary>
        Parent = 1,
        /// <summary>
        /// Child looks for the reference on the child hierarchy of the attributed components game object
        /// using GetComponent(s)InChildren()
        /// </summary>
        Child = 2,
        /// <summary>
        /// Scene looks for the reference anywhere in the scene
        /// using GameObject.FindAnyObjectByType() and GameObject.FindObjectsOfType()
        /// </summary>
        Scene = 4,
    }
    
    /// <summary>
    /// Optional flags offering additional functionality.
    /// </summary>
    [Flags]
    public enum Flag
    {
        /// <summary>
        /// Default behaviour.
        /// </summary>
        None = 0,
        /// <summary>
        /// Allow empty (or null in the case of non-array types) results.
        /// </summary>
        Optional = 1 << 0,
        /// <summary>
        /// Include inactive components in the results (only applies to Child and Parent). 
        /// </summary>
        IncludeInactive = 1 << 1,
        /// <summary>
        /// Allows the user to override the automatic selection. Will still validate that
        /// the field location (self, child, etc) matches as expected.
        /// </summary>
        Editable = 1 << 2,
        /// <summary>
        /// Excludes components on current GameObject from search(only applies to Child and Parent).
        /// </summary>
        ExcludeSelf = 1 << 3,
        /// <summary>
        /// Allows the user to manually set the reference and does not validate the location if manually set
        /// </summary>
        EditableAnywhere = 1 << 4 | Editable
    }
    
    /// <summary>
    /// Attribute allowing you to decorate component reference fields with their search criteria. 
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public abstract class SceneRefAttribute : PropertyAttribute
    {
        internal RefLoc Loc { get; }
        internal Flag Flags { get; }

        internal SceneRefFilter Filter
        {
            get
            {
                if (this._filterType == null)
                    return null;
                return (SceneRefFilter) Activator.CreateInstance(this._filterType);
            }
        }

        private readonly Type _filterType;

        internal SceneRefAttribute(
            RefLoc loc, 
            Flag flags,
            Type filter
        ) 
        {
            this.Loc = loc;
            this.Flags = flags;
            this._filterType = filter;
        }

        internal bool HasFlags(Flag flags)
            => (this.Flags & flags) == flags;
    }
    
    /// <summary>
    /// Anywhere will only validate the reference isn't null, but relies on you to 
    /// manually assign the reference yourself.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class AnywhereAttribute : SceneRefAttribute
    {
        public AnywhereAttribute(Flag flags = Flag.None, Type filter = null) 
            : base(RefLoc.Anywhere, flags, filter)
        {}
    }
    
    /// <summary>
    /// Self looks for the reference on the same game object as the attributed component
    /// using GetComponent(s)()
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class SelfAttribute : SceneRefAttribute
    {
        public SelfAttribute(Flag flags = Flag.None, Type filter = null) 
            : base(RefLoc.Self, flags, filter)
        {}
    }
    
    /// <summary>
    /// Parent looks for the reference on the parent hierarchy of the attributed components game object
    /// using GetComponent(s)InParent()
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class ParentAttribute : SceneRefAttribute
    {
        public ParentAttribute(Flag flags = Flag.None, Type filter = null) 
            : base(RefLoc.Parent, flags, filter)
        {}
    }
    
    /// <summary>
    /// Child looks for the reference on the child hierarchy of the attributed components game object
    /// using GetComponent(s)InChildren()
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class ChildAttribute : SceneRefAttribute
    {
        public ChildAttribute(Flag flags = Flag.None, Type filter = null) 
            : base(RefLoc.Child, flags, filter)
        {}
    }
    
    /// <summary>
    /// Scene looks for the reference anywhere in the scene
    /// using GameObject.FindAnyObjectByType() and GameObject.FindObjectsOfType()
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class SceneAttribute : SceneRefAttribute
    {
        public SceneAttribute(Flag flags = Flag.None, Type filter = null) 
            : base(RefLoc.Scene, flags, filter)
        {}
    }
}