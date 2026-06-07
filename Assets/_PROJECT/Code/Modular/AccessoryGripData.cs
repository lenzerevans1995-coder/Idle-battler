using UnityEngine;

namespace ARPG_V2.Modular
{
    /// <summary>
    /// Placement table for RIGID bone-attached accessories (crowns/hats/masks → Head, cloaks/backpacks → Back,
    /// belts/pouches → Hip). Same idea as <see cref="WeaponGripData"/> for weapons: each entry says which socket
    /// an accessory mounts to and its tuned local offset/rotation/scale. Tuned in the CharacterPreview station
    /// (select the spawned prop, W/E in the Scene view, SAVE). Skinned accessories (FantasyHero HipsAttachment,
    /// Synty Cape_Rig capes) are NOT here — they go through the modular body / cape rig.
    /// </summary>
    public enum AccessorySocket { Head, Back, Hip }

    [CreateAssetMenu(menuName = "PLAYER TWO/ARPG Project/Accessory Grips")]
    public class AccessoryGripData : ScriptableObject
    {
        [System.Serializable]
        public class Grip
        {
            public string group;            // e.g. "Crown", "Hat", "Cloak", "Backpack", "Belt"
            public GameObject prefab;        // the rigid accessory prop
            [Tooltip("CATEGORY match: any prop whose name CONTAINS this token uses this grip. Empty = exact prefab only.")]
            public string matchName;
            public AccessorySocket socket;
            public Vector3 localPosition;
            public Vector3 localRotation;
            public float scale = 1f;
        }

        public Grip[] grips;

        /// <summary>Find the grip for a prop by its (clone-stripped) name. Exact prefab match wins, else category.</summary>
        public Grip Find(string prefabName)
        {
            if (grips == null || string.IsNullOrEmpty(prefabName)) return null;
            foreach (var g in grips)
                if (g != null && g.prefab != null && g.prefab.name == prefabName) return g;
            foreach (var g in grips)
                if (g != null && !string.IsNullOrEmpty(g.matchName) && prefabName.Contains(g.matchName)) return g;
            return null;
        }
    }
}
