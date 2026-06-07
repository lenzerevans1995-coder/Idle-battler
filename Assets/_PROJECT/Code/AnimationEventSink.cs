using UnityEngine;

/// <summary>
/// No-op receiver for the animation events baked into the ExplosiveLLC RPG-Character / Sorceress / Archer
/// clips (Hit, Shoot, FootR, etc.). Those events target ExplosiveLLC's own controller; on Emerald-driven
/// agents (which handle damage themselves) they have no receiver and spam the console. Put this on the
/// GameObject that has the Animator to silence them. (Hook real logic here later if you want.)
/// </summary>
public class AnimationEventSink : MonoBehaviour
{
    public void Hit() { }
    public void Hit(string s) { }
    public void Shoot() { }
    public void Shoot(string s) { }
    public void FootR() { }
    public void FootL() { }
    public void Footstep() { }
    public void Land() { }
    public void WeaponSwitch() { }
    public void Cast() { }
    public void Release() { }
    // generic single-arg catch-alls Unity may route to:
    public void OnAnimationEvent() { }
    public void AnimationEvent() { }
}
