using System.Diagnostics;
using System.ComponentModel;
using System.Runtime.InteropServices;
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

        if (OperatingSystem.IsWindows())
        {
            var normalizedQueueName = queueName.Trim();
            await Task.Run(() => SendRawToWindowsQueue(normalizedQueueName, zpl), cancellationToken);
            return;
        }

        if (!OperatingSystem.IsMacOS() && !OperatingSystem.IsLinux())
            throw new InvalidOperationException("Druk przez kolejkę systemową jest dostępny na Windows/macOS/Linux.");

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

    private static void SendRawToWindowsQueue(string queueName, string zpl)
    {
        var bytes = Encoding.ASCII.GetBytes(zpl);

        if (!WindowsRawPrinter.OpenPrinter(queueName, out var printerHandle, IntPtr.Zero))
            throw CreateWindowsPrintException(queueName, "Nie udało się otworzyć kolejki drukarki");

        try
        {
            var documentStarted = false;
            var pageStarted = false;
            var docInfo = new WindowsRawPrinter.DocInfo
            {
                DocName = "QiviLabel ZPL",
                DataType = "RAW"
            };

            if (WindowsRawPrinter.StartDocPrinter(printerHandle, 1, ref docInfo) == 0)
                throw CreateWindowsPrintException(queueName, "Nie udało się rozpocząć zadania drukowania");

            documentStarted = true;

            try
            {
                if (!WindowsRawPrinter.StartPagePrinter(printerHandle))
                    throw CreateWindowsPrintException(queueName, "Nie udało się rozpocząć strony drukowania");

                pageStarted = true;

                try
                {
                    if (!WindowsRawPrinter.WritePrinter(printerHandle, bytes, bytes.Length, out var written))
                        throw CreateWindowsPrintException(queueName, "Nie udało się wysłać danych do drukarki");

                    if (written != bytes.Length)
                        throw new InvalidOperationException($"Wysłano tylko {written} z {bytes.Length} bajtów do kolejki '{queueName}'.");
                }
                finally
                {
                    if (pageStarted)
                        WindowsRawPrinter.EndPagePrinter(printerHandle);
                }
            }
            finally
            {
                if (documentStarted)
                    WindowsRawPrinter.EndDocPrinter(printerHandle);
            }
        }
        finally
        {
            WindowsRawPrinter.ClosePrinter(printerHandle);
        }
    }

    private static InvalidOperationException CreateWindowsPrintException(string queueName, string prefix)
    {
        var errorCode = Marshal.GetLastWin32Error();
        var errorMessage = new Win32Exception(errorCode).Message;
        return new InvalidOperationException($"{prefix} '{queueName}' (Win32: {errorCode}, {errorMessage}).");
    }

    private static class WindowsRawPrinter
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct DocInfo
        {
            [MarshalAs(UnmanagedType.LPWStr)]
            public string DocName;

            [MarshalAs(UnmanagedType.LPWStr)]
            public string OutputFile;

            [MarshalAs(UnmanagedType.LPWStr)]
            public string DataType;
        }

        [DllImport("winspool.Drv", EntryPoint = "OpenPrinterW", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern bool OpenPrinter(string printerName, out IntPtr printerHandle, IntPtr defaults);

        [DllImport("winspool.Drv", SetLastError = true)]
        internal static extern bool ClosePrinter(IntPtr printerHandle);

        [DllImport("winspool.Drv", EntryPoint = "StartDocPrinterW", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern int StartDocPrinter(IntPtr printerHandle, int level, ref DocInfo docInfo);

        [DllImport("winspool.Drv", SetLastError = true)]
        internal static extern bool EndDocPrinter(IntPtr printerHandle);

        [DllImport("winspool.Drv", SetLastError = true)]
        internal static extern bool StartPagePrinter(IntPtr printerHandle);

        [DllImport("winspool.Drv", SetLastError = true)]
        internal static extern bool EndPagePrinter(IntPtr printerHandle);

        [DllImport("winspool.Drv", SetLastError = true)]
        internal static extern bool WritePrinter(IntPtr printerHandle, byte[] bytes, int count, out int written);
    }
}
