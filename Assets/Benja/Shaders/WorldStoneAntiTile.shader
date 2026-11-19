// Benja/WorldStoneAntiTile  (URP)
// Piedra en coordenadas de mundo (triplanar) con anti-tiling a dos escalas
// y mancha de suciedad a gran escala. Un solo material sirve para piso, paredes y escaleras.
Shader "Benja/WorldStoneAntiTile"
{
    Properties
    {
        [MainTexture] _BaseMap("Piedra (Base Map)", 2D) = "white" {}
        [MainColor]   _BaseColor("Tinte", Color) = (1,1,1,1)
        _Tiling("Tiling (repeticiones por unidad)", Float) = 0.25
        _Rotation("Rotación 2da muestra (grados)", Float) = 30
        _Macro("Suciedad grande (Macro)", 2D) = "white" {}
        _MacroScale("Escala Macro", Float) = 0.02
        _MacroStrength("Fuerza Macro", Range(0,1)) = 1
        _Smoothness("Smoothness", Range(0,1)) = 0.12
        _BlendSharp("Nitidez triplanar", Range(1,16)) = 4
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);  SAMPLER(sampler_BaseMap);
            TEXTURE2D(_Macro);    SAMPLER(sampler_Macro);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4  _BaseColor;
                float  _Tiling;
                float  _Rotation;
                float  _MacroScale;
                half   _MacroStrength;
                half   _Smoothness;
                float  _BlendSharp;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 lightmapUV : TEXCOORD1;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                half   fogFactor  : TEXCOORD2;
                DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 3);
            };

            Varyings vert(Attributes input)
            {
                Varyings o = (Varyings)0;
                VertexPositionInputs vp = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs   vn = GetVertexNormalInputs(input.normalOS);
                o.positionCS = vp.positionCS;
                o.positionWS = vp.positionWS;
                o.normalWS   = vn.normalWS;
                o.fogFactor  = ComputeFogFactor(vp.positionCS.z);
                OUTPUT_LIGHTMAP_UV(input.lightmapUV, unity_LightmapST, o.lightmapUV);
                OUTPUT_SH(o.normalWS.xyz, o.vertexSH);
                return o;
            }

            float2 Rot(float2 uv, float deg)
            {
                float r = radians(deg);
                float s = sin(r), c = cos(r);
                return float2(uv.x * c - uv.y * s, uv.x * s + uv.y * c);
            }

            // Piedra con anti-tiling: dos muestras a distinta escala/rotación mezcladas
            // por una máscara de baja frecuencia sacada de la textura Macro.
            half3 StoneAntiTile(float2 uv)
            {
                half3 a = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv * _Tiling).rgb;
                half3 b = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, Rot(uv * _Tiling * 0.37, _Rotation)).rgb;
                half  m = SAMPLE_TEXTURE2D(_Macro, sampler_Macro, uv * _MacroScale * 5.0 + 0.37).r;
                m = smoothstep(0.55, 0.9, m);
                return lerp(a, b, m);
            }

            half MacroDirt(float2 uv)
            {
                return SAMPLE_TEXTURE2D(_Macro, sampler_Macro, uv * _MacroScale).r;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 n  = normalize(input.normalWS);
                float3 p  = input.positionWS;

                float3 w = pow(abs(n), _BlendSharp);
                w /= max(w.x + w.y + w.z, 1e-5);

                // planos: X -> (z,y), Y -> (x,z), Z -> (x,y)
                float2 uvX = p.zy;
                float2 uvY = p.xz;
                float2 uvZ = p.xy;

                half3 albedo =
                    StoneAntiTile(uvX) * w.x +
                    StoneAntiTile(uvY) * w.y +
                    StoneAntiTile(uvZ) * w.z;

                half macro =
                    MacroDirt(uvX) * w.x +
                    MacroDirt(uvY) * w.y +
                    MacroDirt(uvZ) * w.z;

                albedo *= lerp(1.0h, macro, _MacroStrength);
                albedo *= _BaseColor.rgb;

                InputData inputData = (InputData)0;
                inputData.positionWS = p;
                inputData.positionCS = input.positionCS;
                inputData.normalWS   = n;
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(p);
                inputData.shadowCoord = TransformWorldToShadowCoord(p);
                inputData.fogCoord = input.fogFactor;
                inputData.bakedGI  = SAMPLE_GI(input.lightmapUV, input.vertexSH, inputData.normalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.shadowMask = SAMPLE_SHADOWMASK(input.lightmapUV);

                SurfaceData s = (SurfaceData)0;
                s.albedo     = albedo;
                s.metallic   = 0;
                s.specular   = 0;
                s.smoothness = _Smoothness;
                s.occlusion  = 1;
                s.alpha      = 1;
                s.normalTS   = float3(0, 0, 1);

                half4 col = UniversalFragmentPBR(inputData, s);
                col.rgb = MixFog(col.rgb, inputData.fogCoord);
                return col;
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/DepthNormals"
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
