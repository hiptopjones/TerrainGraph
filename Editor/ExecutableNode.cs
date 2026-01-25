using System;
using System.Linq.Expressions;
using System.Reflection;
using Unity.GraphToolkit.Editor;
using UnityEditor;
using UnityEngine;

namespace Indiecat.TerrainGraph.Editor
{
    public class OptionValuesBase
    {
        [DefaultValue(true)]
        [IgnoreIf("!HasOutputPort")]
        public bool IsPreviewEnabled;

        public bool IsNodeDisabled;

        [Ignore] public WarningBanner Warning;
        [Ignore] public int VersionHash;
    }

    public class InputValuesBase
    {
        [Ignore] public PreviewImage Preview;
        [Ignore] public int VersionHash;
    }

    public abstract class ExecutableNode<TOptionValues, TInputValues, TResult> : Node,
        IValidatableNode,
        IExecutableNode,
        IEvaluatableNode<TResult>,
        ICacheableNode<TResult>,
        IPreviewableNode
        where TOptionValues : OptionValuesBase
        where TInputValues : InputValuesBase
        where TResult : class, IVersionedObject
    {
        public CacheData<TResult> CacheData { get; set; } = new();

        protected abstract bool TryExecuteNodeInternal();

        protected TOptionValues Options;
        protected TInputValues Inputs;

        private const string NODE_OUTPUT_VALUE_ID = "NodeOutput";

        // LIFECYCLE - important to avoid races, unnecessary work
        //
        // Public entrypoints are
        //   - OnDefineOptions - clears options, clears input
        //   - OnDefinePorts - creates and reads options, clears input
        //   - TryValidateNode - reads options, creates input
        //   - TryPreviewNode - reads options, reads input
        //   - TryGetOutputValue - reads options, reads input
        //   - TryExecuteNode - reads options, reads input
        //   - TryExportNode - reads options, reads input

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            Options = null;
            Inputs = null;

            var customContext = context.UseType<TOptionValues>();

            if (typeof(TOptionValues) != typeof(OptionValuesBase))
            {
                OnDefineOptions(customContext);
            }

            if (HasOutputPort())
            {
                customContext.BuildOption(x => x.IsPreviewEnabled);
            }

            customContext.BuildOption(x => x.IsNodeDisabled);
            customContext.BuildOption(x => x.Warning);
        }

        protected virtual void OnDefineOptions(ICustomOptionDefinitionContext<TOptionValues> context)
        {
            var bindingFlags =
                BindingFlags.Public |
                BindingFlags.Instance |
                BindingFlags.DeclaredOnly; // No inherited members

            var fields = typeof(TOptionValues).GetFields(bindingFlags);
            foreach (var field in fields)
            {
                CallBuilderMethodWithExpression(context, nameof(context.BuildOption), field);
            }
        }

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            Inputs = null;

            if (!TryUpdateOptionValues())
            {
                return;
            }

            // Inputs
            var inputContext = context.UseType<TInputValues>();

            OnDefineInputPorts(inputContext);

            if (HasOutputPort() && Options.IsPreviewEnabled)
            {
                inputContext.BuildInputPort(x => x.Preview);
            }

            // Output
            if (HasOutputPort())
            {
                string displayName = GetOutputPortDisplayName();

                context.AddOutputPort<TResult>(NODE_OUTPUT_VALUE_ID)
                    .WithDisplayName(displayName)
                    .Build();
            }
        }

        protected virtual void OnDefineInputPorts(ICustomInputPortDefinitionContext<TInputValues> context)
        {
            var bindingFlags =
                BindingFlags.Public |
                BindingFlags.Instance |
                BindingFlags.DeclaredOnly; // No inherited members

            var fields = typeof(TInputValues).GetFields(bindingFlags);
            foreach (var field in fields)
            {
                var ignoreIfAttribute = field.GetCustomAttribute<IgnoreIfAttribute>();
                if (ignoreIfAttribute != null && IsPredicateTrue(ignoreIfAttribute.PredicateName))
                {
                    continue;
                }

                CallBuilderMethodWithExpression(context, nameof(context.BuildInputPort), field);
            }
        }

        // Public makes it visible when reflecting subclasses
        public bool HasOutputPort() => typeof(TResult) != typeof(NullOutput);

        public bool TryValidateNode(GraphLogger graphLogger = null)
        {
            if (Options == null)
            {
                graphLogger?.LogError("Options is null");
                return false;
            }

            if (Options.IsNodeDisabled)
            {
                TrySetWarningBanner("DISABLED");
                return true;
            }

            TrySetWarningBanner(null);
            return TryUpdateInputValues(graphLogger);
        }

        public bool TryGetOutputValue(IPort _, out TResult value)
        {
            if (!HasOutputPort())
            {
                // No output port is generated for this type of node
                // This should never be called because there should be no connections
                throw new Exception("Invalid operation - no output port");
            }

            if (Options.IsNodeDisabled)
            {
                // TODO: Fix this
                //return PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_GRID_ID, out value);
                value = null;
                return false;
            }

            if (!TryExecuteNode())
            {
                value = null;
                return false;
            }

            value = CacheData.Output;
            return true;
        }

        public bool TryExecuteNode()
        {
            if (Inputs == null)
            {
                // Not in valid state
                CacheData.Output = null;
                return false;
            }

            if (CacheData.Output != null && CacheData.Output.VersionHash == Inputs.VersionHash)
            {
                // Node is already up-to-date
                return true;
            }

            // Clear the cached values in case there's an early exit below
            CacheData.Output = null;

            var startTime = DateTime.Now;
            if (TryExecuteNodeInternal())
            {
                CacheData.Output.ExecutionTime = (float)(DateTime.Now - startTime).TotalSeconds;
                return true;
            }

            return false;
        }

        private bool TryUpdateOptionValues(GraphLogger graphLogger = null)
        {
            return TryGetNodeOptions(graphLogger, out Options);
        }

        private bool TryGetNodeOptions(GraphLogger graphLogger, out TOptionValues options)
        {
            options = null;

            var tempOptions = Activator.CreateInstance<TOptionValues>();

            var fields = typeof(TOptionValues).GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (var field in fields)
            {
                if (field.GetCustomAttribute<IgnoreAttribute>() != null)
                {
                    continue;
                }

                var ignoreIfAttribute = field.GetCustomAttribute<IgnoreIfAttribute>();
                if (ignoreIfAttribute != null && IsPredicateTrue(ignoreIfAttribute.PredicateName))
                {
                    continue;
                }

                var optionName = NodeHelpers.GetOptionName(field.Name);
                var nodeOption = GetNodeOptionByName(optionName);

                var fieldType = field.FieldType;

                // nodeOption.TryGetValue<bool>(out var isPreviewEnabled)
                var method = typeof(INodeOption)
                    .GetMethod(nameof(nodeOption.TryGetValue))
                    .MakeGenericMethod(fieldType);

                var parameters = new object[] { null };

                if (!(bool)method.Invoke(nodeOption, parameters))
                {
                    Debug.LogError($"Unable to get option value: {optionName}");
                    return false;
                }

                var fieldValue = Convert.ChangeType(parameters[0], fieldType);

                field.SetValue(tempOptions, fieldValue);
            }

            options = tempOptions;
            return true;
        }

        private bool TryUpdateInputValues(GraphLogger graphLogger = null)
        {
            Inputs = null;

            if (!TryGetInputValues(graphLogger, out TInputValues tempInputs))
            {
                graphLogger?.LogError("Upstream failure", this);
                return false;
            }

            if (!TryValidateInputValues(tempInputs, graphLogger))
            {
                graphLogger?.LogError("Validation failed", this);
                return false;
            }

            Inputs = tempInputs;
            return true;
        }

        private bool TryValidateInputValues(TInputValues inputs, GraphLogger graphLogger = null)
        {
            var isValid = true;

            var fields = typeof(TInputValues).GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (var field in fields)
            {
                if (field.GetCustomAttribute<IgnoreAttribute>() != null)
                {
                    continue;
                }

                var ignoreIfAttribute = field.GetCustomAttribute<IgnoreIfAttribute>();
                if (ignoreIfAttribute != null && IsPredicateTrue(ignoreIfAttribute.PredicateName))
                {
                    continue;
                }

                var fieldType = field.FieldType;
                var inputDisplayName = NodeHelpers.GetDisplayName(field);

                if (fieldType.IsEnum)
                {
                    var value = field.GetValue(inputs);
                    if (Enum.IsDefined(fieldType, value))
                    {
                        graphLogger?.LogError($"{inputDisplayName} input invalid", this);
                        isValid = false;
                    }
                }

                if (fieldType == typeof(HeightGrid))
                {
                    var grid = (HeightGrid)field.GetValue(inputs);
                    if (grid == null || !grid.IsValid)
                    {
                        graphLogger?.LogError($"{inputDisplayName} input missing", this);
                        isValid = false;
                    }
                }

                if (fieldType == typeof(SplineWrapper))
                {
                    var spline = (SplineWrapper)field.GetValue(inputs);
                    if (spline == null || !spline.IsValid)
                    {
                        graphLogger?.LogError($"{inputDisplayName} input missing", this);
                        isValid = false;
                    }
                }

                if (fieldType == typeof(SplineListWrapper))
                {
                    var splineList = (SplineListWrapper)field.GetValue(inputs);
                    if (splineList == null || !splineList.IsValid)
                    {
                        graphLogger?.LogError($"{inputDisplayName} input missing", this);
                        isValid = false;
                    }
                }

                if (fieldType == typeof(float) || fieldType == typeof(int))
                {
                    var minAttribute = field.GetCustomAttribute<MinValueAttribute>();
                    if (minAttribute != null)
                    {
                        var rawValue = field.GetValue(inputs);

                        // Use float for both int and float cases, accepting limitations
                        var floatValue = (float)Convert.ChangeType(field.GetValue(inputs), typeof(float));
                        var clampedValue = Mathf.Max(floatValue, minAttribute.Min);

                        if (floatValue != clampedValue)
                        {
                            graphLogger?.LogWarning($"{inputDisplayName} input invalid: {rawValue} (valid: n >= {minAttribute.Min})", this);
                            field.SetValue(inputs, Convert.ChangeType(clampedValue, fieldType));

                            // No failure, just clamp
                        }
                    }

                    var rangeAttribute = field.GetCustomAttribute<RangeValueAttribute>();
                    if (rangeAttribute != null)
                    {
                        var rawValue = field.GetValue(inputs);

                        // Use float for both int and float cases, accepting limitations
                        var floatValue = (float)Convert.ChangeType(field.GetValue(inputs), typeof(float));
                        var clampedValue = Mathf.Clamp(floatValue, rangeAttribute.Min, rangeAttribute.Max);

                        if (floatValue != clampedValue)
                        {
                            graphLogger?.LogWarning($"{inputDisplayName} input invalid: {rawValue} (valid: n >= {rangeAttribute.Min} && n <= {rangeAttribute.Max})", this);
                            field.SetValue(inputs, Convert.ChangeType(clampedValue, fieldType));

                            // No failure, just clamp
                        }
                    }
                }

                var validIfAttributes = field.GetCustomAttributes<ValidIfAttribute>();
                foreach (var validIfAttribute in validIfAttributes)
                {
                    if (!IsPredicateTrue(validIfAttribute.PredicateName, inputs, graphLogger))
                    {
                        isValid = false;
                    }
                }
            }

            return isValid;
        }

        private bool TryGetInputValues(GraphLogger graphLogger, out TInputValues inputs)
        {
            inputs = null;

            var tempInputs = Activator.CreateInstance<TInputValues>();

            var fields = typeof(TInputValues).GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (var field in fields)
            {
                if (field.GetCustomAttribute<IgnoreAttribute>() != null)
                {
                    continue;
                }

                var ignoreIfAttribute = field.GetCustomAttribute<IgnoreIfAttribute>();
                if (ignoreIfAttribute != null && IsPredicateTrue(ignoreIfAttribute.PredicateName))
                {
                    continue;
                }

                var inputPortName = NodeHelpers.GetInputPortName(field.Name);

                var fieldType = field.FieldType;

                // PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_GRID_ID, out temp.Grid)
                var method = typeof(PortEvaluator)
                    .GetMethod(nameof(PortEvaluator.TryEvaluateInputPort))
                    .MakeGenericMethod(fieldType);

                var parameters = new object[] { this, inputPortName, null };

                if (!(bool)method.Invoke(null, parameters))
                {
                    Debug.LogError($"Unable to get port value: {inputPortName}");
                    return false;
                }

                var fieldValue = Convert.ChangeType(parameters[2], fieldType);

                field.SetValue(tempInputs, fieldValue);
            }

            tempInputs.VersionHash = HashCode.Combine(tempInputs.GetHashCode(), Options.GetHashCode());

            inputs = tempInputs;
            return true;
        }

        public bool TryUpdatePreview()
        {
            if (!HasOutputPort())
            {
                // No preview exists for this type of node, treat as up-to-date
                return true;
            }

            // Ensure the node state is up to date
            //  - Needed for standalone nodes that have nobody else to poke them
            //  - Needed to eventually try and cache input values
            if (TryExecuteNode())
            {
                if (!Options.IsPreviewEnabled)
                {
                    // Force generation when next enabled
                    CacheData.PreviewHash = 0;

                    // Preview is disabled, treat as up-to-date
                    return true;
                }

                // TODO: Should not be re-creating this if nothing has changed
                if (TryCreatePreviewTexture(CacheData.Output, out var texture, out var gridSize))
                {
                    if (TrySetPreviewTexture(texture, gridSize))
                    {
                        // Cache generation value to avoid unnecessary updates
                        CacheData.PreviewHash = CacheData.Output.VersionHash;
                        CacheData.PreviewTexture = texture;
                        CacheData.GridSize = gridSize;

                        // Ensure the texture gets cleaned up when the output object goes away
                        TextureMemoryManager.Register(CacheData.Output, texture);

                        // Preview was successfully updated
                        return true;
                    }
                }
            }

            if (Options.IsPreviewEnabled)
            {
                // Make it very clear there is a problem
                var warningTexture = EditorGUIUtility.IconContent("console.warnicon.sml").image;

                // Best effort, not checking the return
                TrySetPreviewTexture(warningTexture, 0);
            }

            // Preview failed to update
            return false;
        }

        private bool TryCreatePreviewTexture(TResult value, out Texture texture, out int gridSize)
        {
            gridSize = 0;
            texture = null;

            if (CacheData.PreviewHash == CacheData.Output.VersionHash)
            {
                // Preview is already up-to-date
                gridSize = CacheData.GridSize;
                texture = CacheData.PreviewTexture;
                return true;
            }

            if (CacheData.Output == null || !CacheData.Output.IsValid)
            {
                // Cached data is not present
                return false;
            }

            if (TextureHelpers.TryCreatePreviewTexture(value, out texture, out gridSize))
            {
                // Successfully created texture
                return true;
            }

            // Unable to create texture
            return false;
        }

        private bool TrySetPreviewTexture(Texture texture, int gridSize)
        {
            try
            {
                var previewPortName = NodeHelpers.GetInputPortName(nameof(InputValuesBase.Preview));

                var previewPort = GetInputPortByName(previewPortName);
                if (previewPort == null)
                {
                    Debug.Log("Unable to get the preview port");
                    return false;
                }

                if (!previewPort.TryGetValue(out PreviewImage previewImage))
                {
                    // Unable to get preview port value, so cannot display anything
                    Debug.LogError("Unable to get the preview image");
                    return false;
                }

                if (previewImage == null)
                {
                    Debug.Log("Preview port image is null");
                    return false;
                }

                previewImage.UpdateTexture(texture, gridSize);

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return false;
            }
        }

        public bool TrySetWarningBanner(string text)
        {
            try
            {
                var warningOptionName = NodeHelpers.GetOptionName(nameof(OptionValuesBase.Warning));

                var warningOption = GetNodeOptionByName(warningOptionName);
                if (warningOption == null)
                {
                    Debug.Log("Unable to get the warning option");
                    return false;
                }

                if (!warningOption.TryGetValue(out WarningBanner warningBanner))
                {
                    // Unable to get warning banner value, so cannot display anything
                    Debug.LogError("Unable to get the warning banner");
                    return false;
                }

                if (warningBanner == null)
                {
                    Debug.Log("Warning banner is null");
                    return false;
                }

                warningBanner.UpdateProperties(text);

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return false;
            }
        }

        public RenderTexture GetOrCreateNodeRenderTexture(int size)
        {
            var texture = CacheData.RenderTexture;

            if (texture == null || texture.width != size)
            {
                if (texture != null)
                {
                    TextureMemoryManager.Unregister(texture);
                }

                texture = TextureHelpers.CreateRenderTexture(size, RenderTextureFormat.RFloat);
                TextureMemoryManager.Register(this, texture);
            }

            CacheData.RenderTexture = texture;

            return texture;
        }

        private void CallBuilderMethodWithExpression<TContext>(TContext context, string methodName, FieldInfo field)
        {
            var parameterExpression = Expression.Parameter(field.DeclaringType, "x");
            var fieldExpression = Expression.Field(parameterExpression, field);

            var funcType = typeof(Func<,>).MakeGenericType(field.DeclaringType, field.FieldType);
            var lambda = Expression.Lambda(funcType, fieldExpression, parameterExpression);

            var method = context.GetType()
                .GetMethod(methodName)
                .MakeGenericMethod(field.FieldType);

            method.Invoke(context, new object[] { lambda });
        }

        private string GetOutputPortDisplayName()
        {
            string displayName = "Unknown";

            if (typeof(TResult) == typeof(HeightGrid))
            {
                displayName = "Grid";
            }
            else if (typeof(TResult) == typeof(SplineWrapper))
            {
                displayName = "Spline";
            }
            else if (typeof(TResult) == typeof(SplineListWrapper))
            {
                displayName = "Spline List";
            }

            return displayName;
        }

        private bool IsPredicateTrue(string predicateName, params object[] parameters)
        {
            var isInverted = predicateName.StartsWith("!");
            predicateName = predicateName.Trim('!');

            var bindingFlags =
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Instance;

            var method = GetType().GetMethod(predicateName, bindingFlags);
            if (method == null)
            {
                throw new Exception($"missing predicate method: {predicateName}");
            }

            var result = (bool)method.Invoke(this, parameters);
            return isInverted ? !result : result;
        }
    }
}
