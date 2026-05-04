# Hero Configuration Menu — Design Document

## Overview

A dedicated **pre-battle scene** where the player configures each hero's skill loadout before entering combat. The configuration is carried into battle via a static data store that survives scene transitions.

---

## Layout

```
┌──────────────────────────────────────────────────────────────────────┐
│                                                          [START BATTLE] │
│  HERO SELECTOR (top-left, circular avatars)                          │
│  ┌───┐ ┌───┐ ┌───┐                                                  │
│  │KAI│ │MIR│ │ + │   ← click to switch hero, active = highlighted   │
│  └───┘ └───┘ └───┘                                                  │
│                                                                      │
├──────────────┬───────────────────────────────────────────────────────┤
│              │                                                       │
│              │  SKILL SLOTS (5 rows, right side)                     │
│              │                                                       │
│   HERO       │  Slot 1: [⬡][⬡][⬡][⬡][⬡] → "Fire Strike" (ATK)    │
│   MODEL      │  Slot 2: [⬡][⬡][⬡][ ][ ] → "Earth Summoning" (SUM) │
│   (3D        │  Slot 3: [ ][ ][ ][ ][ ]   (empty)                   │
│   capsule,   │  Slot 4: [ ][ ][ ][ ][ ]   (empty)                   │
│   idle        │  Slot 5: [ ][ ][ ][ ][ ]   (empty)                   │
│   anim)      │                                                       │
│              │  Each [⬡] is a circular action slot.                  │
│              │  Click a slot → it highlights → click an action       │
│              │  from the pool below to assign it.                    │
│              │  Right-click a filled slot to clear it.               │
│              │                                                       │
├──────────────┤  AVAILABLE ACTIONS (bottom-right grid)                │
│              │  ┌────────────────────────────────────────────┐       │
│  STATS       │  │ [Punch]  [Kick]   [HandSignA] [HandSignB] │       │
│  ──────      │  │ [HandSignC] [Focus] [Meditate]             │       │
│  HP:  100    │  └────────────────────────────────────────────┘       │
│  ATK:  12    │                                                       │
│  DEF:   8    │  Each action shows: icon + name + type tag            │
│  SPD: 3.0    │  (Physical / Elemental:Fire / Support etc.)           │
│              │                                                       │
│  (future:    │                                                       │
│   equipment  │                                                       │
│   bonuses)   │                                                       │
│              │                                                       │
└──────────────┴───────────────────────────────────────────────────────┘
```

---

## Hero Selector — Top Left

- Row of **circular avatar buttons** in the top-left corner
- Each circle shows the hero's portrait (or a colored initial for now: "K", "M")
- **Active hero** = larger circle with a glowing border
- **Click** a circle → switches the entire page to that hero's config
- **"+"** button at the end = placeholder for future "add hero to roster"
- Heroes are loaded from the available `UnitDefinition` assets

---

## Hero Preview — Left Side

- 3D capsule (or future model) standing on a small platform
- Slowly rotates or plays idle animation
- Color matches the hero's team color
- Shows the hero's **display name** above the model
- Future: swap model based on equipment

---

## Stats Panel — Bottom Left

Displays the hero's `StatBlock` values:

```
HP:  100        (from UnitDefinition.baseStats.maxHP)
ATK:  12        (baseStats.attack)
DEF:   8        (baseStats.defense)
SPD: 3.0        (baseStats.moveSpeed)
```

Future additions (greyed out or hidden for now):
- Equipment bonuses: `ATK: 12 (+3 from weapon)`
- Passive ability effects
- Proficiency levels per element

---

## Skill Slots — Right Side

### Structure

5 skill slots displayed vertically. Each slot has:
- **Slot label** on the left: `Slot 1`, `Slot 2`, etc.
- **5 circular action slots** in a horizontal row (the action chain)
- **Technique preview** on the right: resolved name + type

```
Slot 1: [Punch][Kick][ ][ ][ ]  →  "Non-Elemental Strike" (Attack, Pwr: 25)
Slot 2: [HSC][HSC][Focus][ ][ ] →  "Earth Summoning" (Summon, Pwr: 26)
Slot 3: [ ][ ][ ][ ][ ]          →  (empty)
Slot 4: [ ][ ][ ][ ][ ]          →  (empty)
Slot 5: [ ][ ][ ][ ][ ]          →  (empty)
```

### Interaction

1. **Click** an empty action slot → it highlights (selected)
2. **Click** an action from the Available Actions pool → fills the selected slot
3. Actions fill **left-to-right** (no gaps)
4. **Right-click** a filled slot → clears it (and shifts remaining actions left)
5. **Technique preview updates live** — uses `SkillSystem.ResolveSkill()` to show what the combo resolves to in real-time
6. If only 1-2 actions are filled, the remaining slots stay empty — that's a valid short combo

### Slot-to-Battle Mapping

Each filled skill slot becomes one `SkillSlot` in the hero's `equippedSkills` list:

```csharp
// Example: hero with 2 skills configured
unit.equippedSkills[0] = SkillSlot { Punch, Kick }         // Slot 1
unit.equippedSkills[1] = SkillSlot { HandSignC, HandSignC, Focus }  // Slot 2
// Slots 3-5 empty → not added
```

How the AI uses multiple skills in battle is covered in the Battle Design doc.

---

## Available Actions Pool — Bottom Right

- Grid of all `ActionDefinition` assets the player has unlocked
- Each action displays:
  - **Icon** (or colored placeholder square based on type)
  - **Name**: "Punch", "Hand Sign C", etc.
  - **Type tag**: colored label — Physical (grey), Elemental (orange/red/blue), Support (green)
- Actions can be used **multiple times** across different slots (HandSignC + HandSignC is valid)
- Hover/tooltip (future): shows `basePower`, `element`, `description`

### Action Discovery

For now, all actions are available to all heroes. Future hooks:
- Per-hero action unlocks (leveling, story progression)
- Rare/legendary actions found as loot
- Element affinity — heroes are better with certain action types

---

## Data Flow: Menu → Battle

### Static Data Store

```csharp
// Survives scene transitions (static class, not MonoBehaviour)
public static class HeroLoadoutData
{
    // Key = UnitDefinition asset name, Value = list of configured SkillSlots
    public static Dictionary<string, List<SkillSlot>> Loadouts;
    
    // Ordered list of heroes selected for battle
    public static List<UnitDefinition> SelectedHeroes;
}
```

### Flow

```
1. HeroConfigScene loads
2. Player configures skills for each hero
3. Player clicks [START BATTLE]
4. HeroLoadoutData stores all loadouts + selected heroes
5. SceneManager loads BattleScene
6. TerrainBattleManager reads HeroLoadoutData instead of Inspector fields
7. Units spawn with the configured skills
```

---

## Start Battle Button

- Top-right corner, large and prominent
- **Disabled** if no heroes are selected or no skills configured
- **Click** → saves loadouts to `HeroLoadoutData` → loads battle scene
- Future: show a battle preview (map selection, enemy wave info)

---

## Scene Structure

```
HeroConfigScene (new)
├── Main Camera (fixed angle, looking at hero preview area)
├── Directional Light
├── HeroConfigUI (Canvas)
│   ├── HeroSelector (top-left, horizontal layout of circle buttons)
│   ├── HeroPreviewArea (left, 3D capsule on a platform)
│   ├── StatsPanel (bottom-left)
│   ├── SkillSlotsPanel (right, 5 rows of 5 circles each)
│   ├── ActionPoolPanel (bottom-right, grid of available actions)
│   └── StartBattleButton (top-right)
├── HeroConfigManager (script: loads data, handles UI logic)
└── PreviewPlatform (small 3D plane for the hero to stand on)
```

---

## Files to Create

```
Assets/Scripts/UI/
├── HeroConfigManager.cs     — Main controller for the config scene
├── HeroLoadoutData.cs       — Static data store (Menu → Battle bridge)
├── SkillSlotUI.cs           — UI for one skill slot row (5 action circles + preview)
├── ActionSlotUI.cs          — UI for one circular action slot (click to select)
└── ActionPoolUI.cs          — Grid of available actions to pick from
```

---

## Implementation Order

1. **HeroLoadoutData.cs** — Static data class, no UI dependency
2. **HeroConfigManager.cs** — Scene controller, loads heroes + actions
3. **SkillSlotUI + ActionSlotUI** — Interactive skill configuration
4. **ActionPoolUI** — Available actions grid
5. **Update TerrainBattleManager** — Read from HeroLoadoutData instead of Inspector
6. **Hero preview + stats panel** — Visual polish
7. **Scene wiring** — Canvas layout, camera, transitions

---

## Future Enhancements

- **Equipment system**: weapon/armor slots that modify stats
- **Passive abilities**: unlockable perks per hero
- **Hero leveling**: XP from battles, unlock new action slots
- **Action crafting**: combine actions to create new ones
- **Save/Load**: persist loadouts to JSON files
- **Drag-and-drop**: drag actions into slots instead of click-click
- **Undo**: ctrl+Z to revert last action assignment
