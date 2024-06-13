using Unity.Collections;

namespace DELTation.ToonRP
{
    public static class NativeCollectionsUtils
    {
        public static NativeArray<T> CreateTempSingletonArray<T>(T value) where T : struct
        {
            var array = new NativeArray<T>(1, Allocator.Temp);
            array[0] = value;
            return array;
        }
    }
}