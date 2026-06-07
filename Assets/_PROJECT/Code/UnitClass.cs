using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Data definition for a unit "class" (archetype): the single source of truth a character or enemy
/// references for its animator, combat tuning (fed to Emerald), weapon type, outline, and abilities.
/// Authored as assets under _PROJECT/Data/Classes/. Applied to an agent by <see cref="UnitClassBinder"/>.
/// </summary>
[CreateAssetMenu(fileName = "UnitClass", menuName = "Idle Battler/Unit Class")]
public class UnitClass : ScriptableObject
{
    public enum Archetype { HandFighter, SwordFighter, Sorceress, Archer, Custom }
    public enum WeaponKind { Unarmed, OneHandMelee, TwoHandMelee, Bow, Staff }

    [Header("Identity")]
    public string className = "New Class";
    public Archetype archetype = Archetype.HandFighter;
    [TextArea] public string description;

    [Header("Visual")]
    public RuntimeAnimatorController animatorController;
    [Tooltip("Animator playback tempo: 1 = normal, >1 fast (rushdown), <1 slow (juggernaut).")]
    public float animatorTempo = 1f;
    public Color outlineColor = Color.white;

    [Header("Movement (Emerald)")]
    public float walkSpeed = 2f;
    public float runSpeed = 5f;

    [Header("Combat (Emerald)")]
    [Tooltip("Emerald behavior: 0 = Passive, 1 = Coward, 2 = Aggressive.")]
    public int behavior = 2;
    public float attackDistance = 2.5f;
    public float tooCloseDistance = 1f;
    public WeaponKind weaponType = WeaponKind.Unarmed;
    [Tooltip("Ranged 'artillery' kiting (e.g. Sorceress): retreat when an enemy breaches retreatRange.")]
    public bool kites = false;
    public float kiteRetreatRange = 4.5f;

    [Header("Abilities (skills + at-target VFX; filled in Phase 3)")]
    public List<AbilitySpec> abilities = new List<AbilitySpec>();

    [System.Serializable]
    public class AbilitySpec
    {
        public string name;
        [Tooltip("Self-contained at-target VFX (GA strike style), spawned via the SpellStrikeVFX pattern.")]
        public GameObject vfxPrefab;
        public int damage = 25;
        public float castRange = 8f;
        public float vfxScale = 1f;
    }
}
