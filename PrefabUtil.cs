using UnityEngine;

namespace KBCore.Refs
{
    // Subset taken from KBCore.Utils.PrefabUtil for open sourcing
    internal class PrefabUtil
    {
        internal static bool IsUninstantiatedPrefab(GameObject obj)
            => obj.scene.rootCount == 0;
    }
}