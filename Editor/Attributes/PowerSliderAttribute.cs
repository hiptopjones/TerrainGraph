using System;

namespace Indiecat.TerrainGraph.Editor
{
    [AttributeUsage(AttributeTargets.Field)]
    public class PowerSliderAttribute : Attribute
    {
        public float Power;

        public PowerSliderAttribute()
        {
            Power = 2;
        }

        public PowerSliderAttribute(float power)
        {
            Power = power;
        }
    }
}
