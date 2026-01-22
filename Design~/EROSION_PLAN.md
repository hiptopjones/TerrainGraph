# Particle-Based Erosion Node - Algorithm Plan

## Overview

Each compute thread simulates one water droplet from spawn to death. The droplet flows downhill, eroding terrain when it has capacity and depositing when oversaturated or slowing down.

---

## Parameters

| Parameter | Type | Default | Range | Purpose |
|-----------|------|---------|-------|---------|
| Droplets | int | 50000 | 1000-500000 | Number of droplets per execution |
| Erosion | float | 0.3 | 0.01-1.0 | Erosion strength multiplier |
| Deposition | float | 0.3 | 0.01-1.0 | Deposition strength multiplier |
| Inertia | float | 0.05 | 0.0-1.0 | 0=follow gradient, 1=ignore gradient |
| Evaporation | float | 0.02 | 0.0-0.1 | Water loss per step |

### Internal Constants (not exposed)
| Constant | Value | Rationale |
|----------|-------|-----------|
| MAX_LIFETIME | 64 | Hard cap on droplet steps to prevent infinite loops |
| MIN_SLOPE | 0.0001 | Prevents division by zero in capacity calc |
| INITIAL_WATER | 1.0 | Starting water volume |
| INITIAL_SPEED | 1.0 | Starting speed |
| GRAVITY | 4.0 | Acceleration factor from height differences |
| CAPACITY_FACTOR | 8.0 | Multiplier for sediment capacity |
| MIN_CAPACITY | 0.01 | Floor for capacity to allow some erosion on flat terrain |
| EROSION_RADIUS | 3.0 | Radius for distributing erosion/deposition |

---

## Algorithm: Per-Droplet Simulation

```
INITIALIZE:
  pos = random position in [1, size-2] (away from edges)
  dir = (0, 0)
  speed = INITIAL_SPEED
  water = INITIAL_WATER
  sediment = 0

FOR step = 0 TO MAX_LIFETIME:

  1. CALCULATE GRADIENT at pos using bilinear interpolation
     - Sample heights at 4 corners: (ix, iy), (ix+1, iy), (ix, iy+1), (ix+1, iy+1)
     - Compute gradient in x: (h_right - h_left)
     - Compute gradient in y: (h_up - h_down)

  2. UPDATE DIRECTION
     - newDir = dir * inertia + gradient * (1 - inertia)
     - Normalize newDir (if length > 0)
     - If length == 0: terminate (in a pit)

  3. MOVE DROPLET
     - oldPos = pos
     - pos = pos + newDir
     - If pos out of bounds [1, size-2]: terminate

  4. CALCULATE HEIGHT DIFFERENCE
     - oldHeight = bilinear sample at oldPos
     - newHeight = bilinear sample at pos
     - deltaHeight = newHeight - oldHeight

  5. UPDATE SPEED
     - speed = sqrt(speed² + deltaHeight * GRAVITY)
     - Clamp speed >= 0

  6. CALCULATE SEDIMENT CAPACITY
     - slope = max(abs(deltaHeight), MIN_SLOPE)
     - capacity = max(slope * speed * water * CAPACITY_FACTOR, MIN_CAPACITY)

  7. ERODE OR DEPOSIT
     IF sediment > capacity:
       # Deposit excess sediment
       depositAmount = (sediment - capacity) * deposition
       sediment -= depositAmount
       DEPOSIT(oldPos, depositAmount)
     ELSE:
       # Erode terrain
       erodeAmount = (capacity - sediment) * erosion
       erodeAmount = min(erodeAmount, -deltaHeight)  # Don't erode below new height
       sediment += erodeAmount
       ERODE(oldPos, erodeAmount)

  8. EVAPORATE
     - water *= (1 - evaporation)
     - If water < 0.001: terminate

END FOR

# Deposit any remaining sediment at final position
DEPOSIT(pos, sediment)
```

---

## Edge Cases & Conflict Resolution

### 1. Race Conditions (Critical)

**Problem:** Multiple droplets may erode/deposit at overlapping positions simultaneously.

**Resolution:** Use atomic operations with fixed-point conversion.
- Convert height floats to integers: `int iHeight = (int)(height * PRECISION_SCALE)`
- Use `InterlockedAdd` for thread-safe modification
- Convert back after all droplets complete
- PRECISION_SCALE = 10000 (4 decimal places)

**Trade-off:** Slight precision loss, but guarantees correctness. Heights should stay in reasonable range (0-100) to avoid overflow.

```hlsl
// Instead of: height -= erodeAmount
// Use:
int delta = (int)(erodeAmount * PRECISION_SCALE);
InterlockedAdd(_HeightBuffer[index], -delta);
```

**Alternative considered:** Non-atomic writes accepting race conditions. Rejected because erosion accumulation errors compound visibly.

---

### 2. Boundary Conditions

**Problem:** Droplet may move outside valid texture coordinates.

**Resolution:** Terminate droplet when position leaves safe zone [1, size-2].
- Safe zone keeps bilinear sampling valid (needs 4 neighbors)
- Natural behavior: water flows off the edge

**Edge sampling:** When calculating gradient near boundaries, clamp neighbor coordinates.

---

### 3. Local Minimum (Pit/Lake)

**Problem:** Gradient is zero or nearly zero; droplet gets stuck.

**Resolution:** Multi-part handling:
1. If gradient length < 0.0001 after direction update: deposit remaining sediment, terminate
2. If speed drops below 0.001: deposit remaining sediment, terminate
3. Natural deposition when slowing fills pits over many iterations

**Why not random jitter?** Jitter creates unnatural patterns. Better to let pits fill naturally with sediment.

---

### 4. Uphill Movement

**Problem:** Inertia can carry droplet uphill, causing `deltaHeight > 0`.

**Resolution:**
- Speed calculation: `speed² + deltaHeight * GRAVITY` can go negative if going uphill
- Take sqrt of max(0, value) to handle this
- When speed approaches zero, increased deposition naturally occurs
- Droplet will reverse direction on next step (gradient pulls it back)

```hlsl
speed = sqrt(max(0, speed * speed - deltaHeight * GRAVITY));
```

---

### 5. Excessive Erosion

**Problem:** Eroding more material than exists at a location.

**Resolution:**
- Clamp erosion to height difference: `erodeAmount = min(erodeAmount, -deltaHeight)`
- This prevents creating negative heights or unrealistic gouges
- If droplet is going downhill (deltaHeight < 0), max erosion = drop amount

**Additional safeguard:** Clamp final height to >= 0 in the atomic operation.

---

### 6. Bilinear Distribution for Erosion/Deposition

**Problem:** Applying erosion to single cell creates blocky artifacts.

**Resolution:** Distribute erosion/deposition to surrounding cells using erosion brush with falloff.

```hlsl
void ApplyErosionBrush(float2 pos, float amount, float radius) {
    int2 center = (int2)pos;
    float weightSum = 0;

    // First pass: calculate total weight
    for (int dy = -radius; dy <= radius; dy++) {
        for (int dx = -radius; dx <= radius; dx++) {
            float dist = length(float2(dx, dy) - frac(pos));
            if (dist < radius) {
                float weight = 1 - dist / radius;  // Linear falloff
                weightSum += weight;
            }
        }
    }

    // Second pass: apply weighted erosion
    for (int dy = -radius; dy <= radius; dy++) {
        for (int dx = -radius; dx <= radius; dx++) {
            int2 coord = center + int2(dx, dy);
            if (InBounds(coord)) {
                float dist = length(float2(dx, dy) - frac(pos));
                if (dist < radius) {
                    float weight = (1 - dist / radius) / weightSum;
                    ApplyHeightDelta(coord, amount * weight);
                }
            }
        }
    }
}
```

**Trade-off:** More GPU work per erosion operation, but much smoother results.

---

### 7. Fixed-Point Precision

**Problem:** `InterlockedAdd` requires integers, but heights are floats.

**Resolution:**
- Use separate `RWBuffer<int>` for atomic height modifications
- PRECISION_SCALE = 10000 gives ±214,748 range with int32
- Heights typically 0-1 normalized, so this is plenty
- Final pass converts back to float texture

**Overflow protection:**
```hlsl
// Clamp delta to prevent overflow
delta = clamp(delta, -1000000, 1000000);
```

---

### 8. Random Number Generation

**Problem:** GPU needs deterministic but varied random positions per droplet.

**Resolution:** Use hash-based RNG seeded with droplet index and iteration.

```hlsl
// Wang hash for good distribution
uint WangHash(uint seed) {
    seed = (seed ^ 61) ^ (seed >> 16);
    seed *= 9;
    seed = seed ^ (seed >> 4);
    seed *= 0x27d4eb2d;
    seed = seed ^ (seed >> 15);
    return seed;
}

float Random(uint seed) {
    return WangHash(seed) / 4294967295.0;
}
```

Seed = `dropletIndex * 17 + iterationIndex * 31` for variation across iterations.

---

## Shader Structure

```hlsl
#pragma kernel CSMain
#pragma kernel CSApplyHeightDeltas

// === CSMain: Simulate droplets ===
// Input: _InHeightTexture (current terrain)
// Output: _HeightDeltaBuffer (atomic int buffer for height changes)
// Each thread = one droplet

// === CSApplyHeightDeltas: Finalize ===
// Input: _InHeightTexture, _HeightDeltaBuffer
// Output: _OutHeightTexture
// Each thread = one pixel
// Applies accumulated deltas and resets buffer
```

### Two-Kernel Approach Rationale

**Why not single kernel?**
- Droplets read from `_InHeightTexture` during simulation
- If we wrote directly to texture, later droplets would see partially eroded terrain
- This causes order-dependent results and visual artifacts

**Solution:**
1. First kernel: All droplets accumulate changes to integer buffer
2. Second kernel: Apply all changes at once to create output texture

---

## C# Node Execution Flow

```
1. Validate inputs
2. Check cache (skip if inputs unchanged)
3. Load compute shader
4. Create buffers:
   - _HeightDeltaBuffer (RWBuffer<int>, size * size)
   - Clear to zero
5. First dispatch: CSMain
   - Thread groups: ceil(dropletCount / 256)
   - Each thread simulates one droplet
6. Second dispatch: CSApplyHeightDeltas
   - Thread groups: ceil(size / 8) x ceil(size / 8)
   - Applies deltas, outputs final texture
7. Release buffers
8. Store output in cache
```

---

## Potential Issues to Monitor

1. **Performance with large droplet counts:** 50,000 droplets × 64 steps = 3.2M iterations. Should be fine on modern GPUs but may need adjustment.

2. **Erosion radius performance:** Radius of 3 means up to 49 atomic operations per erosion. Consider reducing to 2 for performance.

3. **Memory for height delta buffer:** size² × 4 bytes. For 2048×2048 = 16MB. Acceptable.

4. **Determinism:** Hash-based RNG is deterministic for same seed. Results reproducible.

---

## Testing Checklist

- [ ] Flat terrain: Should see minimal change
- [ ] Sloped terrain: Should see channels form
- [ ] Single peak: Should see radial drainage
- [ ] Pit/bowl: Should fill with sediment
- [ ] Edge behavior: No artifacts at boundaries
- [ ] Parameter extremes: No crashes or visual glitches
- [ ] Performance: Acceptable speed for typical use
