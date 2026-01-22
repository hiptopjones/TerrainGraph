# Terrain Graph

A node-based procedural terrain generator.

![alt text](GraphScreenshot.png)
![alt text](IslandScreenshot.png)

Below is an overview of all nodes available in Terrain Graph.

---

## IMPORT

| Node | Description | Key Parameters |
|------|-------------|----------------|
| **Import Spline** | Imports spline from Unity scene by name | Target Object (string, default "My Spline") |

---

## CREATE

### SPLINE

These nodes generate new splines:

| Node | Description | Key Parameters |
|------|-------------|----------------|
| **Circle Spline** | Circular spline primitive | - |
| **Contour Multi Spline** | Multiple contour lines from heightmap | - |
| **Contour Spline** | Single contour line extraction | - |
| **Curve Spline** | Spline from curve definition | - |
| **Select Spline** | Spline selection/filtering | - |

### HEIGHTMAP

These nodes generate new heightmaps:

| Node | Description | Key Parameters |
|------|-------------|----------------|
| **Cellular Noise Height** | Cellular/Worley noise pattern | Offset, Point Count, Seed, Size |
| **Constant Height** | Uniform flat plane | Height (default 0.5), Size (default 256) |
| **Gradient Height** | Applies 1D gradient to the heightmap | Gradient, Size (default 256) |
| **Islands Height** | Island-like terrain generation | - |
| **Perlin Noise Height** | Classic Perlin noise | Offset (Vector2), Frequency (default 0.05), Seed, Size |
| **Radial Shape Height** | Radial primitives (cone, cylinder, gaussian, smoothstep) | Size, Radius Percent, Shape Type |
| **Slope Height** | Sloped plane | - |
| **Spline Curvature Height** | Height based on spline curvature | - |
| **Spline Height** | Rasterizes spline to heightmap | - |
| **Spline Voronoi Height** | Voronoi diagram from spline points | - |
| **Texture Height** | Texture to heightmap conversion | Texture (Texture2D) |
| **Voronoi Noise Height** | Voronoi/cellular noise | Offset (Vector2), Point Count (default 20), Seed, Size |

---

## MODIFY

### HEIGHTMAPS

These nodes modify existing heightmaps:

| Node | Description | Key Parameters |
|------|-------------|----------------|
| **Absolute** | Converts heights to absolute values | Grid |
| **Arithmetic** | Math operations with scalar value | Grid, Value, Operation (Add/Subtract/Multiply/Divide/Min/Max/Average/Compare/Power), Flip Inputs |
| **Bias** | Applies bias curve adjustment (0-1 power curve) | Grid, Bias (default 0.5) |
| **Blend** | Combines two grids with various blend modes | Grid 1, Grid 2, Operation, Flip Inputs |
| **Blur** | Gaussian blur smoothing | Grid, Radius (default 5), Iterations (1-50, default 1) |
| **Clamp** | Constrains heights to min/max range | Grid, Minimum (default 0), Maximum (default 1) |
| **Erosion** | Hydraulic erosion simulation | Grid, Iterations (1-1000), Rain, Evaporation, Capacity, Erosion, Deposition, Gravity |
| **Gain** | Applies gain curve adjustment (S-curve) | Grid, Gain (default 0.5) |
| **Grow** | Morphological dilation (expands features) | Grid, Radius (default 1) |
| **Invert** | Inverts heights around pivot point | Grid, Pivot (default 0) |
| **Isolate** | Set specified height = 1, all others = 0 | Grid, Height (default 0.5) |
| **Lift** | Raises terrain under spline with falloff | Grid, Spline, Strength, Margin, Easing Type |
| **Mask** | Converts non-zero values to binary mask (0 or 1) | Grid |
| **Masked Fill** | Fills masked areas by averaging neighbors | Grid, Mask, Iterations (1-1000, default 100) |
| **Normalize** | Remaps to 0-1 range | Grid |
| **Preview** | Pass-through for visualization | Grid |
| **Ramp** | Remaps using curve or gradient | Grid, Ramp Type (Curve/Gradient), Curve or Gradient |
| **Range** | Remaps from one range to another | Grid, From Range (Vector2), To Range (Vector2) |
| **Rebase** | Shifts floor or ceiling to target value | Grid, Rebase Type (Floor/Ceiling), Value |
| **Relax** | Smooths by iterative neighbor averaging | Grid, Iterations (1-1000, default 100) |
| **Remove** | Sets specified height = 0 | Grid, Height (default 0) |
| **Replace** | Replaces a specific height value | Grid, From (default 1), To (default 0.5) |
| **Resize** | Changes resolution with optional scale preservation | Grid, Size (16+, default 256), Preserve Scale |
| **Stamp** | Stamps one heightmap onto another with mask | Grid, Stamp, Mask, Radius, Easing Type |
| **Step** | Binary threshold (step function) | Grid, Threshold (0-1, default 0.5) |
| **Transform** | 2D transform (translate, rotate, scale) | Grid, Translation Percent, Rotation Degrees, Scale |

### SPLINES

These nodes modify splines:

| Node | Description | Key Parameters |
|------|-------------|----------------|
| **Displace Spline** | Displaces spline vertices | - |
| **Open/Closed Spline** | Converts between open/closed splines | - |
| **Resample Spline** | Changes vertex count | Spline, Vertices (10+, default 100) |
| **Slice Spline** | Extracts spline portion | - |
| **Smooth Spline** | Smooths by averaging | Spline, Iterations (1-100, default 1), Min Angle (default 150) |
| **Splice Spline** | Joins multiple splines | - |

---

## EXPORT

Output nodes for final results:

| Node | Description | Key Parameters |
|------|-------------|----------------|
| **Export Mesh** | Heightmap to mesh asset | - |
| **Export Spline** | Spline to scene/asset | - |
| **Export Stamp** | Heightmap to stamp asset | - |
| **Export Terrain** | Heightmap to Unity TerrainData | Grid, Terrain (string, default "My Terrain Data") |
| **Export Texture** | Heightmap to texture asset | - |

---

## Architecture Notes

1. **Common Options**: All nodes support **Disable** (bypass processing) and **Preview** options
2. **GPU Acceleration**: Height operations use **compute shaders** for performance
3. **Caching**: Version-based caching prevents redundant recomputation
4. **Iterative Processing**: Many nodes support iteration counts (1-1000) for gradual effects
5. **Easing Types**: Common options include Constant, Linear, Cubic, SmoothStep
6. **Spline Integration**: Multiple nodes bridge between splines and heightmaps for terrain authoring