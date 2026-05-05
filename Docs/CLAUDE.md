# CLAUDE.md

> **This is the entry point for any AI agent working on this project. Read it first, every session.**

---

## What this project is

A Unity 6 (URP) **3D auto-battler with directional skills and an extensive combo system**, anime-inspired ninja/martial-fantasy fiction, single-player PvE, intended for Steam release.

Players configure heroes pre-battle (skill loadouts, action sequences) and watch combat resolve automatically on a 3D terrain map. The game's identity comes from the **action-chain combo system**, not from direct combat control.

For the full vision and design intent, see `01_VISION.md`. For the current implementation, see `04_BATTLE_SYSTEM.md`.

---

## Read order for new sessions

Before starting any non-trivial task, skim these in order:

1. **This file (`CLAUDE.md`)** — conventions and guardrails
2. **`01_VISION.md`** — what the game is trying to be
3. **`02_ARCHITECTURE.md`** — how the code is structured and the rules for keeping it that way
4. **The Tier 1 doc most relevant to your task** — see tier list below

For tasks involving combat changes, also read `03_DATA_MODELS.md` and `04_BATTLE_SYSTEM.md` even if they seem tangential. Combat ripples.

---

## Documentation tiers

The docs in this folder are **not all equal**. They serve different purposes and have different update rules.

### Tier 1 — Living docs (must reflect current code)

These describe how systems actually work right now. They are updated **in the same task** as any code change that affects them.

- `03_DATA_MODELS.md` — Live data structures (ScriptableObjects, runtime classes, enums)
- `04_BATTLE_SYSTEM.md` — Combat state machine, subsystems, energy/dodge/block, win conditions
- `05_SKILL_SYSTEM.md` — Action definitions, combo resolution, skill slots, all recipes
- `06_HERO_CONFIG.md` — Pre-battle hero configuration scene

### Tier 2 — Stable docs (intent and principles)

These describe what the game is trying to be and how the code should be organized. Implementation changes do **not** touch them.

- `01_VISION.md` — Game identity, fantasy, long-term direction
- `02_ARCHITECTURE.md` — Architectural rules and engineering standards
- `07_PRESENTATION.md` — Animation pipeline, visual feel goals

**Never update Tier 2 docs without explicit user instruction.** If a task seems to require it, stop and ask — that's a sign the user's intent has shifted and they need to confirm.

### Tier 3 — Forward-looking

- `08_ROADMAP.md` — Future systems (progression, save/load, status effects, presentation features)

Mark items complete when shipped. Don't redesign unshipped items unilaterally.

### Archive

The `archive/` folder contains legacy docs from the original grid-based MVP design. **They do not reflect the current code and must not be used as a reference.** They are kept only for historical context. Never link to them from new docs.

---

## Documentation maintenance rules

**Documentation maintenance is part of every task, not a separate concern.**

When you complete a task that changes how a system actually works, update the relevant Tier 1 doc in the **same response**. Don't defer it. An out-of-date doc is worse than no doc.

### What counts as a change requiring a doc update

- New subsystem MonoBehaviour, new ScriptableObject type, or new enum value
- New field on `ActionDefinition`, `ComboRecipeDefinition`, `UnitDefinition`, `UnitRuntime`, or any other live data model
- New state in `UnitCombatState`, new transition rule, or new combat phase
- New combo recipe, new action input, or new skill behavior path
- Changes to energy / dodge / block / cast-type rules
- New editor menu tool under `TacticalRPG`
- New scene or major scene restructuring

### What does NOT count and should NOT trigger doc updates

- Refactors that don't change observable behavior
- Bug fixes that restore intended behavior
- Internal renames that don't cross system boundaries
- Performance optimizations that preserve semantics

### When in doubt

Ask before editing a doc. A clarifying question is cheaper than a wrong update. **Especially** for Tier 2.

---

## Architectural guardrails

These rules exist to prevent the project from collapsing into god classes. They are **strict, not advisory.**

### 1. `TerrainBattleManager` is a coordinator, never a logic owner

Allowed in `TerrainBattleManager`:
- Spawning and despawning units
- Wiring subsystems together at startup
- Owning references to subsystem instances
- Win-condition checks that delegate to a dedicated method
- Top-level battle flow (countdown, start, end)

**Forbidden in `TerrainBattleManager`:**
- Damage formulas
- Targeting decisions
- Skill resolution
- Animation control
- Specific hero or combo logic
- Anything that could plausibly live in a subsystem

If you're tempted to add a method to `TerrainBattleManager` that does any of the forbidden things, **create a new subsystem MonoBehaviour instead.**

### 2. `TerrainBattleUnit` owns its own state machine, nothing more

Allowed in `TerrainBattleUnit`:
- The combat state machine and transitions (`Engage`, `Decide`, `Melee`, `CastMobile`, `CastRooted`, `Execute`, `Recover`, `Dodging`, `Dead`)
- Animation triggering and timing
- Receiving damage events from subsystems
- Initiative tracking

**Forbidden in `TerrainBattleUnit`:**
- Damage calculation (delegate to `BattleCombatResolver`)
- Target selection (delegate to `BattleTargetFinder`)
- Knockback or hit-stop logic (delegate to `BattleKnockbackSystem` / `BattleHitStopSystem`)
- Skill resolution (delegate to `SkillSystem`)
- Engagement slot management (delegate to `BattleEngagementManager`)

If a `TerrainBattleUnit` method exceeds ~30 lines or has more than one concern, **split it.** State-machine update methods are the only exception, and even those should mostly delegate.

### 3. New combat behavior goes in a new subsystem

When adding a feature like "stamina," "blocking with shields," "ground slam," or "elemental ground effects":

1. Create a new MonoBehaviour subsystem (e.g., `BattleStaminaSystem`)
2. Attach it to the `TerrainBattleManager` GameObject
3. Wire it through `TerrainBattleManager.Start()` like the existing subsystems
4. Other subsystems and `TerrainBattleUnit` query it via interface or direct reference

**Do not** add stamina fields to `UnitRuntime` and update them from five different places. **Do not** put stamina logic in `TerrainBattleUnit`. The rule is: one subsystem owns one concern.

### 4. Static data stays static

`ScriptableObject` assets (`ActionDefinition`, `ComboRecipeDefinition`, `UnitDefinition`, `ComboLibraryAsset`) are **never modified at runtime.** Runtime state lives in plain C# classes (`UnitRuntime`, `ActiveBuff`, `ResolvedTechnique`).

If you find yourself wanting to write to a ScriptableObject during a battle, you've made a wrong turn — that data belongs in a runtime object instead.

### 5. Data-driven over hardcoded

New combos, actions, and balance values go in ScriptableObjects, not in code. Hero-specific or combo-specific exceptions hardcoded in resolvers are a smell. If you're adding `if (technique.name == "X")` to a resolver, stop and design the data structure that makes the special case generic.

### 6. Animation does not own combat decisions

The Animator is presentation. It does not decide:
- When an ability may start
- Whether a hit lands
- Damage timing (use Animation Events to **notify** code, but the code owns the truth)
- Movement during attacks (code drives the `CharacterController`, not root motion, except for explicit opt-in cases per `07_PRESENTATION.md`)

This rule has a dedicated doc (`07_PRESENTATION.md`) — read it before doing animation work.

### 7. Extensibility is a feature

The combat system will grow to include items, passives, equipment, status effects, and progression. Build for that:

- Use **modifier pipelines** for damage and healing calculations (additive then multiplicative, not hardcoded chains)
- Use **event hooks** so future systems can react to damage/death/skill-cast without touching combat code
- Avoid switch statements over hero IDs or combo names

If a feature you're adding can't accommodate "what if items modify this later," redesign it.

---

## Code conventions

### Namespaces

- `TacticalRPG.DataModels` — pure data: `UnitRuntime`, `ActionDefinition`, `ResolvedTechnique`, etc.
- `TacticalRPG.Systems` — plain-C# systems and resolvers: `SkillSystem`, etc.
- `TacticalRPG.ThirdPerson` — Unity-specific MonoBehaviours for the 3D battle: `TerrainBattleManager`, `TerrainBattleUnit`, all `Battle*` subsystems
- `TacticalRPG.Editor` — editor tools

Do not cross-pollinate. `DataModels` should not reference `UnityEngine.MonoBehaviour`. `ThirdPerson` is the only place Unity-lifecycle code lives.

### File organization

```
Assets/
├── Data/
│   ├── Actions/      # ActionDefinition assets
│   ├── Combos/       # ComboRecipeDefinition + ComboLibrary.asset
│   └── Units/        # UnitDefinition assets
├── Scripts/
│   ├── DataModels/   # Pure data, no Unity dependencies
│   ├── Systems/      # Plain-C# logic
│   ├── ThirdPerson/  # 3D battle MonoBehaviours
│   ├── UI/           # UI controllers
│   └── Editor/       # Editor tools
└── Scenes/
    ├── HeroConfigScene.unity   # Active
    └── TerrainBattleScene.unity # Active
```

### Naming

- Subsystems: `BattleXxxSystem` or `BattleXxxManager` (existing convention)
- ScriptableObjects: `XxxDefinition` for static templates
- Runtime types: `XxxRuntime` for live state, no suffix for transient/value types
- Enums in `DataModels/Enums.cs` — all enums live in one file for discoverability

### Comments

Don't write comments that restate the code. Do write comments that explain **why** when the why isn't obvious — especially around combat timing, state-machine transitions, and animation event coupling.

---

## Testing expectations

There is an existing custom in-engine test runner (~60 tests across 7 files) covering the legacy grid systems and parts of the skill system. When adding new logic to systems, add a test if the project pattern supports it. Don't refactor away the existing grid-system tests without an explicit user instruction — they're retained intentionally even though the grid layer isn't used at runtime.

---

## Unity Editor coordination

This project uses MCP for Unity to let agents drive the Unity Editor directly. When making changes:

- After editing C# files, give Unity time to recompile before triggering Editor operations
- After creating new ScriptableObjects via menu items, refresh the asset database
- Commit before scene edits — scene file recovery is harder than code recovery
- If a tool call fails with "Unity not responding," wait 5–10 seconds and retry rather than escalating

---

## What good looks like

A typical task ("add a new combo: A → C → Kick that triggers a Water Slash skill") should:

1. Read `05_SKILL_SYSTEM.md` to understand the skill model
2. Add the recipe to `SkillDataCreator.cs` and `ComboLibrary.cs`
3. Run the `TacticalRPG → Create Combo Library` editor menu
4. **Update `05_SKILL_SYSTEM.md`'s combo recipe table** to include Water Slash
5. Note in the response that the doc was updated

A task that requires changing the architecture, vision, or animation pipeline should:

1. Stop
2. Quote the rule that's in tension
3. Ask the user whether the design has actually changed

---

## Final note

The single biggest risk to this project's health is documentation drift. The current `archive/` folder is a graveyard of docs that described a game that no longer exists. **Don't let the current docs become the next archive.** Update them as you go.
