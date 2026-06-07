using System.Collections.Generic;
using UnityEngine;

namespace ARPG_V2.Modular
{
    /// <summary>
    /// Regions of the Synty PolygonFantasyHeroCharacters modular rig. The first 12 are the always-present
    /// body part renderers (one each — swap their mesh to re-clothe / build a character). The Attach* are
    /// optional attachment renderers (helmet/back/shoulders/elbows/hips/knees) that are OFF unless gear adds them.
    /// </summary>
    public enum BodyRegion
    {
        Head, Hair, Torso, ArmUpperLeft, ArmUpperRight, ArmLowerLeft, ArmLowerRight,
        HandLeft, HandRight, Hips, LegLeft, LegRight,
        AttachHelmet, AttachBack, AttachShoulderLeft, AttachShoulderRight,
        AttachElbowLeft, AttachElbowRight, AttachHips, AttachKneeLeft, AttachKneeRight,
        // appended (keep order — region ints are serialized in catalog assets): cosmetics
        Eyebrows, FacialHair, HeadCovering, Ears
    }

    /// <summary>Modular equipment + appearance slots (Synty's part taxonomy; L/R paired into one slot each).
    /// Armor first, cosmetics last. Helm (Head_No_Elements mesh) and Face (Chr_Head mesh) share the Head region —
    /// the wardrobe applies armor after cosmetics so an equipped Helm overrides the Face.</summary>
    public enum ArmorSlot
    {
        // armor
        Helm, HelmCrest, Torso, ShoulderPads, UpperArms, ElbowPads, LowerArms,
        Hands, Hips, HipGuard, Legs, KneePads, Back,
        // appearance / cosmetic
        Face, Hair, Beard, Eyebrows, Ears, Hat
    }

    /// <summary>A mesh (+ optional material override) assigned to one body region.</summary>
    [System.Serializable]
    public struct RegionMesh
    {
        public BodyRegion region;
        public Mesh mesh;
        public Material[] materials;   // optional; empty = keep the region's current materials (skin)
    }
}
