using System;
using Unity.GraphToolkit.Editor;
using static Unity.GraphToolkit.Editor.Node;

namespace Indiecat.TerrainGraph.Editor
{
    public class CustomInputPortBuilder<TPort> : ICustomInputPortBuilder<TPort>
    {
        private IPortDefinitionContext _originalContext;

        private string _name;

        private bool _hasDisplayName;
        private string _displayName;

        private TPort _defaultValue;
        private bool _hasDefaultValue;

        private bool _hasRange;
        private Tuple<TPort, TPort> _range;

        public CustomInputPortBuilder(string name, IPortDefinitionContext context)
        {
            _name = name;
            _originalContext = context;
        }

        public ICustomInputPortBuilder<TPort> WithDisplayName(string displayName)
        {
            _displayName = displayName;
            _hasDisplayName = true;
            return this;
        }

        public ICustomInputPortBuilder<TPort> WithDefaultValue(TPort defaultValue)
        {
            _defaultValue = defaultValue;
            _hasDefaultValue = true;
            return this;
        }

        public ICustomInputPortBuilder<TPort> WithRange(TPort min, TPort max)
        {
            _range = new Tuple<TPort, TPort>(min, max);
            _hasRange = true;
            return this;
        }

        public IPort Build()
        {
            var builder = _originalContext.AddInputPort<TPort>(_name);

            if (_hasDisplayName)
            {
                builder.WithDisplayName(_displayName);
            }

            if (_hasDefaultValue)
            {
                builder.WithDefaultValue(_defaultValue);
            }

            return builder.Build();
        }
    }
}