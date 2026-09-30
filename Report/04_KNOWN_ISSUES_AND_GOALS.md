# 04 - Known Issues and Goals

## Current State

✅ **Working Systems:**
- Season phase transitions (Rainy → Dry → End)
- Salinity-based scoring for plants and fruits
- VR locomotion with XR Interaction Toolkit
- Multi-language support (Vietnamese/English)
- Farm area management with Fresh/Salt zones
- Weather effects (rain particles, skybox switching)
- Audio system (harvest sounds, background music)

---

## Known Issues

### 1. FruitType Enum Missing Members

**Location:** `David_Fruit.cs` line 8-13

**Problem:** The `FruitType` enum only defines:
```csharp
public enum FruitType 
{ 
    Coconut,  // Dừa
    Durian,   // Sầu riêng
    Fish      // Cá
}
```

But `GetTableScore()` references:
- `FruitType.Shrimp`
- `FruitType.Rice`

**Impact:** Compile error if using Shrimp or Rice types.

**Fix Required:** Add missing enum values:
```csharp
public enum FruitType 
{ 
    Coconut,
    Durian,
    Fish,
    Shrimp,   // ADD
    Rice      // ADD
}
```

---

### 2. Inconsistent Score Tables

**Location:** 
- `David_Fruit.cs` (updated values)
- `Thuan_23127_PlantGrowth.cs` (old values in comments)
- `DavidNguyen/README.md` (outdated documentation)

**Problem:** Score values have been updated but not synced:
- `David_Fruit`: Uses new production-based values (60-100)
- `Thuan_23127_PlantGrowth`: May still use old values (4-15)
- README shows old table

**Recommendation:** Audit all score tables and unify values.

---

### 3. Negative Scores Not Validated

**Location:** `David_Fruit.GetTableScore()`

**Problem:** Some combinations return negative scores:
- Durian + Salt + Dry = **-40**
- Rice + Fresh + Dry = **-20**

**Current behavior:** `GameManager.AddScore(-40)` will subtract from total.

**Consider:** Whether negative scores are intentional design or should be clamped to 0.

---

### 4. Fish Score Mismatch

**Location:** `David_Fruit.cs` lines 117-119

**Problem:** Fish scores differ between old and new:
- Comment says: `if (isFresh) return isRainy ? 1 : 2;`
- Actual code: `if (isFresh) return isRainy ? 10 : 20;`

The code was partially updated but comment describes old logic. However, actual return values may need verification (10/20/30/40 vs 1/2/3/4).

---

### 5. Hardcoded Magic Numbers

**Locations:** Multiple files

**Examples:**
- `90f` and `180f` for phase timing
- `0.3f` and `1.5f` for salinity factors
- Score values in switch statements

**Recommendation:** Consider moving to ScriptableObjects or config file for easier balancing.

---

## Future Goals

### Short-term
- [ ] Fix FruitType enum compilation error
- [ ] Sync score tables across all files
- [ ] Update README documentation
- [ ] Validate negative score behavior

### Medium-term
- [ ] Extract scoring config to ScriptableObject
- [ ] Add unit tests for score calculations
- [ ] Implement score breakdown in result screen
- [ ] Add more crop types

### Long-term
- [ ] GAMA integration for advanced simulation
- [ ] Multiplayer support
- [ ] Additional farm areas/maps
- [ ] Save/load game state

---

## Development Team

Code organized by contributor folders in `InternCode/`:

| Folder | Focus Area |
|--------|------------|
| `Thuan_23127` | Core systems, plant growth, game manager |
| `DavidNguyen` | Fruit harvest, season HUD, tree effects |
| `Dinh_23034` | Additional features |
| `LinhH_B2110085` | Additional features |

---

## Testing Notes

### To Test Scoring:
1. Start game
2. Wait for Rainy phase (0-90s)
3. Harvest durian in Fresh zone → Should get 100 points
4. Wait for Dry phase (90-180s)
5. Durian cannot be harvested (wilted)
6. Fish in Salt zone → Should get 40 points

### Key Edge Cases:
- Harvesting at exact phase boundary
- Multiple harvests in quick succession
- Plant growth spanning phase change
- Force harvest on season end (Seasonal mode)
