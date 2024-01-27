using UnityEngine.Experimental.Rendering;
using UnityEngine.Pool;

namespace DELTation.ToonRP.Xr
{
    internal class ToonXrPass : XRPass
    {
        public static XRPass Create(XRPassCreateInfo createInfo)
        {
            ToonXrPass pass = GenericPool<ToonXrPass>.Get();
            pass.InitBase(createInfo);
            return pass;
        }

        public override void Release()
        {
            GenericPool<ToonXrPass>.Release(this);
        }
    }
}