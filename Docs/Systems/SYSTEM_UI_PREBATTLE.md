# SYSTEM_UI_PREBATTLE.md

## Purpose

This document defines the **Pre-Battle UI System**, where players prepare for combat by selecting their squad, configuring hero loadouts, assigning behaviors, and placing units on the grid before battle begins.

**This is where meaningful player control happens** - the pre-battle phase is critical to the game's identity.

---

## Responsibilities

The Pre-Battle UI System is responsible for:

- Squad selection interface
- Hero placement on grid
- Behavior assignment UI
- Skill slot configuration UI
- Action/sign assignment within skill slots
- Loadout validation (ensure heroes are ready)
- Ready check and battle launch

---

## What Pre-Battle UI Does NOT Do

- **Does not execute combat** - That's BattleManager and combat systems
- **Does not handle progression** - That's ProgressionSystem
- **Does not save data** - That's SaveLoadSystem (but UI triggers saves)

**Pre-Battle UI is where the player configures the plan. Combat executes the plan.**

---

## MVP Scope

For MVP, the pre-battle UI should be **functional but minimal**:

- ✅ Select 2 heroes
- ✅ Assign behavior (dropdown/button)
- ✅ Configure 1-2 skill slots with 5 actions each
- ✅ Place heroes on grid
- ✅ Start battle button

**Defer for later:**
- ❌ Polished animations
- ❌ Hero stat comparison
- ❌ Loadout saving/loading
- ❌ Advanced UI features

**Focus: Readable and functional, not beautiful.**

---

## Pre-Battle Flow

```
1. Mission Selection
   ↓
2. Squad Selection (pick 2 heroes)
   ↓
3. Hero Configuration (for each hero):
   ├─ Assign Behavior
   ├─ Configure Skill Slots (actions)
   └─ Review Stats
   ↓
4. Grid Placement (place heroes on battlefield)
   ↓
5. Ready Check (validate configuration)
   ↓
6. Launch Battle
```

---

## UI Screens

### 1. Squad Selection Screen

**Purpose:** Choose which heroes to bring into battle.

**UI Elements:**
- List of available heroes (roster)
- Selected squad slots (e.g., 2 slots for MVP)
- Hero portraits with names
- Basic stats display (HP, Attack, Defense)
- Confirm/Next button

```
╔════════════════════════════════════════════════════════╗
║                  SELECT YOUR SQUAD                     ║
╠════════════════════════════════════════════════════════╣
║                                                        ║
║  Available Heroes:                                     ║
║  ┌────────┐  ┌────────┐  ┌────────┐                  ║
║  │  Kai   │  │  Rin   │  │ Akira  │                  ║
║  │  Lv 1  │  │  Lv 1  │  │  Lv 1  │                  ║
║  │ HP:100 │  │ HP:120 │  │ HP: 90 │                  ║
║  └────────┘  └────────┘  └────────┘                  ║
║      ▲           ○           ○                         ║
║                                                        ║
║  Selected Squad:                                       ║
║  [ Kai ] [ Empty ]                                     ║
║                                                        ║
║                               [Next: Configure Squad]  ║
╚════════════════════════════════════════════════════════╝
```

**Interaction:**
- Click hero to add to squad
- Click selected hero to remove
- Validate: Must have at least 1 hero (MVP: exactly 2)

---

### 2. Hero Configuration Screen

**Purpose:** Configure each hero's behavior and skills before battle.

**UI Elements:**
- Hero portrait and stats
- Behavior dropdown/selector
- Skill slot configuration panel
- Preview of resolved techniques
- Next/Back buttons

```
╔════════════════════════════════════════════════════════╗
║              CONFIGURE: KAI                            ║
╠════════════════════════════════════════════════════════╣
║                                                        ║
║  Behavior: [Aggressive ▼]                             ║
║                                                        ║
║  ───────────────────────────────────────────────────  ║
║                                                        ║
║  Skill Slot 1:                                         ║
║  ┌─────┐ ┌─────┐ ┌─────┐ ┌─────┐ ┌─────┐            ║
║  │Punch│ │Punch│ │ Kick│ │Strike│ │Focus│            ║
║  └─────┘ └─────┘ └─────┘ └─────┘ └─────┘            ║
║                                                        ║
║  → Resolves to: Triple Strike (Power: 65)             ║
║                                                        ║
║  ───────────────────────────────────────────────────  ║
║                                                        ║
║  Skill Slot 2:                                         ║
║  ┌─────┐ ┌─────┐ ┌─────┐ ┌─────┐ ┌─────┐            ║
║  │Fire │ │Fire │ │Focus│ │Medi-│ │Focus│            ║
║  │Sig A│ │Sig B│ │     │ │tate │ │     │            ║
║  └─────┘ └─────┘ └─────┘ └─────┘ └─────┘            ║
║                                                        ║
║  → Resolves to: Fire Breath (Power: 130) ← +30% Fire! ║
║                                                        ║
║  [Back]                              [Next: Placement]║
╚════════════════════════════════════════════════════════╝
```

**Interaction:**
- Behavior: Dropdown or button group (Aggressive, Defensive, Balanced)
- Skill slots: Click slot to open action picker
- Action picker: Grid of available actions with icons
- Real-time preview of resolved technique

---

### 3. Action Picker Modal

**Purpose:** Select actions to fill a skill slot.

**UI Elements:**
- Grid of available actions (filtered by unlocked actions)
- Action icon, name, and type
- Current selection preview
- Confirm/Cancel buttons

```
╔════════════════════════════════════════════════════════╗
║          SELECT ACTIONS FOR SKILL SLOT 1               ║
╠════════════════════════════════════════════════════════╣
║                                                        ║
║  Available Actions:                                    ║
║                                                        ║
║  ┌──────┐ ┌──────┐ ┌──────┐ ┌──────┐ ┌──────┐       ║
║  │Punch │ │ Kick │ │Strike│ │Charge│ │Fire  │       ║
║  │      │ │      │ │      │ │      │ │Sig A │       ║
║  └──────┘ └──────┘ └──────┘ └──────┘ └──────┘       ║
║                                                        ║
║  ┌──────┐ ┌──────┐ ┌──────┐ ┌──────┐                ║
║  │Fire  │ │Light-│ │Medi- │ │Focus │                ║
║  │Sig B │ │Sig A │ │tate  │ │      │                ║
║  └──────┘ └──────┘ └──────┘ └──────┘                ║
║                                                        ║
║  ───────────────────────────────────────────────────  ║
║                                                        ║
║  Current Sequence:                                     ║
║  [Punch] [Kick] [Strike] [Empty] [Empty]              ║
║                                                        ║
║  [Cancel]                                 [Confirm]    ║
╚════════════════════════════════════════════════════════╝
```

**Interaction:**
- Click action to assign to next empty slot
- Click filled slot to remove action
- Must fill all 5 slots before confirming

---

### 4. Grid Placement Screen

**Purpose:** Place selected heroes on the battlefield before combat starts.

**UI Elements:**
- Battle grid (6x4 or 8x5)
- Hero portraits (draggable)
- Valid placement zones (highlighted)
- Confirm/Start Battle button

```
╔════════════════════════════════════════════════════════╗
║             PLACE YOUR SQUAD                           ║
╠════════════════════════════════════════════════════════╣
║                                                        ║
║  Drag heroes onto the grid:                            ║
║                                                        ║
║  ┌───┬───┬───┬───┬───┬───┐                           ║
║  │ K │   │   │   │   │   │  Player Side              ║
║  ├───┼───┼───┼───┼───┼───┤                           ║
║  │   │   │   │   │   │   │                           ║
║  ├───┼───┼───┼───┼───┼───┤                           ║
║  │   │   │   │   │   │   │                           ║
║  ├───┼───┼───┼───┼───┼───┤                           ║
║  │   │   │   │   │ E │ E │  Enemy Side               ║
║  └───┴───┴───┴───┴───┴───┘                           ║
║                                                        ║
║  K = Kai    E = Enemy                                  ║
║                                                        ║
║  Heroes to Place: [Rin]                                ║
║                                                        ║
║  [Back]                              [START BATTLE!]   ║
╚════════════════════════════════════════════════════════╝
```

**Interaction:**
- Drag heroes from "to place" list onto grid
- Valid tiles are highlighted (e.g., player's half of the grid)
- Can't place multiple units on same tile
- Must place all heroes before starting battle

---

### 5. Ready Check / Validation

**Purpose:** Ensure configuration is valid before launching battle.

**Validation checks:**
- ✅ All heroes have assigned behavior
- ✅ All heroes have at least 1 configured skill slot
- ✅ All heroes are placed on grid
- ✅ No overlapping placements

**If validation fails:**
- Display error message
- Highlight missing configuration
- Disable "Start Battle" button

```
╔════════════════════════════════════════════════════════╗
║                 READY TO BATTLE?                       ║
╠════════════════════════════════════════════════════════╣
║                                                        ║
║  Squad:          [✓] 2 heroes selected                 ║
║  Behaviors:      [✓] All assigned                      ║
║  Skills:         [✓] All configured                    ║
║  Placement:      [✓] All placed                        ║
║                                                        ║
║  [Back to Configuration]      [LAUNCH BATTLE]          ║
╚════════════════════════════════════════════════════════╝
```

---

## Technical Implementation

### Pre-Battle Manager

```csharp
public class PreBattleManager : MonoBehaviour
{
    public List<UnitDefinition> availableHeroes;
    public List<UnitDefinition> selectedSquad = new List<UnitDefinition>();

    public Dictionary<UnitDefinition, BehaviorType> assignedBehaviors;
    public Dictionary<UnitDefinition, List<SkillSlot>> assignedSkills;
    public Dictionary<UnitDefinition, GridPosition> placements;

    public void SelectHero(UnitDefinition hero)
    {
        if (selectedSquad.Count < 2) // MVP: 2 heroes
        {
            selectedSquad.Add(hero);
        }
    }

    public void AssignBehavior(UnitDefinition hero, BehaviorType behavior)
    {
        assignedBehaviors[hero] = behavior;
    }

    public void ConfigureSkillSlot(UnitDefinition hero, int slotIndex, List<ActionDefinition> actions)
    {
        // Create skill slot from actions
        var slot = new SkillSlot
        {
            slotIndex = slotIndex,
            actionSequence = actions.Select((a, i) => new ActionSlot
            {
                subSlotIndex = i,
                action = a
            }).ToList()
        };

        if (!assignedSkills.ContainsKey(hero))
        {
            assignedSkills[hero] = new List<SkillSlot>();
        }

        assignedSkills[hero].Add(slot);
    }

    public void PlaceHero(UnitDefinition hero, GridPosition position)
    {
        placements[hero] = position;
    }

    public bool ValidateConfiguration()
    {
        // Check all heroes have behavior
        foreach (var hero in selectedSquad)
        {
            if (!assignedBehaviors.ContainsKey(hero))
                return false;

            if (!assignedSkills.ContainsKey(hero) || assignedSkills[hero].Count == 0)
                return false;

            if (!placements.ContainsKey(hero))
                return false;
        }

        return true;
    }

    public void LaunchBattle()
    {
        if (!ValidateConfiguration())
        {
            Debug.LogError("Configuration incomplete!");
            return;
        }

        // Create battle state
        var battleState = battleManager.InitializeBattle(
            selectedSquad,
            enemyDefinitions,
            gridWidth: 8,
            gridHeight: 6
        );

        // Apply configurations
        foreach (var hero in selectedSquad)
        {
            var unit = battleState.playerUnits.Find(u => u.definition == hero);

            unit.behavior = new BehaviorLoadout { behaviorType = assignedBehaviors[hero] };
            unit.equippedSkills = assignedSkills[hero];
            unit.position = placements[hero];

            battleState.grid.SetOccupied(unit.position, unit);
        }

        // Start battle
        battleManager.StartBattle(battleState);
    }
}
```

---

## UI Components (Unity)

### Squad Selection Panel

```csharp
public class SquadSelectionPanel : MonoBehaviour
{
    public Transform heroListContainer;
    public HeroCardUI heroCardPrefab;
    public List<SelectedHeroSlot> squadSlots;

    public void Initialize(List<UnitDefinition> availableHeroes)
    {
        foreach (var hero in availableHeroes)
        {
            var card = Instantiate(heroCardPrefab, heroListContainer);
            card.Setup(hero);
            card.OnClicked += () => SelectHero(hero);
        }
    }

    private void SelectHero(UnitDefinition hero)
    {
        preBattleManager.SelectHero(hero);
        RefreshSquadSlots();
    }
}
```

### Skill Configuration Panel

```csharp
public class SkillConfigPanel : MonoBehaviour
{
    public SkillSlotUI[] skillSlotUIs; // 5 slots (MVP: 2)

    public void Initialize(UnitDefinition hero)
    {
        for (int i = 0; i < skillSlotUIs.Length; i++)
        {
            int slotIndex = i;
            skillSlotUIs[i].OnClicked += () => OpenActionPicker(hero, slotIndex);
        }
    }

    private void OpenActionPicker(UnitDefinition hero, int slotIndex)
    {
        actionPickerModal.Show(hero, slotIndex, availableActions);
    }
}
```

### Grid Placement UI

```csharp
public class GridPlacementUI : MonoBehaviour
{
    public GridTileUI[,] gridTiles;

    public void Initialize(GridMap grid)
    {
        for (int x = 0; x < grid.width; x++)
        {
            for (int y = 0; y < grid.height; y++)
            {
                var tileUI = gridTiles[x, y];
                var pos = new GridPosition(x, y);

                tileUI.OnDrop += (hero) => PlaceHero(hero, pos);
            }
        }
    }

    private void PlaceHero(UnitDefinition hero, GridPosition pos)
    {
        if (IsValidPlacement(pos))
        {
            preBattleManager.PlaceHero(hero, pos);
            RefreshGrid();
        }
    }

    private bool IsValidPlacement(GridPosition pos)
    {
        // Player can only place on their half (e.g., Y < 3)
        return pos.y < 3;
    }
}
```

---

## Preview and Feedback

### Technique Preview

Show what technique will resolve from action sequence:

```csharp
public void UpdateTechniquePreview(List<ActionDefinition> actions, UnitDefinition hero)
{
    var tempSkill = new SkillSlot
    {
        actionSequence = actions.Select((a, i) => new ActionSlot
        {
            subSlotIndex = i,
            action = a
        }).ToList()
    };

    var tempUnit = CreateTempUnit(hero);
    var technique = skillSystem.ResolveSequence(tempUnit, tempSkill);

    previewLabel.text = $"→ {technique.techniqueName} (Power: {technique.power})";

    if (technique.element != ElementType.None)
    {
        float proficiency = hero.proficiencies.GetProficiencyBonus(technique.element);
        if (proficiency > 1.0f)
        {
            proficiencyLabel.text = $"+{(int)((proficiency - 1.0f) * 100)}% {technique.element}!";
        }
    }
}
```

---

## MVP UI Recommendations

### Keep It Simple

- **Minimal animations** - Fade in/out, simple transitions
- **Clear labels** - Use text, not just icons
- **Large touch targets** - Buttons should be easy to click
- **Color coding** - Buffs green, debuffs red, neutral gray
- **Immediate feedback** - Click = instant response

### Placeholder Art

- Use colored boxes for heroes
- Use text labels for actions (icons later)
- Simple grid lines for battlefield
- Basic panels with borders

### Focus on Functionality

**Priority 1:** All features work correctly
**Priority 2:** Readability and clarity
**Priority 3 (later):** Visual polish

---

## Summary

The Pre-Battle UI is where **player control happens**:

- ✅ Squad selection (pick heroes)
- ✅ Behavior assignment (how they act)
- ✅ Skill configuration (action-chains)
- ✅ Grid placement (positioning)
- ✅ Validation and launch
- ✅ **MVP: Functional, minimal polish**
- ✅ Real-time technique preview
- ✅ Clear feedback and validation

**Pre-battle is where preparation becomes strategy.**

---

## Related Documentation

- **PROJECT_OVERVIEW.md** - Pre-battle control as core identity
- **SYSTEM_BEHAVIOR.md** - Behavior types player assigns
- **SYSTEM_SKILLS.md** - Skill configuration structure
- **SYSTEM_GRID.md** - Grid placement rules
- **MVP_SCOPE.md** - Minimal UI for MVP

---

## Version

**Version 1.0** - Pre-battle UI system for MVP
