using static Unity.GraphToolkit.Editor.Node;

namespace Indiecat.TerrainGraph.Editor
{
    public static class DefinitionContextExtensions
    {
        // TODO: AddAllOptions() to auto-populate with all fields
        public static ICustomOptionDefinitionContext<TOptions> UseType<TOptions>(this IOptionDefinitionContext context)
        {
            var customContext = new CustomOptionDefinitionContext<TOptions>(context);
            return customContext;
        }
    }
}
