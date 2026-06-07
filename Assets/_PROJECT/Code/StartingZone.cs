using UnityEngine;

/// <summary>
/// The player's free-placement starting area: a rectangle (X by Z) on their half of the arena.
/// Units dragged from the roster bar may be dropped anywhere inside it (no grid snapping).
/// Draws a translucent ground quad while in the placement phase.
/// </summary>
public class StartingZone : MonoBehaviour
{
    public Vector3 center = new Vector3(0f, 0f, -3f);
    [Tooltip("X = width, Y = depth along Z.")]
    public Vector2 size = new Vector2(8f, 4f);
    public float groundY = 0.08f;
    public Color fill = new Color(0.2f, 0.7f, 1f, 0.16f);

    public static StartingZone Instance;
    Transform _quad;

    void Awake() { Instance = this; }
    void Start() { BuildVisual(); }

    void BuildVisual()
    {
        var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
        q.name = "ZoneFill";
        var col = q.GetComponent<Collider>(); if (col != null) Destroy(col);
        q.transform.SetParent(transform, false);
        q.transform.position = new Vector3(center.x, groundY + 0.01f, center.z);
        q.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        q.transform.localScale = new Vector3(size.x, size.y, 1f);
        q.GetComponent<MeshRenderer>().sharedMaterial = new Material(Shader.Find("Sprites/Default")) { color = fill };
        _quad = q.transform;
    }

    public bool Contains(Vector3 p)
    {
        return Mathf.Abs(p.x - center.x) <= size.x * 0.5f
            && Mathf.Abs(p.z - center.z) <= size.y * 0.5f;
    }

    public Vector3 Clamp(Vector3 p)
    {
        p.x = Mathf.Clamp(p.x, center.x - size.x * 0.5f, center.x + size.x * 0.5f);
        p.z = Mathf.Clamp(p.z, center.z - size.y * 0.5f, center.z + size.y * 0.5f);
        p.y = groundY;
        return p;
    }

    public void SetVisible(bool on) { if (_quad != null) _quad.gameObject.SetActive(on); }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.7f);
        Gizmos.DrawWireCube(new Vector3(center.x, groundY, center.z), new Vector3(size.x, 0.05f, size.y));
    }
}
