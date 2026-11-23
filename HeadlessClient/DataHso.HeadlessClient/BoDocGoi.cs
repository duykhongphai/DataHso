namespace DataHso;

public sealed class BoDocGoi
{
    private static readonly HashSet<sbyte> GoiCanGiu = [1, 3, 12, 13, 25, 26, 28, 29, 52, 61, -50, -52, -49, -96];
    private readonly DuLieuHso data;
    private readonly string thuMuc;

    public TaskCompletionSource CoNhanVat { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource CoVatPham { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource CoQuai { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource DaVaoMap { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource DangNhapHong { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public BoDocGoi(DuLieuHso data, string thuMuc)
    {
        this.data = data;
        this.thuMuc = thuMuc;
    }

    private readonly System.Collections.Concurrent.ConcurrentDictionary<sbyte, int> demGoi = new();

    public string? LoiDangNhap => data.LoiDangNhap;

    /// <summary>Tóm tắt các command đã nhận, dùng để chẩn đoán khi server không trả gói mong đợi.</summary>
    public string TomTatGoi() => demGoi.IsEmpty
        ? "chưa nhận gói nào"
        : string.Join(", ", demGoi.OrderBy(x => x.Key).Select(x => x.Value == 1 ? $"{x.Key}" : $"{x.Key}×{x.Value}"));

    public Task NhanAsync(GoiTin goi)
    {
        demGoi.AddOrUpdate(goi.Lenh, 1, (_, n) => n + 1);
        if (GoiCanGiu.Contains(goi.Lenh)) LuuGoiTho(goi);
        try
        {
            switch (goi.Lenh)
            {
                case 1: DocBangDoMap(goi.DuLieu); break;
                case 2: DocDangNhapHong(goi.DuLieu); break;
                case 3: break;
                case 12: DocBanDo(goi.DuLieu); DaVaoMap.TrySetResult(); break;
                case 13: DocNhanVat(goi.DuLieu); CoNhanVat.TrySetResult(); break;
                case 25: DocVatPham(goi.DuLieu); CoVatPham.TrySetResult(); break;
                case 26: DocQuai(goi.DuLieu); CoQuai.TrySetResult(); break;
                case 28: DocTrangBi(goi.DuLieu); break;
                case 29: DocKyNang(goi.DuLieu); break;
                case 52: DocNhiemVu(goi.DuLieu); break;
                case 61: DocTenHeThong(goi.DuLieu); break;
                case -50: DocNpc(goi.DuLieu); break;
                case -51: LuuAnh(goi.DuLieu); break;
                case -52: LuuPart(goi.DuLieu); break;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"  gói {goi.Lenh} đọc dở tại {e.Message}");
        }
        return Task.CompletedTask;
    }

    private void LuuGoiTho(GoiTin goi)
    {
        var dir = Path.Combine(thuMuc, "RawPackets");
        Directory.CreateDirectory(dir);
        File.WriteAllBytes(Path.Combine(dir, $"Command_{goi.Lenh}.bin"), goi.DuLieu);
    }

    private void DocDangNhapHong(byte[] raw)
    {
        var r = new BoDoc(raw);
        data.LoiDangNhap = r.Utf();
        Console.WriteLine("Đăng nhập thất bại: " + data.LoiDangNhap);
        DangNhapHong.TrySetResult();
    }

    private void DocBangDoMap(byte[] raw)
    {
        var r = new BoDoc(raw);
        var bang = new BoDoc(r.Bytes(r.Short()));
        var n = bang.Short();
        lock (data.Khoa)
            for (short id = 0; id < n; id++)
            {
                var anh = bang.Short();
                bang.Byte();
                var dx = bang.Short();
                var dy = bang.Short();
                var soDiem = bang.UByte();
                var diem = new Diem[soDiem];
                for (var j = 0; j < soDiem; j++) diem[j] = new Diem(bang.Byte(), bang.Byte());
                data.BangDoMap[id] = new BangDoMap(id, anh, dx, dy, diem);
            }
    }

    private void DocVatPham(byte[] raw)
    {
        var r = new BoDoc(raw);
        var soPotion = r.Short();
        int soChiSo;
        short soNguyenLieu;
        lock (data.Khoa)
        {
            for (var i = 0; i < soPotion; i++)
            {
                var p = new Potion
                {
                    Id = r.Short(),
                    IconId = r.Short(),
                    Gia = r.Long(),
                    Ten = r.Utf(),
                    MoTa = r.Utf(),
                    Loai = r.Byte(),
                    LoaiTien = r.Byte(),
                    CoTheBan = r.Byte(),
                    GiaTri = r.Short(),
                    CoTheGiaoDich = r.Bool()
                };
                data.Potion[p.Id] = p;
            }

            soChiSo = r.UByte();
            for (var i = 0; i < soChiSo; i++)
                data.MauChiSo[i] = new MauChiSo(i, r.Utf(), r.Byte(), r.Byte());

            soNguyenLieu = r.Short();
            for (var i = 0; i < soNguyenLieu; i++)
            {
                var m = new NguyenLieu
                {
                    Id = r.Short(),
                    IconId = r.Short(),
                    Gia = r.Long(),
                    Ten = r.Utf(),
                    MoTa = r.Utf(),
                    Loai = r.Byte(),
                    LoaiTien = r.Byte(),
                    CoTheBan = r.Byte(),
                    GiaTri = r.Short(),
                    CoTheGiaoDich = r.Byte(),
                    MauTen = r.Byte()
                };
                data.NguyenLieu[m.Id] = m;
            }
        }
        Console.WriteLine($"  vật phẩm: {soPotion} potion, {soChiSo} chỉ số, {soNguyenLieu} nguyên liệu");
    }

    private void DocQuai(byte[] raw)
    {
        var r = new BoDoc(raw);
        var n = r.Short();
        lock (data.Khoa)
            for (var i = 0; i < n; i++)
            {
                var id = r.Short();
                data.Quai[id] = new Quai(id, r.Utf(), r.UByte(), r.Int(), r.Byte());
            }
        Console.WriteLine($"  quái: {n} mẫu");
    }

    private void DocNhanVat(byte[] raw)
    {
        var r = new BoDoc(raw);
        var n = r.UByte();
        lock (data.Khoa)
        {
            data.NhanVat.Clear();
            for (var i = 0; i < n; i++)
            {
                var nv = new NhanVat { Id = r.Int(), Ten = r.Utf(), Dau = r.Byte(), Toc = r.Byte(), Mat = r.Byte() };
                var soDo = r.UByte();
                var ds = new DoDangMac[soDo];
                for (var j = 0; j < soDo; j++) ds[j] = new DoDangMac(r.Byte(), r.Byte());
                nv.DangMac = ds;
                nv.Cap = r.Short();
                nv.Lop = r.Byte();
                r.Byte();
                r.Byte();
                var iconClan = r.Short();
                if (iconClan >= 0) nv.Clan = new Clan(iconClan, r.Utf(), r.Byte());
                data.NhanVat.Add(nv);
            }
        }
        Console.WriteLine($"  nhân vật: {n}");
    }

    private void DocTrangBi(byte[] raw)
    {
        var r = new BoDoc(raw);
        var t = new TrangBi
        {
            Id = r.Short(),
            Ten = r.Utf(),
            Loai = r.Byte(),
            Part = r.Byte(),
            LopNhanVat = r.Byte(),
            IconId = r.Short()
        };
        var n = r.UByte();
        var cs = new ChiSo[n];
        for (var i = 0; i < n; i++) cs[i] = new ChiSo(r.UByte(), r.Int());
        t.ChiSo = cs;
        lock (data.Khoa) data.TrangBi[t.Id] = t;
    }

    private void DocKyNang(byte[] raw)
    {
        var r = new BoDoc(raw);
        var n = r.UByte();
        lock (data.Khoa)
            for (var i = 0; i < n; i++)
            {
                var k = new KyNang
                {
                    Lop = data.LopHienTai,
                    Id = r.Byte(),
                    IconId = r.Byte(),
                    Ten = r.Utf(),
                    Loai = r.Byte(),
                    TamDanh = r.Short(),
                    MoTa = r.Utf(),
                    LoaiBuff = r.Byte(),
                    HieuUngPhu = r.Byte()
                };
                var soCap = r.UByte();
                var cacCap = new CapKyNang[soCap];
                for (var j = 0; j < soCap; j++)
                {
                    var cap = new CapKyNang
                    {
                        Mp = r.Short(),
                        CapYeuCau = r.Short(),
                        HoiChieuMs = r.Int(),
                        ThoiGianBuffMs = r.Int(),
                        PhanTramHieuUngPhu = r.Byte(),
                        ThoiGianHieuUngPhu = r.Short(),
                        CongHp = r.Short(),
                        CongMp = r.Short()
                    };
                    var soCs = r.UByte();
                    var cs = new ChiSo[soCs];
                    for (var x = 0; x < soCs; x++) cs[x] = new ChiSo(r.UByte(), r.Int());
                    cap.ChiSo = cs;
                    cap.SoMucTieu = r.Byte();
                    cap.TamLan = r.Short();
                    cacCap[j] = cap;
                }
                k.CacCap = cacCap;
                k.ThoiGianThiTrien = r.Short();
                k.KieuVe = r.Byte();
                data.KyNang[$"{k.Lop}:{k.Id}"] = k;
            }
        Console.WriteLine($"  kỹ năng: {n}");
    }

    private void DocTenHeThong(byte[] raw)
    {
        var r = new BoDoc(raw);
        var viTri = new string[r.UByte()];
        for (var i = 0; i < viTri.Length; i++) viTri[i] = r.Utf();
        var vp = new string[r.UByte()];
        for (var i = 0; i < vp.Length; i++) vp[i] = r.Utf();
        var nl = new short[r.UByte()];
        for (var i = 0; i < nl.Length; i++) nl[i] = r.Short();
        lock (data.Khoa) data.TenHeThong = new TenHeThong { ViTri = viTri, VatPhamNhiemVu = vp, NguyenLieuDapDo = nl };
    }

    private void DocBanDo(byte[] raw)
    {
        var r = new BoDoc(raw);
        var map = new BanDo { Id = r.Short(), XNhanVat = r.Short(), YNhanVat = r.Short(), IdMini = r.Short(), Ten = r.Utf() };
        var mapRaw = r.Bytes(r.Short());
        if (mapRaw.Length >= 3)
        {
            var mr = new BoDoc(mapRaw);
            map.Rong = mr.Byte();
            map.Cao = mr.Byte();
            map.BoTile = mr.Byte();
            var soTile = Math.Min(mr.Con, (byte)map.Rong * (byte)map.Cao);
            map.Tile = mr.Bytes(soTile).Select(x => (int)unchecked((sbyte)x)).ToArray();
        }
        var nen = r.Byte();
        if (nen >= 0) r.Short();
        var itemMap = r.Short();
        if (itemMap > 0) r.Bytes(itemMap);
        var soLoi = r.UByte();
        var loi = new LoiRa[soLoi];
        for (var i = 0; i < soLoi; i++) loi[i] = new LoiRa(r.Short(), r.Short(), r.Utf());
        map.LoiRa = loi;
        r.Byte();
        map.Khu = r.Byte();
        map.Loai = r.Byte();
        lock (data.Khoa) data.BanDo[map.Id] = map;
        Console.WriteLine($"  map [{map.Id}] {map.Ten}: {map.Rong}x{map.Cao}, {soLoi} lối ra");
    }

    private void DocNpc(byte[] raw)
    {
        var r = new BoDoc(raw);
        var n = r.UByte();
        lock (data.Khoa)
            for (var i = 0; i < n; i++)
            {
                var npc = new Npc
                {
                    Ten = r.Utf(),
                    LoiThoai = r.Utf(),
                    Id = r.Byte(),
                    Anh = r.Byte(),
                    X = r.Short(),
                    Y = r.Short(),
                    Rong = r.Byte(),
                    Cao = r.Byte(),
                    SoKhung = r.Byte()
                };
                npc.Avatar = (short)(r.Byte() + 500);
                npc.MoTa = r.Utf();
                r.Byte();
                r.Bool();
                data.Npc[npc.Id] = npc;
            }
    }

    private void DocNhiemVu(byte[] raw)
    {
        var r = new BoDoc(raw);
        var trangThai = r.Byte();
        if (trangThai >= 3) return;
        var n = r.UByte();
        lock (data.Khoa)
            for (var i = 0; i < n; i++)
            {
                var q = new NhiemVu { TrangThai = trangThai, Id = r.Short(), Chinh = r.Bool(), Ten = r.Utf() };
                if (trangThai == 0)
                {
                    q.NpcGiao = r.Byte(); q.LoiThoai = r.Utf(); q.NpcTra = r.Byte(); q.HuongDan = r.Utf();
                }
                else if (trangThai == 1)
                {
                    q.NpcTra = r.Byte(); q.LoiThoai = r.Utf(); q.HuongDan = r.Utf();
                }
                else
                {
                    q.LoaiMucTieu = r.Byte(); q.TomTat = r.Utf(); q.HuongDan = r.Utf(); q.NpcTra = r.Byte();
                    if (q.LoaiMucTieu is 0 or 1)
                    {
                        var soMucTieu = r.UByte();
                        var mt = new MucTieuNhiemVu[soMucTieu];
                        for (var x = 0; x < soMucTieu; x++) mt[x] = new MucTieuNhiemVu(r.Short(), r.Short(), r.Short());
                        q.MucTieu = mt;
                    }
                    else if (q.LoaiMucTieu == 2)
                    {
                        q.NpcTra = r.Byte(); q.TomTat = r.Utf();
                    }
                }
                data.NhiemVu[$"{trangThai}:{q.Chinh}:{q.Id}"] = q;
            }
    }

    private void LuuAnh(byte[] raw)
    {
        var r = new BoDoc(raw);
        var id = r.Short();
        var bytes = r.ConLai();
        if (bytes.Length == 0) return;
        var dir = Path.Combine(thuMuc, "Icons", (id / 1000).ToString());
        Directory.CreateDirectory(dir);
        File.WriteAllBytes(Path.Combine(dir, id + Duoi(bytes)), bytes);
    }

    private void LuuPart(byte[] raw)
    {
        var r = new BoDoc(raw);
        var loai = r.Byte();
        var id = r.Short();
        var anh = r.Bytes(r.Int());
        var dir = Path.Combine(thuMuc, "CharParts", loai.ToString());
        Directory.CreateDirectory(dir);
        if (anh.Length > 0) File.WriteAllBytes(Path.Combine(dir, id + Duoi(anh)), anh);
        if (r.Con > 0) File.WriteAllBytes(Path.Combine(dir, id + ".bin"), r.ConLai());
    }

    private static string Duoi(byte[] b)
    {
        if (b.Length >= 8 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47) return ".png";
        if (b.Length >= 3 && b[0] == 0x47 && b[1] == 0x49 && b[2] == 0x46) return ".gif";
        if (b.Length >= 2 && b[0] == 0xFF && b[1] == 0xD8) return ".jpg";
        return ".bin";
    }
}
