namespace Epoxid.SourceGeneration.Tests;

public static class Common
{
    public static string WrapCode(string code) => $"""
    using Epoxid.Runtime.Objects;
    using Epoxid.SourceGeneration;

    {code}

    """;

    public const string Declarations = """
    using System;

    namespace Epoxid.Runtime.Objects
    {
        public class EpObject
        {
            protected EpObject()
            {
            }
        }
    }

    namespace Epoxid.SourceGeneration
    {

    }


    """;
}
