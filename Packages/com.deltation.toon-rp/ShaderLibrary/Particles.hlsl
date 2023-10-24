#ifndef TOON_RP_PARTICLES
#define TOON_RP_PARTICLES

float ComputeSoftParticlesFade(const float depth, const float bufferDepth, const float softParticlesDistance, const float softParticlesRange)
{
    const float depthDelta = bufferDepth - depth;
    const float nearAttenuation = (depthDelta - softParticlesDistance) / softParticlesRange;
    return saturate(nearAttenuation);
}

#endif // TOON_RP_PARTICLES