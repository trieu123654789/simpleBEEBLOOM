# Let It Bee VR

Dự án game ong VR trên Unity, kế thừa môi trường và hạ tầng SIMPLE.
Hiện đang chuẩn hóa nền phát triển và thử vòng hoa → mật → tổ với GAMA.

**[Tài liệu chính: kế hoạch, tiến độ, cấu hình và hướng dẫn](Docs/LetItBee.md)**

## Hiện đang có

- Bay bằng hai ray quẹt, nhịp nâng/lướt, trigger trái phanh/giữ vị trí;
  va chạm và phản hồi bay sát bề mặt, âm thanh cánh.
- Hai hoa, một cục mật đang mang, bảy ô tổ; GAMA xác nhận tài nguyên, hồi mật và giao dịch.
- Unity → SIMPLE WebPlatform → LinkToUnity → GAMA; endpoint ngoài APK,
  đường dẫn checkpoint cấu hình trên server.
- APK có bộ đọc cấu hình ngoài; đã sửa và xác nhận mở được cảnh trên Quest,
  còn kiểm thử vòng tương tác và đậu hoa trên kính.

Mục tiêu chính 5 cục/lượt, màn 2 xâm nhập mặn, NPC và cutscene còn trong kế hoạch.
Không coi mô hình pilot là dữ liệu sinh thái đã hiệu chuẩn.

## Bắt đầu

1. Dùng Unity **6000.6.2f1**, mở `Assets/Scenes/VU2/SCN_LetItBee.unity`.
2. Theo [hướng dẫn chạy và cấu hình](Docs/LetItBee.md#4-chạy-simple-và-cấu-hình-chuyển-máy)
   để mở SIMPLE/GAMA trước khi Play.
3. Xem [thao tác lấy/gắn mật](Docs/LetItBee.md#3-vòng-chơi-thử-hiện-tại),
   [chỗ chỉnh bay/âm thanh](Docs/LetItBee.md#6-chỉnh-bay-va-chạm-và-âm-thanh)
   và [backlog](Docs/LetItBee.md#7-backlog-và-asset).

Giữ nguyên dữ liệu người dùng vừa Ctrl+S; không áp lại scene/preset cũ.
Trạng thái, kết quả kiểm tra và giới hạn được theo dõi tại tài liệu chính.
Lab direct WebSocket cũ ở `Experiments/LetItBee-GAMA` là thử nghiệm riêng.

## Dự án

- Đơn vị thực hiện đã ghi trong dự án: nhóm nghiên cứu và phát triển VR,
  Trường Đại học Cần Thơ (CTU).
- Kho lưu trữ: [simpleBEEBLOOM](https://github.com/trieu123654789/simpleBEEBLOOM).
- Hạ tầng tham khảo: [project-SIMPLE](https://github.com/project-SIMPLE/simple.toolchain).
