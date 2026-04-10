# 3DGS Shader Pack (Built-in Render Pipeline)

A Unity package for creating cool visual effects with [3D Gaussian Splatting](https://repo-sam.inria.fr/fungraph/3d-gaussian-splatting/) (3DGS) rendering based on Unity hlsl shader. 

Youtube Demo and tutorial [Video](youtube.com/watch?v=l_yDNO2sTno&feature=youtu.be). 


### Versions & Platform Compatibility
- Supports Unity 2021 and later. 
- Supports and tested with Built-in Render Pipeline (BiRP), Universal Render Pipeline (URP), and Hight Definition Render Pipeline. 
  - VR Performance: URP single pass > BiRP & HDRP
- [Attention!!] Only supports ``D3D12`` and ``Vulkan`` in ``mutilpass rendering``!
- Tested on Windows 11, possible compatibility with Android
- OpenXR compatibility (No Meta XR SDK support yet, as Meta XR SDK only supports D3D11)
- You will need a powerful PC to run larger scenes, e.g. a room scale environment and generated from [World Lab Marble](https://marble.worldlabs.ai/). 

 ### Compatible File types

- Tested supports reading & rendering PLY  files exported from:
  - [World Lab Marble](https://marble.worldlabs.ai/worlds)
  - original 3DGS training output from [INRIA](https://github.com/graphdeco-inria/gaussian-splatting)
  - ply converted by [gsbox](https://github.com/gotoeasy/gsbox)
  - [Apple ML-Sharp](https://github.com/apple/ml-sharp)

- 16 different types of cool visual effects for 3D gaussian splatting inspired by [spark, three.js](https://github.com/sparkjsdev/spark) based on hlsl shader, more will be added in the future as the package evolves: 
  - Deep meditation (visualization)
  - Waves (visualization)
  - Flare (loop animation)
  - Disintegrate (loop animation)
  - Perlin Wave (visualization)
  - Wind  (loop animation)
  - Magic (reveal animation)
  - Spread (reveal animation)
  - Unroll (reveal animation)
  - Twister (reveal animation)
  - Rain (reveal animation)
  - Glitter (visualization)
  - Galaxy Glitter (visualization)
  - Fly Dissolve (dissolve animation)
  - Glow Dissolve (dissolve animation)
  - Radial Expansion (interactivity)

- Gaussians can be correctly blended with transparent meshes based on their bounding boxes (improvement over [Aras' version](https://github.com/aras-p/UnityGaussianSplatting))



## Usage

### Install

After cloning or downloading this repository, open your Unity project (or create a new one). Navigate to `Window > Package Manager`, click the `+` button, select `Install package from disk...`, and then choose the `package.json` file from this repository.

### Setup

First, ensure your project is using a supported Graphics API. For Windows: in `Edit > Project Settings > Player > Other Settings`, uncheck `Auto Graphics API for Windows`. Then, in the `Graphics APIs for Windows` list, add `Vulkan` or `Direct3D12` and remove any other options. Unity will require a restart to switch the Graphics API. You may need to perform similar steps for other platforms. 

Note that for Android, you also need to uncheck `Apply display rotation during rendering` in `Player > Settings for Android > Other Settings > Vulkan Settings`, as this package currently does not support rendering in the native display orientation.

- No additional graphics settings needed for BiRP except for choosing Gamma color space and D3D12 or Vulkan API. 
- URP: Add `Gsplat URP Feature` to the URP renderer settings.
  - Find the `Universal Renderer Data` your project is using, click the `Add Renderer Feature` button, and choose `Gsplat URP Feature.`
  - If you are using Unity 6 or later, the Render Graph "Compatibility Mode" in URP settings must be turned off!
- HDRP: Add `Custom Pass` volume game object in your scene and a `Gsplat HDRP Pass` component to it. The injection Point should be set to `Before Transparent`. 

### Color space 

Most 3DGS assets are trained in Gamma space, following the official implementation. This means that the alpha blending for the Gaussians is also performed in Gamma space.  Since there is no longer an additional render texture that would allow us to convert the color space after the alpha blending of 3DGS, you must ensure your project's color space (`Edit > Project Settings > Player > Other Settings > Rendering > Color Space`) is set to "Gamma" for the 3DGS assets to be rendered correctly (be aware that HDRP doesn't support Gamma mode). For projects using a linear color space, you must retrain the 3DGS asset with linear-space images. While this plugin offers a `Gamma To Linear` option as a workaround, converting the color space before alpha blending leads to incorrect results and will lower the 3DGS rendering quality.


### Import Assets

Copy or drag & drop the PLY file anywhere into your project's `Assets` folder. The package will then automatically read the file and import it as a `Gsplat Asset`.

### Add Gsplat Renderer

Create or choose a game object in your scene, and add the `Gsplat Renderer` component on it. Point the `Gsplat Asset` field to one of your imported Gsplat Assets. Then it should appear in the viewport.

The `SH Degree` option sets the order of SH coefficients used for rendering. The final value is capped by the Gsplat Asset's `SH Bands`.

The `Gamma To Linear` option is offered as a workaround to render Gamma Space Gsplat Assets in a project using the Linear Space. This will degrade the rendering quality, so changing the color space of the project or retraining the 3DGS asset is the recommended approach. If your project uses a linear color space and you do not wish to retrain your 3DGS assets, it is recommended to use [aras-p/UnityGaussianSplatting](https://github.com/aras-p/UnityGaussianSplatting).
