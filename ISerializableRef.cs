using System;

namespace KBCore.Refs
{
    internal interface ISerializableRef
    {
        Type RefType { get; }
        object SerializedObject { get; }
        bool HasSerializedObject { get; }

        /// <summary>
        /// Callback for serialization.
        /// </summary>
        /// <param name="value">Object to serialize.</param>
        /// <returns>True if the value has changed.</returns>
        bool OnSerialize(object value);

        void Clear();
    }

    internal interface ISerializableRef<T> : ISerializableRef
        where T : class
    {
        T Value { get; }
    }
}