// BlueImpact - Toon Lit: iluminación.
// Es UniversalFragmentPBR de URP 17.3 con un cambio: la luz directa (principal y adicionales)
// pasa por una rampa toon. Todo lo demás es igual al Lit: lightmaps, light probes,
// reflection probes, shadowmask, SSAO, decals y Forward+.
// _ToonStrength = 0 se ve como el Lit normal; 1 = sombreado toon completo.
#ifndef BLUEIMPACT_TOON_LIT_LIGHTING_INCLUDED
#define BLUEIMPACT_TOON_LIT_LIGHTING_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

half3 ToonLitDirect(BRDFData brdfData, Light light, half3 normalWS, half3 viewDirectionWS, bool specularHighlightsOff)
{
    half NdotL = dot(normalWS, light.direction);
    half lambert = saturate(NdotL);

    // Rampa: corte entre zona iluminada y zona de sombra.
    half ramp = smoothstep(_ShadeStep - _ShadeFeather, _ShadeStep + _ShadeFeather, NdotL);
    // Las sombras proyectadas también se cortan con borde duro.
    half shadow = light.shadowAttenuation;
    half toonShadow = smoothstep(0.5h - _ShadeFeather, 0.5h + _ShadeFeather, shadow);
    half litMask = ramp * toonShadow;

    // En la sombra la luz no desaparece: se tiñe con _ShadeColor (como el Toon Shader).
    half3 toonDiffuse = lerp(_ShadeColor.rgb, half3(1.0h, 1.0h, 1.0h), litMask);
    half3 pbrDiffuse = (lambert * shadow).xxx;
    half3 diffuseTerm = lerp(pbrDiffuse, toonDiffuse, _ToonStrength);

    half3 lightColor = light.color * light.distanceAttenuation;
    half3 color = brdfData.diffuse * diffuseTerm * lightColor;

#ifndef _SPECULARHIGHLIGHTS_OFF
    [branch] if (!specularHighlightsOff)
    {
        half specMask = lerp(lambert * shadow, litMask, _ToonStrength);
        color += brdfData.specular * DirectBRDFSpecular(brdfData, normalWS, light.direction, viewDirectionWS)
                 * specMask * lightColor;
    }
#endif

    return color;
}

half4 ToonLitFragment(InputData inputData, SurfaceData surfaceData)
{
    #if defined(_SPECULARHIGHLIGHTS_OFF)
    bool specularHighlightsOff = true;
    #else
    bool specularHighlightsOff = false;
    #endif
    BRDFData brdfData;
    InitializeBRDFData(surfaceData, brdfData);

    #if defined(DEBUG_DISPLAY)
    half4 debugColor;
    if (CanDebugOverrideOutputColor(inputData, surfaceData, brdfData, debugColor))
    {
        return debugColor;
    }
    #endif

    BRDFData brdfDataClearCoat = CreateClearCoatBRDFData(surfaceData, brdfData);
    half4 shadowMask = CalculateShadowMask(inputData);
    AmbientOcclusionFactor aoFactor = CreateAmbientOcclusionFactor(inputData, surfaceData);
    uint meshRenderingLayers = GetMeshRenderingLayer();
    Light mainLight = GetMainLight(inputData, shadowMask, aoFactor);

    MixRealtimeAndBakedGI(mainLight, inputData.normalWS, inputData.bakedGI);

    LightingData lightingData = CreateLightingData(inputData, surfaceData);

    // GI igual que el Lit: luz bakeada, probes y reflejos.
    lightingData.giColor = GlobalIllumination(brdfData, brdfDataClearCoat, surfaceData.clearCoatMask,
                                              inputData.bakedGI, aoFactor.indirectAmbientOcclusion, inputData.positionWS,
                                              inputData.normalWS, inputData.viewDirectionWS, inputData.normalizedScreenSpaceUV);
#ifdef _LIGHT_LAYERS
    if (IsMatchingLightLayer(mainLight.layerMask, meshRenderingLayers))
#endif
    {
        lightingData.mainLightColor = ToonLitDirect(brdfData, mainLight,
                                                    inputData.normalWS, inputData.viewDirectionWS, specularHighlightsOff);
    }

    #if defined(_ADDITIONAL_LIGHTS)
    uint pixelLightCount = GetAdditionalLightsCount();

    #if USE_CLUSTER_LIGHT_LOOP
    [loop] for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
    {
        CLUSTER_LIGHT_LOOP_SUBTRACTIVE_LIGHT_CHECK

        Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);

#ifdef _LIGHT_LAYERS
        if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
#endif
        {
            lightingData.additionalLightsColor += ToonLitDirect(brdfData, light,
                                                                inputData.normalWS, inputData.viewDirectionWS, specularHighlightsOff);
        }
    }
    #endif

    LIGHT_LOOP_BEGIN(pixelLightCount)
        Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);

#ifdef _LIGHT_LAYERS
        if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
#endif
        {
            lightingData.additionalLightsColor += ToonLitDirect(brdfData, light,
                                                                inputData.normalWS, inputData.viewDirectionWS, specularHighlightsOff);
        }
    LIGHT_LOOP_END
    #endif

    #if defined(_ADDITIONAL_LIGHTS_VERTEX)
    lightingData.vertexLightingColor += inputData.vertexLighting * brdfData.diffuse;
    #endif

    // Rim light toon (borde iluminado), opcional: _RimStrength = 0 lo apaga.
    half NdotV = saturate(dot(inputData.normalWS, inputData.viewDirectionWS));
    half rim = pow(1.0h - NdotV, _RimPower);
    rim = lerp(rim, smoothstep(0.45h, 0.55h, rim), _ToonStrength);
    lightingData.emissionColor += _RimColor.rgb * rim * _RimStrength;

#if REAL_IS_HALF
    return min(CalculateFinalColor(lightingData, surfaceData.alpha), HALF_MAX);
#else
    return CalculateFinalColor(lightingData, surfaceData.alpha);
#endif
}

#endif
