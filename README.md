# Toon Render Pipeline (Unity SRP)

![GitHub release (latest SemVer including pre-releases)](https://img.shields.io/github/v/release/Delt06/toon-rp?include_prereleases)
![GitHub license](https://img.shields.io/github/license/Delt06/toon-rp)

A Scriptable Render Pipeline (SRP) designed specifically for toon/stylized visuals.

> 🚧 The project is in its very **early** stages and has **not** been tested in production.

> ⚠️ The project only supports **Linear** color space. 
 
![Main](./Documentation/demo.jpg?raw=true)

### Unity Version

> 🔨 Developed and verified with Unity 2021.3.15f1 LTS and Core RP Library v12.1.8.

### Table of Contents

- [Features](#features)
- [Installation](#installation)
- [References](#references)  
- [Used Assets](#used-assets)  

## Features

- [Full customization of the lighting ramp](https://github.com/Delt06/toon-rp/wiki/Global-Ramp);
- Forward and [Forward+](https://github.com/Delt06/toon-rp/wiki/Tiled-Lighting-(Forward-Plus)) rendering paths;
- [Shadows](https://github.com/Delt06/toon-rp/wiki/Shadows);
- [Shader Graph support](https://github.com/Delt06/toon-rp/wiki/Shader-Graph);
- [Screen-Space](https://github.com/Delt06/toon-rp/wiki/Screen‐Space-Outline) and [Inverted Hull Outlines](https://github.com/Delt06/toon-rp/wiki/Inverted-Hull-Outline);
- Stylized post-processing effects ([bloom](https://github.com/Delt06/toon-rp/wiki/Bloom), [SSAO](https://github.com/Delt06/toon-rp/wiki/SSAO), etc.).

See the [project Wiki](https://github.com/Delt06/toon-rp/wiki) for the full feature list.

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

## References

- [Catlike Coding](https://catlikecoding.com/)

## Used Assets
- [Quaternius - Animated Mech Pack](https://quaternius.com/packs/animatedmech.html)
- [Quaternius - Ultimate Stylized Nature Pack](https://quaternius.com/packs/ultimatestylizednature.html)
- [Quaternius - Ultimate Space Kit](https://quaternius.com/packs/ultimatespacekit.html)
- [Quaternius - Toon Shooter Game Kit](https://quaternius.com/packs/toonshootergamekit.html)
- [Quaternius - Cube World Kit](https://quaternius.com/packs/cubeworldkit.html)
- [Aika: Sailor Uniform](https://assetstore.unity.com/packages/3d/characters/aika-sailor-uniform-222398)
- [Stone](https://assetstore.unity.com/packages/3d/environments/landscapes/stone-62333)
- [nidorx/matcaps: A huge library of MatCap textures in PNG and ZMT.](https://github.com/nidorx/matcaps)
