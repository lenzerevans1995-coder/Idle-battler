# PROJECT_STATE — My project (2)

Master index / source of truth. Keep this current in the same change as the code/scene edit it describes.
Roadmap is in `PLAN.md` (same folder). Always-loaded summary is the root `CLAUDE.md`.

_Last updated: 2026-06-07._

## 1. What this is
An **idle gacha RPG** for **mobile portrait (9:16, 1080×1920)**. Static battle scenes resolve as **5v5
auto-combat** via **Emerald AI 2025**. Pre-battle the player drags their team from a roster bar onto a free
placement zone, then presses START BATTLE. Unity **2022.3.62f3**, **URP**, legacy Input Manager.

## 2. Owned content layout (`Assets/_PROJECT/`)
- `Code/` — battle + roster systems (below) and `Code/Modular/` (the ported character creator).
- `Data/Modular/` — Synty modular catalogs (`Catalog`/`Catalog 1..18`) + `Colorways` (creator data).
- `Prefabs/` — `Battle/`, `Characters/`.
- `Scenes/` — `BattleDemo.unity` (the playable 5v5), `CharacterPreview.unity` (creator preview).
- `Shaders/`, `Documentation/` (this folder).

## 3. Battle scene — `Scenes/BattleDemo.unity`
Working copy of Emerald's melee demo, re-themed for the gacha board.

**Teams (2026-06-07): each side = 1 Sorceress + 2 Hand Fighters + 2 Sword Fighters.**
- **Player (faction 0)** — 2 × "Firsts" (Hand) + 2 × "Sword" + **`Player_Sorceress`**. Start **benched**
  (inactive) and placed by the roster bar at runtime. `Player_Sorceress` is an edit-mode `Object.Instantiate`
  clone of `Enemy_Sorceress` (functionally = Ctrl+D; **unverified in Play** — confirm it detects/casts).
- **Enemy (faction 1)** — 2 × "Firsts" (Hand) + 2 × "Sword" + **`Enemy_Sorceress`**, positioned on z>0 facing −Z.
- **Archetypes** (see §3c) are set by NAME ("Firsts"=Hand, "Sword"=Sword, "Sorceress"=Artillery), independent of faction.
- Faction reassignment + the extra-agent deletion were done by script (faction = `EmeraldDetection.CurrentFaction`,
  safe field-config). Edit-mode `Object.Instantiate` of an agent works; the play-mode/`DuplicateAI` path is what
  corrupted agents before.

**Battlefield framing** (widened 2026-06-07):
- Camera: Main Camera, **orthographic**, `BattleCameraFit` **fitWidth = 13** ⇒ visible ground ≈ x[−6.5,6.5].
  Orthographic ⇒ units are the same screen size at any depth.
- Player placement zone (`StartingZone`): center (0,0,−7), size **11×10**.
- `ArenaBoundsClamp` (on BattleSystem) hard-clamps every agent each LateUpdate to **x ±6, z ±16.5** so root-motion
  moves (e.g. dodge) can't leap a unit off-screen; it also exposes Min/Max/CenterX statics used by the
  direction-aware dodge (see §3c).
- **Pathfinding = A\* Pathfinding Pro 5.4.6** (NOT Unity NavMesh). See "A\* integration" below. The old
  `NavMeshSurface` on `Arena` is now inert; the A\* `GridGraph` is built at runtime by `AStarGraphBootstrap`
  on the `A*Pathfinder` object (`worldSize` 12×34 ⇒ x[−6,6] z[−17,17], all walkable on the flat board).
- `Arena` = 40×50 ground plane (layer Default — the grid's ground/height mask).

**`BattleSystem`** GameObject hosts: `BattleManager`, `StartingZone`, `BattleRoster`.
**`A*Pathfinder`** GameObject hosts: `AstarPath` + `AStarGraphBootstrap`.

## 3b. A\* Pathfinding integration (Emerald ↔ A\*)  ✅ working
Emerald 2025 is patched to drive **A\* Pathfinding Pro** instead of Unity NavMesh, using the proven
Goodgulf approach (github.com/Goodgulf281/Emerald2024-Integration). **Combat + pathing confirmed working.**
- **`ASTAR` scripting define** gates everything. With it OFF, Emerald is byte-for-byte stock (NavMesh); with
  it ON, A\* is used. This is the clean revert: remove `ASTAR` from Player Settings ▸ Scripting Define Symbols.
- **`NavMeshAgentImposter`** (`_PROJECT/Code/Integration/`, namespace `Pathfinding`, derives `AIPath`) is a
  NavMeshAgent stand-in exposing `stoppingDistance`→`endReachedDistance`, `speed`→`maxSpeed`, `SetDestination`,
  `Warp`, `ResetPath`, `isOnNavMesh`, off-mesh-link stubs, etc. Emerald's `m_NavMeshAgent` field is retyped to
  it under `#if ASTAR`.
- **Patched core files** (all `#if ASTAR`-guarded): `EmeraldSystem.cs`, `EmeraldMovement.cs`,
  `EmeraldDebugger.cs` (the 1.3.0 Goodgulf diffs applied cleanly to 2025), plus 2025-only fixes:
  `RandomMovementAction.HasArrived` signature and the imposter's off-mesh stubs for `EmeraldNavmeshLink`.
  RootMotion agents: `MoveAIRootMotion` manually drives `MovementUpdate`/`FinalizeMovement` (root motion = X/Z,
  agent = Y) with `simulateMovement/canMove = false`.
- **`AStarGraphBootstrap`** builds + scans the `GridGraph` at runtime (Awake, exec −5000) — scripted editor
  graph creation doesn't serialize, so always (re)build at load. Ground = Default layer.
- Agents get `NavMeshAgentImposter` (+`Seeker`) auto-added at runtime by the patched `EmeraldSystem.Awake` /
  `SetupNavMeshAgent`, which also disables the stock `NavMeshAgent`.
- **DON'T revive** the old `EmeraldMover` hand-rolled fork (deleted) — it broke combat.
- Harmless leftover: A\* 5.4 deprecation warning in vendor `FPSShooterTut` only (ours are cleaned).

## 3c. Unit archetypes (2026-06-07)
Configured by **field-config on existing agents** (resolved by name), not new scripts on Emerald:
- **Sorceress (Artillery)** — `Enemy_Sorceress` / `Player_Sorceress`. Aggressive, `TooCloseDistance=5`,
  `AttackDistance=9`. Casts lightning (`SpellStrikeVFX`) from range. **`ArtilleryKite`** (`Code/ArtilleryKite.cs`,
  on each sorceress) retreats her in LateUpdate toward open space when an enemy is within `retreatRange` (5),
  overriding Emerald's destination (`EmeraldMovement.SetDestination`) — moving interrupts her cast. This replaces
  Emerald's built-in backup, which never fires for a perma-caster (blocked by `CanBackup()` during attacks).
- **Hand Fighter (Rushdown)** — "Firsts" agents. Aggressive, `AttackDistance=1.3`, `RunSpeed=7`, fast tempo.
- **Sword Fighter (Juggernaut)** — "Sword" agents. Aggressive, `AttackDistance=3.3` (reach), `RunSpeed=3`, slow tempo.
- **Tempo** = `ArchetypeAnimatorSpeed` (`Code/ArchetypeAnimatorSpeed.cs`) sets `Animator.speed` at runtime
  (Hand 1.35×, Sword 0.82×, Sorc 1.0×) — RootMotion speed comes from the animations, so this is the speed lever.
- Emerald 2025 behavior presets are only **Passive / Coward / Aggressive** (no "Cautious"/"Guarding").
- TODO (need AttackData/anim assets): Sword **knockback/stun** on hit; confirm Sorceress per-attack distance.

## 4. Battle / roster systems (`Code/`)
- **`BattleManager`** — state machine `Placement → Battle → Done` (singleton `Instance`). On Start (after 1
  frame, so Emerald inits) it **freezes** every active `EmeraldSystem` (`.enabled = false`) so nothing moves
  during placement. `StartBattle()` re-collects active agents (to include now-placed players), enables them,
  and calls `BattleRoster.OnBattleStart()`.
- **`StartingZone`** — the free-placement rectangle (center/size in XZ). `Contains`/`Clamp`, plus a translucent
  ground quad (Sprites/Default). Free placement — **no grid snapping** (the old grid was rejected).
- **`UnitPortrait`** (static) — renders a square head/shoulders **headshot Sprite** of a unit via a temp
  orthographic camera + layer-31 isolation + transparent RT. Call while the unit is active (before benching).
- **`BattleRoster`** — builds a `ScreenSpaceOverlay` Canvas + EventSystem, collects player-faction units,
  renders their portraits, **benches them**, and builds a bottom **bar of Modular-UI-Kit Avatar cards** + a
  styled **START BATTLE** button (Button-Primary). `Place(entry, worldPos)` reveals + positions the unit (faces
  +Z) and keeps it frozen until battle start; placed cards dim.
  - **CRITICAL — bench = freeze + hide, NOT `SetActive(false)`.** Deactivating a faction-0 unit before Emerald
    finishes initializing permanently broke its combat (units "barely attacked"). Bench now = `EmeraldSystem.enabled
    = false` (freeze) + all `Renderer.enabled = false` (hide); the unit stays active+initialized (same model the
    enemies use). `OnBattleStart()` deactivates only **unplaced** units (safe — long initialized) before
    `StartBattle` enables enemies + placed players.
- **`UnitDragCard`** — uGUI drag handler on each Avatar card: drag → ghost icon follows pointer → release over
  the zone → `BattleRoster.Place`. Release elsewhere cancels.
- **`AnimationEventSink`** — no-op receiver for ExplosiveLLC clip events (Hit/Shoot/foot/etc.) on Emerald
  agents that otherwise spam "no receiver".
- **`BattleCameraFit`** — keeps a fixed world **width** visible on any aspect (orthographic: size =
  (fitWidth/2)/aspect; perspective: recompute vertical FOV).
- **`FormationPlacer`** — **DEPRECATED** grid-snap placer (component removed from the scene; file kept for
  reference; superseded by the roster/drag flow).

## 5. The skill-VFX strike pattern (how casters deal damage + show VFX)
Emerald's projectile/ability chain was unreliable for at-target spells, so casters use **`SpellStrikeVFX`** on
the Animator GameObject. On the clip's **"Hit" animation event** it: finds the target (Emerald current target,
else nearest opposing-faction `EmeraldHealth`), **spawns a self-contained VFX at the target**, and **deals
damage** via reflection (`EmeraldHealth.Damage(int,Transform,int,bool)`). It also absorbs the other clip events.

**Sorceress (done):** `vfx` = GabrielAguiar `Prefabs/LightningV2/vfxgraph_LightningStrike01_blue.prefab` (a
self-contained sky-strike — bolt descends to the impact point; place it AT the target). Tuning:
`scale = 4` (uniform — grows bolt height so the start clears the screen; impact-only size props are auto-divided
back down to keep the ground footprint normal), `yOffset = 0`, `damage = 25`, `debug = false`.
- Pitfall: the `Lightning01_Hit` prefab is **impact-only** (no descending bolt) — faking a sky bolt with its
  `GroundImpactDistance` tears the parts apart. Use the **LightningStrikeV2** strike prefabs instead.
- Pitfall: `LightningOffsetY` is a tiny negative curve (a wobble, not bolt length); the bolt's reach is the
  **mesh length**, scaled by the transform. Non-uniform Y scale makes the bolt invisibly thin — use uniform.

## 6. Modular character creator (`Code/Modular`, `Data/Modular`, `CharacterPreview.unity`)
Framework-free Synty modular body system ported from ARPG_V2 (used to author characters; equipment swapping is
data-driven, not an action-RPG inventory). Key scripts: `SyntyModularBody`, `SyntyModularWardrobe`,
`SyntyCharacterPreset`, `SyntyArmorSet`, `ColorwayData`, `TierLadder`, `EquipSlot`, `AccessoryGripData`,
`SyntyModularDefs`. Catalogs/colorways under `Data/Modular/`.

## 7. Vendor packs in use
Emerald AI 2025 (combat), ExplosiveLLC RPG-Character + Sorceress/Archer/Warrior anim packs,
GabrielAguiarProductions (VFX graphs), **ModularGameUIKit** (`Avatar`, `Button-Primary`, containers — battle UI),
Synty PolygonNature + FantasyHeroCharacters, Grass Painter (MysticForge), GSpawn.

## 8. Gotchas (also in CLAUDE.md)
- No reimporting Emerald (reverts the `linearVelocity` patches). No scripted agent creation/duplication.
- Legacy Input Manager only (`UnityEngine.Input`).
- `execute_code` is CodeDom C# 6: no local functions, fully-qualify, reflection-by-type-name for Emerald.
- Check `Application.isPlaying == false` before saving/structural edits; in-play edits don't persist.
- Deleted ExplosiveLLC `SetupInputLayers.cs` (a popup-spawning AssetPostprocessor).

## Open risks to verify in Play
- Emerald **freeze** via `EmeraldSystem.enabled=false` (placement) and clean re-enable at battle start.
- **Portrait** framing (head/shoulders, not empty/upside-down) across the unit models.
- Placed units **path to enemies** after START (navmesh coverage of the enlarged field).
