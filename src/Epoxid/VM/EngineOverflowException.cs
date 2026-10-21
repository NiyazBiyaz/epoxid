namespace Epoxid.VM;

public class EngineOverflowException(int stackSize) : Exception($"Engine stack limit exceeded ({stackSize})");
