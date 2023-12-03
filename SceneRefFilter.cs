namespace KBCore.Refs
{
    public abstract class SceneRefFilter
    {
        internal abstract bool IncludeSceneRef(object obj);
    }
    
    // ReSharper disable once TypeParameterCanBeVariant
    public abstract class SceneRefFilter<T> : SceneRefFilter
        where T : class
    {

        internal override bool IncludeSceneRef(object obj) 
            => this.IncludeSceneRef((T) obj);
        
        /// <summary>
        /// Returns true if the given object should be included as a reference.
        /// </summary>
        public abstract bool IncludeSceneRef(T obj);
    }
}