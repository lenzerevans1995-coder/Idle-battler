namespace ARPG_V2.Modular
{
    /// <summary>
    /// Gameplay equipment slots for the player — the coarse "what you equip" layer, distinct from the 19 fine
    /// <see cref="ArmorSlot"/> builder regions. Head holds a Helm (skinned, replaces the head) OR Headwear (rigid
    /// hat/hood/crown/mask) — mutually exclusive. See EQUIPMENT_SYSTEM_PLAN.md.
    /// </summary>
    public enum EquipSlot
    {
        Head,       // helm (skinned Head region) OR headwear (rigid prop) — one socket, mutually exclusive
        Body,       // Torso + ShoulderPads + UpperArms (skinned)
        Hands,      // LowerArms + ElbowPads + Hands (skinned)
        Legs,       // Hips + Legs + KneePads (skinned)
        Hip,        // belt + pouch (rigid bone-prop)
        Back,       // cape (skinned Cape_Rig) / cloak / backpack (rigid bone-prop)
        MainHand,   // weapon (held)
        OffHand     // shield / off-hand (held)
        // deferred (non-visual, stats only): Amulet, Ring, Ammo
    }

    /// <summary>How a slot's item is put on the body.</summary>
    public enum EquipMechanism { SkinnedRegion, BoneProp, Held }

    public static class EquipSlots
    {
        static readonly ArmorSlot[] k_none = new ArmorSlot[0];

        /// <summary>The fine <see cref="ArmorSlot"/> regions a SKINNED equip slot swaps (empty for prop/held slots).</summary>
        public static ArmorSlot[] RegionsFor(EquipSlot s)
        {
            switch (s)
            {
                case EquipSlot.Head: return new[] { ArmorSlot.Helm, ArmorSlot.Hat };   // helm (Head region) OR hat (HeadCovering) — whichever is worn
                case EquipSlot.Body: return new[] { ArmorSlot.Torso, ArmorSlot.ShoulderPads, ArmorSlot.UpperArms };
                case EquipSlot.Hands: return new[] { ArmorSlot.LowerArms, ArmorSlot.ElbowPads, ArmorSlot.Hands };
                case EquipSlot.Legs: return new[] { ArmorSlot.Hips, ArmorSlot.Legs, ArmorSlot.KneePads };
                default: return k_none;                                        // Hip/Back = props, MainHand/OffHand = held
            }
        }

        /// <summary>Default mechanism for a slot (Head can be either — the item overrides).</summary>
        public static EquipMechanism MechanismFor(EquipSlot s)
        {
            switch (s)
            {
                case EquipSlot.Body: case EquipSlot.Hands: case EquipSlot.Legs: return EquipMechanism.SkinnedRegion;
                case EquipSlot.Hip: case EquipSlot.Back: return EquipMechanism.BoneProp;
                case EquipSlot.MainHand: case EquipSlot.OffHand: return EquipMechanism.Held;
                default: return EquipMechanism.SkinnedRegion;                  // Head defaults skinned (helm); headwear item flags prop
            }
        }
    }
}
