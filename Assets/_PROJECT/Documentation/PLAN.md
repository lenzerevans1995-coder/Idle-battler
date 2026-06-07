# PLAN — My project (2) (idle gacha RPG)

Roadmap / status. Pair with `PROJECT_STATE.md` (architecture) and root `CLAUDE.md` (rules).
Status keys: ✅ done · 🟡 in progress · ⬜ not started.

_Last updated: 2026-06-07._

## Phase 0 — Combat demo foundation ✅
- ✅ Emerald AI 2025 patched for Unity 2022 (`linearVelocity` guards); 5v5 demo agents cloned in-editor.
- ✅ Mobile portrait framing — `BattleCameraFit` (fit-width, orthographic).
- ✅ NavMesh confined then **enlarged** to the bigger board; units kept on-screen.

## Phase 1.5 — A* Pathfinding integration ✅ (user-confirmed combat works)
- ✅ Replaced Unity NavMesh with **A* Pathfinding Pro 5.4.6** via the proven Goodgulf approach
  (github.com/Goodgulf281/Emerald2024-Integration). `#if ASTAR`-guarded patches to
  `EmeraldSystem`/`EmeraldMovement`/`EmeraldDebugger` + `NavMeshAgentImposter` (AIPath-derived NavMeshAgent
  stand-in) + `AStarGraphBootstrap` (runtime GridGraph) + `ASTAR` scripting define. Grid graph scans 2720
  walkable nodes; agents auto-get the imposter; combat + pathing confirmed working.
- ⚠️ My earlier hand-rolled fork (EmeraldMover) broke combat and was reverted — do NOT revive it.
- ⬜ Optional: silence A* 5.4 deprecation warnings (`NNConstraint`→`NearestNodeConstraint`,
  `canMove`→`simulateMovement`) — harmless, obsolete calls still function.
- ⬜ RVO local avoidance + dynamic obstacles (the reason for A*) still to wire onto the grid graph.

## Phase 1 — Sorceress archetype (skill VFX + damage) ✅
- ✅ `SpellStrikeVFX` pattern: animation-event "Hit" → spawn VFX at target + deal damage (bypasses Emerald's
  projectile chain). Absorbs stray ExplosiveLLC clip events.
- ✅ GA **LightningStrikeV2** sky-strike wired (`vfxgraph_LightningStrike01_blue`), uniform `scale=4` with
  impact-size compensation, start clears the screen, lands on target, `debug` off.

## Phase 2 — Pre-battle formation (roster bar + free placement) 🟡
- ✅ `BattleManager` placement→battle state machine; freeze/unfreeze Emerald agents.
- ✅ `StartingZone` free-placement rectangle (no grid snapping) + visual.
- ✅ `BattleRoster` bottom bar of Modular-UI-Kit **Avatar cards** with **rendered headshots**; units benched
  until placed; styled **START BATTLE** button.
- ✅ `UnitDragCard` drag-from-bar → drop in zone → place unit.
- ✅ Enlarged battlefield so the player side isn't empty (user pick: "enlarge battlefield").
- ⬜ **Verify in Play** (user couldn't test yet): freeze, portrait framing, pathing after START, layout/spacing.
- ⬜ **Drag-to-reposition** a placed unit; **return-to-bench** (drag off-zone / back to bar; un-dim card).
- ⬜ Placement polish: card press feedback, "X/5 placed" counter, disable START until ≥1 placed, optional
  ghost preview at the drop point.

## Phase 3 — More archetypes (reuse the SpellStrikeVFX pattern) ⬜
For each: clone an Emerald demo agent **in-editor**, swap to the ExplosiveLLC anim set, then script field-config
+ attach `SpellStrikeVFX`/`AnimationEventSink` and wire a GA VFX.
- ⬜ **Archer** (ExplosiveLLC Archer pack) — ranged arrow/volley VFX at target.
- ⬜ **Warrior** (ExplosiveLLC Warrior pack) — melee slash/impact VFX (GA SwordSlashes / GroundSlash).
- ⬜ Incorporate more RPG-Character animations (cast/idle/hit variety) per archetype.

## Phase 4 — Battle loop completion ⬜
- ⬜ Victory / Defeat detection (count living per faction) + end-of-battle UI (Modular UI Kit popup).
- ⬜ Rewards stub on victory; retry/continue.
- ⬜ Turn the battle setup into a reusable prefab/additive scene so the 25 battle scenes share one system.

## Phase 5 — Meta / gacha layer ⬜
- ⬜ **Team-selection screen** before placement: pick 5 from an owned-character roster (feeds `BattleRoster`).
- ⬜ Owned-character data model (uses the modular creator to generate/store appearances).
- ⬜ Gacha summon, progression/levels, stats wired into Emerald (health/damage per character).
- ⬜ 25 static battle scenes / stage select.

## Tech debt / watch-list
- Emerald enable/disable robustness for freeze; if flaky, fall back to toggling NavMeshAgent + combat instead
  of the whole `EmeraldSystem`.
- Portrait render cost/quality (currently per-unit at scene start); consider caching to sprites.
- `FormationPlacer.cs` is dead — delete once the roster flow is confirmed.
- Decide stat/balance source (Emerald fields vs an external character data asset) before Phase 5.
