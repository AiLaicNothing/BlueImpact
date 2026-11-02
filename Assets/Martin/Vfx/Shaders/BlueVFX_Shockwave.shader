Shader "BlueImpact/VFX/Shockwave"
{
    Properties
    {
        [HDR] _BaseColor ("Base Color", Color) = (2, 1.4, 0.6, 1)
        _Progress ("Wave Progress", Range(0,1)) = 0
        _RingWidth ("Ring Width", Float) = 0.08
        _DissolveAmount ("Dissolve Amount", Range(0,1)) = 0
        _NoiseScale ("Noise Scale", Float) = 6
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "Unlit"
            Blend SrcAlpha One
            ZWrite Off
            Cull Off
            Offset -2, -2
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "BlueNoise.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float _Progress, _RingWidth, _DissolveAmount, _NoiseScale;
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
                float D = length((i.uv - 0.5) * 2.0);
                float rc = _Progress;
                float ring = smoothstep(_RingWidth, 0.0, abs(D - rc));
                float trail = smoothstep(rc, rc - 0.35, D) * step(D, rc) * 0.35;
                float N = ValueNoise(i.uv * _NoiseScale);
                float edge, cell; VoronoiCracks_float(i.uv, 5.0, 0.9, edge, cell);
                float cracks = (1.0 - edge) * step(D, rc);
                float v = saturate(ring + trail + cracks * 0.6);
                float a = v * pow(saturate(1.0 - _Progress), 1.5) * step(_DissolveAmount, N) * step(D, 1.0);
                return half4(_BaseColor.rgb * v, a);
            }
            ENDHLSL
        }
    }
}
