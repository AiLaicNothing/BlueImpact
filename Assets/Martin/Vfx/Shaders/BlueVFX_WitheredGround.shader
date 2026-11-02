Shader "BlueImpact/VFX/WitheredGround"
{
    Properties
    {
        [HDR] _BaseColor ("Base Color", Color) = (0.35, 0.10, 0.45, 1)
        [HDR] _RimColor ("Rim Color", Color) = (1.8, 0.6, 2.0, 1)
        _Reveal ("Reveal", Range(0,1)) = 0
        _DissolveAmount ("Dissolve Amount", Range(0,1)) = 0
        _RadiusUV ("Radius UV", Float) = 0.5
        _NoiseScale ("Noise Scale", Float) = 4
        _EffectSpeed ("Effect Speed", Float) = 0.4
        _Pulse ("Pulse", Range(0,2)) = 0
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
                half4 _BaseColor; half4 _RimColor;
                float _Reveal, _DissolveAmount, _RadiusUV, _NoiseScale, _EffectSpeed, _Pulse;
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
                float D = length(i.uv - 0.5) / max(_RadiusUV, 0.0001);
                float N; FBM_float(i.uv, _NoiseScale, _EffectSpeed, _Time.y, N);
                float edge, cell; VoronoiCracks_float(i.uv, 7.0, 0.8, edge, cell);

                float disk = smoothstep(1.0, 0.85, D);
                float revealMask = 1.0 - smoothstep(_Reveal - 0.05, _Reveal, D);
                float dissolveMask = step(_DissolveAmount, N);
                float rim = smoothstep(0.7, 0.88, D) * disk;

                half3 albedo = lerp(_BaseColor.rgb, _BaseColor.rgb * 0.4, cell);
                half3 crack = (1.0 - edge) * _RimColor.rgb * (0.5 + _Pulse);
                half3 col = albedo + crack + rim * _RimColor.rgb;
                float a = disk * revealMask * dissolveMask * (0.55 + N * 0.35);
                return half4(col, saturate(a));
            }
            ENDHLSL
        }
    }
}
