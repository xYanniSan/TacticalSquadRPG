# PROJECT_OVERVIEW.md

> **Status:** Vision and design intent document. Describes the game's identity, goals, and long-term direction. For the current implemented systems, see [IMPLEMENTATION_OVERVIEW.md](IMPLEMENTATION_OVERVIEW.md).

## Project Summary

This project is a **single-player, PvE-focused tactical squad RPG** with **auto-resolving combat** on a **grid-based battlefield**. The combat is viewed from an **angled top-down perspective similar to Teamfight Tactics**, but the battlefield is intended to be **larger, more tactical, and more movement-oriented** than a standard TFT board.

The game is **not** a direct TFT clone. Its unique identity comes from combining:

- Small-squad tactical RPG progression
- Grid-based automated combat
- Pre-battle behavior configuration
- Customizable hero battle roles
- A modular skill-combo system (action-chains)
- Mission-based PvE progression
- Item farming and long-term squad development

---

## One-Sentence Summary

**A single-player tactical squad RPG where players build and progress a team of heroes, customize each hero's role, skills, and battle logic, then send them into grid-based PvE battles that resolve automatically.**

---

## Core Player Fantasy

The core player fantasy is:

- Assemble a squad of heroes
- Shape each hero through progression
- Assign battle roles and combat logic
- Equip a set of modular skill/action slots
- Place the squad on a tactical battlefield
- Launch the encounter
- Watch the plan unfold automatically
- Improve builds, roles, and tactics over time

### Intended Emotional Payoff

**"I built these heroes, I decided how they fight, and now I get to watch my strategy play out."**

This game is not about live mechanical execution or direct control during battle. It is about:

- Planning
- Preparation
- Squad synergy
- Progression
- Tactical observation
- Iteration after each mission

---

## High-Level Gameplay Identity

The project sits between several design spaces:

- Squad RPG
- Tactical auto-combat
- Grid-based battle game
- PvE progression game
- Behavior-driven combat game
- Anime-inspired ninja/martial fantasy

### Combat Inspiration

The combat inspiration includes the broad feel of:

- **TFT-style visual perspective** - Angled top-down, readable at a glance
- **Gladiabots-style preconfigured behavior** - But simpler and more accessible
- **RPG squad-building and character growth** - Long-term hero development
- **Anime/ninja fantasy combat** - Flashy skills and special techniques

### Design Intent

The intended result is **not** a deep programming simulator. The system should feel like:

**"Setting doctrine and loadouts, not writing code"**

---

## Theme and World Direction

The current fantasy direction is an **anime-inspired ninja / martial fantasy world** with supernatural or magical combat techniques.

### Intended Feel

- Elite fighters / heroes
- Martial arts and ninja-like combat
- Hand-sign-style sequences or combat actions
- Elemental or supernatural techniques
- Flashy special abilities
- Squad-based missions against NPC enemies
- Character progression and specialization

**Note:** This direction may be inspired in tone by anime ninja combat, but the game should remain an **original IP** and should not directly copy copyrighted characters, names, clans, moves, symbols, or lore.

---

## Platform and Project Context

### Technical Assumptions

The current assumption is that the game will be:

- **Single-player**
- **Released on Steam** (likely)
- **Primarily offline/local**
- **Built in Unity**
- **Coded in C#**
- **Developed over 3–4 years**

### Development Focus

This means the main complexity will be in:

- Combat systems
- Gameplay logic
- AI/behavior logic
- Progression
- Architecture
- Content structure

The project is **not** expected to require heavy backend/server systems for the core game.

---

## Core Gameplay Loop

The long-term gameplay loop is expected to be:

1. **Choose or manage the squad**
2. **Progress heroes** - Assign skills, items, and behavior
3. **Enter a PvE mission**
4. **Place the squad on the battlefield**
5. **Start combat**
6. **Watch combat resolve automatically**
7. **Evaluate the outcome**
8. **Gain rewards** - Items, progression resources
9. **Improve the squad** and try harder content

### Intended Rhythm

This loop should create a satisfying rhythm of:

**Prepare → Execute → Observe → Improve → Progress**

---

## Squad Structure

The player controls a **small squad**, currently envisioned as up to **5 heroes**.

This number may evolve later, but the current goal is to keep the squad small enough that:

- Each hero matters
- The player can understand each hero's job
- Behavior setup stays manageable
- Class/role synergy remains readable

### Hero Identity

Each hero is an individual unit with:

- Identity
- Progression
- Stats
- Proficiencies
- Assigned skills
- Assigned battle behavior
- Role flexibility

---

## Hero Role Philosophy

A hero may be **naturally stronger in certain stats, archetypes, or skill proficiencies**, but should **not** be completely locked into only one rigid role.

### Design Intent

- Each hero has **strengths, proficiencies, and growth directions**
- Progression may improve certain stats, skill families, or combat tendencies
- The player can still **influence the hero's actual battle role** through behavior and skill assignment

### Example Hero Directions

A hero may naturally lean toward:

- Tanking
- Assassination
- Melee fighter
- Ranged fighter
- Support
- Caster
- Control

But the player should be able to shift how that hero behaves in battle by changing:

- Battle logic (behavior)
- Equipped skills
- Skill-slot composition
- Progression choices
- Itemization

---

## Role vs Behavior (Critical Distinction)

**Role** and **Behavior** are **not** the same thing.

- **Role** = The overall combat identity the hero fills in the squad
  - Examples: Tank, Assassin, Support, Bruiser

- **Behavior** = The battle logic that decides what the hero tries to do during the fight
  - Examples: Aggressive, Defensive, Balanced

### Example

A hero might be built as a **fighter** (role) but behave **defensively** (behavior).

A hero might have **assassin-oriented behavior** while still using a unique set of elemental or martial skills.

**Behavior is combat logic only, not the full hero identity.**

---

## Battlefield Structure

Combat takes place on a **grid-based battlefield**.

### Camera Perspective

The camera is intended to be:

- Angled top-down
- Visually similar in readability to TFT
- Larger in scale than a standard TFT board
- Tactical enough to support movement, spacing, flanking, and skill setup

### Tactical Features

The battlefield should support:

- Frontline and backline positioning
- Movement and repositioning
- Melee vs ranged spacing
- Formation planning
- Target access decisions
- Area control (later, if needed)
- Skill timing and engagement flow

**Readability is critical** - Since there is no direct player control during battle, the player must be able to understand what's happening at a glance.

---

## Combat Philosophy

Combat is **preconfigured and auto-resolving**.

The player does **not** directly control units once the battle begins. Instead, the player influences the outcome beforehand by deciding:

- Squad composition
- Battlefield placement
- Hero role direction
- Assigned behavior logic
- Equipped skills/actions
- Progression choices
- Itemization

### During Battle

Once battle starts, heroes automatically:

- Move
- Choose targets
- Use skills
- Execute action chains
- React to enemies according to their configured behavior

### Intended Combat Feel

The combat should feel:

- Tactical
- Readable
- Satisfying to watch
- Expressive
- Strategically deep
- Less complex than a full AI scripting game

---

## Pre-Battle Player Control

The player's meaningful control should happen **before** combat starts.

### Pre-Battle Phase

The pre-battle phase should eventually include:

- Choosing heroes
- Assigning their position on the grid
- Assigning or adjusting skill loadouts
- Configuring battle behavior
- Equipping items
- Making progression choices

**The battle itself is the execution of the player's plan.**

---

## Behavior System

Behavior is specifically the **battle logic** for a hero.

**It is NOT the same as role, progression, or overall class identity.**

### What Behavior Determines

Behavior determines how the hero acts during combat, such as:

- Whether they advance or hold position
- Whether they seek close combat or keep distance
- Who they prioritize as targets
- When they trigger skills or action chains
- Whether they stay aggressive or reposition
- How they react under pressure

### Design Goals

The intended system should be **simpler than Gladiabots**. It should not require deep logic trees or coding-like complexity.

**Behavior should feel like setting a combat doctrine.**

### Example Behavior Concepts

Examples of behavior concepts may include:

- Advance toward enemy
- Maintain distance
- Target nearest enemy
- Target backline enemy
- Hold position until condition is met
- Prioritize survival
- Play aggressively
- Cast/trigger combo early
- Save certain actions for later

**The exact categories can be finalized later**, but the important design rule is:

**Behavior defines battle decision-making, not the hero's entire build identity.**

---

## Core Combat Identity: Modular Skill/Action System

One of the **central unique features** of the combat is the hero skill/action system.

### Skill Slot Structure

Each hero can be assigned up to **5 skill slots**.

This number may change later, but **5 is the current working assumption**.

Each **skill slot** is itself made of **5 sub-slots**.

These 5 sub-slots are filled by the player with **actions** or **signs**, similar in spirit to hand signs, stances, or combat inputs.

### Example Actions

Examples of sub-slot actions could include:

- Meditate
- Punch
- Kick
- Hand Sign A
- Hand Sign B
- Step Forward
- Defend
- Focus
- Charge
- Elemental Sign

### Action-Chain Resolution

These action chains are then **interpreted by the combat system**.

Certain combinations can produce **real techniques or abilities**.

#### Example Concept

- Slot contains: `Hand Sign A → Hand Sign B → Meditate → Kick → Focus`
- That full sequence may resolve into a specific skill such as:
  - Fire Breath
  - Stone Wall
  - Lightning Kick
  - Clone Strike
  - Wind Dash
  - Barrier Pulse

**This means the actual combat skill system is not just a flat list of skills. Instead, it is based on configured action sequences.**

**This is one of the core identity pillars of the game.**

---

## Skill/Action Design Goals

The skill-slot system should support:

- Hero customization
- Role flexibility
- Build identity
- Varied battle outcomes
- Progression depth
- Combo discovery or planned loadout building

### Hero Identity Through Skills

The design should allow heroes to feel distinct not only because of base stats, but also because of:

- What action chains they are equipped with
- How those chains interact with their proficiencies
- What behavior logic governs when or how they use them

### Proficiency Influence

A hero may be **more proficient with certain actions, elements, or techniques**, but the player should still have room to assign different combinations and experiment.

---

## Hero Customization: Three-Part Identity

This creates a **three-part identity** for each hero:

### 1. Hero Base Identity
- Stats
- Proficiencies
- Progression tendencies

### 2. Skill/Action Loadout
- Up to 5 skill slots
- Each slot containing 5 action sub-slots
- Action combos producing actual combat techniques

### 3. Behavior Logic
- Determines how and when the hero acts during battle

**Together, these define how a hero performs.**

---

## Example Hero Customization Interpretation

### Example 1: Fire-Proficient Hero

A hero could be naturally proficient in:

- Fire techniques
- Mobility
- Melee burst

The player might still configure that hero in different ways:

- **Aggressive assassin** - Burst combo chains
- **Mid-range skirmisher** - Mobility + lightning-based techniques
- **Fighter** - Kick-heavy combo chains with defensive timing

### Example 2: Defense-Proficient Hero

Another hero might naturally lean toward defense and earth-style skills, but the player could build them as:

- Protective tank
- Frontline control unit
- Defensive bruiser with counterattacks

**The goal is that heroes are guided by their identity, but not completely locked by it.**

---

## Combat Execution Idea

Once battle begins, heroes automatically evaluate:

- Current position
- Nearby enemies
- Assigned behavior
- Available skill slots/action chains
- Current opportunity or threat

They then act according to battle logic.

### Combat Feel

The exact low-level system is still to be designed later, but the core idea is that combat should feel like:

- Heroes choosing targets according to logic
- Heroes moving according to their behavior
- Heroes attempting attacks or action-chain-based techniques
- Heroes expressing their assigned role and build through automatic execution

**The player is watching the squad's preparation and identity come to life.**

---

## PvE Mission Structure

The game is intended to be **PvE-first**.

The player sends the squad into missions against NPC enemies.

### Why PvE?

This direction is important because PvE is a strong fit for:

- Tactical experimentation
- Unusual or expressive combat systems
- Progression-based gameplay
- Encounter design
- Less oppressive balance requirements than PvP

### Long-Term Mission Ideas

Long-term mission ideas may include:

- Defeat all enemies
- Survive waves
- Boss encounters
- Protect target
- Clear elite missions
- Farming missions for gear or resources

**For MVP, only the simplest mission type is needed: defeat all enemies.**

---

## Progression

Character progression is intended to be a **major long-term pillar**.

### Potential Progression Systems

Potential progression systems may include:

- Level growth
- Stat increases
- Proficiency specialization
- Passive unlocks
- Skill tree unlocks
- Expanded action/skill options
- Role-defining perks
- Item-based power or utility

### Progression Philosophy

Progression should **not only raise numbers**. It should also expand:

- Tactical options
- Role flexibility
- Action-chain possibilities
- Build identity
- Synergy with behavior logic

**This is important because the heroes should become more expressive over time, not just stronger.**

---

## Items and Future Bonuses

The long-term game is expected to include:

- Items
- Passives
- Progression perks
- Skill trees
- Modifiers
- Buffs/debuffs

### Item Design Goals

These should interact with combat in a way that supports the hero's build and role.

**Example item effects:**

- Improved elemental combo performance
- Bonus damage when using certain action types
- Reduced time or requirement for specific techniques
- Defensive bonuses during certain behaviors
- Synergy with combo chains or role identity

### Technical Requirement

**The technical design must allow these systems to be added later without forcing a full rewrite of combat flow.**

See ARCHITECTURE_OVERVIEW.md and ENGINEERING_RULES.md for implementation details.

---

## MVP Philosophy

The MVP should **not** try to build the full game immediately.

### First Goal: Validate the Concept

The first real goal is to prove the combat idea in a very small, understandable form.

The earliest playable prototype should answer:

**"Is this behavior-driven, auto-resolving, grid-based squad combat fun and readable?"**

### MVP Focus

The MVP should focus on:

- Tiny battlefield
- Tiny unit count
- Very small set of behavior rules
- Basic combat loop
- Placeholder visuals
- Enough of the action/skill concept to prove uniqueness

**The full long-term systems like advanced progression, large content sets, and polished visuals should come later.**

See MVP_SCOPE.md for detailed scope definition.

---

## Earliest Prototype Direction

The first prototype should likely be **extremely small**.

### Possible First Validation Goals

- 2 player heroes
- 2 enemy units
- 1 simple map
- Basic movement
- Target selection
- One or two simple behavior modes
- One basic attack system
- One lightweight version of the action-chain / technique system
- Win/loss state

### Purpose of First Prototype

The purpose of the first prototype is **not content quantity**. It is to validate:

- Readability
- Automatic combat feel
- Hero identity expression
- Whether preparation feels meaningful

---

## Technical Direction

The game is intended to use a **custom combat engine / custom combat logic**, even if some generic supporting systems are reused.

The core combat systems should be custom because they define the game's identity.

### Expected Technical Stack

- Unity
- C#
- Custom gameplay architecture
- AI-assisted development supported by local `.md` design/spec files

### Preferred Architecture Direction

- Data-driven
- Modular
- System-based
- Clean separation of responsibilities
- Battle manager as coordinator, not rule owner

### Core Principles

- Static data separate from runtime state
- Combat logic separate from visuals
- Behavior logic separate from presentation
- Extensibility for future progression/item/passive systems
- Avoid giant god classes
- Avoid hardcoding specific hero exceptions whenever possible

See ARCHITECTURE_OVERVIEW.md and ENGINEERING_RULES.md for details.

---

## Documentation-Driven Development

The project is expected to use a set of `.md` files as reference documents for an AI coding agent inside the local IDE.

### Purpose of Documentation

The purpose of these docs is to:

- Define systems clearly
- Reduce ambiguity
- Keep boundaries between systems strong
- Make AI-generated code more consistent
- Reduce long-term rework

### Documentation Structure

These docs include:

- **Core Docs** (this file, architecture, engineering rules, MVP scope, data models)
- **System Docs** (one file per major gameplay system)

This document is the **top-level summary** that the AI agent should use as a starting point before referencing more granular `.md` files.

---

## Design Strengths

The current concept's strongest qualities are:

- Clear identity beyond "just another TFT clone"
- Strong PvE fit
- Appealing squad-progression structure
- Flexible hero-role philosophy
- Unique modular action-chain / skill-slot system
- Tactical combat without live micro input
- Strong long-term buildcraft potential

---

## Main Design Risks

The main risks are:

- Making the behavior system too complex
- Making combat difficult to read
- Letting action-chain systems become confusing or bloated
- Overwhelming the player with too many setup decisions too early
- Mixing role, behavior, and skill logic too loosely
- Overbuilding before validating the core loop

### Intended Solutions

The intended solution is to aim for:

- Clear role identity
- Manageable behavior rules
- Expressive but readable skill/action systems
- Small-scope MVP validation

---

## Short Pitch Version

**This project is a single-player PvE tactical RPG with grid-based auto-combat. The player controls a squad of heroes, customizes each hero's progression, behavior, and action-chain skill loadout, places them on the battlefield, and watches the encounter resolve automatically. The game's unique identity comes from combining squad progression, flexible hero roles, accessible combat doctrine, and a combo-driven skill-slot system in an anime-inspired ninja fantasy world.**

---

## Related Documentation

For detailed information, see:

- **ARCHITECTURE_OVERVIEW.md** - System architecture and code structure
- **ENGINEERING_RULES.md** - Coding standards and best practices
- **MVP_SCOPE.md** - What's in/out of scope for first demo
- **DATA_MODELS.md** - Core data structures and shared vocabulary
- **System Docs** - Individual system specifications (Grid, Behavior, Skills, etc.)

---

## Version

**Version 1.0** - Initial project overview
