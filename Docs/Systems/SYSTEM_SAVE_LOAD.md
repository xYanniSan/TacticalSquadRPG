# SYSTEM_SAVE_LOAD.md

## Purpose

This document defines the **Save/Load System**, which handles persistent player data including hero progression, unlocked content, squad configurations, and skill loadouts.

---

## MVP Scope

**Save/Load is OPTIONAL for MVP.**

MVP can use:
- **Option 1:** No persistence (reset on restart)
- **Option 2:** Simple save/load (save skill configurations only)

**Recommended for MVP:** No persistence initially, add simple save later if needed.

**This document describes the full save/load system for future implementation.**

---

## Responsibilities

The Save/Load System is responsible for:

- Saving player progress persistently
- Loading player data on game start
- Handling hero progression data
- Saving squad configurations
- Saving skill/action loadouts
- Saving unlocked content (heroes, actions, passives)
- Auto-save and manual save
- Save file versioning for updates

---

## What Save/Load System Does NOT Do

- **Does not handle battle state** - Battle state is temporary, not saved
- **Does not handle presentation** - UI shows save data, but system doesn't render
- **Does not validate game balance** - Only stores data

---

## Save Data Structure

### Master Save File

```csharp
[Serializable]
public class GameSaveData
{
    public int saveVersion;            // For handling updates
    public DateTime lastSaved;

    // Player meta
    public int playerLevel;
    public int totalBattlesWon;
    public int totalBattlesPlayed;

    // Resources (future)
    public int gold;
    public int skillPoints;

    // Heroes
    public List<HeroSaveData> ownedHeroes;

    // Unlocks
    public List<string> unlockedActions;
    public List<string> unlockedMissions;

    // Squad
    public SquadConfiguration currentSquad;
}
```

### Hero Save Data

```csharp
[Serializable]
public class HeroSaveData
{
    public string heroId;              // Reference to UnitDefinition
    public int level;
    public int currentXP;

    // Stats
    public SerializedStatBlock bonusStats;

    // Proficiencies
    public SerializedProficiencySet proficiencies;

    // Unlocks
    public List<string> unlockedPassives;
    public List<string> unlockedSkillTreeNodes;

    // Loadouts
    public BehaviorType assignedBehavior;
    public List<SkillSlotSaveData> equippedSkills;
}
```

### Skill Slot Save Data

```csharp
[Serializable]
public class SkillSlotSaveData
{
    public int slotIndex;
    public List<string> actionIds;     // 5 action IDs

    public string slotName;            // Player-assigned name
}
```

---

## Serialization Strategy

### Option 1: JSON (Recommended)

**Pros:**
- Human-readable
- Easy to debug
- Cross-platform
- Supports updates/versioning

**Cons:**
- Larger file size
- Slightly slower

```csharp
public void SaveGame(GameSaveData data, string filePath)
{
    string json = JsonUtility.ToJson(data, prettyPrint: true);
    File.WriteAllText(filePath, json);
}

public GameSaveData LoadGame(string filePath)
{
    if (!File.Exists(filePath))
        return null;

    string json = File.ReadAllText(filePath);
    return JsonUtility.FromJson<GameSaveData>(json);
}
```

### Option 2: Binary Serialization

**Pros:**
- Smaller file size
- Faster
- Harder to cheat (but not secure)

**Cons:**
- Not human-readable
- Harder to debug
- Versioning is more complex

```csharp
public void SaveGame(GameSaveData data, string filePath)
{
    using (FileStream stream = File.Create(filePath))
    {
        BinaryFormatter formatter = new BinaryFormatter();
        formatter.Serialize(stream, data);
    }
}

public GameSaveData LoadGame(string filePath)
{
    if (!File.Exists(filePath))
        return null;

    using (FileStream stream = File.OpenRead(filePath))
    {
        BinaryFormatter formatter = new BinaryFormatter();
        return (GameSaveData)formatter.Deserialize(stream);
    }
}
```

**For this project, use JSON.**

---

## Save File Location

### Desktop (Steam)

```csharp
public string GetSaveFilePath()
{
    string saveFolder = Path.Combine(
        Application.persistentDataPath,
        "Saves"
    );

    if (!Directory.Exists(saveFolder))
    {
        Directory.CreateDirectory(saveFolder);
    }

    return Path.Combine(saveFolder, "save_slot_1.json");
}
```

**Example path:**
- Windows: `C:\Users\[User]\AppData\LocalLow\[Company]\[Game]\Saves\save_slot_1.json`
- Mac: `~/Library/Application Support/[Company]/[Game]/Saves/save_slot_1.json`

### Multiple Save Slots

```csharp
public string GetSaveFilePath(int slotIndex)
{
    return Path.Combine(saveFolder, $"save_slot_{slotIndex}.json");
}
```

---

## Auto-Save

### When to Auto-Save

- After battle ends (win or loss)
- After hero progression (level up, skill tree unlock)
- After changing squad configuration
- Periodically (every 5 minutes in meta/UI screens)

### Auto-Save Implementation

```csharp
public class SaveManager : MonoBehaviour
{
    private float autoSaveInterval = 300f; // 5 minutes
    private float timeSinceLastSave;

    void Update()
    {
        if (IsInMetaScreen()) // Don't auto-save during battle
        {
            timeSinceLastSave += Time.deltaTime;

            if (timeSinceLastSave >= autoSaveInterval)
            {
                AutoSave();
                timeSinceLastSave = 0f;
            }
        }
    }

    void AutoSave()
    {
        SaveGame();
        Debug.Log("Auto-saved");
    }
}
```

---

## Save File Versioning

### Why Versioning Matters

When the game updates, save file structure might change. Versioning allows backward compatibility.

```csharp
public const int CURRENT_SAVE_VERSION = 2;

[Serializable]
public class GameSaveData
{
    public int saveVersion = CURRENT_SAVE_VERSION;
    // ...
}

public GameSaveData LoadGame(string filePath)
{
    var data = LoadRawData(filePath);

    if (data.saveVersion < CURRENT_SAVE_VERSION)
    {
        data = MigrateSaveData(data);
    }

    return data;
}

private GameSaveData MigrateSaveData(GameSaveData oldData)
{
    switch (oldData.saveVersion)
    {
        case 1:
            // Migrate v1 → v2
            oldData = MigrateV1ToV2(oldData);
            goto case 2;

        case 2:
            // Already latest version
            break;

        default:
            Debug.LogWarning($"Unknown save version: {oldData.saveVersion}");
            break;
    }

    return oldData;
}

private GameSaveData MigrateV1ToV2(GameSaveData data)
{
    // Example: Add new field with default value
    data.totalBattlesPlayed = 0; // New field in v2
    data.saveVersion = 2;
    return data;
}
```

---

## Loading Game Data

### On Game Start

```csharp
void Start()
{
    GameSaveData saveData = saveManager.LoadGame();

    if (saveData == null)
    {
        // New game
        saveData = CreateNewGameSave();
    }

    ApplySaveData(saveData);
}

private GameSaveData CreateNewGameSave()
{
    return new GameSaveData
    {
        saveVersion = CURRENT_SAVE_VERSION,
        lastSaved = DateTime.Now,
        playerLevel = 1,
        ownedHeroes = GetStartingHeroes(),
        gold = 100,
        skillPoints = 0
    };
}
```

### Applying Save Data

```csharp
private void ApplySaveData(GameSaveData saveData)
{
    // Load heroes with progression
    foreach (var heroSave in saveData.ownedHeroes)
    {
        var heroDef = LoadHeroDefinition(heroSave.heroId);
        var hero = CreateProgressedHero(heroDef, heroSave);
        playerRoster.Add(hero);
    }

    // Load squad
    currentSquad = saveData.currentSquad;

    // Load resources
    playerGold = saveData.gold;
    playerSkillPoints = saveData.skillPoints;
}
```

---

## Creating Battle Units from Save Data

### Merge Static Definition + Progression

```csharp
public UnitRuntime CreateBattleUnit(string heroId, HeroSaveData saveData)
{
    var definition = LoadHeroDefinition(heroId);

    var unit = new UnitRuntime
    {
        definition = definition,
        runtimeId = GenerateRuntimeId(),
        team = UnitTeam.Player,

        // Base stats + progression bonuses
        currentStats = definition.baseStats + saveData.bonusStats,
        maxHP = (definition.baseStats.maxHP + saveData.bonusStats.maxHP),
        currentHP = (definition.baseStats.maxHP + saveData.bonusStats.maxHP),

        // Configured behavior
        behavior = new BehaviorLoadout { behaviorType = saveData.assignedBehavior },

        // Configured skills
        equippedSkills = LoadSkillSlots(saveData.equippedSkills),

        position = GridPosition.Zero,
        isDead = false
    };

    // Apply proficiency progression
    unit.definition.proficiencies = saveData.proficiencies;

    return unit;
}

private List<SkillSlot> LoadSkillSlots(List<SkillSlotSaveData> savedSlots)
{
    var slots = new List<SkillSlot>();

    foreach (var savedSlot in savedSlots)
    {
        var slot = new SkillSlot
        {
            slotIndex = savedSlot.slotIndex,
            slotName = savedSlot.slotName,
            actionSequence = new List<ActionSlot>()
        };

        for (int i = 0; i < savedSlot.actionIds.Count; i++)
        {
            var actionDef = LoadActionDefinition(savedSlot.actionIds[i]);
            slot.actionSequence.Add(new ActionSlot
            {
                subSlotIndex = i,
                action = actionDef
            });
        }

        slots.Add(slot);
    }

    return slots;
}
```

---

## Saving After Battle

```csharp
public void OnBattleEnd(BattleOutcome outcome)
{
    if (outcome == BattleOutcome.Victory)
    {
        // Award XP
        foreach (var hero in playerSquad)
        {
            var saveData = GetHeroSaveData(hero.definition.unitId);
            saveData.GainXP(100); // Battle reward
        }

        // Award resources
        gameSave.gold += 50;
    }

    // Update stats
    gameSave.totalBattlesPlayed++;
    if (outcome == BattleOutcome.Victory)
        gameSave.totalBattlesWon++;

    // Auto-save
    SaveGame();
}
```

---

## Cloud Save (Future)

### Steam Cloud

```csharp
#if STEAMWORKS_NET
public void SaveToSteamCloud(GameSaveData data)
{
    string json = JsonUtility.ToJson(data);
    byte[] bytes = System.Text.Encoding.UTF8.GetBytes(json);

    SteamRemoteStorage.FileWrite("save_slot_1.json", bytes, bytes.Length);
}

public GameSaveData LoadFromSteamCloud()
{
    if (!SteamRemoteStorage.FileExists("save_slot_1.json"))
        return null;

    int size = SteamRemoteStorage.GetFileSize("save_slot_1.json");
    byte[] bytes = new byte[size];

    SteamRemoteStorage.FileRead("save_slot_1.json", bytes, size);
    string json = System.Text.Encoding.UTF8.GetString(bytes);

    return JsonUtility.FromJson<GameSaveData>(json);
}
#endif
```

---

## Backup and Corruption Handling

### Create Backup on Save

```csharp
public void SaveGame()
{
    string savePath = GetSaveFilePath();
    string backupPath = savePath + ".backup";

    // If save file exists, create backup
    if (File.Exists(savePath))
    {
        File.Copy(savePath, backupPath, overwrite: true);
    }

    // Write new save
    try
    {
        string json = JsonUtility.ToJson(gameSaveData, prettyPrint: true);
        File.WriteAllText(savePath, json);
    }
    catch (Exception e)
    {
        Debug.LogError($"Save failed: {e.Message}");
        // Restore backup if write failed
        if (File.Exists(backupPath))
        {
            File.Copy(backupPath, savePath, overwrite: true);
        }
    }
}
```

### Detect Corruption on Load

```csharp
public GameSaveData LoadGame()
{
    string savePath = GetSaveFilePath();
    string backupPath = savePath + ".backup";

    try
    {
        string json = File.ReadAllText(savePath);
        return JsonUtility.FromJson<GameSaveData>(json);
    }
    catch (Exception e)
    {
        Debug.LogWarning($"Save file corrupted: {e.Message}. Attempting to load backup.");

        // Try backup
        if (File.Exists(backupPath))
        {
            try
            {
                string json = File.ReadAllText(backupPath);
                return JsonUtility.FromJson<GameSaveData>(json);
            }
            catch
            {
                Debug.LogError("Backup also corrupted. Starting new game.");
                return null;
            }
        }

        return null;
    }
}
```

---

## Testing

### Unit Tests

```csharp
[Test]
public void SaveAndLoad_PreservesData()
{
    var originalData = new GameSaveData
    {
        playerLevel = 10,
        gold = 500
    };

    string path = Path.GetTempFileName();

    saveManager.SaveGame(originalData, path);
    var loadedData = saveManager.LoadGame(path);

    Assert.AreEqual(10, loadedData.playerLevel);
    Assert.AreEqual(500, loadedData.gold);

    File.Delete(path);
}

[Test]
public void LoadGame_ReturnsNull_WhenFileDoesNotExist()
{
    string fakePath = "nonexistent_save.json";

    var data = saveManager.LoadGame(fakePath);

    Assert.IsNull(data);
}

[Test]
public void MigrateSave_UpgradesOldVersion()
{
    var oldData = new GameSaveData
    {
        saveVersion = 1,
        playerLevel = 5
    };

    var migratedData = saveManager.MigrateSaveData(oldData);

    Assert.AreEqual(2, migratedData.saveVersion);
    Assert.AreEqual(5, migratedData.playerLevel); // Preserved
}
```

---

## Summary

The Save/Load System handles **persistent player data**:

- ✅ Saves hero progression, unlocks, and configurations
- ✅ Loads data on game start
- ✅ Supports auto-save and manual save
- ✅ Handles save file versioning for updates
- ✅ Creates backups to prevent data loss
- ✅ **Optional for MVP** - Add after combat validation

**Save/Load ensures player progress is preserved across sessions.**

---

## Related Documentation

- **DATA_MODELS.md** - UnitDefinition, HeroSaveData structure
- **SYSTEM_PROGRESSION.md** - What data needs to be saved
- **MVP_SCOPE.md** - Save/load excluded from MVP

---

## Version

**Version 1.0** - Save/load system design (future implementation)
