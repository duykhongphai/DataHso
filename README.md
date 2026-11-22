# DataHso

Bộ lấy dữ liệu tự động từ máy chủ Hiệp Sĩ Online, viết lại từ giao thức của client Unity
`hso_v403` (phiên bản game `4.0.3`). Chương trình chạy headless, không cần mở Unity hay điều
khiển cửa sổ game.

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

Các file JSON nằm trong `TeaMobi/<thư-mục-server>/`. Ảnh được chia theo nghìn, ví dụ
`Icons/6/6025.png`.

## Cấu hình GitHub Actions

Tạo repository secret:

- `HSO_TAIKHOAN`: `tài_khoản|mật_khẩu`

Các repository variables tùy chọn:

- `HSO_MAY_CHU`: tên server; mặc định `Chiến Thần`. Client tự đọc danh sách sống từ endpoint
  chính thức nên không cần điền IP.
- `HSO_THUMUC`: thư mục lưu; mặc định `ChienThan`.
- `HSO_NHAN_VAT`: **index 0-based** trong màn chọn nhân vật; `0` là nhân vật đầu tiên,
  `1` là nhân vật thứ hai, `2` là nhân vật thứ ba.
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
