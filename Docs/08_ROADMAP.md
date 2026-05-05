# 08_ROADMAP.md

> **Tier 3 — Forward-looking.** Mark items complete when shipped. Don't redesign unshipped items unilaterally.

---

## Purpose

This document captures the systems that are planned but not yet built, plus visual/feel goals that haven't been integrated. It exists so the long-term shape of the game stays in view without cluttering the live system docs.

When an item here is implemented, **move it to the appropriate Tier 1 doc** and remove the entry here (or mark it shipped). Don't leave both.

---

## Status legend

- 📋 **Planned** — Designed, awaiting implementation
- 🟡 **In progress** — Partially implemented
- ✅ **Shipped** — Fully implemented (move to Tier 1 doc and remove)
- 💭 **Idea** — Not yet designed, just a future direction

---

## Combat extensions

### Status effects (buffs/debuffs system) 📋

A general-purpose system for temporary stat changes, damage-over-time, and special conditions on units.

**Current state:** A narrow version of this exists today as `ActiveBuff` (charge-based bonus damage, used by Elemental actions). It is not yet a general status effect system.

**Target design:**

- `StatusEffectDefinition` ScriptableObjects describing each effect (Burn, Stun, Slow, Strengthen, etc.)
- `StatusEffectInstance` runtime objects on `UnitRuntime` with duration, stacks, and owner reference
- New subsystem `BattleStatusEffectSystem` that ticks effects, applies stat modifiers, handles stacking, and fires expiration events
- Integration with the modifier pipeline in `BattleCombatResolver` (effects contribute to damage and stat calculations)

**Why it matters:** Most of the planned skill expansions (debuffs, healing-over-time, control effects) and most planned items will need this system as a foundation. Build it before adding many skills that depend on it.

**Notes:**
- Reuse the existing `ActiveBuff` pattern, but generalize: not all effects are damage-on-hit; some modify stats, some apply each tick
- Stacking rules must be explicit (refresh, stack-up-to-N, replace, ignore-if-present)
- Visual layer needs a way to show active effects (icons above health bar, particle systems)

---

### Modifier pipeline for damage and healing 🟡

`02_ARCHITECTURE.md` describes the target: a pipeline where damage is computed as `base → additive modifiers → multiplicative modifiers → final`, and items/passives/status-effects contribute modifiers rather than the resolver special-casing.

**Current state:** Proficiency multipliers are applied generically (good). The `pendingPowerBoost` from Support actions is applied as a one-shot multiplier. But there's no general modifier pipeline yet — each effect is hardcoded into its own resolver branch.

**Target:**

- `CombatContext` object passed through resolution, holding a list of `IModifier` contributions
- Subsystems (status effects, items, passives) can register modifier sources
- Resolver iterates the list, applies in order
- Adding a new "+10% damage to fire enemies" item is one new modifier definition, not a resolver change

**Sequencing:** Best done before items and passives come online, so those systems can plug in cleanly.

---

### Items and equipment 📋

Heroes will have equipment slots (weapon, armor, accessory) that modify stats and unlock special effects.

**Target design:**

- `ItemDefinition` ScriptableObjects with stat modifiers, on-hit effects, conditional triggers
- `EquipmentLoadout` on `UnitRuntime` (weapon, armor, accessory)
- Hero config scene gets equipment slots in the hero panel
- Items contribute modifiers via the modifier pipeline (above)
- Some items add new combo recipes or modify existing ones

**Examples:**
- Weapon: "+15% damage with Earth element"
- Armor: "+20 max HP, -10% incoming Lightning damage"
- Accessory: "First combo each battle has +50% power"

**Dependencies:** Status effect system, modifier pipeline.

---

### Passive abilities 📋

Per-hero unlockable perks that change combat behavior.

**Target design:**

- `PassiveDefinition` ScriptableObjects with effect triggers and conditions
- Passives unlocked through progression (level-ups, skill tree nodes)
- Examples: "Berserker — +20% damage when below 50% HP", "First Strike — start battle with +10 attack for 5s", "Combo Master — Physical techniques cost 1 less action"

**Implementation pattern:** Same as items — passives contribute modifiers via the pipeline. Some passives require event hooks (on-kill, on-receive-damage) which the combat subsystems should expose.

**Dependencies:** Modifier pipeline, event hooks on combat subsystems.

---

### Hero progression and leveling 📋

Heroes gain XP from battles, level up, gain stats, unlock options.

**Target design:**

- `HeroProgressionData` runtime object on `UnitRuntime` (level, XP, unlocked passives)
- Stat growth curves on `UnitDefinition` (HP-per-level, attack-per-level, etc.)
- Proficiency point system: 1 point per level, player-assigned to elements/categories
- Skill tree nodes unlocked via skill points (gained from level-up or specific battles)
- Action unlocks gated by level / progression
- Persistence via the save system (below)

**Design philosophy from `01_VISION.md`:** progression should expand tactical options, not just inflate numbers. New actions, new passives, new build directions — not just bigger HP bars.

---

### Save and load 📋

Persistent player data: hero progression, unlocked content, squad configurations, skill loadouts.

**Target design:**

- JSON-based save format (human-readable, easy to debug, supports versioning)
- Save location: `Application.persistentDataPath/Saves/`
- Auto-save after battle end, after progression changes, after squad changes
- Save versioning with migration paths between versions
- Backup file on save (recover from corruption)
- Multiple save slots
- Future: Steam Cloud integration

**Save data scope:**
- Hero levels, XP, stat growth, unlocked passives, proficiency assignments
- Equipped skill loadouts per hero
- Player-level data: total battles, currency, unlocked content
- Selected squad configuration

**Not saved:** active battle state. Battles are atomic; if interrupted, they're lost.

---

### Mission objectives beyond "defeat all enemies" 📋

Currently the only win condition is wiping the enemy team. Planned objectives:

- **Survive for N seconds** — defensive scenarios, wave defense
- **Protect target** — escort missions, defend a friendly NPC unit
- **Defeat boss only** — minions don't count, kill the boss
- **Time limit failure** — must win within X seconds or lose
- **Capture point** — hold a position for N seconds
- **Custom objectives** — mission-specific scripted goals

**Implementation pattern:**

- `IBattleObjective` interface with `CheckOutcome(squad state) → BattleOutcome`, `GetObjectiveText() → string`, `GetProgress(squad state) → float`
- `TerrainBattleManager` queries the active objective each tick
- Objectives are configured per-mission via `MissionDefinition` ScriptableObjects (not yet implemented)

---

### Behavior system expansion 📋

Currently three behavior types: Aggressive, Defensive, Balanced. They influence skill selection and engagement aggression.

**Future archetypes:**

- **Skirmisher** — hit-and-run, prefer mobile cast types, retreat when low HP
- **Support** — prioritize healing/buffing teammates, stay in backline
- **Assassin** — target backline enemies, burst damage focus
- **Tank** — prioritize being targeted, maximize block opportunities
- **Caster** — prefer rooted cast types, optimize energy spending on big skills

**Implementation discipline:** behaviors must remain a *small* set of clear archetypes. The vision is "doctrine, not scripting." Don't allow behavior to evolve into a programmable rules engine.

---

### Refined targeting 📋

Current targeting is nearest-enemy. Planned priorities:

- Lowest HP (finishing blows)
- Highest threat (target backline support first)
- Marked target (designated focus fire)
- Behavior-driven (Assassin → backline; Tank → frontline)

`BattleTargetFinder` is the home for these. They'd be selected by behavior type or by per-hero overrides.

---

## Presentation goals (from anime combat vision)

These are visual/feel features that aren't gameplay-critical but contribute heavily to the anime combat experience.

### Tier A — Highest impact (next priorities) 📋

- **Airborne state and aerial follow-up.** High-power attacks launch enemies; attackers may chase into air combos.
- **Terrain-collision knockback.** When a unit is knocked into terrain or props, they impact and slide down.
- **Per-element ground effects.** Earth attacks crater terrain; Fire scorches and leaves DOT zones; Water leaves slippery puddles; Lightning leaves paralysis zones.
- **Cinematic ultimate moments.** Camera lock, slow motion, vignette on screen-clearing moves.

### Tier B — Significant ⛓️ 💭

- **Destructible environment props.** Rocks, pillars shatter on impact.
- **Terrain deformation.** Heavy hits actually displace terrain (or fake it with decals + particles).
- **Per-element trail effects** on weapons and limbs.
- **Sustained particle systems** for active buffs (fire aura, lightning crackle).
- **Reactive camera framing** that adapts to combat distance and intensity.

### Tier C — Polish 💭

- **Speech bubbles / battle cries** during combat
- **Result screen with combat highlights** (biggest hit, longest combo)
- **Replay system** for sharing fights
- **Spectator camera** mode
- **Photo mode**

### Tier D — Far future 💭

- **Voice acting** for hero callouts
- **Per-hero unique signature animations** beyond the shared move set
- **Cutscene system** for story missions
- **Cosmetic skin system** (visual variants of heroes)

---

## Tooling and workflow improvements

These are quality-of-life items for the development process.

- 💭 **Visual combo builder** in the editor — drag actions into chains, see resolved technique, save as a recipe asset
- 💭 **Battle replay system** — record battle inputs, replay deterministically
- 💭 **Combat scenario sandbox** — quick-start specific matchups for testing combos
- 💭 **Balance dashboard** — automated batches of N battles to gather damage/win-rate stats
- 💭 **In-Editor combat debugger** — visualize state machine transitions live, inspect `UnitRuntime` mid-battle

---

## Out of scope (do not pursue)

These are explicitly **not** part of the project. If a request would push toward them, push back.

- ❌ **Multiplayer / PvP.** Single-player only. PvE design constraints don't translate to PvP balance.
- ❌ **Real-time direct combat control.** The game is auto-resolving by design. Manual abilities during combat would invalidate the entire pre-battle preparation pillar.
- ❌ **Networked features.** No leaderboards-as-core, no async PvP, no MMO elements. Steam Cloud for save sync is the limit.
- ❌ **Roguelike runs as the primary mode.** Mission-based PvE is the structure.
- ❌ **Microtransactions.** Single-purchase Steam game.

---

## How to update this document

When you implement an item from this roadmap:

1. Add the new system to the relevant Tier 1 doc (`03_DATA_MODELS.md`, `04_BATTLE_SYSTEM.md`, etc.)
2. Remove the entry from this doc, OR mark it ✅ shipped with a one-line note about where it now lives
3. If only part of an item is shipped, mark it 🟡 in progress and update the description with what's done

When you have a new idea:

1. Add it as 💭 idea or 📋 planned
2. Don't expand idea-level entries into full specs until they're being actively scheduled
3. Tier 3 is a wishlist; if it grows past ~30 items, prune

When the user explicitly changes scope (adds or removes a planned system):

1. Update this doc immediately in the same conversation
2. If the change touches Tier 2 docs (vision, architecture), update those too — but only with explicit user instruction (per `CLAUDE.md`)
