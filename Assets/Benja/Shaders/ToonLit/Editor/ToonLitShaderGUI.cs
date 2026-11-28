using UnityEditor;
using UnityEngine;

namespace BlueImpactShaders
{
    // Inspector del shader "BlueImpact/Toon Lit".
    // Dibuja todas las propiedades y activa solos los keywords que en el Lit pone su propio
    // inspector (normal map, metallic map, occlusion, parallax, detail, emisión).
    public class ToonLitShaderGUI : ShaderGUI
    {
        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            EditorGUI.BeginChangeCheck();
            materialEditor.PropertiesDefaultGUI(properties);

            EditorGUILayout.Space();
            materialEditor.LightmapEmissionProperty();

            // Siempre se actualizan, para que materiales recién creados o convertidos queden bien.
            EditorGUI.EndChangeCheck();
            foreach (Object target in materialEditor.targets)
            {
                if (target is Material material) SetKeywords(material);
            }
        }

        public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
        {
            base.AssignNewShaderToMaterial(material, oldShader, newShader);
            SetKeywords(material);
        }

        public override void ValidateMaterial(Material material)
        {
            SetKeywords(material);
        }

        private static void SetKeywords(Material m)
        {
            bool hasNormal = HasTexture(m, "_BumpMap");
            bool hasDetail = HasTexture(m, "_DetailAlbedoMap") || HasTexture(m, "_DetailNormalMap");

            SetKeyword(m, "_NORMALMAP", hasNormal || HasTexture(m, "_DetailNormalMap"));
            SetKeyword(m, "_METALLICSPECGLOSSMAP", HasTexture(m, "_MetallicGlossMap"));
            SetKeyword(m, "_OCCLUSIONMAP", HasTexture(m, "_OcclusionMap"));
            SetKeyword(m, "_PARALLAXMAP", HasTexture(m, "_ParallaxMap"));
            SetKeyword(m, "_DETAIL_MULX2", hasDetail);

            // Emisión: activa el keyword y le dice al bake si el material emite luz.
            bool emission = m.HasProperty("_EmissionColor") && m.GetColor("_EmissionColor").maxColorComponent > 0.0001f;
            SetKeyword(m, "_EMISSION", emission);

            MaterialGlobalIlluminationFlags flags = m.globalIlluminationFlags;
            if (emission) flags &= ~MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            else flags |= MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            m.globalIlluminationFlags = flags;
        }

        private static bool HasTexture(Material m, string name)
        {
            return m.HasProperty(name) && m.GetTexture(name) != null;
        }

        private static void SetKeyword(Material m, string keyword, bool on)
        {
            if (on) m.EnableKeyword(keyword);
            else m.DisableKeyword(keyword);
        }
    }
}
