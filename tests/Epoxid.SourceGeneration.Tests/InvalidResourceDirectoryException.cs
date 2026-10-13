namespace Epoxid.SourceGeneration.Tests;

public class InvalidResourceDirectoryException(string name, string reason)
    : Exception($"Invalid directory content for the '{name}' test case: {reason}");
