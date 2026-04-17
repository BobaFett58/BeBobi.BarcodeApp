namespace BarcodeApp.Tests.TestHelpers;

public sealed class TempPath : IDisposable
{
    public TempPath()
    {
        DirectoryPath = Path.Combine(Path.GetTempPath(), "BarcodeAppTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(DirectoryPath);
    }

    public string DirectoryPath { get; }

    public string GetFilePath(string fileName)
    {
        return Path.Combine(DirectoryPath, fileName);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(DirectoryPath))
            {
                Directory.Delete(DirectoryPath, recursive: true);
            }
        }
        catch
        {
            // Keep tests resilient if cleanup is blocked by OS file locks.
        }
    }
}
