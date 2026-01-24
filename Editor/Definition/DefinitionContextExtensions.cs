using static Unity.GraphToolkit.Editor.Node;

namespace Indiecat.TerrainGraph.Editor
{
    public static class DefinitionContextExtensions
    {
        public static ICustomOptionDefinitionContext<TOptionValues> UseType<TOptionValues>(this IOptionDefinitionContext context)
        {
            var customContext = new CustomOptionDefinitionContext<TOptionValues>(context);
            return customContext;
        }

        public static ICustomInputPortDefinitionContext<TInputValues> UseType<TInputValues>(this IPortDefinitionContext context)
        {
            var customContext = new CustomInputPortDefinitionContext<TInputValues>(context);
            return customContext;
        }
    }
}
