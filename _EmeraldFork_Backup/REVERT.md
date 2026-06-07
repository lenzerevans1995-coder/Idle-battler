# Emerald → A* fork — REVERT guide

This folder lets you fully undo the Emerald-AI-uses-A*-Pathfinding fork. It lives **outside `Assets/`** on
purpose so Unity never compiles these backup copies.

## Revert options

### 0. Git (primary) — repo is now under version control
Baseline commit **`158fe0d`** ("Initial commit … pre Emerald->A* fork") on
`https://github.com/lenzerevans1995-coder/Idle-battler.git` is the clean pre-fork state.
- Undo just the Emerald edits: `git checkout 158fe0d -- "Assets/Emerald AI"`
- Or roll the whole tree back: `git revert <fork commits>` (or `git reset --hard 158fe0d` if nothing else to keep).

## Fallbacks (if not using git)

### 1. Runtime toggle (no file changes)
The fork routes Emerald's movement through an `EmeraldMover` adapter that wraps **both** a Unity NavMeshAgent
and an A* `RichAI`. Set the global flag back to NavMesh:
- `EmeraldMover.UseAStarGlobally = false` (or the `Use A Star` toggle on the agents / battle setup object).
With it off, Emerald behaves exactly as the stock NavMesh version. This is the fast, safe rollback.

### 2. Restore the original Emerald scripts (full source revert)
Copy the pristine scripts back over the edited ones:
```
# from the project root "My project (2)"
rm -rf "Assets/Emerald AI/Scripts"
cp -r "_EmeraldFork_Backup/Scripts_ORIGINAL" "Assets/Emerald AI/Scripts"
```
Then in Unity let it recompile. (The .meta files are preserved, so GUIDs/refs stay intact.)

### 3. Remove the added project code + scene components
Delete the new files under `Assets/_PROJECT/Code/` listed below, and remove the A* components from the scene
(AstarPath, RVOSimulator, and Seeker/RichAI on agents).

## What the fork changes (kept in sync as work proceeds)

**Edited in place (Assets/Emerald AI/Scripts/...):**
- _none yet — updated as edits are made_

**Added (Assets/_PROJECT/Code/...):**
- _none yet — updated as files are added_

**Scene (BattleDemo.unity):**
- _AstarPath + Recast graph, RVOSimulator, Seeker/RichAI on agents — added in Phase A_

**Packages:** `com.arongranberg.astar` v5.4.6 (A* Pathfinding Pro) — already present; left as-is.

_Backup created 2026-06-07, before any Emerald edits._
