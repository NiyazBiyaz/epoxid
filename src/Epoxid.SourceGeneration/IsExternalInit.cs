// Because of Roslyn still works on netstandard2.0 we need to add this things

#pragma warning disable IDE0161 // Convert to file-scoped namespace
#pragma warning disable IDE0130 // Namespace does not match folder structure

namespace System.Runtime.CompilerServices
{
    public class IsExternalInit;
}
