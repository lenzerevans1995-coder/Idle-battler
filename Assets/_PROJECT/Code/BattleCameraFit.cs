using UnityEngine;

/// <summary>
/// Keeps a fixed world-space WIDTH always visible regardless of screen aspect ratio.
/// A perspective camera has a fixed VERTICAL fov, so its horizontal view shrinks on taller
/// (narrower) phones and the battlefield clips off the sides. This recomputes the vertical fov
/// each frame so <see cref="fitWidth"/> is always framed horizontally — works across every device aspect.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class BattleCameraFit : MonoBehaviour
{
    [Tooltip("World point the camera frames (battle center).")]
    public Transform focus;
    [Tooltip("World-space width that must always stay fully visible (the row width + margin).")]
    public float fitWidth = 6f;
    public float minFOV = 15f;
    public float maxFOV = 80f;

    Camera _cam;

    void OnEnable() { _cam = GetComponent<Camera>(); Apply(); }
    void Update() { Apply(); }
    void LateUpdate() { Apply(); }

    void Apply()
    {
        if (_cam == null) _cam = GetComponent<Camera>();
        if (_cam == null) return;

        float aspect = _cam.aspect; // viewport width/height
        if (aspect < 0.01f) return;

        if (_cam.orthographic)
        {
            // orthographicSize = half the VERTICAL height; horizontal half-width = size * aspect.
            // Solve so fitWidth always stays visible horizontally on any aspect:
            _cam.orthographicSize = (fitWidth * 0.5f) / aspect;
            return;
        }

        Vector3 f = focus != null ? focus.position : transform.position + transform.forward * 10f;
        float dist = Vector3.Distance(transform.position, f);
        if (dist < 0.01f) return;

        // horizontal half-angle needed to show fitWidth at this distance...
        float hHalf = Mathf.Atan2(fitWidth * 0.5f, dist);
        // ...converted to the vertical fov for the current aspect: tan(hHalf) = tan(vHalf) * aspect
        float vHalf = Mathf.Atan(Mathf.Tan(hHalf) / aspect);
        _cam.fieldOfView = Mathf.Clamp(vHalf * 2f * Mathf.Rad2Deg, minFOV, maxFOV);
    }
}
