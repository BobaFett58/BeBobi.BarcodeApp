using System.Net.Sockets;
using System.Text;

namespace Qivisoft.BarcodeApp.Services;

public interface IZebraPrinterService
{
    Task SendAsync(string host, int port, string zpl, CancellationToken cancellationToken = default);
}

public sealed class ZebraPrinterService : IZebraPrinterService
{
    private static readonly Encoding ZplEncoding = new UTF8Encoding(false);

    public async Task SendAsync(string host, int port, string zpl, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(host);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(port);
        ArgumentException.ThrowIfNullOrWhiteSpace(zpl);

        using var client = new TcpClient();
        await client.ConnectAsync(host.Trim(), port, cancellationToken);

        await using var stream = client.GetStream();
        var bytes = ZplEncoding.GetBytes(zpl);
        await stream.WriteAsync(bytes, cancellationToken);
        await stream.FlushAsync(cancellationToken);
    }
}
