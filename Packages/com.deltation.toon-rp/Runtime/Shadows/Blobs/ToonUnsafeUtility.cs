using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Assertions;

namespace DELTation.ToonRP.Shadows.Blobs
{
    internal static unsafe class ToonUnsafeUtility
    {
        public static void MemcpyToManagedArray<TManaged, TNative>(
            TManaged[] managedDestination, NativeList<TNative> nativeSource
        )
            where TNative : unmanaged where TManaged : unmanaged
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            Assert.IsTrue(nativeSource.Length * sizeof(TNative) <= managedDestination.Length * sizeof(TManaged));
#endif // ENABLE_UNITY_COLLECTIONS_CHECKS
            
            fixed (TManaged* managedPtr = managedDestination)
            {
                UnsafeUtility.MemCpy(managedPtr,
                    nativeSource.GetUnsafePtr(),
                    nativeSource.Length * sizeof(TNative)
                );
            }
        }
    }
}