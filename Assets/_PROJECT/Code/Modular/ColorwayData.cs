using UnityEngine;

namespace ARPG_V2.Modular
{
    /// <summary>
    /// A recolor for the Synty <c>CustomCharacters_URP</c> shader — the per-mask-zone color slots + metallic/
    /// smoothness. Applied to a piece's material(s) at runtime (no texture baking), so a tier is just one of these.
    /// Per-zone EMISSION is NOT here (the shader has only a global <c>_Emission</c> float) — emission zones are a
    /// custom feature added later. Authored in the tier editor.
    /// </summary>
    [CreateAssetMenu(menuName = "ARPG/Modular/Colorway", fileName = "Colorway")]
    public class ColorwayData : ScriptableObject
    {
        public string colorwayName = "Colorway";

        [Header("Cloth")]
        public Color primary = new Color(0.48f, 0.42f, 0.35f);
        public Color secondary = new Color(0.74f, 0.74f, 0.74f);
        [Header("Leather")]
        public Color leatherPrimary = new Color(0.31f, 0.21f, 0.16f);
        public Color leatherSecondary = new Color(0.37f, 0.33f, 0.28f);
        [Header("Metal")]
        public Color metalPrimary = new Color(0.56f, 0.62f, 0.60f);
        public Color metalSecondary = new Color(0.40f, 0.40f, 0.36f);
        public Color metalDark = new Color(0.17f, 0.22f, 0.27f);
        [Header("Finish")]
        [Range(0, 1)] public float metallic = 0f;
        [Range(0, 1)] public float smoothness = 0.2f;

        static readonly int k_primary = Shader.PropertyToID("_Color_Primary");
        static readonly int k_secondary = Shader.PropertyToID("_Color_Secondary");
        static readonly int k_leatherP = Shader.PropertyToID("_Color_Leather_Primary");
        static readonly int k_leatherS = Shader.PropertyToID("_Color_Leather_Secondary");
        static readonly int k_metalP = Shader.PropertyToID("_Color_Metal_Primary");
        static readonly int k_metalS = Shader.PropertyToID("_Color_Metal_Secondary");
        static readonly int k_metalD = Shader.PropertyToID("_Color_Metal_Dark");
        static readonly int k_metallic = Shader.PropertyToID("_Metallic");
        static readonly int k_smooth = Shader.PropertyToID("_Smoothness");

        /// <summary>Write this colorway's values into a MaterialPropertyBlock (skin/hair left untouched).</summary>
        public void Write(MaterialPropertyBlock mpb)
        {
            mpb.SetColor(k_primary, primary);
            mpb.SetColor(k_secondary, secondary);
            mpb.SetColor(k_leatherP, leatherPrimary);
            mpb.SetColor(k_leatherS, leatherSecondary);
            mpb.SetColor(k_metalP, metalPrimary);
            mpb.SetColor(k_metalS, metalSecondary);
            mpb.SetColor(k_metalD, metalDark);
            mpb.SetFloat(k_metallic, metallic);
            mpb.SetFloat(k_smooth, smoothness);
        }

        /// <summary>Read the current values off a material into this colorway (for authoring/capture).</summary>
        public void CaptureFrom(Material m)
        {
            if (m == null) return;
            primary = m.GetColor(k_primary); secondary = m.GetColor(k_secondary);
            leatherPrimary = m.GetColor(k_leatherP); leatherSecondary = m.GetColor(k_leatherS);
            metalPrimary = m.GetColor(k_metalP); metalSecondary = m.GetColor(k_metalS); metalDark = m.GetColor(k_metalD);
            metallic = m.GetFloat(k_metallic); smoothness = m.GetFloat(k_smooth);
        }
    }
}
