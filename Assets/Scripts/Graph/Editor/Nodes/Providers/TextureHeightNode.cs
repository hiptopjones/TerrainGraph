using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;

[Serializable]
public class TextureHeightNode : ProviderNode<HeightProvider>
{
    private class InputValues
    {
        public Texture2D Texture;

        public int VersionHash;

        public override int GetHashCode()
        {
            return HashCode.Combine(Texture);
        }
    }

    // Options

    // Inputs
    private const string NODE_INPUT_TEXTURE_ID = "texture_input";
    private const string NODE_INPUT_TEXTURE_TITLE = "Texture";

    // Outputs
    private const string NODE_OUTPUT_PROVIDER_ID = "provier_output";
    private const string NODE_OUTPUT_PROVIDER_TITLE = "Provider";

    protected override void OnDefineOptions(IOptionDefinitionContext context)
    {
        // N/A
    }

    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        // Input
        context.AddInputPort<Texture>(NODE_INPUT_TEXTURE_ID)
            .WithDisplayName(NODE_INPUT_TEXTURE_TITLE)
            .Build();

        // Output
        context.AddOutputPort<HeightProvider>(NODE_OUTPUT_PROVIDER_ID)
            .WithDisplayName(NODE_OUTPUT_PROVIDER_TITLE)
            .Build();
    }

    public override bool TryValidateNode(GraphLogger graphLogger = null)
    {
        return TryGetValidatedInputValues(out _, graphLogger);
    }

    private bool TryGetValidatedInputValues(out InputValues validatedInput, GraphLogger graphLogger = null)
    {
        validatedInput = null;

        if (!TryGetInputValues(out var input))
        {
            if (graphLogger != null) graphLogger.LogError("Upstream failure", this);
            return false;
        }

        var isValid = true;

        if (input.Texture == null)
        {
            if (graphLogger != null) graphLogger.LogError($"{NODE_INPUT_TEXTURE_TITLE} value missing", this);
            isValid = false;
        }

        if (isValid)
        {
            validatedInput = input;
        }

        return isValid;
    }

    private bool TryGetInputValues(out InputValues input)
    {
        input = null;

        var temp = new InputValues();
        var success =
            PortEvaluator.TryEvaluateInputPort(this, NODE_INPUT_TEXTURE_ID, out temp.Texture);

        if (success)
        {
            temp.VersionHash = temp.GetHashCode();

            input = temp;
            return true;
        }

        return false;
    }

    public override bool TryGetOutputValue(IPort _, out HeightProvider value)
    {
        if (!TryGetValidatedInputValues(out var inputValues))
        {
            value = null;
            return false;
        }

        value = new TextureHeightProvider()
        {
            Texture = inputValues.Texture,

            VersionHash = inputValues.VersionHash
        };

        return true;
    }
}