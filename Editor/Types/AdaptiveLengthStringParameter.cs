namespace Indiecat.TerrainGraph.Editor
{
    [System.Serializable]
    public class AdaptiveLengthStringParameter : WrappedParameter<string>
    {
        #region Implicit Operators
        public static implicit operator string(AdaptiveLengthStringParameter wrapper)
        {
            return wrapper.Value;
        }

        public static implicit operator AdaptiveLengthStringParameter(string value)
        {
            return new AdaptiveLengthStringParameter(value);
        }
        #endregion

        public AdaptiveLengthStringParameter(string value)
            : base(value)
        {
        }
    }
}