Shader "Hidden/HeightGridColorizer"
{
    Properties
    {
        _MainTex ("Height Data (RFloat)", 2D) = "white" {}
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
            };

            TEXTURE2D(_MainTex); // RFloat
            SAMPLER(sampler_MainTex);

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv; // No tiling or offsets
                return OUT;
            }
            
            half4 frag (Varyings IN) : SV_Target
            {
                half r = 0;
                half g = 0;
                half b = 0;
                half a = 1;

                float height = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv).r;

                if (height < 0)
                {
                    float t = saturate(abs(height) / 100 + 0.2);
                    r = t;
                }
                else if (height > 1)
                {
                    float t = saturate((height - 1) / 100 + 0.2);
                    g = t;
                }
                else
                {
                    r = height;
                    g = height;
                    b = height;
                }

                return half4(r, g, b, a);
            }
            ENDHLSL
        }
    }
}
