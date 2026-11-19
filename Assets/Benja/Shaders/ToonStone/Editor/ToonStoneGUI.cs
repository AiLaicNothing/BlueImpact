using System;
using UnityEditor;
using UnityEngine;

// Inspector del shader "Benja/Toon Stone (Anti-Tiling)".
// Reutiliza el inspector original de Unity Toon Shader (UTS3GUI) y añade abajo
// los parametros de la piedra de mundo.
public class ToonStoneGUI : ShaderGUI
{
    ShaderGUI _uts;
    static readonly string[] Extra =
    {
        "_UseWorldStone", "_StoneTiling", "_StoneRotation",
        "_StoneMacro", "_StoneMacroScale", "_StoneMacroStrength", "_StoneBlendSharp"
    };

    ShaderGUI Uts()
    {
        if (_uts != null) return _uts;
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            var t = asm.GetType("UnityEditor.Rendering.Toon.UTS3GUI");
            if (t != null)
            {
                try { _uts = (ShaderGUI)Activator.CreateInstance(t, true); } catch { _uts = null; }
                break;
            }
        }
        return _uts;
    }

    public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
    {
        var u = Uts();
        if (u != null) u.AssignNewShaderToMaterial(material, oldShader, newShader);
        else base.AssignNewShaderToMaterial(material, oldShader, newShader);
    }

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        var u = Uts();
        if (u != null) u.OnGUI(materialEditor, properties);
        else base.OnGUI(materialEditor, properties);

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Piedra de mundo (anti-tiling)", EditorStyles.boldLabel);
        foreach (var name in Extra)
        {
            var p = FindProperty(name, properties, false);
            if (p != null) materialEditor.ShaderProperty(p, p.displayName);
        }
    }
}
