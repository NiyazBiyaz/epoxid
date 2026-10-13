using Microsoft.CodeAnalysis;

namespace Epoxid.SourceGeneration.Tests;

public static class GeneratorTestHelper
{
    public static void GetResources(string resourceName, out FileValue[] commonSources, out FileValue[] sources, out FileValue[] generated)
    {
        string resourcesPath = Path.Combine(AppContext.BaseDirectory, "Resources");

        if (!Directory.Exists(resourcesPath))
        {
            throw new InvalidResourceDirectoryException("Resources", "directory not found");
        }

        if (!Directory.Exists(Path.Combine(resourcesPath, "Common")))
        {
            throw new InvalidResourceDirectoryException("Common", "directory nod found");
        }

        if (!Directory.Exists(Path.Combine(resourcesPath, resourceName)))
        {
            throw new InvalidResourceDirectoryException(resourceName, "directory nod found");
        }

        commonSources = Directory.EnumerateFiles(Path.Combine(resourcesPath, "Common"))
            .Where(fname => fname.EndsWith(".txt"))
            .Select(fname => new FileValue(fname[..^4], File.ReadAllText(fname)))
            .ToArray();

        sources = Directory.EnumerateFiles(Path.Combine(resourcesPath, resourceName))
            .Where(fname => fname.EndsWith(".cs.txt") && !fname.EndsWith(".g.cs.txt"))
            .Select(fname => new FileValue(Path.GetFileName(fname[..^4]), File.ReadAllText(fname)))
            .ToArray();

        generated = Directory.EnumerateFiles(Path.Combine(resourcesPath, resourceName))
            .Where(fname => fname.EndsWith(".g.cs.txt"))
            .Select(fname => new FileValue(Path.GetFileName(fname[..^4]), File.ReadAllText(fname)))
            .ToArray();
    }
}
