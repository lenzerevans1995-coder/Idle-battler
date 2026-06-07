using System.Collections.Generic;
using UnityEngine;

namespace ARPG_V2.Modular
{
    /// <summary>One rung of a material's tier ladder: a name, level gate, stat scale, and its colorway.</summary>
    [System.Serializable]
    public class ArmorTier
    {
        public string tierName = "Tier";
        public int requiredLevel = 1;
        [Tooltip("Scales the piece's base defense (and any future stats).")]
        public float statMultiplier = 1f;
        [Tooltip("The recolor for this tier (authored in the Tier Editor).")]
        public ColorwayData colorway;
    }

    /// <summary>
    /// A material's full tier progression (e.g. Metal: Bronze → … → top). The Tier Editor authors these and
    /// generates tiered <see cref="SyntyArmorSet"/> + ItemArmor per style × tier. One ladder per material
    /// (Metal/Leather/Cotton) by convention, but any grouping works — it's just data.
    /// </summary>
    [CreateAssetMenu(menuName = "ARPG/Modular/Tier Ladder", fileName = "TierLadder")]
    public class TierLadder : ScriptableObject
    {
        [Tooltip("Material this ladder is for (Metal / Leather / Cotton).")]
        public string materialName = "Metal";
        [Tooltip("Base defense at multiplier 1; each tier's defense = baseDefense * statMultiplier.")]
        public int baseDefense = 10;
        public List<ArmorTier> tiers = new List<ArmorTier>();
    }
}
