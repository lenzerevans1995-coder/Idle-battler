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

**Agents** (Emerald demo clones — cloned in-editor, never via script):
- **Player (faction 0)** — 5 × "Example Combat AI (Firsts)". Start **benched** (inactive) and are placed by
  the roster bar at runtime.
- **Enemy (faction 1)** — 5 × "Example Combat AI (Sword)" + **Enemy_Sorceress**. Spread to fill their half:
  front row z=+4, mid row z=+7.5, Sorceress back z=+11, all facing −Z (the player).

**Battlefield framing** (enlarged 2026-06-07 so the tall portrait screen isn't mostly empty grass):
- Camera: Main Camera at (0,12,−16), pitch ~35.7°, **orthographic**, with `BattleCameraFit` (fit-width = 9).
  Orthographic ⇒ units are the same screen size at any depth. Visible ground ≈ x[−4.5,4.5], z[−16,+17].
- Player placement zone (`StartingZone`): center (0,0,−7), size 9×10 ⇒ z[−12,−2].
- **NavMeshSurface** on `Arena` (collectObjects = Volume): center (0,1,−0.5), size (9.5,6,26) ⇒ covers the
  enlarged field. Rebuild via `NavMeshSurface.BuildNavMesh()` (reflection) after resizing — placed units can't
  move if they're warped off the navmesh.
- `Arena` = 40×50 ground plane.

**`BattleSystem`** GameObject hosts: `BattleManager`, `StartingZone`, `BattleRoster`.

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
  styled **START BATTLE** button (Button-Primary). `Place(entry, worldPos)` activates the unit, warps it into
  the zone (clamped), faces +Z, and keeps it frozen until battle start. Placed cards dim.
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
