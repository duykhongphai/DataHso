namespace DataHso;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        var c = CauHinh.Doc(args);
        if (c.Loi() is { } loi)
        {
            Console.Error.WriteLine(loi);
            return 2;
        }

        using var het = new CancellationTokenSource(c.ChoDuLieuMs);
        try
        {
            var mayChu = await TimMayChuAsync(c, het.Token);
            Console.WriteLine($"DataHso: {mayChu.Ten} ({mayChu.Host}:{mayChu.Port}), nhân vật index {c.NhanVat}");
            Console.WriteLine($"Thư mục ra: {Path.GetFullPath(c.ThuMucRa)}");

            var data = new DuLieuHso();
            BoXuat.NapCu(data, c.ThuMucRa);
            var doc = new BoDocGoi(data, c.ThuMucRa);

            await using var phien = new PhienHso();
            phien.Nhan += doc.NhanAsync;
            await phien.NoiAsync(mayChu.Host, mayChu.Port, het.Token);
            await phien.DaBatTay.Task.WaitAsync(TimeSpan.FromSeconds(30), het.Token);

            // Lần đầu xin các bảng nền nếu máy chưa có cache client. Server HSO dừng ở đây sau
            // khi trả command 25/26; client gốc đổi indexInfoLogin về 0 rồi người chơi đăng nhập
            // lại. Headless client tự làm luôn bước đăng nhập thứ hai bên dưới.
            await GuiDangNhapAsync(phien, c, 1, het.Token);
            await phien.GuiAsync(61, [], het.Token); // tên vị trí, vật phẩm nhiệm vụ, bảng đập đồ

            var bangNen = Task.WhenAll(doc.CoVatPham.Task, doc.CoQuai.Task);
            var dauTien = await Task.WhenAny(bangNen, doc.DangNhapHong.Task, Task.Delay(90_000, het.Token));
            if (dauTien == doc.DangNhapHong.Task)
                throw new InvalidOperationException(data.LoiDangNhap ?? "Đăng nhập thất bại.");
            if (!bangNen.IsCompleted)
                throw new TimeoutException("Server chưa trả các bảng vật phẩm/quái trong 90 giây.");

            if (!doc.CoNhanVat.Task.IsCompleted)
            {
                Console.WriteLine("Đã nhận bảng nền; đăng nhập tiếp với cache hiện hành để lấy danh sách nhân vật...");
                BoXuat.Ghi(data, c.ThuMucRa, mayChu, c.NhanVat);
                await GuiDangNhapAsync(phien, c, 0, het.Token);
                dauTien = await Task.WhenAny(doc.CoNhanVat.Task, doc.DangNhapHong.Task, Task.Delay(90_000, het.Token));
                if (dauTien == doc.DangNhapHong.Task)
                    throw new InvalidOperationException(data.LoiDangNhap ?? "Đăng nhập thất bại.");
            }
            if (!doc.CoNhanVat.Task.IsCompleted)
                throw new TimeoutException("Server chưa trả danh sách nhân vật trong 90 giây.");

            BoXuat.Ghi(data, c.ThuMucRa, mayChu, c.NhanVat);
            NhanVat? nhanVat;
            lock (data.Khoa)
                nhanVat = c.NhanVat < data.NhanVat.Count ? data.NhanVat[c.NhanVat] : null;

            if (nhanVat == null)
            {
                Console.WriteLine(data.NhanVat.Count == 0
                    ? "Tài khoản chưa có nhân vật; đã giữ lại các bảng nhận được trước màn chọn nhân vật."
                    : $"Không có nhân vật index {c.NhanVat}; tài khoản chỉ có {data.NhanVat.Count} nhân vật.");
                return data.NhanVat.Count == 0 ? 0 : 3;
            }

            Console.WriteLine($"Chọn [{c.NhanVat}] {nhanVat.Ten} (id {nhanVat.Id}, cấp {nhanVat.Cap}, lớp {nhanVat.Lop})");
            await phien.GuiAsync(13, new BoGhi().Byte(0).Int(nhanVat.Id).Xong(), het.Token);
            await Task.WhenAny(doc.DaVaoMap.Task, Task.Delay(90_000, het.Token));
            BoXuat.Ghi(data, c.ThuMucRa, mayChu, c.NhanVat);

            if (c.ItemToiDa >= 0)
            {
                Console.WriteLine($"Quét mẫu trang bị 0..{c.ItemToiDa} (đã có {data.TrangBi.Count})...");
                for (var id = 0; id <= c.ItemToiDa && !het.IsCancellationRequested; id++)
                {
                    lock (data.Khoa)
                        if (data.TrangBi.ContainsKey((short)id)) continue;
                    await phien.GuiAsync(28, new BoGhi().Short(id).Xong(), het.Token);
                    await Task.Delay(c.NhipItemMs, het.Token);
                }
                await Task.Delay(8_000, het.Token);
                Console.WriteLine($"  mẫu trang bị: {data.TrangBi.Count}");
                BoXuat.Ghi(data, c.ThuMucRa, mayChu, c.NhanVat);
            }

            if (c.TaiAnh)
            {
                var anh = GomAnh(data);
                var canHoi = anh.Where(id => !DaCoAnh(c.ThuMucRa, id)).ToList();
                Console.WriteLine($"Tải ảnh: {anh.Count} id tham chiếu, đã có {anh.Count - canHoi.Count}, cần hỏi {canHoi.Count}");
                foreach (var id in canHoi)
                {
                    await phien.GuiAsync(-51, new BoGhi().Short(id).Xong(), het.Token);
                    await Task.Delay(c.NhipAnhMs, het.Token);
                }
                await Task.Delay(12_000, het.Token);
            }

            BoXuat.Ghi(data, c.ThuMucRa, mayChu, c.NhanVat);
            Console.WriteLine("Hoàn tất.");
            return 0;
        }
        catch (OperationCanceledException)
        {
            Console.Error.WriteLine("Hết thời gian lấy dữ liệu.");
            return 4;
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e.Message);
            return 1;
        }
    }

    private static async Task<MayChu> TimMayChuAsync(CauHinh c, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(c.Host)) return new MayChu(c.MayChu, c.Host, c.Port, 0);
        var ds = await MayChu.LayDanhSachAsync(ct);
        return ds.FirstOrDefault(x => string.Equals(x.Ten, c.MayChu, StringComparison.OrdinalIgnoreCase))
               ?? ds.FirstOrDefault(x => x.Ten.Contains(c.MayChu, StringComparison.OrdinalIgnoreCase))
               ?? throw new InvalidOperationException($"Không tìm thấy máy chủ '{c.MayChu}'. Có: {string.Join(", ", ds.Select(x => x.Ten))}");
    }

    private static Task GuiDangNhapAsync(PhienHso phien, CauHinh c, sbyte indexInfoLogin, CancellationToken ct)
    {
        var goi = new BoGhi()
            .Utf(c.TaiKhoan).Utf(c.MatKhau).Utf(c.PhienBan)
            .Utf("0").Utf("0").Utf("0")
            .Byte(4)       // zoomLevel: lấy ảnh mức cao nhất của client PC
            .Byte(4)       // TemMidlet.DIVICE của bản Windows
            .Int(-1)       // chưa chọn characterId
            .Byte(-1)      // area
            .Byte(1)       // Main.isPC
            .Byte(-1)      // IndexRes
            .Byte(indexInfoLogin)
            .Byte(0)
            .Short(-1)     // IndexCharPar, server sẽ trả bản hiện hành
            .Utf("")
            .Xong();
        return phien.GuiAsync(1, goi, ct);
    }

    private static SortedSet<short> GomAnh(DuLieuHso d)
    {
        var ids = new SortedSet<short>();
        lock (d.Khoa)
        {
            foreach (var x in d.BangDoMap.Values) Them(x.Anh);
            foreach (var x in d.Quai.Values) Them(x.Id + 1000);
            foreach (var x in d.TrangBi.Values) Them(x.IconId + 2000);
            foreach (var x in d.Npc.Values) Them((byte)x.Anh + 3000);
            foreach (var x in d.Potion.Values) Them(x.IconId + 4000);
            foreach (var x in d.NguyenLieu.Values) Them(x.IconId + 5500);
            foreach (var x in d.KyNang.Values) Them((byte)x.IconId + 6000);
        }
        return ids;

        void Them(int id)
        {
            if (id is >= 0 and <= short.MaxValue) ids.Add((short)id);
        }
    }

    private static bool DaCoAnh(string thuMuc, short id)
    {
        var dir = Path.Combine(thuMuc, "Icons", (id / 1000).ToString());
        return Directory.Exists(dir) && Directory.EnumerateFiles(dir, id + ".*").Any();
    }
}
