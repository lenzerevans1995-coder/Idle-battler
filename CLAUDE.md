# My project (2) — Claude project rules (auto-loaded)

Solo Unity project: an **idle, grid/board-based gacha RPG** for **mobile (portrait, 9:16)**. Static battle
scenes with **5v5 auto-combat** driven by **Emerald AI 2025**. Characters are built from a ported
framework-free **modular character creator** (Synty). Unity **2022.3.62f3**, **URP** (renders via URP).

> Different project from `C:\unity_3d_game\ARPG_V2`. Do not apply ARPG_V2's PlayerTwo/TabbedInventory rules here.

## Start of every session (HARD RULE)
1. **Read `Assets/_PROJECT/Documentation/PROJECT_STATE.md` first** — master index / source of truth.
2. The roadmap lives in `Assets/_PROJECT/Documentation/PLAN.md`.
3. Update both in the **same change** as the code/scene edit they describe.

## Hard "don't"s
- **Don't reimport / update Emerald AI.** Two framework files are hand-patched for Unity 2022/2023 compat
  (`#if UNITY_6000_0_OR_NEWER` guards around `linearVelocity`/`linearDamping`): 
  `Emerald AI/Scripts/.../Ability Object Scripts/Grenade/Grenade.cs` and
  `Emerald AI/Scripts/Components/Optional/LocationBasedDamage.cs`. A reimport reverts them.
- **Don't create or duplicate Emerald agents from script** (instantiate/DuplicateAI corrupts Emerald's
  registration and can break *all* agents globally). Clone agents only in the editor (Ctrl+D / AI Duplicator).
  **Field-config on existing agents via script IS safe** (positions, factions, component refs, enable/disable).
- **Don't use the new Input System** — this project has **no `com.unity.inputsystem`** package; it uses the
  **legacy Input Manager**. Use `UnityEngine.Input` (`Input.GetMouseButtonDown`, `Input.GetTouch`, `Input.mousePosition`).
- **Don't blindly `git add -A`** — vendor packs are large/owned (Emerald AI, ExplosiveLLC, Synty,
  GabrielAguiarProductions, ModularGameUIKit, GSpawn, Grass Painter). Add only `Assets/_PROJECT/**` unless told.
- **Don't re-enable ARPG/extra animators** on an Emerald agent — Emerald drives the Animator. ExplosiveLLC clip
  events (Hit/Shoot/foot) are absorbed by `AnimationEventSink` (or `SpellStrikeVFX` on casters).

## Working conventions
- **UnityMCP** is the editor bridge. `mcp__UnityMCP__execute_code` runs **CodeDom C# 6**: no local functions,
  fully-qualify types, disambiguate `UnityEngine.Object`, use `BindingFlags.NonPublic` for private members.
  Resolve Emerald/own types by **reflection on type name** (e.g. `"EmeraldSystem"`, `"EmeraldDetection"`).
- **Play-mode race:** check `Application.isPlaying == false` before `SaveScene` or scene/prefab structural edits;
  `manage_editor stop` is not instantaneous (re-check before proceeding). Edits made *in play* don't persist.
- Owned content lives under **`Assets/_PROJECT/`** (`Code/`, `Data/`, `Prefabs/`, `Scenes/`, `Shaders/`).
- Factions: **0 = player**, **1 = enemy** (Emerald Faction Data; player units face +Z, enemies face −Z).
- Verify visual changes by asking the user to Play (no automated playtest assumed). Don't claim a visual works
  until confirmed.

See `Assets/_PROJECT/Documentation/PROJECT_STATE.md` for architecture, the battle/roster systems, the VFX
strike pattern, the modular creator, and all gotchas.
