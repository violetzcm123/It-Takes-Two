Shader "URP/PixelLit3D"
{
    Properties
    {
        _BaseMap ("Texture", 2D) = "white" {}
        _PixelSize ("Pixel Size", Float) = 0.1
        _Color ("Color", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
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
                float4 positionCS : SV_POSITION;
                float3 worldPos : TEXCOORD0;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            float _PixelSize;
            float4 _Color;

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.worldPos = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.worldPos);
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                float3 p = floor(i.worldPos / _PixelSize) * _PixelSize;
                float2 uv = p.xz;
                half4 col = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv);
                return col * _Color;
            }
            ENDHLSL
        }
    }
}
