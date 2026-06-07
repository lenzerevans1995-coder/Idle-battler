using System.Collections.Generic;
using UnityEngine;

namespace ARPG_V2.Modular
{
    /// <summary>
    /// Framework-free wardrobe for the multi-part <see cref="SyntyModularBody"/>.
    /// Equipment is driven by the <b>builder</b> path: <see cref="SetSlotPiece"/> puts a catalog
    /// <see cref="SyntyArmorSet"/> in an <see cref="ArmorSlot"/>, and <see cref="RebuildBuilder"/> rebuilds the
    /// whole look (base preset → equipped pieces → colorway/emission). No item/inventory system required.
    /// Also handles rigid bone-attached accessories (crown/hat/cloak/belt) and live colorway/emission recolor.
    /// </summary>
    [RequireComponent(typeof(SyntyModularBody))]
    [AddComponentMenu("ARPG_V2/Synty Modular Wardrobe")]
    public class SyntyModularWardrobe : MonoBehaviour
    {
        [Tooltip("The character's base build (skin + base body parts). Null = use the body's authored defaults.")]
        public SyntyCharacterPreset basePreset;
        [Tooltip("Placement table for rigid bone-attached accessories (crown/hat/mask/cloak/belt).")]
        public AccessoryGripData accessoryGrips;
        [Tooltip("Apply the base preset on Start.")]
        public bool applyPresetOnStart = true;

        SyntyModularBody m_body;
        readonly Dictionary<AccessorySocket, Transform> m_sockets = new Dictionary<AccessorySocket, Transform>();
        readonly Dictionary<AccessorySocket, GameObject> m_accProps = new Dictionary<AccessorySocket, GameObject>();

        void Awake()
        {
            m_body = GetComponent<SyntyModularBody>();
            EnsureSockets();
        }

        void Start()
        {
            if (applyPresetOnStart) RebuildBuilder();
        }

        // ---- rigid accessory props (bone-attached) ----
        void EnsureSockets()
        {
            var root = m_body != null && m_body.bodyRoot != null ? m_body.bodyRoot : transform;
            CreateSocket(AccessorySocket.Head, "Head", "HeadSocket", root);
            CreateSocket(AccessorySocket.Back, "Spine_03", "BackSocket", root);
            CreateSocket(AccessorySocket.Hip, "Hips", "HipSocket", root);
        }

        void CreateSocket(AccessorySocket s, string boneName, string socketName, Transform root)
        {
            Transform bone = null;
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == boneName) { bone = t; break; }
            if (bone == null) return;
            var sock = bone.Find(socketName);   // idempotent: reuse if already made
            if (sock == null)
            {
                sock = new GameObject(socketName).transform;
                sock.SetParent(bone, false);
                sock.localScale = Vector3.one * 100f;   // counter the 0.01 rig bone scale
                sock.localPosition = Vector3.zero; sock.localRotation = Quaternion.identity;
            }
            m_sockets[s] = sock;
        }

        /// <summary>Attach a rigid accessory prop into its socket at the tuned grip.</summary>
        public void SetAccessory(AccessoryGripData.Grip g)
        {
            if (g == null || g.prefab == null || !m_sockets.TryGetValue(g.socket, out var sock) || sock == null) return;
            if (m_accProps.TryGetValue(g.socket, out var old) && old != null) Destroy(old);
            var prop = Instantiate(g.prefab, sock);
            prop.name = g.prefab.name;
            prop.transform.localPosition = g.localPosition;
            prop.transform.localEulerAngles = g.localRotation;
            prop.transform.localScale = Vector3.one * (g.scale <= 0f ? 1f : g.scale);
            m_accProps[g.socket] = prop;
        }

        /// <summary>Attach an accessory by prop name (looked up in <see cref="accessoryGrips"/>).</summary>
        public void SetAccessoryByName(string propName)
        {
            if (accessoryGrips == null) return;
            var g = accessoryGrips.Find(propName);
            if (g != null) SetAccessory(g);
        }

        public void ClearAccessory(AccessorySocket socket)
        {
            if (m_accProps.TryGetValue(socket, out var p) && p != null) Destroy(p);
            m_accProps.Remove(socket);
        }

        // ---- look (colorway recolor + emission zone) via one MaterialPropertyBlock ----
        MaterialPropertyBlock m_mpb;
        ColorwayData m_lastColorway;
        int m_emZone = -1; Color m_emColor = Color.cyan; float m_emIntensity = 0f;

        static readonly int k_emZone = Shader.PropertyToID("_EmissionZone");
        static readonly int k_emColor = Shader.PropertyToID("_EmissionColor");
        static readonly int k_emInt = Shader.PropertyToID("_Emission");

        // Build ONE MPB (colorway colors + emission zone) and push it to every body renderer.
        void RebuildLookMPB()
        {
            if (m_body == null) return;
            if (m_mpb == null) m_mpb = new MaterialPropertyBlock(); else m_mpb.Clear();
            if (m_lastColorway != null) m_lastColorway.Write(m_mpb);
            m_mpb.SetFloat(k_emZone, m_emZone);
            m_mpb.SetColor(k_emColor, m_emColor);
            m_mpb.SetFloat(k_emInt, m_emIntensity);
            var root = m_body.bodyRoot != null ? m_body.bodyRoot : transform;
            foreach (var r in root.GetComponentsInChildren<SkinnedMeshRenderer>(true)) r.SetPropertyBlock(m_mpb);
        }

        /// <summary>Recolor the whole body to a colorway (tier).</summary>
        public void ApplyColorway(ColorwayData cw) { m_lastColorway = cw; RebuildLookMPB(); }

        /// <summary>Set the emission zone/color/intensity live (used by the preview emission picker).</summary>
        public void SetEmission(int zone, Color color, float intensity) { m_emZone = zone; m_emColor = color; m_emIntensity = intensity; RebuildLookMPB(); }

        /// <summary>Character-creation entry point: set the base build and rebuild.</summary>
        public void SetPreset(SyntyCharacterPreset preset) { basePreset = preset; RebuildBuilder(); }

        // ---- Character-builder path (equip catalog pieces by slot) ----
        readonly Dictionary<ArmorSlot, SyntyArmorSet> m_builder = new Dictionary<ArmorSlot, SyntyArmorSet>();

        public SyntyArmorSet GetSlotPiece(ArmorSlot slot) { return m_builder.TryGetValue(slot, out var s) ? s : null; }

        /// <summary>Put a catalog piece in a slot (null clears it) and rebuild the whole modular look.</summary>
        public void SetSlotPiece(ArmorSlot slot, SyntyArmorSet set)
        {
            if (set == null) m_builder.Remove(slot); else m_builder[slot] = set;
            RebuildBuilder();
        }

        public void ClearAllSlots() { m_builder.Clear(); RebuildBuilder(); }

        /// <summary>Rebuild from base + the builder's equipped catalog pieces.</summary>
        public void RebuildBuilder()
        {
            if (m_body == null) m_body = GetComponent<SyntyModularBody>();
            if (m_body == null) return;
            m_body.RestoreAll();
            m_body.ApplyPreset(basePreset);
            // Apply cosmetics first, armor last, so armor wins on shared regions (Helm/Face both = Head region).
            var ordered = new List<KeyValuePair<ArmorSlot, SyntyArmorSet>>(m_builder);
            ordered.Sort((a, b) => ((int)b.Key).CompareTo((int)a.Key));   // descending enum: cosmetics (high) -> armor (low)
            foreach (var kv in ordered)
                if (kv.Value != null)
                    foreach (var rm in kv.Value.regions) m_body.SetRegion(rm.region, rm.mesh, rm.materials);
            // Hides last so they win (e.g. a No_Hair hat hides hair even though hair's mesh was just set).
            foreach (var kv in ordered)
                if (kv.Value != null)
                    foreach (var hr in kv.Value.hideRegions) m_body.HideRegion(hr);
            // gather the look (colorway + emission zone) from equipped sets, then push one MPB
            m_lastColorway = null; m_emIntensity = 0f;
            foreach (var kv in ordered)
                if (kv.Value != null)
                {
                    if (kv.Value.colorway != null && m_lastColorway == null) m_lastColorway = kv.Value.colorway;
                    if (kv.Value.emissionIntensity > 0f) { m_emZone = kv.Value.emissionZone; m_emColor = kv.Value.emissionColor; m_emIntensity = kv.Value.emissionIntensity; }
                }
            RebuildLookMPB();
        }
    }
}
