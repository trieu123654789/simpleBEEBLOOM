# Bee Bloom — Kế hoạch, tiến độ và hướng dẫn phát triển

Cập nhật: **18/10/2026**. Đây là tài liệu chính của Bee Bloom, hợp nhất các bản
plan, checklist, tiến độ và hướng dẫn kỹ thuật đã tạo trong quá trình làm việc.
Khi thay đổi phạm vi hoặc hoàn thành công việc, cập nhật tại đây.

Lượt hợp nhất này chỉ đối chiếu tài liệu, mã nguồn và kết quả kiểm tra đã lưu;
không chạy lại gameplay, không chỉnh scene, prefab hoặc thông số Unity.

## Mục lục

- [1. Trạng thái và phạm vi đã chốt](#1-trạng-thái-và-phạm-vi-đã-chốt)
- [2. Quy tắc làm tiếp](#2-quy-tắc-làm-tiếp)
- [3. Vòng chơi thử hiện tại](#3-vòng-chơi-thử-hiện-tại)
- [4. Chạy SIMPLE và cấu hình chuyển máy](#4-chạy-simple-và-cấu-hình-chuyển-máy)
- [5. Kiến trúc và giao thức Unity–GAMA](#5-kiến-trúc-và-giao-thức-unitygama)
- [6. Chỉnh bay, va chạm và âm thanh](#6-chỉnh-bay-va-chạm-và-âm-thanh)
- [7. Backlog và asset](#7-backlog-và-asset)
- [8. Đề xuất màn 2 và trực quan hóa GAMA](#8-đề-xuất-màn-2-và-trực-quan-hóa-gama)
- [9. Kiểm chứng và giới hạn](#9-kiểm-chứng-và-giới-hạn)
- [10. Ước lượng tham khảo](#10-ước-lượng-tham-khảo)
- [11. Tài liệu nền và nguồn tham khảo](#11-tài-liệu-nền-và-nguồn-tham-khảo)

## 1. Trạng thái và phạm vi đã chốt

Mục tiêu hiện tại là chuẩn hóa nền kết nối và thử vòng **hoa → mật → tổ** trước
khi phát triển gameplay chính. Chưa yêu cầu đóng gói đi thi hoặc làm level design.

| Hạng mục | Đang có | Phần chưa hoàn thành |
| --- | --- | --- |
| Bay | Hai ray quẹt độc lập, nhịp một/hai tay, lướt, phanh và trigger trái giữ vị trí | Cân chỉnh, độ thoải mái và hướng joystick trên Quest |
| Map | Môi trường kế thừa SIMPLE, xử lý va chạm và phản hồi bay sát bề mặt | Kiểm tra toàn map, asset mới, hiệu năng trên kính |
| Camera/HUD | Đã có rig, HUD và các đợt sửa trước | Nghiệm thu tracking, độ cao, la bàn, bố cục và dữ liệu gameplay trên kính |
| Âm thanh | Chubby Bumblebee loop, nhấn âm theo pha cánh | Nghe và cân chỉnh trên thiết bị |
| Hoa/mật/tổ | Pilot hai hoa thuộc hai cây, một cục mật, một ô tổ; GAMA xác nhận | Luồng chính đủ 5 cục/lượt và nhiệm vụ đầy đủ |
| Kết nối | SIMPLE WebPlatform → LinkToUnity → GAMA; IP ngoài APK, đường dẫn phía server | Quest/LAN và máy thứ hai chưa kiểm chứng |
| GAMA | Model giao dịch, checkpoint, bản đồ pilot và vị trí ong liên tục khi kết nối | Model sinh thái đã hiệu chuẩn, bản đồ quan sát hoàn thiện |
| NPC/cutscene | Có ý tưởng và code NPC kế thừa để khảo sát | Chưa có NPC/cutscene Bee Bloom hoàn chỉnh được nghiệm thu |
| Luồng game | Chưa nghiệm thu thành game hai màn từ đầu đến cuối | Menu, pause, retry, chuyển màn, kết thúc, lưu/reset theo thiết kế chính |

**Các quyết định đã chốt:**

- Bối cảnh vườn–rừng **nước ngọt ven sông**; màn 2 có xâm nhập mặn.
- Màn 1: ong thụ phấn, lấy mật dạng cục, mang về và lắp vào tổ.
- **Đủ 5 cục hoàn thành một lượt**; chưa chốt số tổ, số lượt hay điều kiện hết màn.
- Hoa cúc là lựa chọn tạm; asset hiện có tên `daisy`, chưa chốt loài/giống khoa học.
- Nhân vật là ong thợ; đây là vai trò trong đàn, chưa xác định loài ong.
- Muốn có NPC và cutscene; số lượng/nội dung còn chờ duyệt.

Pilot hiện tại chỉ nhận **một cục, một ô**. Kết quả 1/1 của pilot và demo cũ 5/5
không có nghĩa vòng chơi chính 5 cục đã hoàn tất.

## 2. Quy tắc làm tiếp

- Đọc `AGENTS.md` nếu có, trạng thái Unity, file mới nhất và diff trước khi sửa.
- Dữ liệu người dùng Ctrl+S là nguồn hiện hành. Không khôi phục scene/HUD/prefab
  hoặc áp lại preset từ bản sửa cũ. Không lưu toàn scene khi tác vụ không cần.
- Tương tác Unity qua MCP; không điều khiển desktop của người dùng để thay thế MCP.
- Các phần pilot và chuẩn hóa kết nối đã được cho phép, đã làm. Bỏ chỉ thị cũ
  “dừng toàn bộ vì chờ asset” vì không còn phản ánh phạm vi hiện tại.
- Với hạng mục mới phụ thuộc asset, chờ người dùng cung cấp/xác nhận và yêu cầu
  triển khai; không tự dùng placeholder để bắt đầu phần chưa được duyệt.
- Phân biệt **đã chốt**, **đề xuất**, **đã có code/kiểm tra PC** và **đã thử kính**.
  Lưu một ý tưởng vào backlog không đồng nghĩa được phép triển khai.
- Chỉ commit/push khi được yêu cầu. HEAD đã đọc ở lượt này: `95f8d96`; các thay đổi
  sau đó chưa được coi là đã sao lưu lên remote.

## 3. Vòng chơi thử hiện tại

Scene chính: `Assets/Scenes/VU2/SCN_BeeBloom.unity`, đối tượng **BeeBloom Honey Pilot**.
Khi đã khởi chạy SIMPLE theo mục 4, bắt đầu từ Edit Mode rồi nhấn Play.

| Bước | Thao tác và kết quả |
| --- | --- |
| Ghé hoa A | Ong ở trong `Contact Radius` khoảng 0,25 giây nhận phấn từ cây A |
| Lấy mật | Ngắm tâm hoa bằng tay phải, giữ trigger phải đủ `Collect Duration`; GAMA xác nhận rồi cấp cục mật |
| Cầm mật | Giữ trigger trái để ổn định, ngắm/đưa tay gần mật, bấm grip phải một lần |
| Chuyển phấn | Bay tới hoa B của cây khác nhưng cùng mã loài; GAMA xác nhận cây B nhận phấn, kể cả lúc đang mang mật |
| Gắn vào tổ | Giữ trigger trái, đưa tay vào vùng bóng mờ của ô, bấm grip phải lần nữa; xác nhận xong mới hiện 1/1 |
| Thả ngoài tổ | Cục mật lơ lửng gần điểm thả để nhặt lại; chưa có vật lý rơi/va chạm cục mật hoàn chỉnh |

Grip là thao tác bấm một lần để đổi cầm/thả, không cần giữ liên tục. Trong VR,
`Require Position Hold In Vr` mặc định yêu cầu giữ vị trí khi lấy/cầm/thả để tránh
xung đột với input bay đã lưu. Thả trigger trái để tiếp tục bay mang mật.
Desktop dùng **F** thay trigger phải, **G** thay grip phải; vẫn kiểm tra khoảng
cách và hướng ngắm, không bỏ qua luật GAMA.

Lấy mật và thụ phấn là hai sự kiện khác nhau. Đứng tại một cây không tự tính
thụ phấn cho chính cây đó; khác mã loài không tính. `chrysanthemum-pilot` là mã
thử nghiệm, chưa xác nhận phân loại sinh học. Nhận phấn không đồng nghĩa đã thụ
tinh/đậu hạt. Cục mật rắn để cầm/gắn là quy ước gameplay.

### Thông số tương tác

Dừng Play, chọn **BeeBloom Honey Pilot**, chỉnh và Ctrl+S khi muốn lưu.

| Trường | Công dụng |
| --- | --- |
| Contact Radius | Khoảng cách thân ong tới tâm hoa để ghé/lấy mật |
| Grab Radius | Khoảng cách tay có thể bắt cục mật |
| Snap Radius | Vùng tay được phép thả mật vào ô |
| Collect Duration | Thời gian giữ trigger phải |
| Aim Radius | Mức hỗ trợ ngắm vào tâm hoa/cục mật |
| Label Scale | Kích thước nhãn riêng của pilot |
| Require Position Hold In Vr | Yêu cầu giữ vị trí để thao tác trong VR |
| Session Id | Phiên tiến độ, giữ nguyên để tiếp tục checkpoint tương ứng |

Điểm gắn/mật đã gắn đi theo animation tổ; Unity gửi vị trí tương tác đang chuyển
động để GAMA kiểm tra. Không dùng tọa độ/độ cao trong tài liệu cũ làm vị trí chuẩn
để áp lại scene người dùng.

Khi **chủ động** đổi bố trí hoa/tổ hoặc bán kính tương tác và muốn thử cấu hình
mới, tạo phiên bằng **VU2 → Bee → Honey Pilot → New session (keep scene tuning)**
ở Edit Mode. Phiên cũ vẫn còn. Dừng/launch lại experiment trước khi đổi phiên.

## 4. Chạy SIMPLE và cấu hình chuyển máy

Các lệnh bên dưới chạy trong thư mục `Experiments/BeeBloom-HoneyPilot`.

### Cấu hình PC

Máy mới: sao chép `machine.example.json` thành `machine.local.json`, sửa đường dẫn:

```json
{
  "webPlatformPath": "D:/Dev/simple.webplatform",
  "gamaHome": "C:/Program Files/Gama",
  "gamaHost": "127.0.0.1",
  "gamaPort": 1000,
  "headlessPort": 6875,
  "nodePath": ""
}
```

Đường dẫn tương đối tính từ thư mục chứa file cấu hình. `nodePath` trống sẽ tìm
Node trong PATH. WebPlatform cần dependencies đã cài; GAMA cần LinkToUnity.
Máy hiện tại đã có file local; khi chuyển máy tạo lại cho đúng nơi cài đặt.

1. Mở GAMA bình thường, bật server GUI cổng 1000. Đóng experiment cũ bằng X đỏ
   trước khi nạp phiên mới; không tự đóng experiment khác của người dùng.
2. Chạy `Start-Simple.ps1`.
3. Mở `http://localhost:8000`, chọn **Bee Bloom - Honey Pilot**, launch
   **BeeBloomVR**, rồi Play/resume. Bấm launch một lần và đợi nạp xong.
4. Trong Unity mở scene chính, nhấn Play từ Edit Mode. Inspector phải báo
   **LIVE · SIMPLE → LinkToUnity → GAMA**.
5. GAMA **HoneyPilotMap** hiện **UNITY SERVER**: tam giác vàng là ong đang cập nhật,
   hai chấm là hoa, hình vuông là ô tổ; hoa xanh đã nhận phấn.

```powershell
.\Start-Simple.ps1
# Khi cần dừng middleware:
.\Stop-Simple.ps1
```

Dùng `Start-Simple.ps1 -Headless` để chạy server riêng cổng 6875. Phiên này không
xuất hiện trong cửa sổ GAMA GUI khác. Dừng platform trước khi đổi chế độ/cấu hình;
`Stop-Simple.ps1 -HeadlessGama` dừng thêm server headless do launcher tạo, không
đóng GAMA GUI. Launcher không mở trùng khi hai tiến trình của nó đang chạy.

Cổng mặc định của bộ cấu hình: web **8000**, monitor **8001**, kính **8080**.
`.env`, cache và log nằm riêng ở `Runtime/simple-platform`, không phải thư mục
WebPlatform dùng chung.

### Đổi IP ngoài scene/APK

```powershell
.\Set-BeeConnection.ps1 -Endpoint 'ws://192.168.2.8:8080'
```

Thay IP ví dụ bằng IPv4 LAN của PC. Lệnh tạo `Runtime/beebloom-connection.json`:

```json
{
  "schema": "beebloom-connection-v1",
  "simpleEndpoint": "ws://192.168.2.8:8080"
}
```

Editor đọc file trên. APK đọc cùng tên tại `Application.persistentDataPath`, với
package hiện tại là:

```text
/sdcard/Android/data/com.quoctrieu.beebloom/files/beebloom-connection.json
```

Để gửi vào Quest: cài/mở app một lần, cắm USB và cho phép USB debugging. Thay IP,
đường dẫn ADB thật trước khi chạy lệnh:

```powershell
.\Set-BeeConnection.ps1 -Endpoint 'ws://IP-MAY-PC:8080' -PushToQuest -AdbPath 'DUONG-DAN/adb.exe'
```

Bỏ `-AdbPath` nếu ADB trong PATH; có nhiều thiết bị thì thêm `-Serial`.
`localhost`/`127.0.0.1` trên Quest là chính chiếc kính, không phải PC.

Client đọc lại file khoảng mỗi giây khi vòng kết nối rảnh, đổi endpoint thì tự
nối lại; gói đang chờ/mất mạng có thể cần thêm timeout. Giao dịch giữ event ID
để tránh nhân đôi. File vắng thì dùng `Simple Endpoint` đã lưu trong scene;
file có nhưng sai JSON/schema/địa chỉ thì báo lỗi. File chỉ nhận hai khóa trên,
không nhận thông số bay/HUD/camera. Hiện chỉ hỗ trợ `ws`, IPv4/hostname, không
có credentials, path, query hoặc fragment trong endpoint.

APK mới có bộ đọc cấu hình ngoài đã build ở
`Experiments/BeeBloom-HoneyPilot/Build/BeeBloom-SIMPLE.apk`. **Cần cài cập nhật một
lần từ APK cũ**; các lần đổi IP/đường dẫn sau không cần build lại. Gỡ app/xóa app
data có thể mất file cấu hình; gửi lại khi cần. Thao tác gửi ADB trên kính chưa
được kiểm chứng vì chưa có Quest.

### Đường dẫn model, checkpoint và chuyển tiến độ

WebPlatform đọc `settings.json` rồi nạp `./GAMA/BeeBloomSimpleVR.gaml`; APK không
cần biết model nằm ở ổ nào. GAMA quyết định nơi ghi checkpoint, mặc định:

```text
Runtime/checkpoints/<Session Id>.json
```

Muốn đổi nơi lưu, sao chép `GAMA/server.example.json` thành `GAMA/server.local.json`,
sửa rồi dừng/launch lại experiment:

```json
{ "checkpoint_directory": "D:/BeeBloomData/checkpoints" }
```

Đường dẫn tương đối trong file này tính từ model GAMA. Tài khoản chạy GAMA cần
quyền ghi. Session Id phải là GUID 32 ký tự hex thường; client gửi khóa phiên,
server tự chọn đường dẫn và không dùng đường dẫn checkpoint do APK cũ gửi lên.

Để chuyển máy mà tiếp tục tiến độ: sao chép checkpoint cùng tên vào nơi lưu mới,
giữ `Session Id` và cấu hình hoa/tổ tương ứng trước khi chạy. Đổi IP không tự
chuyển dữ liệu. `machine.local.json`, `server.local.json`, Runtime và Build không
vào Git; cần mang dữ liệu/cấu hình cần thiết riêng khi chuyển máy.

### Lỗi thường gặp và chế độ cũ

| Hiện tượng | Cách kiểm tra |
| --- | --- |
| Logo SIMPLE rất lớn, nền lặp | CSS/Tailwind chưa nạp đúng; launcher đã cấu hình đường dẫn tuyệt đối. Đọc `web-error.log`, tải lại Ctrl+F5 |
| GAMA hỏi đóng experiment nhiều lần | Đóng hộp thoại/experiment cũ, chỉ launch một lần rồi đợi |
| `session_or_geometry_mismatch` | Dừng Play và chạy lại từ Edit Mode trước; không bật pilot giữa animation. Nếu chủ động đổi bố trí, tạo phiên mới; không xóa tiến độ để che lỗi |
| Editor nối được, Quest không nối | IP LAN đúng, cùng mạng, client isolation và Windows Firewall cổng 8080 |
| GAMA ghi `READ ONLY` | Đang xem checkpoint, chưa phải phiên Unity cập nhật liên tục |
| Đổi máy thấy tiến độ mới | Kiểm tra đã chép đúng checkpoint, Session Id và thư mục server chưa |

**Xem checkpoint từ workspace:** import GAMA Project từ thư mục `GAMA` của package,
bỏ chọn **Copy projects into workspace** để liên kết file gốc; mở
`BeeBloomHoneyPilot.gaml` → **HoneyPilot**, chọn `Progress file`. Bản xem đọc khoảng
0,5 giây/lần, không ghi tiến độ; ong là mẫu tại giao dịch gần nhất. Nếu trước đó
copy một file lẻ vào workspace thì đó là bản sao, không tự nhận cập nhật file gốc.

**Direct legacy:** `Start-GamaServer.ps1`/cổng 6873 và `HoneyGamaClient` còn phục
vụ chẩn đoán cũ. `Endpoint`, `Model On Server`, `Checkpoint Directory On Server`
trong Inspector chỉ dùng cho chế độ này. Không dùng hướng dẫn “sửa IP cổng 6873
rồi build lại APK” làm luồng phát triển hiện tại.

## 5. Kiến trúc và giao thức Unity–GAMA

```text
Unity Editor / APK Quest
  → ConnectionManager kế thừa SIMPLE
  → WebPlatform :8080
  → LinkToUnity / BeeBloomVR
  → luật giao dịch + checkpoint GAMA
  → snapshot xác nhận về Unity
```

| Phần | Trách nhiệm và đối tượng |
| --- | --- |
| BeeController, BeeSwipeFlap, BeeFlightRhythm | Di chuyển BeeRoot, nhận nhịp quẹt, nâng/lướt/phanh |
| BeeFlightCollision, BeeMapSurfaces, BeeMapSurfaceBuild, BeeSurfaceSkim | Va chạm, collider bổ sung và phản hồi sát bề mặt |
| BeeAltitudeHold, BeeFlapping, BeeCameraRig, BeeHud, BeeKeyboardAssist | Các chức năng giữ độ cao, cánh/âm thanh, rig, HUD, hỗ trợ desktop đã có; xem giá trị hiện hành trong scene |
| BeeHoneyPilot | Tham chiếu BeeRoot/tay phải/hai đầu hoa/ô tổ; gửi input, dựng mật/phấn/nhãn và phản hồi runtime |
| HoneySimpleClient, BeeSimpleConnection | Xác thực, gửi `ask`, ghép phản hồi; dùng ConnectionManager/WebSocket hiện có |
| BeeConnectionProfile | Đọc endpoint ngoài, loại đường dẫn PC khỏi payload SIMPLE; không nạp cấu hình vào scene |
| BeeBloomSimpleVR.gaml | Kế thừa `abstract_unity_linker`/`abstract_unity_player`, experiment `type: unity`, đăng ký player và chuyển giao dịch |
| BeeBloomHoneyPilot.gaml | Quản lý phấn, nectar, ID mật, cầm/thả, ô tổ, state và biên nhận checkpoint |

Map, hạ tầng XR và ConnectionManager/WebSocket kế thừa dự án SIMPLE. Logic bay
ong, phản hồi bề mặt, vòng mật/phấn và model Bee Bloom là phần phát triển thêm.
Unity giữ camera/tracking, input, bay, va chạm, animation, HUD, VFX/SFX/rung tay.
GAMA là nơi xác nhận tài nguyên và giao dịch; không đồng thời cộng/trừ ở hai phía.
Không áp geometry/spawn/giới hạn di chuyển từ SDK lên XR rig có sẵn.

### Hợp đồng dữ liệu của pilot

- Schema state: `beebloom-honey-pilot-v1`; profile kết nối dùng schema riêng
  `beebloom-connection-v1`.
- 1 đơn vị = 1 mét. Unity `(x,y,z)` → GAMA `(x,z,y)`; không áp thêm hệ số
  `precision` geometry SDK vào payload nghiệp vụ này.
- ID hoa `flower-a`, `flower-b`; cây `plant-a`, `plant-b`; ô `slot-a`.
- Gửi tới `simulation[0].unity_linker[0]`, action `bee_request`, args `id` và
  `envelope_json` gồm `correlation`, `operation`, `payload`.
- Operations: `configure`, `set_pose`, `transact`, `snapshot`.
- Giao dịch: `visit`, `collect`, `pick`, `drop`, `deposit`; mang `event_id`,
  `session`, ID vật thể và vị trí bee/hand. Danh tính cục mật do GAMA cấp.
- LinkToUnity `send_message` trả `bee_bloom`: `correlation`, `fault`, `snapshot`.
  Snapshot có state, response, cycle, vị trí hoa, ô tổ và ong.
- `correlation` ghép yêu cầu/phản hồi; `event_id` chống thực thi hai lần. Nối lại
  có thể dùng correlation mới nhưng giữ event_id của giao dịch đang chờ.
- GAMA lưu state và biên nhận trước khi ACK; nối lại khôi phục từ checkpoint.
  Mất mạng vẫn bay được, không tự xác nhận thao tác mật/phấn khi server chưa trả.
- Pilot hiện chỉ một người chơi; chưa nghiệm thu multiplayer.

Giao diện web dùng component/ảnh/CSS của SIMPLE. Checkout trên máy có hai thay
đổi backend về reconnect (`GamaConnector.ts`, `MonitorServer.ts`). Launcher tạo
`simple-compat.mjs` riêng: headless thêm `exp_id`, GUI dùng experiment đang hoạt
động và đợi trạng thái khởi tạo. Vì vậy kết quả hiện tại chưa chứng minh mọi bản
SIMPLE nguyên gốc hoặc server bên ngoài đều chạy ngay không cần đối chiếu.

### Tệp cần tìm khi phát triển

| Đường dẫn từ gốc project | Nội dung |
| --- | --- |
| `Assets/Scripts/VU2/Bee/` | Bay, camera, HUD, âm thanh và script nền ong |
| `Assets/Scripts/VU2/HoneyPilot/` | Pilot, client direct/SIMPLE, cấu hình kết nối |
| `Assets/Editor/VU2/` | Inspector, công cụ setup/build/check; không tự chạy setup lên scene đã chỉnh |
| `Assets/Prefabs/BeeBloom/HoneyPilot/` | Mesh/material mật, idle clip/controller riêng của pilot |
| `Experiments/BeeBloom-HoneyPilot/GAMA/` | Model hiện tại và cấu hình server mẫu |
| `Experiments/BeeBloom-HoneyPilot/settings.json` | Learning package **Bee Bloom - Honey Pilot**, experiment **BeeBloomVR** |

## 6. Chỉnh bay, va chạm và âm thanh

Các giá trị tham khảo dưới đây là giá trị khởi đầu trong tài liệu triển khai,
**không phải giá trị mới để áp lại Inspector**. Dùng giá trị người dùng đang lưu
làm nguồn hiện hành. Dừng Play trước khi chỉnh; Ctrl+S để lưu. Nếu chỉnh khi Play,
ghi lại và nhập ngoài Play trước khi lưu.

### Bay V2

Quẹt một tay tạo nhịp nâng trải trong thời gian ngắn. Hai tay trong cửa sổ nhịp
đôi nhận bonus; không cần hai tia nằm trên thanh cùng lúc. Mỗi tay có cooldown
riêng, giữ tia đứng yên không phát nhịp lặp. Sau quẹt có khoảng lướt rồi chuyển
dần sang độ rơi đã cấu hình.

Trigger trái phanh theo hướng đang bay rồi giữ tại điểm dừng phía trước; không
khóa tức thì tại vị trí bóp. Thả sau khi dừng bắt đầu từ vận tốc 0; thả sớm giữ
phần đà còn lại. Nhấp nhô ở thân/cánh, không thêm dao động camera. Nhiễu vị trí
BeeRoot được bỏ qua trong V2, giữ lại giá trị `Organic Jitter Scale` cũ.

**BeeRoot → Bee Controller → Flight V2 / Nhip vo - luot - phanh:**

| Thông số | Khởi đầu tham khảo | Tác dụng |
| --- | --- | --- |
| Enable Rhythm Flight | Bật | Bật/tắt V2 để so sánh cơ chế cũ |
| Stroke Lift Duration | 0,22 s | Trải lực nhịp nâng |
| Stroke Lift Scale | 1,8 | Nhân lực quẹt/bonus đã lưu |
| Stroke Stamina Cost | 3 | Thể lực mỗi nhịp; bonus đôi không tính lần thứ ba |
| Rest Glide Duration | 1,25 s | Khoảng lướt, gồm đoạn thoát |
| Rest Glide Sink Speed | 0,35 m/s | Tốc độ hạ hướng tới, không đổi vận tốc tức thì |
| Rest Glide Deceleration | 1,4 m/s² | Giảm đà nâng trong lướt |
| Glide Exit Duration | 0,6 s | Chuyển từ lướt sang rơi tự nhiên |
| Hold Brake Duration | 0,4 s | Thời gian phanh trước giữ chỗ |

**BeeRoot → Bee Swipe Flap → Flight V2 / Nhan nhip hai tay:**

| Thông số | Khởi đầu tham khảo | Tác dụng |
| --- | --- | --- |
| Paired Stroke Window | 0,18 s | Độ lệch nhịp cho phép giữa hai tay |
| Minimum Rhythm Interval | 0,28 s | Dùng giá trị lớn hơn giữa đây và Cooldown Per Hand |

`Max Ascend Speed`, `Max Fall Speed`, `Fall Acceleration` nằm trên BeeController
để chỉnh giới hạn nâng/rơi và gia tốc rơi. Không có bộ thông số đã hiệu chuẩn
sinh học cho loài ong cụ thể. V2 để BeeController làm chủ lực nâng;
KeyboardAssist không cộng nguồn nâng thứ hai, khoảng lướt V2 thay trợ lực lướt
cũ của SwipeFlap. Quẹt tính thể lực theo nhịp; giữ nút bay tính theo thời gian.

### Va chạm và bay sát bề mặt

BeeFlightCollision quét hình cầu theo quãng đường mỗi frame, xử lý bay ngang,
lên/xuống và trượt dọc vật cản; dùng `bodyRadius`, `groundClearance`, `groundMask`
đã lưu. Không tự chuyển ong về một tọa độ cố định vì Y thấp hoặc Z âm.

BeeMapSurfaces thêm MeshCollider thiếu cho nhóm đã nhận diện:
`nen_dat_low:*`, `SM_Terrain_Island_01*`, `SM_dat_trong_trot_*`, `dike`,
`SF_Water_Sea*`, `SM_House_01*`. BeeMapSurfaceBuild làm trên bản sao Play/build
trước static batching để bake collision vào APK. Trong SCN_BeeBloom, chuyển động
ong/dò bề mặt bỏ qua proxy đi bộ `InfinityWall`, `Teleport Area*`, `Teleport Cau*`;
không xóa/vô hiệu collider đó cho hệ thống khác.

Sát nước có gợn; sát đất/vật cản có cung gió, cường độ/rung theo tốc độ/khoảng cách.
Không đổi vận tốc, camera, FOV hay HUD. Pool tối đa 16 vệt, tuổi thọ 0,7 giây;
không phát khi giữ vị trí, đứng yên hoặc khóa di chuyển. Shader trong Resources,
hỗ trợ stereo instancing.

**BeeRoot → BeeController → Surface skim / Luot sat be mat:**

| Thông số | Tham khảo | Tác dụng |
| --- | --- | --- |
| Enable Surface Skim | Bật | Công tắc phản hồi sát bề mặt |
| Skim Distance | 1,5 m | Khoảng bắt đầu có phản hồi |
| Skim Min Speed | 1,2 m/s | Tốc độ tối thiểu |
| Skim Haptic Strength | 0,08 | Rung tối đa; 0 để tắt |

BeeSurfaceSkim tự gắn runtime; lưu thông số ở BeeController. Collider theo mesh
tĩnh, không theo sóng displacement; cây/cỏ và asset mới/đổi tên cần kiểm tra
collider riêng. Tường thiếu collider không phát phản hồi. Phục hồi xuyên ban đầu
chỉ trong khoảng 2 m ở đất/nước nhận diện, không kéo xuyên mái hoặc từ sâu tùy ý.

### Âm thanh cánh

**BeeRoot → BeeFlapping → Audio / Âm thanh:**

| Trường | Công dụng |
| --- | --- |
| Flap Volume | Âm lượng tối đa; 0 để tắt |
| Min Pitch / Max Pitch | Cao độ khi nhịp chậm/nhanh; tăng làm clip chạy nhanh và cao hơn |
| Wing Beat Audio Depth | Nhấn âm theo pha; 0 đều, 1 rõ nhất; tham khảo 0,55 |
| Audio Fade Speed | Tốc độ fade vào/ra; tham khảo 2 |
| Audio Spatial Blend | 0 = 2D, 1 = 3D theo vị trí; tham khảo 0,5 |
| Sync Audio To Wings | Đồng bộ lớp nhấn âm theo pha cánh |
| Use Chubby Bumblebee Default | Dùng loop mới khi Flap Clip trống |
| Flap Clip | Nguồn âm riêng, ưu tiên hơn mặc định |

Chỉnh ở BeeFlapping, không chỉnh Volume/Pitch trực tiếp AudioSource vì bị cập
nhật runtime. File gốc ở `Assets/Audio/Bee/Chubby_Bumblebee_Bouncy_Flap_(SFX).wav`;
loop ở `Assets/Resources/BeeAudio/ChubbyBumblebeeLoop.wav`: đoạn 0,30–2,60 s,
nối chéo 40 ms, dài 2,26 s. File Downloads gốc không đổi.

Một nguồn loop liên tục được điều chỉnh volume/pitch theo cánh; không phát
chồng clip dài mỗi nhịp. Đây là đồng bộ lớp nhấn âm với animation, không tách
từng tiếng đập trong bản ghi; không đổi camera/nhịp cánh để chạy theo âm thanh.

## 7. Backlog và asset

`[x]` = đã có đúng phạm vi ghi bên cạnh; không đồng nghĩa nghiệm thu trên Quest.
`[ ]` = còn việc. Nhãn **Đề xuất** phải được duyệt trước khi triển khai.

### Nền đã có và asset cần xác nhận

- [x] Map/rig kế thừa, bay V2, hai ray, hold, va chạm và phản hồi bề mặt.
- [x] Âm thanh Chubby Bumblebee và pilot tương tác hiện tại.
- [x] Người dùng đã thêm asset daisy/hoa cúc, cây dừa có tổ và mật; đã dùng trong
  lượt khảo sát/pilot. Không tiếp tục ghi “chưa có hoa/tổ/mật” chung chung.
- [x] GAMA gameplay pilot, SIMPLE, checkpoint chống trùng và cấu hình chuyển máy.
- [ ] Xác nhận asset nào là bản cuối; bộ tổ 5 ô, trạng thái hoàn thành và VFX/SFX cuối.
- [ ] Chốt loài cúc/ong; đối chiếu tên asset và ID thử nghiệm với loài được chọn.
- [ ] Kiểm kê model ong/rig/cánh; quyết định có thay Bee_Placeholder không.
- [ ] Kiểm kê tỷ lệ, pivot, collider, URP, texture, polygon, giấy phép và chi phí Quest.
- [ ] Chọn NPC/animation/thoại, thiên địch và icon/map GAMA theo phần được duyệt.

### Hoàn thiện vòng chơi chính màn 1

- [ ] Chốt cách kiếm một cục: lượng cần thu, sức chứa, số được mang, xử lý làm rơi.
- [ ] Mở rộng từ pilot một ô thành **5 cục/lượt**; tách cấu hình này khỏi số tổ,
  tổng lượt và điều kiện hết màn. Chưa tự chốt các số còn mở.
- [ ] **Đề xuất:** năm ô dễ thấy, bắt dính gần ô hợp lệ, âm/hình và tiến độ 0/5–5/5.
- [ ] Nối BeeGameStats/HUD/la bàn/nhiệm vụ với dữ liệu GAMA thật; không giả định
  BeeGameStats ở BeeRoot (lượt khảo sát trước ghi nhận ở BeeHUD).
- [ ] Khi dùng lại BeeFlower/BeePollinationSystem/BeeHive/BeeQuestManager cũ,
  rà input/tham chiếu; ngăn `autoDeliverWhenFull` cộng mật mà chưa mang/lắp,
  và để CollectNectar theo sự kiện thu thay vì suy từ mật đã giao.
- [ ] Kiểm tra bỏ dở thao tác, mất/nối mạng, reset/retry, rơi/nhặt và không nhân đôi.
- [ ] Kiểm thử tương tác đủ gần/dễ thấy/đủ cao để bay tới trên Quest; chưa làm
  level design hay thêm tuyến/nhiệm vụ ngoài phạm vi được giao.

### Các phần game còn lại

- [ ] Chốt số tổ/lượt, thời lượng chơi, mục tiêu và điều kiện chuyển hai màn.
- [ ] Triển khai màn 2 sau khi có quy luật/dữ liệu và phạm vi được duyệt (mục 8).
- [ ] Khảo sát lại NPC.cs/NPCDialogue.cs kế thừa trước khi tái sử dụng.
- [ ] **Đề xuất chưa chốt:** một NPC ong hướng dẫn qua hai màn, thoại theo kịch bản;
  ba cutscene 15–30 s: giới thiệu, biến cố mặn, kết quả. Chưa làm thoại AI tự do.
- [ ] Viết kịch bản/thoại/phụ đề, chuẩn bị rig/animation/âm thanh và điểm kích hoạt.
  Lượt khảo sát trước chưa có PlayableDirector cho Bee Bloom; cần khảo sát lại
  khi bắt đầu, không coi ý tưởng cutscene là nội dung đã có.
- [ ] Giữ head tracking trong cutscene; có skip, khôi phục input/nhiệm vụ sau skip,
  pause và retry; cân thoại với tiếng cánh.
- [ ] Menu, âm thanh, pause/resume, hướng dẫn đầu game, hoàn thành/thua, retry,
  chuyển màn, quay menu và lưu/reset tiến độ theo thiết kế được duyệt.
- [ ] Chơi thử đầu–cuối bằng asset thật; cân tài nguyên, thời lượng, độ khó.
- [ ] Kiểm thử Quest: controller/joystick, tracking đầu, độ rơi/phanh/hold,
  mỏi tay/chóng mặt, HUD/độ cao/la bàn, va chạm nhiều vị trí, tiếng ong,
  FPS/frame time/bộ nhớ/stereo/rung tay, mạng và sleep/resume.
- [ ] Kiểm thử PC thứ hai và Quest cùng bộ phiên bản; chỉ chuẩn bị yêu cầu hệ
  thống thi khi người dùng cần, không coi đó là mục tiêu hiện tại.

## 8. Đề xuất màn 2 và trực quan hóa GAMA

Phần này lưu để nghiên cứu/phê duyệt sau, chưa phải tính năng đã triển khai.

### Màn 2 và cơ sở khoa học

Ý tưởng: nước mặn vào làm khu vực nước ngọt biến đổi, nguồn hoa ảnh hưởng và ong
khó kiếm ăn. Hướng đề xuất: mặn/thời gian phơi nhiễm tác động khác nhau lên cây;
nguồn hoa thay đổi khiến ong đổi tuyến, có thể đi xa hơn. Tái sử dụng map/tuyến
quen từ màn 1 để thấy sự thay đổi.

- [ ] Chốt địa điểm đại diện, loài cây/ong, nguồn số liệu và mục tiêu thắng/thua.
- [ ] Có dữ liệu cho quan hệ độ mặn–thời gian phơi nhiễm–sức khỏe/tài nguyên hoa;
  không mặc định mọi loài cùng ngưỡng hoặc mật luôn giảm tuyến tính.
- [ ] Định nghĩa đơn vị độ mặn, bước mô phỏng và hệ số rút ngắn thời gian; ghi rõ
  khi nén quá trình dài ngày thành vài phút chơi.
- [ ] Phân biệt dữ liệu đo, tham số nghiên cứu, giả định gameplay và đầu ra mô hình;
  hiệu chuẩn/đối chiếu và xin góp ý chuyên môn khi có điều kiện.
- [ ] Theo dõi độ mặn theo vùng, phơi nhiễm, sức khỏe cây, hoa nở/tài nguyên,
  chuyển phấn, quãng đường kiếm ăn và mật giao về.
- [ ] **Đề xuất cần kiểm chứng:** thiên địch vốn ở vùng nguy hiểm, tuyến kiếm ăn
  đổi làm ong dễ gặp chúng hơn. Chưa có cơ sở để đặt mặn tăng → địch đông/hung dữ.
- [ ] Nếu dùng AI địch cũ, rà tác động thật; khảo sát trước thấy hàm tấn công gọi
  sự kiện kiệt sức mà chưa trừ stamina, cần kiểm tra lại trước khi tích hợp.
- [ ] Không cho thụ phấn chữa đất mặn/phục hồi rừng tức thì; giữ vùng còn phù hợp
  để người chơi có lựa chọn. Không đồng loạt đổi màu mọi cây ngay lập tức.
- [ ] Không áp thẳng ngưỡng rừng nước ngoài cho ĐBSCL hoặc đánh đồng sodium trong
  mật hoa với stress mặn của cây. Thiếu dữ liệu thì ghi rõ là minh họa/giả định.

GAMA chạy luật do mình viết, không tự bảo đảm tính khoa học. Pilot mật/phấn
hiện tại chưa chứa model độ mặn, sức khỏe rừng hoặc thiên địch đã hiệu chuẩn.

### Bản đồ quan sát GAMA

Đã có HoneyPilotMap đơn giản. Bản đồ đẹp/giàu dữ liệu dưới đây vẫn là đề xuất:

- [ ] Bản đồ 2D nhìn trên cao theo bố cục Unity, nền xanh dịu, nước xanh, đường
  đất, điểm nhấn vàng mật; bản đồ lớn + bảng chỉ số gọn + một biểu đồ.
- [ ] **Đề xuất bản đầu:** ảnh map Unity từ trên cao, căn gốc/hướng/trục/tỷ lệ;
  thay thế bằng hình học sông/ruộng/đường nếu cần tô vùng theo dữ liệu.
  Ảnh chỉ là nền, ID/tọa độ/trạng thái là dữ liệu riêng, không suy từ pixel.
- [ ] Icon ong, hướng/độ cao, vệt vàng mảnh đoạn bay gần đây có bật/tắt.
- [ ] Hoa theo loài, lượng mật, khỏe/cạn/héo và chuyển phấn bằng cả màu/ký hiệu;
  trạng thái phấn độc lập với tài nguyên/sức khỏe. Nhấp xem chi tiết thay vì phủ nhãn.
- [ ] Tổ hiển thị số cục/5 và lượt theo thiết kế chính, không lấy 1/1 pilot làm mục tiêu cuối.
- [ ] Lớp độ mặn/sức khỏe có bật/tắt khi model có dữ liệu; hiệu ứng xác nhận tương tác.
- [ ] Chỉ số mật/cục đang mang/đã giao, hoa nhận phấn/khỏe/héo, biểu đồ theo thời
  gian, trạng thái kết nối và độ mới dữ liệu; hỗ trợ trình diễn toàn màn hình.
- [ ] Chuẩn bị icon và ảnh nền, kiểm tra khớp vị trí, cập nhật/mất/nối mạng trước
  khi hoàn thiện hình ảnh. Không đổi rig/HUD/bố trí Unity chỉ để khớp bản đồ GAMA.

Nội dung display GAMA có thể thiết kế bằng model (hình học, ảnh, chữ, màu, lớp,
biểu đồ), còn IDE có khung làm việc riêng. Chưa cần dựng lại map Unity 3D;
cú pháp/khả năng 3D OpenGL cần đối chiếu phiên bản khi triển khai.

## 9. Kiểm chứng và giới hạn

Đây là bằng chứng từ các lượt trước và artefact đã đọc; không cộng các lần chạy
lại cùng bộ thành số lượng kiểm tra độc lập và không coi PC là nghiệm thu Quest.

| Nhóm | Kết quả đã lưu | Phạm vi |
| --- | --- | --- |
| Flight V2 / Surface Flight / Wing Audio | 55 / 13 / 7 kiểm tra đã qua trong các lượt trước | Editor; bề mặt từng thử 29 mesh, 242 điểm rơi, khoảng hở nhỏ nhất ~0,353 m với thiết lập thử 0,35 m |
| Direct pilot trước SIMPLE | 32 kiểm tra model, 15 client C# ở lượt direct | Chẩn đoán lịch sử, không phải bộ client SIMPLE mới |
| SIMPLE pilot | 32 kiểm tra giao dịch, 13 client C#; từng qua GUI và headless | Sau chuẩn hóa đã chạy lại trên GUI; không suy ra mọi phiên bản server khác |
| Cấu hình ngoài | 15 kiểm tra đã qua | File thiếu/sai/schema, endpoint, giữ tuning, loại đường dẫn PC |
| Chuyển thư mục/đổi IP | Đã qua trên cùng PC | Đường dẫn có khoảng trắng/dấu nháy, thư mục checkpoint riêng, nối lại giữ tiến độ; đổi endpoint khi Play không sửa Inspector |
| APK cấu hình ngoài | Build thành công 18/10 lúc 23:15; 0 lỗi, 492 cảnh báo; file ~114 MB | ARM64, INTERNET, có bộ đọc cấu hình; chưa cài/chạy trên Quest |
| Bảo toàn giá trị người dùng | Lượt SIMPLE đối chiếu 1.526 component; lượt cấu hình đối chiếu 21 scene/prefab không đổi | Bằng chứng của các lượt đó, không phải lý do bỏ kiểm tra file mới ở lần sửa sau |

Menu Editor: **Bee Bloom → Check Flight V2 / Check Surface Flight / Check Wing Audio**,
chạy khi dừng Play; các bộ dùng môi trường thử riêng. Trong kiểm tra PC từng gặp
Oculus XR không khởi tạo vì không có phiên kính; không lấy đó làm lỗi GAMA hay
bằng chứng tracking trên kính đã hoạt động.

Trong thư mục pilot, `python validate_simple.py` chạy qua WebPlatform/LinkToUnity
thật, dùng checkpoint/player riêng. **Chỉ chạy khi Unity dừng**; script reset
experiment, không chạy giữa lúc chơi. `validate_gama.py` dành cho direct legacy.

`BeeSimpleChecks.Begin()` qua MCP trong Play dùng cờ Editor
`BeeBloom.Simple.IsolatedChecks` để tạm tắt pilot ở lần Play kế tiếp, không lưu
cờ vào scene. `BeePortableChecks.Run()` kiểm tra cấu hình. Khi thử nền qua MCP,
`Application.runInBackground` từng được bật tạm rồi khôi phục, không lưu vào
PlayerSettings hoặc thay cấu hình người dùng.

Kết quả máy đọc hiện nằm ở:

- `Experiments/BeeBloom-HoneyPilot/Runtime/simple-validation.json`.
- `Library/HoneyPilot/unity-simple-validation.json`, `portable-validation.json`,
  `portable-relocation-validation.json`, `portable-live-validation.json`.
- `Library/HoneyPilot/simple-apk-build.json`; `size` trong BuildReport không phải
  kích thước file APK trên ổ đĩa. Dùng kích thước file thật khi ghi dung lượng.

**Chưa xác minh:** Quest/controller thực, tracking/stereo/âm/rung/độ thoải mái,
LAN từ kính, PC thứ hai, server ban tổ chức, multiplayer và mô phỏng sinh thái
có dữ liệu hiệu chuẩn. Bảng kiểm tra trên không thay thế các mục này.

Lab riêng `Experiments/BeeBloom-GAMA` dùng direct WebSocket, hình khối desktop,
10 hoa/5 cục và `stress` minh họa. Từng qua 31 kiểm tra Unity standalone, nhưng
reconnect tạo phiên mới; không dùng giao thức/luật pollination của lab thay pilot
chính. Giữ README tại lab, đã gộp kết quả kiểm chứng để tái hiện lịch sử, không làm
nguồn tiến độ Bee Bloom hiện tại.

## 10. Ước lượng tham khảo

Các mốc từng trao đổi trước khi có nền pilot/kết nối hiện tại:

| Phạm vi giả định | Ước lượng cũ |
| --- | --- |
| Một màn đầu–cuối để thử | 7–10 ngày làm việc |
| Hai màn gọn, GAMA tối thiểu, một NPC, ba cutscene ngắn | 20–30 ngày, khoảng 4–6 tuần |
| Bản trau chuốt hơn | 30–50 ngày, khoảng 6–10 tuần |

Giữ để tham khảo quy mô, **không phải thời gian còn lại hay lịch giao hàng hiện tại**.
Cần dự toán lại sau khi chốt asset/phạm vi và thử kính. Giả định một lập trình
viên tập trung 5 ngày/tuần, tận dụng map/code, phản hồi/asset đến đúng lúc, không
mở rộng multiplayer hoặc nhiều nhánh NPC. Thời gian chờ và hiệu chuẩn khoa học
có tiến độ riêng. Tự làm rig/animation/thoại hoặc mở phạm vi sẽ đổi ước lượng.

Thứ tự gợi ý khi được giao tiếp: xác nhận asset/phạm vi → nghiệm thu pilot trên
kính → mở rộng vòng 5 cục → dữ liệu/model màn 2 → NPC/cutscene/menu → tích hợp,
cân bằng và kiểm thử đầu–cuối. Tiếp tục phát triển Unity–GAMA song song.

## 11. Tài liệu nền và nguồn tham khảo

Tài liệu này thay chín file `BeeBloom-*` trước đây: TODO, Progress, Game Design
Draft, GAMA Display Proposal, SIMPLE, Connection Setup, FlightV2, SurfaceFlight,
Audio. Nội dung còn dùng đã được đưa vào các mục trên; bỏ thông tin trùng và các
khẳng định lỗi thời như “chưa có GAMA”, “chưa có hoa/tổ/mật”, “phải build lại để
đổi IP”. README ở package chỉ dẫn về đây để không có hai hướng dẫn vận hành.

Tài liệu `Report/`, `Docs/TreeBarrier_Mechanic.md` và
`Assets/Scripts/VU2/Documentation/` là nền kế thừa/các hệ thống khác, được giữ
nguyên; không dùng các cơ chế thu hoạch/công cụ/điểm mặn trong đó làm thiết kế
Bee Bloom đã duyệt. Lab thử nghiệm cũ có
[README và kết quả lịch sử](../Experiments/BeeBloom-GAMA/README.md).

Phiên bản đã kiểm tra: Unity **6000.6.2f1**, GAMA **2025.6.4**, LinkToUnity
**2.0.0.v20260113-1516**, SIMPLE WebPlatform package **4.0.0**, checkout nền
**1d116cf**, Node **24.11.1** trên máy phát triển.

Nguồn đã dùng trong các khảo sát trước; không xác thực lại web trong lượt gom tài liệu:

- [SIMPLE WebPlatform](https://github.com/project-SIMPLE/simple.webplatform),
  [Unity VR template](https://github.com/project-SIMPLE/simple.toolchain/tree/Unity-6/Unity%20Template%20VR),
  [chạy model/game](https://github.com/project-SIMPLE/simple.toolchain/wiki/01-Running-a-model-game).
- [Nhập dữ liệu GAMA](https://github.com/project-SIMPLE/simple.toolchain/wiki/02-Importing-Data-from-GAMA),
  [xuất dữ liệu Unity](https://github.com/project-SIMPLE/simple.toolchain/wiki/03-Export-Data-from-Unity).
- [GAMA Server](https://gama-platform.org/wiki/HeadlessServer),
  [Displays](https://gama-platform.org/wiki/Displays),
  [Statements](https://gama-platform.org/wiki/Statements),
  [json_file](https://gama-platform.org/wiki/OperatorsIM#json_file),
  [from_json](https://gama-platform.org/wiki/OperatorsDH#from_json).
- [USGS: rừng nước ngọt và thay đổi độ mặn](https://www.usgs.gov/programs/climate-research-and-development-program/news/temporary-boost-surviving-trees-tidal).
- [Nghiên cứu sodium trong mật hoa](https://pmc.ncbi.nlm.nih.gov/articles/PMC8889166/):
  không dùng thay nghiên cứu ảnh hưởng mặn lên cây/hoa ở bối cảnh đã chọn.
