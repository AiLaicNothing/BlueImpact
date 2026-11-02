Shader "BlueImpact/VFX/MagmaCrack"
{
    Properties
    {
        [HDR] _BaseColor ("Base Color", Color) = (3, 0.75, 0.06, 1)
        _RockColor ("Rock Color", Color) = (0.06, 0.04, 0.04, 1)
        _CrackWidth ("Crack Width", Range(0.02,0.6)) = 0.18
        _Reveal ("Reveal", Range(0,1)) = 0
        _DissolveAmount ("Dissolve Amount", Range(0,1)) = 0
        _EffectSpeed ("Effect Speed", Float) = 0.6
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
            #include "BlueNoise.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor; half4 _RockColor;
                float _CrackWidth, _Reveal, _DissolveAmount, _EffectSpeed;
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
                float edge, cell; VoronoiCracks_float(i.uv, 6.0, 1.0, edge, cell);
                float crack = 1.0 - smoothstep(0.0, _CrackWidth, edge);
                float wave = step(D, _Reveal);
                float N; FBM_float(i.uv, 4.0, _EffectSpeed, _Time.y, N);
                half3 hot = lerp(_BaseColor.rgb, _RockColor.rgb, _DissolveAmount);
                float c = crack * wave;
                half3 col = lerp(_RockColor.rgb, hot, c) * (0.7 + N * 0.6);
                float a = smoothstep(1.0, 0.7, D) * (c + 0.35 * wave) * (1.0 - _DissolveAmount);
                return half4(col, saturate(a));
            }
            ENDHLSL
        }
    }
}
