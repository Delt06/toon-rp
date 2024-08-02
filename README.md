# Toon Render Pipeline (Unity SRP)

![GitHub release (latest SemVer including pre-releases)](https://img.shields.io/github/v/release/Delt06/toon-rp?include_prereleases)
![GitHub license](https://img.shields.io/github/license/Delt06/toon-rp)

A Scriptable Render Pipeline (SRP) designed specifically for toon/stylized visuals.

> 🚧 The project is in its **early** stages and has **not** been tested in production.

> ⚠️ The project only supports **Linear** color space.

> 📝 The development progress can be tracked via [Issues](https://github.com/Delt06/toon-rp/issues) and [Project Board](https://github.com/users/Delt06/projects/3).
 
![Main](./Documentation/demo.jpg?raw=true)

### Unity Version

> 🔨 Developed and verified with Unity 2022.3.12f1 LTS and Core RP Library v14.0.9.

### Verified Platforms

- Windows (DirectX 11, DirectX 12, Vulkan, OpenGL)
- Android (OpenGL ES 3.0+, Vulkan)
- WebGL 2.0
- XR (see [XR](https://github.com/Delt06/toon-rp/wiki/XR) Wiki page for full info)

> ⚠️ Other platforms may work but have not been tested yet.

### Table of Contents

- [Features](#features)
- [Installation](#installation)
- [References](#references)  
- [Used Assets](#used-assets)  

## Features

> See the [project Wiki](https://github.com/Delt06/toon-rp/wiki) for the full feature list.

- [Full customization of the lighting ramp](https://github.com/Delt06/toon-rp/wiki/Global-Ramp): arbitrary threshold, smoothness, and number of steps.

![Ramp](https://github.com/Delt06/toon-rp/assets/32465621/4bd838bf-afd6-46d7-9437-e1042b00dfe8)
![Ramp Texture](https://user-images.githubusercontent.com/32465621/278110582-b180659a-da28-4c87-a374-375439725c5a.png)

- [Shadows](https://github.com/Delt06/toon-rp/wiki/Shadows): multiple options for soft shadows and stylization.

| Crisp Shadows | Soft Shadows | Blob Shadows |
|-|-|-|
|![Shadows](https://github.com/Delt06/toon-rp/assets/32465621/ad145e2c-b09d-40b1-a20c-537978e400dc)|![VSM Shadows](https://github.com/Delt06/toon-rp/assets/32465621/251fa5a1-03cf-4aaf-83de-4959e28fb6e8)|![Blob Shadows](https://github.com/Delt06/toon-rp/assets/32465621/b28fc531-734b-4bc7-867d-9cfb0030cb02)|

- Optional [Tiled Lighting (Forward+)](https://github.com/Delt06/toon-rp/wiki/Tiled-Lighting-(Forward-Plus)): render many realtime lights.

![Tiled Lighting](https://github.com/Delt06/toon-rp/assets/32465621/a896782f-7f7a-49d5-acd3-c9b848390dc2)

- [Shader Graph support](https://github.com/Delt06/toon-rp/wiki/Shader-Graph).

![Shader Graph](https://user-images.githubusercontent.com/32465621/278428880-83cd2645-a14a-4548-b7c6-f1d54c4837c2.png)

- [Screen-Space](https://github.com/Delt06/toon-rp/wiki/Screen‐Space-Outline) and [Inverted Hull Outlines](https://github.com/Delt06/toon-rp/wiki/Inverted-Hull-Outline).

![Screen-Space Outlines](https://github.com/Delt06/toon-rp/assets/32465621/3b164de2-d7ad-4e70-b150-e2346e2a64f9)
![Inverted HullOutlines](https://user-images.githubusercontent.com/32465621/278466766-627bf696-a9cf-4a79-ac7c-b7d41415e5ef.png)

- Stylized post-processing effects: [bloom](https://github.com/Delt06/toon-rp/wiki/Bloom), [SSAO](https://github.com/Delt06/toon-rp/wiki/SSAO), etc.

![Bloom](https://github.com/Delt06/toon-rp/assets/32465621/daa436eb-ee5b-45a9-9c5f-670c5557cc5d)
![SSAO](https://user-images.githubusercontent.com/32465621/278386089-dc03df40-093a-4e9f-9b77-abbb79692ca5.png)

## Installation

> 📝 Note: to install the package for an older Unity version, refer to the [Installation](https://github.com/Delt06/toon-rp/wiki#older-unity-versions) page. 

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

Inside the Project window, right click and select `Create/Toon RP/Toon Render Pipeline Asset`.

### 3. Set the pipeline asset

Go to `Edit/Project Settings/Graphics` and set the field `Scriptable Render Pipeline Settings` with the newly created pipeline asset.

## References

- [Catlike Coding](https://catlikecoding.com/)

## Used Assets
- [Quaternius - Animated Mech Pack](https://quaternius.com/packs/animatedmech.html)
- [Quaternius - Ultimate Stylized Nature Pack](https://quaternius.com/packs/ultimatestylizednature.html)
- [Quaternius - Ultimate Space Kit](https://quaternius.com/packs/ultimatespacekit.html)
- [Quaternius - Toon Shooter Game Kit](https://quaternius.com/packs/toonshootergamekit.html)
- [Quaternius - Cube World Kit](https://quaternius.com/packs/cubeworldkit.html)
- [Quaternius - Cyberpunk Game Kit](https://quaternius.com/packs/cyberpunkgamekit.html)
- [Aika: Sailor Uniform](https://assetstore.unity.com/packages/3d/characters/aika-sailor-uniform-222398)
- [Stone](https://assetstore.unity.com/packages/3d/environments/landscapes/stone-62333)
- [nidorx/matcaps: A huge library of MatCap textures in PNG and ZMT.](https://github.com/nidorx/matcaps)
