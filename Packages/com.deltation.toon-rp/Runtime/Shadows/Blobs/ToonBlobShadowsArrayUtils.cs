using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace DELTation.ToonRP.Shadows.Blobs
{
    internal static class ToonBlobShadowsArrayUtils
    {
        public static unsafe void ExpandArray<T>(ref NativeArray<T> array, Allocator allocator,
            NativeArrayOptions options) where T : struct
        {
            var newArray = new NativeArray<T>(array.Length * 2, allocator, options);
            UnsafeUtility.MemCpy(newArray.GetUnsafePtr(), array.GetUnsafePtr(),
                UnsafeUtility.SizeOf<T>() * array.Length
            );
            array.Dispose();
            array = newArray;
        }
    }
}