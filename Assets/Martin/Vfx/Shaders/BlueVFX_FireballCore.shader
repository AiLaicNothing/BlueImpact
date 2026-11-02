Shader "BlueImpact/VFX/FireballCore"
{
    Properties
    {
        [HDR] _BaseColor ("Base Color", Color) = (3, 1.0, 0.15, 1)
        [HDR] _HotColor ("Hot Color", Color) = (4, 3.6, 2, 1)
        _EffectSpeed ("Effect Speed", Float) = 2.5
        _NoiseScale ("Noise Scale", Float) = 3
        _DissolveAmount ("Dissolve Amount", Range(0,1)) = 0
        _FresnelPower ("Fresnel Power", Float) = 2.5
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "Unlit"
            Blend SrcAlpha One
            ZWrite Off
            Cull Back
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "BlueNoise.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor; half4 _HotColor;
                float _EffectSpeed, _NoiseScale, _DissolveAmount, _FresnelPower;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 viewDirWS : TEXCOORD1;
                float3 dirOS : TEXCOORD2;
            };

            Varyings vert(Attributes i)
            {
                Varyings o;
                float3 dir = normalize(i.positionOS.xyz);
                float2 puv = dir.xy + dir.z;
                float n; FBM_float(puv, _NoiseScale, _EffectSpeed, _Time.y, n);
                float3 pos = i.positionOS.xyz + i.normalOS * (n - 0.5) * 0.15;
                float3 posWS = TransformObjectToWorld(pos);
                o.positionCS = TransformWorldToHClip(posWS);
                o.normalWS = TransformObjectToWorldNormal(i.normalOS);
                o.viewDirWS = GetWorldSpaceViewDir(posWS);
                o.dirOS = dir;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float3 nrm = normalize(i.normalWS);
                float3 vdir = normalize(i.viewDirWS);
                float fr = pow(1.0 - saturate(dot(nrm, vdir)), _FresnelPower);
                float n; FBM_float(i.dirOS.xy + i.dirOS.z, _NoiseScale, _EffectSpeed, _Time.y, n);
                half3 fire = lerp(_BaseColor.rgb, _HotColor.rgb, saturate(n * 1.4 - fr * 0.4));
                float cut = step(_DissolveAmount, n);
                return half4(fire * (1.0 + fr), saturate(fr + 0.5) * cut);
            }
            ENDHLSL
        }
    }
}
