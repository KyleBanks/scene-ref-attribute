using System;

namespace KBCore.Refs
{
    internal interface ISerializableRef
    {
        Type RefType { get; }
        object SerializedObject { get; }
        bool HasSerializedObject => this.SerializedObject != null;
        
        /// <summary>
        /// Callback for serialization.
        /// </summary>
        /// <param name="value">Object to serialize.</param>
        /// <returns>True if the value has changed.</returns>
        bool OnSerialize(object value);
    }

    internal interface ISerializableRef<T> : ISerializableRef
        where T : class
    {
        Type ISerializableRef.RefType => typeof(T);
        
        T Value { get; }
    }
}