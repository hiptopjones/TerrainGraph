using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Unity.GraphToolkit.Editor;
using UnityEditor;
using UnityEngine;

namespace Indiecat.TerrainGraph.Editor
{
    // Use a less-parameterized base class so using the inner classes is less ugly
    public abstract class BaseNode<TResult> : Node
    {
        public abstract class OptionValuesBase
        {
            [DefaultValue(true)]
            [IncludeIf(nameof(HasOutputPort))]
            public bool IsPreviewEnabled;

            public bool IsNodeDisabled;

            [Ignore]
            public BehaviorInjector Injector;

            [Ignore]
            public int VersionHash;
        }

        public abstract class InputValuesBase
        {
            [Ignore]
            [IncludeIf(nameof(HasOutputPort))]
            public PreviewImage Preview;

            [Ignore]
            public int VersionHash;
        }

        // Public to make it visible when reflecting subclasses
        public bool HasOutputPort() => typeof(TResult) != typeof(NullOutput);
    }

    public abstract class BaseNode<TOptionValues, TInputValues, TResult> : BaseNode<TResult>,
        IValidatableNode,
        IExecutableNode,
        IEvaluatableNode<TResult>,
        ICacheableNode<TResult>,
        IPreviewableNode
        where TOptionValues : BaseNode<TResult>.OptionValuesBase
        where TInputValues : BaseNode<TResult>.InputValuesBase
        where TResult : class, IVersionedObject
    {
        public CacheData<TResult> CacheData { get; set; } = new();

        public bool IsNodeValid => Inputs != null;


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
        //   - TryUpdatePreview - reads options, reads input
        //   - TryGetOutputValue - reads options, reads input
        //   - TryExecuteNode - reads options, reads input
        //   - TryExportNode - reads options, reads input
        
        // Called by Graph Toolkit when creating a node
        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            Options = null;
            Inputs = null;

            OnDefineCustomOptions(context);
            OnDefineBaseOptions(context);
        }

        private void OnDefineBaseOptions(IOptionDefinitionContext context)
        {
            if (HasOutputPort())
            {
                BuildOption(context, x => x.IsPreviewEnabled);
            }

            BuildOption(context, x => x.IsNodeDisabled);
            BuildOption(context, x => x.Injector);
        }

        protected virtual void OnDefineCustomOptions(IOptionDefinitionContext context)
        {
            var optionsModel = ClassModelCache.GetClassModel<TOptionValues>();

            var subclassFieldModels = optionsModel.FieldModels
                .Where(x => x.DeclaringType != typeof(OptionValuesBase));

            foreach (var fieldModel in subclassFieldModels)
            {
                BuildOption(context, fieldModel);
            }
        }

        protected void BuildOption<TField>(
            IOptionDefinitionContext context, Expression<Func<TOptionValues, TField>> fieldExpression)
        {
            var member = fieldExpression.Body as MemberExpression;
            var fieldInfo = member?.Member as FieldInfo;
         
            if (fieldInfo == null)
            {
                throw new ArgumentException("Invalid expression");
            }

            var fieldModel = ClassModelCache.GetFieldModel(fieldInfo);
            BuildOption(context, fieldModel);
        }

        private void BuildOption(IOptionDefinitionContext context, FieldModel fieldModel)
        {
            var builder = context.AddOption(fieldModel.PortName, fieldModel.FieldType)
                .WithDisplayName(fieldModel.DisplayName);

            if (fieldModel.DefaultValue != null)
            {
                // Make sure it's the exact right type
                var defaultValue = Convert.ChangeType(fieldModel.DefaultValue ?? default, fieldModel.FieldType);
                builder.WithDefaultValue(defaultValue);
            }

            builder.Build();
        }

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            Inputs = null;

            if (!TryUpdateOptionValues())
            {
                return;
            }

            OnDefineCustomInputPorts(context);
            OnDefineBaseInputPorts(context);

            OnDefineOutputPort(context);
        }

        private void OnDefineOutputPort(IPortDefinitionContext context)
        {
            if (!HasOutputPort())
            {
                return;
            }

            string displayName = GetOutputPortDisplayName();

            context.AddOutputPort<TResult>(NODE_OUTPUT_VALUE_ID)
                .WithDisplayName(displayName)
                .Build();
        }

        protected virtual void OnDefineCustomInputPorts(IPortDefinitionContext context)
        {
            var inputsModel = ClassModelCache.GetClassModel<TInputValues>();

            var customFieldModels = inputsModel.FieldModels
                .Where(x => x.DeclaringType != typeof(InputValuesBase));

            foreach (var fieldModel in customFieldModels)
            {
                if (!fieldModel.IsIncluded(this))
                {
                    // Field has been excluded by IncludeIf attribute
                    continue;
                }

                BuildInputPort(context, fieldModel);
            }
        }

        protected virtual void OnDefineBaseInputPorts(IPortDefinitionContext context)
        {
            if (HasOutputPort() && Options.IsPreviewEnabled)
            {
                BuildInputPort(context, x => x.Preview);
            }

            // Need unity to call us back after everything has been defined
            // so we can update the injector type
            EditorApplication.delayCall += () => UpdateInjector();
        }

        protected void BuildInputPort<TField>(
            IPortDefinitionContext context, Expression<Func<TInputValues, TField>> fieldExpression)
        {
            var member = fieldExpression.Body as MemberExpression;
            var fieldInfo = member?.Member as FieldInfo;

            if (fieldInfo == null)
            {
                throw new ArgumentException("Invalid expression");
            }

            var fieldModel = ClassModelCache.GetFieldModel(fieldInfo);
            BuildInputPort(context, fieldModel);
        }

        private void BuildInputPort(IPortDefinitionContext context, FieldModel fieldModel)
        {
            var builder = context.AddInputPort(fieldModel.PortName)
                .WithDataType(fieldModel.FieldType)
                .WithDisplayName(fieldModel.DisplayName);

            if (fieldModel.DefaultValue != null)
            {
                // Make sure it's the exact right type
                var defaultValue = Convert.ChangeType(fieldModel.DefaultValue ?? default, fieldModel.FieldType);
                builder.WithDefaultValue(defaultValue);
            }

            builder.Build();
        }

        public bool TryValidateNode(GraphLogger graphLogger = null)
        {
            if (Options == null)
            {
                graphLogger?.LogError("Options is null");
                return false;
            }

            if (Options.IsNodeDisabled)
            {
                // No validation when disabled
                return true;
            }

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
                var inputsModel = ClassModelCache.GetClassModel<TInputValues>();
                var fieldModel = inputsModel.FieldModels.FirstOrDefault(x => x.IsPassthru);
                if (fieldModel != null)
                {
                    return PortEvaluator.TryEvaluateInputPort(this, fieldModel.PortName, out value);
                }

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

            var optionsModel = ClassModelCache.GetClassModel<TOptionValues>();

            foreach (var fieldModel in optionsModel.FieldModels)
            {
                if (fieldModel.IsIgnored || !fieldModel.IsIncluded(this))
                {
                    continue;
                }

                var nodeOption = GetNodeOptionByName(fieldModel.PortName);

                var fieldType = fieldModel.FieldType;

                // TODO: Make this a compiled method
                // nodeOption.TryGetValue<bool>(out var isPreviewEnabled)
                var method = typeof(INodeOption)
                    .GetMethod(nameof(nodeOption.TryGetValue))
                    .MakeGenericMethod(fieldType);

                var parameters = new object[] { null };

                if (!(bool)method.Invoke(nodeOption, parameters))
                {
                    Debug.LogError($"Unable to get option value: {fieldModel.PortName}");
                    return false;
                }

                fieldModel.SetValue(tempOptions, parameters[0]);
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

                ClearPreview();
                return false;
            }

            if (!TryValidateInputValues(tempInputs, graphLogger))
            {
                ClearPreview();
                return false;
            }

            Inputs = tempInputs;

            return true;
        }

        private bool TryValidateInputValues(TInputValues inputs, GraphLogger graphLogger = null)
        {
            var isValid = true;

            var inputsModel = ClassModelCache.GetClassModel<TInputValues>();

            foreach (var fieldModel in inputsModel.FieldModels)
            {
                if (fieldModel.IsIgnored || !fieldModel.IsIncluded(this))
                {
                    continue;
                }

                var fieldType = fieldModel.FieldType;

                if (fieldType.IsEnum)
                {
                    var value = fieldModel.GetValue(inputs);
                    if (Enum.IsDefined(fieldType, value))
                    {
                        graphLogger?.LogError($"{fieldModel.DisplayName} input invalid", this);
                        isValid = false;
                    }
                }

                if (fieldType == typeof(HeightGrid))
                {
                    var grid = (HeightGrid)fieldModel.GetValue(inputs);
                    if (grid == null || !grid.IsValid)
                    {
                        graphLogger?.LogError($"{fieldModel.DisplayName} input missing", this);
                        isValid = false;
                    }
                }

                if (fieldType == typeof(SplineWrapper))
                {
                    var spline = (SplineWrapper)fieldModel.GetValue(inputs);
                    if (spline == null || !spline.IsValid)
                    {
                        graphLogger?.LogError($"{fieldModel.DisplayName} input missing", this);
                        isValid = false;
                    }
                }

                if (fieldType == typeof(SplineListWrapper))
                {
                    var splineList = (SplineListWrapper)fieldModel.GetValue(inputs);
                    if (splineList == null || !splineList.IsValid)
                    {
                        graphLogger?.LogError($"{fieldModel.DisplayName} input missing", this);
                        isValid = false;
                    }
                }

                if (isValid)
                {
                    // Only run these rules if the above validation checked out
                    // Otherwise validators may have to do redundant checks to avoid spurious errors

                    foreach (var rule in fieldModel.Rules)
                    {
                        var result = rule.Validate(this, inputs);
                        if (!result.IsValid)
                        {
                            graphLogger?.LogError(result.Message, this);
                            isValid = false;
                        }
                        else if (!string.IsNullOrEmpty(result.Message))
                        {
                            // Probably corrected something for the user
                            graphLogger?.LogWarning(result.Message, this);
                        }
                    }
                }
            }

            return isValid;
        }

        private bool TryGetInputValues(GraphLogger graphLogger, out TInputValues inputs)
        {
            var tempInputs = Activator.CreateInstance<TInputValues>();

            var inputsModel = ClassModelCache.GetClassModel<TInputValues>();

            foreach (var fieldModel in inputsModel.FieldModels)
            {
                if (fieldModel.IsIgnored || !fieldModel.IsIncluded(this))
                {
                    continue;
                }

                var fieldType = fieldModel.FieldType;

                // TODO: Make this a compiled method
                // PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_GRID_ID, out temp.Grid)
                var method = typeof(PortEvaluator)
                    .GetMethod(nameof(PortEvaluator.TryEvaluateInputPort))
                    .MakeGenericMethod(fieldType);

                var parameters = new object[] { this, fieldModel.PortName, null };

                if (!(bool)method.Invoke(null, parameters))
                {
                    // Too noisy to log here
                    inputs = null;
                    return false;
                }

                fieldModel.SetValue(tempInputs, parameters[2]);
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

            if (!Options.IsPreviewEnabled)
            {
                // Force generation when next enabled
                CacheData.PreviewHash = 0;

                // Preview is disabled, treat as up-to-date
                return true;
            }

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

            ClearPreview();

            // Preview failed to update
            return false;
        }

        private void ClearPreview()
        {
            // Force generation when next enabled
            CacheData.PreviewHash = 0;

            if (Options.IsPreviewEnabled)
            {
                // Make it very clear there is a problem
                var warningTexture = EditorGUIUtility.IconContent("console.warnicon.sml").image;

                // Best effort, not checking the return
                TrySetPreviewTexture(warningTexture, 0);
            }
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
                var inputsModel = ClassModelCache.GetClassModel<TInputValues>();
                var fieldModel = inputsModel.GetFieldModel(nameof(InputValuesBase.Preview));

                var previewPort = GetInputPortByName(fieldModel.PortName);
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

        private void UpdateInjector()
        {
            var optionsModel = ClassModelCache.GetClassModel<TOptionValues>();
            var inputsModel = ClassModelCache.GetClassModel<TInputValues>();

            var injectorModel = optionsModel.GetFieldModel(nameof(OptionValuesBase.Injector));
            var injectorOption = GetNodeOptionByName(injectorModel.PortName);

            if (injectorOption.TryGetValue<BehaviorInjector>(out var injector))
            {
                if (injector != null)
                {
                    // The injector uses the inputs model (not options)
                    injector.TypeName = inputsModel.ClassType.FullName;
                }
            }
        }
    }
}
