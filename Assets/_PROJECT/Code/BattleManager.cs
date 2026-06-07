using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Drives the pre-battle flow. The fight opens in <see cref="State.Placement"/> with every Emerald agent
/// frozen (its EmeraldSystem disabled) so the player can arrange their formation; pressing START BATTLE
/// re-enables the agents and combat runs. Engine-agnostic: it finds Emerald components by type name via
/// reflection so it has no hard dependency on the Emerald assembly.
/// </summary>
public class BattleManager : MonoBehaviour
{
    public enum State { Placement, Battle, Done }
    public State state = State.Placement;

    public static BattleManager Instance;

    readonly List<Behaviour> _agents = new List<Behaviour>(); // EmeraldSystem components

    void Awake() { Instance = this; }

    IEnumerator Start()
    {
        // Let Emerald run its own init for a frame, then freeze everyone for the placement phase.
        yield return null;
        CollectAgents();
        SetAgentsEnabled(false);
        state = State.Placement;
    }

    void CollectAgents()
    {
        _agents.Clear();
        foreach (var mb in FindObjectsOfType<MonoBehaviour>())
            if (mb != null && mb.GetType().Name == "EmeraldSystem")
                _agents.Add((Behaviour)mb);
    }

    void SetAgentsEnabled(bool on)
    {
        foreach (var a in _agents)
            if (a != null) a.enabled = on;
    }

    public void StartBattle()
    {
        if (state != State.Placement) return;
        if (BattleRoster.Instance != null) BattleRoster.Instance.OnBattleStart(); // deactivate unplaced FIRST
        CollectAgents();           // now collects enemies + PLACED players only (unplaced are inactive)
        SetAgentsEnabled(true);
        state = State.Battle;
    }
}
