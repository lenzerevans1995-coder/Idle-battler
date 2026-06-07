using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Renders a square head-and-shoulders headshot of a unit into a transparent Sprite, for use as a roster
/// face icon. Isolates the unit onto a dedicated layer so only it appears, frames the upper body from the
/// front, and restores everything afterward. Call while the unit is active (before benching it).
/// </summary>
public static class UnitPortrait
{
    const int PortraitLayer = 31;

    public static Sprite Render(Transform unit, int res = 256)
    {
        var rends = unit.GetComponentsInChildren<Renderer>();
        if (rends.Length == 0) return null;

        Bounds b = rends[0].bounds;
        foreach (var r in rends) b.Encapsulate(r.bounds);

        var savedLayer = new Dictionary<GameObject, int>();
        foreach (var r in rends)
        {
            if (!savedLayer.ContainsKey(r.gameObject)) savedLayer[r.gameObject] = r.gameObject.layer;
            r.gameObject.layer = PortraitLayer;
        }

        var rt = new RenderTexture(res, res, 16, RenderTextureFormat.ARGB32);
        var camGO = new GameObject("PortraitCam");
        var cam = camGO.AddComponent<Camera>();
        cam.cullingMask = 1 << PortraitLayer;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
        cam.orthographic = true;
        cam.nearClipPlane = 0.01f;
        cam.targetTexture = rt;

        Vector3 fwd = unit.forward.sqrMagnitude > 0.01f ? unit.forward : Vector3.forward;
        float headY = Mathf.Lerp(b.center.y, b.max.y, 0.5f);   // head/shoulders height
        Vector3 target = new Vector3(b.center.x, headY, b.center.z);
        cam.orthographicSize = Mathf.Max(0.2f, b.size.y * 0.22f);
        camGO.transform.position = target + fwd * 3f + Vector3.up * 0.05f;
        camGO.transform.LookAt(target, Vector3.up);

        cam.Render();

        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        var tex = new Texture2D(res, res, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, res, res), 0, 0);
        tex.Apply();
        RenderTexture.active = prev;

        foreach (var kv in savedLayer) kv.Key.layer = kv.Value;
        cam.targetTexture = null;
        Object.Destroy(camGO);
        rt.Release();
        Object.Destroy(rt);

        return Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), 100f);
    }
}
