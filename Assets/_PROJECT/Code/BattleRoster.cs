using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Pre-battle roster + free placement. Collects the player-faction units, renders a headshot for each,
/// benches them (inactive), and builds a bottom bar of Modular-UI-Kit Avatar cards. Dragging a card onto
/// the <see cref="StartingZone"/> activates and places that unit at the drop point. A styled START BATTLE
/// button hands off to <see cref="BattleManager"/>.
/// </summary>
public class BattleRoster : MonoBehaviour
{
    [Header("Refs (assign UI Kit prefabs)")]
    public GameObject avatarPrefab;       // ModularGameUIKit Avatar.prefab
    public GameObject startButtonPrefab;  // ModularGameUIKit Button-Primary.prefab

    [Header("Config")]
    public int playerFaction = 0;
    public float cardSize = 150f;
    public float barHeight = 210f;
    public Vector2 referenceResolution = new Vector2(1080f, 1920f);

    public static BattleRoster Instance;

    public class Entry { public Transform unit; public GameObject card; public Sprite portrait; public bool placed; }
    public readonly List<Entry> entries = new List<Entry>();

    Canvas _canvas;
    RectTransform _bar;
    GameObject _startBtn;

    void Awake() { Instance = this; }

    void Start()
    {
        EnsureEventSystem();
        BuildCanvas();
        CollectBenchAndPortrait();
        BuildBar();
        BuildStartButton();
    }

    // ---------- setup ----------
    void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    void BuildCanvas()
    {
        var go = new GameObject("BattleUI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        _canvas = go.GetComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 100;
        var sc = go.GetComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = referenceResolution;
        sc.matchWidthOrHeight = 0.5f;
    }

    void CollectBenchAndPortrait()
    {
        foreach (var mb in FindObjectsOfType<MonoBehaviour>())
        {
            if (mb == null || mb.GetType().Name != "EmeraldSystem") continue;
            if (FactionOf(mb.transform) != playerFaction) continue;
            entries.Add(new Entry { unit = mb.transform });
        }
        entries.Sort((a, b) => a.unit.position.x.CompareTo(b.unit.position.x));
        foreach (var e in entries) e.portrait = UnitPortrait.Render(e.unit); // while still visible
        // Bench by FREEZE + HIDE (NOT SetActive(false), which deactivates before Emerald finishes init and
        // permanently breaks the unit's combat). Units stay active+initialized, just frozen and invisible.
        foreach (var e in entries) { SetEmerald(e.unit, false); SetVisible(e.unit, false); }
    }

    void BuildBar()
    {
        var barGO = new GameObject("RosterBar", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
        barGO.transform.SetParent(_canvas.transform, false);
        _bar = barGO.GetComponent<RectTransform>();
        _bar.anchorMin = _bar.anchorMax = _bar.pivot = new Vector2(0.5f, 0f);
        _bar.anchoredPosition = new Vector2(0f, 30f);
        _bar.sizeDelta = new Vector2(entries.Count * (cardSize + 20f) + 40f, barHeight);
        barGO.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.35f);

        var hlg = barGO.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 20f;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = hlg.childControlHeight = false;
        hlg.childForceExpandWidth = hlg.childForceExpandHeight = false;
        hlg.padding = new RectOffset(20, 20, 20, 20);

        foreach (var e in entries)
        {
            var card = Instantiate(avatarPrefab, _bar);
            card.name = "Card_" + e.unit.name;
            var crt = card.GetComponent<RectTransform>(); if (crt != null) crt.sizeDelta = new Vector2(cardSize, cardSize);

            var face = card.transform.Find("Image");
            var faceImg = face != null ? face.GetComponent<Image>() : null;
            if (faceImg != null && e.portrait != null) { faceImg.sprite = e.portrait; faceImg.color = Color.white; }

            var toggle = card.transform.Find("Toggle"); if (toggle != null) toggle.gameObject.SetActive(false);
            var tip = card.transform.Find("Tooltip-Image-RollOver"); if (tip != null) tip.gameObject.SetActive(false);

            var d = card.AddComponent<UnitDragCard>();
            d.roster = this; d.entry = e;
            e.card = card;
        }
    }

    void BuildStartButton()
    {
        if (startButtonPrefab == null) return;
        _startBtn = Instantiate(startButtonPrefab, _canvas.transform);
        var rt = _startBtn.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, barHeight + 60f);

        var label = _startBtn.GetComponentInChildren<TMP_Text>(true);
        if (label != null) label.text = "START BATTLE";

        var btn = _startBtn.GetComponent<Button>();
        if (btn != null) btn.onClick.AddListener(() => { if (BattleManager.Instance != null) BattleManager.Instance.StartBattle(); });
    }

    // ---------- placement ----------
    public void Place(Entry e, Vector3 worldPos)
    {
        if (e == null || e.placed) return;
        var zone = StartingZone.Instance;
        var pos = zone != null ? zone.Clamp(worldPos) : worldPos;

        SetVisible(e.unit, true);            // reveal (unit was active all along, just hidden)
        e.unit.position = pos;               // A* unit is frozen; setting transform is fine
        e.unit.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);

        SetEmerald(e.unit, false); // stay frozen until battle starts
        e.placed = true;
        DimCard(e.card);
    }

    void DimCard(GameObject card)
    {
        if (card == null) return;
        var cg = card.GetComponent<CanvasGroup>(); if (cg == null) cg = card.AddComponent<CanvasGroup>();
        cg.alpha = 0.35f; cg.blocksRaycasts = false; cg.interactable = false;
    }

    public void OnBattleStart()
    {
        // Unplaced units sit this battle out — deactivate now (safe; they're long-initialized) so the
        // StartBattle enable-all doesn't bring them into the fight from the bench.
        foreach (var e in entries) if (!e.placed && e.unit != null) e.unit.gameObject.SetActive(false);
        if (_bar != null) _bar.gameObject.SetActive(false);
        if (_startBtn != null) _startBtn.SetActive(false);
        if (StartingZone.Instance != null) StartingZone.Instance.SetVisible(false);
    }

    // ---------- helpers ----------
    void SetEmerald(Transform unit, bool on)
    {
        foreach (var c in unit.GetComponentsInChildren<Component>(true))
            if (c != null && c.GetType().Name == "EmeraldSystem") ((Behaviour)c).enabled = on;
    }

    void SetVisible(Transform unit, bool on)
    {
        foreach (var r in unit.GetComponentsInChildren<Renderer>(true)) r.enabled = on;
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
}
