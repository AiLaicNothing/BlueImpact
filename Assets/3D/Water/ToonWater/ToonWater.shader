// BlueImpact - Toon Water: agua estilizada para URP (Forward+), sin texturas obligatorias.
// - Color por profundidad en bandas (orilla clara, fondo oscuro), con transparencia en lo poco profundo.
// - Espuma de borde duro en la orilla y alrededor de lo que toca el agua.
// - Anillos/lineas de contorno alrededor de rocas e islas (opcional).
// - Lineas de "causticas" (celdas) suaves sobre la superficie.
// - Destellos (puntitos blancos) que parpadean.
// - Brillos toon de la luz principal y de las luces cercanas (antorchas, cristales), en franjas.
// - Se oscurece con la luz de la escena (en la cueva no brilla de mas).
// Requisitos (ya activos en el proyecto): Depth Texture y Opaque Texture en el URP Asset.
// Modo lago (por defecto): superficies horizontales, usa la profundidad del fondo.
// Modo cascada (casilla "Cascada"): usa las UV del mesh, con chorros que caen, espuma arriba y abajo.
Shader "BlueImpact/Toon Water"
{
    Properties
    {
        [Header(Modo)]
        [Toggle(_WATERFALL)] _Waterfall("Cascada (usar en mallas verticales)", Float) = 0
        [Toggle(_MAGIC)] _Magic("Agua magica (brilla sola, remolinos, vetas)", Float) = 0
        [Toggle] _DebugDepth("Ver profundidad (depuracion, solo agua magica)", Float) = 0
        _FloorDepth("Profundidad del fondo en aguas abiertas (m)", Range(0.1, 10)) = 1

        [Header(Color)]
        _ShallowColor("Color en la orilla", Color) = (0.20, 0.58, 0.66, 1)
        _DeepColor("Color en lo profundo", Color) = (0.03, 0.10, 0.22, 1)
        _DepthDistance("Distancia hasta el color profundo (m)", Range(0.1, 30)) = 3
        [IntRange] _ColorBands("Bandas de color (1 = degradado suave)", Range(1, 8)) = 3
        _Clarity("Transparencia en lo poco profundo", Range(0, 1)) = 0.45
        _Refraction("Deformacion del fondo", Range(0, 0.1)) = 0.015
        _HorizonColor("Color al mirar de lado (fresnel)", Color) = (0.35, 0.55, 0.75, 1)
        _HorizonStrength("Fuerza del fresnel", Range(0, 1)) = 0.35

        [Header(Luz de la escena)]
        _LightInfluence("Cuanto le afecta la luz de la escena", Range(0, 1)) = 0.75
        _AmbientBoost("Luz ambiente extra", Range(0, 2)) = 1

        [Header(Ondas)]
        _RippleScale("Tamano de las ondas (m)", Range(0.5, 20)) = 3
        _RippleStrength("Fuerza de las ondas", Range(0, 1)) = 0.35
        _FlowSpeed("Velocidad", Range(0, 2)) = 0.2
        _FlowDirection("Direccion del flujo (XZ)", Vector) = (1, 0.4, 0, 0)

        [Header(Brillos toon)]
        _SpecColor("Color del brillo", Color) = (1, 0.97, 0.85, 1)
        _SpecSize("Tamano del brillo", Range(0.5, 0.999)) = 0.93
        _SpecIntensity("Intensidad del brillo", Range(0, 5)) = 1.5

        [Header(Espuma)]
        _FoamColor("Color de la espuma", Color) = (0.88, 0.97, 1, 1)
        _FoamDistance("Ancho de la espuma (m)", Range(0, 3)) = 0.45
        _FoamNoiseScale("Detalle de la espuma", Range(0.2, 10)) = 3
        _FoamCutoff("Cantidad de espuma", Range(0, 1)) = 0.55

        [Header(Anillos de contorno)]
        _RingColor("Color de los anillos", Color) = (0.85, 0.95, 1, 0.8)
        _RingDistance("Alcance de los anillos (m, 0 = sin anillos)", Range(0, 5)) = 1.6
        _RingCount("Cantidad de anillos", Range(1, 6)) = 2
        _RingWidth("Grosor de los anillos", Range(0.02, 0.4)) = 0.12
        _RingSpeed("Velocidad de los anillos", Range(0, 2)) = 0.35

        [Header(Causticas y destellos)]
        _CausticColor("Color de las causticas", Color) = (0.6, 0.9, 1, 0.25)
        _CausticScale("Tamano de las celdas (m)", Range(0.3, 10)) = 2
        _CausticWidth("Grosor de las lineas", Range(0.01, 0.3)) = 0.07
        _SparkleColor("Color de los destellos", Color) = (1, 1, 1, 0.9)
        _SparkleDensity("Cantidad de destellos", Range(0, 1)) = 0.25
        _SparkleSize("Tamano de los destellos", Range(0.01, 0.2)) = 0.06
        _SparkleScale("Separacion de los destellos (m)", Range(0.2, 5)) = 1.2

        [Header(Agua magica  solo con la casilla Agua magica)]
        [HDR] _MagicShallow("Magica: color cerca de la orilla", Color) = (0.1, 0.75, 0.7, 1)
        [HDR] _MagicDeep("Magica: color base del agua", Color) = (0.0, 0.16, 0.19, 1)
        _MagicGlow("Magica: brillo propio", Range(0, 4)) = 1
        [HDR] _EdgeGlowColor("Magica: brillo de la orilla", Color) = (0.5, 2.2, 2.0, 1)
        _EdgeGlowWidth("Magica: ancho del brillo de la orilla (m)", Range(0.02, 3)) = 0.3
        [HDR] _FilamentColor("Magica: color de las vetas", Color) = (0.15, 1.3, 1.1, 1)
        _FilamentScale("Magica: tamano de las vetas (m)", Range(2, 80)) = 16
        _FilamentSharpness("Magica: finura de las vetas", Range(2, 40)) = 32
        _FilamentCoverage("Magica: zonas con vetas", Range(0, 1)) = 0.7
        [HDR] _SwirlColor("Magica: color de los remolinos", Color) = (0.25, 1.6, 1.4, 1)
        _SwirlScale("Magica: separacion de los remolinos (m)", Range(5, 200)) = 80
        _SwirlDensity("Magica: cantidad de remolinos", Range(0, 1)) = 0.35
        _SwirlTightness("Magica: vueltas del remolino", Range(1, 10)) = 4
        _SwirlSpeed("Magica: velocidad de giro", Range(0, 5)) = 0.6
        _SwirlGlow("Magica: brillo en el centro del remolino", Range(0, 2)) = 0.6
        _StarAmount("Magica: cantidad de estrellitas", Range(0, 1)) = 0.8
        _StarScale("Magica: separacion de las estrellitas (m)", Range(0.3, 10)) = 1
        [HDR] _StarColor("Magica: color de las estrellitas", Color) = (2, 2.6, 2.6, 1)

        [Header(Cascada  solo con la casilla Cascada)]
        _FallDirection("Direccion de caida en UV (XY)", Vector) = (0, -1, 0, 0)
        _FallSpeed("Velocidad de caida", Range(0, 5)) = 1.2
        _FallTiling("Repeticion de los chorros (X ancho, Y largo)", Vector) = (4, 1, 0, 0)
        _StreakAmount("Cantidad de chorros claros", Range(0, 1)) = 0.45
        _StreakColor("Color de los chorros", Color) = (0.85, 0.97, 1, 0.9)
        _TopFoam("Espuma arriba (en UV, 0 = sin espuma)", Range(0, 0.5)) = 0.08
        _BottomFoam("Espuma abajo (en UV, 0 = sin espuma)", Range(0, 0.5)) = 0.12
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
            Name "ToonWaterForward"
            Tags { "LightMode" = "UniversalForward" }

            // El color final ya incluye el fondo (refraccion): se escribe opaco.
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
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile _ _LIGHT_LAYERS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_instancing
            #pragma shader_feature_local_fragment _WATERFALL
            #pragma shader_feature_local_fragment _MAGIC
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"

            // Superficie transparente: sombras en espacio de mundo, no de pantalla.
            #define _SURFACE_TYPE_TRANSPARENT 1

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _ShallowColor;
                half4 _DeepColor;
                half  _DepthDistance;
                half  _ColorBands;
                half  _Clarity;
                half  _Refraction;
                half4 _HorizonColor;
                half  _HorizonStrength;
                half  _LightInfluence;
                half  _AmbientBoost;
                half  _RippleScale;
                half  _RippleStrength;
                half  _FlowSpeed;
                half4 _FlowDirection;
                half4 _SpecColor;
                half  _SpecSize;
                half  _SpecIntensity;
                half4 _FoamColor;
                half  _FoamDistance;
                half  _FoamNoiseScale;
                half  _FoamCutoff;
                half4 _RingColor;
                half  _RingDistance;
                half  _RingCount;
                half  _RingWidth;
                half  _RingSpeed;
                half4 _CausticColor;
                half  _CausticScale;
                half  _CausticWidth;
                half4 _SparkleColor;
                half  _SparkleDensity;
                half  _SparkleSize;
                half  _SparkleScale;
                half4 _FallDirection;
                half  _FallSpeed;
                half4 _FallTiling;
                half  _StreakAmount;
                half4 _StreakColor;
                half  _TopFoam;
                half  _BottomFoam;
                half4 _MagicShallow;
                half4 _MagicDeep;
                half  _MagicGlow;
                half4 _EdgeGlowColor;
                half  _EdgeGlowWidth;
                half4 _FilamentColor;
                half  _FilamentScale;
                half  _FilamentSharpness;
                half4 _SwirlColor;
                half  _SwirlScale;
                half  _SwirlDensity;
                half  _SwirlTightness;
                half  _SwirlSpeed;
                half  _SwirlGlow;
                half  _FilamentCoverage;
                half  _StarScale;
                half  _StarAmount;
                half  _DebugDepth;
                half  _FloorDepth;
                half4 _StarColor;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float4 screenPos  : TEXCOORD2;
                half   fogFactor  : TEXCOORD3;
                float2 uv         : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // ---------------- Ruido ----------------
            float TW_Hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float2 TW_Hash2(float2 p)
            {
                return float2(TW_Hash(p), TW_Hash(p + 17.31));
            }

            float TW_Noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(TW_Hash(i), TW_Hash(i + float2(1, 0)), u.x),
                            lerp(TW_Hash(i + float2(0, 1)), TW_Hash(i + float2(1, 1)), u.x), u.y);
            }

            float TW_Fbm(float2 p)
            {
                return TW_Noise(p) * 0.6 + TW_Noise(p * 2.17 + 5.3) * 0.4;
            }

            // Celdas (Voronoi): distancia al borde entre celdas -> lineas tipo causticas.
            float TW_CellEdge(float2 p, float t)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float d1 = 8.0, d2 = 8.0;
                [unroll] for (int y = -1; y <= 1; y++)
                [unroll] for (int x = -1; x <= 1; x++)
                {
                    float2 g = float2(x, y);
                    float2 o = TW_Hash2(i + g);
                    o = 0.5 + 0.4 * sin(t + 6.2831 * o);   // los puntos se mueven
                    float d = length(g + o - f);
                    if (d < d1) { d2 = d1; d1 = d; }
                    else if (d < d2) { d2 = d; }
                }
                return d2 - d1;
            }

            // Pendiente de las ondas (para deformar el fondo y romper los brillos).
            float2 TW_Slope(float2 xz, float t)
            {
                float2 dir = normalize(_FlowDirection.xy + 1e-4);
                float2 p1 = xz / _RippleScale + dir * t;
                float2 p2 = xz / (_RippleScale * 0.7) - dir.yx * t * 1.2 + 9.1;
                const float e = 0.07;
                float h  = TW_Fbm(p1) + TW_Fbm(p2);
                float hx = TW_Fbm(p1 + float2(e, 0)) + TW_Fbm(p2 + float2(e, 0));
                float hz = TW_Fbm(p1 + float2(0, e)) + TW_Fbm(p2 + float2(0, e));
                return float2(hx - h, hz - h) / e;
            }

            // Remolinos grandes y pocos: brazos en espiral con brillo suave hacia el centro.
            float TW_Swirls(float2 p, float t)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float best = 0.0;
                [unroll] for (int y = -1; y <= 1; y++)
                [unroll] for (int x = -1; x <= 1; x++)
                {
                    float2 g = float2(x, y);
                    float2 id = i + g;
                    if (TW_Hash(id + 3.7) > _SwirlDensity) continue;
                    float2 c = g + 0.3 + 0.4 * TW_Hash2(id);
                    float2 v = f - c;
                    float radius = lerp(0.36, 0.5, TW_Hash(id + 5.5));    // cada remolino de distinto tamano
                    float r = length(v) / radius;                         // 0 centro -> 1 borde
                    if (r > 1.0) continue;
                    float spin = TW_Hash(id + 9.1) > 0.5 ? 1.0 : -1.0;
                    float a = atan2(v.y, v.x) * spin;
                    // Espiral logaritmica: los brazos se abren hacia afuera.
                    float phase = a * 3.0 + log(r + 0.05) * _SwirlTightness * 2.0 - t * _SwirlSpeed * 2.0;
                    // Brazos principales finos + hebras secundarias entre ellos.
                    float arms = smoothstep(0.55, 1.0, sin(phase)) + smoothstep(0.8, 1.0, sin(phase * 2.0 + 1.3)) * 0.45;
                    arms *= pow(1.0 - r, 1.2) * smoothstep(0.03, 0.18, r);
                    float halo = pow(1.0 - r, 1.6) * 0.35;                // resplandor de todo el remolino
                    float core = pow(1.0 - r, 3.0) * _SwirlGlow;
                    best = max(best, arms + halo + core);
                }
                return best;
            }

            // Vetas: lineas finas y curvas con un halo suave, solo en algunas zonas del lago.
            float TW_Filaments(float2 p, float t)
            {
                float2 w = float2(TW_Fbm(p * 0.8 + float2(t * 0.1, 0)), TW_Fbm(p * 0.8 + float2(5.2, -t * 0.08)));
                float n = TW_Fbm(p + w * 1.8);
                float ridge = saturate(1.0 - abs(n * 2.0 - 1.0));
                float veins = pow(ridge, _FilamentSharpness) + pow(ridge, _FilamentSharpness * 0.2) * 0.18;
                float zone = smoothstep(1.0 - _FilamentCoverage, 1.0 - _FilamentCoverage + 0.3, TW_Fbm(p * 0.35 + 17.0));
                return veins * zone;
            }

            Varyings WaterVert(Attributes input)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                o.normalWS = TransformObjectToWorldNormal(input.normalOS);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.screenPos = ComputeScreenPos(o.positionCS);
                o.fogFactor = ComputeFogFactor(o.positionCS.z);
                o.uv = input.uv;
                return o;
            }

            // Luz toon: cuanta luz llega (difusa, suave) y brillo en franja dura.
            void TW_AddLight(Light light, half3 normalWS, half3 viewDirWS, inout half3 diffuse, inout half3 spec)
            {
                half atten = light.distanceAttenuation * light.shadowAttenuation;
                half nl = saturate(dot(normalWS, light.direction)) * 0.5h + 0.5h;
                diffuse += light.color * (nl * atten);

                half3 h = SafeNormalize(light.direction + viewDirWS);
                half nh = saturate(dot(normalWS, h));
                half s = step(_SpecSize, nh);                 // borde duro
                spec += light.color * (s * atten);
            }

            half4 WaterFrag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float3 positionWS = input.positionWS;
                half3 viewDirWS = SafeNormalize(GetWorldSpaceViewDir(positionWS));
                float t = _Time.y * _FlowSpeed;
                float2 xz = positionWS.xz;
                float2 dir = normalize(_FlowDirection.xy + 1e-4);

                // ---------------- Normal (ondas) ----------------
                half3 baseN = normalize(input.normalWS);
                float2 slope = TW_Slope(xz, t) * _RippleStrength * 0.25;
                half3 normalWS = normalize(baseN + half3(-slope.x, 0.0h, -slope.y));

                // ---------------- Profundidad ----------------
                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                float waterEye = input.screenPos.w;
                float sceneEye = LinearEyeDepth(SampleSceneDepth(screenUV), _ZBufferParams);
                float depthDiff = max(0.0, sceneEye - waterEye);

                float2 refrUV = screenUV + normalWS.xz * _Refraction * saturate(depthDiff);
                float refrEye = LinearEyeDepth(SampleSceneDepth(refrUV), _ZBufferParams);
                if (refrEye < waterEye) { refrUV = screenUV; refrEye = sceneEye; }
                float refrDepth = max(0.0, refrEye - waterEye);
                half3 background = SampleSceneColor(refrUV);

                // ---------------- Color por bandas ----------------
                half depthT = 1.0h - exp(-refrDepth / _DepthDistance);
                if (_ColorBands > 1.0h)
                    depthT = saturate(floor(depthT * _ColorBands + 0.5h) / _ColorBands);
                half3 waterColor = lerp(_ShallowColor.rgb, _DeepColor.rgb, depthT);

                // Fondo visible en lo poco profundo, teñido por el agua.
                half visible = _Clarity * (1.0h - depthT);
                waterColor = lerp(waterColor, background * _ShallowColor.rgb * 1.4h, visible);

                // Fresnel estilizado (color, no reflejo real).
                half NdotV = saturate(dot(baseN, viewDirWS));
                half fres = pow(1.0h - NdotV, 4.0h) * _HorizonStrength;
                waterColor = lerp(waterColor, _HorizonColor.rgb, fres);

                // ---------------- Luz de la escena ----------------
                float4 shadowCoord = TransformWorldToShadowCoord(positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                half3 diffuse = SampleSH(baseN) * _AmbientBoost;
                half3 spec = 0;
                TW_AddLight(mainLight, normalWS, viewDirWS, diffuse, spec);

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
                    TW_AddLight(light, normalWS, viewDirWS, diffuse, spec);
                }
                #endif

                LIGHT_LOOP_BEGIN(pixelLightCount)
                    Light light = GetAdditionalLight(lightIndex, positionWS, shadowMask);
                    TW_AddLight(light, normalWS, viewDirWS, diffuse, spec);
                LIGHT_LOOP_END
                #endif

                // En zonas oscuras el agua se oscurece; cerca de luces se ilumina.
                half3 lighting = lerp(half3(1, 1, 1), diffuse, _LightInfluence);
                half3 color = waterColor * lighting;

            #if defined(_WATERFALL)
                // ================= MODO CASCADA =================
                // Coordenadas a lo largo (caida) y a lo ancho de la cascada, en las UV del mesh.
                float2 fall = normalize(_FallDirection.xy + 1e-4);
                float2 across = float2(-fall.y, fall.x);
                float along = dot(input.uv, -fall);          // 0 arriba -> 1 abajo (si la UV va asi)
                float side = dot(input.uv, across);
                float2 sp = float2(side * _FallTiling.x, along * _FallTiling.y * 3.0 + _Time.y * _FallSpeed);

                // Chorros: ruido estirado en la direccion de caida, cortado en bandas.
                half streak = TW_Noise(float2(sp.x * 2.0, sp.y * 0.35)) * 0.65 + TW_Noise(float2(sp.x * 5.0, sp.y * 0.8) + 3.1) * 0.35;
                half bands = step(1.0h - _StreakAmount, streak);
                half3 fallColor = lerp(_DeepColor.rgb, _ShallowColor.rgb, step(0.4h, streak)) ;
                fallColor = lerp(fallColor, _StreakColor.rgb, bands * _StreakColor.a);
                color = fallColor * lighting + spec * _SpecColor.rgb * _SpecIntensity;

                // Espuma arriba (donde empieza a caer) y abajo (donde choca), con borde irregular.
                half fn = TW_Noise(float2(sp.x * 3.0, _Time.y * _FallSpeed * 2.0));
                half topF = step(frac(along) , _TopFoam * (0.6h + fn * 0.8h)) * step(0.0001h, _TopFoam);
                half botF = step(1.0h - _BottomFoam * (0.6h + fn * 0.8h), frac(along)) * step(0.0001h, _BottomFoam);
                // Y tambien donde toca rocas o el lago (profundidad).
                half hit = 0;
                if (_FoamDistance > 0.0h)
                {
                    half foamEdge = 1.0h - saturate(depthDiff / _FoamDistance);
                    hit = step(1.0h - foamEdge * _FoamCutoff * 1.6h, TW_Fbm(sp * 0.7)) * step(0.001h, foamEdge);
                }
                half foamAll = saturate(topF + botF + hit);
                color = lerp(color, _FoamColor.rgb * max(lighting, 0.4h), foamAll * _FoamColor.a);

                color = MixFog(color, input.fogFactor);
                return half4(color, 1.0h);
            #else
            #if defined(_MAGIC)
                // ================= AGUA MAGICA =================
                // Brilla por si misma (emision): no depende de la luz de la escena.

                // Todo se mide respecto a la profundidad del fondo (_FloorDepth):
                // asi funciona aunque el lago sea poco profundo y el fondo plano.
                half F = max(_FloorDepth, 0.05h);
                half contact = saturate(depthDiff / F);          // 0 = tocando un objeto, 1 = agua abierta

                // Vista de depuracion: negro = borde de islas/pilares, blanco = agua abierta.
                // Ajusta "Profundidad del fondo" hasta que el agua abierta se vea blanca
                // y solo los contornos queden oscuros.
                if (_DebugDepth > 0.5h)
                    return half4(contact.xxx, 1.0h);

                // Agua abierta oscura; turquesa solo cerca de los bordes, en bandas (toon).
                half shore = 1.0h - smoothstep(0.15h, 0.6h, contact);
                shore = floor(shore * 3.0h + 0.5h) / 3.0h;
                half patches = TW_Fbm(xz * 0.03 + t * 0.03);
                half patchLight = smoothstep(0.45h, 0.85h, patches);            // manchas cian
                // Canales abiertos muy oscuros; manchas luminosas solo en algunas zonas.
                half3 deep = _MagicDeep.rgb * lerp(0.35h, 1.25h, patches)
                           + _MagicShallow.rgb * patchLight * 0.3h;
                half3 magic = lerp(deep, _MagicShallow.rgb * 0.7h, shore) * _MagicGlow;

                // Vetas: linea nitida (toon) + halo suave.
                half fil = TW_Filaments(xz / _FilamentScale, _Time.y * _FlowSpeed);
                half filSharp = smoothstep(0.35h, 0.45h, fil);
                half filGlow = smoothstep(0.0h, 0.5h, fil);                      // resplandor ancho y suave
                magic += _FilamentColor.rgb * (filSharp + fil * 0.35h + filGlow * 0.25h);

                // Remolinos: brazos nitidos + halo.
                half sw = TW_Swirls(xz / _SwirlScale, _Time.y);
                half swSharp = smoothstep(0.42h, 0.52h, sw);
                half swGlow = smoothstep(0.0h, 0.7h, sw);
                magic += _SwirlColor.rgb * (swSharp * 0.9h + sw * 0.3h + swGlow * 0.3h);

                // Brillo en la orilla: franja fina y nitida.
                half edgeW = min(_EdgeGlowWidth, F * 0.5h);
                half edgeBand = 1.0h - smoothstep(edgeW * 0.6h, edgeW, depthDiff);   // borde duro
                magic += _EdgeGlowColor.rgb * edgeBand;

                // Estrellitas: pequenas y nitidas, cada una con su tamano y ritmo.
                if (_StarAmount > 0.0h)
                {
                    float2 st = xz / _StarScale + dir * t * 0.3;
                    float2 cell = floor(st);
                    float2 center = cell + 0.15 + 0.7 * TW_Hash2(cell);
                    half size = lerp(0.025h, 0.11h, pow(TW_Hash(cell + 7.7), 3.0));
                    half starDot = step(length(st - center), size);
                    // Mas estrellitas donde el agua brilla (vetas, remolinos, manchas cian, orillas).
                    half lum = saturate(fil * 2.0h + sw + patchLight + shore);
                    half has = step(1.0h - _StarAmount * lerp(0.25h, 1.3h, lum), TW_Hash(cell + 2.3));
                    half twinkle = step(0.35h, 0.5h + 0.5h * sin(_Time.y * (2.0 + TW_Hash(cell) * 4.0) + TW_Hash(cell + 1.1) * 6.28));
                    magic += _StarColor.rgb * starDot * has * twinkle * lerp(0.5h, 1.5h, TW_Hash(cell + 4.4));
                }

                // Brillos de las luces (antorchas, cristales) encima.
                magic += spec * _SpecColor.rgb * _SpecIntensity;

                // Anillos concentricos alrededor de islas y pilares.
                if (_RingDistance > 0.0h)
                {
                    half ringRange = min(_RingDistance, F * 0.9h);
                    half ringMask = 1.0h - saturate(depthDiff / ringRange);
                    half wobble = (TW_Noise(xz * 0.6 + t) - 0.5h) * 0.3h;
                    half r = frac(depthDiff / ringRange * _RingCount - _Time.y * _RingSpeed + wobble);
                    half ring = step(1.0h - _RingWidth, r) * step(0.02h, ringMask) * step(depthDiff, ringRange);
                    magic = lerp(magic, _RingColor.rgb * 1.6h, ring * _RingColor.a);
                }

                // Espuma de contacto: linea continua pegada al borde + borde exterior ondulado.
                if (_FoamDistance > 0.0h)
                {
                    half n = TW_Fbm(xz * _FoamNoiseScale + dir * t * 1.5);
                    // Grosor irregular a lo largo del borde (ruido de baja frecuencia).
                    half thick = lerp(0.5h, 1.6h, TW_Noise(xz * 0.18 + t * 0.2));
                    half foamD = min(_FoamDistance, F * 0.35h) * thick;
                    half solid = step(depthDiff, foamD * 0.5h);
                    half wavy = step(depthDiff, foamD * (0.5h + n * _FoamCutoff * 1.2h));
                    // Segunda linea ondulada, despegada del borde.
                    half d2 = depthDiff - foamD * (1.25h + (n - 0.5h) * 0.6h);
                    half second = step(abs(d2), foamD * 0.12h) * step(depthDiff, F * 0.9h);
                    half foam = max(max(solid, wavy), second);
                    magic = lerp(magic, _FoamColor.rgb * 1.8h, foam * _FoamColor.a);
                }

                magic = MixFog(magic, input.fogFactor);
                return half4(magic, 1.0h);
            #else

                // ---------------- Causticas (lineas de celdas) ----------------
                half edge = TW_CellEdge(xz / _CausticScale + dir * t * 0.3, _Time.y * 0.6);
                half caustic = 1.0h - smoothstep(0.0h, _CausticWidth, edge);
                color += _CausticColor.rgb * caustic * _CausticColor.a * (1.0h - depthT * 0.6h) * lighting;

                // ---------------- Destellos ----------------
                if (_SparkleDensity > 0.0h)
                {
                    float2 sp = xz / _SparkleScale + dir * t * 0.5;
                    float2 cell = floor(sp);
                    float2 center = cell + 0.2 + 0.6 * TW_Hash2(cell);
                    half sparkleDot = step(length(sp - center), _SparkleSize * 4.0);
                    // Cada destello parpadea a su ritmo.
                    half blink = step(1.0h - _SparkleDensity, TW_Hash(cell + floor(_Time.y * 1.5 + TW_Hash(cell) * 7.0)));
                    color = lerp(color, _SparkleColor.rgb * max(lighting, 0.5h), sparkleDot * blink * _SparkleColor.a);
                }

                // ---------------- Brillos toon de las luces ----------------
                color += spec * _SpecColor.rgb * _SpecIntensity;

                // ---------------- Anillos alrededor de objetos ----------------
                if (_RingDistance > 0.0h)
                {
                    half ringMask = 1.0h - saturate(depthDiff / _RingDistance);
                    half wobble = TW_Noise(xz * 1.3 + t) * 0.15h;
                    half r = frac(depthDiff / _RingDistance * _RingCount - _Time.y * _RingSpeed + wobble);
                    half ring = step(1.0h - _RingWidth, r) * step(0.001h, ringMask) * ringMask;
                    color = lerp(color, _RingColor.rgb * max(lighting, 0.4h), ring * _RingColor.a);
                }

                // ---------------- Espuma de orilla ----------------
                if (_FoamDistance > 0.0h)
                {
                    half foamEdge = 1.0h - saturate(depthDiff / _FoamDistance);
                    half n = TW_Fbm(xz * _FoamNoiseScale + dir * t * 1.5);
                    half foam = step(1.0h - foamEdge * _FoamCutoff * 1.6h, n) * step(0.001h, foamEdge);
                    foam = max(foam, step(0.85h, foamEdge));     // linea solida pegada al borde
                    color = lerp(color, _FoamColor.rgb * max(lighting, 0.4h), foam * _FoamColor.a);
                }

                color = MixFog(color, input.fogFactor);
                return half4(color, 1.0h);
            #endif
            #endif
            }
            ENDHLSL
        }
    }

    FallBack Off
}
