Shader "Hidden/HeightGridPreview"
{
    Properties
    {
        _HeightGridTexture ("Height Grid Texture", 2D) = "white" {}
        _HeightScale ("Height Scale", Float) = 100
        [Slider] _AmbientWrap ("Ambient Wrap", Range (0, 0.5)) = 0.2 // 0 = normal lambert, 1 = very soft
        [Toggle] _CullZero ("Cull Zero", Float ) = 0
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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

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

                Light mainLight = GetMainLight();

                float ndotlWrapped = saturate((dot(normalWS, mainLight.direction) + _AmbientWrap) / (1 + _AmbientWrap));
                float3 color = ndotlWrapped;

                return half4(color, 1);
            }
            ENDHLSL
        }
    }
}
