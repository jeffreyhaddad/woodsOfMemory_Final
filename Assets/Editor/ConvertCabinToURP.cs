using UnityEngine;
using UnityEditor;
using System.IO;

public class ConvertCabinToURP
{
    [MenuItem("Tools/Convert Cabin Materials to URP")]
    static void Convert()
    {
        string folder = "Assets/ThirdParty/Cabin/Models/Materials";
        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { folder });

        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        Shader urpParticle = Shader.Find("Universal Render Pipeline/Particles/Unlit");

        if (urpLit == null)
        {
            Debug.LogError("Could not find URP Lit shader. Is URP installed?");
            return;
        }

        int converted = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) continue;

            string shaderName = mat.shader.name;

            // Skip already-URP materials
            if (shaderName.Contains("Universal Render Pipeline"))
            {
                Debug.Log($"Skipping (already URP): {mat.name}");
                continue;
            }

            if (shaderName == "Particles/Additive" || shaderName == "Particles/Standard Unlit")
            {
                ConvertParticleMaterial(mat, urpParticle);
                converted++;
            }
            else if (shaderName == "Standard" || shaderName == "Standard (Specular setup)")
            {
                ConvertStandardMaterial(mat, urpLit);
                converted++;
            }
            else
            {
                Debug.LogWarning($"Unknown shader '{shaderName}' on {mat.name}, skipping.");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Converted {converted} cabin materials to URP.");
    }

    static void ConvertStandardMaterial(Material mat, Shader urpLit)
    {
        // Cache old values before switching shader
        Texture mainTex = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null;
        Texture bumpMap = mat.HasProperty("_BumpMap") ? mat.GetTexture("_BumpMap") : null;
        Texture metallicMap = mat.HasProperty("_MetallicGlossMap") ? mat.GetTexture("_MetallicGlossMap") : null;
        Texture occlusionMap = mat.HasProperty("_OcclusionMap") ? mat.GetTexture("_OcclusionMap") : null;
        Texture emissionMap = mat.HasProperty("_EmissionMap") ? mat.GetTexture("_EmissionMap") : null;

        Color color = mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;
        Color emissionColor = mat.HasProperty("_EmissionColor") ? mat.GetColor("_EmissionColor") : Color.black;
        float metallic = mat.HasProperty("_Metallic") ? mat.GetFloat("_Metallic") : 0f;
        float smoothness = mat.HasProperty("_Glossiness") ? mat.GetFloat("_Glossiness") : 0.5f;
        float bumpScale = mat.HasProperty("_BumpScale") ? mat.GetFloat("_BumpScale") : 1f;
        float occlusionStrength = mat.HasProperty("_OcclusionStrength") ? mat.GetFloat("_OcclusionStrength") : 1f;
        float cutoff = mat.HasProperty("_Cutoff") ? mat.GetFloat("_Cutoff") : 0.5f;
        float mode = mat.HasProperty("_Mode") ? mat.GetFloat("_Mode") : 0f;

        // Switch shader
        mat.shader = urpLit;

        // Assign textures
        mat.SetTexture("_BaseMap", mainTex);
        mat.SetTexture("_MainTex", mainTex); // URP Lit keeps _MainTex as alias
        mat.SetTexture("_BumpMap", bumpMap);
        mat.SetTexture("_MetallicGlossMap", metallicMap);
        mat.SetTexture("_OcclusionMap", occlusionMap);
        mat.SetTexture("_EmissionMap", emissionMap);

        // Assign values
        mat.SetColor("_BaseColor", color);
        mat.SetColor("_Color", color);
        mat.SetFloat("_Metallic", metallic);
        mat.SetFloat("_Smoothness", smoothness);
        mat.SetFloat("_BumpScale", bumpScale);
        mat.SetFloat("_OcclusionStrength", occlusionStrength);
        mat.SetFloat("_Cutoff", cutoff);

        // Emission
        if (emissionMap != null || emissionColor != Color.black)
        {
            mat.SetColor("_EmissionColor", emissionColor);
            mat.EnableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
        }

        // Normal map keyword
        if (bumpMap != null)
            mat.EnableKeyword("_NORMALMAP");

        // Metallic map keyword
        if (metallicMap != null)
            mat.EnableKeyword("_METALLICSPECGLOSSMAP");

        // Occlusion keyword
        if (occlusionMap != null)
            mat.EnableKeyword("_OCCLUSIONMAP");

        // Handle rendering mode: 0=Opaque, 1=Cutout, 2=Fade, 3=Transparent
        mat.SetFloat("_WorkflowMode", 1); // Metallic workflow

        if (mode == 0f)
        {
            // Opaque
            SetupOpaque(mat);
        }
        else if (mode == 1f)
        {
            // Cutout -> AlphaClip
            SetupAlphaClip(mat, cutoff);
        }
        else
        {
            // Fade/Transparent
            SetupTransparent(mat);
        }

        EditorUtility.SetDirty(mat);
        Debug.Log($"Converted: {mat.name} (mode {mode})");
    }

    static void ConvertParticleMaterial(Material mat, Shader urpParticle)
    {
        Texture mainTex = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null;
        Color tintColor = mat.HasProperty("_TintColor") ? mat.GetColor("_TintColor") : Color.white;

        mat.shader = urpParticle;

        mat.SetTexture("_BaseMap", mainTex);
        mat.SetTexture("_MainTex", mainTex);
        mat.SetColor("_BaseColor", tintColor);

        // Additive blending
        mat.SetFloat("_Surface", 1); // Transparent
        mat.SetFloat("_Blend", 2);   // Additive
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
        mat.SetInt("_ZWrite", 0);
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.renderQueue = 3000;

        EditorUtility.SetDirty(mat);
        Debug.Log($"Converted particle: {mat.name}");
    }

    static void SetupOpaque(Material mat)
    {
        mat.SetFloat("_Surface", 0);
        mat.SetFloat("_AlphaClip", 0);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
        mat.SetInt("_ZWrite", 1);
        mat.SetOverrideTag("RenderType", "Opaque");
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
    }

    static void SetupAlphaClip(Material mat, float cutoff)
    {
        mat.SetFloat("_Surface", 0);
        mat.SetFloat("_AlphaClip", 1);
        mat.SetFloat("_Cutoff", cutoff);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
        mat.SetInt("_ZWrite", 1);
        mat.SetOverrideTag("RenderType", "TransparentCutout");
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
        mat.EnableKeyword("_ALPHATEST_ON");
    }

    static void SetupTransparent(Material mat)
    {
        mat.SetFloat("_Surface", 1);
        mat.SetFloat("_AlphaClip", 0);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
    }
}
