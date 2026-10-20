# Sơ đồ Let It Bee cho GDD

Các sơ đồ là SVG độc lập, đã đo nhãn và kiểm tra XML. Bản PNG để dàn trang được render bằng librsvg qua Sharp.

Bảng màu: #17272F, #FFFFFF, #176B53, #5E6B70, #996515.

Nhãn trên sơ đồ thời gian được rút gọn bằng cách bỏ chủ ngữ lặp; ngày là mốc Git, không phải lịch giao hàng.

## Vòng chơi thành công của pilot Let It Bee

File: letitbee-pilot-process-flow.svg. 9 phần tử; 1200 x 684.

Alt text: Người chơi ghé hoa A, nhận phấn, lấy và cầm mật, ghé hoa B để chuyển phấn, đưa mật về ô tổ và nhận xác nhận 1 trên 1 từ GAMA.

1. Người chơi bay đến hoa A → Đủ gần hoa A
2. GAMA cấp phấn cây A → Phấn cây A
3. Người chơi giữ cò phải → Yêu cầu lấy mật
4. GAMA xác nhận lấy mật → ID cục mật
5. Người chơi cầm cục mật → Mật đang mang
6. Người chơi ghé hoa B → Vị trí hoa B
7. GAMA ghi nhận chuyển phấn → Hoa B nhận phấn
8. Người chơi đưa mật về tổ → Yêu cầu gắn ô
9. GAMA chốt ô tổ 1/1 → Ô tổ đã nhận 1/1

## Luồng xác nhận giao dịch Unity SIMPLE GAMA

File: letitbee-transaction-swimlane.svg. 7 phần tử; 1200 x 524.

Alt text: Unity gửi yêu cầu qua SIMPLE tới GAMA. GAMA từ chối thao tác không hợp lệ hoặc ghi state và biên nhận trước khi trả snapshot để Unity hiển thị kết quả.

1. Unity đọc input và tư thế → pose và event_id.
2. SIMPLE chuyển yêu cầu → bee_request đến GAMA.
3. GAMA kiểm tra hợp lệ → đạt hoặc không đạt.
4. GAMA từ chối khi không đạt → accepted=false và lý do.
5. GAMA lưu state và biên nhận khi đạt → ACK và snapshot.
6. SIMPLE chuyển phản hồi → correlation và kết quả xác nhận.
7. Unity hiển thị kết quả → người chơi nhận phản hồi.

## Các mốc phát triển theo Git

File: letitbee-development-timeline.svg. 4 phần tử; 1200 x 232.

Alt text: Bốn mốc trong lịch sử mã nguồn: nhập nền SIMPLE ngày 30 tháng 9, chỉnh bay ngày 6 tháng 10, sửa rig và hai ray ngày 14 tháng 10, tích hợp pilot GAMA ngày 18 tháng 10 năm 2026.

1. Người phát triển nhập dự án nền SIMPLE tại mốc 30/09 → map và XR
2. Người phát triển chỉnh điều khiển bay tại mốc 06/10 → bay cơ bản
3. Người phát triển sửa rig và hai ray VR tại mốc 14/10 → rig VR
4. Người phát triển nối pilot với GAMA tại mốc 18/10 → pilot đồng bộ GAMA
