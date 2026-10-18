# Bee Bloom — Unity ↔ GAMA lab

> **Lab lịch sử, không phải luồng game hiện tại.** Tài liệu này mô tả demo desktop direct WebSocket riêng.
> Tiến độ và kết nối Bee Bloom hiện hành xem [tài liệu chính](../../Docs/BeeBloom.md).

Demo thử nghiệm độc lập, ngày 18/10/2026. Không sửa scene, prefab, HUD, camera, thông số bay hoặc Build Settings của project Bee Bloom chính.

## Mục đích và giới hạn

Kiểm chứng Unity và GAMA chạy đồng thời, truyền vị trí và tương tác hai chiều. Đây là desktop lab dùng hình khối đơn giản; chưa tích hợp kính Quest, cơ chế bay thật, assets hay mạng LAN. Thụ phấn được đánh dấu khi lấy mật chỉ để thử giao thức; chưa mô phỏng thụ phấn chéo sinh học.

Demo kết nối trực tiếp GAMA Server qua WebSocket. **Chưa phải triển khai toàn bộ SIMPLE WebPlatform / plugin / VR template.** Giữ cách chia trách nhiệm của SIMPLE: Unity hiển thị và gửi tương tác; GAMA chạy mô hình và trả kết quả. Transport riêng tránh singleton/PlayerPrefs và giao thức SIMPLE cũ trong project chính.

`stress` là biến minh họa 0–1 làm đổi màu hoa và tốc độ hồi mật. Không có đơn vị độ mặn; không phải số liệu đo, ngưỡng sinh học hay kết luận thiên địch tăng vì xâm nhập mặn.

## Chạy nhanh

Nhấp đúp `Chay-Demo.cmd` trong thư mục này để mở hai ứng dụng. Nếu demo đang chạy, lệnh giữ cửa sổ hiện tại, không mở phiên Unity thứ hai.

Trong PowerShell, tại thư mục project chính:

```powershell
& .\Experiments\BeeBloom-GAMA\Start-Demo.ps1
```

Lệnh mở **GAMA desktop riêng** (cổng 6872, workspace `Runtime/gama-view-workspace`) rồi mở Unity player. Khi GAMA khởi tạo thư viện lần đầu, nếu hiện câu hỏi cập nhật thư viện của workspace demo thì chọn Yes. Không cần đóng Unity Editor hay GAMA đang làm việc.

Đợi Unity báo `LIVE` và cycle tăng. Trong GAMA xem `BeeBloomMap`, các monitor `Carried chunk`, `Hive slots / 5`, `Pollination visits`, `Unity position`.

Nếu muốn chỉ chạy bộ máy GAMA nền:

```powershell
& .\Experiments\BeeBloom-GAMA\Start-Demo.ps1 -Headless
```

GAMA headless ở cổng 6871, không có bản đồ IDE. Đây vẫn là mô hình GAMA thật, không phải mock.

## Điều khiển

| Thao tác | Phím/nút |
|---|---|
| Đi trên mặt phẳng | WASD |
| Lên / xuống | E / Q |
| Lấy một cục mật từ hoa gần nhất | Space / Collect nearest flower |
| Nạp cục mật vào tổ | H / Deposit at hive |
| Đến hoa còn mật để thử nhanh | Go to next available flower |
| Về tổ để thử nhanh | Return to hive |
| Đổi biến môi trường minh họa | Toggle illustrative vegetation stress |

Mỗi lần mang 1 cục; đủ 5 cục thì hiện `ROUND COMPLETE`. Hoa chuyển xanh khi được đánh dấu thụ phấn. Phải đứng gần hoa/tổ; bấm nút khi ở xa sẽ không cộng số. Các nút di chuyển nhanh chỉ phục vụ kiểm chứng kết nối, không phải cơ chế gameplay chính.

Tắt cửa sổ Unity khi xong. Với GAMA desktop, đóng riêng cửa sổ có tên workspace demo. GAMA headless được giữ để chạy lại nhanh; PID lưu ở `Runtime/gama-server.pid`. Không tắt mọi `java.exe`/`javaw.exe` vì có thể là phiên làm việc khác.

## Mở và sửa demo

- Mở **project riêng** `UnityDemo` bằng Unity 6000.6.2f1.
- Scene: `UnityDemo/Assets/Scenes/BeeBloomGamaDemo.unity`.
- Script điều khiển/hiển thị: `UnityDemo/Assets/BeeBloomDemo.cs`.
- Client kết nối: `UnityDemo/Assets/GamaClient.cs`.
- Mô hình GAMA: `GAMA/BeeBloomDemo.gaml`, experiment `BeeBloomLink`.
- Editor menu `Bee Bloom Lab/Create demo scene` tạo lại scene **của lab**; không dùng nếu muốn giữ chỉnh sửa thủ công trong scene lab.
- `Build-Demo.ps1` cũng tạo lại scene lab. Không tác động scene Bee Bloom chính.

Build lại và kiểm thử:

```powershell
& .\Experiments\BeeBloom-GAMA\Build-Demo.ps1
& .\Experiments\BeeBloom-GAMA\Start-Demo.ps1 -SelfTest
```

Script build lấy hai DLL đã có trong project chính (`websocket-sharp`, `Newtonsoft.Json`); không đổi Packages/manifest.json chính. Các DLL copy, Library, Build, runtime caches/logs bị loại khỏi Git. Cần import project chính một lần nếu máy mới chưa có package cache. Giữ giấy phép của các thư viện khi phân phối bản build.

## Luồng dữ liệu và trách nhiệm

1. Unity mở socket → nhận `ConnectionSuccessful` → `load` model → lưu `exp_id` và `socket_id` → `play`.
2. Khoảng 10 lần/giây, Unity gửi `ask set_pose`, các thao tác đang chờ rồi đọc `expression` snapshot.
3. GAMA kiểm tra khoảng cách, lượng mật và cargo; cập nhật `collect` / `deposit`. `event_id` tăng dần giúp bỏ qua gói tương tác lặp trong một phiên.
4. Unity chỉ cập nhật số mật, tổ, màu hoa và vị trí ong hiển thị từ snapshot GAMA. Không cộng điểm trước phản hồi.
5. Gói có `request_id`; phản hồi lỗi/timeout/ngắt socket được báo rõ. Reconnect tạo experiment demo mới và reset tiến độ — chưa có lưu/khôi phục phiên.

Quy ước demo: 1 đơn vị = 1 mét; Unity `(x,y,z)` → GAMA `(x,z,y)`; y của Unity là độ cao. Hai bản đồ có thể đặt hướng nhìn khác nhau, nhưng số tọa độ dùng quy ước này.

Snapshot v1 là mảng:

```text
[cycle, x, map_y, altitude, cargo, stored, visits, stress,
 [[flower_id, x, map_y, nectar, pollinated], ...]]
```

Actions GAMA: `set_pose(px,py,pz)`, `collect(flower_id,event_id)`, `deposit(event_id)`, `set_stress(value)`, target `simulation[0]`. `args` là chuỗi JSON theo API GAMA 2025.6.4. Đây là giao thức lab; trước khi ghép game chính nên chuyển sang snapshot có tên trường/version/session và handshake tương thích SIMPLE.

## Kiểm chứng

`Runtime/unity-test-result.json` được ghi bởi **Unity standalone player**, không phải Python giả lập Unity. Kiểm tra khởi tạo, tọa độ/độ cao, lấy mật, đánh dấu hoa, nạp tổ, khoảng cách, gửi lặp, biến môi trường, cycle chạy độc lập, ngắt kết nối và tạo phiên mới.

`probe.py` chỉ là công cụ chẩn đoán GAMA bổ sung, không thay thế kiểm thử Unity. Log: `Runtime/unity-build.log`, `unity-player.log`, `unity-self-test.log`, `gama-server.log`, `gama-desktop.log`.

Chưa xác minh ở đây: APK Quest, XR, SIMPLE WebPlatform, nhiều người chơi, reconnect giữ trạng thái, mạng LAN, đồng bộ bản đồ Bee Bloom thật, số liệu sinh thái thực nghiệm.

## Tài liệu đã đối chiếu

- [Unity Template VR / Unity-6](https://github.com/project-SIMPLE/simple.toolchain/tree/Unity-6/Unity%20Template%20VR): template hiện tại dẫn sang website tài liệu mới, yêu cầu phiên bản Unity riêng. Demo dùng Unity đã cài, không nâng/hạ project chính.
- [Wiki Tutorials](https://github.com/project-SIMPLE/simple.toolchain/wiki/01-Tutorials): URL cũ không tải được; đọc [bản Overview chính thức thay thế](https://github.com/project-SIMPLE/simple.toolchain.documentation/blob/main/docs/07-tutorials/01-Tutorials.md) và các bước 1–3 trong cùng thư mục.
- [Importing Data from GAMA](https://github.com/project-SIMPLE/simple.toolchain/wiki/02-Importing-Data-from-GAMA): import hình học vào Unity Editor.
- [Export Data from Unity](https://github.com/project-SIMPLE/simple.toolchain/wiki/03-Export-Data-from-Unity): export hình học sang GAMA; khác với cập nhật trạng thái gameplay liên tục.
- [SIMPLE Quick Start](https://doc.project-simple.eu/overview/quick-start): triển khai SIMPLE đầy đủ có GAMA, WebPlatform và Unity.
- [GAMA Server API](https://gama-platform.org/wiki/HeadlessServer): handshake, load, play, step, ask, expression, phản hồi và vòng đời kết nối.

Các tài liệu được dùng làm tham khảo kỹ thuật, không phải quyền ghi đè scene/thông số người dùng.

## Kết quả kiểm chứng lịch sử — 18/10/2026

Gộp từ VALIDATION.md của lab; không phải kiểm thử bản VR/SIMPLE hiện tại.

- Windows standalone Unity 6000.6.2f1 build thành công, mã thoát 0,
  marker `DEMO_BUILD_OK`.
- Player nối GAMA desktop `BeeBloomLink`, từng hiện LIVE. Ảnh do Unity chụp
  sau sửa camera ở `Runtime/unity-demo.png` đã được xem; chữ/nút rõ, không lưu vết.
- Đọc độc lập từ GAMA thấy 1 ong, 10 hoa, vị trí `(20,20,2)`;
  cycle 7297 → 7307 sau khoảng một giây, tương ứng 10 bước/giây.
- Unity standalone tự kiểm tra lúc **15:34:40 giờ Việt Nam, 18/10/2026**
  (`2026-10-18T08:34:40Z`): **31 kiểm tra qua**,
  kết quả ở `Runtime/unity-test-result.json`.
- Các nhóm: khởi tạo, trục/độ cao, lấy mật/đánh dấu hoa, nạp đủ 5 cục,
  kiểm tra khoảng cách, chống gói lặp, biến stress minh họa, cycle độc lập,
  báo ngắt kết nối và reconnect tạo phiên mới.

Các kết quả thuộc lab riêng, không sửa scene/prefab/HUD/bay của game chính.
Giới hạn XR, LAN, SIMPLE, lưu/khôi phục và dữ liệu sinh thái của lab đã nêu ở trên;
không dùng `stress` hoặc thao tác đánh dấu hoa của lab làm bằng chứng sinh thái.
