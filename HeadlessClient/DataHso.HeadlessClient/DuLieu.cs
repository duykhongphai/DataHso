namespace DataHso;

public sealed record ChiSo(int Id, int GiaTri);
public sealed record DoDangMac(int ViTri, int Mau);
public sealed record BangDoMap(short Id, short Anh, short Dx, short Dy, Diem[] Diem);
public sealed record Diem(sbyte X, sbyte Y);

public sealed class Potion
{
    public short Id { get; set; }
    public short IconId { get; set; }
    public long Gia { get; set; }
    public string Ten { get; set; } = "";
    public string MoTa { get; set; } = "";
    public sbyte Loai { get; set; }
    public sbyte LoaiTien { get; set; }
    public sbyte CoTheBan { get; set; }
    public short GiaTri { get; set; }
    public bool CoTheGiaoDich { get; set; }
}

public sealed record MauChiSo(int Id, string Ten, sbyte Mau, sbyte LaPhanTram);

public sealed class NguyenLieu
{
    public short Id { get; set; }
    public short IconId { get; set; }
    public long Gia { get; set; }
    public string Ten { get; set; } = "";
    public string MoTa { get; set; } = "";
    public sbyte Loai { get; set; }
    public sbyte LoaiTien { get; set; }
    public sbyte CoTheBan { get; set; }
    public short GiaTri { get; set; }
    public sbyte CoTheGiaoDich { get; set; }
    public sbyte MauTen { get; set; }
}

public sealed class TrangBi
{
    public short Id { get; set; }
    public string Ten { get; set; } = "";
    public sbyte Loai { get; set; }
    public sbyte Part { get; set; }
    public sbyte LopNhanVat { get; set; }
    public short IconId { get; set; }
    public ChiSo[] ChiSo { get; set; } = [];
}

public sealed record Quai(short Id, string Ten, short Cap, int Hp, sbyte KieuDiChuyen);

public sealed class CapKyNang
{
    public short Mp { get; set; }
    public short CapYeuCau { get; set; }
    public int HoiChieuMs { get; set; }
    public int ThoiGianBuffMs { get; set; }
    public sbyte PhanTramHieuUngPhu { get; set; }
    public short ThoiGianHieuUngPhu { get; set; }
    public short CongHp { get; set; }
    public short CongMp { get; set; }
    public ChiSo[] ChiSo { get; set; } = [];
    public sbyte SoMucTieu { get; set; }
    public short TamLan { get; set; }
}

public sealed class KyNang
{
    public sbyte Id { get; set; }
    public sbyte IconId { get; set; }
    public string Ten { get; set; } = "";
    public sbyte Loai { get; set; }
    public short TamDanh { get; set; }
    public string MoTa { get; set; } = "";
    public sbyte LoaiBuff { get; set; }
    public sbyte HieuUngPhu { get; set; }
    public CapKyNang[] CacCap { get; set; } = [];
    public short ThoiGianThiTrien { get; set; }
    public sbyte KieuVe { get; set; }
}

public sealed record Clan(short IconId, string TenTat, sbyte ChucVu);

public sealed class NhanVat
{
    public int Id { get; set; }
    public string Ten { get; set; } = "";
    public sbyte Dau { get; set; }
    public sbyte Toc { get; set; }
    public sbyte Mat { get; set; }
    public DoDangMac[] DangMac { get; set; } = [];
    public short Cap { get; set; }
    public sbyte Lop { get; set; }
    public Clan? Clan { get; set; }
}

public sealed record LoiRa(short X, short Y, string Ten);

public sealed class BanDo
{
    public short Id { get; set; }
    public short IdMini { get; set; }
    public string Ten { get; set; } = "";
    public short XNhanVat { get; set; }
    public short YNhanVat { get; set; }
    public sbyte Rong { get; set; }
    public sbyte Cao { get; set; }
    public sbyte BoTile { get; set; }
    public int[] Tile { get; set; } = [];
    public LoiRa[] LoiRa { get; set; } = [];
    public sbyte Khu { get; set; }
    public sbyte Loai { get; set; }
}

public sealed class Npc
{
    public sbyte Id { get; set; }
    public string Ten { get; set; } = "";
    public string LoiThoai { get; set; } = "";
    public sbyte Anh { get; set; }
    public short X { get; set; }
    public short Y { get; set; }
    public sbyte Rong { get; set; }
    public sbyte Cao { get; set; }
    public sbyte SoKhung { get; set; }
    public short Avatar { get; set; }
    public string MoTa { get; set; } = "";
}

public sealed record MucTieuNhiemVu(short Id, short HienTai, short Can);

public sealed class NhiemVu
{
    public sbyte TrangThai { get; set; }
    public short Id { get; set; }
    public bool Chinh { get; set; }
    public string Ten { get; set; } = "";
    public sbyte LoaiMucTieu { get; set; } = -1;
    public sbyte NpcGiao { get; set; }
    public sbyte NpcTra { get; set; }
    public string LoiThoai { get; set; } = "";
    public string TomTat { get; set; } = "";
    public string HuongDan { get; set; } = "";
    public MucTieuNhiemVu[] MucTieu { get; set; } = [];
}

public sealed class TenHeThong
{
    public string[] ViTri { get; set; } = [];
    public string[] VatPhamNhiemVu { get; set; } = [];
    public short[] NguyenLieuDapDo { get; set; } = [];
}

public sealed class DuLieuHso
{
    public object Khoa { get; } = new();
    public Dictionary<short, BangDoMap> BangDoMap { get; } = [];
    public Dictionary<short, Potion> Potion { get; } = [];
    public Dictionary<int, MauChiSo> MauChiSo { get; } = [];
    public Dictionary<short, NguyenLieu> NguyenLieu { get; } = [];
    public Dictionary<short, TrangBi> TrangBi { get; } = [];
    public Dictionary<short, Quai> Quai { get; } = [];
    public Dictionary<sbyte, KyNang> KyNang { get; } = [];
    public List<NhanVat> NhanVat { get; } = [];
    public Dictionary<short, BanDo> BanDo { get; } = [];
    public Dictionary<sbyte, Npc> Npc { get; } = [];
    public Dictionary<string, NhiemVu> NhiemVu { get; } = [];
    public TenHeThong TenHeThong { get; set; } = new();
    public string? LoiDangNhap { get; set; }
}

