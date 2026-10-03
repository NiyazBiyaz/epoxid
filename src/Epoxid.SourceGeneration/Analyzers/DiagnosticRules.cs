using Microsoft.CodeAnalysis;

namespace Epoxid.SourceGeneration.Analyzers;

public static class DiagnosticRules
{
#pragma warning disable RS2008 // Enable analyzer release tracking

    public static readonly DiagnosticDescriptor MakePartial = new(
        "EPSG001",
        "Element should be declared as partial",
        "Element should be declared as partial",
        "Correctness",
        DiagnosticSeverity.Error,
        true,
        "This class or property should be declared as partial for source generation."
    );

    public static readonly DiagnosticDescriptor MakeClassDerivedFromEpObject = new(
        "EPSG002",
        "This type should be derived from 'Epoxid.Runtime.Objects.EpObject' because it is declared as epoxid class",
        "This type should be derived from 'Epoxid.Runtime.Objects.EpObject' because it is declared as epoxid class",
        "Correctness",
        DiagnosticSeverity.Error,
        true,
        "Types that are defined to be epoxid type should be derived from EpObject to have common object semantics."
    );

#pragma warning restore RS2008 // Enable analyzer release tracking
}
