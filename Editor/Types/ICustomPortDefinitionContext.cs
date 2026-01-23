namespace Indiecat.TerrainGraph.Editor
{
    public interface ICustomPortDefinitionContext
    {
        ICustomInputPortBuilder<T> AddInputPort<T>(string name);
        ICustomOutputPortBuilder<T> AddOutputPort<T>(string name);
    }
}
