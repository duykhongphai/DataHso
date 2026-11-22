# DataHso

Bộ lấy dữ liệu tự động từ máy chủ Hiệp Sĩ Online, viết lại từ giao thức của client Unity
`hso_v403` (phiên bản game `4.0.3`). Chương trình chạy headless, không cần mở Unity hay điều
khiển cửa sổ game.

Trang tra cứu: **https://duykhongphai.github.io/DataHso/**

Giao diện web đọc trực tiếp kho `TeaMobi/DuLieu`, hỗ trợ tìm kiếm, sắp xếp, phân trang, xem
chi tiết, chép ID/JSON và mở JSON gốc trên cả máy tính lẫn điện thoại. Khi lượt lấy dữ liệu đầu
tiên chưa hoàn tất, trang hiển thị trạng thái chờ và đường dẫn tới GitHub Actions thay vì báo lỗi
trống.

## Dữ liệu hiện lấy được

- Bảng potion, nguyên liệu, thuộc tính/option vật phẩm.
- Mẫu trang bị được hỏi theo ID.
- Bảng mẫu quái.
- Danh sách nhân vật của tài khoản và chọn nhân vật theo index.
- Bộ kỹ năng của nhân vật đã chọn, gồm đầy đủ các cấp kỹ năng.
- Tên vị trí, tên vật phẩm nhiệm vụ và cấu hình đập đồ.
- Map nhân vật đang đứng: tile, bộ tile, kích thước, khu và các lối ra.
- NPC và trạng thái nhiệm vụ server gửi ở map hiện tại.
- Ảnh icon được các bảng trên tham chiếu.
- Gói gốc quan trọng trong `RawPackets/` để tiếp tục giải mã các phần chưa hỗ trợ.

Các file JSON nằm trong kho dùng chung `TeaMobi/DuLieu/`. Dữ liệu game giống nhau giữa các
server nên workflow đăng nhập tuần tự nhiều nhân vật nhưng vẫn hợp nhất vào cùng một thư mục.
Ảnh được chia theo nghìn, ví dụ
`Icons/6/6025.png`.

## Cấu hình GitHub Actions

Tạo repository secret:

- `HSO_TAIKHOAN`: `tài_khoản|mật_khẩu`

Workflow hiện chạy tuần tự các nguồn sau bằng cùng tài khoản trong secret:

- Nhân Mã (`hs5`), slot 0 (Độc).
- Nhân Mã (`hs5`), slot 1 (Lửa).
- Kì Lân (`hs6`), slot 0 (Băng).
- Thiên Hà (`hs7`), slot 0 (Điện).

Mỗi lượt được ngắt kết nối hoàn toàn trước khi chuyển sang slot hoặc server tiếp theo. Các
repository variables tùy chọn:

- `HSO_THUMUC`: thư mục kho dùng chung; mặc định `DuLieu`.
- `HSO_ITEM_TOI_DA`: ID trang bị lớn nhất cần quét; mặc định `4000`.

Workflow chạy hằng ngày lúc khoảng 06:15 giờ Việt Nam và cũng có thể chạy tay trong tab
Actions.

## Chạy ở máy cá nhân

PowerShell:

```powershell
$env:HSO_TK = "tai_khoan"
$env:HSO_MK = "mat_khau"
$env:HSO_MAY_CHU = "Chiến Thần"
$env:HSO_NHAN_VAT = "0"
$env:HSO_RA = "."
dotnet run --project HeadlessClient/DataHso.HeadlessClient/DataHso.HeadlessClient.csproj -c Release
```

Hoặc dùng tham số:

```text
--maychu "Chiến Thần" --tk ... --mk ... --nhan-vat 0 --ra .
```

Không ghi tài khoản hoặc mật khẩu trực tiếp vào repository.

## Ghi chú giao thức

Client bắt tay bằng command `-40`. Server trả key XOR; từ đó command, độ dài và payload đều
được mã hóa bằng hai con trỏ key độc lập cho chiều gửi/nhận. Login là command `1`; danh sách
nhân vật và lệnh chọn nhân vật cùng dùng command `13`. Chọn nhân vật gửi `byte 0` rồi
`int characterId`, đúng với client gốc.
