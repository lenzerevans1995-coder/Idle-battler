using System.Collections.Generic;
using UnityEngine;

namespace ARPG_V2.Modular
{
    /// <summary>
    /// The modular visual for one piece/set of gear: which framework <see cref="Item"/> it represents (optional),
    /// the gear slot, the region meshes it swaps in (body parts + attachments to enable), and any body regions to
    /// hide while worn (e.g. hair under a helmet).
    /// (Own file so Unity creates a proper MonoScript link — when this SO lived in a multi-type file not named
    /// after it, the generated assets had m_Script = 0 and went null after every domain reload.)
    /// </summary>
    [CreateAssetMenu(menuName = "ARPG/Modular/Armor Set", fileName = "ArmorSet")]
    public class SyntyArmorSet : ScriptableObject
    {
        [Tooltip("Which modular equipment slot this piece occupies (used by the character builder / preview panel).")]
        public ArmorSlot armorSlot;
        [Tooltip("Source preset this piece was extracted from (for reference/regeneration).")]
        public string sourcePreset;
        [Tooltip("Body-part meshes to swap in + attachment regions to enable when this set is worn.")]
        public List<RegionMesh> regions = new List<RegionMesh>();
        [Tooltip("Body regions hidden (disabled) while this set is worn, e.g. arms under a full chestpiece.")]
        public List<BodyRegion> hideRegions = new List<BodyRegion>();
        [Tooltip("Tier colorway applied to the body when this piece is worn (set by the tier generator). Null = leave colors as-is.")]
        public ColorwayData colorway;

        [Header("Emission zone (glow spot — buckle/bands/trim)")]
        [Tooltip("Which color region glows: -1 = none, 0 Primary, 1 Secondary, 2 LeatherP, 3 LeatherS, 4 MetalP, 5 MetalS, 6 MetalDark.")]
        public int emissionZone = -1;
        [ColorUsage(true, true)] public Color emissionColor = Color.cyan;
        [Range(0, 8)] public float emissionIntensity = 0f;
    }
}
