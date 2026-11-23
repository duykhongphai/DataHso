using System.Text.Json;
using System.Text.Json.Serialization;

namespace DataHso;

public static class BoXuat
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static void NapCu(DuLieuHso d, string thuMuc)
    {
        Nap(Path.Combine(thuMuc, "MapItemTemplates.json"), d.BangDoMap, x => x.Id);
        Nap(Path.Combine(thuMuc, "Potions.json"), d.Potion, x => x.Id);
        Nap(Path.Combine(thuMuc, "ItemOptions.json"), d.MauChiSo, x => x.Id);
        Nap(Path.Combine(thuMuc, "Materials.json"), d.NguyenLieu, x => x.Id);
        Nap(Path.Combine(thuMuc, "EquipmentItems.json"), d.TrangBi, x => x.Id);
        Nap(Path.Combine(thuMuc, "Monsters.json"), d.Quai, x => x.Id);
        Nap(Path.Combine(thuMuc, "Skills.json"), d.KyNang, x => $"{x.Lop}:{x.Id}");
        // Bản cũ không ghi lớp nên các phái ghi đè lên nhau; bỏ đi để lượt mới lấy lại đúng lớp.
        foreach (var khoa in d.KyNang.Where(x => x.Value.Lop < 0).Select(x => x.Key).ToList()) d.KyNang.Remove(khoa);
        Nap(Path.Combine(thuMuc, "Maps.json"), d.BanDo, x => x.Id);
        Nap(Path.Combine(thuMuc, "Npcs.json"), d.Npc, x => x.Id);
        Nap(Path.Combine(thuMuc, "Quests.json"), d.NhiemVu, x => $"{x.TrangThai}:{x.Chinh}:{x.Id}");

        var nhanVat = Doc<List<NhanVat>>(Path.Combine(thuMuc, "Characters.json"));
        if (nhanVat != null) d.NhanVat.AddRange(nhanVat);
        d.TenHeThong = Doc<TenHeThong>(Path.Combine(thuMuc, "Names.json")) ?? new TenHeThong();
    }

    private static T? Doc<T>(string path)
    {
        if (!File.Exists(path)) return default;
        try { return JsonSerializer.Deserialize<T>(File.ReadAllText(path), Json); }
        catch { return default; }
    }

    private static void Nap<T, TKey>(string path, Dictionary<TKey, T> dich, Func<T, TKey> khoa)
        where TKey : notnull
    {
        if (!File.Exists(path)) return;
        try
        {
            foreach (var x in JsonSerializer.Deserialize<List<T>>(File.ReadAllText(path), Json) ?? [])
                dich[khoa(x)] = x;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Không đọc được dữ liệu cũ {Path.GetFileName(path)}: {e.Message}");
        }
    }

    public static void Ghi(DuLieuHso d, string thuMuc, MayChu mayChu, int nhanVat)
    {
        Directory.CreateDirectory(thuMuc);
        lock (d.Khoa)
        {
            Tep(thuMuc, "MapItemTemplates.json", d.BangDoMap.Values.OrderBy(x => x.Id));
            Tep(thuMuc, "Potions.json", d.Potion.Values.OrderBy(x => x.Id));
            Tep(thuMuc, "ItemOptions.json", d.MauChiSo.Values.OrderBy(x => x.Id));
            Tep(thuMuc, "Materials.json", d.NguyenLieu.Values.OrderBy(x => x.Id));
            Tep(thuMuc, "EquipmentItems.json", d.TrangBi.Values.OrderBy(x => x.Id));
            Tep(thuMuc, "Monsters.json", d.Quai.Values.OrderBy(x => x.Id));
            Tep(thuMuc, "Skills.json", d.KyNang.Values.OrderBy(x => x.Lop).ThenBy(x => x.Id));
            Tep(thuMuc, "Characters.json", d.NhanVat);
            Tep(thuMuc, "Maps.json", d.BanDo.Values.OrderBy(x => x.Id));
            Tep(thuMuc, "Npcs.json", d.Npc.Values.OrderBy(x => x.Id));
            Tep(thuMuc, "Quests.json", d.NhiemVu.Values.OrderBy(x => x.TrangThai).ThenBy(x => x.Id));
            Tep(thuMuc, "Names.json", d.TenHeThong);
            Tep(thuMuc, "Server.json", new
            {
                mayChu.Ten,
                mayChu.Host,
                mayChu.Port,
                mayChu.NgonNgu,
                characterIndex = nhanVat,
                updatedAt = DateTimeOffset.UtcNow
            });
        }

        File.WriteAllText(Path.Combine(thuMuc, "LastUpdated"), DateTimeOffset.UtcNow.ToString("O") + "\n");
        GhiDanhSachAnh(thuMuc);
    }

    private static void GhiDanhSachAnh(string thuMuc)
    {
        var dir = Path.Combine(thuMuc, "Icons");
        if (!Directory.Exists(dir)) return;
        var ids = Directory.EnumerateFiles(dir, "*.*", SearchOption.AllDirectories)
            .Select(Path.GetFileNameWithoutExtension)
            .Select(x => short.TryParse(x, out var id) ? (short?)id : null)
            .Where(x => x.HasValue).Select(x => x!.Value).Distinct().OrderBy(x => x);
        Tep(thuMuc, "Images.json", ids);
    }

    private static void Tep<T>(string thuMuc, string ten, T duLieu) =>
        File.WriteAllText(Path.Combine(thuMuc, ten), JsonSerializer.Serialize(duLieu, Json) + "\n");
}

