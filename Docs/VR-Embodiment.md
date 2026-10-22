# VR embodiment prototype — 22/10/2026

Đợt này triển khai bảy giai đoạn: hệ quy chiếu sàn, tỉ lệ, tiếp cận cành,
lơ lửng, hoạt cảnh đậu/rời cành, kiểm tra GAMA và âm thanh. Bản thử cần được
đánh giá trực tiếp trên Quest trước khi chốt cảm giác hoặc áp cho các hoa khác.

## Current implementation: balanced flight

Sau phản hồi rơi quá nhanh, bản này dùng nhịp **quẹt → nâng → lượn → hạ có giới hạn**.
Đây là tuning cảm giác chơi, không phải mô phỏng lực khí động học sinh học.
Lực nâng đi qua envelope 0,3 s; sau nhịp cuối, lực nâng dư giảm trong 0,3 s.
Vận tốc được tích phân liên tục, không đặt lại bằng zero lúc ngừng quẹt.
Một nhịp hãm phần lớn đà rơi thông thường; nhịp kế tiếp đưa ong bay lên.
Hai tay cùng quẹt nhận bonus 25% so với tổng hai nhịp, vẫn giới hạn tốc độ đi lên.

Đà ngang lưu theo world space. Nhả joystick rồi quay đầu không bẻ hướng đang trôi;
đẩy joystick mới điều hướng dần. Kéo ngược cần sẽ hãm trước khi lùi. Giữ nguyên
các tốc độ tiến/lùi/ngang và scale đã lưu. Rời cánh hoa có lực nâng nhỏ trải qua
cùng envelope; đi trên cánh, mép chống rơi và latch trigger giữ như bản trước.
Âm cánh giảm nhẹ khi lượn; haptic quẹt và hiệu ứng sát mặt nước dùng các thành phần
hiện có. Không thêm âm gió hoặc thay file âm người dùng.

Chọn **BeeRoot → Bee Controller** trong scene `SCN_LetItBee`:

| Inspector parameter | Value | Purpose |
| --- | --- | --- |
| Balanced World Flight | On | Bật mô hình bay cân bằng; giữ tên serialized cũ để tương thích |
| **Max Descent Speed** | **0.9 m/s** | Hạ thấp nếu vẫn thấy rơi nhanh |
| **Stroke Lift Strength** | **0.95** | Lực nâng tổng mỗi nhịp, nhân strength của swipe |
| **Glide Transition Time** | **0.3 s** | Thời gian chuyển từ nâng dư sang hạ |
| Descent Acceleration | 1.4 m/s² | Tốc độ tăng vận tốc hạ |
| Fall Air Drag | 0.12 /s | Cản đứng |
| Stroke Lift Duration | 0.3 s | Trải lực nâng theo nhịp quẹt |
| Paired Lift Bonus | 0.25 | Bonus cho hai nhịp được chấp nhận |
| Petal Takeoff Lift | 0.45 m/s | Tổng lực nâng bổ sung lúc chủ động rời cánh |
| Flap Recovery Acceleration | 1.6 m/s² | Bay liên tục bằng phím/nút thử nghiệm |

Ba dòng in đậm là ba thông số nên chỉnh trước, từng thông số một. Các giá trị này
là world units; không nhân thêm `Player Scale`. Tốc độ ngang và trần tốc độ bay lên
vẫn theo `BeeEmbodiment`; với scale 0,15 hiện tại, tốc độ lên tối đa là 0,9 m/s.
Các trường comfort/rest-glide cũ được giữ để tương thích dữ liệu, không chồng vào
mô hình đang bật. Không đổi HUD, camera, cảnh, input binding, cấu hình mạng hay checkpoint.

Kiểm tra bằng cấu hình scene thật và chạy `BeeEmbodiment.Start` ở 30/72/90/120 FPS:

- Sau một giây từ nghỉ: hạ 0,607–0,618 m, vận tốc đạt trần 0,9 m/s.
- Từ tốc độ rơi tối đa: một nhịp đưa về khoảng -0,18 m/s; nhịp thứ hai lên khoảng +0,51 m/s.
- Quẹt một tay mỗi 0,6 s: mô phỏng nâng được khoảng 23,6–23,8 m trong 30 s, không bị từ chối do stamina. Đây là đường bay trống, không tính vật cản/trần 25 m.
- Rời hoa: lực nâng bổ sung làm tăng khoảng 8,5 cm trong 0,4 s, chưa tính chuyển tiếp rời cánh có sẵn.
- 70 kiểm tra mới + 250 kiểm tra đi cánh/va chạm + 352 kiểm tra hồi quy đều đạt (672 tổng).
- So sánh 1.637 component: chỉ `BeeController` đổi 5 giá trị đã duyệt và thêm 3 trường; không thêm/xóa component. ProjectSettings và checkpoint giữ nguyên.

Kiểm tra GAMA ở đợt này là hồi quy codec/transport và luồng tương tác cục bộ;
không chạy lại phiên GAMA thật hoặc thử qua Quest. Kết quả GAMA thật ở bản trước
được ghi bên dưới. Cảm giác thoải mái, input tay thật và tốc độ khung hình trên
Quest cần thử bằng APK mới; các kiểm tra số không thay thế việc đeo kính.

## Latest APK: balanced flight

`Experiments/LetItBee-HoneyPilot/Build/LetItBee-Quest-balanced-flight.apk`

Android ARM64 build completed 22/10/2026 at 23:17 (Asia/Saigon), 0 errors,
495 warnings. APK size: 114,155,473 bytes (~109 MiB).
SHA256: `87e4bc637feeabb6695907ca207071e26a0bda250f505571a2693b23f8151616`.
Balanced flight, takeoff envelope and petal collision types confirmed in the APK metadata.
After build: all 1,637 scene components match the validated scene; ProjectSettings
match the pre-build copy; scene saved with no pending edits. Editor console has
no errors. No installation or headset comfort/performance test was performed.

```powershell
cd C:\Projects\Unity\simple.CTU.VU2\Experiments\LetItBee-HoneyPilot
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Connect-Quest.ps1 -InstallApk .\Build\LetItBee-Quest-balanced-flight.apk
```

Thử: quẹt đều để lên → ngừng quẹt và lượn xuống → một nhịp hãm, nhịp tiếp theo lên lại.
Sau đó nhả joystick và nhìn sang bên để kiểm tra đà không tự bẻ hướng; kéo cần
ngược để hãm. Đậu, đi quanh cánh, lấy mật rồi bấm trigger trái rời hoa. Giữ
quy trình SIMPLE/GAMA và checkpoint hiện có, không cần reset tiến độ.

## Previous implementation: walking on petals and world gravity

Đợt sửa tiếp theo thay việc neo tại điểm đậu bằng di chuyển trên mặt hoa.
Trigger trái vẫn bấm để đậu, bấm lại để rời. Khi đã đậu, joystick trái/WASD
đi quanh cánh trắng và qua nhụy theo hướng nhìn ngang; nhả cần thì dừng.
Tới mép hoặc khe thiếu mặt đỡ thì dừng/trượt dọc mép, giữ latch đến khi bấm rời.
Đang đi thì tạm ngừng tích tiến độ hút mật; đứng lại và giữ trigger phải để tiếp tục.

`BeeFlowerSurface` trên nhóm `Bee Perch A/B` tạo MeshCollider từ mesh skinned
đang chạy, bao phủ toàn hoa thay cho các ô nhỏ rời rạc. Mesh cập nhật 15 Hz khi
ong ở gần (3 m) hoặc đang đậu, mỗi hoa hiện 2.021 tam giác. Các BoxCollider điểm
đậu cũ vẫn lưu trong scene, tạm tắt khi dùng mặt hoa mới và khôi phục khi tắt component.
Mỗi bước đi chia nhỏ tối đa 8 mm; dò tâm và bốn hướng quanh chân để giữ mép.
Điểm đứng dùng triangle/barycentric để theo cánh biến dạng, tải trọng chuyển theo
cánh và khoảng cách từ gốc. Chuyển động đàn hồi được bù ở rig, không ghi vào pose đầu.

Lực rơi mới dùng 9,81 m/s² theo world space, độc lập `Player Scale`. Lực cản
đứng nhẹ 0,12/s; sau một giây từ nghỉ, tốc độ khoảng 9,24 m/s. Giới hạn rơi
20 m/s dùng đơn vị thế giới. Ngừng quẹt thì lực nâng của nhịp hiện tại hoàn tất,
trọng lực tiếp tục tác dụng; không còn khoảng tự giữ độ cao/hạ chậm. Đà ngang
và điều hướng khi xuống tạo đường lượn. Quẹt/nút bay tăng lực nâng để bắt lại
vận tốc dần; không đặt vận tốc rơi về zero ngay. Đây là điều chỉnh cảm giác
chơi; chưa xác nhận là mô hình khí động sinh học của ong.

| Select in Inspector | Parameter | Current value |
| --- | --- | --- |
| BeeRoot → `BeeController` | Use World Gravity | On |
| BeeRoot → `BeeController` | Gravity Acceleration | 9.81 m/s² |
| BeeRoot → `BeeController` | Fall Air Drag | 0.12 /s |
| BeeRoot → `BeeController` | Terminal Fall Speed | 20 m/s |
| BeeRoot → `BeeController` | Flap Recovery Acceleration | 12 m/s² net upward recovery, before air drag |
| BeeRoot → `BeeController` | Stroke Recovery Scale | 3, world recovery gain per unscaled stroke lift unit |
| Honey pilot → `BeeHoneyPilot` | Enable Petal Walking | On |
| Honey pilot → `BeeHoneyPilot` | Petal Walk Speed / Walk Acceleration | 0.22 m/s / 0.8 m/s² |
| Honey pilot → `BeeHoneyPilot` | Surface Probe Distance / Edge Margin | 0.35 m / 0.008 m |
| Honey pilot → `BeeHoneyPilot` | Max Surface Step Height | 0.04 m |
| Bee Perch A/B → `BeeFlowerSurface` | Collision Refresh Rate | 15 Hz |

Các thông số comfort/rest-glide cũ còn lưu để tương thích, không chi phối chế độ
`Use World Gravity`. Không thay scale, HUD, camera tuning, binding hay checkpoint.
Kiểm tra tự động trên Editor và GAMA ghi ở phần APK bên dưới. Cần test trên Quest
để đánh giá thao tác, độ dễ chịu và chi phí cập nhật collision trên thiết bị.

## Previous APK: petal walking

`Experiments/LetItBee-HoneyPilot/Build/LetItBee-Quest-petal-walk.apk`

Android ARM64 build completed 22/10/2026 at 22:41 (Asia/Saigon), 0 errors,
503 warnings. APK size: 114,155,474 bytes (~109 MiB).
SHA256: `2b62474f29c5ae5baebb6e912110679ae5249e8737e1c38acf8eb9e146850d11`.
New walking/collision/gravity types confirmed present in player metadata.

Validation: 602 Editor checks (250 specific to walking, animation support,
edge retention, fast collision and gravity; 352 regression checks) passed.
Real GAMA: 57 checks using walking positions, collection, pickup and deposit,
plus 37 full-hive/reconnect/idempotence checks passed. Disposable GAMA on 6883
was stopped; the existing session/checkpoint was not reset.
All 1,635 original components and saved fields were preserved; two
`BeeFlowerSurface` components and new walking/gravity fields were added.
Editor collision refresh measured about 0.85 ms per flower on this PC;
Quest performance, physical input and comfort have not been tested.

```powershell
cd C:\Projects\Unity\simple.CTU.VU2\Experiments\LetItBee-HoneyPilot
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Connect-Quest.ps1 -InstallApk .\Build\LetItBee-Quest-petal-walk.apk
```

Try: land with left trigger → walk to the centre and edges with left joystick
→ release joystick and hold right trigger to gather → grip right to carry
→ left trigger to leave → swipe to rise, stop swiping to descend with horizontal
momentum → carry honey back to a free hive cell. Check both flowers and landing
again after falling. Keep the usual SIMPLE/GAMA startup and connection profile.

## Previous petal landing iteration

Phần này thay cho cơ chế đậu trên nhánh xanh và giữ trigger ở các ghi chép lịch sử bên dưới.
Hai hoa có tổng cộng 26 điểm đậu trên cánh trắng (13 mỗi hoa), collider lưu sẵn
trong scene và đi theo xương cánh. Không thay scale/transform cây, HUD hoặc binding.

- Bấm trigger trái gần hoa để tự đáp lên cánh phù hợp; thả nút vẫn ở trên hoa.
- Bấm lại lúc đã đậu để rời. Bấm lại lúc đang tiếp cận để hủy đáp.
- `H` trên bàn phím dùng cùng thao tác bấm trong Editor. `F` hút mật, `G` cầm mật.
- Trigger trái ở xa hoa không giữ ong giữa không trung. Cần nhả rồi bấm lại khi gần hoa.
- Vị trí đầu/nghiêng đầu vẫn theo tracking. Nhún cánh và thân được bù tại camera;
  camera không bị ép xoay vào nhụy. Mất mạng không tự làm ong rời cánh.
- Ngừng vỗ cánh thì chuyển từ lượn sang hạ dần, có lực cản và tốc độ rơi giới hạn.
  Các giá trị dưới được nhân với `BeeEmbodiment.Player Scale` khi chạy.

| Component / asset | Inspector parameter | Current value / purpose |
| --- | --- | --- |
| `BeeHoneyPilot` | `Toggle Petal Landing` | On; bấm đậu/bấm rời |
| Petal `BeePerchPoint` | `Landing Detection Radius` | 1.15 world metres |
| Petal `BeePerchPoint` | `Surface Visual` | `Petal_XX_1`; xương cánh trắng |
| Petal `BeePerchPoint` | `Petal Bend Angle` | 2 degrees |
| Petal `BeePerchPoint` | `Spring Frequency` / `Damping Ratio` | 1.6 Hz / 0.6 |
| `BeeController` | `Comfort Descent` | On |
| `BeeController` | `Comfort Fall Acceleration` | 6 m/s² before miniature scale; effective 0.9 |
| `BeeController` | `Comfort Max Fall Speed` | 2.8 m/s before miniature scale; effective 0.42 |
| `BeeController` | `Glide Drag` | Saved 1.6, effective 0.24 at scale 0.15 |
| `BeeFlapping` | `Flap Clip` | `FlightFlutterLoop` from supplied Cartoon Flutter file |
| `InteractionAudio.asset` | `Landing Clip` / `Landing Volume` | `PetalLanding` / 0.6 |
| `InteractionAudio.asset` | `Honey Formation` / `Formation Volume` | `NectarFormation` / saved 0.7 |
| `InteractionAudio.asset` | `Honey Pickup` / `Pickup Volume` | `NectarPickup` / saved 0.45 |
| `InteractionAudio.asset` | `Formation Impact Seconds` / `Pickup Impact Seconds` | 0.76 s / 0.42 s |
| `InteractionAudio.asset` | `Landing Haptic Intensity` / `Pollen Particle Count` | 0.07 / 8 |

Chọn xương `Petal_XX_1 → Petal Landing XX → Petal landing surface` để xem/chỉnh
Box Collider. Không bật `Is Trigger`. Nhóm `Bee Perch A/B` giữ danh sách các điểm
đậu; vị trí nhánh xanh cũ không còn collider đậu. Các tên trường cũ vẫn được giữ
ở nơi cần tương thích dữ liệu Unity; tên hiển thị mới và tooltip đều bằng tiếng Anh.

Âm thanh gốc nằm trong `Assets/Audio/LetItBee/Source`; bản chạy mono trong
`Assets/Resources/BeeAudio`. Giữ bản gốc nguyên byte. Bản flight dùng phrase lặp
1.475 s có crossfade; landing dài 0.574 s, forming 1.384 s, pickup 0.984 s sau cắt
đuôi im lặng/fade. Điểm nhấn 0.76/0.42 s lấy từ envelope clip, đồng bộ thời lượng
ngưng tụ và nhấc mật. Tiếng đáp, ít hạt phấn và rung tay trái chỉ kích hoạt khi
đã chạm ổn định; không phát lại theo phản hồi mạng lặp.

Kiểm tra: 352 mục Editor đạt; 31 mục dùng vị trí thật của 26 cánh với GAMA thật
và 37 mục tổ đầy/khôi phục/idempotence đạt. Phiên GAMA thử ở cổng riêng 6883 đã dừng.
Đối chiếu scene: 5 component cũ đổi đúng phần đã duyệt, thêm 104 component cho
26 điểm cánh, bỏ 8 component collider nhánh sai. Các component cũ khác giữ nguyên.
Chưa nghiệm thu cảm giác chóng mặt, độ lớn âm và thao tác vật lý trên Quest.

## Previous APK: point landing

`Experiments/LetItBee-HoneyPilot/Build/LetItBee-Quest-petal-landing.apk`

Build Android ARM64 hoàn tất 22/10/2026 lúc 21:58 (Asia/Saigon), 0 lỗi,
495 cảnh báo; file APK 114.153.349 byte (khoảng 109 MiB). Metadata có toggle
landing, bộ chọn cánh và các trường âm thanh mới. Toàn bộ 1.635 component sau
build khớp scene trước build; ProjectSettings khớp bản ngay trước build.
Chưa cài hoặc đeo Quest để nghiệm thu bản này.

Thay bản APK cũ bằng file này. Nếu đang dùng SIMPLE/GAMA, giữ cùng quy trình
khởi chạy và profile kết nối; không reset checkpoint để thử cơ chế đậu.

```powershell
cd C:\Projects\Unity\simple.CTU.VU2\Experiments\LetItBee-HoneyPilot
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Connect-Quest.ps1 -InstallApk .\Build\LetItBee-Quest-petal-landing.apk
```

Thử: bay gần phía trên cánh trắng → bấm/thả trigger trái → kiểm tra bám cánh,
nhún, nhìn/cúi đầu → giữ trigger phải thu mật → grip phải nhấc → bấm trigger
trái để cất lên → mang mật về tổ. Kiểm tra riêng trường hợp hủy giữa lúc đáp,
mất mạng khi đã đậu và lượn xuống sau khi ngừng vỗ cánh.

## Previous implementation notes

## Scene và dữ liệu

Đọc `SCN_LetItBee` ở Edit Mode, scene đã lưu trước khi chỉnh.
Các giá trị bay, HUD, binding và camera có sẵn không bị áp lại từ preset.

| Thành phần | Trước | Bản thử |
| --- | --- | --- |
| Scale `HoaCuc_Final` và `Pilot Flower B` | 0,08 | 0,092 |
| Scale `CayDua_Quad_Rig` | 2,7 | 2,916 |
| Tỉ lệ XR khi chạy | 1 | 0,15, qua `BeeEmbodiment` |
| Điểm đậu | Tự tìm đầu xương lá | Hai anchor riêng tại `Leaf_2_2_end` |
| Tốc độ tiếp cận điểm đậu riêng | Chưa có | 0,22 world units/s |
| Thời gian làm mềm tiếp cận | Chưa có | 0,55 s |
| Thời gian rời cành | Chưa có | 0,45 s |

Vị trí gốc của cây và hoa được giữ. Đây là tỉ lệ trải nghiệm cách điệu,
chưa phải kích thước sinh học được hiệu chuẩn cho một loài ong cụ thể.

Lượt trước: `1f28f796590a4f208226a1de2dfda7c2`.
Lượt thử bố trí mới: `46c5af5b854947f49d64c9c6dacef12f`.
Checkpoint cũ được giữ. Dùng lại lượt cũ với bố trí mới sẽ bị kiểm tra geometry
từ chối; không đổi session để ép nạp checkpoint không khớp.

## Cách hoạt động

`BeeEmbodiment` thu nhỏ XR rig lúc chạy, gồm camera và tay cầm. Tốc độ, gia tốc,
bán kính va chạm và độ hở được đổi tương ứng trong bộ nhớ, rồi phục hồi khi
component dừng. Trần bay tuyệt đối được giữ để tổ vẫn nằm trong vùng bay tới.
Thanh quẹt, tia và kích thước mật đang mang được đổi tương ứng; các thông số
Inspector gốc của chúng vẫn là dữ liệu tham chiếu.

Khi có tracking đầu thật lần đầu, camera rig lấy collider mặt đất theo mask,
bỏ qua proxy đi bộ cũ, rồi đặt gốc sàn XR theo mặt này. Không gán local pose của
camera. Chuyển động đầu sau đó vẫn do tracking điều khiển. Căn sàn chỉ làm một
lần; không liên tục kéo người chơi về mặt đất khi đang bay hoặc đậu.

Điểm đậu riêng cho phép bắt đầu tiếp cận khi giữ trigger trái trong vùng nhận,
sau khi giảm tốc. Đường đi được kiểm tra vật cản. Vị trí đích được chốt khi bắt
đầu; animation cành không kéo camera. Ong tăng/giảm tốc khi vào vị trí và nâng
nhẹ khi rời. Thả trigger trong lúc chưa đậu xong hủy tiếp cận.

Ở giữa không trung, trigger trái giảm tốc rồi giữ vị trí. Dao động góc nhìn
chỉ theo phương đứng, vào/ra mềm; `Reduced Motion` đưa phần dao động nhân tạo
về không. Không có bảo đảm hết chóng mặt: cần thử biên độ trên kính thực tế.

## Chỗ chỉnh trong Unity

| Chọn | Component/asset | Thông số |
| --- | --- | --- |
| `BeeRoot` | `BeeEmbodiment` | `Player Scale`, `Align Floor At First Tracking`, `Hover View Strength`, `Reduced Motion` |
| `BeeRoot` | `BeeController` | `Hold Bob Amplitude`, `Hold Bob Frequency`, `Hold Bob Blend Duration` |
| Đối tượng chứa pilot | `BeeHoneyPilot` | `Perch Approach Speed`, `Perch Smooth Time`, `Perch Release Duration` |
| `Bee Perch A` hoặc `Bee Perch B` | `BeePerchPoint` | Vị trí trên cành, `View Offset`, `Approach Radius`, `Branch Visual` |
| `Assets/Resources/BeeAudio/InteractionAudio.asset` | `BeeInteractionAudio` | Clip và âm lượng phấn, hút mật, tạo/cầm mật, gắn tổ; thời gian fade và điểm nhấn gắn |
| `BeeRoot` | `BeeFlapping` | `Flap Clip`, `Flap Volume`, pitch và đồng bộ nhịp cánh |

Dừng Play trước khi chỉnh và lưu. Để vị trí gốc của `Bee Perch` bám hình cành;
`View Offset` là vị trí mắt tương đối và không xoay đầu người chơi. Điểm đậu
vẫn phải nằm trong khoảng tương tác hoa mà GAMA chấp nhận.

## Điều chỉnh bay và đậu trên cành — 22/10/2026

Cập nhật sau lần thử đầu trên kính: lỗi nhận quẹt và khả năng leo cao được
xử lý riêng. Canvas thanh quẹt là con của camera, vì vậy chỉ kế thừa scale
XR một lần; tính chạm chuyển từ pixel về mét của rig một lần. Mỗi tay giữ
trạng thái chạm/cooldown riêng và cặp hai tay vẫn tạo bonus nâng.

Cấu hình trong scene hiện tại:

| Chọn trong Inspector | Thông số | Giá trị lưu | Khi chạy với scale 0,15 |
| --- | --- | --- | --- |
| `BeeRoot > BeeController` | `Max Flight Altitude` | 25 | Trần Y tuyệt đối 25 |
| `BeeRoot > BeeController` | `Max Ascend Speed` | 6 | 0,90 world units/s |
| `BeeRoot > BeeController` | `Move Speed` | 7,8 | 1,17 world units/s |
| `BeeRoot > BeeController` | `Reverse Speed` | 4,55 | 0,6825 world units/s |
| `BeeRoot > BeeController` | `Strafe Speed` | 3,9 | 0,585 world units/s |
| `BeeRoot > BeeEmbodiment` | `Continuous Flight Endurance Multiplier` | 3 | Hao stamina bay bằng nút giảm còn 1/3 |
| `Bee Perch A/B > BeePerchPoint` | `Stand On Branch` | Bật | Thân nằm trên mặt đỡ cành |
| `Bee Perch A/B > BeePerchPoint` | `Support Size` | 0,12 × 0,12 | Kích thước mặt đỡ theo world units |

Stamina gốc vẫn lưu 22/s; chi phí nhịp quẹt giữ 3. Hệ số endurance chỉ áp vào
bộ nhớ khi chạy và phục hồi khi component dừng. Các tốc độ là giá trị thử cho
VR, chưa phải vận tốc sinh học đã hiệu chuẩn. Với tốc độ nhỏ cũ và stamina
cũ, bay liên tục hết sức trước khi lên được tới tổ, dù trần Y còn cao.

Điểm thân nằm trên đầu cành/lá phía ngoài, thay vì nằm tại vị trí mắt.
`View Offset` dành cho góc quan sát cách điệu để nhìn hoa. Mặt đỡ được tạo
lúc chạy; không có chân ong mới. Tiến trình tiếp cận dùng chung cho thân và
vị trí mắt để giới hạn tốc độ thay đổi góc nhìn. Khi rời cành, offset mắt
được chuyển dần về vị trí thân đang bay, tránh tụt mắt khi trả quyền điều khiển.
Local pose của đầu và thông số camera lưu trong scene được giữ.

Điểm tựa chốt theo world khi vào đậu; cành nhún bằng phần hình ảnh, không
kéo đầu người chơi theo animation. Không đổi giao thức SIMPLE/GAMA, session,
checkpoint, vị trí cây/hoa/tổ hoặc thông số HUD trong cập nhật này.

Kiểm tra cập nhật: 269 mục hồi quy Editor và 23 mục mới đều đạt; 37 mục
GAMA tổ đầy và 7 mục theo tọa độ hai điểm đậu mới đều đạt. Mô phỏng bay liên
tục từ Y = 0,4 với cấu hình cũ dừng ở Y ≈ 3,37 do hết stamina; cấu hình mới
đạt Y ≈ 11,01 sau 12 giây, vẫn còn stamina. Kiểm tra va chạm đường thử từ
điểm xuất phát lên Y = 12, sang phía trên tổ rồi hạ tiếp cận đều không bị chặn.
Đây là kiểm chứng mã/scene, chưa phải kết quả đo trên Quest.

Đối chiếu 1.531 component của scene: chỉ 6 component trong phạm vi bay/điểm
đậu thay đổi; 1.525 component còn lại giữ nguyên. Checkpoint cũ được đối
chiếu SHA-256 và giữ nguyên. Các bản APK trước vẫn là bản cũ; cần cài APK
mới để thử những thay đổi này.

## Bộ âm thanh

File gốc nằm ở `Assets/Audio/LetItBee/Source/`; clip dùng khi chạy nằm trong
`Assets/Resources/BeeAudio/`. Bản dùng chuyển mono cho nguồn âm 3D, cắt phần
đuôi gần im lặng và thêm fade ngắn. Chi tiết xử lý được ghi khi thực hiện.

| Clip | Sự kiện |
| --- | --- |
| `ChubbyBumblebeeLoop` | Bay và giữ vị trí; nhịp âm theo pha cánh, giảm khi đậu |
| `PollenPop` | Nhận/chuyển phấn đã được xác nhận |
| `NectarSip` | Đang giữ trigger phải thu mật; fade khi hủy hoặc chờ xác nhận |
| `HoneyFormation` | Một nhịp giọt mật khi hình thành và khi nhấc mật |
| `HiveDeposit` | Phát trong chuyển động gắn, điểm nhấn khoảng 0,5 s của clip khớp thời điểm khít ô |

Tiếng cánh giảm trong lúc thu/gắn mật. Các sự kiện ngắn dùng nguồn âm riêng để
âm của hoa trước không bị kéo sang hoa sau. Giao dịch lặp không phát thưởng
lặp. Hủy feedback không hủy asset âm thanh dùng chung.

## Kiểm tra và giới hạn

Kết quả đợt bảy giai đoạn đầu: 11 bộ kiểm tra Editor đạt 269 mục; 37 mục kiểm tra tổ đầy
và 7 mục kiểm tra tọa độ cụm mới với GAMA thật. APK Android ARM64 build thành
công, 0 lỗi, 533 cảnh báo; kích thước file 114.153.647 byte (khoảng 109 MiB).
Đã kiểm tra metadata trong APK có ba lớp `BeeEmbodiment`, `BeePerchPoint`,
`BeeInteractionAudio`. Chưa cài và thử bản này trên kính trong đợt triển khai.

- Kiểm tra riêng về embodiment: căn sàn trên collider thử, thu nhỏ tracking,
  phục hồi thông số, giới hạn dao động, tiếp cận/rời cành và vòng đời asset âm.
- GAMA: hai điểm đậu trong scene được chấp nhận contact, thu mật, cầm và gắn
  tổ; giao dịch lặp vẫn chỉ tính một lần. Session/checkpoint thử riêng.
- Bộ tổ đầy: 37 kiểm tra với GAMA thật, gồm bảy ô và khôi phục tiến độ.
- Cần thử trên Quest: cảm nhận tỉ lệ, sàn khi đứng/ngồi, độ dễ chịu lúc đậu,
  nhấp nhô, hướng âm, độ lớn âm và thao tác hai tay sau khi đổi tỉ lệ.
- Không có chân ong mới, NPC, cutscene hay level design mới trong đợt này.

Đường khởi chạy SIMPLE/GAMA giữ như [hướng dẫn chính](LetItBee.md).

## Cành có va chạm và nhún khi đáp — bổ sung 22/10/2026

`BeePerchPoint` tạo collider đặc dọc đoạn cành được gán ở `Branch Visual`,
cùng mặt đỡ tại điểm đứng. Collider bám theo chuyển động cành; thân ong đứng
trên mặt đỡ và theo độ võng của cành. Đây là collider đơn giản cho đoạn cành
đậu, không phải MeshCollider bao toàn bộ hoa.

Khi đáp, cành võng xuống, nảy nhẹ rồi ổn định; khi rời đi, cành hồi về.
Dao động thân được bù ở camera để giữ hình ảnh êm và giữ nguyên tracking đầu.
Vị trí mắt đã chỉnh để nhìn rõ hoa vẫn được dùng khi lấy mật.

Chọn `Bee Perch A` hoặc `Bee Perch B`, component **Bee Perch Point**:

- **Landing Bend Degrees**: độ võng/nhún, mặc định 2 độ; đặt 0 để tắt.
- **Branch Collider Radius**: bán kính va chạm của đoạn cành, mặc định 0,012 m.
- **Support Size**: diện tích mặt đỡ tại điểm đậu; giữ giá trị bạn đã lưu.

Collider được gắn sẵn trong scene dưới mỗi `Bee Perch A/B`:
`Branch landing surface` có **Box Collider**, con của nó `Branch collision`
có **Capsule Collider**. Cả hai bật, `Is Trigger` tắt. Dùng **Edit Collider**
để chỉnh hình va chạm; kích thước collider đã lưu không bị mã chạy ghi đè.
`Landing Surface` và `Branch Surface` trên `BeePerchPoint` tham chiếu tới
hai component này. Khi chúng đã được gán, mã không sinh collider trùng.
Các điểm đậu cũ chưa có component vẫn dùng cách tạo collider lúc chạy.
APK `flight-branch-fix` bên dưới được build trước bổ sung này, cần build lại
để thấy thay đổi trên Quest.

Đã đạt 116 kiểm tra Editor về bay/đậu, embodiment, tương tác và grip. Kiểm tra
trên bản sao hai cây trong scene xác nhận collider đỡ đúng mặt trên, độ võng
khoảng 0,00837 m ở thiết lập mặc định và khôi phục cành khi bỏ tải. Đối chiếu
1.531 component: chỉ có hai trường mới ở mỗi `BeePerchPoint`, các giá trị cũ
giữ nguyên; scene không được ghi lại. Chưa kiểm tra bổ sung này trên Quest.

## APK gần nhất

### Căn lại cành theo mesh

Điểm đầu lá `Leaf_2_2` trước đây chưa đúng chỗ đứng mong muốn. Hai điểm đậu
đã chuyển tới bề mặt cành trên đoạn `Leaf_2_0 → Leaf_2_1`, tại 40% chiều dài
đoạn, lấy điểm chạm bằng raycast xuống mesh đã bake. Mặt đỡ 0,035 × 0,035 m,
dày 0,008 m, xoay theo pháp tuyến bề mặt. Capsule dọc cành có bán kính 0,0045 m.
Vị trí mắt trong thế giới giữ nguyên bằng cách bù `View Offset` theo vị trí
điểm đứng mới. Thân ong được đặt theo pháp tuyến mặt đỡ để đáp được trên cành nghiêng.

Collider đã chỉnh trong Inspector giữ vị trí tương đối với cành khi chạy;
mã không ép nó trở lại đường nối bone. Nhún mặc định 2 độ tại điểm mới cho
độ võng khoảng 2,6 mm. Những giá trị này thay cho căn chỉnh cành ở phần trên.

Kiểm tra hoàn tất sau sửa mặt nghiêng: 123 mục Editor đạt (36 bay/cành,
26 đậu, 17 embodiment, 21 tương tác và 23 grip). Sweep va chạm dùng bán kính
ong thu nhỏ trong scene tới được mục tiêu cả A và B, sai lệch bằng 0.
Bản sao hai cây xác nhận collider đã lưu không bị tạo trùng, vị trí tương
đối và kích thước giữ nguyên khi khởi chạy/nhún; xóa script trong Edit Mode
không xóa collider đã lưu. Chưa xác minh cảm giác đáp và nhún trên Quest.

Bản `flight-branch-fix` build thành công ngày 22/10/2026 lúc 16:13 (giờ Việt Nam):
0 lỗi, 510 cảnh báo; ARM64; 114.153.753 byte (khoảng 109 MiB). Đã xác minh
metadata có các thay đổi endurance, điểm đậu thân và offset mắt lúc đậu.
Chưa cài/thử trực tiếp trên Quest. Sau kiểm tra và build, toàn bộ 1.531
component trong scene vẫn khớp bản đã lưu sau chỉnh sửa có chủ đích.

APK đầu ra: `Experiments/LetItBee-HoneyPilot/Build/LetItBee-Quest-flight-branch-fix.apk`.
Đóng game trên kính, giữ kết nối USB đã được cho phép, mở PowerShell Administrator:

```powershell
cd C:\Projects\Unity\simple.CTU.VU2\Experiments\LetItBee-HoneyPilot
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Stop-Simple.ps1 -HeadlessGama
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Start-Simple.ps1 -Headless
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Connect-Quest.ps1 -InstallApk .\Build\LetItBee-Quest-flight-branch-fix.apk -UpdateFirewall -EnableStreaming
```

Khởi động lại experiment để lượt thử mới được nhận. Không reset checkpoint cũ.
Mở game trên kính, thêm player vào phiên SIMPLE nếu còn đang chờ admission.

Lần đầu, đứng hoặc ngồi ở tư thế chơi bình thường để tracking sàn ổn định.
Kiểm tra cúi/lean trước, rồi thử giữ trigger trái giữa không trung. Sau đó bay
gần điểm đậu trên cành, giữ trigger trái tới khi ổn định; trigger phải hút mật,
grip phải cầm. Thả trigger trái để rời cành. Nếu khó chịu, dừng thử và bật
`Reduced Motion` ở component `BeeEmbodiment` cho bản kiểm tra tiếp theo.

Không coi Game View hoặc các kiểm tra mô phỏng đầu vào là xác nhận người chơi
không chóng mặt. Chưa có kết quả nghe/đeo kính trực tiếp cho đợt này.
