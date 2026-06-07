using UnityEngine;
using Pathfinding;

/// <summary>
/// Builds + scans the A* Recast graph at runtime so RichAI always has a graph to follow. Scripted graph
/// creation in the editor doesn't reliably serialize into the AstarPath component, so we (re)build it on
/// load instead — which also makes per-scene/dynamic-obstacle setups trivial. Runs in Awake before any
/// agent's first path search (which happens in Update).
/// </summary>
[DefaultExecutionOrder(-5000)]
public class AStarGraphBootstrap : MonoBehaviour
{
    [Tooltip("Recast graph bounds (covers the playable arena).")]
    public Vector3 boundsCenter = new Vector3(0f, 0.5f, 0f);
    public Vector3 boundsSize = new Vector3(18f, 6f, 32f);
    public float cellSize = 0.3f;
    public float characterRadius = 0.4f;
    public float walkableHeight = 2f;
    public float walkableClimb = 0.5f;
    public float maxSlope = 45f;
    public bool logResult = true;

    void Awake()
    {
        if (!EmeraldMover.UseAStarGlobally) return;

        var astar = AstarPath.active;
        if (astar == null) astar = GetComponent<AstarPath>();
        if (astar == null)
        {
            var go = GameObject.Find("A*Pathfinder");
            if (go == null) go = new GameObject("A*Pathfinder");
            astar = go.GetComponent<AstarPath>();
            if (astar == null) astar = go.AddComponent<AstarPath>();
        }

        astar.scanOnStartup = false; // we scan explicitly below

        RecastGraph g = null;
        if (astar.data.graphs != null)
            foreach (var gr in astar.data.graphs) if (gr is RecastGraph rg) { g = rg; break; }
        if (g == null) g = astar.data.AddGraph(typeof(RecastGraph)) as RecastGraph;

        g.forcedBoundsCenter = boundsCenter;
        g.forcedBoundsSize = boundsSize;
        g.cellSize = cellSize;
        g.characterRadius = characterRadius;
        g.walkableHeight = walkableHeight;
        g.walkableClimb = walkableClimb;
        g.maxSlope = maxSlope;
        g.rasterizeColliders = true;
        g.rasterizeMeshes = true;
        g.rasterizeTerrain = true;

        astar.Scan();

        if (logResult)
        {
            long nodes = 0;
            g.GetNodes(n => nodes++);
            Debug.Log("[AStarGraphBootstrap] Recast graph scanned at runtime. nodes=" + nodes);
        }
    }
}
