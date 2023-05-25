# Toon Render Pipeline (Unity SRP)

![GitHub release (latest SemVer including pre-releases)](https://img.shields.io/github/v/release/Delt06/toon-rp?include_prereleases)
![GitHub license](https://img.shields.io/github/license/Delt06/toon-rp)



A Scriptable Render Pipeline (SRP) designed specifically for toon/stylized visuals.

> The project is in its very **early** stages and has **not** been tested in production.
 
![Main](./Documentation/demo.jpg?raw=true)

### Unity Version

> Developed and verified with Unity 2021.3.15f1 LTS and Core RP Library v12.1.8.

### Table of Contents

- [Installation](#installation)
- [Features](#features)
- [References](#references)  
- [Used Assets](#used-assets)  


## Installation

### 1. Add the package

#### Option 1
- Open Package Manager through `Window/Package Manager`
- Click "+" and choose "Add package from git URL..."
- Insert the URL:

```
https://github.com/Delt06/toon-rp.git?path=Packages/com.deltation.toon-rp
```

#### Option 2
Add the following line to `Packages/manifest.json`:
```
"com.deltation.toon-rp": "https://github.com/Delt06/toon-rp.git?path=Packages/com.deltation.toon-rp",
```

### 2. Create a pipeline asset

Inside the Project window, right click and select `Create/Rendering/Toon Render Pipeline Asset`.

### 3. Set the pipeline asset

Go to `Edit/Project Settings/Graphics` and set the field `Scriptable Render Pipeline Settings` with the newly created pipeline asset.

## Features


- Toon Shader
  - Globally/locally configurable ramp
  - Alpha Clipping and Transparency
  - Normal Map
  - GPU Instancing
  - SRP Batcher Support

![Toon Shader](./Documentation/features_toon_shader.jpg?raw=true)

- Crisp anti-aliased ramp

![Crisp AA Ramp](./Documentation/features_crips_aa_ramp.jpg?raw=true)

- Variance Shadow Mapping (VSM)
  - _Optional_: crisp anti-aliased ramp
  - _Optional_: up to four cascades

![VSM](./Documentation/features_vsm.jpg?raw=true)

- Blob Shadows
    - _Optional_: crisp anti-aliased ramp

![Blob Shadows](./Documentation/features_blob_shadows.jpg?raw=true)

- MSAA

![MSAA](./Documentation/features_msaa.jpg?raw=true)

- FXAA
    - _Optional_: low quality variant, about 1.5-2 times faster

![FXAA](./Documentation/features_fxaa.jpg?raw=true)

- HDR
- Bloom
  - _Optional_: stylized pattern

![Bloom](./Documentation/features_bloom.jpg?raw=true)

- Light Scattering (Post FX)

![Light Scattering](./Documentation/features_light_scattering.jpg?raw=true)

- Outline (Inverted Hull)
  - Distance fade
  - _Optional_: get normals from a custom channel to improve outlines quality. Comes with a utility to bake these custom normals.
  - _Optional_: remove inner outlines via stencil.
  - _Optional_: randomize thickness.

![Outline (Inverted Hull)](./Documentation/features_outlines_inverted_hull.jpg?raw=true)

![Outline Fade](./Documentation/features_outlines_fade.gif?raw=true)

- Outline (Screen Space)
  - Based on colors, normals, and depth.

![Outline (Screen Space)](./Documentation/features_outlines_screen_space.jpg?raw=true)

- SSAO
  - _Optional_: stylized pattern

![SSAO](./Documentation/features_ssao.jpg?raw=true)

- Fog:
  - Affects the outlines too

![Fog](./Documentation/features_fog.jpg?raw=true)

- Matcap:
  - Additive (e.g., fake lighting)
  - Multiplicative: (e.g., fake reflections)

![Matcap: Additive](./Documentation/features_matcap_additive.jpg?raw=true)


![Matcap: Multiplicative](./Documentation/features_matcap_multiplicative.jpg?raw=true)


## References
- [Catlike Coding](https://catlikecoding.com/)
- [LearnOpenGL - SSAO](https://learnopengl.com/Advanced-Lighting/SSAO)
- [Ronja's tutorials - Partial Derivatives (fwidth)](https://www.ronja-tutorials.com/post/046-fwidth/)
- [Geeks 3D - Fast Approximate Anti-Aliasing (FXAA) Demo](https://www.geeks3d.com/20110405/fxaa-fast-approximate-anti-aliasing-demo-glsl-opengl-test-radeon-geforce/3/)

## Used Assets
- [Quaternius - Animated Mech Pack](https://quaternius.com/packs/animatedmech.html)
- [Quaternius - Ultimate Stylized Nature Pack](https://quaternius.com/packs/ultimatestylizednature.html)
- [Aika: Sailor Uniform](https://assetstore.unity.com/packages/3d/characters/aika-sailor-uniform-222398)
- [Stone](https://assetstore.unity.com/packages/3d/environments/landscapes/stone-62333)
- [nidorx/matcaps: A huge library of MatCap textures in PNG and ZMT.](https://github.com/nidorx/matcaps)
