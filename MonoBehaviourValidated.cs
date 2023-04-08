using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KBCore.Refs
{
    public class MonoBehaviourValidated : MonoBehaviour
    {
        private void OnValidate()
        {
            this.ValidateRefs();
        }
    }
}
