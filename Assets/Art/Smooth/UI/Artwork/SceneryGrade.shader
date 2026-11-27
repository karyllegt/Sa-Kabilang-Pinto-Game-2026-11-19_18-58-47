Shader "Skp/Smooth Scenery"
{
    Properties
    {
        [PerRendererData] _MainTex ("Scenery", 2D) = "white" {}
        _Title ("Title warmth", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                half _Title;
            CBUFFER_END

            struct Attributes
            {
                float4 position : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 position : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.position = TransformObjectToHClip(input.position.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 art = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half value = dot(art.rgb, half3(0.21h, 0.72h, 0.07h));
                half lit = smoothstep(0.04h, 0.48h, value);

                // Keep the details in the painted highlights and lift cool middle-distance silhouettes.
                half3 night = art.rgb * lerp(0.88h, 1.13h, lit);
                half sky = smoothstep(0.32h, 0.8h, input.uv.y);
                half haze = sky * (1.0h - lit);
                night += half3(0.016h, 0.019h, 0.044h) * haze;
                half warmth = _Title * smoothstep(0.43h, 0.79h, input.uv.x) * lit;
                night *= lerp(half3(1, 1, 1), half3(1.16h, 1.04h, 0.88h), warmth);
                return half4(saturate(night), art.a);
            }
            ENDHLSL
        }
    }
}
