using UnityEngine;
using System.Reflection;

/// <summary>
/// Makes a ranged "artillery" caster kite: when an enemy breaches <see cref="retreatRange"/>, it overrides
/// Emerald's destination (in LateUpdate, after Emerald's combat movement) to a point directly away from the
/// nearest enemy, so the unit retreats toward open space. Moving interrupts its cast (Emerald cancels attacks
/// while moving), giving the intended "forced to move = cast interrupted" feel. When no enemy is close it does
/// nothing, so Emerald resumes casting from range. Reliable for perma-casters where Emerald's built-in backup
/// (blocked during attacks) never triggers. Reflection-based so it has no Emerald assembly dependency.
/// </summary>
public class ArtilleryKite : MonoBehaviour
{
    [Tooltip("Retreat when the nearest enemy is closer than this.")]
    public float retreatRange = 4.5f;
    [Tooltip("How far to aim each retreat burst.")]
    public float retreatStep = 4f;
    [Tooltip("After a retreat burst, hold this long (let her cast) before retreating again — prevents perma-flee.")]
    public float castWindow = 1.4f;

    Component _em;
    object _move;
    MethodInfo _setDest;
    int _faction = -1;
    float _tick;
    float _cooldown;

    void Awake()
    {
        foreach (var c in GetComponentsInParent<Component>(true))
            if (c != null && c.GetType().Name == "EmeraldSystem") { _em = c; break; }
        if (_em != null)
        {
            var mc = _em.GetType().GetField("MovementComponent");
            _move = mc != null ? mc.GetValue(_em) : null;
            if (_move != null) _setDest = _move.GetType().GetMethod("SetDestination", new System.Type[] { typeof(Vector3) });
        }
        _faction = FactionOf(transform);
    }

    void LateUpdate()
    {
        if (_em == null || _setDest == null) return;
        _cooldown -= Time.deltaTime;
        _tick -= Time.deltaTime;
        if (_tick > 0f) return;
        _tick = 0.1f;

        if (_cooldown > 0f) return; // in a cast window after a retreat burst — hold + let her cast

        float dist;
        Transform nearest = NearestEnemy(out dist);
        if (nearest == null || dist >= retreatRange) return; // safe: cast from range

        // one retreat burst, then a cast window before the next (avoids perma-flee that never casts)
        Vector3 away = transform.position - nearest.position; away.y = 0f;
        if (away.sqrMagnitude < 0.01f) away = -transform.forward;
        away.Normalize();

        Vector3 target = transform.position + away * retreatStep;
        target.x = Mathf.Clamp(target.x, ArenaBoundsClamp.MinX, ArenaBoundsClamp.MaxX);
        target.z = Mathf.Clamp(target.z, -16f, 16f);
        _setDest.Invoke(_move, new object[] { target });
        _cooldown = castWindow;
    }

    Transform NearestEnemy(out float dist)
    {
        dist = float.MaxValue; Transform best = null;
        foreach (var mb in FindObjectsOfType<MonoBehaviour>())
        {
            if (mb == null || mb.GetType().Name != "EmeraldHealth") continue;
            var go = mb.gameObject;
            if (go == gameObject || transform.IsChildOf(go.transform) || go.transform.IsChildOf(transform)) continue;
            int f = FactionOf(go.transform);
            if (f < 0 || f == _faction) continue;
            float d = Vector3.Distance(go.transform.position, transform.position);
            if (d < dist) { dist = d; best = go.transform; }
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
}
