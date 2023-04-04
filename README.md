# Toon Render Pipeline (Unity SRP)

A Scriptable Render Pipeline (SRP) designed specifically for toon/stylized visuals.

> The project is in its very **early** stages and has **not** been tested in production.
 
![Main](./Documentation/demo.jpg?raw=true)

### Unity Version

> Developed and verified with Unity 2021.3.0f1 LTS and Core RP Library v12.1.6.

### Table of Contents

- [Features](#features)
- [References](#references)  
- [Used Assets](#used-assets)  


## Features


- Toon Shader with globally configurable ramp

![Toon Shader](./Documentation/features_toon_shader.jpg?raw=true)

- Crisp anti-aliased ramp

![Crisp AA Ramp](./Documentation/features_crips_aa_ramp.jpg?raw=true)

- Variance Shadow Mapping (VSM)
  - _Optional_: crisp anti-aliased ramp

![VSM](./Documentation/features_vsm.jpg?raw=true)

- Blob Shadows
    - _Optional_: crisp anti-aliased ramp

![Blob Shadows](./Documentation/features_blob_shadows.jpg?raw=true)

- MSAA

![MSAA](./Documentation/features_msaa.jpg?raw=true)

- HDR
- Bloom
  - _Optional_: stylized pattern

![MSAA](./Documentation/features_bloom.jpg?raw=true)

- Outline (Inverted Hull)
  - Distance fade
  - _Optional_: get normals from a custom UV channel to improve outlines quality. Comes with a utility to bake these custom normals.

![Outline](./Documentation/features_outlines_fade.gif?raw=true)

- SSAO
  - _Optional_: stylized pattern

![SSAO](./Documentation/features_ssao.jpg?raw=true)

- Fog:
  - Affects the outlines too

![Fog](./Documentation/features_fog.jpg?raw=true)


## References
- [Catlike Coding](https://catlikecoding.com/)
- [LearnOpenGL - SSAO](https://learnopengl.com/Advanced-Lighting/SSAO)
- [Ronja's tutorials - Partial Derivatives (fwidth)](https://www.ronja-tutorials.com/post/046-fwidth/)

## Used Assets
- [Quaternius - Animated Mech Pack](https://quaternius.com/packs/animatedmech.html)
- [Quaternius - Ultimate Stylized Nature Pack](https://quaternius.com/packs/ultimatestylizednature.html)
