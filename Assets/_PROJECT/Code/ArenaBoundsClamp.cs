using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Hard-clamps every Emerald agent's position to the playable area each LateUpdate (after animation/root
/// motion). A* keeps *pathing* inside the grid, but root-motion moves like the dodge sidestep aren't graph-
/// clamped and can leap a unit off-screen; this snaps them back to the boundary so they always stay visible.
/// Put one on the BattleSystem object. Bounds should sit just inside the camera's visible width.
/// </summary>
public class ArenaBoundsClamp : MonoBehaviour
{
    [Tooltip("Center of the playable area (X, Z).")]
    public Vector2 center = Vector2.zero;
    [Tooltip("Max |X| from center (keep <= camera half-width ~4.5).")]
    public float halfWidthX = 4.0f;
    [Tooltip("Max |Z| from center.")]
    public float halfDepthZ = 15.5f;
    public string agentTypeName = "EmeraldSystem";

    // Exposed so behaviors (e.g. the dodge) can steer away from the side walls.
    public static float MinX = -4f, MaxX = 4f, CenterX = 0f;

    readonly List<Transform> _agents = new List<Transform>();
    float _rescanTimer;

    void LateUpdate()
    {
        _rescanTimer -= Time.deltaTime;
        if (_rescanTimer <= 0f) { Rescan(); _rescanTimer = 1f; }

        float minX = center.x - halfWidthX, maxX = center.x + halfWidthX;
        float minZ = center.y - halfDepthZ, maxZ = center.y + halfDepthZ;
        MinX = minX; MaxX = maxX; CenterX = center.x;

        for (int i = _agents.Count - 1; i >= 0; i--)
        {
            var t = _agents[i];
            if (t == null) { _agents.RemoveAt(i); continue; }
            var p = t.position;
            if (p.x < minX || p.x > maxX || p.z < minZ || p.z > maxZ)
            {
                p.x = Mathf.Clamp(p.x, minX, maxX);
                p.z = Mathf.Clamp(p.z, minZ, maxZ);
                t.position = p;
            }
        }
    }

    void Rescan()
    {
        _agents.Clear();
        foreach (var mb in FindObjectsOfType<MonoBehaviour>())
            if (mb != null && mb.GetType().Name == agentTypeName) _agents.Add(mb.transform);
    }
}
