# TerrainGraph

A node-based procedural terrain heightmap generation tool for Unity. TerrainGraph provides a visual graph editor where nodes are connected to build complex terrain generation pipelines, with all heightmap processing running on the GPU via compute shaders for real-time preview feedback.

![alt text](GraphScreenshot.png)
![alt text](IslandScreenshot.png)

## Features

- Visual node graph editor built on Unity GraphToolkit
- GPU-accelerated heightmap processing via HLSL compute shaders
- Real-time preview of each node's output in the editor
- Spline-based terrain shaping tools
- Export to Unity TerrainData, textures, and meshes
- Topological graph execution with dependency-aware caching

## Node Reference

### Generate

Nodes that produce heightmaps or splines from scratch.

#### Height

| Node | Description |
|------|-------------|
| **Constant** | Flat heightmap at a fixed value |
| **Gradient** | Height that varies across the map using a configurable gradient ramp |
| **Perlin Noise** | Perlin noise heightmap with configurable frequency, offset, and seed |
| **Voronoi Noise** | Voronoi cell-based heightmap with configurable frequency and seed |
| **Value Noise** | Smooth cellular noise heightmap |
| **Grid Noise** | Grid-pattern noise heightmap |
| **Radial Shape** | Height based on distance from a center point, with configurable shape type |
| **Slope** | Height that varies along a configurable direction |
| **Islands** | Procedural island landmass shapes |
| **Spline Height** | Height influenced by proximity to a spline |
| **Spline Voronoi** | Voronoi pattern generated along a spline path |
| **Spline Radial** | Radial height falloff emanating from a spline |
| **Spline Ridge** | Ridge or mountain shape along a spline path |
| **Spline Curvature** | Height derived from the curvature of a spline |

#### Spline

| Node | Description |
|------|-------------|
| **Circle** | Circular spline with configurable radius, arc angle, and vertex count |
| **Curve** | Spline drawn from a Bezier curve |
| **Contour** | Extracts a height contour line from a heightmap at a given level |
| **Multi Contour** | Extracts multiple contour lines from a heightmap at specified levels |
| **Select** | Selects a single spline from a spline list by index |

---

### Modify Height

Nodes that take one or more heightmaps as input and produce a modified heightmap.

#### Arithmetic

| Node | Description |
|------|-------------|
| **Add** | Adds a constant value or a second heightmap |
| **Subtract** | Subtracts a constant value or a second heightmap |
| **Multiply** | Multiplies by a constant value or a second heightmap |
| **Divide** | Divides by a constant value or a second heightmap |
| **Average** | Averages with a constant value |
| **Power** | Raises values to a configurable exponent |
| **Absolute** | Takes the absolute value of each height |
| **Invert** | Inverts values (`1 - value`) |
| **Arithmetic** | General arithmetic node supporting Add, Subtract, Multiply, Divide, Min, Max, Average, Compare, and Power operations in one node |

#### Blending

| Node | Description |
|------|-------------|
| **Blend** | Combines two heightmaps using a configurable operator: Add, Subtract, Multiply, Divide, Min, Max, Average, or Compare |
| **Minimum** | Element-wise minimum of two heightmaps |
| **Maximum** | Element-wise maximum of two heightmaps |
| **Compare** | Element-wise comparison of two heightmaps |
| **Stamp** | Stamps a heightmap pattern onto a base heightmap, with optional mask and configurable easing |
| **Mask** | Multiplies a heightmap by a mask |

#### Range

| Node | Description |
|------|-------------|
| **Normalize** | Remaps the heightmap range to [0, 1] |
| **Range** | Remaps the heightmap range to a configurable [min, max] |
| **Clamp** | Clamps values to a configurable [min, max] |
| **Lift** | Raises all values to a configurable minimum |
| **Rebase** | Shifts the base of the height range |
| **Bias** | Applies a bias curve to skew values toward 0 or 1 |
| **Gain** | Applies a gain curve to push values toward the extremes or center |
| **Step** | Applies a threshold step function |
| **Ramp** | Creates a height ramp across the map |
| **Terrace** | Creates a terraced, stepped effect with configurable step size and smoothness |

#### Filtering

| Node | Description |
|------|-------------|
| **Blur** | Gaussian blur with configurable radius and iteration count |
| **Relax** | Laplacian smoothing that averages each point with its neighbors |
| **Grow** | Max-filter dilation that expands elevated features |
| **Isolate** | Detects and isolates local peaks |

#### Transform

| Node | Description |
|------|-------------|
| **Transform** | Translates, rotates, and scales the heightmap |
| **Translate** | Offsets the heightmap by X/Y percentage |
| **Rotate** | Rotates the heightmap by a given angle |
| **Scale** | Scales (zooms) the heightmap |
| **Resize** | Changes the heightmap resolution with configurable interpolation |

#### Replace & Remove

| Node | Description |
|------|-------------|
| **Replace** | Replaces heights within a threshold range with a new value |
| **Remove** | Zeros out heights within a threshold range |

#### Erosion

| Node | Description |
|------|-------------|
| **Erosion** | Hydraulic erosion simulation — water flows downslope, eroding and depositing sediment based on flow capacity |
| **Particle Erosion** | Particle-based erosion — simulates individual water droplets traveling downslope, carrying and depositing sediment with configurable inertia, erosion rate, and deposition rate |

#### Utility

| Node | Description |
|------|-------------|
| **Preview** | Pass-through node for inserting a preview point mid-graph without affecting values |

---

### Modify Spline

Nodes that take a spline as input and produce a modified spline.

| Node | Description |
|------|-------------|
| **Smooth** | Smooths sharp angles in a spline over configurable iterations |
| **Resample** | Changes the vertex density of a spline |
| **Slice** | Extracts a segment of a spline by start and end index |
| **Splice** | Combines multiple splines from a spline list into one |
| **Displace** | Displaces spline vertices along a direction |
| **Open / Close** | Toggles whether the spline is open or closed |

---

### Import

| Node | Description |
|------|-------------|
| **Import Texture** | Imports a Texture2D asset as a heightmap |
| **Import Spline** | Imports a spline from a scene object |

---

### Export

Export nodes have no output port. They write data to external assets or files when the graph executes.

| Node | Description |
|------|-------------|
| **Export Terrain** | Writes the heightmap to a Unity TerrainData asset |
| **Export Texture** | Saves the heightmap as a PNG or EXR texture asset |
| **Export Mesh** | Generates and saves a mesh asset from the heightmap |
| **Export Spline** | Exports spline vertex data to a file |
| **Export Stamp** | Saves the heightmap as a reusable stamp for use in Stamp nodes |

---

## How It Works

Nodes are connected in a directed acyclic graph. When any node's parameters or connections change, the graph re-evaluates in topological order — upstream nodes first — so each node always receives up-to-date inputs. Results are cached per-node and invalidated only when inputs change, keeping re-evaluation fast.

All heightmaps are stored as GPU RenderTextures and processed via HLSL compute shaders. This means even complex multi-node graphs with large resolutions update in real time in the editor.

### Data Types

- **HeightGrid** — a heightmap stored as a floating-point RenderTexture
- **SplineWrapper** — a single spline curve
- **SplineListWrapper** — a collection of splines (e.g. contour lines)

### Example Pipeline

```
Perlin Noise → Blur → Normalize → Stamp (with mask) → Relax → Export Terrain
```

1. **Perlin Noise** generates a base heightmap
2. **Blur** smooths the raw noise
3. **Normalize** remaps the range to [0, 1]
4. **Stamp** layers a detail pattern onto the terrain using a mask
5. **Relax** applies a final smoothing pass
6. **Export Terrain** writes the result to a Unity TerrainData asset
