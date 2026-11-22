using System.Globalization;
using System.Text;

namespace DataHso;

public sealed class CauHinh
{
    public string MayChu { get; set; } = "Chiến Thần";
    public string? Host { get; set; }
    public int Port { get; set; } = 19129;
    public string TaiKhoan { get; set; } = "";
    public string MatKhau { get; set; } = "";
    public int NhanVat { get; set; }
    public string PhienBan { get; set; } = "4.0.3";
    public string ThuMuc { get; set; } = "ChienThan";
    public string Ra { get; set; } = ".";
    public int ItemToiDa { get; set; } = 4000;
    public int NhipItemMs { get; set; } = 60;
    public int NhipAnhMs { get; set; } = 80;
    public int ChoDuLieuMs { get; set; } = 600_000;
    public bool TaiAnh { get; set; } = true;

    public string ThuMucRa => Path.Combine(Ra, "TeaMobi", ThuMuc);

    public static CauHinh Doc(string[] args)
    {
        var c = new CauHinh();
        Moi("HSO_MAY_CHU", v => c.MayChu = v);
        Moi("HSO_HOST", v => c.Host = v);
        Moi("HSO_PORT", v => c.Port = So(v, c.Port));
        Moi("HSO_TK", v => c.TaiKhoan = v);
        Moi("HSO_MK", v => c.MatKhau = v);
        Moi("HSO_NHAN_VAT", v => c.NhanVat = So(v, 0));
        Moi("HSO_PHIEN_BAN", v => c.PhienBan = v);
        Moi("HSO_THUMUC", v => c.ThuMuc = v);
        Moi("HSO_RA", v => c.Ra = v);
        Moi("HSO_ITEM_TOI_DA", v => c.ItemToiDa = So(v, c.ItemToiDa));
        Moi("HSO_NHIP_ITEM", v => c.NhipItemMs = So(v, c.NhipItemMs));
        Moi("HSO_NHIP_ANH", v => c.NhipAnhMs = So(v, c.NhipAnhMs));
        Moi("HSO_CHO", v => c.ChoDuLieuMs = So(v, c.ChoDuLieuMs));
        Moi("HSO_KHONG_ANH", _ => c.TaiAnh = false);

        for (var i = 0; i < args.Length; i++)
        {
            string KeTiep() => i + 1 < args.Length ? args[++i] : "";
            switch (args[i])
            {
                case "--maychu": c.MayChu = KeTiep(); break;
                case "--host": c.Host = KeTiep(); break;
                case "--port": c.Port = So(KeTiep(), c.Port); break;
                case "--tk": c.TaiKhoan = KeTiep(); break;
                case "--mk": c.MatKhau = KeTiep(); break;
                case "--nhan-vat": c.NhanVat = So(KeTiep(), 0); break;
                case "--phien-ban": c.PhienBan = KeTiep(); break;
                case "--thumuc": c.ThuMuc = KeTiep(); break;
                case "--ra": c.Ra = KeTiep(); break;
                case "--item-toi-da": c.ItemToiDa = So(KeTiep(), c.ItemToiDa); break;
                case "--nhip-item": c.NhipItemMs = So(KeTiep(), c.NhipItemMs); break;
                case "--nhip-anh": c.NhipAnhMs = So(KeTiep(), c.NhipAnhMs); break;
                case "--cho": c.ChoDuLieuMs = So(KeTiep(), c.ChoDuLieuMs); break;
                case "--khong-anh": c.TaiAnh = false; break;
            }
        }

        if (string.IsNullOrWhiteSpace(c.ThuMuc)) c.ThuMuc = KhongDau(c.MayChu);
        return c;

        static void Moi(string ten, Action<string> dat)
        {
            var v = Environment.GetEnvironmentVariable(ten);
            if (!string.IsNullOrWhiteSpace(v)) dat(v.Trim());
        }
    }

    public string? Loi()
    {
        if (string.IsNullOrWhiteSpace(TaiKhoan)) return "Thiếu tài khoản: HSO_TK hoặc --tk";
        if (string.IsNullOrWhiteSpace(MatKhau)) return "Thiếu mật khẩu: HSO_MK hoặc --mk";
        if (NhanVat < 0) return "Index nhân vật phải từ 0 trở lên";
        if (Port is < 1 or > 65535) return "Cổng máy chủ không hợp lệ";
        return null;
    }

    private static int So(string value, int macDinh) =>
        int.TryParse(value, out var n) ? n : macDinh;

    public static string KhongDau(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var b = new StringBuilder();
        foreach (var ch in normalized)
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark &&
                char.IsLetterOrDigit(ch)) b.Append(ch == 'đ' || ch == 'Đ' ? 'd' : ch);
        return b.Length == 0 ? "Server" : b.ToString();
    }
}

public sealed record MayChu(string Ten, string Host, int Port, int NgonNgu)
{
    private const string DiaChi = "http://knightageonline.com/srvip/";

    private static readonly MayChu[] DuPhong =
    [
        new("Bạch Hổ New", "hs8.teamobi.com", 19129, 0),
        new("Chiến Thần", "hs1.teamobi.com", 19129, 0),
        new("Rồng Lửa", "hs2.teamobi.com", 19129, 0),
        new("Global Server", "hsglobal.teamobi.com", 19129, 1),
        new("Phượng Hoàng", "hs3.teamobi.com", 19129, 0),
        new("Nhân Mã", "hs5.teamobi.com", 19129, 0),
        new("Kì Lân", "hs6.teamobi.com", 19129, 0),
        new("Thiên Hà", "hs7.teamobi.com", 19129, 0),
        new("Thách Đấu", "hs4.teamobi.com", 19129, 0)
    ];

    public static async Task<IReadOnlyList<MayChu>> LayDanhSachAsync(CancellationToken ct)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            var text = (await http.GetStringAsync(DiaChi, ct)).TrimStart('\uFEFF').Trim();
            var ds = new List<MayChu>();
            foreach (var muc in text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var f = muc.Split(':');
                if (f.Length == 4 && int.TryParse(f[2], out var port) && int.TryParse(f[3], out var lang))
                    ds.Add(new MayChu(f[0].Trim(), f[1].Trim(), port, lang));
            }
            if (ds.Count > 0) return ds;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Không lấy được danh sách máy chủ sống: {e.Message}; dùng bản dự phòng.");
        }
        return DuPhong;
    }
}

