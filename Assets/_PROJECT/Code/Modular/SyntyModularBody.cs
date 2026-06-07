using System.Collections.Generic;
using UnityEngine;

namespace ARPG_V2.Modular
{
    /// <summary>
    /// Drives the Synty PolygonFantasyHeroCharacters modular body. The all-parts body holds EVERY variant of
    /// every region as its own (correctly-skinned) SkinnedMeshRenderer, with one enabled per body region (the
    /// configured character). So we re-clothe by <b>enabling/disabling the right variant renderer</b> — NOT by
    /// swapping a renderer's mesh (that breaks skinning → invisible parts). Each region tracks all its variant
    /// renderers, the base (configured) one, and which is currently shown.
    /// </summary>
    public class SyntyModularBody : MonoBehaviour
    {
        [Tooltip("Root containing the part renderers (the 'Skin'). Defaults to this GameObject.")]
        public Transform bodyRoot;

        class Region
        {
            public readonly List<SkinnedMeshRenderer> variants = new List<SkinnedMeshRenderer>();
            public SkinnedMeshRenderer baseRenderer;   // enabled at index = the configured character (null for attachments)
            public SkinnedMeshRenderer current;        // currently shown (base, or an armor variant)
            public bool isAttachment;
        }

        readonly Dictionary<BodyRegion, Region> m_regions = new Dictionary<BodyRegion, Region>();
        bool m_indexed;

        void Awake() => Index();

        public void Index()
        {
            if (m_indexed) return;
            var rootT = bodyRoot != null ? bodyRoot : transform;
            foreach (var r in rootT.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (!Resolve(r.name, out var reg, out var attach)) continue;
                if (!m_regions.TryGetValue(reg, out var region)) { region = new Region { isAttachment = attach }; m_regions[reg] = region; }
                region.variants.Add(r);
                // variants are toggled by their GameObject's active state (not renderer.enabled)
                bool shown = r.gameObject.activeInHierarchy;
                if (shown && region.current == null) region.current = r;                       // track an active variant (incl. attachments)
                if (!attach && shown && region.baseRenderer == null) region.baseRenderer = r;   // body regions also get a base
            }
            m_indexed = true;
        }

        public bool Has(BodyRegion reg) { Index(); return m_regions.ContainsKey(reg); }

        /// <summary>Show the variant of <paramref name="reg"/> whose mesh matches <paramref name="mesh"/> (enable it,
        /// disable the currently-shown one). Optional material override on the target.</summary>
        public void SetRegion(BodyRegion reg, Mesh mesh, Material[] mats)
        {
            Index();
            if (mesh == null || !m_regions.TryGetValue(reg, out var region)) return;
            SkinnedMeshRenderer target = null;
            foreach (var v in region.variants)
                if (v.sharedMesh == mesh || (v.sharedMesh != null && v.sharedMesh.name == mesh.name)) { target = v; break; }
            if (target == null) return;   // no renderer for that variant on this body
            if (region.current != null && region.current != target) region.current.gameObject.SetActive(false);
            target.gameObject.SetActive(true);
            target.enabled = true;
            if (mats != null && mats.Length > 0) target.sharedMaterials = mats;
            region.current = target;
        }

        /// <summary>Restore a region to its base (configured) variant; attachments turn off.</summary>
        public void RestoreRegion(BodyRegion reg)
        {
            Index();
            if (!m_regions.TryGetValue(reg, out var region)) return;
            // turn off EVERY variant (catches attachments that were active in the prefab), then re-enable the base.
            foreach (var v in region.variants) if (v != region.baseRenderer) v.gameObject.SetActive(false);
            if (region.baseRenderer != null) { region.baseRenderer.gameObject.SetActive(true); region.current = region.baseRenderer; }
            else region.current = null;   // attachment region: nothing shown
        }

        public void HideRegion(BodyRegion reg)
        {
            Index();
            if (m_regions.TryGetValue(reg, out var region) && region.current != null) { region.current.gameObject.SetActive(false); region.current = null; }
        }

        public void RestoreAll() { Index(); foreach (var kv in m_regions) RestoreRegion(kv.Key); }

        /// <summary>Recolour the currently-shown body renderers (not attachments) with a skin material.</summary>
        public void SetSkin(Material skin)
        {
            Index();
            if (skin == null) return;
            foreach (var kv in m_regions)
            {
                var region = kv.Value;
                if (region.isAttachment || region.current == null) continue;
                var mats = new Material[region.current.sharedMaterials.Length];
                for (int i = 0; i < mats.Length; i++) mats[i] = skin;
                region.current.sharedMaterials = mats;
            }
        }

        /// <summary>Character creation: set the base build. Each chosen variant becomes the region's new base.</summary>
        public void ApplyPreset(SyntyCharacterPreset p)
        {
            if (p == null) return;
            Index();
            foreach (var rm in p.baseParts)
            {
                SetRegion(rm.region, rm.mesh, rm.materials);
                if (m_regions.TryGetValue(rm.region, out var region)) region.baseRenderer = region.current; // chosen part is the new base
            }
            if (p.skinMaterial != null) SetSkin(p.skinMaterial);
        }

        public void ApplyArmor(SyntyArmorSet set)
        {
            if (set == null) return;
            Index();
            foreach (var rm in set.regions) SetRegion(rm.region, rm.mesh, rm.materials);
            foreach (var h in set.hideRegions) HideRegion(h);
        }

        // name -> region. Attachment / side-specific names checked BEFORE generic ones.
        static bool Resolve(string n, out BodyRegion reg, out bool attach)
        {
            reg = BodyRegion.Head; attach = true;
            if (n.Contains("HelmetAttachment")) { reg = BodyRegion.AttachHelmet; return true; }
            if (n.Contains("BackAttachment")) { reg = BodyRegion.AttachBack; return true; }
            if (n.Contains("ShoulderAttachRight")) { reg = BodyRegion.AttachShoulderRight; return true; }
            if (n.Contains("ShoulderAttachLeft")) { reg = BodyRegion.AttachShoulderLeft; return true; }
            if (n.Contains("ElbowAttachRight")) { reg = BodyRegion.AttachElbowRight; return true; }
            if (n.Contains("ElbowAttachLeft")) { reg = BodyRegion.AttachElbowLeft; return true; }
            if (n.Contains("KneeAttachRight")) { reg = BodyRegion.AttachKneeRight; return true; }
            if (n.Contains("KneeAttachLeft")) { reg = BodyRegion.AttachKneeLeft; return true; }
            if (n.Contains("HipsAttachment")) { reg = BodyRegion.AttachHips; return true; }
            attach = false;
            if (n.Contains("ArmUpperRight")) { reg = BodyRegion.ArmUpperRight; return true; }
            if (n.Contains("ArmUpperLeft")) { reg = BodyRegion.ArmUpperLeft; return true; }
            if (n.Contains("ArmLowerRight")) { reg = BodyRegion.ArmLowerRight; return true; }
            if (n.Contains("ArmLowerLeft")) { reg = BodyRegion.ArmLowerLeft; return true; }
            if (n.Contains("HandRight")) { reg = BodyRegion.HandRight; return true; }
            if (n.Contains("HandLeft")) { reg = BodyRegion.HandLeft; return true; }
            if (n.Contains("LegRight")) { reg = BodyRegion.LegRight; return true; }
            if (n.Contains("LegLeft")) { reg = BodyRegion.LegLeft; return true; }
            if (n.Contains("Torso")) { reg = BodyRegion.Torso; return true; }
            if (n.Contains("Hips")) { reg = BodyRegion.Hips; return true; }
            if (n.Contains("HeadCoverings")) { reg = BodyRegion.HeadCovering; return true; }   // before Head
            if (n.Contains("FacialHair")) { reg = BodyRegion.FacialHair; return true; }          // before Hair
            if (n.Contains("Eyebrow")) { reg = BodyRegion.Eyebrows; return true; }
            if (n.Contains("Ear")) { reg = BodyRegion.Ears; return true; }
            if (n.Contains("Hair")) { reg = BodyRegion.Hair; return true; }
            if (n.Contains("Head")) { reg = BodyRegion.Head; return true; }   // Head_No_Elements (helm) + Chr_Head (face) both = Head region
            return false;
        }
    }
}
