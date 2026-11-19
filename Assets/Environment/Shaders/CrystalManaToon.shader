Shader "BlueImpact/CrystalManaToon"
{
    // Cristal de mana estilizado: degradado emisivo por altura (color de vertice R),
    // leve variacion por cara (color de vertice G) y borde Fresnel. No depende de la luz.
    Properties
    {
        _BaseColor ("Base (azul intenso)", Color) = (0.06,0.22,1,1)
        _MidColor ("Medio (cian)", Color) = (0.1,0.85,1,1)
        _TipColor ("Punta (blanco cian)", Color) = (0.85,1,1,1)
        _MidPoint ("Punto medio del degradado", Range(0.05,0.95)) = 0.45
        _Intensity ("Intensidad (HDR, para Bloom)", Range(0.5,4)) = 1.6
        _FacetVariation ("Variacion entre caras", Range(0,0.4)) = 0.12
        _FresnelColor ("Color de borde", Color) = (0.6,1,1,1)
        _FresnelPower ("Potencia de borde", Range(0.5,8)) = 3
        _FresnelStrength ("Fuerza de borde", Range(0,2)) = 0.6
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            half4 _MidColor;
            half4 _TipColor;
            float _MidPoint;
            float _Intensity;
            float _FacetVariation;
            half4 _FresnelColor;
            float _FresnelPower;
            float _FresnelStrength;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode"="UniversalForward" }
            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            struct Attributes { float4 positionOS : POSITION; float4 color : COLOR; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float4 color      : TEXCOORD1;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs vp = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = vp.positionCS;
                OUT.positionWS = vp.positionWS;
                OUT.color = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float t = saturate(IN.color.r);
                half3 col = lerp(_BaseColor.rgb, _MidColor.rgb, smoothstep(0.0, _MidPoint, t));
                col = lerp(col, _TipColor.rgb, smoothstep(_MidPoint, 1.0, t));

                // Facetas: cada cara un poco mas clara u oscura
                col *= 1.0 + (IN.color.g - 0.5) * 2.0 * _FacetVariation;

                // Borde Fresnel con normal plana por cara
                float3 n = normalize(cross(ddy(IN.positionWS), ddx(IN.positionWS)));
                float3 v = normalize(GetWorldSpaceViewDir(IN.positionWS));
                half fres = pow(1.0 - abs(dot(n, v)), _FresnelPower);
                col += _FresnelColor.rgb * fres * _FresnelStrength;

                return half4(col * _Intensity, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; };
            Varyings vert(Attributes IN) { Varyings o; o.positionCS = TransformObjectToHClip(IN.positionOS.xyz); return o; }
            half frag(Varyings IN) : SV_Target { return IN.positionCS.z; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormals" }
            ZWrite On

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
