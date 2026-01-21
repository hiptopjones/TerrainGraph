namespace Indiecat.TerrainGraph.Editor
{
    [System.Serializable]
    public class NormalizedFloatParameter : WrappedParameter<float>
    {
        #region Implicit Operators
        public static implicit operator float(NormalizedFloatParameter wrapper)
        {
            return wrapper.Value;
        }

        public static implicit operator NormalizedFloatParameter(float value)
        {
            return new NormalizedFloatParameter(value);
        }
        #endregion

        public NormalizedFloatParameter(float value)
            : base(value)
        {
        }
    }
}