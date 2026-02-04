Shader "Hidden/HeightGridPreview"
{
    Properties
    {
        _HeightGridTexture ("Height Grid Texture", 2D) = "white" {}
        _HeightScale ("Height Scale", Float) = 100
        _AmbientWrap ("Ambient Wrap", Range (0, 0.5)) = 0.4
        [Toggle] _CullZero ("Cull Zero", Float ) = 0
        _ContrastPower ("Contrast Power", Range(0.2, 5)) = 2.5
        _SlopeStrength("Slope Strength", Float ) = 0.5
        _LightDirection("Light Direction", Vector) = (45, 45, 0)
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
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float _HeightScale;
                float _AmbientWrap;
                float _CullZero;
                float _ContrastPower;
                float _SlopeStrength;
                float3 _LightDirection;
            CBUFFER_END

            TEXTURE2D(_HeightGridTexture);
            SAMPLER(sampler_HeightGridTexture);

            Varyings vert (Attributes IN)
            {
                Varyings OUT;

                float3 displacedPositionOS = IN.positionOS.xyz;
 
                float height = SAMPLE_TEXTURE2D_LOD(_HeightGridTexture, sampler_HeightGridTexture, IN.uv, 0).r;
                displacedPositionOS.y += height * _HeightScale;

                OUT.positionHCS = TransformObjectToHClip(displacedPositionOS);
                OUT.positionWS = TransformObjectToWorld(displacedPositionOS);
                OUT.uv = IN.uv;

                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                if (IN.positionWS.y == 0 && _CullZero)
                {
                    discard;
                }

                float3 dpdx = ddx(IN.positionWS);
                float3 dpdy = ddy(IN.positionWS);
                float3 normalWS = normalize(cross(dpdy, dpdx));
                
                float3 lightDirection = normalize(_LightDirection);

                float ndl = saturate((dot(normalWS, lightDirection) + _AmbientWrap) / (1 + _AmbientWrap));
                ndl = pow(ndl, _ContrastPower);

                float slope = 1.0 - abs(normalWS.y);

                float3 color = ndl.xxx + slope * _SlopeStrength;
                color = saturate(color);

                float ao = pow(normalWS.y * 0.5 + 0.5, 2);
                color *= ao;

                return half4(color, 1);
            }
            ENDHLSL
        }
    }
}
