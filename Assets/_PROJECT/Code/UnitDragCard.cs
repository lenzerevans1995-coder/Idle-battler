using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Attached to each roster Avatar card. Dragging the card spawns a floating ghost icon that follows the
/// pointer; releasing over the <see cref="StartingZone"/> places the card's unit at that world point
/// (free placement, no grid). Releasing elsewhere cancels.
/// </summary>
public class UnitDragCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [System.NonSerialized] public BattleRoster roster;
    [System.NonSerialized] public BattleRoster.Entry entry;

    Canvas _canvas;
    RectTransform _ghost;

    void Awake() { _canvas = GetComponentInParent<Canvas>(); }

    public void OnBeginDrag(PointerEventData e)
    {
        if (entry == null || entry.placed || _canvas == null) return;

        var g = new GameObject("DragGhost", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        g.transform.SetParent(_canvas.transform, false);
        var img = g.GetComponent<Image>();
        var face = transform.Find("Image");
        var src = face != null ? face.GetComponent<Image>() : GetComponent<Image>();
        if (src != null) img.sprite = src.sprite;
        img.preserveAspect = true;
        g.GetComponent<CanvasGroup>().blocksRaycasts = false;
        _ghost = g.GetComponent<RectTransform>();
        _ghost.sizeDelta = new Vector2(120f, 120f);
        _ghost.position = e.position;
    }

    public void OnDrag(PointerEventData e)
    {
        if (_ghost != null) _ghost.position = e.position;
    }

    public void OnEndDrag(PointerEventData e)
    {
        if (_ghost != null) Destroy(_ghost.gameObject);
        if (entry == null || entry.placed) return;

        var cam = Camera.main;
        var zone = StartingZone.Instance;
        if (cam == null || zone == null) return;

        var ray = cam.ScreenPointToRay(e.position);
        var plane = new Plane(Vector3.up, new Vector3(0f, zone.groundY, 0f));
        if (plane.Raycast(ray, out float dist))
        {
            var p = ray.GetPoint(dist);
            if (zone.Contains(p) && roster != null) roster.Place(entry, p);
        }
    }
}
