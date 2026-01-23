using System;
using Unity.GraphToolkit.Editor;
using static Unity.GraphToolkit.Editor.Node;

namespace Indiecat.TerrainGraph.Editor
{
    public class CustomOptionBuilder<T> : ICustomOptionBuilder<T>
    {
        private IOptionDefinitionContext _originalContext;

        private string _name;

        private bool _hasDisplayName;
        private string _displayName;
        
        private T _defaultValue;
        private bool _hasDefaultValue;

        private bool _hasRange;
        private Tuple<T, T> _range;

        public CustomOptionBuilder(string name, IOptionDefinitionContext context)
        {
            _name = name;
            _originalContext = context;
        }

        public ICustomOptionBuilder<T> WithDisplayName(string displayName)
        {
            _displayName = displayName;
            _hasDisplayName = true;
            return this;
        }

        public ICustomOptionBuilder<T> WithDefaultValue(T defaultValue)
        {
            _defaultValue = defaultValue;
            _hasDefaultValue = true;
            return this;
        }

        public ICustomOptionBuilder<T> WithRange(T min, T max)
        {
            _range = new Tuple<T, T>(min, max);
            _hasRange = true;
            return this;
        }

        public INodeOption Build()
        {
            var builder = _originalContext.AddOption<T>(_name);

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