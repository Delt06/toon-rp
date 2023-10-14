using System;
using JetBrains.Annotations;

namespace DELTation.ToonRP
{
    [Flags]
    public enum PrePassMode
    {
        Off,
        Depth = 1 << 0,
        Normals = 1 << 1,
        MotionVectors = 1 << 2,
    }

    public static class PrePassModeExtensions
    {
        [Pure]
        public static bool Includes(this PrePassMode mode, PrePassMode other) => (mode & other) != 0;

        [Pure]
        public static PrePassMode Sanitize(this PrePassMode mode)
        {
            if (!mode.Includes(PrePassMode.Depth))
            {
                if (mode.Includes(PrePassMode.Normals) || mode.Includes(PrePassMode.MotionVectors))
                {
                    mode |= PrePassMode.Depth;
                }
            }

            return mode;
        }
    }
}