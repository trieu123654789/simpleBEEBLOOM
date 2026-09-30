# Hướng Dẫn Chơi

## Mùa

**Mùa Mưa**: `Saltwater_Intrusion < 1.0` (độ mặn thấp)  
**Mùa Khô**: `Saltwater_Intrusion >= 1.0` (độ mặn cao)

## Bảng Điểm

| Sản Phẩm | Ngọt+Mưa | Ngọt+Khô | Lợ+Mưa | Lợ+Khô |
|----------|----------|----------|--------|--------|
| Sầu Riêng | 100 | 80 | 60 | **-40** |
| Dừa | 100 | 80 | 60 | 50 |
| Cá | 10 | 20 | 30 | 40 |
| Tôm | 20 | 20 | 20 | 20 |
| Lúa | 60 | **-20** | 40 | 20 |
| Trứng | 3 | 3 | 3 | 3 |

**Điểm âm** = Cây chết, mất mùa.

## Chiến Lược

**Mùa Mưa**: Trồng Sầu/Dừa (Nước Ngọt) → 100 điểm  
**Mùa Khô**: Nuôi Cá (Nước Lợ) → 40 điểm  
**Tránh**: Sầu ở Lợ+Khô (-40), Lúa ở Ngọt+Khô (-20)

## Code References

- Tính điểm: `David_Fruit.GetTableScore()`
- Hiển thị Details: `RulesoftheGame_VU2_1.ShowResultDetailsScore()`
- Tracking: `Thuan_23127_SeasonalSummary`
