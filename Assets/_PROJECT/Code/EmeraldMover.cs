using UnityEngine;
using UnityEngine.AI;
using Pathfinding;

/// <summary>
/// Adapter that lets Emerald AI 2025 drive either Unity's NavMeshAgent (stock) or A* Pathfinding's RichAI,
/// exposing the exact member surface Emerald reads/writes on its NavMeshAgent so the ~250 call sites in the
/// Emerald codebase compile unchanged after the field's TYPE is switched from NavMeshAgent to EmeraldMover.
///
/// Revert: set <see cref="UseAStarGlobally"/> = false and every agent behaves exactly like stock NavMesh
/// Emerald (the adapter just forwards to a real NavMeshAgent). See _EmeraldFork_Backup/REVERT.md.
///
/// The few NavMesh-only concepts Emerald touches that A* expresses differently (path reachability,
/// NavMesh.SamplePosition, off-mesh links) are handled by the helper methods here + the hand-edited
/// "leaky sites" in EmeraldMovement.
/// </summary>
public class EmeraldMover
{
    /// <summary>Global backend switch. true = A* RichAI, false = Unity NavMeshAgent (instant revert).</summary>
    public static bool UseAStarGlobally = true;

    public readonly bool usingAStar;
    readonly NavMeshAgent nav;   // backend A
    readonly RichAI ai;          // backend B (A* Pro, recommended for Recast/navmesh graphs)
    readonly Seeker seeker;

    EmeraldMover(NavMeshAgent nav) { this.nav = nav; usingAStar = false; }
    EmeraldMover(RichAI ai, Seeker seeker) { this.ai = ai; this.seeker = seeker; usingAStar = true; }

    /// <summary>
    /// Wrap (or create) the right backend on <paramref name="go"/>. In A* mode this ensures Seeker + RichAI
    /// exist and disables any NavMeshAgent so the two don't fight; in NavMesh mode it ensures a NavMeshAgent.
    /// </summary>
    public static EmeraldMover Wrap(GameObject go)
    {
        if (UseAStarGlobally)
        {
            var existingNav = go.GetComponent<NavMeshAgent>();
            if (existingNav != null) existingNav.enabled = false; // stop it competing with A*

            var seeker = go.GetComponent<Seeker>();
            if (seeker == null) seeker = go.AddComponent<Seeker>();
            var ai = go.GetComponent<RichAI>();
            if (ai == null) ai = go.AddComponent<RichAI>();

            // Emerald owns rotation + (in RootMotion mode) position; default to letting RichAI move the body.
            ai.updateRotation = false;
            ai.enableRotation = false;
            return new EmeraldMover(ai, seeker);
        }
        else
        {
            var n = go.GetComponent<NavMeshAgent>();
            if (n == null) n = go.AddComponent<NavMeshAgent>();
            return new EmeraldMover(n);
        }
    }

    /// <summary>True if the backend component still exists (mirrors a null NavMeshAgent check).</summary>
    public bool Exists => usingAStar ? ai != null : nav != null;

    // ---- enable / lifecycle ----
    public bool enabled
    {
        get => usingAStar ? (ai != null && ai.enabled) : (nav != null && nav.enabled);
        set { if (usingAStar) { if (ai != null) ai.enabled = value; } else if (nav != null) nav.enabled = value; }
    }

    // ---- destination / path ----
    public Vector3 destination
    {
        get => usingAStar ? ai.destination : nav.destination;
        set { if (usingAStar) ai.destination = value; else nav.destination = value; }
    }

    public void SetDestination(Vector3 d)
    {
        if (usingAStar) { ai.destination = d; ai.SearchPath(); }
        else nav.SetDestination(d);
    }

    public void ResetPath()
    {
        if (usingAStar) { if (ai.hasPath) ai.SetPath(null); }
        else nav.ResetPath();
    }

    public void Warp(Vector3 pos)
    {
        if (usingAStar) ai.Teleport(pos);
        else nav.Warp(pos);
    }

    public float remainingDistance => usingAStar ? ai.remainingDistance : nav.remainingDistance;
    public bool pathPending => usingAStar ? ai.pathPending : nav.pathPending;
    public bool hasPath => usingAStar ? ai.hasPath : nav.hasPath;
    public Vector3 steeringTarget => usingAStar ? ai.steeringTarget : nav.steeringTarget;

    // A* always clamps the agent to its graph; treat "on graph" as the NavMesh equivalent.
    public bool isOnNavMesh => usingAStar ? (AstarPath.active != null) : nav.isOnNavMesh;

    // ---- speed / motion ----
    public float speed
    {
        get => usingAStar ? ai.maxSpeed : nav.speed;
        set { if (usingAStar) ai.maxSpeed = value; else nav.speed = value; }
    }

    public Vector3 velocity => usingAStar ? ai.velocity : nav.velocity;
    public Vector3 desiredVelocity => usingAStar ? ai.desiredVelocity : nav.desiredVelocity;

    public bool isStopped
    {
        get => usingAStar ? ai.isStopped : nav.isStopped;
        set { if (usingAStar) ai.isStopped = value; else nav.isStopped = value; }
    }

    // Emerald's "stopping distance" maps to RichAI.endReachedDistance.
    public float stoppingDistance
    {
        get => usingAStar ? ai.endReachedDistance : nav.stoppingDistance;
        set { if (usingAStar) ai.endReachedDistance = value; else nav.stoppingDistance = value; }
    }

    public float radius
    {
        get => usingAStar ? ai.radius : nav.radius;
        set { if (usingAStar) ai.radius = value; else nav.radius = value; }
    }

    public float acceleration
    {
        get => usingAStar ? ai.acceleration : nav.acceleration;
        set { if (usingAStar) ai.acceleration = value; else nav.acceleration = value; }
    }

    // ---- flags Emerald sets on the NavMeshAgent (no-ops / closest equivalents on A*) ----
    public bool updateRotation
    {
        get => usingAStar ? ai.updateRotation : nav.updateRotation;
        set { if (usingAStar) { ai.updateRotation = value; ai.enableRotation = value; } else nav.updateRotation = value; }
    }

    public bool updatePosition
    {
        get => usingAStar ? ai.updatePosition : nav.updatePosition;
        set { if (usingAStar) ai.updatePosition = value; else nav.updatePosition = value; }
    }

    public bool updateUpAxis
    {
        get => usingAStar ? false : nav.updateUpAxis;
        set { if (!usingAStar) nav.updateUpAxis = value; } // A* handles its own up axis via the movement plane
    }

    public bool autoBraking
    {
        get => usingAStar ? true : nav.autoBraking;
        set { if (!usingAStar) nav.autoBraking = value; } // RichAI always slows into the end of its path
    }

    public int areaMask
    {
        get => usingAStar ? -1 : nav.areaMask;
        set { if (!usingAStar) nav.areaMask = value; } // A* uses graph/tag masks instead
    }

    // ---- reachability (replaces NavMeshAgent.CalculatePath + NavMeshPathStatus checks) ----
    /// <summary>True if a complete path exists from the agent to <paramref name="dest"/>.</summary>
    public bool CanReach(Vector3 dest)
    {
        if (!usingAStar)
        {
            var p = new NavMeshPath();
            return nav.CalculatePath(dest, p) && p.status == NavMeshPathStatus.PathComplete;
        }
        if (AstarPath.active == null) return false;
        var from = AstarPath.active.GetNearest(ai.position).node;
        var to = AstarPath.active.GetNearest(dest).node;
        if (from == null || to == null) return false;
        return PathUtilities.IsPathPossible(from, to);
    }

    /// <summary>Snap a world point to the nearest reachable position on the graph/navmesh.</summary>
    public static bool SamplePosition(Vector3 src, out Vector3 result, float maxDistance)
    {
        if (UseAStarGlobally)
        {
            if (AstarPath.active == null) { result = src; return false; }
            var nn = AstarPath.active.GetNearest(src);
            result = nn.position;
            return nn.node != null && (result - src).sqrMagnitude <= maxDistance * maxDistance;
        }
        else
        {
            if (NavMesh.SamplePosition(src, out NavMeshHit hit, maxDistance, NavMesh.AllAreas)) { result = hit.position; return true; }
            result = src; return false;
        }
    }

    // ---- off-mesh links (RichAI traverses links itself; stub the NavMeshAgent API on A*) ----
    public bool isOnOffMeshLink => usingAStar ? false : nav.isOnOffMeshLink;
    public bool autoTraverseOffMeshLink
    {
        get => usingAStar ? true : nav.autoTraverseOffMeshLink;
        set { if (!usingAStar) nav.autoTraverseOffMeshLink = value; }
    }
    public OffMeshLinkData currentOffMeshLinkData => usingAStar ? default(OffMeshLinkData) : nav.currentOffMeshLinkData;
    public void CompleteOffMeshLink() { if (!usingAStar) nav.CompleteOffMeshLink(); }

    /// <summary>Escape hatch to the underlying components when a call site needs the concrete type.</summary>
    public NavMeshAgent NavBackend => nav;
    public RichAI AStarBackend => ai;
    public Seeker SeekerBackend => seeker;
}
