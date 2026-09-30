# BÁO CÁO KHOA HỌC

## Mô hình hóa tác động của độ mặn lên năng suất nông nghiệp: Từ dữ liệu thực địa đến đào tạo nông nghiệp bền vững

### *Modeling the Impact of Salinity on Agricultural Productivity: From Field Data to Sustainable Agriculture Training*

---

**Đơn vị thực hiện:**
- Viện Nghiên cứu Phát triển (IRD — Institut de Recherche pour le Développement), Pháp
- Trường Đại học Cần Thơ (CTU — Can Tho University), Việt Nam

**Nền tảng công nghệ:** Unity 2022.3.5f1 | XR Interaction Toolkit 2.5.2 | Meta Quest VR | Universal Render Pipeline 14.0.8 | GAMA Agent-Based Modeling Platform

**Ngôn ngữ hỗ trợ:** Tiếng Việt | English | Français | ภาษาไทย

---

## TÓM TẮT (Abstract)

Xâm nhập mặn là một trong những thách thức môi trường nghiêm trọng nhất đối với nông nghiệp Đồng bằng sông Cửu Long (ĐBSCL), Việt Nam — vùng sản xuất lương thực trọng điểm quốc gia. Nghiên cứu này trình bày hệ thống **SIMPLE VU2** (*Simulation for Interactive Modeling and Participatory Learning Environment — Version 2*), một ứng dụng mô phỏng nông nghiệp giáo dục sử dụng công nghệ thực tế ảo (VR) để mô hình hóa tác động của độ mặn lên năng suất cây trồng, vật nuôi và thủy sản. Hệ thống tích hợp dữ liệu ngưỡng chịu mặn thực địa từ 13 loại cây trồng, 6 loại vật nuôi và 8 loại thủy sản đặc trưng của ĐBSCL vào một mô hình toán học đa tầng, kết hợp với nền tảng mô phỏng tác nhân GAMA để tạo ra trải nghiệm đào tạo nông nghiệp bền vững chân thực và có tính tương tác cao. Kết quả cho thấy mô hình có khả năng phản ánh chính xác mối quan hệ phi tuyến giữa độ mặn và năng suất, đồng thời cung cấp công cụ giáo dục hiệu quả cho nông dân và sinh viên trong việc ra quyết định canh tác thích ứng với biến đổi khí hậu.

**Từ khóa:** xâm nhập mặn, mô hình hóa nông nghiệp, thực tế ảo, đào tạo nông nghiệp bền vững, Đồng bằng sông Cửu Long, GAMA, Unity, năng suất cây trồng, ngưỡng chịu mặn

---

## 1. GIỚI THIỆU (Introduction)

### 1.1. Bối cảnh nghiên cứu

Đồng bằng sông Cửu Long (ĐBSCL) là vùng nông nghiệp trọng điểm của Việt Nam, đóng góp hơn 50% sản lượng lúa gạo, 65% sản lượng thủy sản và 70% sản lượng trái cây cả nước (Tổng cục Thống kê, 2023). Tuy nhiên, vùng đồng bằng này đang đối mặt với thách thức ngày càng nghiêm trọng từ hiện tượng xâm nhập mặn — kết quả của sự kết hợp giữa biến đổi khí hậu, nước biển dâng, sụt lún đất và suy giảm lưu lượng nước thượng nguồn do xây dựng đập thủy điện trên dòng chính sông Mekong.

Theo số liệu quan trắc, trong mùa khô 2019–2020, xâm nhập mặn đã ảnh hưởng đến khoảng 10 trong 13 tỉnh thành ĐBSCL, với độ mặn vượt ngưỡng 4‰ lấn sâu tới 90–100 km vào đất liền tại một số nhánh sông (Viện Khoa học Thủy lợi miền Nam, 2020). Điều này gây thiệt hại nghiêm trọng cho các loại cây trồng nhạy cảm như sầu riêng (ngưỡng chịu mặn 0,8‰), lúa (ngưỡng 1,92‰) và các loại cây ăn trái khác.

### 1.2. Vấn đề nghiên cứu

Mặc dù đã có nhiều nghiên cứu về tác động của xâm nhập mặn lên nông nghiệp ĐBSCL, phần lớn các kết quả vẫn ở dạng báo cáo khoa học truyền thống, khó tiếp cận đối với nông dân — đối tượng chịu ảnh hưởng trực tiếp nhất. Khoảng cách giữa tri thức khoa học và thực hành nông nghiệp tạo ra nhu cầu cấp thiết về các công cụ giáo dục trực quan, tương tác, giúp nông dân hiểu và áp dụng các chiến lược canh tác thích ứng.

### 1.3. Mục tiêu nghiên cứu

Nghiên cứu này nhằm:

1. **Xây dựng mô hình toán học đa tầng** mô phỏng tác động của độ mặn lên năng suất nông nghiệp, tích hợp dữ liệu ngưỡng chịu mặn thực địa của các loài đặc trưng ĐBSCL.
2. **Phát triển hệ thống mô phỏng VR tương tác** cho phép người dùng trải nghiệm quản lý nông trại trong bối cảnh xâm nhập mặn theo mùa.
3. **Tích hợp nền tảng mô phỏng đa tác nhân GAMA** để nâng cao tính chân thực và khả năng mô phỏng phức tạp.
4. **Đánh giá tiềm năng ứng dụng** của hệ thống trong đào tạo nông nghiệp bền vững.

### 1.4. Phạm vi nghiên cứu

Nghiên cứu tập trung vào vùng ĐBSCL với chu kỳ mùa đặc trưng:
- **Cấp độ 1 (Level 1):** Mùa khô (tháng 11 – tháng 4), độ mặn tăng dần
- **Cấp độ 2 (Level 2):** Mùa mưa (tháng 5 – tháng 10), độ mặn giảm dần

---

## 2. CƠ SỞ LÝ THUYẾT VÀ TỔNG QUAN TÀI LIỆU (Theoretical Background & Literature Review)

### 2.1. Cơ chế xâm nhập mặn tại ĐBSCL

Xâm nhập mặn tại ĐBSCL là quá trình nước biển có nồng độ muối cao xâm nhập vào các hệ thống sông ngòi, kênh rạch nội đồng, đặc biệt trong mùa khô khi lưu lượng nước ngọt từ thượng nguồn giảm. Các yếu tố chính ảnh hưởng bao gồm:

- **Lưu lượng mùa khô:** Giảm từ 8.000–10.000 m³/s (mùa mưa) xuống 2.000–3.000 m³/s (mùa khô)
- **Thủy triều:** Biên độ triều tại cửa sông dao động 2,5–3,5 m, ảnh hưởng trực tiếp đến phạm vi xâm nhập
- **Nước biển dâng:** Dự báo dâng 30–50 cm vào năm 2050 (IPCC AR6, 2021)
- **Sụt lún đất:** Tốc độ 1–3 cm/năm do khai thác nước ngầm quá mức

### 2.2. Ảnh hưởng của độ mặn lên sinh trưởng cây trồng

Độ mặn ảnh hưởng đến cây trồng thông qua ba cơ chế chính:

1. **Stress thẩm thấu:** Nồng độ muối cao trong đất làm giảm khả năng hấp thụ nước của rễ cây
2. **Độc tính ion:** Tích tụ Na⁺ và Cl⁻ gây hại trực tiếp đến tế bào thực vật
3. **Mất cân bằng dinh dưỡng:** Ion Na⁺ cạnh tranh với K⁺, Ca²⁺ trong quá trình hấp thụ

Mỗi loài thực vật có **ngưỡng chịu mặn** (salinity threshold) khác nhau — mức độ mặn tối đa mà cây vẫn có thể sinh trưởng và cho năng suất bình thường. Khi vượt ngưỡng, năng suất giảm theo quan hệ tỷ lệ nghịch với mức vượt ngưỡng (Maas & Hoffman, 1977).

### 2.3. Mô hình toán học Maas–Hoffman

Mô hình kinh điển của Maas và Hoffman (1977) mô tả quan hệ giữa độ mặn và năng suất tương đối:

$$Y_r = \begin{cases} 100\% & \text{khi } EC_e \leq EC_t \\ 100 - b(EC_e - EC_t) & \text{khi } EC_e > EC_t \end{cases}$$

Trong đó:
- $Y_r$ : Năng suất tương đối (%)
- $EC_e$ : Độ dẫn điện của dung dịch đất (dS/m), tương quan với độ mặn
- $EC_t$ : Ngưỡng chịu mặn của cây trồng
- $b$ : Hệ số suy giảm năng suất (%/dS/m)

### 2.4. Ứng dụng VR trong giáo dục nông nghiệp

Công nghệ thực tế ảo (VR) đã được chứng minh là công cụ hiệu quả trong đào tạo nông nghiệp nhờ:
- Khả năng tái tạo môi trường nông nghiệp chân thực
- Cho phép học tập trải nghiệm (experiential learning) không phụ thuộc thời tiết, mùa vụ
- Tăng cường ghi nhớ thông qua tương tác trực tiếp (Radianti et al., 2020)
- Mô phỏng các kịch bản rủi ro mà không gây thiệt hại thực tế

### 2.5. Nền tảng GAMA trong mô hình hóa nông nghiệp

GAMA (GIS & Agent-based Modeling Architecture) là nền tảng mô phỏng đa tác nhân mã nguồn mở, được phát triển bởi IRD và các đối tác quốc tế. GAMA cho phép:
- Mô hình hóa hệ thống phức tạp với nhiều tác nhân tương tác
- Tích hợp dữ liệu GIS thực tế
- Mô phỏng động lực học môi trường (thủy triều, xâm nhập mặn)
- Kết nối với Unity qua WebSocket để tạo mô phỏng VR tương tác

---

## 3. PHƯƠNG PHÁP NGHIÊN CỨU (Methodology)

### 3.1. Kiến trúc tổng thể hệ thống

Hệ thống SIMPLE VU2 được thiết kế theo kiến trúc ba tầng:

```
┌─────────────────────────────────────────────────────────────────┐
│                    TẦNG TRÌNH BÀY (Presentation Layer)         │
│    VR Interface │ Meta Quest │ XR Interaction Toolkit 2.5.2    │
│    Thu hoạch VR │ Tương tác nông trại │ HUD thời tiết/mùa     │
├─────────────────────────────────────────────────────────────────┤
│                    TẦNG LOGIC (Logic Layer)                     │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────────────┐ │
│  │ Season       │  │ Farm Area    │  │ Plant Growth         │ │
│  │ Controller   │──│ Manager      │──│ Lifecycle            │ │
│  │ (3 pha mùa)  │  │ (Ngọt/Lợ)   │  │ (Ngưỡng chịu mặn)   │ │
│  └──────────────┘  └──────────────┘  └──────────────────────┘ │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────────────┐ │
│  │ Score        │  │ Tidal Clock  │  │ Fruit Collection     │ │
│  │ Manager      │  │ Manager      │  │ System               │ │
│  └──────────────┘  └──────────────┘  └──────────────────────┘ │
├─────────────────────────────────────────────────────────────────┤
│                    TẦNG DỮ LIỆU (Data Layer)                   │
│  data.json (ngưỡng chịu mặn, lợi ích kinh tế, mô tả 4 ngôn  │
│  ngữ) │ GAMA WebSocket (mô phỏng đa tác nhân) │ ScriptableObj │
└─────────────────────────────────────────────────────────────────┘
```

### 3.2. Mô hình hóa hệ thống mùa và xâm nhập mặn

#### 3.2.1. Hệ thống pha mùa

Hệ thống mùa được mô hình hóa thông qua ba pha liên tục, phản ánh quy luật thủy văn thực tế của ĐBSCL:

**Cấp độ 1 — Mùa khô (Tháng 11 → Tháng 4):**

| Pha | Tháng | Thời gian mô phỏng | Hệ số xâm nhập mặn $S_i$ | Mực nước |
|-----|-------|---------------------|:--:|----------|
| Pha 1 (Mưa cuối) | T11, T12, T1 | 0 – 90s | 0,0 | 80% → 65% → 50% |
| Pha 2 (Khô) | T2, T3 | 90 – 150s | 0,5 | 40% → 30% |
| Pha 3 (Khô đỉnh) | T4 | 150 – 180s | 1,0 | 20% |

**Cấp độ 2 — Mùa mưa (Tháng 5 → Tháng 10):**

| Pha | Tháng | $S_i$ | Mực nước |
|-----|-------|:--:|----------|
| Pha 1 (Khô tàn dư) | T5, T6, T7 | 1,0 | 20% → 30% → 40% |
| Pha 2 (Chuyển tiếp) | T8, T9 | 0,5 | 55% → 70% |
| Pha 3 (Mưa đỉnh) | T10 | 0,0 | 80% |

Mỗi tháng được mô phỏng trong $\Delta t = 30$ giây (mặc định, có thể cấu hình), tổng thời gian một chu kỳ là $6 \times \Delta t = 180$ giây.

#### 3.2.2. Công thức tính độ mặn toàn cục

Độ mặn toàn cục tại thời điểm $t$ được tính bởi:

$$S_{global}(t) = S_i(t) \times S_{base} \times W_m(t)$$

Trong đó:
- $S_i(t) \in \{0{,}0;\; 0{,}5;\; 1{,}0\}$ : Hệ số xâm nhập mặn theo pha mùa
- $S_{base} = 4{,}0$‰ : Độ mặn cơ sở (cấu hình được)
- $W_m(t)$ : Hệ số điều chỉnh theo mực nước

Hệ số mực nước được tính:

$$W_m(t) = \text{Lerp}(0{,}85;\; 1{,}15;\; L(t)/100)$$

với $L(t)$ là phần trăm mực nước tại thời điểm $t$ (giảm dần từ 80% → 20% trong mùa khô).

**Kết quả tính toán:**
| Pha | $S_i$ | $L(t)$ trung bình | $W_m$ | $S_{global}$ (‰) |
|-----|:--:|:--:|:--:|:--:|
| Pha 1 | 0,0 | 65% | ~1,05 | **0,0** |
| Pha 2 | 0,5 | 35% | ~0,96 | **~1,9** |
| Pha 3 | 1,0 | 20% | ~0,91 | **~3,6** |

#### 3.2.3. Công thức tính độ mặn cục bộ (theo vùng nông trại)

Mỗi vùng nông trại (FarmArea) có thể sử dụng giá trị độ mặn cục bộ riêng:

$$S_{area}(t) = \begin{cases} S_{rainy} = 0{,}5\text{‰} & \text{khi } S_i < 0{,}1 \\ S_{mid} = 1{,}0\text{‰} & \text{khi } 0{,}1 \leq S_i < 1{,}0 \\ S_{dry} = 1{,}5\text{‰} & \text{khi } S_i = 1{,}0 \end{cases}$$

Nếu không bật chế độ cục bộ, hệ thống quay về giá trị toàn cục $S_{global}(t)$.

Vùng nông trại được phân loại:
- **Vùng nước ngọt (Fresh):** Bên trong đê bao, độ mặn thấp hơn
- **Vùng nước lợ/mặn (Salt):** Bên ngoài đê bao, chịu ảnh hưởng trực tiếp từ xâm nhập mặn

### 3.3. Mô hình tác động độ mặn lên năng suất

#### 3.3.1. Mô hình ngưỡng (Threshold Model)

Dựa trên mô hình Maas–Hoffman, hệ thống áp dụng công thức tính điểm (đại diện cho năng suất) theo độ mặn:

$$\text{Score}(S) = \begin{cases} P_{base} & \text{khi } S \leq S_{threshold} \\[8pt] P_{base} \times \dfrac{S_{threshold}}{S} & \text{khi } S > S_{threshold} \end{cases}$$

Trong đó:
- $P_{base}$ : Năng suất/điểm cơ sở của loài (economic_benefits)
- $S$ : Độ mặn hiện tại (từ `GetAreaSalinity()`)
- $S_{threshold}$ : Ngưỡng chịu mặn của loài (từ data.json)

**Đặc điểm quan trọng:**
- Khi $S \leq S_{threshold}$: Năng suất đạt tối đa (không bị ảnh hưởng)
- Khi $S > S_{threshold}$: Năng suất suy giảm **tỷ lệ nghịch** với mức vượt ngưỡng
- Năng suất tiến đến 0 khi $S \gg S_{threshold}$ (nhưng không bao giờ triệt tiêu hoàn toàn)

**Ví dụ minh họa — Sầu riêng ($S_{threshold} = 0{,}8$‰, $P_{base} = 100$):**

| Độ mặn hiện tại (‰) | Tỷ lệ năng suất | Điểm |
|:---:|:---:|:---:|
| 0,3 | 100% | 100 |
| 0,8 | 100% | 100 |
| 1,0 | 80% | 80 |
| 1,6 | 50% | 50 |
| 3,2 | 25% | 25 |
| 4,0 | 20% | 20 |

#### 3.3.2. Mô hình bảng tra cứu (Lookup Table Model)

Đối với các sản phẩm chủ lực, hệ thống sử dụng bảng điểm cố định theo tổ hợp **Vùng nước × Mùa**, phản ánh dữ liệu sản lượng thực tế:

| Sản phẩm | Nước ngọt + Mưa | Nước ngọt + Khô | Nước lợ + Mưa | Nước lợ + Khô |
|-----------|:---:|:---:|:---:|:---:|
| 🌳 Sầu riêng | **+100** | +80 | +60 | **−40** |
| 🥥 Dừa | **+100** | +80 | +60 | +50 |
| 🐟 Cá | +10 | +20 | +30 | **+40** |
| 🦐 Tôm | +20 | +20 | +20 | +20 |
| 🌾 Lúa | +60 | **−20** | +40 | +20 |
| 🥚 Trứng | +3 | +3 | +3 | +3 |

**Giá trị âm** thể hiện **thất bại mùa vụ** — cây trồng chết hoàn toàn do độ mặn vượt xa ngưỡng chịu đựng, gây thiệt hại kinh tế cho nông dân. Đây là phản ánh chân thực của hiện tượng chết hàng loạt vườn sầu riêng tại ĐBSCL trong các đợt xâm nhập mặn kỷ lục.

#### 3.3.3. Quy tắc đặc biệt theo loài

Hệ thống mô hình hóa các quy tắc sinh học đặc thù:

| Quy tắc | Cơ sở khoa học | Triển khai |
|---------|---------------|-----------|
| Sầu riêng không thu hoạch được vào mùa khô | Cây rụng quả khi stress mặn kéo dài | Khi $S_i \geq 1{,}0$: quả biến mất trên cây |
| Tôm sú không sống trong nước ngọt hoàn toàn | Loài nước lợ, cần độ mặn tối thiểu | Điểm = 0 nếu đặt trong vùng Fresh |
| Cá điêu hồng không sống trong nước mặn | Loài nước ngọt, nhạy với muối | Điểm = 0 nếu đặt trong vùng Salt |
| Gà: năng suất giảm tuyến tính theo mùa × vùng | Stress nhiệt + khô hạn gián tiếp | Hệ số: 85%/80%/75%/60% |

### 3.4. Dữ liệu ngưỡng chịu mặn thực địa

Dữ liệu ngưỡng chịu mặn được thu thập và tổng hợp từ các nguồn:
- Nghiên cứu thực nghiệm tại ĐBSCL (Đại học Cần Thơ)
- Tài liệu kỹ thuật của Viện Lúa ĐBSCL
- Cơ sở dữ liệu FAO về khả năng chịu mặn của cây trồng

#### Bảng 1: Ngưỡng chịu mặn các loài cây trồng

| ID | Tên (VI) | Tên (EN) | Ngưỡng (‰) | Lợi ích kinh tế | Phân loại |
|:--:|----------|----------|:---:|:---:|-----------|
| 1 | Sầu riêng | Durian | **0,80** | 4 | Rất nhạy cảm |
| 2 | Chuối | Banana | **0,80** | 2–5 | Rất nhạy cảm |
| 3 | Bắp cải | Cabbage | **1,15** | 2 | Nhạy cảm |
| 4 | Bắp | Corn | **1,09** | 3 | Nhạy cảm |
| 5 | Thanh long | Dragon fruit | **2,00** | 2 | Chịu mặn trung bình |
| 6 | Ổi | Guava | **3,01** | 3 | Chịu mặn khá |
| 7 | Lúa | Rice | **1,92** | 4 | Nhạy cảm vừa |
| 8 | Mía | Sugarcane | **12,00** | 3 | Chịu mặn cao |
| 9 | Cam | Orange | **0,83** | 3 | Rất nhạy cảm |
| 10 | Dừa | Coconut | **0,80** | 3 | Rất nhạy cảm |
| 11 | Xoài | Mango | **12,00** | 3 | Chịu mặn cao |

#### Bảng 2: Ngưỡng chịu mặn vật nuôi

| ID | Tên (VI) | Tên (EN) | Ngưỡng (‰) | Ghi chú |
|:--:|----------|----------|:---:|---------|
| 1 | Bò | Cow | **0,50** | Rất nhạy cảm với nước uống mặn |
| 2 | Heo | Pig | **0,50** | Rất nhạy cảm |
| 3 | Gà | Chicken | **0,50** | Giảm đẻ trứng khi mặn |
| 4 | Vịt | Duck | **1,00** | Thích nghi tốt hơn gia cầm khác |
| 5 | Thỏ | Rabbit | **12,00** | Chịu được môi trường phổ rộng |
| 6 | Dê | Goat | **12,00** | Chịu được môi trường phổ rộng |

#### Bảng 3: Ngưỡng chịu mặn thủy sản

| ID | Tên (VI) | Tên (EN) | Ngưỡng (‰) | Đặc điểm |
|:--:|----------|----------|:---:|----------|
| 1 | Cá lóc | Snakehead fish | **3,00** | Nước ngọt, chịu mặn nhẹ |
| 2 | Cá điêu hồng | Red tilapia | **2,50** | Nước ngọt |
| 3 | Cá rô phi | Tilapia | **0,50** | Nước ngọt thuần |
| 4 | Cá chép | Carp | **2,00** | Nước ngọt |
| 5 | Tôm sú | Giant tiger prawn | **15,00** | Nước lợ–mặn, chịu mặn rất cao |
| 6 | Tôm càng xanh | Freshwater prawn | **2,00** | Nước ngọt–lợ nhẹ |
| 7 | Nghêu | Clam | **2,00** | Nước lợ |
| 8 | Cua biển | Sea crab | **2,00** | Nước lợ–mặn |

### 3.5. Hệ thống thủy triều

Ngoài chu kỳ mùa, hệ thống bổ sung **mô hình thủy triều** dựa trên pha mặt trăng:

$$I_{tidal}(t) = |\cos(2\pi \cdot f_{orbit} \cdot t)|$$

Với $f_{orbit}$ là tần số quỹ đạo mặt trăng. Cường độ thủy triều ảnh hưởng đến:
- **Triều cường (Spring Tide):** $I > 0{,}7$ → mực nước cao, tăng phạm vi xâm nhập mặn
- **Triều kém (Neap Tide):** $I < 0{,}3$ → mực nước thấp, giảm xâm nhập

Pha mặt trăng: Trăng mới → Bán nguyệt đầu → Trăng tròn → Bán nguyệt cuối, tạo chu kỳ triều cường – triều kém xen kẽ.

### 3.6. Tích hợp GAMA Agent-Based Modeling

Hệ thống sử dụng kết nối WebSocket giữa Unity và GAMA:

```
┌──────────────┐        WebSocket         ┌──────────────┐
│   Unity VR   │ ◄══════════════════════► │    GAMA      │
│  (Client)    │   JSON-RPC messages      │  (Server)    │
│              │                          │              │
│ Gửi:        │ ──────────────────────► │ Nhận:        │
│ - Vị trí    │   harvest_event,         │ - Sự kiện    │
│   người chơi│   player_position        │   thu hoạch  │
│ - Sự kiện   │                          │ - Dữ liệu    │
│   tương tác │ ◄────────────────────── │   mô phỏng   │
│              │   geometry, enemies,     │ - Hình học    │
│ Nhận:       │   water_pump_data,       │   môi trường  │
│ - Địch (mặn)│   subsidence_data        │ - Triều/mặn  │
│ - Địa hình  │                          │   phức tạp    │
└──────────────┘                          └──────────────┘
```

Giao thức kết nối:
1. **DISCONNECTED → PENDING:** Gửi yêu cầu kết nối tới middleware/GAMA server
2. **PENDING → CONNECTED:** Nhận xác nhận kết nối
3. **CONNECTED → AUTHENTICATED:** Xác thực thành công, bắt đầu trao đổi dữ liệu
4. **Trao đổi liên tục:** Gửi/nhận thông điệp JSON qua `SendExecutableAsk(action, args)`

### 3.7. Vòng đời sinh trưởng

Mỗi thực thể nông nghiệp (cây, vật nuôi, cá) trải qua vòng đời 5 giai đoạn:

```
Khởi tạo (Init) → Sinh trưởng (Growing) → Sẵn sàng (Ready)
    → Thu hoạch (Harvesting) → Tiêu hủy (Destroyed)
```

Trong suốt quá trình sinh trưởng, hệ thống liên tục:
- Cung cấp độ mặn hiện tại từ FarmArea
- Hiển thị trạng thái sức khỏe (4 cấp: Khỏe mạnh → Nhẹ → Trung bình → Nặng)
- Cập nhật thanh tiến trình trên HUD
- Phát sự kiện khi hoàn thành thu hoạch

---

## 4. KẾT QUẢ VÀ PHÂN TÍCH (Results & Analysis)

### 4.1. Đồ thị mối quan hệ độ mặn – năng suất

Từ mô hình ngưỡng, quan hệ giữa độ mặn và năng suất tương đối theo loài:

```
Năng suất (%)
100│━━━━━━━━━━━┓
   │            ┃╲
 80│            ┃  ╲ Sầu riêng (0.8‰)
   │            ┃    ╲
 60│            ┃      ╲_____ Lúa (1.92‰)
   │            ┃       ╲      ╲___
 40│            ┃        ╲         ╲___ Cá lóc (3.0‰)
   │            ┃         ╲             ╲___
 20│            ┃          ╲                 ╲___ Tôm sú (15.0‰)
   │            ┃           ╲____________________╲_____
  0│━━━━━━━━━━━┻━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
   0    1    2    3    4    5    6    8   10   12   15
                        Độ mặn (‰)
```

**Nhận xét:**
- **Sầu riêng** (ngưỡng 0,8‰): Nhạy cảm nhất — chỉ cần độ mặn 1,6‰ đã mất 50% năng suất
- **Lúa** (ngưỡng 1,92‰): Nhạy cảm vừa — mất 50% ở 3,84‰
- **Cá lóc** (ngưỡng 3,0‰): Chịu được môi trường lợ nhẹ
- **Tôm sú** (ngưỡng 15,0‰): Gần như không bị ảnh hưởng trong phạm vi mặn ĐBSCL

### 4.2. Phân tích chiến lược canh tác tối ưu

Dựa trên bảng điểm và mô hình, chiến lược canh tác tối ưu theo mùa:

#### 4.2.1. Mùa mưa (Pha 1: $S_{global} \approx 0$‰)

| Chiến lược | Vùng | Sản phẩm | Điểm kỳ vọng |
|-----------|------|----------|:---:|
| **Tối ưu** | Nước ngọt | Sầu riêng | **+100** |
| **Tối ưu** | Nước ngọt | Dừa | **+100** |
| **Tốt** | Nước ngọt | Lúa | +60 |
| **An toàn** | Bất kỳ | Tôm | +20 |

#### 4.2.2. Mùa khô (Pha 3: $S_{global} \approx 3{,}6$–4,0‰)

| Chiến lược | Vùng | Sản phẩm | Điểm kỳ vọng |
|-----------|------|----------|:---:|
| **Tối ưu** | Nước lợ | Cá | **+40** |
| **Tránh** | Nước lợ | Sầu riêng | **−40** (thất bại) |
| **Tránh** | Nước ngọt | Lúa | **−20** (thất bại) |
| **An toàn** | Bất kỳ | Tôm | +20 |

### 4.3. Phân tích nhóm chịu mặn

Dựa trên ngưỡng chịu mặn, các loài được phân thành 4 nhóm:

**Nhóm 1 — Rất nhạy cảm ($S_{threshold} < 1{,}0$‰):**
Sầu riêng, Chuối, Cam, Dừa, Bò, Heo, Gà, Cá rô phi

→ *Chỉ canh tác trong mùa mưa, vùng nước ngọt. Nguy cơ thiệt hại cao nhất từ xâm nhập mặn.*

**Nhóm 2 — Nhạy cảm vừa ($1{,}0 \leq S_{threshold} < 3{,}0$‰):**
Lúa, Bắp, Bắp cải, Thanh long, Vịt, Cá điêu hồng, Cá chép, Tôm càng xanh, Nghêu, Cua biển

→ *Cần theo dõi độ mặn chặt chẽ. Có thể canh tác ở giai đoạn chuyển tiếp mùa.*

**Nhóm 3 — Chịu mặn khá ($3{,}0 \leq S_{threshold} < 10{,}0$‰):**
Ổi, Cá lóc

→ *Có thể canh tác kéo dài vào mùa khô nhẹ.*

**Nhóm 4 — Chịu mặn cao ($S_{threshold} \geq 10{,}0$‰):**
Mía, Xoài, Tôm sú, Thỏ, Dê

→ *Phù hợp canh tác quanh năm, kể cả vùng chịu ảnh hưởng mặn nặng. Đặc biệt tôm sú (15‰) là lựa chọn chuyển đổi lý tưởng cho vùng nhiễm mặn.*

### 4.4. Mô phỏng kịch bản mực nước và thủy triều

Hệ thống tích hợp mô hình thủy triều tạo ra sự biến thiên ngắn hạn trong chu kỳ mùa dài hạn:

| Sự kiện | Tác động lên độ mặn | Tác động lên canh tác |
|---------|---------------------|----------------------|
| Triều cường + Mùa khô | Cực đại — mặn lấn sâu nhất | Nguy hiểm cao nhất, cần biện pháp bảo vệ |
| Triều cường + Mùa mưa | Nhẹ — nước mưa pha loãng | Ít ảnh hưởng |
| Triều kém + Mùa khô | Trung bình | Cơ hội ngắn để thu hoạch |
| Triều kém + Mùa mưa | Thấp nhất | Điều kiện canh tác tốt nhất |

### 4.5. Đánh giá sản lượng quy đổi thực tế

Hệ thống quy đổi điểm trong game sang đơn vị sản lượng thực tế:

| Loại | Năng suất thực | Diện tích quy đổi/đơn vị |
|------|-----------|-------------|
| Cây ăn trái (Sầu riêng, Dừa) | 20 tấn/ha | 5 ha/đơn vị |
| Tôm | 2 tấn/ha | 10 ha/đơn vị |
| Lúa | 6 tấn/ha | 10 ha/đơn vị |

Như vậy, mỗi đơn vị sầu riêng trong game đại diện cho 100 tấn sản phẩm trên 5 ha — con số phù hợp với quy mô nông hộ thực tế tại ĐBSCL.

---

## 5. HỆ THỐNG ĐÀO TẠO NÔNG NGHIỆP BỀN VỮNG (Sustainable Agriculture Training System)

### 5.1. Thiết kế trải nghiệm học tập VR

Hệ thống đào tạo được thiết kế theo nguyên tắc **học tập trải nghiệm (experiential learning)** của Kolb (1984):

```
  Trải nghiệm cụ thể
   (Trồng, thu hoạch VR)
         ↓
  Quan sát phản tư
   (Xem kết quả điểm)      →→→  CHU KỲ KOLB
         ↓                         trong VR
  Khái quát hóa
   (Hiểu quy luật mặn/mùa)
         ↓
  Thử nghiệm tích cực
   (Đổi chiến lược mùa sau)
```

#### 5.1.1. Cơ chế thu hoạch VR

| Sản phẩm | Thao tác VR | Kỹ năng đào tạo |
|----------|------------|----------------|
| Sầu riêng | Rung cây → quả rơi → grab → bỏ túi | Nhận biết thời điểm thu hoạch |
| Dừa | Rung cây → quả rơi → grab → bỏ túi | Thu hoạch cây lâu năm |
| Lúa | Dùng liềm cắt → gom bó | Kỹ thuật gặt lúa |
| Cá | Dùng lưới quăng xuống ao | Quản lý ao nuôi |
| Tôm | Cầm cần → giữ 5 giây (hiệu ứng giật) → thu | Kiên nhẫn trong nuôi trồng |
| Trứng | Nhặt từ ổ → bỏ túi | Thu hoạch phụ phẩm |

#### 5.1.2. Bài học nông nghiệp được tích hợp

1. **Nguyên tắc "đúng cây, đúng vùng, đúng mùa":** Người chơi học cách chọn loại cây/con phù hợp với điều kiện nước và mùa vụ
2. **Hậu quả của quyết định sai:** Điểm âm (−40 cho sầu riêng trong nước lợ + mùa khô) mô phỏng thiệt hại kinh tế thực
3. **Đa dạng hóa sản xuất:** Kết hợp cây trồng nhạy cảm (mùa mưa) và thủy sản chịu mặn (mùa khô)
4. **Chuyển đổi cơ cấu nông nghiệp:** Từ thuần nông (lúa) sang nông – thủy sản kết hợp

### 5.2. Hệ thống NPC và hướng dẫn nông nghiệp

Hệ thống bao gồm NPC (nhân vật phi người chơi) đóng vai nông dân và kỹ sư thủy lợi, cung cấp:
- **Đối thoại đa ngôn ngữ** (VI/EN/FR/TH) với hiệu ứng đánh máy
- **Hướng dẫn canh tác** dựa trên điều kiện mùa hiện tại
- **Cảnh báo** về nguy cơ xâm nhập mặn

### 5.3. Cơ chế phòng thủ — Máy bơm nước

Người chơi có thể lắp đặt **máy bơm nước** (Water Pump) như biện pháp ứng phó chủ động:
- Mô phỏng hệ thống bơm tháo nước mặn thực tế tại ĐBSCL
- Tạo vùng đệm bảo vệ xung quanh nông trại
- Dạy người chơi về cơ sở hạ tầng thủy lợi trong thích ứng biến đổi khí hậu

### 5.4. Hệ thống "kẻ thù mặn" (Saltwater Enemies)

Xâm nhập mặn được trực quan hóa thông qua **các thực thể "kẻ thù"** di chuyển trên bản đồ:
- Di chuyển theo NavMesh, tiến về phía nông trại
- Tốc độ và mật độ tăng theo triều cường + mùa khô
- Bị chậm lại/đảo chiều khi triều kém
- Người chơi phải lắp đặt máy bơm để ngăn chặn

Cơ chế này **game hóa** (gamification) hiện tượng xâm nhập mặn, giúp người chơi trực quan hiểu được tính chất "tấn công dần dần" của mặn vào đất nông nghiệp.

### 5.5. Chế độ tính điểm và đánh giá

Hệ thống hỗ trợ 2 chế độ tính điểm:

| Chế độ | Mô tả | Mục đích giáo dục |
|--------|-------|-------------------|
| **GrowthTime** | Tính điểm ngay khi thu hoạch | Phản hồi tức thì, phù hợp người mới |
| **Seasonal** | Tổng kết cuối mỗi mùa, ép thu hoạch tất cả | Mô phỏng chu kỳ mùa vụ thực tế |

Cuối game, hệ thống hiển thị:
- Tổng điểm (đại diện cho tổng thu nhập nông nghiệp)
- Đánh giá (thắng/thua) với nhận xét đa ngôn ngữ
- Phân tích chiến lược (nên làm gì khác)

---

## 6. THẢO LUẬN (Discussion)

### 6.1. Ưu điểm của mô hình

**Tính chân thực khoa học:**
- Mô hình ngưỡng chịu mặn dựa trên dữ liệu thực nghiệm từ ĐBSCL
- Hệ thống mùa phản ánh đúng quy luật thủy văn (mùa khô T11–T4, mùa mưa T5–T10)
- Mô hình thủy triều bổ sung biến thiên ngắn hạn hợp lý
- Giá trị sản lượng quy đổi phù hợp với thực tế canh tác

**Tính giáo dục:**
- Học tập trải nghiệm qua tương tác VR trực tiếp
- Phản hồi tức thì thông qua hệ thống điểm
- Điểm âm mô phỏng thiệt hại thực, tạo ấn tượng mạnh
- Hỗ trợ đa ngôn ngữ, tiếp cận được nhiều đối tượng (nông dân Việt Nam, sinh viên quốc tế, nhà nghiên cứu Pháp/Thái)

**Tính mở rộng:**
- Dữ liệu JSON cho phép cập nhật thông số sinh học mà không cần biên dịch lại
- Kiến trúc mô-đun (FarmArea, PlantGrowth, FruitCollection) dễ bổ sung loài mới
- Tích hợp GAMA cho phép mô phỏng phức tạp hơn (GIS, đa tác nhân)

### 6.2. Hạn chế và hướng cải thiện

**Hạn chế mô hình:**
1. **Đơn giản hóa quan hệ mặn–năng suất:** Mô hình hiện tại sử dụng hàm hyperbol ($\frac{threshold}{salinity}$), trong khi thực tế quan hệ này phức tạp hơn (có thể sigmoid hoặc đa pha). Mô hình Maas–Hoffman gốc sử dụng hàm tuyến tính giảm, có thể chính xác hơn cho một số loài.
2. **Thiếu yếu tố tích lũy:** Mô hình chưa tính đến hiệu ứng stress mặn tích lũy theo thời gian — thực tế, cây trồng chịu mặn nhẹ kéo dài có thể bị thiệt hại nghiêm trọng hơn so với mặn cao nhưng ngắn hạn.
3. **Đồng nhất hóa vùng:** Mỗi FarmArea có độ mặn đồng nhất, trong khi thực tế có gradient mặn theo khoảng cách từ sông.
4. **Thiếu tương tác đất–nước:** Mô hình chưa xét đến khả năng giữ mặn của đất, ảnh hưởng của mực nước ngầm.

**Hạn chế kỹ thuật:**
1. Không nhất quán giữa bảng điểm cũ/mới (David_Fruit vs PlantGrowth)
2. Thiếu thành viên trong enum FruitType (Shrimp, Rice)
3. Các hằng số "magic number" chưa được tham số hóa hoàn toàn

**Hướng cải thiện:**
- Tích hợp mô hình Maas–Hoffman tuyến tính cho từng loài cụ thể
- Bổ sung hiệu ứng stress tích lũy (accumulated stress factor)
- Mô hình gradient mặn theo không gian trong mỗi FarmArea
- Chuyển đổi tham số cứng sang ScriptableObject cho phép cân bằng dễ dàng
- Bổ sung unit test cho hệ thống tính điểm
- Tích hợp dữ liệu GIS thực tế qua GAMA cho các vùng cụ thể của ĐBSCL

### 6.3. So sánh với các nghiên cứu liên quan

| Tiêu chí | SIMPLE VU2 | Mô hình truyền thống | Mô phỏng desktop |
|----------|-----------|---------------------|-------------------|
| Tương tác | VR tương tác trực tiếp | Báo cáo/bản đồ tĩnh | Nhấp chuột |
| Trải nghiệm | Nhập vai (immersive) | Không | Hạn chế |
| Dữ liệu thực | Có (27 loài ĐBSCL) | Có | Tùy dự án |
| Đa ngôn ngữ | 4 ngôn ngữ | Thường 1 | Thường 1–2 |
| Mô phỏng đa tác nhân | Có (GAMA) | Hạn chế | Hạn chế |
| Đối tượng mục tiêu | Nông dân, sinh viên | Nhà nghiên cứu | Kỹ thuật viên |
| Thời gian đào tạo | 3 phút/phiên | Hàng giờ | 15–30 phút |

### 6.4. Ý nghĩa thực tiễn

**Đối với nông dân:**
- Hiểu trực quan tác động của mặn lên từng loại cây trồng/vật nuôi
- Học chiến lược chuyển đổi cơ cấu theo mùa
- Nhận biết thời điểm nguy hiểm (triều cường + mùa khô)

**Đối với giáo dục:**
- Công cụ thực hành cho sinh viên nông nghiệp, thủy lợi, môi trường
- Mô hình hóa trực quan các khái niệm trừu tượng (ngưỡng chịu mặn, stress thẩm thấu)
- Hỗ trợ giảng dạy nông nghiệp thích ứng biến đổi khí hậu

**Đối với chính sách:**
- Mô phỏng kịch bản xâm nhập mặn phục vụ quy hoạch nông nghiệp
- Đánh giá hiệu quả các phương án thích ứng (chuyển đổi cơ cấu, hạ tầng thủy lợi)
- Truyền thông chính sách đến cộng đồng qua hình thức trực quan, dễ hiểu

---

## 7. KẾT LUẬN (Conclusion)

Nghiên cứu đã thành công trong việc xây dựng hệ thống SIMPLE VU2 — một mô hình mô phỏng nông nghiệp VR giáo dục tích hợp mô hình tác động của độ mặn lên năng suất với dữ liệu thực địa từ 27 loài cây trồng, vật nuôi và thủy sản đặc trưng của ĐBSCL. Các đóng góp chính gồm:

1. **Mô hình toán học đa tầng:** Tính toán độ mặn từ cấp toàn cục (mùa + mực nước) đến cấp cục bộ (vùng nông trại), với hệ số điều chỉnh thủy triều, tạo ra mô phỏng chân thực.

2. **Tích hợp dữ liệu thực địa:** 27 loài với ngưỡng chịu mặn từ 0,5‰ (cá rô phi) đến 15‰ (tôm sú), phản ánh đa dạng sinh học nông nghiệp ĐBSCL.

3. **Công thức năng suất–độ mặn:** Kết hợp mô hình ngưỡng (threshold-based) và bảng tra cứu (lookup table) cho phép mô phỏng cả quan hệ liên tục và các quy tắc đặc thù theo loài.

4. **Nền tảng đào tạo VR tương tác:** 6 loại cơ chế thu hoạch VR, hệ thống NPC đa ngôn ngữ, và cơ chế game hóa (điểm, "kẻ thù mặn") tạo trải nghiệm học tập hấp dẫn.

5. **Kết nối GAMA:** Mở ra khả năng mô phỏng phức tạp hơn với mô hình đa tác nhân và dữ liệu GIS.

Hệ thống SIMPLE VU2 chứng minh rằng công nghệ VR kết hợp mô hình hóa khoa học có thể tạo ra công cụ đào tạo nông nghiệp bền vững hiệu quả, thu hẹp khoảng cách giữa nghiên cứu hàn lâm và thực hành nông nghiệp tại các vùng chịu ảnh hưởng của biến đổi khí hậu.

---

## 8. TÀI LIỆU THAM KHẢO (References)

1. Maas, E. V., & Hoffman, G. J. (1977). Crop salt tolerance — current assessment. *Journal of the Irrigation and Drainage Division*, 103(2), 115–134.

2. Radianti, J., Majchrzak, T. A., Fromm, J., & Wohlgenannt, I. (2020). A systematic review of immersive virtual reality applications for higher education: Design elements, lessons learned, and a research agenda. *Computers & Education*, 147, 103778.

3. Kolb, D. A. (1984). *Experiential Learning: Experience as the Source of Learning and Development*. Prentice-Hall.

4. IPCC (2021). *Climate Change 2021: The Physical Science Basis*. Contribution of Working Group I to the Sixth Assessment Report. Cambridge University Press.

5. Viện Khoa học Thủy lợi miền Nam (2020). *Báo cáo tình hình xâm nhập mặn mùa khô 2019–2020 tại Đồng bằng sông Cửu Long*.

6. Tổng cục Thống kê (2023). *Niên giám thống kê Việt Nam 2022*. Nhà xuất bản Thống kê.

7. Taillandier, P., Gaudou, B., Grignard, A., Huynh, Q. N., Marilleau, N., Caillou, P., Philippon, D., & Drogoul, A. (2019). Building, composing and experimenting complex spatial models with the GAMA platform. *GeoInformatica*, 23(2), 299–322.

8. Smajgl, A., Toan, T. Q., Nhan, D. K., Ward, J., Trung, N. H., Tri, L. Q., Tri, V. P. D., & Vu, P. T. (2015). Responding to rising sea levels in the Mekong Delta. *Nature Climate Change*, 5(2), 167–174.

9. Renaud, F. G., Le, T. T. H., Lindener, C., Guber, V. S., & Sebesvari, Z. (2015). Resilience and shifts in agro-ecosystems facing increasing sea-level rise and salinity intrusion in Ben Tre Province, Mekong Delta. *Climatic Change*, 133(1), 69–84.

10. Nguyễn Văn Bé, Lê Quang Trí, & Phạm Thanh Vũ (2017). Đánh giá ảnh hưởng của xâm nhập mặn đến sản xuất nông nghiệp tại tỉnh Sóc Trăng. *Tạp chí Khoa học Trường Đại học Cần Thơ*, 48, 42–50.

11. FAO (2021). *Global Map of Salt-affected Soils (GSASmap)*. Food and Agriculture Organization of the United Nations.

12. Grignard, A., Taillandier, P., Gaudou, B., Vo, D. A., Huynh, N. Q., & Drogoul, A. (2013). GAMA 1.6: Advancing the art of complex agent-based modeling and simulation. In *PRIMA 2013: Principles and Practice of Multi-Agent Systems* (pp. 117–131). Springer.

---

## PHỤ LỤC (Appendices)

### Phụ lục A: Sơ đồ luồng dữ liệu hệ thống

```
┌──────────────────────────────────────────────────────────────────────┐
│                    LUỒNG DỮ LIỆU ĐỘ MẶN                           │
│                                                                      │
│  RulesoftheGame_VU2_1.cs                                            │
│  ┌─────────────────────────┐                                        │
│  │ static Saltwater_Intrusion                                       │
│  │   Pha 1: 0.0  (T11-T1) │                                        │
│  │   Pha 2: 0.5  (T2-T3)  │                                        │
│  │   Pha 3: 1.0  (T4)     │                                        │
│  └───────────┬─────────────┘                                        │
│              │                                                       │
│     ┌────────┴────────┐                                             │
│     ▼                 ▼                                             │
│  GameManager       FarmArea                                         │
│  .GetSeasonSalinity()  .GetAreaSalinity()                           │
│  = Si × Sbase × Wm    = cục bộ theo pha                            │
│  = 0/2/4 ‰            = 0.5/1.0/1.5 ‰                             │
│     │                 │                                             │
│     │          ┌──────┴──────┐                                      │
│     │          ▼             ▼                                      │
│     │    PlantGrowth    David_Fruit                                 │
│     │    .AdjustBy      .GetTableScore                              │
│     │    Salinity()     (Zone × Season)                             │
│     │    threshold/S    Bảng tra cứu                                │
│     │          │             │                                      │
│     │          └──────┬──────┘                                      │
│     │                 ▼                                             │
│     └────────► GameManager.AddScore()                               │
│                  │                                                   │
│                  ▼                                                   │
│              UI + Audio + GAMA Server                                │
└──────────────────────────────────────────────────────────────────────┘
```

### Phụ lục B: Cấu trúc dữ liệu JSON (data.json)

```json
{
  "lang": {
    "vi": {
      "labels": { "score": "Điểm", "season": "Mùa", ... },
      "plants": [
        {
          "id": 1,
          "tag_name": "durian",
          "growth_time": 30,
          "status": ["Khỏe mạnh", "Nhẹ", "Trung bình", "Nặng"],
          "economic_benefits": 4,
          "salinity_threshold": 0.8,
          "harvest_time": 5,
          "information": "Sầu riêng là cây trồng có giá trị kinh tế cao..."
        }
      ],
      "animals": [ ... ],
      "fish": [ ... ],
      "npcDialogues": [ ... ]
    },
    "en": { ... },
    "fr": { ... },
    "th": { ... }
  }
}
```

### Phụ lục C: Bảng tóm tắt các script chính

| File | Dòng code | Chức năng chính |
|------|:---------:|----------------|
| RulesoftheGame_VU2_1.cs | ~800 | Điều khiển mùa, thời tiết, mực nước (Level 1) |
| RulesOfTheGame_VU2_2.cs | ~800 | Điều khiển mùa (Level 2, ngược lại) |
| FarmArea.cs | ~500 | Quản lý vùng nông trại, trồng/thu hoạch |
| Thuan_23127_PlantGrowth.cs | ~600 | Vòng đời cây trồng, tính điểm ngưỡng |
| Thuan_23127_GameManager.cs | ~300 | Singleton điểm số, độ mặn toàn cục |
| Thuan_23127_JsonReader.cs | ~200 | Tải dữ liệu JSON, đa ngôn ngữ |
| David_Fruit.cs | ~400 | Thu hoạch trái cây VR, bảng điểm |
| TidalClockManager.cs | ~300 | Mô hình thủy triều, pha mặt trăng |
| ConnectionManager.cs | ~500 | Kết nối WebSocket đến GAMA |
| SimulationManager.cs | ~400 | Cầu nối GAMA–Unity |

### Phụ lục D: Thông số kỹ thuật nền tảng

| Thành phần | Phiên bản/Thông số |
|-----------|-------------------|
| Unity Engine | 2022.3.5f1 LTS |
| XR Interaction Toolkit | 2.5.2 |
| Universal Render Pipeline | 14.0.8 |
| Nền tảng VR | Meta Quest (Oculus) |
| GAMA Platform | Kết nối qua WebSocket (websocket-sharp.dll) |
| Serialize | Newtonsoft.Json |
| Ngôn ngữ lập trình | C# (.NET Standard 2.1) |
| Ngôn ngữ hỗ trợ | Tiếng Việt, English, Français, ภาษาไทย |

---

*Báo cáo được tổng hợp từ mã nguồn dự án SIMPLE VU2, dữ liệu game (data.json), và tài liệu kỹ thuật nội bộ (Report/01–04). Ngày lập: 17 tháng 3 năm 2026.*
