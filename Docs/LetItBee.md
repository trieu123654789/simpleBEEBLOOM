# Let It Bee — Kế hoạch, tiến độ và hướng dẫn phát triển

Cập nhật: **21/10/2026**. Đây là tài liệu chính của Let It Bee, hợp nhất các bản
plan, checklist, tiến độ và hướng dẫn kỹ thuật đã tạo trong quá trình làm việc.
Khi thay đổi phạm vi hoặc hoàn thành công việc, cập nhật tại đây.

Đợt cập nhật 19/10 bổ sung chẩn đoán APK trên Quest và thử đậu hoa. Scene, prefab,
HUD và thông số bay người dùng đã lưu được giữ nguyên; hiệu ứng đậu hoa tạo lúc chạy.

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

### Bản đổi tên đã kiểm tra ngày 21/10

Thư mục hiện hành là `Experiments/LetItBee-HoneyPilot`, scene là
`Assets/Scenes/VU2/SCN_LetItBee.unity`. Các ghi nhận ngày trước ở dưới là lịch sử
kiểm chứng; không dùng tên APK lịch sử làm tên file của bản build mới.

Schema gameplay hiện hành là `letitbee-honey-pilot-v1`. Client mới chấp nhận cả
schema BeeBloom cũ; server trả riêng envelope `let_it_bee` và envelope tương thích
`bee_bloom` có schema cũ cho APK cũ. Schema quan sát `beebloom-world-v1` được giữ
nguyên ở hai đầu để không đổi giao thức tọa độ. Đây là các tên tương thích kỹ thuật.

Checkpoint cũ chỉ được chuyển schema khi session, vị trí và bán kính đều khớp.
Server lưu bản `TEN-CHECKPOINT.json.before-letitbee.json` trước khi chuyển, giữ
nguyên revision, mật, phấn và receipts. Cấu hình không khớp bị từ chối mà không
ghi checkpoint. Không tự reset lượt chơi khi đổi tên.

`Connect-Quest.ps1` mặc định cập nhật app `com.quoctrieu.letitbee`. Với APK BeeBloom
cũ, thêm `-PackageName com.quoctrieu.beebloom`. Hai package là hai app riêng trên
Android; script không gỡ app cũ hay xóa dữ liệu. Tiến độ gameplay vẫn do checkpoint
GAMA theo session quản lý. Mỗi app nhận profile mới và profile tương thích tên cũ;
từng bước upload/activate được kiểm tra mã lỗi riêng.

Đã kiểm chứng với server thật: 32 kiểm tra gameplay trực tiếp GAMA, 32 kiểm tra
qua SIMPLE, 25 kiểm tra client Unity C# và 11 kiểm tra chuyển đổi schema/checkpoint.
Bản sao checkpoint thật (revision 34, đã gắn 1 cục, 34 receipts) chuyển đổi đúng;
checkpoint thật không bị sửa. Kiểm tra Editor còn bao gồm 17 kiểm tra cấu hình,
21 tương tác, 26 đậu hoa, 23 grip, 53 hiệu ứng và 18 đồng bộ thế giới.
Chưa kiểm tra APK đổi tên trên Quest vì không có thiết bị ADB trong lượt này.

Mục tiêu hiện tại là chuẩn hóa nền kết nối và thử vòng **hoa → mật → tổ** trước
khi phát triển gameplay chính. Chưa yêu cầu đóng gói đi thi hoặc làm level design.

| Hạng mục | Đang có | Phần chưa hoàn thành |
| --- | --- | --- |
| Bay | Hai ray quẹt độc lập, nhịp một/hai tay, lướt, phanh và trigger trái giữ vị trí | Cân chỉnh, độ thoải mái và hướng joystick trên Quest |
| Map | Môi trường kế thừa SIMPLE, xử lý va chạm và phản hồi bay sát bề mặt | Kiểm tra toàn map, asset mới, hiệu năng trên kính |
| Camera/HUD | Đã có rig, HUD và các đợt sửa trước | Nghiệm thu tracking, độ cao, la bàn, bố cục và dữ liệu gameplay trên kính |
| Âm thanh | Chubby Bumblebee loop, nhấn âm theo pha cánh | Nghe và cân chỉnh trên thiết bị |
| Hoa/mật/tổ | Pilot hai hoa thuộc hai cây, một cục mật, một ô tổ; GAMA xác nhận | Luồng chính đủ 5 cục/lượt và nhiệm vụ đầy đủ |
| Kết nối | SIMPLE WebPlatform → LinkToUnity → GAMA; IP ngoài APK, đường dẫn phía server; đã nhận GAMA ready trên Quest qua hotspot | Nghiệm thu toàn vòng tương tác trên kính và PC thứ hai |
| GAMA | Model giao dịch, checkpoint, bản đồ pilot và vị trí ong liên tục khi kết nối | Model sinh thái đã hiệu chuẩn, bản đồ quan sát hoàn thiện |
| NPC/cutscene | Có ý tưởng và code NPC kế thừa để khảo sát | Chưa có NPC/cutscene Let It Bee hoàn chỉnh được nghiệm thu |
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

Scene chính: `Assets/Scenes/VU2/SCN_LetItBee.unity`, đối tượng **LetItBee Honey Pilot**.
Khi đã khởi chạy SIMPLE theo mục 4, bắt đầu từ Edit Mode rồi nhấn Play.

**Bản thao tác 3.2 đã build và cài cập nhật lên Quest ngày 20/10 lúc 15:03.** APK:
`Experiments/LetItBee-HoneyPilot/Build/LetItBee-Quest-honey-interaction-v3.2.apk`.
Build thành công, 0 lỗi, 146 cảnh báo; APK đã kiểm tra ZIP và metadata đúng 3.2.
Quest PID 14440 nhận `GAMA ready` lúc 15:04:44; SIMPLE ghi nhận Player_62 in-game,
experiment LetItBeeVR 49964 RUNNING. Checkpoint lúc nối là revision 0, hai hoa
còn mật, chưa có cục mật và ô tổ trống. Chưa nghiệm thu hoạt ảnh 3.2 bằng mắt trên kính.
Log app thật lúc 14:26 có `Honey interaction 3` và `GAMA ready`; SIMPLE xác nhận
Player_62 đang in-game, LetItBeeVR RUNNING. Bản cũ `branch-perch` vẫn dùng grip
bấm đổi cầm/thả. Sau khi cài 3.1, checkpoint thật ghi nhận `pick`, `drop`,
`pick`, `deposit` được GAMA chấp nhận ở revision 80–83, ô tổ đạt 1/1.
Đây là bằng chứng đường giao dịch đã hoạt động trên Quest; độ đẹp/thoải mái
của hoạt ảnh vẫn cần người chơi đánh giá.

| Bước | Thao tác và kết quả |
| --- | --- |
| Ghé hoa A | Giữ ổn định, đậu hẳn hoặc chạm/ngắm nhụy chủ động khoảng 0,4 giây; GAMA xác nhận nhận phấn. Bay ngang không tự thụ phấn |
| Đậu hoa | Giữ trigger trái cạnh cành/lá; ong phanh rồi đáp chậm khi nằm trong vùng tiếp cận. Thả trigger trái để rời cành |
| Lấy mật | Khi đã đậu, giữ trigger phải đủ `Collect Duration`; nếu chưa đậu thì ngắm tâm hoa bằng tay phải. GAMA xác nhận rồi cấp cục mật |
| Cầm mật | Đưa tay gần hoặc ngắm đúng cục mật, giữ grip phải (cò bên hông, ngón giữa) để cầm; không bắt buộc giữ trigger trái. Trigger phía trước, ngón trỏ, dùng lấy mật |
| Chuyển phấn | Đậu hoặc chủ động tiếp xúc hoa B của cây khác nhưng cùng mã loài; GAMA xác nhận cây B nhận phấn, kể cả lúc đang mang mật |
| Gắn vào tổ | Giữ grip, đưa cục mật vào ô được đánh dấu rồi thả grip; xác nhận xong mới hiện 1/1. Có thể giữ trigger trái để ổn định khi ngắm |
| Thả ngoài tổ | Thả grip ngoài ô: cục mật ở vị trí thả để nhặt lại; chưa có vật lý rơi/va chạm cục mật hoàn chỉnh |

Grip mới là giữ để cầm, thả để buông. Ý định được giữ qua lúc GAMA bận, kể cả
thả trước khi nhận xác nhận cầm; bóp lại hủy lần thả chưa gửi. Trong VR,
`Require Position Hold In Vr` yêu cầu giữ vị trí khi nhận/chuyển phấn và lấy mật từ hoa.
Input trigger/grip phải được pilot giữ riêng lúc chạy để không kích hoạt nút bay
cũ; không sửa binding hoặc giá trị bay đã lưu. Thả trigger trái để bay mang mật.
Desktop dùng **F** thay trigger phải, **G** thay grip phải; vẫn kiểm tra khoảng
cách và hướng ngắm, không bỏ qua luật GAMA. **H** giữ để thử đậu hoa trên desktop.

Đậu hoa là thao tác chủ động, không tự kéo ong từ xa. Điểm đậu cố định trong thế
giới, có kiểm tra va chạm và trần bay; animation hoa không kéo camera theo. Cánh
và tiếng cánh nghỉ khi đậu, hoa nhún nhẹ; nhận phấn thành công có phản hồi hạt phấn.
Không thay đổi tư thế tracking cục bộ của kính. Bản sửa 20/10 chọn đầu cành/lá
gần hướng tiếp cận. Bản thao tác V3 đo chiều dài xương cánh hoa kể cả khi nụ
đang khép, tăng khoảng nhìn theo kích thước bông và giữ mắt trên mặt nhụy khi
cần. Các giá trị `Perch View Distance` và `Perch Height` đã lưu vẫn làm mốc tối
thiểu. Vòng nhỏ đánh dấu cành để đáp; camera không bị ép xoay về hoa. Bản render
kiểm tra asset thật có bán kính bông 0,805 m và mắt cách mặt hoa 1,375 m, thấy
trọn bông. Đây chưa là nghiệm thu cảm giác trên Quest.

[Ảnh kiểm tra điểm đậu và nhãn V3](Previews/HoneyInteraction-V3-perch.png) dùng
asset thật trong preview scene riêng, camera hướng vào hoa chỉ để kiểm tra bố
cục; runtime không tự xoay tracking của người chơi theo góc ảnh này.

Mỗi thời điểm chỉ hướng dẫn quanh mục tiêu đang thao tác: mật đã tạo thì nhãn
đặt ngay cục mật, cầm mật thì ô tổ hướng dẫn thả; không hiện lời mời lấy/cầm mật
ở cả hai hoa. Điểm mật trên bông được gửi riêng sang GAMA, cùng điểm người chơi
nhìn thấy. Khi thả/gắn, GAMA kiểm tra tâm mật và giới hạn nó trong 0,25 m từ tay;
giữ các bán kính đã lưu. Giao dịch cũ không có tâm mật vẫn dùng điểm tay.
Nhãn được giới hạn bề ngang 0,6 m, chiều cao 0,11 m và thu theo khoảng cách tới
mắt; không đổi `Label Scale` đã lưu. Chạm/ngắm đúng mật có viền sáng và rung nhẹ,
giữ trigger lấy mật có vòng tiến độ. Thả trong lúc chờ ACK giữ nguyên tâm mật
đã gửi, không tiếp tục kéo theo tay. Bị từ chối hiện cách thử lại tại đúng mục tiêu.

Checkpoint khảo sát đầu lượt 20/10 có hai hoa đã thụ phấn, một cục mật sẵn ở
hoa B. Mật sẵn chặn tạo cục thứ hai theo luật pilot; đây không phải bằng chứng
mất kết nối. Người chơi sau đó đã cầm và gắn cục này vào tổ thành công.
Từ bản 3.1, grip đọc cả analog và nút nhấn (`gripPressed`/`gripButton`) qua XR
và các thiết bị Input System có usage RightHand, tránh chỉ lấy thiết bị đầu tiên
ở XRNode. Không dùng input tay trái hoặc đổi binding bay đã lưu. Bộ kiểm tra
21 trường hợp có thiết bị XR chỉ cung cấp nút nhấn, thả nút và cách ly tay trái.
Nhãn gọi rõ "nút bên hông tay phải". Vòng vàng dưới bông là dấu đậu cành/lá,
chỉ hiện khi giữ vị trí để chuẩn bị đậu và chưa có mật; mật sẵn hoặc đang cầm
thì ẩn dấu này để không nhầm với vùng cầm.
Lượt thử Quest V3 lúc 14:28 đã nhận tiếp xúc hoa B (`visit -> visited`); tay vào
vùng cầm (`grab=True`), nhưng input ghi nhận trigger phải và `grip=False`, chưa
phát sinh `pick`. Đợt 3.1 sau đó đã có giao dịch cầm/gắn thật như ghi ở trên.

**Lượt thử hoa sinh mật từ đầu, 20/10 lúc 14:54:** dừng app và experiment trước
khi sao lưu nguyên checkpoint revision 83 (đã gắn 1/1) tại
`Experiments/LetItBee-HoneyPilot/Runtime/backups/before-flower-creation-20261020-145452.json`.
Sau đó chỉ đặt lại tiến độ runtime: hai hoa còn mật, chưa mang phấn, chưa có
cục mật và ô tổ trống; giữ nguyên session, vị trí, bán kính và scene đã lưu.
Để xem mật hình thành, tới hoa, giữ trigger trái để ổn định/đậu, ngắm nhụy bằng
tay phải rồi giữ trigger phải đủ 1,2 giây sau khi nhận phấn được xác nhận.
Giọt mật hội tụ và cục mật xuất hiện sau xác nhận GAMA. Grip dùng để cầm cục đã
tạo, không khởi tạo mật. Không chạy lại bộ kiểm tra giao thức trên phiên chơi thật.
Muốn khôi phục lượt cũ: dừng app và experiment, sao lưu lượt hiện tại, chép bản
backup trên đè đúng file cùng session trong `Runtime/checkpoints`, rồi khởi chạy
lại experiment và app. Không cần đổi session trong scene hoặc build lại APK.

**Hoạt ảnh lắp mật 3.2:** bản cũ đưa tâm mật tới điểm tương tác phía trước tổ,
cách tâm miếng mật gốc khoảng 12 cm, giữ kích thước cầm tay và bỏ góc roll của ô.
Bản mới lấy tâm, rotation và kích thước từ renderer miếng mật gốc đang ẩn:
căn hướng/kích thước trước miệng ô trong 0,36 giây, trượt theo pháp tuyến vào
đúng tâm trong 0,6 giây; sau đó phát tiếng khớp, rung và niêm viền.
Chỉ bắt đầu khi GAMA chấp nhận `deposit`; không đổi checkpoint hoặc ô tương tác,
không tự gắn chỉ vì ở gần, không sửa camera hoặc dữ liệu Inspector đã lưu.
Trạng thái khôi phục cũng dùng đúng tâm và kích thước đã ngồi trong ô.
Bộ kiểm tra preview dùng asset thật đã qua: 21 kiểm tra tương tác và 53 kiểm tra
trình bày (bao gồm từng mẫu đường trượt), xác nhận hình học cuối khớp renderer
gốc, theo chuyển động tổ, giữ nguyên scene và không ghi tư thế camera.

[Xem thử hoạt ảnh trượt vào ô tổ 3.2](Previews/HoneySocket-V3.2.gif): render từ
asset và code thật trong preview scene riêng, chưa phải ghi hình người chơi trên Quest.

### Hiệu ứng hoa → mật → tổ (bản thử 19/10)

Đã thêm lớp trình bày `BeeHoneyFeedback`, tạo trong runtime, không cần gắn thêm
component hoặc lưu lại scene để bật bản mặc định. Không thêm chân hoặc bộ phận ong.

- **Đậu/rời hoa:** dùng các xương cánh hoa có sẵn, hoa hạ nhẹ và hồi đàn hồi;
  giữ pose Animator, khôi phục khi tắt pilot. Không tác động camera hay lực bay.
- **Lấy mật:** ba dòng giọt nhỏ hội tụ, âm nền tăng theo tiến độ, rung tay ngắt
  nhẹ. Nhả sớm thì rút hiệu ứng; chờ GAMA giữ trạng thái hội tụ, không cấp mật.
  Ba giọt mật có thể tích lớn dần rồi nhập lại. Khi được chấp nhận, mesh mật
  thành hình với một nhịp nảy ngắn và âm thanh mềm.
- **Nhấc/cầm mật:** sau xác nhận grip, mật nhấc từ đúng vị trí đang thấy trong
  khoảng 0,34 giây, co giãn nhẹ và kéo một sợi mật nhớt rồi đứt. Có giọt nhỏ,
  âm kéo/dứt và hai nhịp rung nhẹ. Khi mang, mật đi theo tay với độ trễ vị trí
  tối đa 3,5 cm và góc tối đa 10°, tránh trôi xa tay khi quay nhanh.
- **Phấn:** cụm bụi nhỏ cạnh điểm mang đồ/tay phải, đi cùng mật khi đang cầm.
  Sau xác nhận giao dịch, phấn bay từ hoa tới điểm mang, hoặc tới hoa khác;
  hoa nhận phấn mở nhẹ, hiện dấu phấn nhỏ. Phấn và mật vẫn là hai trạng thái riêng.
  Phản hồi `same_visit` sau khi khôi phục phiên chỉ giải phóng hàng đợi, không
  liên tục gửi lại lượt ghé và không phát thưởng thụ phấn giả.
- **Gắn tổ:** viền lục giác sáng khi tâm mật ở đúng vùng, chưa tự hút mật. Sau ACK,
  mật xoay khớp trước miệng ô rồi ép vào trong, tổng khoảng 0,6 giây. Mật nén nhẹ,
  âm/rung khớp lúc tới ô; đường sáng chạy quanh viền như niêm sáp, sáu dấu lục
  giác nhỏ lóe nhẹ rồi tắt. Những dấu này chỉ là hiệu ứng, không thêm ô gameplay.
  Replay cùng event ID không phát lại phần thưởng.
- Material mật được tạo riêng lúc chạy để có mặt khối, phản chiếu mềm và bọt
  nhỏ chuyển động bên trong; giữ mesh,
  màu/texture nguồn, không ghi lại material đã lưu. Điểm hiển thị trên nhụy được
  hiệu chỉnh theo scale daisy vì bone Head thấp hơn bề mặt; tọa độ giao dịch GAMA
  và các bán kính tương tác đã lưu không đổi.

Chỉnh tại **LetItBee Honey Pilot → Honey presentation**:

| Trường | Công dụng |
| --- | --- |
| Enhanced Feedback | Bật lớp hiệu ứng mới; tắt để dùng bộ cue cơ bản |
| Feedback Intensity | Độ rõ của bụi phấn, dòng mật và ánh sáng; mặc định 1 |
| Feedback Volume | Âm lấy/chuyển/gắn mật; mặc định 0,45, độc lập tiếng cánh |
| Feedback Haptics | Cường độ rung của tương tác; mặc định 0,6 |

[Bản xem lấy → nhấc/cầm → gắn mật mới](Previews/HoneyInteraction-V2-2026-10-19.gif)
render từ asset hoa và tổ thật, đủ bốn giai đoạn (97 khung hình),
trong scene thử riêng. Góc camera trong bản xem chỉ phục vụ kiểm tra, không phải
chuyển động camera áp vào người chơi. Đây chưa là nghiệm thu hình ảnh, âm/rung hay
FPS trên Quest. APK `connection-recovery` tạo trước đợt này **chưa có hiệu ứng mới**.

Lấy mật và thụ phấn là hai sự kiện khác nhau. Đứng tại một cây không tự tính
thụ phấn cho chính cây đó; khác mã loài không tính. `chrysanthemum-pilot` là mã
thử nghiệm, chưa xác nhận phân loại sinh học. Nhận phấn không đồng nghĩa đã thụ
tinh/đậu hạt. Cục mật rắn để cầm/gắn là quy ước gameplay.

### Hồi mật để thử lại và đổi bố trí level

Mật, phấn, cục đang cầm và ô tổ thuộc checkpoint **GAMA**, không phải các giá trị
Inspector để sửa tay trong scene. Pilot chỉ có một ô; muốn thử lại sau khi gắn
1/1 phải bắt đầu một lượt mới. Không cần nhờ người khác sửa JSON hoặc đổi Session ID.

1. Đóng Let It Bee trên kính bằng Quit, dừng Play trong Unity nếu đang chạy;
   đợi khoảng 15 giây để SIMPLE nhận kính đã ngắt. Giữ SIMPLE/GAMA đang mở.
2. Mở PowerShell tại `Experiments/LetItBee-HoneyPilot`, chạy:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Reset-HoneyPilot.ps1
```

3. Script tự dừng riêng experiment LetItBeeVR, sao lưu rồi đặt lại **toàn lượt
   thử**: hai hoa có mật, chưa nhận/chuyển phấn, không có cục mật, ô tổ trống.
   Trong SIMPLE chọn Let It Bee - Honey Pilot, Launch/Play LetItBeeVR lại, mở game
   rồi thêm kính vào experiment. Không cần Administrator hay cài APK lại để hồi mật.

Lệnh thường giữ nguyên hình học và bán kính trong checkpoint. Khi đã chủ động
đổi vị trí hoa/tổ hoặc bán kính để bố trí level, dùng thay bằng:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Reset-HoneyPilot.ps1 -UseCurrentLayout
```

Tùy chọn này lưu trữ checkpoint cũ để GAMA nhận cấu hình từ Unity/APK ở lần nối
kế tiếp. Nó không sửa scene, không di chuyển vật thể, không thay thông số bay/HUD.
Ctrl+S vẫn lưu bố trí trong Unity; muốn **Quest thấy bố trí mới** thì phải build/cài
APK chứa bố trí đó. Lệnh reset không truyền scene mới vào một APK cũ.

Có thể thêm `-Preview` để chỉ xem session và thay đổi dự kiến, không ghi dữ liệu
và không dừng experiment. Mặc định Session được đọc từ scene SCN_LetItBee đã lưu;
nếu APK cố ý dùng Session khác, truyền `-Session <ID-32-ký-tự>`.

Dữ liệu mặc định: `Runtime/checkpoints/<Session Id>.json`; backup nguyên trạng:
`Runtime/checkpoints/backups/<Session Id>-before-reset-*.json`. Nếu server dùng
thư mục checkpoint riêng, script đọc `GAMA/server.local.json` và tạo backup trong
thư mục `backups` cạnh checkpoint đó. Không sửa JSON khi experiment đang chạy.
Script hiện phục vụ pilot **hai hoa, một cục, một ô**, chưa là hệ thống respawn
tài nguyên hoặc lưu/chuyển level hoàn chỉnh. Đã kiểm tra reset trên bản sao,
giữ hình học/thông số, ghi nguyên tử và không làm hỏng file bị khóa; tiến độ người
chơi hiện tại chưa bị reset trong lúc thêm công cụ.

### Chỉnh các thông số tương tác

Dừng Play, chọn **LetItBee Honey Pilot**, chỉnh và Ctrl+S khi muốn lưu.

| Trường | Công dụng |
| --- | --- |
| Contact Radius | Khoảng cách điểm tương tác tới tâm hoa; góc nhìn thứ nhất dùng camera, góc nhìn thứ ba dùng thân ong |
| Grab Radius | Khoảng cách tay có thể bắt cục mật |
| Snap Radius | Vùng tay được phép thả mật vào ô |
| Collect Duration | Thời gian giữ trigger phải |
| Aim Radius | Mức hỗ trợ ngắm vào tâm hoa/cục mật |
| Label Scale | Kích thước nhãn riêng của pilot |
| Require Position Hold In Vr | Yêu cầu giữ vị trí để thao tác trong VR |
| Enable Flower Perching | Bật/tắt thao tác đậu hoa |
| Perch Approach Radius | Khoảng cách tới cành hoặc điểm nhìn khi đậu để bắt đầu đáp, mặc định 0,65 m |
| Perch Height | Độ cao mắt trên cành/lá khi đậu, mặc định 0,36 m; vẫn tôn trọng khoảng hở va chạm |
| Perch View Distance | Khoảng nhìn ngang tới tâm hoa khi đậu, mặc định 0,75 m; tự giới hạn trong Contact Radius để GAMA vẫn chấp nhận lấy mật |
| Flower A Perch / Flower B Perch | Tùy chọn kéo Transform trên cành vào; để trống thì chọn đầu lá `Leaf_*_end` gần hướng tiếp cận. Gizmo xanh lá là điểm cành, xanh cyan là vị trí mắt |
| Perch Settle Speed | Tốc độ đáp tối đa, mặc định 0,25 m/s |
| Perch Dwell | Thời gian tối thiểu ở gần hoa trước khi đậu, mặc định 0,4 s |
| Session Id | Phiên tiến độ, giữ nguyên để tiếp tục checkpoint tương ứng |

Điểm gắn/mật đã gắn đi theo animation tổ; Unity gửi vị trí tương tác đang chuyển
động để GAMA kiểm tra. Không dùng tọa độ/độ cao trong tài liệu cũ làm vị trí chuẩn
để áp lại scene người dùng.

Khi **chủ động** đổi bố trí hoa/tổ hoặc bán kính tương tác và muốn thử cấu hình
mới, tạo phiên bằng **VU2 → Bee → Honey Pilot → New session (keep scene tuning)**
ở Edit Mode. Phiên cũ vẫn còn. Dừng/launch lại experiment trước khi đổi phiên.

## 4. Chạy SIMPLE và cấu hình chuyển máy

Các lệnh bên dưới chạy trong thư mục `Experiments/LetItBee-HoneyPilot`.

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
3. Mở `http://localhost:8000`, chọn **Let It Bee - Honey Pilot**, launch
   **LetItBeeVR**, rồi Play/resume. Bấm launch một lần và đợi nạp xong.
4. Mở ứng dụng trên kính (hoặc Play trong Editor khi có runtime XR phù hợp).
   Trong WebPlatform thêm headset đang kết nối vào experiment. Chỉ mở server/cổng
   chưa đủ: **LetItBeeVR phải RUNNING và player phải được thêm vào game**. Inspector phải báo
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

Nếu mạng Windows là **Private** và kết nối từ kính bị firewall chặn, có thể chạy
helper trong **PowerShell Administrator** (ví dụ địa chỉ, thay theo mạng thực tế):

```powershell
.\Set-BeeFirewall.ps1 -LocalAddress 192.168.2.8 -RemoteAddress 192.168.2.0/24
```

Helper chỉ cho inbound TCP **8080**, profile Private, đúng IP PC và IP/subnet kính
được chỉ định; in lại rule sau khi cập nhật. Khi đổi kính, địa chỉ IP kính có thể
khác; khi đổi Wi-Fi, kiểm tra lại IP, subnet và profile của rule. File cấu hình
IP ngoài APK được lưu riêng trên **từng kính**, không tự đi theo khi đổi thiết bị.

Khi đổi kính, xóa player kính cũ khỏi experiment trong SIMPLE rồi thêm kính mới.
Trạng thái `in_game` trong middleware chưa chứng minh pilot đã nhận `GAMA ready`.
Đợt đổi kính 20/10 ghi nhận ID cũ `Player_62` còn giữ quyền pilot, kính mới
`Player_182` bị `pilot_owned_by_another_player`. Model đã sửa: `remove_player`
nhả quyền của ID bị xóa; `configure` cũng thu hồi owner không còn trong danh sách
agent. Owner đang hiện diện vẫn được bảo vệ. Không xóa checkpoint, đổi session
hoặc build APK để chuyển kính; sau cập nhật model cần stop/launch experiment một lần.
Năm kiểm tra GAMA riêng đã qua, gồm giới hạn một player, nhả quyền, kính thay thế
nhận lại nguyên trạng checkpoint và thu hồi owner mồ côi.
Kính mới PID 31690 đã nhận `GAMA ready` lúc **16:09:49 ngày 20/10**;
Player_182 in-game, LetItBeeVR 56790 RUNNING. Checkpoint sau khi nối so với trước
sửa giữ nguyên (revision 0, chưa có cục mật, ô tổ trống); scene và ProjectSettings
cũng giữ nguyên hash. Chưa xác nhận thao tác hoa bằng mắt trên kính ở đợt nối này.

Launcher nạp `SimpleHeartbeatCompat.mjs` cho WebPlatform 4.0: khi socket mới thay
socket cũ, hủy interval heartbeat cũ trước khi nó tích lũy qua các lần nối lại.
Giữ nguyên thông điệp SIMPLE và interval hiện hành; không sửa mã cài đặt vendor.
Kiểm tra adapter bằng `node check-heartbeat-compat.mjs` trong thư mục pilot.

### Đổi IP ngoài scene/APK

```powershell
.\Set-BeeConnection.ps1 -Endpoint 'ws://192.168.2.8:8080'
```

Thay IP ví dụ bằng IPv4 LAN của PC. Lệnh tạo `Runtime/letitbee-connection.json`:

```json
{
  "schema": "letitbee-connection-v1",
  "simpleEndpoint": "ws://192.168.2.8:8080"
}
```

Editor đọc file trên. APK đọc cùng tên tại `Application.persistentDataPath`, với
package hiện tại là:

```text
/sdcard/Android/data/com.quoctrieu.letitbee/files/letitbee-connection.json
```

Để gửi vào Quest: cài/mở app một lần, cắm USB và cho phép USB debugging. Thay IP,
đường dẫn ADB thật trước khi chạy lệnh:

```powershell
.\Set-BeeConnection.ps1 -Endpoint 'ws://IP-MAY-PC:8080' -PushToQuest -AdbPath 'DUONG-DAN/adb.exe'
```

Bỏ `-AdbPath` nếu ADB trong PATH; có nhiều thiết bị thì thêm `-Serial`.
`localhost`/`127.0.0.1` trên Quest là chính chiếc kính, không phải PC.

**Cập nhật Quest thuận tiện hơn khi đổi mạng (20/10):** mở PowerShell tại thư mục
pilot, cắm một kính đã cho phép USB debugging và chạy:

```powershell
.\Connect-Quest.ps1
```

Script tự lấy IPv4 theo route mặc định và tìm ADB trong PATH/Unity Hub; lưu profile
Quest riêng ở `Runtime/quest/`, gửi vào kính. Không ghi đè profile Editor, scene
hoặc Inspector. Nếu có nhiều card mạng, truyền `-PcAddress IP-PC-CUNG-MANG-QUEST`;
nhiều kính dùng `-Serial`. `-PrepareOnly` chỉ tạo file, không tác động kính.

Nếu gặp `File.Replace: Unable to remove the file to be replaced`, cập nhật
`Set-BeeConnection.ps1` rồi chạy lại cùng lệnh Connect-Quest. Bản sửa 20/10 giữ
nguyên file khi endpoint không đổi; khi đổi IP dùng rename nguyên tử trên Windows
và thử lại ngắn nếu file tạm bị khóa, không xóa/truncate cấu hình đang hoạt động.
Đã kiểm tra bằng Windows PowerShell 5.1: tạo/thay file, bỏ qua khi không đổi,
lỗi khóa giữ nguyên JSON cũ, dọn file tạm và ghi lại sau khi mở khóa.

Khi đổi Wi-Fi/IP hoặc rule cũ không khớp, chạy **PowerShell Administrator**:

```powershell
.\Connect-Quest.ps1 -UpdateFirewall -InstallApk '.\Build\LetItBee-Quest-branch-perch.apk'
```

`-InstallApk` cài cập nhật giữ dữ liệu app; bỏ tham số này nếu chỉ đổi kết nối.
`-UpdateFirewall` đọc IP Wi-Fi của đúng kính và profile mạng hiện tại rồi cập nhật
rule cho **đúng IP PC, đúng IP kính, TCP 8080**. Hỗ trợ Private/Public, không đổi
loại mạng và không tắt firewall. IP kính đổi thì chạy lại. Wi-Fi khách có chế độ
cách ly thiết bị vẫn có thể chặn ở router; khi đó dùng cùng một hotspot cho PC/Quest.
Sau cập nhật vẫn cần SIMPLE mở **LetItBeeVR RUNNING** và thêm player vào experiment.

### Chiếu màn hình Quest trên SIMPLE

Kết nối gameplay TCP 8080 và chiếu màn hình là hai đường riêng. WebPlatform
hiện có chỉ nhận stream từ ADB mạng; chỉ cắm USB hoặc thấy GAMA ready chưa đủ.
Lúc chuẩn bị kính, cắm USB, chấp nhận debugging và chạy:

```powershell
.\Connect-Quest.ps1 -EnableStreaming
```

Lệnh bật ADB mạng từ đúng kính qua USB, nối IP kính và kiểm tra trạng thái
`device`, rồi bạn mở `http://localhost:8000/streamPlayerScreen` bằng Chrome/Edge.
Nếu đã chọn một thiết bị ADB mạng bằng `-Serial IP:PORT`, dùng nguyên cổng đó.
Sau khi bật stream, script ưu tiên một kính USB nếu có cả USB và ADB mạng để
không nhầm hai kết nối của cùng kính thành hai kính. USB dùng chuẩn bị; stream
tiếp tục qua Wi-Fi. Khi khởi động lại kính/đổi mạng có thể cần chuẩn bị lại.

`Start-Simple.ps1` đã được sửa để sao chép toolkit chính thức vào đúng thư mục
runtime và tìm ADB trong PATH/Unity Hub cho backend. Không sửa mã hay giao diện
SIMPLE gốc bên ngoài dự án. Trang HTTP và control socket 8082 đã phản hồi đúng;
bốn file toolkit khớp hash gốc. Lượt kiểm tra trực tiếp tiếp theo ngày 20/10:
Quest ban đầu chỉ có ADB USB nên danh sách stream rỗng. Sau khi chạy
`Connect-Quest.ps1 -EnableStreaming`, ADB mạng kết nối thành công và SIMPLE
nhận stream `10.18.47.62`. Socket video trả về cấu hình H.265 và 23 gói khung hình,
có keyframe. Đã xác minh dữ liệu video thực tế; chưa xác minh hình hiển thị trên
trình duyệt của người dùng hoặc độ ổn định dài hạn. Giữ kính thức và mở Let It Bee,
rồi tải lại trang stream bằng Ctrl+F5 nếu trang vẫn hiện hình chờ.

Nếu trang báo Vite không resolve được `@yume-chan/stream-extra`, nguyên nhân đã
kiểm chứng là cache ngoài thư mục WebPlatform không tìm thấy dependency bị loại
khỏi pre-bundle. Launcher đã thêm alias tới các package cài trong WebPlatform,
giữ cấu hình exclude của SIMPLE gốc. Sau áp sửa và khởi động lại riêng web,
132 module trong cây import của decoder/stream tải HTTP 200, không còn import
`@yume-chan` chưa resolve. Backend/GAMA giữ nguyên; không cần build lại APK.

Chẩn đoán ngày 20/10: PC `10.18.46.146` trên Wi-Fi Public, file cũ trỏ
`192.168.2.8`; rule Windows vẫn giới hạn Private, PC `172.20.10.2` và subnet
`172.20.10.0/28`. Đã sửa profile Editor thành loopback, chạy LetItBeeVR headless,
kiểm chứng 25 kiểm tra client Unity thật qua SIMPLE/GAMA. Profile Quest mới đã
chuẩn bị; chưa gửi vào kính hoặc áp rule mạng mới vì ADB offline và người dùng
chọn tự test. Không coi kiểm tra PC là kết quả kết nối Quest.

Lượt cài tiếp theo 20/10: streamed install thất bại nhưng không có mã lỗi chi tiết;
ADB quan sát được ở trạng thái offline. Đã đổi `Connect-Quest.ps1` sang
`install --no-streaming -r`, giữ stdout/stderr và lưu log trong `Runtime/quest/`.
Sau khi ADB trở lại đã cài thành công (`Success`, exit 0) lên Quest 3
`2G0YC5ZGB904N9` và đọc lại profile trên kính đúng `ws://10.18.46.146:8080`.
Kính dùng Wi-Fi `10.18.47.62`. Rule Windows vẫn là IP/mạng Private cũ; lệnh
cập nhật bị Windows từ chối do phiên công cụ không có quyền Administrator.
Người dùng chạy `Connect-Quest.ps1 -UpdateFirewall` từ PowerShell Administrator;
không cần cài APK lại. SIMPLE đang LetItBeeVR RUNNING; chưa nghiệm thu kết nối Quest.

Client đọc lại file khoảng mỗi giây khi vòng kết nối rảnh, đổi endpoint thì tự
nối lại; gói đang chờ/mất mạng có thể cần thêm timeout. Giao dịch giữ event ID
để tránh nhân đôi. File vắng thì dùng `Simple Endpoint` đã lưu trong scene;
file có nhưng sai JSON/schema/địa chỉ thì báo lỗi. File chỉ nhận hai khóa trên,
không nhận thông số bay/HUD/camera. Hiện chỉ hỗ trợ `ws`, IPv4/hostname, không
có credentials, path, query hoặc fragment trong endpoint.

APK mới có bộ đọc cấu hình ngoài đã build ở
`Experiments/LetItBee-HoneyPilot/Build/LetItBee-SIMPLE.apk`. **Cần cài cập nhật một
lần từ APK cũ**; các lần đổi IP/đường dẫn sau không cần build lại. Gỡ app/xóa app
data có thể mất file cấu hình; gửi lại khi cần. Đã gửi cấu hình qua ADB cho hai
Quest trong đợt 19/10; hiện người dùng không có kính để thử APK phục hồi mới.

### Đường dẫn model, checkpoint và chuyển tiến độ

WebPlatform đọc `settings.json` rồi nạp `./GAMA/LetItBeeSimpleVR.gaml`; APK không
cần biết model nằm ở ổ nào. GAMA quyết định nơi ghi checkpoint, mặc định:

```text
Runtime/checkpoints/<Session Id>.json
```

Muốn đổi nơi lưu, sao chép `GAMA/server.example.json` thành `GAMA/server.local.json`,
sửa rồi dừng/launch lại experiment:

```json
{ "checkpoint_directory": "D:/LetItBeeData/checkpoints" }
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
| Quest cùng hotspot nhưng TCP 8080 timeout | Đối chiếu profile mạng Windows với rule Node/SIMPLE: rule Public không áp dụng cho mạng Private. Nếu cần thêm rule, dùng quyền Administrator, giới hạn TCP 8080 và IP kính/mạng nội bộ; không tắt toàn bộ firewall |
| GAMA ghi `READ ONLY` | Đang xem checkpoint, chưa phải phiên Unity cập nhật liên tục |
| Đổi máy thấy tiến độ mới | Kiểm tra đã chép đúng checkpoint, Session Id và thư mục server chưa |

**Xem checkpoint từ workspace:** import GAMA Project từ thư mục `GAMA` của package,
bỏ chọn **Copy projects into workspace** để liên kết file gốc; mở
`LetItBeeHoneyPilot.gaml` → **HoneyPilot**, chọn `Progress file`. Bản xem đọc khoảng
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
  → LinkToUnity / LetItBeeVR
  → luật giao dịch + checkpoint GAMA
  → snapshot xác nhận về Unity
```

| Phần | Trách nhiệm và đối tượng |
| --- | --- |
| BeeController, BeeSwipeFlap, BeeFlightRhythm | Di chuyển BeeRoot, nhận nhịp quẹt, nâng/lướt/phanh |
| BeeFlightCollision, BeeMapSurfaces, BeeMapSurfaceBuild, BeeSurfaceSkim | Va chạm, collider bổ sung và phản hồi sát bề mặt |
| BeeAltitudeHold, BeeFlapping, BeeCameraRig, BeeHud, BeeKeyboardAssist | Các chức năng giữ độ cao, cánh/âm thanh, rig, HUD, hỗ trợ desktop đã có; xem giá trị hiện hành trong scene |
| BeeHoneyPilot | Tham chiếu BeeRoot/tay phải/hai đầu hoa/ô tổ; gửi input, dựng mật/phấn/nhãn và phản hồi runtime |
| HoneySimpleClient, HoneyRpcRetry, BeeSimpleConnection | Xác thực, gửi `ask`, ghép phản hồi và thử lại có giới hạn; dùng ConnectionManager/WebSocket hiện có |
| BeeConnectionProfile | Đọc endpoint ngoài, loại đường dẫn PC khỏi payload SIMPLE; không nạp cấu hình vào scene |
| LetItBeeSimpleVR.gaml | Kế thừa `abstract_unity_linker`/`abstract_unity_player`, experiment `type: unity`, đăng ký player và chuyển giao dịch |
| LetItBeeHoneyPilot.gaml | Quản lý phấn, nectar, ID mật, cầm/thả, ô tổ, state và biên nhận checkpoint |

Map, hạ tầng XR và ConnectionManager/WebSocket kế thừa dự án SIMPLE. Logic bay
ong, phản hồi bề mặt, vòng mật/phấn và model Let It Bee là phần phát triển thêm.
Unity giữ camera/tracking, input, bay, va chạm, animation, HUD, VFX/SFX/rung tay.
GAMA là nơi xác nhận tài nguyên và giao dịch; không đồng thời cộng/trừ ở hai phía.
Không áp geometry/spawn/giới hạn di chuyển từ SDK lên XR rig có sẵn.

Khi chưa nhận phản hồi, client thử tối đa **3 lần, mỗi lần chờ 4 giây**, giữ nguyên
correlation và event ID. GAMA trả lại biên nhận đã xác nhận, không cấp/trừ mật lần
nữa. Nếu hết thời gian hoặc socket đóng, pilot nối lại, nạp checkpoint và giữ
giao dịch đang chờ với cùng event ID; không tự cộng tiến độ tại Unity. Phản hồi
đã đến lúc Unity tiếp tục chạy được ưu tiên hơn bộ đếm thời gian đã hết.

### Hợp đồng dữ liệu của pilot

- Schema state: `letitbee-honey-pilot-v1`; profile kết nối dùng schema riêng
  `letitbee-connection-v1`.
- 1 đơn vị = 1 mét. Unity `(x,y,z)` → GAMA `(x,z,y)`; không áp thêm hệ số
  `precision` geometry SDK vào payload nghiệp vụ này.
- ID hoa `flower-a`, `flower-b`; cây `plant-a`, `plant-b`; ô `slot-a`.
- Gửi tới `simulation[0].unity_linker[0]`, action `bee_request`, args `id` và
  `envelope_json` gồm `correlation`, `operation`, `payload`.
- Operations: `configure`, `set_pose`, `transact`, `snapshot`.
- `set_pose.nectar_positions` (tùy chọn) là hai điểm mật hiển thị ở hoa A/B;
  không thay điểm tiếp xúc dùng cho thụ phấn. Client cũ bỏ trường này dùng điểm hoa.
- `set_pose.flower_surfaces` và `flower_radii` (tùy chọn) mô tả mặt nhụy và bán
  kính bông, thống nhất với khoảng nhìn khi đậu. GAMA kiểm tra khoảng cách tới
  mặt nhụy trong `contact_radius + radius`, giới hạn radius tối đa 1,5 m. Client
  cũ thiếu hai trường giữ luật khoảng cách tới điểm hoa cũ; không đổi cấu trúc
  checkpoint hay các bán kính người dùng đã lưu.
- Giao dịch `drop`/`deposit` có thể gửi `held_chunk`: tâm mật cách tay tối đa 0,25 m.
  Gắn tổ dùng khoảng cách tâm mật tới ô; thiếu trường thì giữ luật điểm tay cũ.
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

### Tự đồng bộ đối tượng từ Unity — nền đầu tiên 19/10

Đã thêm lớp quan sát `letitbee-world-v1` trên **cùng kết nối SIMPLE**. Unity là
nơi bố trí đối tượng; GAMA tạo/xóa agent quan sát theo danh sách nhận được.
Không thêm một ConnectionManager hay mô phỏng AI ong thứ hai.

**Dùng trong Unity:**

1. Dừng Play, mở scene Let It Bee hiện hành để bố trí và lưu đối tượng.
2. Ong người chơi, hai hoa và ô tổ pilot được gửi tự động, không cần thêm component.
3. Với đối tượng mới, chọn GameObject, vào **Let It Bee → World Sync → Track selected
   as Bee / Flower / Hive**. Thao tác chỉ thêm `BeeWorldEntity` vào đối tượng đã chọn.
4. Trong component, `Anchor` có thể trỏ tới điểm cần theo dõi, ví dụ bone Head của
   hoa. Để trống thì dùng vị trí GameObject. `Species Id` là mã dữ liệu, không tự
   suy tên loài khoa học từ tên mesh. Đối tượng trang trí chưa đánh dấu không gửi.
5. Ctrl+S trong **Edit Mode**, chạy SIMPLE/GAMA theo mục 4 rồi Play. Nhân bản đối
   tượng đã đánh dấu sẽ có mã riêng; thêm, di chuyển, tắt/bật component hoặc
   GameObject và xóa sẽ phản ánh ở GAMA. Thay đổi trong Play chỉ dùng thử, theo
   cơ chế lưu bình thường của Unity; muốn giữ phải bố trí và lưu ở Edit Mode.

GAMA có monitor **World sync** với tổng số, số ong/hoa/tổ và danh sách mã/vị trí.
Map trong **experiment đang được SIMPLE chạy** hiển thị các agent này. Một cửa sổ
workspace đọc checkpoint độc lập chỉ có tiến độ đã lưu, chưa nhận danh sách quan
sát runtime. Headless có map phía server; không tự mở cửa sổ GAMA GUI khác.

**Phạm vi hiện tại:** đối tượng mới được quan sát trên bản đồ, chưa tự có AI,
thụ phấn, lấy mật hoặc ô tổ gameplay mới. Tương tác vẫn là hai hoa, một cục và
một ô của pilot. Chưa triển khai sông, độ mặn hoặc mô hình sinh thái màn 2.
Di chuyển reference pilot trong Play đã có cập nhật pose; thay bố trí reference
pilot đã lưu rồi dùng checkpoint khác cấu hình vẫn chịu kiểm tra
`session_or_geometry_mismatch`, chưa tự chuyển tiến độ sang cấu hình khác.

Mỗi đối tượng đánh dấu có ID; bộ đồng bộ gửi snapshot toàn bộ tối đa bốn lần/giây
khi có thay đổi, tối đa 256 đối tượng kể cả bốn mốc pilot ở bản đầu. Đây là giới hạn
bản thử, chưa phải kết quả benchmark số lượng trên Quest. Nhận lại đúng source và
sequence mới xác nhận; dữ liệu cũ/lỗi/ID trùng bị từ chối nguyên bản cập nhật.
Nối lại gửi danh sách đầy đủ để xóa agent dư và khôi phục agent thiếu. Danh sách
quan sát không cộng mật, không thụ phấn và không thay thế checkpoint gameplay.

Map đã căn thêm theo vị trí ong/các agent mới để không rơi ngoài khung, với Bắc
ở trên tương ứng +Z Unity. Phép đổi dữ liệu giao dịch `(x,y,z) → (x,z,y)` giữ nguyên.

- Kiểm tra Unity cô lập: **Let It Bee → World Sync → Check world sync**.
- Kiểm tra GAMA thật: `python Experiments/LetItBee-HoneyPilot/validate_world_sync.py`
  khi headless cổng 6875 đang chạy; dùng phiên/checkpoint kiểm tra riêng.
- [Ảnh map GAMA thật với hai ong, ba hoa và một tổ](Previews/WorldSync-2026-10-19.png).

### Tệp cần tìm khi phát triển

| Đường dẫn từ gốc project | Nội dung |
| --- | --- |
| `Assets/Scripts/VU2/Bee/` | Bay, camera, HUD, âm thanh và script nền ong |
| `Assets/Scripts/VU2/HoneyPilot/` | Pilot, client direct/SIMPLE, cấu hình kết nối |
| `Assets/Editor/VU2/` | Inspector, công cụ setup/build/check; không tự chạy setup lên scene đã chỉnh |
| `Assets/Prefabs/LetItBee/HoneyPilot/` | Mesh/material mật, idle clip/controller riêng của pilot |
| `Experiments/LetItBee-HoneyPilot/GAMA/` | Model hiện tại và cấu hình server mẫu |
| `Experiments/LetItBee-HoneyPilot/settings.json` | Learning package **Let It Bee - Honey Pilot**, experiment **LetItBeeVR** |

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
trước static batching để bake collision vào APK. Trong SCN_LetItBee, chuyển động
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

- [x] Nền tự đồng bộ đối tượng Unity → SIMPLE → GAMA: ong/hoa/tổ được đánh dấu,
  thêm/xóa/di chuyển và phục hồi quan sát sau nối lại; chưa mở rộng luật gameplay.
- [ ] Mở rộng luật gameplay theo danh sách ID, thay các reference cố định của pilot;
  thiết kế chuyển bố trí/phiên mà giữ tiến độ đúng, trước khi nối mô hình màn 2.

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
  Lượt khảo sát trước chưa có PlayableDirector cho Let It Bee; cần khảo sát lại
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
| Khởi động Quest 19/10 | APK `LetItBee-Quest-startup-fix.apk`: build 0 lỗi, 509 cảnh báo; cài cập nhật thành công, người dùng xác nhận đã thấy cảnh game | Đã thấy ảnh stereo, HUD và hai tay cầm; chưa nghiệm thu trọn vòng hoa/mật/tổ |
| APK đậu hoa 19/10 | `LetItBee-Quest-flower-landing.apk` build Succeeded và đã cài cập nhật; metadata có mã đậu hoa mới | BuildReport ghi 497 cảnh báo và 2 log lỗi ghi meta `PerformanceTestRunInfo.json.meta` ở bước prebuild; vẫn tạo/cài APK thành công. Chưa nghiệm thu tương tác Quest |
| Đậu hoa 19/10 | 20 kiểm tra qua MCP trong scene thử riêng; chạy lại 55 kiểm tra Flight V2 đều qua | Có XR origin thấp hơn đầu, cùng điểm tương tác ở local/GAMA, khoảng hở mặt đất, giữ trigger trong lúc đáp, không ghi head pose hoặc đổi tuning; chưa nghiệm thu cảm giác Quest |
| Đậu cành 20/10 | 26 kiểm tra qua MCP trên bản sửa cuối; 25 kiểm tra client Unity thật qua SIMPLE/GAMA đều qua | Dùng đầu lá thật, khoảng nhìn ngang và độ cao trên cành; anchor thủ công, cành chuyển động, đứng cao cạnh hoa thấp, giới hạn tầm lấy mật, không ghi tracking hoặc tuning. Chưa nghiệm thu trên Quest |
| Rà soát thao tác/stream 20/10 | 23 kiểm tra grip độc lập; Runtime và Editor compile bằng Roslyn với response file của Unity đều exit 0; 32 kiểm tra cũ + 8 kiểm tra mới qua GAMA trực tiếp đạt, qua SIMPLE chạy liên tiếp hai lượt đều qua | Cầm/thả qua độ trễ, tâm mật trong ô khi tay ngoài ô, từ chối tâm giả, điểm mật động trùng hình ảnh; đã sửa timer sót khi remove/re-add player và chạy kiểm tra Node. HTTP stream 200, control socket 8082 và hash toolkit đạt. Người dùng không có kính; Unity đang đóng nên chưa chạy Play qua MCP, chưa build/cài APK mới và chưa thử hình ảnh thực tế |
| Stream Quest trực tiếp 20/10 | Bật ADB mạng từ Quest USB thành công; SIMPLE liệt kê stream thật và trả cấu hình H.265 cùng 23 gói khung hình, có keyframe | Đã xác minh đường truyền video. Chưa xác nhận hình trên trình duyệt hoặc độ ổn định dài hạn; kiểm tra này không build/cài mã gameplay mới hay đổi scene Unity |
| Tương tác V3 20/10 | 17 kiểm tra vòng thao tác qua MCP, 23 grip, 26 đậu cành, 31 trình bày; 44 trường hợp GAMA trực tiếp và qua SIMPLE đạt. Render asset thật thấy trọn bông, mật và nhãn gọn | Có kiểm tra chuyển mục tiêu khi vùng hai hoa chồng nhau. Lượt SIMPLE đầu timeout; harness đã đợi ACK xóa player thay sleep cố định và hỗ trợ retry giữ nguyên correlation/event ID như client Unity. Lượt chạy lại đủ 44 trường hợp không dùng retry. Các kiểm tra dùng phiên/scene riêng; chưa thay nghiệm thu thao tác bằng tay trên Quest |
| APK tương tác V3 20/10 | Bản cuối Succeeded, 0 lỗi, 144 cảnh báo; 114.075.982 byte, ARM64, ZIP hợp lệ; thư viện native trong APK khớp bản build cuối. Cài non-streamed thành công lúc 14:25, log app có V3 và GAMA ready lúc 14:26 | Đã xác thực kết nối Quest → SIMPLE → GAMA, Player_62 in-game trên experiment mới. 36 file scene/cấu hình/checkpoint khớp baseline sau build đầu; giữ checkpoint có mật ở B để người dùng cầm tiếp. Chưa xác nhận thao tác vật lý trọn vòng |
| APK đậu cành 20/10 | `LetItBee-Quest-branch-perch.apk`: Succeeded, 0 lỗi, 144 cảnh báo; 114.076.047 byte, ARM64, ZIP hợp lệ. Đã cài thành công qua non-streamed install lúc 10:52 | Có mã đậu cành; scene, ProjectSettings, material hoa/mật/bầu trời và checkpoint khớp hash lúc bắt đầu. Đã gửi/đọc lại IP trên Quest; còn cập nhật firewall bằng quyền Administrator và người dùng thử thực tế |
| Server SIMPLE 19/10 | Chạy lại 32 kiểm tra qua WebPlatform → LinkToUnity → GAMA, checkpoint người chơi giữ nguyên | Dùng player/phiên kiểm tra riêng; không thay thế kiểm tra client trên kính |
| Client Quest 19/10 | Đã xác thực Player_4, nhận GAMA ready với phiên cũ; trace có hơn 240 yêu cầu/phản hồi vị trí/snapshot liên tục | Sau bổ sung dọn timer heartbeat vẫn từng gặp một timeout snapshot rồi tự nối lại; chưa nghiệm thu dài hạn hoặc đủ vòng giao dịch bằng tay cầm |
| Heartbeat nối lại | Kiểm tra adapter qua Node đã qua: hủy timer cũ qua nhiều lần thay socket, giữ timer hiện tại/player khác và thông báo gốc | Bộ kiểm tra timer riêng; không cộng vào 32 kiểm tra giao dịch GAMA |
| Phục hồi phản hồi 19/10 | 6 kiểm tra C# phục hồi và 13 kiểm tra client Unity thật qua SIMPLE/LinkToUnity/GAMA headless đã qua | Proxy bỏ phản hồi configure và collect đã được chấp nhận, trì hoãn set_pose 4,5 giây; client thử lại cùng correlation, mật không nhân đôi; nối lại vẫn giữ 1 ô tổ |
| Trình bày hoa/mật/tổ 19/10 | 31 kiểm tra qua MCP; chạy lại 20 đậu hoa, 55 Flight V2 và 6 RPC đều qua | Render asset thật; kiểm tra chờ/nhả trigger, ACK/replay, nhấc/kéo/đứt sợi mật, giới hạn độ trễ theo tay, khớp ô tổ động, same_visit sau nối lại, state GAMA và camera; scene/ProjectSettings/material mật/font cache khớp hash trước sửa. Chưa kiểm tra Quest |
| Tự đồng bộ đối tượng 19/10 | 18 kiểm tra Unity cô lập, 25 kiểm tra client Unity thật qua SIMPLE và 28 kiểm tra GAMA thật đều qua; 32 kiểm tra giao dịch cũ cũng qua | Thêm/sao chép/di chuyển/tắt/bật/xóa, replay, ACK bị bỏ qua, nối lại, đổi stream, dữ liệu lỗi và trạng thái mật; có render map nguồn hiện hành. Chưa benchmark quần thể trên Quest; chưa mở rộng gameplay sang các hoa mới |
| APK phục hồi 19/10 lúc 19:24 | `LetItBee-Quest-connection-recovery.apk`: Succeeded, 0 lỗi, 497 cảnh báo; 114.072.198 byte | ARM64, INTERNET; metadata có mã retry và điểm tương tác camera; ZIP hợp lệ. Chưa cài/chạy trên kính vì người dùng hiện không có Quest |
| Bảo toàn giá trị người dùng | Lượt SIMPLE đối chiếu 1.526 component; lượt cấu hình đối chiếu 21 scene/prefab không đổi | Bằng chứng của các lượt đó, không phải lý do bỏ kiểm tra file mới ở lần sửa sau |

Menu Editor: **Let It Bee → Check Flight V2 / Check Surface Flight / Check Wing Audio /
Check Flower Landing / Check Honey Grip / Check RPC Recovery**,
chạy khi dừng Play; các bộ dùng môi trường thử riêng. Trong kiểm tra PC từng gặp
Oculus XR không khởi tạo vì không có phiên kính; không lấy đó làm lỗi GAMA hay
bằng chứng tracking trên kính đã hoạt động.

Trong thư mục pilot, `python validate_simple.py` chạy qua WebPlatform/LinkToUnity
thật, dùng checkpoint/player riêng. **Chỉ chạy khi Unity dừng**; script reset
experiment, không chạy giữa lúc chơi. `validate_gama.py` dành cho direct legacy.

`python validate_interactions.py` chạy 32 kiểm tra cũ và 12 kiểm tra điểm cầm/thả,
thể tích bông và giới hạn khoảng cách
mới qua SIMPLE, cùng điều kiện không có người chơi đang dùng server. Kết quả ở
`Runtime/interaction-validation.json`; thêm `--direct` để thử server GAMA 6875.
Đợt rà soát phát hiện thêm orphan heartbeat khi remove/re-add player cùng IP;
adapter trong dự án nay hủy timer trước khi gọi hàm xóa gốc. Không đổi giao thức
SIMPLE. Có kiểm tra Node cho reconnect, xóa bằng ID/IP và thêm lại player.

Menu **Let It Bee → Check Flower Honey Interaction** chạy 17 kiểm tra V3 trong
preview scene riêng: ý định tiếp xúc, chờ ACK, thả sớm, nhặt không giữ cò trái,
tâm mật khi gắn, lý do từ chối và kích thước chữ. Không vào Play hay ghi scene.

Menu **Let It Bee → Check SIMPLE without headset** chạy `BeeSimpleChecks` với cấu
hình XR clone trong bộ nhớ, tránh khởi động Quest Link; sau kiểm tra tự dừng Play
và khôi phục cấu hình tạm. Cần SIMPLE/GAMA đang chạy và thêm player kiểm tra vào
experiment. Dùng khi không có người chơi đang sử dụng server. Không ghi cấu hình
XR, scene hay PlayerSettings. `BeePortableChecks.Run()` kiểm tra cấu hình ngoài.

Kết quả máy đọc hiện nằm ở:

- `Experiments/LetItBee-HoneyPilot/Runtime/simple-validation.json`.
- `Library/HoneyPilot/unity-simple-validation.json`, `portable-validation.json`,
  `portable-relocation-validation.json`, `portable-live-validation.json`.
- `Library/HoneyPilot/flower-landing-validation.json`, `rpc-retry-validation.json`;
  trace thử mất/chậm phản hồi: `Library/HoneyPilot/QuestBlackScreen/rpc-fault-proxy.jsonl`.
- `Library/HoneyPilot/simple-apk-build.json`; `size` trong BuildReport không phải
  kích thước file APK trên ổ đĩa. Dùng kích thước file thật khi ghi dung lượng.

Lỗi màn hình đen sau logo: kết nối WebSocket đồng bộ trong `ConnectionManager.Start`
có thể chặn luồng chính khi endpoint không tới được. `BeeSimpleConnection` hiện
mở kết nối bất đồng bộ và để pilot quản lý timeout/retry. Địa chỉ Quest được cập
nhật bằng file cấu hình ngoài APK, không ghi lại endpoint trong scene.

Khi thử APK đậu hoa, PC đã đổi Wi-Fi sang IP `172.20.10.2` trong khi Quest còn
`10.18.46.149`; log kính báo **No route to host**. Đã cập nhật endpoint ngoài APK
theo IP PC mới. Hai thiết bị cần cùng mạng có thể truy cập nhau; mỗi lần đổi mạng
phải kiểm tra lại IP PC và file kết nối, không cần build lại. Chưa coi nhãn
“chưa nối SIMPLE/GAMA” trong lần thử khác mạng này là lỗi luật tương tác hoa.

Lần kiểm tra tiếp theo dùng một Quest khác, IP `172.20.10.4`: máy này chưa có file
kết nối và rule trước đó chỉ cho kính `.3`. Đã gửi cấu hình riêng cho kính mới;
người dùng cập nhật rule qua `Set-BeeFirewall.ps1` cho subnet `172.20.10.0/28`.
Đã nhận **HTTP 101 Switching Protocols** từ cổng SIMPLE 8080 ngay trên kính mới.
Tiếp đó đã nhận **GAMA ready** trong app và dữ liệu vị trí/snapshot liên tục.
Đây chưa thay thế xác nhận giao dịch hoa/mật/tổ bằng tay cầm. APK người dùng cài lúc 16:13 đã có mã đậu hoa;
không cài đè APK này trong lần chẩn đoán tiếp theo.

Lượt kiểm tra tại nhà dùng GAMA **headless** và client Unity trong Editor, không
phải hình ảnh chạy trên GAMA GUI hoặc nghiệm thu Quest. Đã dừng experiment kiểm
tra và proxy gây mất phản hồi sau khi hoàn tất. Scene và ProjectSettings sau
build khớp hash bản đã đọc trước kiểm tra; đã loại đúng danh sách preload XR
tạm do build thêm, không áp preset bay/HUD. Unity có cập nhật cache glyph của
font TMP trong Play/build; cache này không phải thông số bố cục HUD.

**Chưa xác minh đầy đủ:** vòng tương tác, âm/rung/độ thoải mái và độ ổn định LAN dài hạn
trên kính, PC thứ hai, server ban tổ chức, multiplayer và mô phỏng sinh thái có
dữ liệu hiệu chuẩn. Log Quest còn có lỗi OpenGL native plugin và mesh Polybrush
không readable; chưa xử lý trong đợt này. Bảng trên không thay thế nghiệm thu thiết bị.

Lab riêng `Experiments/LetItBee-GAMA` dùng direct WebSocket, hình khối desktop,
10 hoa/5 cục và `stress` minh họa. Từng qua 31 kiểm tra Unity standalone, nhưng
reconnect tạo phiên mới; không dùng giao thức/luật pollination của lab thay pilot
chính. Giữ README tại lab, đã gộp kết quả kiểm chứng để tái hiện lịch sử, không làm
nguồn tiến độ Let It Bee hiện tại.

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

Tài liệu này thay chín file `LetItBee-*` trước đây: TODO, Progress, Game Design
Draft, GAMA Display Proposal, SIMPLE, Connection Setup, FlightV2, SurfaceFlight,
Audio. Nội dung còn dùng đã được đưa vào các mục trên; bỏ thông tin trùng và các
khẳng định lỗi thời như “chưa có GAMA”, “chưa có hoa/tổ/mật”, “phải build lại để
đổi IP”. README ở package chỉ dẫn về đây để không có hai hướng dẫn vận hành.

Tài liệu `Report/`, `Docs/TreeBarrier_Mechanic.md` và
`Assets/Scripts/VU2/Documentation/` là nền kế thừa/các hệ thống khác, được giữ
nguyên; không dùng các cơ chế thu hoạch/công cụ/điểm mặn trong đó làm thiết kế
Let It Bee đã duyệt. Lab thử nghiệm cũ có
[README và kết quả lịch sử](../Experiments/LetItBee-GAMA/README.md).

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
