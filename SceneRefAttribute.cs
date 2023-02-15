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
        Optional = 1,
        /// <summary>
        /// Include inactive components in the results (only applies to Child and Parent). 
        /// </summary>
        IncludeInactive = 2,
    }
    
    /// <summary>
    /// Attribute allowing you to decorate component reference fields with their search criteria. 
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public abstract class SceneRefAttribute : PropertyAttribute
    {
        internal RefLoc Loc { get; }
        internal Flag Flags { get;  }

        internal SceneRefAttribute(RefLoc loc, Flag flags = Flag.None) 
        {
            this.Loc = loc;
            this.Flags = flags;
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
        public AnywhereAttribute(Flag flags = Flag.None) 
            : base(RefLoc.Anywhere, flags: flags)
        {}
    }
    
    /// <summary>
    /// Self looks for the reference on the same game object as the attributed component
    /// using GetComponent(s)()
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class SelfAttribute : SceneRefAttribute
    {
        public SelfAttribute(Flag flags = Flag.None) 
            : base(RefLoc.Self, flags: flags)
        {}
    }
    
    /// <summary>
    /// Parent looks for the reference on the parent hierarchy of the attributed components game object
    /// using GetComponent(s)InParent()
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class ParentAttribute : SceneRefAttribute
    {
        public ParentAttribute(Flag flags = Flag.None) 
            : base(RefLoc.Parent, flags: flags)
        {}
    }
    
    /// <summary>
    /// Child looks for the reference on the child hierarchy of the attributed components game object
    /// using GetComponent(s)InChildren()
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class ChildAttribute : SceneRefAttribute
    {
        public ChildAttribute(Flag flags = Flag.None) 
            : base(RefLoc.Child, flags: flags)
        {}
    }
}