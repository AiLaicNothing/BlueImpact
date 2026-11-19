Shader "BlueImpact/RuneDecalGlow"
{
    // Decal de runa brillante sobre el suelo. Mezcla aditiva: suma luz a la piedra en vez de taparla.
    // Salida HDR para que el Bloom genere el halo. Offset de profundidad para evitar z-fighting.
    Properties
    {
        _MainTex ("Textura (PNG con alfa)", 2D) = "white" {}
        [HDR] _Color ("Color", Color) = (1,1,1,1)
        _Intensity ("Intensidad (HDR, para Bloom)", Range(0,6)) = 2
        _PulseSpeed ("Velocidad de pulso", Range(0,5)) = 1.2
        _PulseAmount ("Cantidad de pulso", Range(0,1)) = 0.25
        _ScrollSpeed ("Desplazamiento U (lineas de energia)", Range(-2,2)) = 0
        _EdgeFade ("Fundido en bordes del plano", Range(0,0.5)) = 0.05
        _DepthOffset ("Separacion del suelo", Range(-10,0)) = -2
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent-50" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }

        Pass
        {
            Name "RuneGlow"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha One
            ZWrite Off
            Cull Off
            Offset [_DepthOffset], [_DepthOffset]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4  _Color;
                float  _Intensity;
                float  _PulseSpeed;
                float  _PulseAmount;
                float  _ScrollSpeed;
                float  _EdgeFade;
                float  _DepthOffset;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float2 rawUV : TEXCOORD1; float phase : TEXCOORD2; };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex) + float2(_ScrollSpeed * _Time.y, 0);
                OUT.rawUV = IN.uv;
                // fase segun posicion en el mundo, para que no todas las runas pulsen a la vez
                float3 pivot = TransformObjectToWorld(float3(0,0,0));
                OUT.phase = frac(dot(pivot, float3(0.137, 0.0, 0.211))) * 6.2831;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                float pulse = 1.0 - _PulseAmount * (0.5 + 0.5 * sin(_Time.y * _PulseSpeed * 6.2831 + IN.phase));
                float2 e = min(IN.rawUV, 1.0 - IN.rawUV);
                float edge = (_EdgeFade > 0) ? saturate(min(e.x, e.y) / _EdgeFade) : 1.0;
                half a = tex.a * _Color.a * edge;
                return half4(tex.rgb * _Color.rgb * _Intensity * pulse, a);
            }
            ENDHLSL
        }
    }
}
