using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pre-battle formation placement for the player's units. Builds a slot grid on the player's half of the
/// arena, snaps the player-faction Emerald agents into it, draws flat slot markers, and lets the player
/// tap a unit then tap a slot to move it there (occupied slots swap). Active only while
/// <see cref="BattleManager"/> is in the Placement state.
/// </summary>
public class FormationPlacer : MonoBehaviour
{
    [Header("Grid (player half, world space)")]
    public int columns = 3;
    public int rows = 2;
    [Tooltip("Center of the slot grid (player side: negative Z).")]
    public Vector3 center = new Vector3(0f, 0.08f, -3f);
    public float colSpacing = 1.6f;
    public float rowSpacing = 1.8f;
    public int playerFaction = 0;
    [Tooltip("Units face this world direction once placed (toward the enemy = +Z).")]
    public Vector3 facing = Vector3.forward;

    [Header("Markers")]
    public Color slotColor = new Color(0.25f, 0.7f, 1f, 0.30f);
    public Color selectColor = new Color(1f, 0.85f, 0.2f, 0.55f);
    public float slotSize = 1.1f;

    readonly List<Vector3> _slots = new List<Vector3>();
    readonly List<Transform> _markers = new List<Transform>();
    Transform[] _occupant;          // unit currently standing in each slot
    Transform _selected;
    Transform _selectMarker;
    Material _slotMat, _selMat;
    Camera _cam;

    void Start()
    {
        _cam = Camera.main;
        BuildSlots();
        BuildMaterials();
        BuildMarkers();
        SnapUnitsToSlots();
    }

    // ---------- setup ----------
    void BuildSlots()
    {
        _slots.Clear();
        float x0 = -((columns - 1) * colSpacing) * 0.5f;
        float z0 = -((rows - 1) * rowSpacing) * 0.5f;
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < columns; c++)
                _slots.Add(center + new Vector3(x0 + c * colSpacing, 0f, z0 + r * rowSpacing));
        _occupant = new Transform[_slots.Count];
    }

    void BuildMaterials()
    {
        var sh = Shader.Find("Sprites/Default"); // unlit + honors vertex/material color alpha
        _slotMat = new Material(sh) { color = slotColor };
        _selMat = new Material(sh) { color = selectColor };
    }

    void BuildMarkers()
    {
        foreach (var s in _slots)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = "SlotMarker";
            var col = q.GetComponent<Collider>(); if (col != null) Destroy(col);
            q.transform.SetParent(transform, false);
            q.transform.position = s + Vector3.up * 0.01f;
            q.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            q.transform.localScale = Vector3.one * slotSize;
            q.GetComponent<MeshRenderer>().sharedMaterial = _slotMat;
            _markers.Add(q.transform);
        }

        var sel = GameObject.CreatePrimitive(PrimitiveType.Quad);
        sel.name = "SelectMarker";
        var sc = sel.GetComponent<Collider>(); if (sc != null) Destroy(sc);
        sel.transform.SetParent(transform, false);
        sel.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        sel.transform.localScale = Vector3.one * (slotSize * 1.15f);
        sel.GetComponent<MeshRenderer>().sharedMaterial = _selMat;
        _selectMarker = sel.transform;
        _selectMarker.gameObject.SetActive(false);
    }

    void SnapUnitsToSlots()
    {
        var units = new List<Transform>();
        foreach (var mb in FindObjectsOfType<MonoBehaviour>())
        {
            if (mb == null || mb.GetType().Name != "EmeraldSystem") continue;
            if (FactionOf(mb.transform) != playerFaction) continue;
            units.Add(mb.transform);
        }
        for (int i = 0; i < units.Count && i < _slots.Count; i++)
        {
            MoveUnit(units[i], _slots[i]);
            _occupant[i] = units[i];
        }
    }

    // ---------- input ----------
    void Update()
    {
        bool placing = BattleManager.Instance == null || BattleManager.Instance.state == BattleManager.State.Placement;
        if (!placing) { SetMarkersVisible(false); _selected = null; return; }
        SetMarkersVisible(true);

        if (_selected != null && _selectMarker != null)
            _selectMarker.position = _selected.position + Vector3.up * 0.02f;

        Vector2 sp;
        if (!PressedThisFrame(out sp) || _cam == null) return;
        Ray ray = _cam.ScreenPointToRay(sp);

        Transform unit = RaycastPlayerUnit(ray);
        if (unit != null) { Select(unit); return; }

        if (_selected != null && GroundPoint(ray, out Vector3 gp))
            PlaceSelectedAt(NearestSlot(gp));
    }

    bool PressedThisFrame(out Vector2 sp)
    {
        sp = default;
        if (Input.touchCount > 0)
        {
            var t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Began) { sp = t.position; return true; }
            return false;
        }
        if (Input.GetMouseButtonDown(0)) { sp = Input.mousePosition; return true; }
        return false;
    }

    Transform RaycastPlayerUnit(Ray ray)
    {
        var hits = Physics.RaycastAll(ray, 200f);
        float best = float.MaxValue; Transform found = null;
        foreach (var h in hits)
        {
            Transform root = RootWithEmerald(h.collider.transform);
            if (root == null || FactionOf(root) != playerFaction) continue;
            if (h.distance < best) { best = h.distance; found = root; }
        }
        return found;
    }

    void Select(Transform unit)
    {
        _selected = unit;
        if (_selectMarker != null)
        {
            _selectMarker.gameObject.SetActive(true);
            _selectMarker.position = unit.position + Vector3.up * 0.02f;
        }
    }

    void PlaceSelectedAt(int slot)
    {
        if (slot < 0) return;
        int from = System.Array.IndexOf(_occupant, _selected);
        if (from == slot) return;

        Transform other = _occupant[slot];
        _occupant[slot] = _selected;
        MoveUnit(_selected, _slots[slot]);

        if (from >= 0)
        {
            _occupant[from] = other;
            if (other != null) MoveUnit(other, _slots[from]);
        }
    }

    // ---------- helpers ----------
    int NearestSlot(Vector3 p)
    {
        int best = -1; float bd = float.MaxValue;
        for (int i = 0; i < _slots.Count; i++)
        {
            float d = (_slots[i] - p).sqrMagnitude;
            if (d < bd) { bd = d; best = i; }
        }
        return best;
    }

    bool GroundPoint(Ray ray, out Vector3 p)
    {
        var plane = new Plane(Vector3.up, new Vector3(0f, center.y, 0f));
        if (plane.Raycast(ray, out float ent)) { p = ray.GetPoint(ent); return true; }
        p = default; return false;
    }

    void MoveUnit(Transform u, Vector3 pos)
    {
        var agent = u.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null && agent.enabled && agent.isOnNavMesh) agent.Warp(pos);
        else u.position = pos;
        if (facing.sqrMagnitude > 0.001f) u.rotation = Quaternion.LookRotation(facing.normalized, Vector3.up);
    }

    Transform RootWithEmerald(Transform t)
    {
        foreach (var c in t.GetComponentsInParent<Component>(true))
            if (c != null && c.GetType().Name == "EmeraldSystem") return c.transform;
        return null;
    }

    int FactionOf(Transform t)
    {
        foreach (var c in t.GetComponentsInChildren<Component>(true))
            if (c != null && c.GetType().Name == "EmeraldDetection")
            {
                var f = c.GetType().GetField("CurrentFaction");
                if (f != null) return (int)f.GetValue(c);
            }
        return -1;
    }

    void SetMarkersVisible(bool on)
    {
        foreach (var m in _markers) if (m != null && m.gameObject.activeSelf != on) m.gameObject.SetActive(on);
        if (!on && _selectMarker != null) _selectMarker.gameObject.SetActive(false);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.25f, 0.7f, 1f, 0.6f);
        float x0 = -((columns - 1) * colSpacing) * 0.5f;
        float z0 = -((rows - 1) * rowSpacing) * 0.5f;
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < columns; c++)
                Gizmos.DrawWireCube(center + new Vector3(x0 + c * colSpacing, 0f, z0 + r * rowSpacing), new Vector3(slotSize, 0.05f, slotSize));
    }
}
