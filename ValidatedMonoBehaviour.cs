using UnityEngine;

namespace KBCore.Refs
{
    public class ValidatedMonoBehaviour : MonoBehaviour
    {
#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            if (!Application.isPlaying)
                this.ValidateRefs();
        }
#else
        protected virtual void OnValidate() { }
#endif
    }
}
