using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Assertions;

namespace DELTation.ToonRP.Shadows.Blobs
{
    internal static unsafe class ToonUnsafeUtility
    {
        public static void MemcpyToManagedArray<TManaged, TNative>(
            TManaged[] managedDestination, TNative* nativeSource, int nativeSourceLength
        )
            where TNative : unmanaged where TManaged : unmanaged
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            Assert.IsTrue(nativeSourceLength * sizeof(TNative) <= managedDestination.Length * sizeof(TManaged));
#endif // ENABLE_UNITY_COLLECTIONS_CHECKS

            fixed (TManaged* managedPtr = managedDestination)
            {
                UnsafeUtility.MemCpy(managedPtr,
                    nativeSource,
                    nativeSourceLength * sizeof(TNative)
                );
            }
        }
    }
}