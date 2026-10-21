namespace Epoxid.VM;

public class InvalidCodeException(string? description) : Exception($"Invalid code object: {description}");
