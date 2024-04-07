namespace DELTation.ToonRP
{
    public static class ToonPasses
    {
        public static readonly Pass Forward = new("Toon RP Forward", "ToonRPForward", "SHADERPASS_FORWARD");
        public static readonly Pass ForwardUnlit =
            new("Toon RP Forward (Unlit)", "ToonRPForward", "SHADERPASS_FORWARD_UNLIT");
        public static readonly Pass ForwardParticlesUnlit = new("Toon RP Forward (Particles Unlit)", "ToonRPForward",
            "SHADERPASS_FORWARD_PARTICLES_UNLIT"
        );
        public static readonly Pass DepthOnly = new("Toon RP Depth Only", "ToonRPDepthOnly", "SHADERPASS_DEPTHONLY"
        );
        public static readonly Pass DepthNormals =
            new("Toon RP Depth Normals", "ToonRPDepthNormals", "SHADERPASS_DEPTHNORMALS");
        public static readonly Pass MotionVectors =
            new("Toon RP Motion Vectors", "ToonRPMotionVectors", "SHADERPASS_MOTIONVECTORS");
        public static readonly Pass ShadowCaster =
            new("Toon RP Shadow Caster", "ShadowCaster", "SHADERPASS_SHADOWCASTER");
        public static readonly Pass Meta = new("Toon RP Meta", "Meta", "SHADERPASS_META");

        public readonly struct Pass
        {
            public readonly string Name;
            public readonly string LightMode;
            public readonly string ReferenceName;

            public Pass(string name, string lightMode, string referenceName)
            {
                Name = name;
                LightMode = lightMode;
                ReferenceName = referenceName;
            }
        }
    }
}