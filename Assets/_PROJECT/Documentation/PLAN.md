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

## Phase 3 — Unit archetypes & teams ✅ (pending Play verify — user away)
- ✅ Three archetypes via Emerald field-config (behavior=Aggressive + attack/too-close distances + Animator tempo):
  - **Sorceress (Artillery)** — casts from range; `ArtilleryKite` retreats her when an enemy is within 5 (cast
    interrupts), since Emerald's built-in backup is blocked while attacking.
  - **Hand Fighter (Rushdown, "Firsts")** — short attack range (1.3), fast tempo (1.35×).
  - **Sword Fighter (Juggernaut, "Sword")** — long melee reach (3.3), slow heavy tempo (0.82×).
- ✅ Both teams = **1 Sorceress + 2 Hand + 2 Sword** (player faction 0 benched for the roster; enemy faction 1
  positioned). `Player_Sorceress` is an edit-mode `Object.Instantiate` clone (= Ctrl+D) — **needs Play verify**
  it isn't corrupted (detects/casts; original still works).
- ✅ Playable area widened: camera `fitWidth` 13 (visible x ±6.5), A* grid + clamp x ±6, zone 11 wide;
  `ArenaBoundsClamp` hard-keeps units on-screen; direction-aware dodge steers toward center near walls.
- ⬜ TODO (need AttackData/anim assets): Sword **knockback/stun** on hit; Sorceress charge-up polish; confirm her
  AttackData distance so she truly holds range.
- ⬜ Archer / Warrior (ExplosiveLLC packs) as more unit types — feeds the gacha roster (Phase 5).

## Phase 4 — Battle loop completion ⬜
- ⬜ Victory / Defeat detection (count living per faction) + end-of-battle UI (Modular UI Kit popup).
- ⬜ Rewards stub on victory; retry/continue.
- ⬜ Turn the battle setup into a reusable prefab/additive scene so the 25 battle scenes share one system.

## Phase 5 — Meta / gacha layer + unit variety ⬜  (target design — stated 2026-06-07)
Goal: a roster of **varied units the player pulls via gacha**, plus varied **enemy** units, where each unit:
- can have **more than one ability** (multiple Emerald Type1Attacks/AttackData + AbilityObjects; at-target
  spells via the `SpellStrikeVFX` pattern), and
- can **equip different weapons of its corresponding type** (Emerald Type1/Type2 weapon slots; melee vs ranged
  matched to the unit — reuse the modular creator's equip system for the weapon models).
Tasks:
- ⬜ Owned-character **data model** (modular-creator appearance + stats + ability list + equipped weapon).
- ⬜ **Team-selection screen** before placement: pick 5 from the owned roster (feeds `BattleRoster`).
- ⬜ **Gacha summon** + progression/levels; stats wired into Emerald (health/damage per character).
- ⬜ **Multi-ability units** — assign/cycle several abilities per unit.
- ⬜ **Weapon equipping** per unit (swap weapon model + AttackData by type).
- ⬜ More archetypes/units (Archer, Warrior, …) as gacha pulls.
- ⬜ 25 static battle scenes / stage select.

## Tech debt / watch-list
- Emerald enable/disable robustness for freeze; if flaky, fall back to toggling NavMeshAgent + combat instead
  of the whole `EmeraldSystem`.
- Portrait render cost/quality (currently per-unit at scene start); consider caching to sprites.
- `FormationPlacer.cs` is dead — delete once the roster flow is confirmed.
- Decide stat/balance source (Emerald fields vs an external character data asset) before Phase 5.
