using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Indiecat.TerrainGraph.Editor
{
    [System.Serializable]
    public class RangedFloatParameter : WrappedParameter<float>
    {
        public float Min;
        public float Max;

        public List<VisualElement> Containers = new();

        #region Implicit Operators
        public static implicit operator float(RangedFloatParameter wrapper)
        {
            return wrapper.Value;
        }

        public static implicit operator RangedFloatParameter(float value)
        {
            return new RangedFloatParameter(value);
        }
        #endregion

        public RangedFloatParameter(float value)
            : base(value)
        {
        }

        public void UpdateRange(float min, float max)
        {
            Min = min;
            Max = max;

            if (Containers == null)
            {
                // Happens during serialization?
                return;
            }

            foreach (var container in Containers)
            {
                var slider = container.Q<Slider>();
                if (slider != null)
                {
                    slider.lowValue = min;
                    slider.highValue = max;
                }
            }
        }
    }
}