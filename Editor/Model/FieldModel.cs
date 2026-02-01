using System;
using System.Collections.Generic;

namespace Indiecat.TerrainGraph.Editor
{
    public class FieldModel
    {
        public ClassModel ClassModel;

        public string Name;
        public string PortName;
        public string DisplayName;
        public Type FieldType;
        public Type DeclaringType;

        public bool IsPassthru;

        public bool IsIgnored;
        public bool IsCustom;

        // parameter 0: node object
        // parameter 1: predicate result
        public Func<object, bool> IsIncluded;

        // parameter 0: class object
        // parameter 1: field value
        public Func<object, object> GetValue;
        public Action<object, object> SetValue;

        public object DefaultValue;

        public float? Min;
        public float? Max;

        public List<IValidationRule> Rules = new();

        public bool UseLinearSlider;
        public bool UsePowerSlider;
        public float PowerSliderPower;
    }
}
