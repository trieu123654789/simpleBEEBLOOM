# Let It Bee — Honey Pilot

Package hai hoa, một cục mật đang mang và bảy ô tổ trong scene SCN_LetItBee.
Luồng hiện hành: Unity → SIMPLE WebPlatform → LinkToUnity → LetItBeeVR.

Mã Unity được chia thành `Gameplay`, `Presentation`, `Integration` và
`Compatibility` trong `Assets/Scripts/VU2/HoneyPilot/`. Xem
[kiến trúc dự án](../../Docs/Architecture.md) để tra trách nhiệm từng phần.
Các script khởi chạy vẫn nằm tại thư mục này; đường dẫn model và checkpoint
không thay đổi theo việc sắp xếp mã Unity.

Kế hoạch, tiến độ và hướng dẫn được hợp nhất tại **[Docs/LetItBee.md](../../Docs/LetItBee.md)**:

- [Chạy, đổi IP và đường dẫn](../../Docs/LetItBee.md#4-chạy-simple-và-cấu-hình-chuyển-máy).
- [Thụ phấn, lấy/cầm/thả/gắn mật](../../Docs/LetItBee.md#3-vòng-chơi-thử-hiện-tại).
- [Tự hồi mật, reset lượt thử và đổi bố trí level](../../Docs/LetItBee.md#hồi-mật-để-thử-lại-và-đổi-bố-trí-level): dùng `Reset-HoneyPilot.ps1`; có backup tự động, không sửa scene.
- [Giao thức và mã nguồn](../../Docs/LetItBee.md#5-kiến-trúc-và-giao-thức-unitygama).
- [Kiểm chứng và giới hạn](../../Docs/LetItBee.md#9-kiểm-chứng-và-giới-hạn).

`HoneyPilot` read-only và direct cổng 6873 còn phục vụ chẩn đoán;
`LetItBeeVR` qua SIMPLE là luồng phát triển chính. Hướng dẫn đầy đủ ở cùng tài liệu trên.
