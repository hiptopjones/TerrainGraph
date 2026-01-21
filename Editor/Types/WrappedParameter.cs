namespace Indiecat.TerrainGraph.Editor
{
    [System.Serializable]
    public class WrappedParameter<T>
    {
        public T Value;

        #region Implicit Operators
        public static implicit operator T(WrappedParameter<T> wrapper)
        {
            return wrapper.Value;
        }

        public static implicit operator WrappedParameter<T>(T value)
        {
            return new WrappedParameter<T>(value);
        }
        #endregion

        public WrappedParameter(T value)
        {
            Value = value;
        }
    }
}