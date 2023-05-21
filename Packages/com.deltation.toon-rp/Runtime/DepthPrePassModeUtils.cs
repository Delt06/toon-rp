namespace DELTation.ToonRP
{
    public static class DepthPrePassModeUtils
    {
        public static DepthPrePassMode CombineDepthPrePassModes(DepthPrePassMode mode1, DepthPrePassMode mode2) =>
            mode1 > mode2 ? mode1 : mode2;
    }
}