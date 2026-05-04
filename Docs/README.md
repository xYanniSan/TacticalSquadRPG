# DOCUMENTATION INDEX

## Purpose

This is the **master index** for all project documentation. Use this as your starting point to navigate the complete documentation suite.

---

## Quick Navigation

### 🎯 **Start Here**
- [IMPLEMENTATION_OVERVIEW.md](Core/IMPLEMENTATION_OVERVIEW.md) - **What is currently built** — systems, scenes, tech stack, key files
- [PROJECT_OVERVIEW.md](Core/PROJECT_OVERVIEW.md) - Game vision, identity, and long-term direction
- [MVP_SCOPE.md](Core/MVP_SCOPE.md) - Original MVP scope decisions *(planning artifact)*

### 🏗️ **Architecture & Standards**
- [ARCHITECTURE_OVERVIEW.md](Core/ARCHITECTURE_OVERVIEW.md) - System architecture and code structure
- [ENGINEERING_RULES.md](Core/ENGINEERING_RULES.md) - Coding standards and best practices
- [DATA_MODELS.md](Core/DATA_MODELS.md) - Core data structures and shared vocabulary

### ⚔️ **Combat Systems**
- [BATTLE_DESIGN.md](BATTLE_DESIGN.md) - **★ Active** — 3D auto-battle state machine, energy, combos, subsystems
- [SKILL_CREATION_GUIDE.md](SKILL_CREATION_GUIDE.md) - **★ Active** — How to add new skills and new skill behaviour
- [SKILL_REFERENCE.md](SKILL_REFERENCE.md) - All actions and combo recipes with damage estimates
- [SYSTEM_SKILLS.md](Systems/SYSTEM_SKILLS.md) - Action-chain skill system design spec
- [SYSTEM_BEHAVIOR.md](Systems/SYSTEM_BEHAVIOR.md) - AI decision-making and behavior types
- [SYSTEM_COMBAT_RESOLUTION.md](Systems/SYSTEM_COMBAT_RESOLUTION.md) - Damage, healing, death handling
- [SYSTEM_TARGETING.md](Systems/SYSTEM_TARGETING.md) - Target selection logic
- [SYSTEM_WIN_CONDITIONS.md](Systems/SYSTEM_WIN_CONDITIONS.md) - Victory/defeat conditions

### 🗺️ **Battlefield Systems** *(hex/grid prototype)*
- [SYSTEM_GRID.md](Systems/SYSTEM_GRID.md) - Grid structure and spatial queries
- [SYSTEM_MOVEMENT.md](Systems/SYSTEM_MOVEMENT.md) - Unit movement execution
- [SYSTEM_BATTLE_STATE.md](Systems/SYSTEM_BATTLE_STATE.md) - Source of truth for active battle

### 🧙 **Hero & Progression Systems**
- [SYSTEM_UNITS.md](Systems/SYSTEM_UNITS.md) - Hero definitions vs runtime instances
- [SYSTEM_PROGRESSION.md](Systems/SYSTEM_PROGRESSION.md) - Leveling, stats, skill trees *(future)*
- [SYSTEM_STATUS_EFFECTS.md](Systems/SYSTEM_STATUS_EFFECTS.md) - Buffs, debuffs, DOT/HOT *(future)*

### 🎨 **Design & UX**
- [ANIME_COMBAT_VISION.md](ANIME_COMBAT_VISION.md) - Visual and feel direction for combat
- [HERO_CONFIG_MENU.md](HERO_CONFIG_MENU.md) - Hero config UI design

### 💾 **Meta Systems**
- [SYSTEM_SAVE_LOAD.md](Systems/SYSTEM_SAVE_LOAD.md) - Persistence and save files *(future)*
- [SYSTEM_UI_PREBATTLE.md](Systems/SYSTEM_UI_PREBATTLE.md) - Pre-battle configuration UI

---

## Documentation Structure

```
Docs/
├── README.md                           # This file — master index
│
├── Core/                               # Foundation documents
│   ├── IMPLEMENTATION_OVERVIEW.md     # ★ What is currently built
│   ├── PROJECT_OVERVIEW.md             # Game vision and identity
│   ├── MVP_SCOPE.md                    # Original MVP scope (planning artifact)
│   ├── ARCHITECTURE_OVERVIEW.md        # Code architecture principles
│   ├── ENGINEERING_RULES.md            # Coding standards
│   └── DATA_MODELS.md                  # Data structures and enums
│
├── Systems/                            # Individual system specs
│   ├── SYSTEM_SKILLS.md
│   ├── SYSTEM_BEHAVIOR.md
│   ├── SYSTEM_COMBAT_RESOLUTION.md
│   ├── SYSTEM_TARGETING.md
│   ├── SYSTEM_WIN_CONDITIONS.md
│   ├── SYSTEM_GRID.md
│   ├── SYSTEM_MOVEMENT.md
│   ├── SYSTEM_BATTLE_STATE.md
│   ├── SYSTEM_UNITS.md
│   ├── SYSTEM_PROGRESSION.md
│   ├── SYSTEM_STATUS_EFFECTS.md
│   ├── SYSTEM_SAVE_LOAD.md
│   └── SYSTEM_UI_PREBATTLE.md
│
├── BATTLE_DESIGN.md                    # ★ Active 3D combat design
├── SKILL_CREATION_GUIDE.md             # ★ How to add new skills
├── SKILL_REFERENCE.md                  # All actions and combo recipes
├── ANIME_COMBAT_VISION.md              # Visual/feel direction
└── HERO_CONFIG_MENU.md                 # Config UI design
```

---

## Which docs to read first (for a new session)

To get up to speed on what's built and how to work on it:

1. `Core/IMPLEMENTATION_OVERVIEW.md` — tech stack, scenes, all systems at a glance
2. `BATTLE_DESIGN.md` — how the 3D combat loop works
3. `SKILL_CREATION_GUIDE.md` — how to add or modify skills
4. `SKILL_REFERENCE.md` — what actions and combos currently exist

    ├── SYSTEM_SKILLS.md            # ★ CORE IDENTITY
    ├── SYSTEM_COMBAT_RESOLUTION.md
    ├── SYSTEM_STATUS_EFFECTS.md
    ├── SYSTEM_WIN_CONDITIONS.md
    ├── SYSTEM_PROGRESSION.md
    ├── SYSTEM_SAVE_LOAD.md
    └── SYSTEM_UI_PREBATTLE.md
```

---

## Document Summaries

### Core Documents

| Document | Purpose | Priority |
|----------|---------|----------|
| **PROJECT_OVERVIEW.md** | High-level game vision, player fantasy, core mechanics | Read First |
| **MVP_SCOPE.md** | What to build first, what to defer | Critical |
| **DATA_MODELS.md** | Shared data structures and terminology | Critical |
| **ARCHITECTURE_OVERVIEW.md** | System architecture and design patterns | Critical |
| **ENGINEERING_RULES.md** | Coding standards and best practices | Critical |

---

### System Documents

| System | Purpose | MVP Priority |
|--------|---------|--------------|
| **SYSTEM_SKILLS** | ★ **Action-chain skill system** (5x5 structure) | **CRITICAL** |
| **SYSTEM_GRID** | Battlefield grid structure | High |
| **SYSTEM_BATTLE_STATE** | Source of truth for active battle | High |
| **SYSTEM_UNITS** | Hero definitions vs runtime instances | High |
| **SYSTEM_BEHAVIOR** | AI decision-making | High |
| **SYSTEM_TARGETING** | Target selection | High |
| **SYSTEM_MOVEMENT** | Unit movement execution | High |
| **SYSTEM_COMBAT_RESOLUTION** | Damage/healing calculation | High |
| **SYSTEM_WIN_CONDITIONS** | Victory/defeat logic | High |
| **SYSTEM_UI_PREBATTLE** | Pre-battle configuration interface | High |
| **SYSTEM_STATUS_EFFECTS** | Buffs/debuffs | Optional (Future) |
| **SYSTEM_PROGRESSION** | Leveling and stat growth | Out of Scope (Future) |
| **SYSTEM_SAVE_LOAD** | Persistence | Out of Scope (Future) |

---

## Reading Paths

### 🚀 **For Getting Started (First-Time Read)**

1. [PROJECT_OVERVIEW.md](Core/PROJECT_OVERVIEW.md) - Understand the vision
2. [MVP_SCOPE.md](Core/MVP_SCOPE.md) - Know what to build first
3. [DATA_MODELS.md](Core/DATA_MODELS.md) - Learn shared terminology
4. [ARCHITECTURE_OVERVIEW.md](Core/ARCHITECTURE_OVERVIEW.md) - Understand code structure
5. [ENGINEERING_RULES.md](Core/ENGINEERING_RULES.md) - Learn coding standards

---

### ⚔️ **For Implementing Combat**

1. [SYSTEM_GRID.md](Systems/SYSTEM_GRID.md) - Foundation
2. [SYSTEM_BATTLE_STATE.md](Systems/SYSTEM_BATTLE_STATE.md) - Source of truth
3. [SYSTEM_UNITS.md](Systems/SYSTEM_UNITS.md) - What fights
4. [SYSTEM_BEHAVIOR.md](Systems/SYSTEM_BEHAVIOR.md) - How they decide
5. [SYSTEM_TARGETING.md](Systems/SYSTEM_TARGETING.md) - Who they attack
6. [SYSTEM_MOVEMENT.md](Systems/SYSTEM_MOVEMENT.md) - How they move
7. **[SYSTEM_SKILLS.md](Systems/SYSTEM_SKILLS.md)** - ★ **Core identity**
8. [SYSTEM_COMBAT_RESOLUTION.md](Systems/SYSTEM_COMBAT_RESOLUTION.md) - Damage execution
9. [SYSTEM_WIN_CONDITIONS.md](Systems/SYSTEM_WIN_CONDITIONS.md) - When battle ends

---

### 🎨 **For Implementing UI**

1. [SYSTEM_UI_PREBATTLE.md](Systems/SYSTEM_UI_PREBATTLE.md) - Pre-battle interface
2. [SYSTEM_SKILLS.md](Systems/SYSTEM_SKILLS.md) - Action-chain configuration
3. [SYSTEM_BEHAVIOR.md](Systems/SYSTEM_BEHAVIOR.md) - Behavior assignment
4. [SYSTEM_GRID.md](Systems/SYSTEM_GRID.md) - Grid placement

---

### 🔮 **For Future Features**

1. [SYSTEM_PROGRESSION.md](Systems/SYSTEM_PROGRESSION.md) - Hero leveling
2. [SYSTEM_SAVE_LOAD.md](Systems/SYSTEM_SAVE_LOAD.md) - Persistence
3. [SYSTEM_STATUS_EFFECTS.md](Systems/SYSTEM_STATUS_EFFECTS.md) - Buffs/debuffs

---

## Core Principles (From Documentation)

### 🎯 **Design Identity**

1. **Action-Chain Skill System** - 5 skill slots × 5 actions = signature mechanic
2. **Pre-Battle Preparation** - Player control happens before combat
3. **Auto-Resolving Combat** - Watch the plan unfold
4. **Flexible Hero Roles** - Guided by proficiencies, not locked
5. **Tactical Grid Combat** - Positioning and movement matter

---

### 🏗️ **Architecture Principles**

1. **Modular Systems** - Independent, loosely coupled systems
2. **Data-Driven Design** - Configuration in data files, not code
3. **Static vs Runtime Separation** - ScriptableObjects never modified at runtime
4. **Battle Manager as Coordinator** - Not owner of all logic
5. **Extensibility** - Build for future items, passives, modifiers

---

### 📏 **Engineering Rules**

1. **One Class, One Job** - Single responsibility principle
2. **No God Classes** - Avoid monolithic managers
3. **No Hardcoding** - Use data-driven modifiers
4. **Gameplay Truth in Code** - Not animation events
5. **Clean System Boundaries** - Communication through data, not direct calls

---

### 🎮 **MVP Discipline**

1. **Tiny Scope** - 2v2 battles, minimal features
2. **Prove Core Combat** - Validate before expanding
3. **Placeholder Visuals** - Functional over beautiful
4. **Simplified Skill System** - 2 slots × 5 actions (not full 5×5)
5. **Defer Progression** - Focus on combat feel

---

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | 2024 | Initial documentation suite complete |

---

## Using These Docs with AI Agents

### Recommended Workflow

1. **Reference core docs first** - PROJECT_OVERVIEW, ARCHITECTURE_OVERVIEW, ENGINEERING_RULES
2. **Reference relevant system docs** - When implementing specific features
3. **Follow DATA_MODELS** - Use shared structures and terminology
4. **Stay within MVP_SCOPE** - Don't overbuild

### AI Prompting Tips

**Good prompt:**
> "Implement the TargetingSystem following SYSTEM_TARGETING.md. Use the nearest-enemy logic for MVP and follow the data models in DATA_MODELS.md."

**Bad prompt:**
> "Make targeting work."

---

## Glossary

Quick reference for common terms (see DATA_MODELS.md for full definitions):

- **Unit** - Any hero or enemy on the battlefield
- **Definition** - Static template (ScriptableObject)
- **Runtime** - Live instance during battle
- **Action** - Single building block (punch, kick, sign)
- **Action-Chain** - Sequence of 5 actions
- **Technique** - Resolved ability from action-chain
- **Skill Slot** - Container for one action-chain (5 slots per hero)
- **Behavior** - Battle decision-making logic (not hero identity)
- **Proficiency** - Natural strength with actions/elements
- **Intent** - What a unit wants to do (generated by behavior)
- **Execution** - Actually doing the action

---

## Contributing to Documentation

### When to Update Docs

- ✅ System architecture changes
- ✅ New features added
- ✅ MVP scope changes
- ✅ Data model changes
- ✅ Engineering rules evolve

### How to Update

1. Edit the relevant `.md` file
2. Update version number at bottom
3. Update this index if adding new files
4. Commit with clear message

---

## Summary

This documentation suite provides:

- ✅ **Complete game vision** (PROJECT_OVERVIEW)
- ✅ **Clear scope** (MVP_SCOPE)
- ✅ **Solid architecture** (ARCHITECTURE_OVERVIEW)
- ✅ **Coding standards** (ENGINEERING_RULES)
- ✅ **Shared terminology** (DATA_MODELS)
- ✅ **Detailed system specs** (13 SYSTEM_*.md files)

**Everything needed to build this game is documented here.**

---

## Contact / Feedback

For questions or suggestions about these docs, please reach out to the development team.

---

**Last Updated:** 2024
**Documentation Version:** 1.0
**Total Documents:** 18 files (5 core + 13 systems)
