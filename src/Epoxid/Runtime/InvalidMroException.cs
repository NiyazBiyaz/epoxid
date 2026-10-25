namespace Epoxid.Runtime;

public class MroConstructingException(string className)
    : Exception($"Cannot create Method Resolution Order for class '{className}'");
