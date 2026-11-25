// BlueImpact - Water: agua para URP (Forward+).
// - Color según la profundidad (orilla clara, fondo oscuro) usando la Depth Texture.
// - Refracción: se ve el fondo deformado por las ondas (Opaque Texture).
// - Reflejos de reflection probes con fresnel (más reflejo al mirar de costado).
// - Brillos (specular) de la luz principal y de TODAS las luces adicionales (antorchas, cristales).
// - Recibe sombras de la luz principal.
// - Ondas: normal map opcional en dos capas + ondas procedurales (no necesita texturas).
// - Espuma en la orilla y alrededor de objetos que tocan el agua.
// - Olas suaves en los vértices (opcional) y niebla.
// Requisitos (ya activos en el proyecto): Depth Texture y Opaque Texture en el URP Asset.
// Usar en planos horizontales (lagos, ríos, charcos). Para cascadas, el shader de cascada.
Shader "BlueImpact/Water"
{
    Properties
    {
        [Header(Color)]
        _ShallowColor("Color en la orilla (poco profundo)", Color) = (0.25, 0.65, 0.72, 1)
        _DeepColor("Color en lo profundo", Color) = (0.02, 0.09, 0.2, 1)
        _DepthDistance("Distancia hasta el color profundo", Range(0.1, 30)) = 4
        _Clarity("Transparencia (cuanto se ve el fondo)", Range(0, 1)) = 0.6

        [Header(Ondas)]
        [NoScaleOffset][Normal] _NormalMap("Normal Map (opcional)", 2D) = "bump" {}
        _NormalStrength("Fuerza del normal map", Range(0, 2)) = 0.6
        _NormalTiling("Tamano de las ondas del normal map (metros)", Range(0.5, 50)) = 6
        _RippleStrength("Ondas procedurales: fuerza", Range(0, 2)) = 0.5
        _RippleScale("Ondas procedurales: tamano (metros)", Range(0.5, 30)) = 4
        _FlowSpeed("Velocidad de las ondas", Range(0, 2)) = 0.25
        _FlowDirection("Direccion del flujo (XZ)", Vector) = (1, 0.3, 0, 0)

        [Header(Reflejo y brillo)]
        _Smoothness("Smoothness", Range(0, 1)) = 0.93
        _ReflectionStrength("Fuerza del reflejo", Range(0, 2)) = 1
        _SpecularIntensity("Brillo de las luces", Range(0, 5)) = 1.5
        _RefractionStrength("Refraccion (deformacion del fondo)", Range(0, 0.15)) = 0.04

        [Header(Espuma)]
        _FoamColor("Color de la espuma", Color) = (0.85, 0.95, 1, 1)
        _FoamDistance("Ancho de la espuma (metros)", Range(0, 3)) = 0.6
        _FoamNoiseScale("Detalle de la espuma", Range(0.1, 10)) = 2.5
        _FoamSoftness("Suavidad del borde de la espuma", Range(0.01, 0.5)) = 0.08

        [Header(Olas en los vertices)]
        _WaveHeight("Altura de las olas (0 = sin olas)", Range(0, 1)) = 0
        _WaveLength("Largo de las olas (metros)", Range(1, 50)) = 8
        _WaveSpeed("Velocidad de las olas", Range(0, 5)) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent-10"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "WaterForward"
            Tags { "LightMode" = "UniversalForward" }

            // El color final ya incluye el fondo (refracción), así que se escribe opaco.
            Blend One Zero
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex WaterVert
            #pragma fragment WaterFrag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_ATLAS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile _ _LIGHT_LAYERS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"

            // Superficie transparente: las sombras se leen en espacio de mundo, no en pantalla.
            #define _SURFACE_TYPE_TRANSPARENT 1

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            TEXTURE2D(_NormalMap); SAMPLER(sampler_NormalMap);

            CBUFFER_START(UnityPerMaterial)
                half4 _ShallowColor;
                half4 _DeepColor;
                half  _DepthDistance;
                half  _Clarity;
                half  _NormalStrength;
                half  _NormalTiling;
                half  _RippleStrength;
                half  _RippleScale;
                half  _FlowSpeed;
                half4 _FlowDirection;
                half  _Smoothness;
                half  _ReflectionStrength;
                half  _SpecularIntensity;
                half  _RefractionStrength;
                half4 _FoamColor;
                half  _FoamDistance;
                half  _FoamNoiseScale;
                half  _FoamSoftness;
                half  _WaveHeight;
                half  _WaveLength;
                half  _WaveSpeed;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float4 screenPos  : TEXCOORD2;
                half   fogFactor  : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // ---------------- Ruido procedural ----------------
            float W_Hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float W_Noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(W_Hash(i), W_Hash(i + float2(1, 0)), u.x),
                            lerp(W_Hash(i + float2(0, 1)), W_Hash(i + float2(1, 1)), u.x), u.y);
            }

            float W_Fbm(float2 p)
            {
                return W_Noise(p) * 0.55 + W_Noise(p * 2.13 + 7.1) * 0.3 + W_Noise(p * 4.37 + 3.3) * 0.15;
            }

            // Pendiente de la superficie por diferencias finitas: dos capas que se mueven distinto.
            float2 W_RippleSlope(float2 xz, float t)
            {
                float2 dir = normalize(_FlowDirection.xy + 1e-4);
                float2 p1 = xz / _RippleScale + dir * t;
                float2 p2 = xz / (_RippleScale * 0.63) - dir.yx * t * 1.3 + 13.7;
                const float e = 0.05;
                float h  = W_Fbm(p1) + W_Fbm(p2) * 0.6;
                float hx = W_Fbm(p1 + float2(e, 0)) + W_Fbm(p2 + float2(e, 0)) * 0.6;
                float hz = W_Fbm(p1 + float2(0, e)) + W_Fbm(p2 + float2(0, e)) * 0.6;
                return float2(hx - h, hz - h) / e;
            }

            // Olas de vértice: dos senos cruzados.
            float W_WaveHeight(float2 xz, float t)
            {
                float k = TWO_PI / _WaveLength;
                return (sin(dot(xz, float2(1, 0.3)) * k + t) + sin(dot(xz, float2(-0.4, 1)) * k * 1.37 + t * 1.21) * 0.5) * _WaveHeight * 0.66;
            }

            Varyings WaterVert(Attributes input)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

                if (_WaveHeight > 0.0)
                {
                    float t = _Time.y * _WaveSpeed;
                    float h  = W_WaveHeight(positionWS.xz, t);
                    float hx = W_WaveHeight(positionWS.xz + float2(0.1, 0), t);
                    float hz = W_WaveHeight(positionWS.xz + float2(0, 0.1), t);
                    positionWS.y += h;
                    normalWS = normalize(float3(-(hx - h) / 0.1, 1.0, -(hz - h) / 0.1));
                }

                o.positionWS = positionWS;
                o.normalWS = normalWS;
                o.positionCS = TransformWorldToHClip(positionWS);
                o.screenPos = ComputeScreenPos(o.positionCS);
                o.fogFactor = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half3 W_Specular(Light light, half3 normalWS, half3 viewDirWS, half smoothness)
            {
                half3 h = SafeNormalize(light.direction + viewDirWS);
                half nh = saturate(dot(normalWS, h));
                half exponent = exp2(10.0h * smoothness + 1.0h);
                half spec = pow(nh, exponent) * (exponent + 8.0h) / (8.0h * PI);
                half atten = light.distanceAttenuation * light.shadowAttenuation;
                return light.color * (spec * atten * saturate(dot(normalWS, light.direction) * 4.0h));
            }

            half3 W_Diffuse(Light light, half3 normalWS)
            {
                half nl = saturate(dot(normalWS, light.direction)) * 0.5h + 0.5h; // luz "envolvente" suave
                return light.color * (nl * light.distanceAttenuation * light.shadowAttenuation);
            }

            half4 WaterFrag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float3 positionWS = input.positionWS;
                half3 viewDirWS = SafeNormalize(GetWorldSpaceViewDir(positionWS));
                float t = _Time.y * _FlowSpeed;
                float2 xz = positionWS.xz;

                // ---------------- Normal ----------------
                float2 slope = W_RippleSlope(xz, t) * _RippleStrength * 0.15;

                float2 dir = normalize(_FlowDirection.xy + 1e-4);
                float2 uv1 = xz / _NormalTiling + dir * t * 0.5;
                float2 uv2 = xz / (_NormalTiling * 1.7) - dir.yx * t * 0.35 + 0.5;
                half3 n1 = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uv1), _NormalStrength);
                half3 n2 = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uv2), _NormalStrength);
                half2 mapSlope = n1.xy + n2.xy;

                half3 baseN = normalize(input.normalWS);
                half3 normalWS = normalize(baseN + half3(mapSlope.x - slope.x, 0.0h, mapSlope.y - slope.y));

                // ---------------- Profundidad ----------------
                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                float waterEye = input.screenPos.w;
                float sceneEye = LinearEyeDepth(SampleSceneDepth(screenUV), _ZBufferParams);
                float depthDiff = max(0.0, sceneEye - waterEye);

                // ---------------- Refracción ----------------
                float2 offset = normalWS.xz * _RefractionStrength * saturate(depthDiff);
                float2 refrUV = screenUV + offset;
                float refrEye = LinearEyeDepth(SampleSceneDepth(refrUV), _ZBufferParams);
                // Si lo que se ve deformado está por delante del agua, no se deforma (evita fantasmas).
                if (refrEye < waterEye) { refrUV = screenUV; refrEye = sceneEye; }
                float refrDepth = max(0.0, refrEye - waterEye);
                half3 refracted = SampleSceneColor(refrUV);

                // ---------------- Luz ----------------
                float4 shadowCoord = TransformWorldToShadowCoord(positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                half3 ambient = SampleSH(normalWS);

                half3 diffuseLight = ambient + W_Diffuse(mainLight, normalWS);
                half3 specular = W_Specular(mainLight, normalWS, viewDirWS, _Smoothness);

                #if defined(_ADDITIONAL_LIGHTS)
                InputData inputData = (InputData)0;
                inputData.positionWS = positionWS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = viewDirWS;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                half4 shadowMask = half4(1, 1, 1, 1);
                uint pixelLightCount = GetAdditionalLightsCount();

                #if USE_CLUSTER_LIGHT_LOOP
                [loop] for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
                {
                    CLUSTER_LIGHT_LOOP_SUBTRACTIVE_LIGHT_CHECK
                    Light light = GetAdditionalLight(lightIndex, positionWS, shadowMask);
                    diffuseLight += W_Diffuse(light, normalWS);
                    specular += W_Specular(light, normalWS, viewDirWS, _Smoothness);
                }
                #endif

                LIGHT_LOOP_BEGIN(pixelLightCount)
                    Light light = GetAdditionalLight(lightIndex, positionWS, shadowMask);
                    diffuseLight += W_Diffuse(light, normalWS);
                    specular += W_Specular(light, normalWS, viewDirWS, _Smoothness);
                LIGHT_LOOP_END
                #endif

                // ---------------- Color del agua ----------------
                // Cuanto más profundo, más se pierde el fondo y domina el color profundo.
                half depthT = 1.0h - exp(-refrDepth / _DepthDistance);
                half3 waterTint = lerp(_ShallowColor.rgb, _DeepColor.rgb, depthT);
                half3 seeThrough = refracted * _ShallowColor.rgb;          // fondo visto a través del agua
                half3 body = waterTint * diffuseLight;                      // el agua iluminada
                half visible = _Clarity * (1.0h - depthT);
                half3 color = lerp(body, seeThrough, visible);

                // ---------------- Reflejo (fresnel) ----------------
                half NdotV = saturate(dot(normalWS, viewDirWS));
                half fresnel = 0.02h + 0.98h * pow(1.0h - NdotV, 5.0h);
                half3 reflectDir = reflect(-viewDirWS, normalWS);
                half3 reflection = GlossyEnvironmentReflection(reflectDir, positionWS, 1.0h - _Smoothness, 1.0h,
                                                               GetNormalizedScreenSpaceUV(input.positionCS));
                color = lerp(color, reflection, saturate(fresnel * _ReflectionStrength));
                color += specular * _SpecularIntensity;

                // ---------------- Espuma ----------------
                if (_FoamDistance > 0.0)
                {
                    half edge = 1.0h - saturate(depthDiff / _FoamDistance);
                    half noise = W_Fbm(xz * _FoamNoiseScale + dir * t * 2.0);
                    half foam = smoothstep(noise - _FoamSoftness, noise + _FoamSoftness, edge) * _FoamColor.a;
                    half3 foamLit = _FoamColor.rgb * (ambient + W_Diffuse(mainLight, baseN));
                    color = lerp(color, foamLit, foam);
                }

                color = MixFog(color, input.fogFactor);
                return half4(color, 1.0h);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
