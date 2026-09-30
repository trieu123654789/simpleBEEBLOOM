# 02 - Pipeline and Systems

## Game Flow Pipeline

```
┌─────────────────┐
│   Start Menu    │
└────────┬────────┘
         │ StartGame()
         ▼
┌─────────────────────────────────────────────────────────┐
│                    GAME LOOP                            │
│  ┌─────────────┐        ┌─────────────┐                │
│  │  RAINY (1)  │──90s──▶│     DRY     │──90s──▶ END    │
│  │  0-90 sec   │        │  90-180 sec │                │
│  │ Salinity=0  │        │ Salinity=1  │                │
│  └─────────────┘        └─────────────┘                │
└─────────────────────────────────────────────────────────┘
         │ timeRemaining > 180s
         ▼
┌─────────────────┐
│  Result Menu    │
└─────────────────┘
```

---

## Core Systems

### 1. Season Controller (`RulesoftheGame_VU2_1.cs`)

**Singleton-like** manager that controls:

| Responsibility | Implementation |
|----------------|----------------|
| Time tracking | `timeRemaining` increments each frame |
| Phase transitions | `SetPhase(SeasonPhase)` at 0s, 90s, 180s |
| Weather effects | Rain particles, skybox material swap |
| Water level movement | Lerp water object between `pointA` ↔ `pointB` |
| VR movement lock | Locks XR locomotion during menus |

**Static Properties:**
- `Saltwater_Intrusion`: `0.0f` (rainy) or `1.0f` (dry)
- `GameActive`: Boolean for game running state
- `OnPhaseChanged`: Event fired when season changes

**Scoring Modes:**
- `GrowthTime`: Score on each harvest
- `Seasonal`: Score settled when season changes

---

### 2. Farm Area System (`FarmArea.cs`)

Each **FarmArea** represents a farming zone with:

| Property | Description |
|----------|-------------|
| `waterType` | `WaterType.Fresh` or `WaterType.Salt` |
| `plots[]` | Transform array for planting positions |
| `areaHUD` | UI reference for this zone |

**Key Methods:**
- `GetAreaSalinity()` → Returns salinity based on season + zone type
- `PlantInternal()` → Instantiates plant prefab at plot
- `WireGrowthForAreaTotals()` → Binds plant harvest events to area score
- `SettleAndClearForNewSeason()` → Forces harvest all plants, clears plots

**Salinity Calculation:**
```
Fresh zone: baseSalinity × (rainy ? 0.3 : 1.5)
Salt zone:  baseSalinity × (rainy ? 0.5 : 2.0)  // approximate
```

---

### 3. Plant Growth System (`Thuan_23127_PlantGrowth.cs`)

Handles lifecycle of plants, animals, fish:

**States:**
```
Init → Growing → Ready → Harvesting → Destroyed
```

**Key Methods:**
| Method | Purpose |
|--------|---------|
| `Init(Plant/Animal/Fish)` | Initialize from data model |
| `CoGrow()` | Coroutine for growth progress |
| `TryStartHarvest()` | Check conditions, start harvest |
| `FinalizeHarvest()` | Calculate score, fire event, cleanup |
| `AdjustBySalinity()` | Apply salinity-based score modifier |
| `ForceHarvestImmediateAndDestroy()` | Instant harvest for season end |

**Events:**
- `OnHarvested(int score)` → Consumed by FarmArea
- `OnBeingDestroyed` → Cleanup before destruction
- `OnProgressChanged(float)` → UI updates
- `OnSalinityChanged(float)` → HUD updates

---

### 4. Fruit Collection System (`David_Fruit.cs`)

Handles collectible objects (fruits fallen from trees):

**Flow:**
```
Collision with "Bag" tag → TryCollect() → GetTableScore() → AddScore() → Destroy
```

**Score Table (Zone × Season):**
| Type | Fresh+Rainy | Fresh+Dry | Salt+Rainy | Salt+Dry |
|------|-------------|-----------|------------|----------|
| Durian | 100 | 80 | 60 | -40 |
| Coconut | 100 | 80 | 60 | 50 |
| Fish | 10 | 20 | 30 | 40 |
| Shrimp | 20 | 20 | 20 | 20 |
| Rice | 60 | -20 | 40 | 20 |

> **Note:** Negative scores represent crop failure

---

### 5. Score Manager (`Thuan_23127_GameManager.cs`)

**Singleton** that manages global score:

| Method | Purpose |
|--------|---------|
| `AddScore(int)` | Add points, play SFX, update UI |
| `ResetScore()` | Reset to 0, update UI |
| `GetSeasonSalinity()` | Calculate salinity from base × season factor |

**Dependencies:**
- `Thuan_23127_JsonReader` for UI text references
- `audioSource` + `harvestClip` for harvest sound

---

## Event System

```
RulesoftheGame_VU2_1.OnPhaseChanged
    ├── David_SeasonHUD.OnSeasonChanged()
    ├── David_TreeSpawner (respawn fruits)
    ├── David_TreeWiltController (wilt effects)
    └── FarmArea.SettleAndClearForNewSeason()

Thuan_23127_PlantGrowth.OnHarvested
    └── FarmArea.WireGrowthForAreaTotals() → GameManager.AddScore()

Thuan_23127_GameManager.OnScoreChanged
    └── UI text updates
```

---

## XR/VR Integration

Uses **XR Interaction Toolkit**:
- `ActionBasedContinuousMoveProvider` for locomotion
- `ActionBasedContinuousTurnProvider` for rotation
- `XRGrabInteractable` on harvestable objects
- Movement locked during menus

---

## Localization System

- Data stored in `Assets/Resources/data.json`
- Loaded by `Thuan_23127_JsonReader`
- Supports Vietnamese (vi) and English (en)
- Labels include: score, season names, plant descriptions
