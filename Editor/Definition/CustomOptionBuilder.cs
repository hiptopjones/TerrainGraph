using System;
using Unity.GraphToolkit.Editor;
using static Unity.GraphToolkit.Editor.Node;

namespace Indiecat.TerrainGraph.Editor
{
    public class CustomOptionBuilder<TOption> : ICustomOptionBuilder<TOption>
    {
        private IOptionDefinitionContext _originalContext;

        private string _name;

        private bool _hasDisplayName;
        private string _displayName;

        private TOption _defaultValue;
        private bool _hasDefaultValue;

        private bool _hasRange;
        private Tuple<TOption, TOption> _range;

        public CustomOptionBuilder(string name, IOptionDefinitionContext context)
        {
            _name = name;
            _originalContext = context;
        }

        public ICustomOptionBuilder<TOption> WithDisplayName(string displayName)
        {
            _displayName = displayName;
            _hasDisplayName = true;
            return this;
        }

        public ICustomOptionBuilder<TOption> WithDefaultValue(TOption defaultValue)
        {
            _defaultValue = defaultValue;
            _hasDefaultValue = true;
            return this;
        }

        public ICustomOptionBuilder<TOption> WithRange(TOption min, TOption max)
        {
            _range = new Tuple<TOption, TOption>(min, max);
            _hasRange = true;
            return this;
        }

        public INodeOption Build()
        {
            var builder = _originalContext.AddOption<TOption>(_name);

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