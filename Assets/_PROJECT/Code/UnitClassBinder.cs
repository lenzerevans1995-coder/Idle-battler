using UnityEngine;
using System.Reflection;

/// <summary>
/// Holds a <see cref="UnitClass"/> reference and applies it to this Emerald agent — the bridge that makes
/// class assets the single source of truth for a unit. Sets Emerald behavior, attack/too-close distances,
/// move speeds, the animator controller, the animation tempo (<see cref="ArchetypeAnimatorSpeed"/>), and the
/// kiting component (<see cref="ArtilleryKite"/>). Emerald is reached by reflection (no assembly dependency).
/// Outline color is stored for the EPO pass (Phase 4). Use the "Apply Class" context menu in the editor, or
/// enable <see cref="applyOnAwake"/> for a runtime apply.
/// </summary>
[DisallowMultipleComponent]
public class UnitClassBinder : MonoBehaviour
{
    public UnitClass unitClass;
    [Tooltip("Also apply at runtime in Awake (usually applied in the editor instead).")]
    public bool applyOnAwake = false;
    [Tooltip("Swap the Animator's controller to the class's. OFF for Emerald agents that already have an " +
             "Emerald-compatible controller — the class's raw ExplosiveLLC controller lacks Emerald's params.")]
    public bool applyAnimatorController = false;

    void Awake() { if (applyOnAwake) Apply(); }

    [ContextMenu("Apply Class")]
    public void Apply()
    {
        if (unitClass == null) { Debug.LogWarning("[UnitClassBinder] No UnitClass assigned on " + name, this); return; }

        foreach (var c in GetComponents<Component>())
        {
            if (c == null) continue;
            switch (c.GetType().Name)
            {
                case "EmeraldBehaviors":
                    var bf = c.GetType().GetField("CurrentBehaviorType");
                    if (bf != null) bf.SetValue(c, System.Enum.ToObject(bf.FieldType, unitClass.behavior));
                    break;
                case "EmeraldCombat":
                    SetFloat(c, "AttackDistance", unitClass.attackDistance);
                    SetFloat(c, "TooCloseDistance", unitClass.tooCloseDistance);
                    break;
                case "EmeraldMovement":
                    SetFloat(c, "WalkSpeed", unitClass.walkSpeed);
                    SetFloat(c, "RunSpeed", unitClass.runSpeed);
                    break;
            }
        }

        if (applyAnimatorController && unitClass.animatorController != null)
        {
            var anim = GetComponent<Animator>();
            if (anim != null) anim.runtimeAnimatorController = unitClass.animatorController;
        }

        var tempo = GetComponent<ArchetypeAnimatorSpeed>();
        if (tempo == null) tempo = gameObject.AddComponent<ArchetypeAnimatorSpeed>();
        tempo.animatorSpeed = unitClass.animatorTempo;

        var kite = GetComponent<ArtilleryKite>();
        if (unitClass.kites)
        {
            if (kite == null) kite = gameObject.AddComponent<ArtilleryKite>();
            kite.retreatRange = unitClass.kiteRetreatRange;
            kite.enabled = true;
        }
        else if (kite != null) kite.enabled = false;

        // Outline color (unitClass.outlineColor) is applied in the EPO pass (Phase 4 / UnitOutline).
    }

    void SetFloat(Component c, string field, float value)
    {
        var f = c.GetType().GetField(field, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (f != null && f.FieldType == typeof(float)) f.SetValue(c, value);
    }
}
