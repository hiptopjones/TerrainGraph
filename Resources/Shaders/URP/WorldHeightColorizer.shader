Shader "Custom/WorldHeightColorizer"
{
    Properties
    {
        _LowColor ("Low Color", Color) = (0, 0, 1, 1)
        _HighColor ("High Color", Color) = (0, 1, 0, 1)
        _MinHeight ("Min Height", Float) = 0
        _MaxHeight ("Max Height", Float) = 100
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _LowColor;
                half4 _HighColor;
                float _MinHeight;
                float _MaxHeight;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float t = saturate((IN.positionWS.y - _MinHeight) / (_MaxHeight - _MinHeight));
                return lerp(_LowColor, _HighColor, t);
            }
            ENDHLSL
        }
    }
}
