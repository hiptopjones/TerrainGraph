using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Indiecat.TerrainGraph.Editor
{
    [Serializable]
    public class ParticleErosionNode 
        : BaseNode<ParticleErosionNode.OptionValues, ParticleErosionNode.InputValues, HeightGrid>
    {
        public class OptionValues : OptionValuesBase
        {
        }

        public class InputValues : InputValuesBase
        {
            [Passthru]
            public HeightGrid Grid;

            [RangeValue(10, 500_000), DefaultValue(50_000)]
            [Slider]
            [DisplayName("Particles")]
            public int ParticleCount;

            [RangeValue(1, 200), DefaultValue(50)]
            [Slider]
            [DisplayName("Iterations")]
            public int IterationCount;

            [RangeValue(0.01f, 1), DefaultValue(0.1f)]
            [PowerSlider]
            public float ErosionRate;

            [RangeValue(0.01f, 1), DefaultValue(0.3f)]
            [PowerSlider]
            public float DepositionRate;

            [RangeValue(0, 0.5f), DefaultValue(0.02f)]
            [PowerSlider]
            public float EvaporationRate;

            [RangeValue(0, 0.5f), DefaultValue(0.05f)]
            [PowerSlider]
            public float Inertia;

            public int Seed;
        }

        private const float GRAVITY = 4.0f;
        private const float SEDIMENT_CAPACITY = 4.0f;
        private const float MIN_CAPACITY = 0.0001f;
        private const float MAX_SPEED = 3.0f;
        private const float START_WATER = 1.0f;
        private const float START_SPEED = 1.0f;
        private const float BOUNDARY_MARGIN = 2.0f;

        [StructLayout(LayoutKind.Sequential)]
        struct Particle
        {
            public Vector2 pos;
            public Vector2 vel;
            public float water;
            public float sediment;
            public uint alive;
            public uint pad;
        }

        protected override bool TryExecuteNodeInternal()
        {
            ComputeBuffer particlesBuffer = null;
            ComputeBuffer heightsBuffer = null;

            try
            {
                var inputGrid = Inputs.Grid;
                var particleCount = Inputs.ParticleCount;
                var iterationCount = Inputs.IterationCount;
                var erosionRate = Inputs.ErosionRate;
                var depositionRate = Inputs.DepositionRate;
                var evaporationRate = Inputs.EvaporationRate;
                var inertia = Inputs.Inertia;
                var seed = Inputs.Seed;

                var size = inputGrid.Size;
                var inputTexture = inputGrid.RenderTexture;

                var outputTexture = GetOrCreateNodeRenderTexture(size);

                if (!ComputeHelpers.TryLoadComputeShader(nameof(ParticleErosionNode), out var shader))
                {
                    return false;
                }

                // Bind common stuff
                shader.SetInt("_Size", size);
                shader.SetInt("_ParticleCount", particleCount);
                shader.SetInt("_FixedPointScale", 65536);
                shader.SetFloat("_DeltaTime", 1);
                shader.SetFloat("_EvaporationRate", evaporationRate);
                shader.SetFloat("_DepositionRate", depositionRate);
                shader.SetFloat("_ErosionRate", erosionRate);
                shader.SetFloat("_Inertia", inertia);
                shader.SetFloat("_Gravity", GRAVITY);
                shader.SetFloat("_SedimentCapacity", SEDIMENT_CAPACITY);
                shader.SetFloat("_MinCapacity", MIN_CAPACITY);
                shader.SetFloat("_MaxSpeed", MAX_SPEED);
                shader.SetFloat("_StartWater", START_WATER);
                shader.SetFloat("_StartSpeed", START_SPEED);
                shader.SetFloat("_BoundaryMargin", BOUNDARY_MARGIN);

                int size2 = size * size;

                // IMPORT HEIGHTS
                heightsBuffer = new ComputeBuffer(size2, sizeof(int));

                var importKernel = shader.FindKernel("CSMain_ImportHeights");
                shader.SetBuffer(importKernel, "_FixedPointHeights", heightsBuffer);
                shader.SetTexture(importKernel, "_InTexture", inputTexture);

                var importGroups = Mathf.CeilToInt(size / 8.0f);
                shader.Dispatch(importKernel, importGroups, importGroups, 1);

                // INITIALIZE PARTICLES
                int stride = Marshal.SizeOf(typeof(Particle));
                particlesBuffer = new ComputeBuffer(particleCount, stride);

                var initKernel = shader.FindKernel("CSMain_InitializeParticles");
                shader.SetBuffer(initKernel, "_Particles", particlesBuffer);

                int initGroups = Mathf.CeilToInt(size2 / 256.0f);
                shader.Dispatch(initKernel, initGroups, 1, 1);

                // STEP PARTICLES
                var stepKernel = shader.FindKernel("CSMain_StepParticles");
                shader.SetBuffer(stepKernel, "_Particles", particlesBuffer);
                shader.SetBuffer(stepKernel, "_FixedPointHeights", heightsBuffer);

                int stepGroups = Mathf.CeilToInt(size2 / 256.0f);

                for (int i = 0; i < iterationCount; i++)
                {
                    shader.Dispatch(stepKernel, stepGroups, 1, 1);
                }

                // EXPORT HEIGHTS
                var exportKernal = shader.FindKernel("CSMain_ExportHeights");
                shader.SetBuffer(exportKernal, "_FixedPointHeights", heightsBuffer);
                shader.SetTexture(exportKernal, "_OutTexture", outputTexture);

                int exportGroups = Mathf.CeilToInt(size / 8.0f);
                shader.Dispatch(exportKernal, exportGroups, exportGroups, 1);

                var outputGrid = new HeightGrid(size);

                outputGrid.RenderTexture = outputTexture;
                outputGrid.VersionHash = Inputs.VersionHash;

                CacheData.Output = outputGrid;
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return false;
            }
            finally
            {
                if (heightsBuffer != null)
                {
                    heightsBuffer.Release();
                    heightsBuffer = null;
                }

                if (particlesBuffer != null)
                {
                    particlesBuffer.Release();
                    particlesBuffer = null;
                }
            }
        }
    }
}