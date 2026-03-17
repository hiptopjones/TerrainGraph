using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Unity.GraphToolkit.Editor;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CodeFirst.TerrainGraph.Editor
{
    // Use a less-parameterized base class so using the inner classes is less ugly
    [Serializable]
    public abstract class BaseNode<TResult> : Node
    {
        [Serializable]
        public abstract class OptionValuesBase
        {
            [DefaultValue(true)]
            [IncludeIf(nameof(HasOutputPort))]
            [DisplayName("Enable Preview")]
            public bool IsPreviewEnabled;

            [DisplayName("Disable Node")]
            public bool IsNodeDisabled;

            [Ignore]
            public BehaviorInjector Injector;

            [Ignore]
            public int VersionHash;
        }

        [Serializable]
        public abstract class InputValuesBase
        {
            [Ignore]
            public int VersionHash;
        }

        // Public to make it visible when reflecting subclasses
        public bool HasOutputPort() => typeof(TResult) != typeof(NullOutput);
    }

    [Serializable]
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

        protected abstract bool TryExecuteNodeInternal();

        // NOTE: Any duplicates are managed during graph change processing
        public string Id = Guid.NewGuid().ToString();

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
                // No validation when disabled, but force regeneration when re-enabled
                CacheData.Output = null;
                return true;
            }

            if (!TryUpdateInputValues(graphLogger))
            {
                // Force regeneration when valid
                CacheData.Output = null;
                return false;
            }

            return true;
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
                // We are disabled, try to pass along the upstream value if possible
                var inputsModel = ClassModelCache.GetClassModel<TInputValues>();
                var fieldModel = inputsModel.FieldModels.FirstOrDefault(x => x.IsPassthru);
                if (fieldModel != null)
                {
                    return PortEvaluator.TryEvaluateInputPort(this, fieldModel.PortName, out value);
                }

                value = null;
                return false;
            }

            if (CacheData.Output == null)
            {
                // Validate or execute failed
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

            if (!HasOutputPort())
            {
                // Node has no output port, so nothing to execute
                // It is probably an export node.
                return true;
            }

            if (CacheData.Output != null && CacheData.Output.VersionHash == Inputs.VersionHash)
            {
                // Node is already up-to-date
                return true;
            }

            // Clear cache state to be safe
            CacheData.Output = null;

            return TryExecuteNodeInternal();
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

            tempOptions.VersionHash = GetOptionsHashCode(tempOptions);

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

            tempInputs.VersionHash = HashCode.Combine(GetInputsHashCode(tempInputs), Options.VersionHash);

            inputs = tempInputs;
            return true;
        }

        public bool TryUpdatePreview()
        {
            if (!HasOutputPort())
            {
                // No preview exists for this type of node, treat as up-to-date
                ClearPreview();
                return true;
            }

            if (!Options.IsPreviewEnabled)
            {
                // Preview is disabled, treat as up-to-date
                ClearPreview();
                return true;
            }

            if (Inputs == null)
            {
                SetWarningPreview();

                // Validation failed
                return false;
            }

            if (CacheData.Output == null)
            {
                SetWarningPreview();

                // Execution failed
                return false;
            }

            return TryUpdatePreviewInternal();
        }

        private bool TryUpdatePreviewInternal()
        {
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

            // Preview failed to update
            return false;
        }

        private void SetWarningPreview()
        {
            // Force generation when next enabled
            CacheData.PreviewHash = 0;

            // Make it very clear there is a problem
            var warningTexture = Resources.Load<Texture2D>("Textures/Warning");

            // Best effort, not checking the return
            TrySetPreviewTexture(warningTexture, 0);
        }

        private void ClearPreview()
        {
            TrySetPreviewTexture(null, 0);
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
                var optionsModel = ClassModelCache.GetClassModel<TOptionValues>();

                var injectorModel = optionsModel.GetFieldModel(nameof(OptionValuesBase.Injector));
                var injectorOption = GetNodeOptionByName(injectorModel.PortName);

                if (injectorOption.TryGetValue<BehaviorInjector>(out var injector))
                {
                    if (injector != null)
                    {
                        var description = $"{gridSize} x {gridSize}";
                        injector.SetPreviewTexture(texture, description);
                    }
                }

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
                    injector.OptionsTypeName = optionsModel.ClassType.FullName;
                    injector.InputsTypeName = inputsModel.ClassType.FullName;

                    if (typeof(TResult) == typeof(HeightGrid))
                    {
                        injector.SetMeshPreview = SetMeshPreviewTexture;
                    }
                }
            }
        }

        protected virtual int GetInputsHashCode(TInputValues inputs)
        {
            var inputsModel = ClassModelCache.GetClassModel<TInputValues>();

            var hashCode = 0;

            foreach (var fieldModel in inputsModel.FieldModels)
            {
                if (fieldModel.IsCustom)
                {
                    if (fieldModel.FieldType == typeof(HeightGrid))
                    {
                        var grid = (HeightGrid)fieldModel.GetValue(inputs);
                        hashCode = HashCode.Combine(hashCode, grid?.VersionHash);
                    }
                    else if (fieldModel.FieldType == typeof(SplineWrapper))
                    {
                        var splineWrapper = (SplineWrapper)fieldModel.GetValue(inputs);
                        hashCode = HashCode.Combine(hashCode, splineWrapper?.VersionHash);
                    }
                    else if (fieldModel.FieldType == typeof(SplineListWrapper))
                    {
                        var splineListWrapper = (SplineListWrapper)fieldModel.GetValue(inputs);
                        hashCode = HashCode.Combine(hashCode, splineListWrapper?.VersionHash);
                    }
                    else if (fieldModel.FieldType == typeof(Gradient))
                    {
                        var gradient = (Gradient)fieldModel.GetValue(inputs);
                        hashCode = HashCode.Combine(hashCode, GradientHelpers.GetHashCode(gradient));
                    }
                    else
                    {
                        var value = fieldModel.GetValue(inputs);
                        hashCode = HashCode.Combine(hashCode, value);
                    }
                }
            }

            return hashCode;
        }

        protected virtual int GetOptionsHashCode(TOptionValues options)
        {
            var optionsModel = ClassModelCache.GetClassModel<TOptionValues>();

            var hashCode = 0;

            foreach (var fieldModel in optionsModel.FieldModels)
            {
                if (fieldModel.IsCustom)
                {
                    var value = fieldModel.GetValue(options);
                    hashCode = HashCode.Combine(hashCode, value);
                }
            }

            return hashCode;
        }

        protected void SetMeshPreviewTexture()
        {
            var gridPreview = GetOrCreateMeshPreviewComponent();

            if (TryGetOutputValue(null, out TResult value))
            {
                var grid = value as HeightGrid;
                if (grid != null && grid.IsValid)
                {
                    gridPreview.SetHeightTexture(grid.RenderTexture);
                }
            }
        }

        public static MeshPreviewComponent GetOrCreateMeshPreviewComponent()
        {
            MeshPreviewComponent preview = Object.FindAnyObjectByType<MeshPreviewComponent>();
            if (preview == null)
            {
                GameObject go = new GameObject("Grid Preview");
                preview = go.AddComponent<MeshPreviewComponent>();
                go.transform.position = Vector3.zero;
            }

            return preview;
        }
    }
}
