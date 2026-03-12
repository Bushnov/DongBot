using System;
using System.IO;

namespace DongBot.Tests;

internal sealed class TestWorkspace : IDisposable
{
    public string RootPath { get; } = Path.Combine(Path.GetTempPath(), "DongBot.Tests", Guid.NewGuid().ToString("N"));

    public TestWorkspace()
    {
        Directory.CreateDirectory(RootPath);
    }

    public string GetPath(string relativePath)
    {
        string fullPath = Path.Combine(RootPath, relativePath);
        string? directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        return fullPath;
    }

    public void Dispose()
    {
        if (Directory.Exists(RootPath))
        {
            Directory.Delete(RootPath, recursive: true);
        }
    }
}
