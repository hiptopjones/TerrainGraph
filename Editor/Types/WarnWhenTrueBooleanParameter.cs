namespace Indiecat.TerrainGraph.Editor
{
    [System.Serializable]
    public class WarnWhenTrueBooleanParameter : WrappedParameter<bool>
    {
        #region Implicit Operators
        public static implicit operator bool(WarnWhenTrueBooleanParameter wrapper)
        {
            return wrapper.Value;
        }

        public static implicit operator WarnWhenTrueBooleanParameter(bool value)
        {
            return new WarnWhenTrueBooleanParameter(value);
        }
        #endregion

        public WarnWhenTrueBooleanParameter(bool value)
            : base(value)
        {
        }
    }
}