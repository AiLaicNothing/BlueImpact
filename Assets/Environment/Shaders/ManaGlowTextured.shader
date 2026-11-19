Shader "BlueImpact/ManaGlowTextured"
{
    // Variante del cristal de mana para cualquier objeto con textura.
    // El color sale de la textura; Intensity (HDR) lo empuja por encima del umbral del Bloom.
    Properties
    {
        [MainTexture] _BaseMap ("Textura", 2D) = "white" {}
        [MainColor] _BaseColor ("Tinte", Color) = (1,1,1,1)

        _GlowMask ("Mascara de brillo (blanco = brilla, negro = no)", 2D) = "white" {}
        _Intensity ("Intensidad de brillo (HDR)", Range(0,6)) = 1.8

        _LitAmount ("Mezcla con la luz de la escena", Range(0,1)) = 0.25
        _ShadeStep ("Corte de sombra (toon)", Range(0,1)) = 0.5
        _ShadeColor ("Color de sombra", Color) = (0.35,0.45,0.85,1)

        _FresnelColor ("Color de borde", Color) = (0.6,1,1,1)
        _FresnelPower ("Potencia de borde", Range(0.5,8)) = 3
        _FresnelStrength ("Fuerza de borde", Range(0,3)) = 0.6

        [Toggle(_ALPHATEST_ON)] _AlphaClip ("Recortar por alfa", Float) = 0
        _Cutoff ("Umbral de recorte", Range(0,1)) = 0.5
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        TEXTURE2D(_BaseMap);  SAMPLER(sampler_BaseMap);
        TEXTURE2D(_GlowMask); SAMPLER(sampler_GlowMask);
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            float4 _GlowMask_ST;
            half4  _BaseColor;
            float  _Intensity;
            float  _LitAmount;
            float  _ShadeStep;
            half4  _ShadeColor;
            half4  _FresnelColor;
            float  _FresnelPower;
            float  _FresnelStrength;
            float  _Cutoff;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "ForwardGlow"
            Tags { "LightMode"="UniversalForward" }
            Cull [_Cull]
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS   : TEXCOORD2;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs vp = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = vp.positionCS;
                OUT.positionWS = vp.positionWS;
                OUT.normalWS   = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv         = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, TRANSFORM_TEX(IN.uv, _BaseMap)) * _BaseColor;
                #if defined(_ALPHATEST_ON)
                    clip(tex.a - _Cutoff);
                #endif
                half mask = SAMPLE_TEXTURE2D(_GlowMask, sampler_GlowMask, TRANSFORM_TEX(IN.uv, _GlowMask)).r;

                float3 n = normalize(IN.normalWS);
                float3 v = normalize(GetWorldSpaceViewDir(IN.positionWS));

                // Parte iluminada (toon) para que el objeto no se vea plano
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(IN.positionWS));
                float ndl  = dot(n, mainLight.direction) * 0.5 + 0.5;
                float toon = smoothstep(_ShadeStep - 0.03, _ShadeStep + 0.03, ndl * lerp(0.6, 1.0, mainLight.shadowAttenuation));
                half3 lit = tex.rgb * lerp(_ShadeColor.rgb, half3(1,1,1), toon) * mainLight.color;

                // Zona sin brillo: textura con algo de luz de escena
                half3 baseCol = lerp(tex.rgb, lit, _LitAmount);
                // Zona con brillo: textura x intensidad HDR (supera el umbral del Bloom)
                half3 glowCol = lerp(tex.rgb, lit, _LitAmount * 0.5) * _Intensity;
                half3 col = lerp(baseCol, glowCol, mask);

                half fres = pow(1.0 - saturate(abs(dot(n, v))), _FresnelPower);
                col += _FresnelColor.rgb * fres * _FresnelStrength * mask;

                return half4(col, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            ColorMask 0
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local _ALPHATEST_ON
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            float3 _LightDirection;
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            Varyings vert(Attributes IN)
            {
                Varyings o;
                float3 posWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 nWS = TransformObjectToWorldNormal(IN.normalOS);
                o.positionCS = TransformWorldToHClip(ApplyShadowBias(posWS, nWS, _LightDirection));
                #if UNITY_REVERSED_Z
                    o.positionCS.z = min(o.positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    o.positionCS.z = max(o.positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                o.uv = IN.uv;
                return o;
            }
            half4 frag(Varyings IN) : SV_Target
            {
                #if defined(_ALPHATEST_ON)
                    clip(SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, TRANSFORM_TEX(IN.uv, _BaseMap)).a * _BaseColor.a - _Cutoff);
                #endif
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On
            ColorMask R
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local _ALPHATEST_ON
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            Varyings vert(Attributes IN) { Varyings o; o.positionCS = TransformObjectToHClip(IN.positionOS.xyz); o.uv = IN.uv; return o; }
            half frag(Varyings IN) : SV_Target
            {
                #if defined(_ALPHATEST_ON)
                    clip(SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, TRANSFORM_TEX(IN.uv, _BaseMap)).a * _BaseColor.a - _Cutoff);
                #endif
                return IN.positionCS.z;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormals" }
            ZWrite On
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; };
            Varyings vert(Attributes IN)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                o.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                return o;
            }
            half4 frag(Varyings IN) : SV_Target { return half4(normalize(IN.normalWS), 0.0); }
            ENDHLSL
        }
    }
}
