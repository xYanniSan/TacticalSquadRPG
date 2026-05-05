# 01_VISION.md

> **Tier 2 — Stable.** Describes what the game is trying to be. Do not modify without explicit user instruction.

---

## One-sentence pitch

A single-player PvE tactical squad RPG with **3D auto-resolving combat** in an anime-inspired ninja/martial-fantasy world, where players build heroes, configure their combat behavior and an action-chain skill loadout, then watch the battle play out automatically.

---

## Core player fantasy

> **"I built these heroes, I decided how they fight, and now I get to watch my strategy play out."**

The game is **not** about live mechanical execution. The player's agency lives in:

- Squad composition
- Hero progression
- Behavior assignment
- **Action-chain skill loadout** (the signature mechanic)
- Pre-battle preparation and iteration

Once combat starts, heroes act autonomously. The player's job is to design the plan; combat reveals whether the plan was good.

The intended emotional rhythm is: **Prepare → Execute → Observe → Improve → Progress.**

---

## Identity pillars

These are the things that make this game distinct. Other systems serve these.

### 1. Action-chain skill system

Every skill slot is a **sequence of actions** rather than a single fixed ability. Players combine basic actions (punches, kicks, hand signs, focus) into chains. The first three actions of a chain can match a **combo recipe** to trigger a named skill (Earth Fist, Triple Sign, Summoning, etc.); remaining actions in the chain layer their own bonuses on top.

This is the game's most identity-defining mechanic. It is **not** a peripheral system — it is what differentiates the game from Teamfight Tactics, Gladiabots, and other auto-battlers.

See `05_SKILL_SYSTEM.md` for the full design.

### 2. Pre-battle preparation, post-battle iteration

The meaningful decisions happen *before* combat. The post-combat experience is evaluation: what worked, what didn't, what loadout to try next. The game lives in this loop.

### 3. 3D real-time auto-resolving combat

Combat plays out in 3D space with characters running, dodging, casting, and clashing. The camera is angled and tactical (TFT-style readability) but the combat itself is fluid 3D — not abstract grid movement. Visual readability matters more than cinematic realism, but combat should still feel kinetic and anime-flavored.

See `04_BATTLE_SYSTEM.md` for the live combat design and `07_PRESENTATION.md` for the visual feel goals.

### 4. Flexible hero roles

Heroes have **proficiencies and natural strengths** but are not hard-locked to a single role. A hero with strong Earth proficiency *tends* toward elemental builds, but the player can equip them with physical action-chains and an aggressive behavior and play them as a melee bruiser instead.

**Role** = the combat identity the hero fills (tank, assassin, support, etc.).
**Behavior** = the AI logic that decides what the hero does in battle (aggressive, defensive, balanced).

These are different concepts. A hero might be built as a *fighter* (role) but configured to behave *defensively* (behavior). Both are player choices, expressed through different systems.

### 5. PvE-first

Single-player, mission-based, against NPC enemies. Long-term mission types include: defeat all enemies, survive waves, boss encounters, protect target, farming missions. PvP is not a current goal and is unlikely to become one.

PvE supports the design's strengths — tactical experimentation, expressive systems, progression-driven gameplay — without the balancing constraints PvP would impose.

---

## Squad and hero direction

- Squads of up to **5 heroes** (current target; may evolve).
- Each hero has identity, stats, proficiencies, equipped skills, assigned behavior.
- Progression expands tactical options, not just numbers — new actions, new passives, new build directions, not just bigger HP bars.
- Item and equipment systems are planned long-term (see `08_ROADMAP.md`).

---

## World and tone

- Anime-inspired ninja / martial-arts fantasy.
- Hand-sign-style elemental sequences, flashy techniques, supernatural combat.
- Original IP — inspired in tone, not derivative. **No copyrighted characters, names, clans, moves, symbols, or lore.**
- Visual feel: Naruto Storm, Dragon Ball FighterZ, Demon Slayer, Jujutsu Kaisen as reference points for combat *feel*, not aesthetic copy.

---

## Platform and timeline

- **Engine:** Unity 6 (URP)
- **Language:** C#
- **Platform:** PC, Steam-targeted, primarily offline/local
- **Multiplayer:** Not planned
- **Development horizon:** Multi-year project, designed for incremental growth

The architecture must support 3+ years of development without catastrophic rewrites. See `02_ARCHITECTURE.md`.

---

## Design risks (named so they're avoided)

These are the failure modes most likely to kill the design:

- **Behavior system becoming a programming simulator.** The pre-battle UI must feel like setting doctrine, not writing scripts.
- **Combat becoming unreadable.** With no direct control during battle, the player must understand what's happening at a glance. If players can't tell why a hero died, the system has failed.
- **Action-chain system becoming bloated or confusing.** Combo recipes must remain learnable. Adding combos for the sake of variety without clear thematic identity hurts the game.
- **Mixing role, behavior, and skill logic too loosely.** These must stay conceptually separate even when they interact.
- **Mixing combat truth and animation timing.** Animation reflects combat state; it does not define it. See `07_PRESENTATION.md`.

---

## What this game is *not*

- Not a fighting game (no live combo input)
- Not Teamfight Tactics (no shared pool, no rounds, no traits-as-identity)
- Not Final Fantasy Tactics (not turn-based, not grid-locked)
- Not a deus ex machina simulator like a coding-bot game (behavior is selectable presets, not authored scripts)
- Not a multiplayer game

If a design suggestion would push the game toward any of these, push back.

---

## Short pitch

A single-player PvE tactical RPG with 3D auto-combat. The player controls a squad of heroes, customizes each hero's progression, behavior, and action-chain skill loadout, and watches each battle resolve automatically. The game's unique identity comes from combining squad progression, flexible hero roles, accessible combat doctrine, and a combo-driven action-chain skill system in an anime-inspired ninja fantasy world.
