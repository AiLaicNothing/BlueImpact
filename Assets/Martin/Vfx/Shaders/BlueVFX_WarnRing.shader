Shader "BlueImpact/VFX/WarnRing"
{
    Properties
    {
        [HDR] _BaseColor ("Base Color", Color) = (3, 0.2, 0.1, 1)
        _Reveal ("Reveal", Range(0,1)) = 0
        _Pulse ("Pulse Speed", Float) = 12
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "Unlit"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            Offset -2, -2
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor; float _Reveal, _Pulse;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings vert(Attributes i)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(i.positionOS.xyz);
                o.uv = i.uv;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float D = length(i.uv - 0.5);
                float ring = smoothstep(0.46, 0.48, D) - smoothstep(0.49, 0.5, D);
                float fill = step(D * 2.0, _Reveal) * 0.25;
                float a = (ring + fill) * (0.7 + 0.3 * sin(_Time.y * _Pulse));
                return half4(_BaseColor.rgb, saturate(a));
            }
            ENDHLSL
        }
    }
}
