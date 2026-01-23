using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Indiecat.TerrainGraph.Editor
{
    [System.Serializable]
    public class RangedIntParameter : WrappedParameter<int>
    {
        public int Min;
        public int Max;

        public List<VisualElement> Containers = new();

        #region Implicit Operators
        public static implicit operator int(RangedIntParameter wrapper)
        {
            return wrapper.Value;
        }

        public static implicit operator RangedIntParameter(int value)
        {
            return new RangedIntParameter(value);
        }
        #endregion

        public RangedIntParameter(int value)
            : base(value)
        {
        }

        public void UpdateRange(int min, int max)
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
                var slider = container.Q<SliderInt>();
                if (slider != null)
                {
                    slider.lowValue = min;
                    slider.highValue = max;
                }
            }
        }
    }
}