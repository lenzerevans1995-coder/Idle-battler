using UnityEngine;

/// <summary>
/// Sets the Animator playback speed at runtime to give an archetype its movement + attack tempo. With
/// Emerald RootMotion agents, locomotion speed comes from the animations, so scaling Animator.speed is the
/// simplest per-archetype speed lever (also scales attack cadence — fast jabs vs slow heavy swings).
/// </summary>
public class ArchetypeAnimatorSpeed : MonoBehaviour
{
    [Tooltip("1 = normal. >1 faster (rushdown), <1 slower (juggernaut).")]
    public float animatorSpeed = 1f;

    void Start()
    {
        var a = GetComponent<Animator>();
        if (a != null) a.speed = animatorSpeed;
    }
}
