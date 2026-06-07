using UnityEngine;
using Pathfinding;

/// <summary>
/// Builds + scans an A* Grid graph at runtime so the Goodgulf Emerald↔A* integration (NavMeshAgentImposter,
/// which derives from AIPath) has a graph to follow. Scripted graph creation in the editor doesn't reliably
/// serialize into the AstarPath component, so we (re)build it on load — which also makes per-scene setup and
/// dynamic obstacles easy. Runs in Awake (exec order -5000) before any agent's first path request.
/// Harmless when the ASTAR define is off (agents use stock NavMesh; this grid just goes unused).
/// </summary>
[DefaultExecutionOrder(-5000)]
public class AStarGraphBootstrap : MonoBehaviour
{
    [Tooltip("Grid center (XZ over the arena). Y is where height rays start from + fromHeight.")]
    public Vector3 center = new Vector3(0f, 0f, 0f);
    [Tooltip("World size of the grid (X by Z). Node counts derive from this / nodeSize.")]
    public Vector2 worldSize = new Vector2(20f, 34f);
    public float nodeSize = 0.5f;
    [Tooltip("Layer(s) that form the walkable ground (height raycast target).")]
    public LayerMask groundMask = 1;           // Default
    [Tooltip("Layer(s) that block movement (obstacles). None for a flat board.")]
    public LayerMask obstacleMask = 0;
    public float characterDiameter = 0.8f;
    public bool logResult = true;

    void Awake()
    {
        var astar = AstarPath.active;
        if (astar == null) astar = GetComponent<AstarPath>();
        if (astar == null)
        {
            var go = GameObject.Find("A*Pathfinder") ?? new GameObject("A*Pathfinder");
            astar = go.GetComponent<AstarPath>() ?? go.AddComponent<AstarPath>();
        }
        astar.scanOnStartup = false; // we scan explicitly below

        // Remove any pre-existing graphs (e.g. the old recast graph) so we have exactly one grid graph.
        if (astar.data.graphs != null)
        {
            var existing = new System.Collections.Generic.List<NavGraph>(astar.data.graphs);
            foreach (var g in existing) if (g != null) astar.data.RemoveGraph(g);
        }

        var grid = astar.data.AddGraph(typeof(GridGraph)) as GridGraph;
        grid.center = center;
        grid.SetDimensions(Mathf.RoundToInt(worldSize.x / nodeSize), Mathf.RoundToInt(worldSize.y / nodeSize), nodeSize);
        grid.collision.heightCheck = true;
        grid.collision.heightMask = groundMask;
        grid.collision.fromHeight = 12f;
        grid.collision.collisionCheck = obstacleMask.value != 0;
        grid.collision.mask = obstacleMask;
        grid.collision.diameter = characterDiameter / nodeSize;
        grid.maxSlope = 45f;

        astar.Scan();

        if (logResult)
        {
            long nodes = 0, walkable = 0;
            grid.GetNodes(n => { nodes++; if (n.Walkable) walkable++; });
            Debug.Log("[AStarGraphBootstrap] Grid graph scanned: nodes=" + nodes + " walkable=" + walkable);
        }
    }
}
