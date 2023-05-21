#ifndef TOON_RP_VSM
#define TOON_RP_VSM

static const float ToonRp_Vsm_DepthScale = 0.1f;

float PackVsmDepth(const float depth)
{
    return depth * ToonRp_Vsm_DepthScale;
}

#endif // TOON_RP_VSM