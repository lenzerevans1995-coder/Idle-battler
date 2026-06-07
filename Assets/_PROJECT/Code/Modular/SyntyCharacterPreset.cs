using System.Collections.Generic;
using UnityEngine;

namespace ARPG_V2.Modular
{
    /// <summary>
    /// A base character build (used by character creation): skin material + the base mesh for each body region.
    /// Applied over the modular body's authored defaults to define "who the character is" before gear.
    /// (Own file so Unity can create a MonoScript link for the ScriptableObject — assets break after a domain
    /// reload if the SO class shares a file not named after it.)
    /// </summary>
    [CreateAssetMenu(menuName = "ARPG/Modular/Character Preset", fileName = "CharacterPreset")]
    public class SyntyCharacterPreset : ScriptableObject
    {
        public Material skinMaterial;
        public List<RegionMesh> baseParts = new List<RegionMesh>();
    }
}
