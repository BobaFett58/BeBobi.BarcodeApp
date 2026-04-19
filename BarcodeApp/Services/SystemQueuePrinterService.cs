using System.Diagnostics;
using System.Text;

namespace Qivisoft.BarcodeApp.Services;

public interface ISystemQueuePrinterService
{
    Task SendRawAsync(string queueName, string zpl, CancellationToken cancellationToken = default);
}

public sealed class SystemQueuePrinterService : ISystemQueuePrinterService
{
    public async Task SendRawAsync(string queueName, string zpl, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queueName);
        ArgumentException.ThrowIfNullOrWhiteSpace(zpl);

        if (!OperatingSystem.IsMacOS() && !OperatingSystem.IsLinux())
            throw new InvalidOperationException("Druk przez kolejkę systemową jest dostępny na macOS/Linux.");

        var tempPath = Path.Combine(Path.GetTempPath(), $"qivilabel-{Guid.NewGuid():N}.zpl");

        try
        {
            await File.WriteAllTextAsync(tempPath, zpl, Encoding.ASCII, cancellationToken);

            var psi = new ProcessStartInfo
            {
                FileName = "lp",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };
            psi.ArgumentList.Add("-d");
            psi.ArgumentList.Add(queueName.Trim());
            psi.ArgumentList.Add("-o");
            psi.ArgumentList.Add("raw");
            psi.ArgumentList.Add(tempPath);

            using var process = Process.Start(psi)
                ?? throw new InvalidOperationException("Nie udało się uruchomić polecenia lp.");

            await process.WaitForExitAsync(cancellationToken);
            var stdErr = await process.StandardError.ReadToEndAsync(cancellationToken);

            if (process.ExitCode != 0)
                throw new InvalidOperationException(string.IsNullOrWhiteSpace(stdErr)
                    ? $"Polecenie lp zakończyło się kodem {process.ExitCode}."
                    : stdErr.Trim());
        }
        finally
        {
            try
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
            catch
            {
                // Best effort cleanup.
            }
        }
    }
}
