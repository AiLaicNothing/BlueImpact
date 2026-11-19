// Benja/Wood Procedural Toon  (URP)
// Madera sin texturas: anillos + fibras generados en el espacio del objeto,
// con variación por objeto e iluminación en bandas estilo toon.
Shader "Benja/Wood Procedural Toon"
{
    Properties
    {
        _WoodLight("Madera clara", Color) = (0.55, 0.36, 0.18, 1)
        _WoodDark("Madera oscura (vetas)", Color) = (0.28, 0.16, 0.08, 1)
        [Enum(X,0,Y,1,Z,2)] _GrainAxis("Eje de la veta", Float) = 1
        _RingFreq("Densidad de anillos", Float) = 5
        _Warp("Ondulacion de la veta", Range(0,1.5)) = 0.5
        _FiberFreq("Fibras finas", Float) = 18
        _PlankWidth("Ancho de tablon (0 = sin tablones)", Float) = 0
        _Smoothness("Smoothness", Range(0,1)) = 0.05
        _Bands("Bandas toon", Range(1,8)) = 3
        _BandSoft("Suavidad de bandas", Range(0.01,0.5)) = 0.12
        _ToonAmount("Fuerza toon", Range(0,1)) = 0.8
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

            CBUFFER_START(UnityPerMaterial)
                half4  _WoodLight;
                half4  _WoodDark;
                float  _GrainAxis;
                float  _RingFreq;
                float  _Warp;
                float  _FiberFreq;
                float  _PlankWidth;
                half   _Smoothness;
                float  _Bands;
                float  _BandSoft;
                half   _ToonAmount;
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
                float3 woodPos    : TEXCOORD4;
                DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 3);
            };

            float Hash13(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.zyx + 31.32);
                return frac((p.x + p.y) * p.z);
            }

            float VNoise(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float n000 = Hash13(i);
                float n100 = Hash13(i + float3(1, 0, 0));
                float n010 = Hash13(i + float3(0, 1, 0));
                float n110 = Hash13(i + float3(1, 1, 0));
                float n001 = Hash13(i + float3(0, 0, 1));
                float n101 = Hash13(i + float3(1, 0, 1));
                float n011 = Hash13(i + float3(0, 1, 1));
                float n111 = Hash13(i + float3(1, 1, 1));
                return lerp(lerp(lerp(n000, n100, f.x), lerp(n010, n110, f.x), f.y),
                            lerp(lerp(n001, n101, f.x), lerp(n011, n111, f.x), f.y), f.z);
            }

            float FBM(float3 p)
            {
                return VNoise(p) * 0.55 + VNoise(p * 2.03) * 0.3 + VNoise(p * 4.1) * 0.15;
            }

            Varyings vert(Attributes input)
            {
                Varyings o = (Varyings)0;
                VertexPositionInputs vp = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs   vn = GetVertexNormalInputs(input.normalOS);
                o.positionCS = vp.positionCS;
                o.positionWS = vp.positionWS;
                o.normalWS   = vn.normalWS;
                o.fogFactor  = ComputeFogFactor(vp.positionCS.z);

                // posicion en el espacio del objeto, escalada por la escala del objeto
                float4x4 m = GetObjectToWorldMatrix();
                float3 sc = float3(length(m._m00_m10_m20), length(m._m01_m11_m21), length(m._m02_m12_m22));
                float seed = Hash13(float3(m._m03, m._m13, m._m23));
                o.woodPos = input.positionOS.xyz * sc + seed * 57.0;

                OUTPUT_LIGHTMAP_UV(input.lightmapUV, unity_LightmapST, o.lightmapUV);
                OUTPUT_SH(o.normalWS.xyz, o.vertexSH);
                return o;
            }

            half3 WoodColor(float3 p)
            {
                float a;
                float2 perp;
                if (_GrainAxis < 0.5)      { a = p.x; perp = p.yz; }
                else if (_GrainAxis < 1.5) { a = p.y; perp = p.xz; }
                else                       { a = p.z; perp = p.xy; }

                // anillos: distancia al "centro del tronco" deformada por ruido de baja frecuencia a lo largo de la veta
                float warp = FBM(float3(perp * 0.8, a * 0.12));
                float r = length(perp + (warp - 0.5) * _Warp * 2.0) * _RingFreq;
                float rings = sin(r * 6.2831853) * 0.5 + 0.5;
                rings = pow(rings, 1.6);

                // fibras finas, estiradas a lo largo de la veta
                float fibers = VNoise(float3(perp * _FiberFreq, a * _FiberFreq * 0.07));
                float tone = saturate(rings * 0.6 + fibers * 0.4);

                half3 col = lerp(_WoodDark.rgb, _WoodLight.rgb, tone);

                // tablones opcionales
                if (_PlankWidth > 0.001)
                {
                    float pc = perp.x / _PlankWidth;
                    float pid = floor(pc);
                    float gap = min(frac(pc), 1.0 - frac(pc));
                    col *= 0.85 + 0.3 * Hash13(float3(pid, 3.7, 1.3));
                    col *= 1.0 - 0.75 * (1.0 - smoothstep(0.0, 0.03, gap));
                }
                return col;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 n = normalize(input.normalWS);
                float3 p = input.positionWS;

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
                s.albedo     = WoodColor(input.woodPos);
                s.metallic   = 0;
                s.specular   = 0;
                s.smoothness = _Smoothness;
                s.occlusion  = 1;
                s.alpha      = 1;
                s.normalTS   = float3(0, 0, 1);

                half4 col = UniversalFragmentPBR(inputData, s);

                // bandas toon sobre la luminancia
                half l = max(dot(col.rgb, half3(0.299h, 0.587h, 0.114h)), 1e-4h);
                float t = l * _Bands;
                float band = (floor(t) + smoothstep(0.5 - _BandSoft, 0.5 + _BandSoft, frac(t))) / _Bands;
                col.rgb *= lerp(1.0h, (half)(band / l), _ToonAmount);

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
