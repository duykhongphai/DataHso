using System.Buffers.Binary;
using System.Net.Sockets;
using System.Text;

namespace DataHso;

public sealed record GoiTin(sbyte Lenh, byte[] DuLieu);

public sealed class BoDoc
{
    private readonly byte[] data;
    private int pos;

    public BoDoc(byte[] data) => this.data = data;
    public int Con => data.Length - pos;
    public int ViTri => pos;

    public sbyte Byte()
    {
        Can(1);
        return unchecked((sbyte)data[pos++]);
    }

    public byte UByte()
    {
        Can(1);
        return data[pos++];
    }

    public bool Bool() => Byte() > 0;

    public short Short()
    {
        Can(2);
        var n = BinaryPrimitives.ReadInt16BigEndian(data.AsSpan(pos, 2));
        pos += 2;
        return n;
    }

    public ushort UShort()
    {
        Can(2);
        var n = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(pos, 2));
        pos += 2;
        return n;
    }

    public int Int()
    {
        Can(4);
        var n = BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(pos, 4));
        pos += 4;
        return n;
    }

    public long Long()
    {
        Can(8);
        var n = BinaryPrimitives.ReadInt64BigEndian(data.AsSpan(pos, 8));
        pos += 8;
        return n;
    }

    public string Utf()
    {
        var n = UShort();
        return Encoding.UTF8.GetString(Bytes(n));
    }

    public byte[] Bytes(int n)
    {
        Can(n);
        var b = data.AsSpan(pos, n).ToArray();
        pos += n;
        return b;
    }

    public byte[] ConLai() => Bytes(Con);

    private void Can(int n)
    {
        if (n < 0 || pos + n > data.Length)
            throw new EndOfStreamException($"Gói thiếu dữ liệu tại {pos}, cần {n}, còn {Con}.");
    }
}

public sealed class BoGhi
{
    private readonly MemoryStream stream = new();

    public BoGhi Byte(int n) { stream.WriteByte(unchecked((byte)n)); return this; }
    public BoGhi Bool(bool value) => Byte(value ? 1 : 0);

    public BoGhi Short(int n)
    {
        Span<byte> b = stackalloc byte[2];
        BinaryPrimitives.WriteInt16BigEndian(b, unchecked((short)n));
        stream.Write(b);
        return this;
    }

    public BoGhi Int(int n)
    {
        Span<byte> b = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(b, n);
        stream.Write(b);
        return this;
    }

    public BoGhi Utf(string value)
    {
        var b = Encoding.UTF8.GetBytes(value ?? "");
        if (b.Length > ushort.MaxValue) throw new InvalidDataException("Chuỗi UTF quá dài.");
        Short(b.Length);
        stream.Write(b);
        return this;
    }

    public byte[] Xong() => stream.ToArray();
}

public sealed class PhienHso : IAsyncDisposable
{
    private static readonly HashSet<sbyte> LenhDai = [-51, -52, -54, 126];
    private readonly TcpClient tcp = new();
    private readonly SemaphoreSlim khoaGhi = new(1, 1);
    private readonly CancellationTokenSource dung = new();
    private NetworkStream? stream;
    private byte[]? key;
    private int viTriDoc;
    private int viTriGhi;
    private Task? viecDoc;

    public event Func<GoiTin, Task>? Nhan;
    public TaskCompletionSource DaBatTay { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource DaNgat { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public async Task NoiAsync(string host, int port, CancellationToken ct)
    {
        await tcp.ConnectAsync(host, port, ct);
        tcp.ReceiveBufferSize = 128_000;
        stream = tcp.GetStream();
        viecDoc = Task.Run(DocAsync);
        await GuiAsync(-40, [], ct);
    }

    public async Task GuiAsync(sbyte lenh, byte[] duLieu, CancellationToken ct = default)
    {
        if (stream == null) throw new InvalidOperationException("Chưa nối máy chủ.");
        if (duLieu.Length > ushort.MaxValue) throw new InvalidDataException("Gói gửi quá 65535 byte.");

        await khoaGhi.WaitAsync(ct);
        try
        {
            var goi = new byte[3 + duLieu.Length];
            goi[0] = MaHoaGhi(unchecked((byte)lenh));
            goi[1] = MaHoaGhi((byte)(duLieu.Length >> 8));
            goi[2] = MaHoaGhi((byte)duLieu.Length);
            for (var i = 0; i < duLieu.Length; i++) goi[i + 3] = MaHoaGhi(duLieu[i]);
            await stream.WriteAsync(goi, ct);
            await stream.FlushAsync(ct);
        }
        finally
        {
            khoaGhi.Release();
        }
    }

    private async Task DocAsync()
    {
        try
        {
            while (!dung.IsCancellationRequested)
            {
                var cmd = unchecked((sbyte)MaHoaDoc(await DocMotByteAsync(dung.Token)));
                var dai = LenhDai.Contains(cmd);
                if (cmd == 126 && key != null)
                    cmd = unchecked((sbyte)MaHoaDoc(await DocMotByteAsync(dung.Token)));

                var n = dai && key != null
                    ? await DocSoAsync(4, dung.Token)
                    : await DocSoAsync(2, dung.Token);
                if (n < 0 || n > 32 * 1024 * 1024) throw new InvalidDataException($"Độ dài gói {cmd} không hợp lệ: {n}");
                var raw = await DocDuAsync(n, dung.Token);
                if (key != null)
                    for (var i = 0; i < raw.Length; i++) raw[i] = MaHoaDoc(raw[i]);

                if (cmd == -40 && key == null)
                {
                    DocKey(raw);
                    DaBatTay.TrySetResult();
                    continue;
                }

                var xuLy = Nhan;
                if (xuLy != null) await xuLy(new GoiTin(cmd, raw));
            }
        }
        catch (Exception e)
        {
            if (!dung.IsCancellationRequested) Console.WriteLine($"Mất kết nối: {e.Message}");
            DaBatTay.TrySetException(e);
        }
        finally
        {
            DaNgat.TrySetResult();
        }
    }

    private void DocKey(byte[] raw)
    {
        var r = new BoDoc(raw);
        var n = r.UByte();
        key = r.Bytes(n);
        for (var i = 0; i < key.Length - 1; i++) key[i + 1] ^= key[i];
        viTriDoc = viTriGhi = 0;
        Console.WriteLine($"Bắt tay xong: key {key.Length} byte.");
    }

    private byte MaHoaDoc(byte b)
    {
        if (key == null) return b;
        var k = key[viTriDoc++];
        if (viTriDoc >= key.Length) viTriDoc = 0;
        return (byte)(k ^ b);
    }

    private byte MaHoaGhi(byte b)
    {
        if (key == null) return b;
        var k = key[viTriGhi++];
        if (viTriGhi >= key.Length) viTriGhi = 0;
        return (byte)(k ^ b);
    }

    private async Task<byte> DocMotByteAsync(CancellationToken ct)
    {
        var b = await DocDuAsync(1, ct);
        return b[0];
    }

    private async Task<int> DocSoAsync(int soByte, CancellationToken ct)
    {
        var b = await DocDuAsync(soByte, ct);
        for (var i = 0; i < b.Length; i++) b[i] = MaHoaDoc(b[i]);
        return soByte == 2
            ? BinaryPrimitives.ReadUInt16BigEndian(b)
            : BinaryPrimitives.ReadInt32BigEndian(b);
    }

    private async Task<byte[]> DocDuAsync(int n, CancellationToken ct)
    {
        if (stream == null) throw new EndOfStreamException();
        var b = new byte[n];
        var daDoc = 0;
        while (daDoc < n)
        {
            var k = await stream.ReadAsync(b.AsMemory(daDoc, n - daDoc), ct);
            if (k == 0) throw new EndOfStreamException("Máy chủ đóng socket.");
            daDoc += k;
        }
        return b;
    }

    public async ValueTask DisposeAsync()
    {
        dung.Cancel();
        tcp.Close();
        if (viecDoc != null)
            try { await viecDoc; } catch { }
        khoaGhi.Dispose();
        dung.Dispose();
    }
}
