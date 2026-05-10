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

### Combat work — required reading

For **any combat-system change** (engine, brains, moves, reactions, hit resolution, choreography, animation triggering, status effects), read these two Tier 2 design docs *before writing code*:

- **`Docs/Design/COMBAT_DESIGN.md`** — the move-based engine spec, brain layer, reaction system, resource model, range bands, animation contract, determinism. **Combat is move-based, frame-data driven, reactive.** If a change in code would push combat away from that model (toward state-machine-discrete, exchange-locked, or 1-second Decide cycles), stop and flag it — that's a regression.
- **`Docs/Design/MOVES_CATALOG.md`** — the master list of every move animation handle. Code references move names from this list; new moves are added to the catalog *first*, then to code. Don't reference a move name that isn't in the catalog.

The Tier 1 `04_BATTLE_SYSTEM.md` describes what is currently shipped. The Tier 2 design docs describe where the system is going. **They will diverge during the engine rewrite** — that's expected. Tier 1 catches up as code lands.

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

These describe what the game is trying to be and how the code should be organized.

- `01_VISION.md` — Game identity, fantasy, long-term direction
- `02_ARCHITECTURE.md` — Architectural rules and engineering standards
- `07_PRESENTATION.md` — Animation pipeline, visual feel goals
- `Docs/Design/*.md` — Design specs, roadmaps, and long-term vision (combat, environment, animation catalog, etc.)

**Update Tier 2 docs in the same task as the change that affects them**, including when:

- A subsystem ships, is renamed, replaced, or removed (update tables, references, and roadmap status)
- A roadmap phase or milestone described in the doc lands (mark it shipped)
- The user changes a design described in the doc — reflect their new direction
- Cross-references to other docs become stale because of code or doc moves

**Stop and ask before touching Tier 2 in these cases:**

- The proposed edit changes *intent*, not just current state — e.g., "no multiplayer" → "snapshot multiplayer", or rewriting the architectural philosophy
- A code change conflicts with a stated principle (e.g., adding logic to `TerrainBattleManager` that the doc says doesn't belong there) — flag it, don't unilaterally rewrite the rule away
- The change is about long-term direction, vision, or game identity rather than current implementation

The bar is: **reflecting reality is fine; redefining the goal is not.**

### Tier 3 — Forward-looking

- `08_ROADMAP.md` — Future systems (progression, save/load, status effects, presentation features)

Mark items complete when shipped. Don't redesign unshipped items unilaterally.

### Archive

The `archive/` folder contains legacy docs from the original grid-based MVP design. **They do not reflect the current code and must not be used as a reference.** They are kept only for historical context. Never link to them from new docs.

---

## Documentation maintenance rules

**Documentation maintenance is part of every task, not a separate concern.**

When you complete a task that changes how a system actually works, update the relevant doc in the **same response**. Don't defer it. An out-of-date doc is worse than no doc. This applies across all tiers — Tier 2 included, when the edit reflects shipped reality (see the tier table above for the intent/state distinction).

### What counts as a change requiring a doc update

- New subsystem MonoBehaviour, new ScriptableObject type, or new enum value (update Tier 1 data/system docs and Tier 2 subsystem tables / architecture references)
- New field on `ActionDefinition`, `ComboRecipeDefinition`, `UnitDefinition`, `UnitRuntime`, or any other live data model
- New state in `UnitCombatState`, new transition rule, or new combat phase
- New combo recipe, new action input, or new skill behavior path
- Changes to energy / dodge / block / cast-type rules
- New editor menu tool under `TacticalRPG`
- New scene or major scene restructuring
- Roadmap phase shipped — update the spec doc (`Docs/Design/COMBAT_DESIGN.md`, `08_ROADMAP.md`, etc.) to mark it done

### What does NOT count and should NOT trigger doc updates

- Refactors that don't change observable behavior
- Bug fixes that restore intended behavior
- Internal renames that don't cross system boundaries
- Performance optimizations that preserve semantics

### When in doubt

Ask before editing a doc that defines *intent* — vision, principles, long-term direction. Edits that just bring a doc up to date with what shipped or with a design change the user asked for are routine and don't need confirmation.

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
