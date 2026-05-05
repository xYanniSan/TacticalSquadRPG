# 06_HERO_CONFIG.md

> **Tier 1 — Living.** Update this doc in the same task as any change to the hero configuration scene or the loadout bridge.

---

## Purpose

The Hero Configuration scene (`HeroConfigScene`) is where the player prepares heroes before each battle: selecting which heroes to deploy, configuring each hero's skill loadout, and launching the battle.

This is **already implemented**. This doc captures the current design and the contract between the config scene and the battle scene.

For data structure details see `03_DATA_MODELS.md`. For the skill model see `05_SKILL_SYSTEM.md`.

---

## Player flow

```
1. HeroConfigScene loads
2. Player selects active hero from the top-left selector
3. Player drags/clicks actions from the action pool into skill slots
4. Skill slot preview updates live (resolved technique name + type)
5. Player switches between heroes, configures each one
6. Player clicks [START BATTLE]
7. HeroLoadoutData is populated
8. SceneManager loads TerrainBattleScene
9. TerrainBattleManager reads HeroLoadoutData and spawns units
```

---

## Layout

```
┌──────────────────────────────────────────────────────────────────────┐
│                                                          [START BATTLE]│
│  HERO SELECTOR (top-left, circular avatars)                          │
│  ┌───┐ ┌───┐ ┌───┐                                                   │
│  │KAI│ │MIR│ │ + │   Click to switch hero, active = highlighted     │
│  └───┘ └───┘ └───┘                                                   │
│                                                                      │
├──────────────┬───────────────────────────────────────────────────────┤
│              │                                                       │
│              │  SKILL SLOTS (5 rows)                                 │
│              │                                                       │
│   HERO       │  Slot 1: [⬡][⬡][⬡][⬡][⬡] → Resolved technique      │
│   PREVIEW    │  Slot 2: [⬡][⬡][⬡][ ][ ] → Resolved technique      │
│   (3D model, │  Slot 3: [ ][ ][ ][ ][ ]                              │
│    idle      │  Slot 4: [ ][ ][ ][ ][ ]                              │
│    anim,     │  Slot 5: [ ][ ][ ][ ][ ]                              │
│    rotates)  │                                                       │
│              │  Click empty slot → highlights                        │
│              │  Click action in pool → fills selected slot           │
│              │  Right-click filled slot → clears + shifts left       │
│              │  Live preview = SkillSystem.ResolveSkill output       │
│              │                                                       │
├──────────────┤  AVAILABLE ACTIONS POOL (bottom-right grid)           │
│              │  ┌────────────────────────────────────────────┐       │
│  STATS PANEL │  │ [Punch] [Kick] [Sign A] [Sign B]          │       │
│  HP:    100  │  │ [Sign C] [Focus]                           │       │
│  ATK:    12  │  └────────────────────────────────────────────┘       │
│  DEF:     8  │                                                       │
│  SPD:   3.0  │  Each action: icon + name + type tag                 │
│              │  Type tags: Physical (grey), Elemental (colored),    │
│              │  Support (green)                                     │
└──────────────┴───────────────────────────────────────────────────────┘
```

---

## UI components

All under `Assets/Scripts/UI/`.

### `HeroConfigManager`

Main scene controller. Responsibilities:

- Load all `UnitDefinition` assets and populate the hero selector
- Load all `ActionDefinition` assets and populate the action pool
- Track the currently active hero
- Forward UI events to the right slot
- Save loadouts to `HeroLoadoutData` on Start Battle
- Trigger scene load to `TerrainBattleScene`

### `HeroLoadoutData` (static bridge)

Static class — survives scene transitions because it isn't a MonoBehaviour.

```csharp
public static class HeroLoadoutData
{
    // Loadouts per hero, keyed by UnitDefinition asset name
    public static Dictionary<string, List<SkillSlot>> Loadouts;

    // Ordered list of heroes selected for battle
    public static List<UnitDefinition> SelectedHeroes;
}
```

This is the contract between the config scene and the battle scene. **`TerrainBattleManager` reads from here** at battle start instead of using its own Inspector-configured hero list.

### `SkillSlotUI`

Renders one row of 5 action sub-slots plus the resolved-technique preview. Owns:

- Hover highlight
- Click-to-select state
- Live call to `SkillSystem.ResolveSkill` when the slot's contents change

### `ActionSlotUI`

One circular sub-slot. Holds either an empty placeholder or a reference to an `ActionDefinition`. Click selects this slot for the next action assignment; right-click clears.

### `ActionPoolUI`

Grid of all available `ActionDefinition` assets. Click-to-assign sends the selected action to the currently selected `ActionSlotUI`.

---

## Interaction rules

- **Actions fill left-to-right with no gaps.** Clearing a middle slot shifts later slots left.
- **The same action can appear multiple times.** `Hand Sign A → Hand Sign A → Punch` is valid.
- **Short chains are valid.** A 2-action skill (`A → B`) is fine; remaining slots stay empty.
- **Live preview** uses `SkillSystem.ResolveSkill` and shows the resolved technique name and type. Empty slots show "(empty)".
- **Heroes with no configured skills cannot start battle.** [START BATTLE] is disabled until at least one hero has at least one filled skill slot.

---

## Data flow into battle

```
HeroConfigScene populates:
  HeroLoadoutData.SelectedHeroes  = [UnitDefinition_Kai, UnitDefinition_Mira, ...]
  HeroLoadoutData.Loadouts        = {
      "hero_kai": [
          SkillSlot { Punch, Kick },
          SkillSlot { HandSignC, HandSignC, Focus },
          ... (empty slots not stored)
      ],
      "hero_mira": [...]
  }

SceneManager.LoadScene("TerrainBattleScene")

TerrainBattleManager.Start():
  for each hero in HeroLoadoutData.SelectedHeroes:
      Read UnitDefinition and matching loadout
      Create UnitRuntime, copy baseStats, assign equippedSkills
      Spawn visualPrefab as TerrainBattleUnit
```

If `HeroLoadoutData.SelectedHeroes` is empty (e.g., scene was loaded directly for testing), `TerrainBattleManager` falls back to its Inspector-configured player list.

---

## Stats panel

Displays the active hero's `UnitDefinition.baseStats`:

```
HP:  100        (baseStats.maxHP)
ATK:  12        (baseStats.attack)
DEF:   8        (baseStats.defense)
SPD: 3.0        (baseStats.moveSpeed)
```

Future: equipment bonuses, passive effects, proficiency display.

---

## Hero preview

A 3D capsule (or hero model when available) on a small platform, slowly rotating with idle animation. Color matches team color. Hero display name shown above.

When equipment systems come online, the preview should reflect equipped gear visually.

---

## Action discovery

Currently, all `ActionDefinition` assets are visible in the pool for every hero — no per-hero unlocks.

Future hooks (planned, not implemented):

- Per-hero action unlocks gated by progression
- Element affinity highlighting (showing which actions a hero is naturally good with)
- Rare/legendary actions found as loot

---

## Adding new UI to this scene

The same architectural rules apply as for combat (`02_ARCHITECTURE.md`):

- Each UI concern is its own component
- `HeroConfigManager` coordinates; it does not own click handlers for individual sub-slots
- Avoid `FindObjectOfType` for runtime references; wire dependencies in `Start()`

When the hero config scene grows (equipment slots, passive selection, behavior assignment UI), add new components rather than expanding `HeroConfigManager`.

---

## Future expansions

These are planned but not implemented:

- **Behavior assignment UI** — currently behavior is set on `UnitDefinition.defaultBehavior`. Future: per-hero per-battle behavior dropdown in the config scene.
- **Equipment slots** — weapon, armor, accessory slots that modify stats and unlock action variants.
- **Skill loadout presets** — save and load named skill configurations.
- **Drag-and-drop** — currently click-to-assign; drag-and-drop is a UX upgrade target.
- **Proficiency display** — show element/action proficiencies the hero is best with.
- **Squad position assignment** — placement order on the battlefield (frontline priority).

When any of these come online, update this doc and add the corresponding section.
