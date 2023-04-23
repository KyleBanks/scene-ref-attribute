using UnityEngine;

namespace KBCore.Refs
{
    public class ValidatedMonoBehaviour : MonoBehaviour
    {
#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            this.ValidateRefs();
        }
#else
        protected virtual void OnValidate() { }
#endif
    }
}
