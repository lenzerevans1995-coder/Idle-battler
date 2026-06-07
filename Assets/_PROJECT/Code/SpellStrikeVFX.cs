using UnityEngine;

/// <summary>
/// On the clip's "Hit" animation event: spawn a VFX at the agent's target and deal damage to it.
/// Bypasses Emerald's projectile/ability chain. Robust target finding (Emerald current target, then a
/// nearest-enemy-faction fallback). Put on the same GameObject as the Animator. Also absorbs the other
/// ExplosiveLLC clip events so they don't spam the console.
/// </summary>
public class SpellStrikeVFX : MonoBehaviour
{
    [Tooltip("VFX prefab to spawn at the target. Use a self-contained sky-strike (GA LightningStrikeV2) so all parts stay anchored at the impact point.")]
    public GameObject vfx;
    [Tooltip("Vertical offset added to the target position. Leave 0 for the LightningStrikeV2 prefabs (they descend from above on their own).")]
    public float yOffset = 0f;
    [Tooltip("Uniform scale — grows the whole strike (incl. bolt height) so its start clears the screen. The ground impact is kept ~constant by compensating its size properties.")]
    public float scale = 3f;
    [Tooltip("Auto-destroy the spawned VFX after this many seconds.")]
    public float lifetime = 3f;
    [Tooltip("Damage dealt to the target (0 = VFX only).")]
    public int damage = 25;
    [Tooltip("Log what Hit() resolves (turn off once working).")]
    public bool debug = true;

    Component _em;

    void Awake()
    {
        foreach (var c in GetComponentsInParent<Component>(true))
            if (c != null && c.GetType().Name == "EmeraldSystem") { _em = c; break; }
    }

    // ---- animation event ----
    public void Hit()
    {
        var t = GetTarget();
        Transform tt = t != null ? t : transform;
        Vector3 targetPos = tt.position;
        if (debug) Debug.Log("[SpellStrike] Hit fired. target=" + (t != null ? t.name : "NULL"), this);
        if (vfx != null)
        {
            // Spawn the self-contained strike AT the impact point; the bolt descends from above on its own.
            Vector3 spawnPos = targetPos + Vector3.up * yOffset;
            var fx = Instantiate(vfx, spawnPos, Quaternion.identity);
            // Scale the whole strike up so the bolt's start clears the screen (keeps the bolt's proportions, so it stays
            // visible), then divide the impact-only size properties back down so the ground footprint stays ~normal.
            if (scale > 0f && Mathf.Abs(scale - 1f) > 0.001f)
            {
                fx.transform.localScale = Vector3.one * scale;
                string[] impactSizes = { "FlashBrightSize", "FlashDarkSize", "MarkSize", "ShockwaveSize", "ShockwaveDarkSize", "ImpactParticlesSize", "SparksSize" };
                foreach (var ve2 in fx.GetComponentsInChildren<UnityEngine.VFX.VisualEffect>(true))
                {
                    foreach (var p in impactSizes)
                        if (ve2.HasFloat(p)) ve2.SetFloat(p, ve2.GetFloat(p) / scale);
                    if (ve2.HasVector3("SparksScale")) ve2.SetVector3("SparksScale", ve2.GetVector3("SparksScale") / scale);
                }
            }
            // legacy support for the GA Hit prefab (origin above, bolt draws DOWN by GroundImpactDistance)
            if (yOffset > 0f)
            {
                var ve = fx.GetComponentInChildren<UnityEngine.VFX.VisualEffect>(true);
                if (ve != null && ve.HasFloat("GroundImpactDistance")) ve.SetFloat("GroundImpactDistance", -yOffset);
            }
            if (lifetime > 0f) Destroy(fx, lifetime);
        }
        if (t != null && damage > 0) DealDamage(t);
    }

    Transform GetTarget()
    {
        // 1) Emerald's current combat target
        if (_em != null)
        {
            var ctiF = _em.GetType().GetField("CurrentTargetInfo");
            var cti = ctiF != null ? ctiF.GetValue(_em) : null;
            if (cti != null)
            {
                var tsF = cti.GetType().GetField("TargetSource");
                var ts = tsF != null ? tsF.GetValue(cti) : null;
                if (ts is Component comp && comp != null) return comp.transform;
                if (ts is GameObject go && go != null) return go.transform;
            }
        }
        // 2) fallback: nearest opposing-faction agent
        return NearestEnemy();
    }

    Transform NearestEnemy()
    {
        int myFac = FactionOf(transform);
        Transform best = null; float bd = float.MaxValue;
        foreach (var hb in FindObjectsOfType<MonoBehaviour>())
        {
            if (hb == null || hb.GetType().Name != "EmeraldHealth") continue;
            var go = hb.gameObject;
            if (go == gameObject || transform.IsChildOf(go.transform) || go.transform.IsChildOf(transform)) continue;
            int f = FactionOf(go.transform);
            if (f < 0 || f == myFac) continue;
            float d = (go.transform.position - transform.position).sqrMagnitude;
            if (d < bd) { bd = d; best = go.transform; }
        }
        return best;
    }

    int FactionOf(Transform t)
    {
        foreach (var c in t.GetComponentsInParent<Component>(true))
            if (c != null && c.GetType().Name == "EmeraldDetection")
            {
                var f = c.GetType().GetField("CurrentFaction");
                if (f != null) return (int)f.GetValue(c);
            }
        return -1;
    }

    void DealDamage(Transform t)
    {
        Component health = null;
        foreach (var c in t.GetComponentsInParent<Component>(true))
            if (c != null && c.GetType().Name == "EmeraldHealth") { health = c; break; }
        if (health == null) { if (debug) Debug.Log("[SpellStrike] no EmeraldHealth on target", this); return; }
        var m = health.GetType().GetMethod("Damage", new System.Type[] { typeof(int), typeof(Transform), typeof(int), typeof(bool) });
        if (m != null) m.Invoke(health, new object[] { damage, transform, 0, false });
        if (debug) Debug.Log("[SpellStrike] dealt " + damage + " to " + t.name, this);
    }

    // ---- absorb other ExplosiveLLC clip events ----
    public void Hit(string s) { Hit(); }
    public void Shoot() { }
    public void Shoot(string s) { }
    public void FootR() { }
    public void FootL() { }
    public void Footstep() { }
    public void Land() { }
    public void WeaponSwitch() { }
    public void Cast() { }
    public void Release() { }
}
