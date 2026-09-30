# 03 - Data and Rules

## Data Models

All data models are serializable classes in `Assets/Scripts/VU2/Systems/Data/`.

### Plant

```csharp
public class Plant
{
    public int id;
    public string tag_name;          // e.g., "rice", "durian", "coconut"
    public int growth_time;          // Time to grow (seconds)
    public string[] status;          // Status descriptions
    public int economic_benefits;    // Base score value
    public string information;       // Description text
    public float salinity_threshold; // Max salinity before damage
    public float harvest_time;       // Time to complete harvest
}
```

### Animal

```csharp
public class Animal
{
    public int id;
    public string tag_name;          // e.g., "chicken", "duck"
    public int growth_time;
    public string[] status;
    public int economic_benefits;
    public string information;
    public float salinity_threshold;
    public float harvest_time;
}
```

### Fish

```csharp
public class Fish
{
    public int id;
    public string tag_name;          // e.g., "fish", "shrimp"
    public int growth_time;
    public string[] status;
    public int economic_benefits;
    public string information;
    public float salinity_threshold;
    public float harvest_time;
}
```

---

## Game Rules

### Season Timing
| Phase | Time Range | Salinity Factor |
|-------|------------|-----------------|
| Rainy | 0 - 90 sec | `0.0f` (low) |
| Dry | 90 - 180 sec | `1.0f` (high) |
| Game End | >180 sec | — |

### Water Type Zones
| Zone Type | Enum Value | Description |
|-----------|------------|-------------|
| Fresh | `WaterType.Fresh` | Inside dyke, lower salinity |
| Salt | `WaterType.Salt` | Outside dyke, higher salinity |

### Salinity Calculation

From `Thuan_23127_GameManager.GetSeasonSalinity()`:
```csharp
float k = (Saltwater_Intrusion == 1f) ? dryFactor : rainyFactor;
return salinityBase * k;

// Default values:
// salinityBase = 1.0‰
// rainyFactor = 0.3
// dryFactor = 1.5
```

Result:
- **Rainy season**: 1.0 × 0.3 = **0.3‰**
- **Dry season**: 1.0 × 1.5 = **1.5‰**

---

## Scoring System

### Mode 1: GrowthTime (harvest-based)
- Score added immediately when player harvests
- No season-end settlement

### Mode 2: Seasonal (season-based)
- `SettleAllFarmsForNewSeason()` called on phase change
- All plants force-harvested, scores calculated
- Plots cleared for new season

### Score Calculation

#### For Plants (`Thuan_23127_PlantGrowth.AdjustBySalinity`):

1. **Table-based** (if defined):
   - Uses fixed values from `GetTableBasedScore()`
   
2. **Threshold-based** (fallback):
   ```csharp
   if (currentSalinity <= salinityThreshold)
       return baseScore;  // Full points
   else
       return baseScore * (threshold / salinity);  // Reduced
   ```

#### For Fruits (`David_Fruit.GetTableScore`):

| Type | Fresh+Rainy | Fresh+Dry | Salt+Rainy | Salt+Dry |
|------|-------------|-----------|------------|----------|
| **Durian** | 100 | 80 | 60 | -40 |
| **Coconut** | 100 | 80 | 60 | 50 |
| **Fish** | 10 | 20 | 30 | 40 |
| **Shrimp** | 20 | 20 | 20 | 20 |
| **Rice** | 60 | -20 | 40 | 20 |

**Interpretation:**
- **Durian**: Best in fresh water + rainy; **fails** (-40) in salt + dry
- **Rice**: Also fails in fresh + dry (-20 penalty)
- **Fish/Shrimp**: Thrive better in salt water

---

## Harvest Rules

### Durian Special Rule (`David_Fruit.TryCollect`):
```csharp
if (fruitType == FruitType.Durian)
{
    if (Saltwater_Intrusion >= 1f)  // Dry season
        return;  // Cannot harvest - wilted
}
```

### Harvest Flow:
1. Player touches object with bag (tag: `"Bag"`)
2. `OnTriggerEnter` or `OnCollisionEnter` fires
3. `TryCollect()` validates game state + special rules
4. `CollectFruit()` calculates score, adds to GameManager
5. Object destroyed or deactivated (for respawn)

---

## Data Loading

### JSON Structure (`data.json`)
```json
{
  "plants": [ { Plant objects } ],
  "animals": [ { Animal objects } ],
  "fish": [ { Fish objects } ],
  "lang": {
    "vi": { Vietnamese strings },
    "en": { English strings }
  }
}
```

### Loader: `Thuan_23127_JsonReader`
- Loads from `Resources/data.json`
- Provides `GetCurrentLangData()` for localization
- Updates UI text elements for score, labels

---

## Production Values (New Formula)

Based on recent updates, production is calculated as:

| Type | Yield | Area Equivalent |
|------|-------|-----------------|
| Fruit Trees (Durian, Coconut) | 20 tons/ha | 5 ha per tree |
| Shrimp | 2 tons/ha | 10 ha per unit |
| Rice | 6 tons/ha | 10 ha per plant |

This explains the higher score values (60-100) vs old values (4-15).
