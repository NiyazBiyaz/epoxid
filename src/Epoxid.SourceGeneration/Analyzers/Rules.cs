using Microsoft.CodeAnalysis;

namespace Epoxid.SourceGeneration.Analyzers;

public static class Rules
{
#pragma warning disable RS2008 // Enable analyzer release tracking

    public static readonly DiagnosticDescriptor MakePartial = new(
        "EPSG001",
        "Element should be declared as partial",
        "Element should be declared as partial",
        "Vital",
        DiagnosticSeverity.Error,
        true,
        "This class or property should be declared as partial for source generation."
    );

    public static readonly DiagnosticDescriptor MakeClassDerivedFromEpObject = new(
        "EPSG002",
        "This type should be derived from 'Epoxid.Runtime.Objects.EpObject' because it is declared as epoxid class",
        "This type should be derived from 'Epoxid.Runtime.Objects.EpObject' because it is declared as epoxid class",
        "Design",
        DiagnosticSeverity.Error,
        true,
        "Types that are defined to be epoxid type should be derived from EpObject to have common object semantics."
    );

    public static readonly DiagnosticDescriptor AddBaseInitializer = new(
        "EPSG003",
        "Constructor of Epoxid type should have base initializer",
        "Constructor of Epoxid type should have base initializer",
        "Design",
        DiagnosticSeverity.Warning,
        true,
        "Constructor of the Epoxid type should call base initializer for the correct slots allocation."
    );

#pragma warning restore RS2008 // Enable analyzer release tracking
}
