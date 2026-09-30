<p align="center">
  <h1 align="center">🌾 SIMPLE VU2</h1>
  <p align="center">
    <b>VR Educational Farming Simulation — Salinity Intrusion in the Mekong Delta</b><br/>
    <b>Mô phỏng nông nghiệp giáo dục VR — Xâm nhập mặn ở Đồng bằng sông Cửu Long</b><br/><br/>
    A project by <b>IRD</b> (Institut de Recherche pour le Développement, France) &amp; <b>CTU</b> (Can Tho University, Vietnam)<br/>
    Dự án của <b>IRD</b> (Viện Nghiên cứu Phát triển, Pháp) &amp; <b>ĐH Cần Thơ</b> (Việt Nam)
  </p>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/IRD-France-blue?logo=data:image/svg+xml;base64,PHN2Zz48L3N2Zz4=" alt="IRD"/>
  <img src="https://img.shields.io/badge/CTU-Vietnam-red" alt="CTU"/>
  <img src="https://img.shields.io/badge/Unity-2022.3.5f1-blue?logo=unity" alt="Unity"/>
  <img src="https://img.shields.io/badge/XR_Toolkit-2.5.2-green" alt="XR"/>
  <img src="https://img.shields.io/badge/Platform-Meta_Quest-purple" alt="Platform"/>
  <img src="https://img.shields.io/badge/URP-14.0.8-orange" alt="URP"/>
  <img src="https://img.shields.io/badge/Languages-VI%20|%20EN%20|%20FR%20|%20TH-red" alt="Languages"/>
</p>

---

## 📖 About / Giới thiệu

**EN:** SIMPLE VU2 is an immersive VR educational game set in the Mekong Delta of Vietnam. Players manage crops, livestock, and aquaculture while adapting to seasonal salinity changes caused by saltwater intrusion — a real environmental challenge. The game teaches players about sustainable farming strategies through hands-on interaction.

**VI:** SIMPLE VU2 là game giáo dục VR đặt trong bối cảnh Đồng bằng sông Cửu Long. Người chơi quản lý cây trồng, vật nuôi và thủy sản, thích ứng với sự thay đổi độ mặn theo mùa do xâm nhập mặn — một thách thức môi trường thực tế. Game giúp người chơi hiểu về chiến lược canh tác bền vững thông qua tương tác thực hành.

---

## 🎮 Gameplay / Cách chơi

### Season System / Hệ thống mùa

| Phase / Pha | Time / Thời gian | Salinity / Độ mặn | Description / Mô tả |
|-------------|-------------------|--------------------|----------------------|
| 🌧 Rainy / Mùa Mưa | 0 – 90s | Low / Thấp (`salinityBase × 0.3`) | Best for freshwater crops / Tốt nhất cho cây nước ngọt |
| ☀️ Dry / Mùa Khô | 90 – 180s | High / Cao (`salinityBase × 1.5`) | Saltwater fish thrive / Cá nước lợ phát triển tốt |
| 🏁 End / Kết thúc | >180s | — | Score summary / Tổng kết điểm |

> **EN:** Default `salinityBase = 1.0`, giving Rainy = 0.3‰ and Dry = 1.5‰ (configurable in `Thuan_23127_GameManager`).  
> **VI:** Mặc định `salinityBase = 1.0`, cho Mưa = 0.3‰ và Khô = 1.5‰ (có thể chỉnh trong `Thuan_23127_GameManager`).

### Score Table / Bảng điểm

> Source / Nguồn: `David_Fruit.cs → GetTableScore()`

| Product / Sản phẩm | 🌧 Fresh+Rainy | ☀️ Fresh+Dry | 🌧 Salt+Rainy | ☀️ Salt+Dry |
|---------------------|:--------------:|:------------:|:-------------:|:-----------:|
| 🌳 Durian / Sầu riêng | **100** | 80 | 60 | **-40** |
| 🥥 Coconut / Dừa | **100** | 80 | 60 | 50 |
| 🐟 Fish / Cá | 10 | 20 | 30 | **40** |
| 🦐 Shrimp / Tôm | 20 | 20 | 20 | 20 |
| 🌾 Rice / Lúa | 60 | **-20** | 40 | 20 |
| 🥚 Egg / Trứng | **3** (fixed / cố định, no zone modifier) | | | |

> **EN:** Negative scores mean crop failure — the plant dies due to excessive salinity.  
> **VI:** Điểm âm nghĩa là thất bại mùa vụ — cây chết do độ mặn quá cao.

### Special Rules / Luật đặc biệt

| Rule / Luật | Detail / Chi tiết |
|-------------|-------------------|
| 🌳 Durian — Dry Season Lock | **Cannot be harvested** in Dry season (`Saltwater_Intrusion ≥ 1`). Fruit disappears from tree. / **Không thể thu hoạch** vào mùa Khô. Quả sẽ biến mất trên cây. |
| 🦐 Shrimp (Fish ID 5,6) — Zone Lock | 0 points if placed in **Fresh** water zone / 0 điểm nếu đặt trong vùng **nước Ngọt** |
| 🐟 Red Tilapia (Fish ID 2) — Zone Lock | 0 points if placed in **Salt** water zone / 0 điểm nếu đặt trong vùng **nước Mặn** |
| 🐓 Chicken (Animal ID 3) — Scaled Score | Score = `base × percent` (Fresh+Rainy: 85%, Fresh+Dry: 80%, Salt+Rainy: 75%, Salt+Dry: 60%) |
| 🐟 All Fish via PlantGrowth | Fish scored through `PlantGrowth`: Fresh+Rainy: 1pt, Fresh+Dry: 2pt, Salt+Rainy: 3pt, Salt+Dry: 4pt |

### Scoring Mechanics / Cơ chế tính điểm

**Primary path (tree fruits, shrimp, rice):** `David_Fruit.CollectFruit()` → `GetTableScore()` → fixed table above.

**Secondary path (planted crops/animals/fish via FarmArea):** `Thuan_23127_PlantGrowth.FinalizeHarvest()` → `GetTableBasedScore()`.  
If entity is not in the table, falls back to threshold formula:
```
score = baseValue × Clamp01(salinity_threshold / current_salinity)
```
*(score is reduced proportionally when salinity exceeds entity's threshold)*

### VR Harvest Mechanics / Cơ chế thu hoạch VR

| Item / Vật phẩm | Method / Cách thu hoạch |
|-----------------|-------------------------|
| 🌳 Durian / Sầu riêng | Shake tree → fruit falls → grab → put in bag |
| 🥥 Coconut / Dừa | Shake tree → fruit falls → grab → put in bag |
| 🌾 Rice / Lúa | Use Sickle tool → cut rice → collect stalks |
| 🐟 Fish / Cá | Use Fishing Net → swing into pond |
| 🦐 Shrimp / Tôm | Grab shrimp pole → hold 5s (struggle animation) → release to catch |
| 🥚 Egg / Trứng | Grab egg from nest → put in bag (3 pts fixed) |
| 🐓 Chicken / Gà | Auto-harvest via PlantGrowth lifecycle |

### Strategy / Chiến lược

- **Rainy / Mưa:** Plant Durian & Coconut in freshwater zones → **100 pts each**
- **Dry / Khô:** Farm Fish/Crab in saltwater zones → **40 pts each**
- **Avoid / Tránh:** Durian in Salt+Dry (−40), Rice in Fresh+Dry (−20)
- **Tip / Mẹo:** Shrimp is zone-free (always 20 pts) — safe baseline investment

---

## 🏗 Project Structure / Cấu trúc dự án

```
simple.CTU.VU2/
├── Assets/
│   ├── Art/                    # 3D models, textures, materials
│   ├── Audio/                  # BGM, SFX (harvest sounds...)
│   ├── Prefabs/                # Reusable game objects
│   ├── Resources/
│   │   └── data.json           # Game data: plants, animals, fish, localization
│   ├── Scenes/
│   │   ├── VU1/                # Legacy scenes (version 1)
│   │   └── VU2/                # ★ Main scenes (version 2)
│   ├── Scripts/
│   │   ├── VU1/                # Legacy scripts
│   │   └── VU2/                # ★ Main codebase (see below)
│   ├── ThirdParty/             # Third-party assets
│   └── UI/                     # UI sprites, icons
├── Packages/                   # Unity package dependencies
├── ProjectSettings/            # Unity project configuration
└── Report/                     # Technical documentation (4 files)
```

### Scripts Architecture / Kiến trúc Scripts

```
Scripts/VU2/
├── Managers/                   # Game controllers / Điều khiển game
│   └── RulesoftheGame_VU2_1.cs # Main game loop, seasons, weather
├── Game_Logic/                 # Farm logic / Logic nông trại
│   ├── FarmArea.cs             # Zone management (Fresh/Salt)
│   └── Pet_AI.cs               # Pet idle behavior
├── Systems/Data/               # Data models (15 classes)
│   ├── Root.cs, Lang.cs        # JSON root & localization
│   ├── Plant.cs, Animal.cs     # Crop & livestock models
│   └── Fish.cs, Labels.cs...   # Fish, UI labels, etc.
├── InternCode/                 # Team member contributions
│   ├── Thuan_23127/            # Core systems (21 files)
│   ├── DavidNguyen/            # Harvest & VR grab (19 files)
│   ├── Dinh_23034/             # UI & environment (4 files)
│   └── LinhH_B2110085/         # NPC & infrastructure (4 files)
├── User_Interface/             # VR canvas follower
├── Others/Localization/        # Language system
└── Documentation/              # In-project docs (3 .md files)
```

---

## ⚙️ Core Systems / Hệ thống cốt lõi

### 1. Season Controller — `RulesoftheGame_VU2_1.cs`

**EN:** Main game loop. Controls season phases, weather effects (rain, skybox), water level movement, VR locomotion lock, and month overlay system (Nov–Apr).

**VI:** Vòng lặp game chính. Điều khiển pha mùa, hiệu ứng thời tiết (mưa, skybox), di chuyển mực nước, khóa di chuyển VR, và hệ thống tháng overlay (T11–T4).

### 2. Farm Area — `FarmArea.cs`

**EN:** Manages farming zones with `Fresh` or `Salt` water types. Each zone has planting plots, its own salinity calculation, and a dedicated HUD.

**VI:** Quản lý vùng nông trại với loại nước `Ngọt` hoặc `Lợ`. Mỗi vùng có ô trồng riêng, tính toán độ mặn và HUD riêng.

### 3. Plant Growth — `Thuan_23127_PlantGrowth.cs`

**EN:** Manages the lifecycle of plants, animals, and fish: `Init → Growing → Ready → Harvesting → Destroyed`. Scoring adjusts based on salinity threshold.

**VI:** Quản lý vòng đời cây trồng, vật nuôi, và cá: `Khởi tạo → Phát triển → Sẵn sàng → Thu hoạch → Hủy`. Điểm số điều chỉnh theo ngưỡng chịu mặn.

### 4. Fruit Collection — `David_Fruit.cs`

**EN:** Handles VR-based fruit collection. Player grabs fruit (auto-pull to hand), puts into bag → score calculated from Zone × Season table. Special rule: Durian cannot be harvested in Dry season.

**VI:** Xử lý thu hoạch trái cây bằng VR. Người chơi grab trái (tự bay vào tay), bỏ vào túi → tính điểm theo bảng Vùng × Mùa. Luật riêng: Sầu riêng không thu hoạch được trong mùa Khô.

### 5. Score Manager — `Thuan_23127_GameManager.cs`

**EN:** Singleton. Manages global score, salinity calculation, and optional GAMA server reporting.

**VI:** Singleton. Quản lý điểm toàn cục, tính toán độ mặn, và tùy chọn báo cáo lên server GAMA.

### 6. Tool-Based Harvesting — `DavidNguyen/Harvest/`

**EN:** 10+ scripts for immersive VR harvesting: sickle for rice, fishing net for fish, shrimp fishing (5s delay), coconut/durian tree shaking, fish pond management.

**VI:** 10+ script cho thu hoạch VR chân thực: liềm cắt lúa, lưới đánh cá, câu tôm (5 giây), rung cây dừa/sầu riêng, quản lý ao cá.

---

## 📊 Game Data / Dữ liệu game

All data is stored in `Assets/Resources/data.json` and supports 4 languages.

| Category / Danh mục | Count / Số lượng | Examples / Ví dụ |
|----------------------|------------------|-------------------|
| Plants / Cây trồng | 13 | Durian, Rice, Coconut, Corn, Sugarcane, Mango... |
| Livestock / Vật nuôi | 6 | Cow, Pig, Chicken, Duck, Rabbit, Goat |
| Fish / Thủy sản | 8 | Snakehead, Tilapia, Shrimp, Crab, Clam... |
| NPC Dialogues | 3 | Farmer, LinhH, Engineer Linh |

Each entity has: `id`, `tag_name`, `growth_time`, `status[]`, `economic_benefits`, `salinity_threshold`, `harvest_time`.

---

## 🔧 Requirements / Yêu cầu

| Requirement | Version |
|-------------|---------|
| Unity Editor | **2022.3.5f1** (LTS) |
| XR Interaction Toolkit | 2.5.2 |
| XR Plugin: Oculus | 4.0.0 |
| XR Plugin: OpenXR | 1.8.2 |
| Universal Render Pipeline | 14.0.8 |
| Target Device | Meta Quest 2/3/Pro |

---

## 🚀 Getting Started / Bắt đầu

1. **Clone the repository / Clone repo:**
   ```bash
   git clone <repository-url>
   ```

2. **Open in Unity Hub / Mở bằng Unity Hub:**
   - Use Unity **2022.3.5f1**
   - URP and XR packages will auto-resolve from `Packages/manifest.json`

3. **Open the main scene / Mở scene chính:**
   ```
   Assets/Scenes/VU2/SCN_VU2_Level1_New.unity
   ```

4. **Build for Quest / Build cho Quest:**
   - Switch platform to **Android**
   - Enable **Oculus** in XR Plugin Management
   - Build & Run

5. **Desktop testing / Test trên Desktop:**
   - Use XR Device Simulator (included in XR Toolkit samples)
   - Press Play in Editor

---

## 👥 Team / Nhóm phát triển

| Member / Thành viên | Role / Vai trò | Focus / Chuyên môn |
|----------------------|----------------|---------------------|
| **Thuận** (Thuan_23127) | Core Systems Lead | PlantGrowth, GameManager, JsonReader, AreaHUD, ScoreTracker, WaterPump |
| **David** (DavidNguyen) | Harvest & VR Interaction | Fruit collection, ShrimpGrab, TreeSpawner, SeasonHUD, Tool harvesting |
| **Dinh** (Dinh_23034) | UI & Environment | SkyboxController, PlantArea, PlantUIManager |
| **Linh H** (LinhH_B2110085) | NPC & Infrastructure | NPC dialogue, ConversationUI, WaterPump, AnimalSound |

---

## 📁 Documentation / Tài liệu

| File | Content / Nội dung |
|------|--------------------|
| [Report/01_PROJECT_OVERVIEW.md](Report/01_PROJECT_OVERVIEW.md) | Project summary / Tổng quan |
| [Report/02_PIPELINE_AND_SYSTEMS.md](Report/02_PIPELINE_AND_SYSTEMS.md) | Systems architecture / Kiến trúc hệ thống |
| [Report/03_DATA_AND_RULES.md](Report/03_DATA_AND_RULES.md) | Data models & game rules / Model dữ liệu & luật chơi |
| [Report/04_KNOWN_ISSUES_AND_GOALS.md](Report/04_KNOWN_ISSUES_AND_GOALS.md) | Known issues & roadmap / Vấn đề & lộ trình |

---

## 🏛 Institutions / Đơn vị thực hiện

### IRD — Institut de Recherche pour le Développement (France)
**EN:** IRD is a French public research institute focused on sustainable development in partnership with countries in the Global South. IRD leads the SIMPLE project, providing scientific guidance on salinity modeling, environmental simulation, and serious game design for climate change education.

**VI:** IRD là viện nghiên cứu công lập của Pháp chuyên về phát triển bền vững, hợp tác với các nước phía Nam. IRD chủ trì dự án SIMPLE, cung cấp chỉ đạo khoa học về mô hình hóa độ mặn, mô phỏng môi trường, và thiết kế game nghiêm túc (serious game) cho giáo dục biến đổi khí hậu.

🔗 [https://www.ird.fr](https://www.ird.fr)

### CTU — Can Tho University (Vietnam)
**EN:** Can Tho University is the leading university in the Mekong Delta region. CTU contributes the development team and provides local expertise on agriculture, salinity intrusion, and the socio-economic context of the Mekong Delta.

**VI:** Đại học Cần Thơ là trường đại học hàng đầu vùng ĐBSCL. ĐH Cần Thơ đóng góp đội ngũ phát triển và chuyên môn địa phương về nông nghiệp, xâm nhập mặn, và bối cảnh kinh tế-xã hội vùng ĐBSCL.

🔗 [https://www.ctu.edu.vn](https://www.ctu.edu.vn)

---

## 📝 License / Giấy phép

This project is developed for educational and research purposes by **IRD** (France) in collaboration with **Can Tho University** (Vietnam).

Dự án được phát triển phục vụ mục đích giáo dục và nghiên cứu bởi **IRD** (Pháp) phối hợp với **Trường Đại học Cần Thơ** (Việt Nam).
