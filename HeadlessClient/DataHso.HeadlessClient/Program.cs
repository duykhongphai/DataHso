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

            // Mỗi lần thử dùng một kết nối mới. Với device > 0, server có thể chỉ trả bảng nền rồi
            // chuyển sang luồng cập nhật dữ liệu (-54/-57) mà không gửi danh sách nhân vật, nên
            // lần cuối dùng đúng cấu hình đăng nhập của ToolHSO (zoom 1, device 0).
            (sbyte Zoom, sbyte ThietBi, sbyte InfoLogin, string MoTa)[] cachThu =
            [
                (4, 4, 1, "client PC, xin bảng nền"),
                (4, 4, 0, "client PC, dùng cache bảng nền"),
                (1, 0, 1, "zoom 1, device 0")
            ];

            PhienHso? phien = null;
            BoDocGoi? doc = null;
            for (var i = 0; i < cachThu.Length && phien == null; i++)
            {
                var (zoom, thietBi, infoLogin, moTa) = cachThu[i];
                if (i > 0) await Task.Delay(5_000, het.Token); // để server nhả phiên cũ của tài khoản
                Console.WriteLine($"Đăng nhập lần {i + 1}/{cachThu.Length} ({moTa})...");

                var thuDoc = new BoDocGoi(data, c.ThuMucRa);
                var thuPhien = new PhienHso();
                thuPhien.Nhan += thuDoc.NhanAsync;
                string? lyDo;
                try
                {
                    lyDo = await ThuDangNhapAsync(thuPhien, thuDoc, mayChu, c, zoom, thietBi, infoLogin, het.Token);
                }
                catch (Exception e) when (e is not OperationCanceledException)
                {
                    lyDo = e.Message;
                }

                if (lyDo == null)
                {
                    phien = thuPhien;
                    doc = thuDoc;
                    break;
                }

                Console.WriteLine($"  không lấy được danh sách nhân vật: {lyDo}");
                Console.WriteLine($"  gói đã nhận: {thuDoc.TomTatGoi()}");
                BoXuat.Ghi(data, c.ThuMucRa, mayChu, c.NhanVat); // giữ lại bảng nền đã nhận
                await thuPhien.DisposeAsync();
            }

            if (phien == null || doc == null)
                throw new TimeoutException($"Đã thử {cachThu.Length} cách đăng nhập nhưng server không trả danh sách nhân vật.");

            await using var _ = phien;
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

    /// <summary>Nối, bắt tay, đăng nhập và chờ danh sách nhân vật. Trả null nếu thành công, ngược lại là lý do.</summary>
    private static async Task<string?> ThuDangNhapAsync(PhienHso phien, BoDocGoi doc, MayChu mayChu, CauHinh c,
        sbyte zoom, sbyte thietBi, sbyte infoLogin, CancellationToken ct)
    {
        await phien.NoiAsync(mayChu.Host, mayChu.Port, ct);
        await phien.DaBatTay.Task.WaitAsync(TimeSpan.FromSeconds(30), ct);
        await GuiDangNhapAsync(phien, c, zoom, thietBi, infoLogin, ct);
        await phien.GuiAsync(61, [], ct); // tên vị trí, vật phẩm nhiệm vụ, bảng đập đồ

        var nhanVat = doc.CoNhanVat.Task;
        var bangNen = Task.WhenAll(doc.CoVatPham.Task, doc.CoQuai.Task);
        var dung = new[] { nhanVat, doc.DangNhapHong.Task, phien.DaNgat.Task };

        var xong = await Task.WhenAny([.. dung, bangNen, Task.Delay(90_000, ct)]);
        // Bảng nền thường đến trước danh sách nhân vật; cho thêm một khoảng ngắn rồi mới bỏ cuộc.
        if (xong == bangNen && !nhanVat.IsCompleted)
            xong = await Task.WhenAny([.. dung, Task.Delay(30_000, ct)]);

        if (nhanVat.IsCompleted) return null;
        if (doc.DangNhapHong.Task.IsCompleted) return doc.LoiDangNhap ?? "đăng nhập thất bại";
        if (phien.DaNgat.Task.IsCompleted) return "máy chủ ngắt kết nối";
        return bangNen.IsCompleted ? "đã nhận bảng nền nhưng hết thời gian chờ" : "hết thời gian chờ";
    }

    private static Task GuiDangNhapAsync(PhienHso phien, CauHinh c, sbyte zoom, sbyte thietBi, sbyte indexInfoLogin,
        CancellationToken ct)
    {
        var goi = new BoGhi()
            .Utf(c.TaiKhoan).Utf(c.MatKhau).Utf(c.PhienBan)
            .Utf("0").Utf("0").Utf("0")
            .Byte(zoom)    // mGraphics.zoomLevel: 4 lấy ảnh lớn nhất của client PC
            .Byte(thietBi) // TemMidlet.DIVICE: 4 là bản Windows
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
