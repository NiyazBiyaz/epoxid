using System.Collections.Immutable;
using System.Diagnostics;
using Epoxid.Runtime;
using Epoxid.SyntaxAnalysis;
using Epoxid.VM;

namespace Epoxid.CodeGen;

internal class FunctionGenerator
{
    private readonly List<ParameterView> positionalParams = [];
    private readonly List<ParameterView> freeParams = [];
    private readonly List<ParameterView> keywordParams = [];
    private PositionalVariadicParameterView? positionalVariadic;
    private KeywordVariadicParameterView? keywordVariadic;

    public CodeObject GenerateCodeObject(CodeBuilder parentBuilder, FunctionDefView functionDef)
    {
        var functionParameters = functionDef.Parameters;

        if (functionParameters != null)
        {
            fillParameters(functionParameters.Parameters);
        }

        var builder = new CodeBuilder(parentBuilder.SymbolTable.ChildSymbolTables.First(st => st.Owner == functionDef.Name.RawString))
        {
            IsFunction = true,
        };

        CodeGenerator.GenerateFunction(builder, functionDef);

        return builder.Compile();
    }

    public FunctionParametersDescription GetParamsDescription() => new()
    {
        PositionalOnlyParams = positionalParams.Select(p => p.AsFunctionParameter()).ToImmutableArray(),

        FreeParams = freeParams.Select(p => p.AsFunctionParameter()).ToImmutableArray(),

        KeywordOnlyParams = keywordParams.Select(p => p.AsFunctionParameter()).ToImmutableArray(),

        VariadicPositionalParam = positionalVariadic?.AsFunctionParameter(),

        VariadicKeywordParam = keywordVariadic?.AsFunctionParameter(),
    };

    private void fillParameters(ImmutableArray<ParameterView> parameters)
    {
        ParamSection section;
        if (parameters.Any(p => p is PositionalOnlyMarkerView))
            section = ParamSection.Positional;
        else
            section = ParamSection.Free;

        bool wasDefault = false;
        bool wantParam = section is ParamSection.Positional;

        foreach (var param in parameters)
        {
            switch (section)
            {
                case ParamSection.Positional:
                {
                    switch (param)
                    {
                        case OrdinalParameterView ordinal:
                            if (wasDefault)
                                throw new SyntaxErrorException("parameter without a default follows parameter with a default");

                            positionalParams.Add(ordinal);
                            break;

                        case ParameterDefaultView def:
                            wasDefault = true;
                            positionalParams.Add(def);
                            break;

                        case PositionalOnlyMarkerView:
                            if (wantParam)
                                throw new SyntaxErrorException("at least one parameter must precede positional-only marker");

                            section = ParamSection.Free;
                            break;

                        case KeywordOnlyMarkerView:
                        case KeywordVariadicParameterView:
                        case PositionalVariadicParameterView:
                            throw new SyntaxErrorException("invalid positional-only parameter");

                        default:
                            throw new UnreachableException();
                    }

                    wantParam = false;
                    break;
                }

                case ParamSection.Free:
                {
                    switch (param)
                    {
                        case OrdinalParameterView ordinal:
                            if (wasDefault)
                                throw new SyntaxErrorException("parameter without a default follows parameter with a default");

                            freeParams.Add(ordinal);
                            break;

                        case ParameterDefaultView def:
                            wasDefault = true;
                            freeParams.Add(def);
                            break;

                        case KeywordOnlyMarkerView:
                            wantParam = true;
                            section = ParamSection.Keyword;
                            break;

                        case PositionalVariadicParameterView args:
                            section = ParamSection.Keyword;
                            positionalVariadic = args;
                            break;

                        case KeywordVariadicParameterView kwargs:
                            section = ParamSection.AfterKeywordVariadic;
                            keywordVariadic = kwargs;
                            break;

                        case PositionalOnlyMarkerView:
                            throw new SyntaxErrorException("positional-only marker cannot be used twice");

                        default:
                            throw new UnreachableException();
                    }

                    break;
                }

                case ParamSection.Keyword:
                {
                    switch (param)
                    {
                        case OrdinalParameterView ordinal:
                            keywordParams.Add(ordinal);
                            break;

                        case ParameterDefaultView def:
                            keywordParams.Add(def);
                            break;

                        case KeywordVariadicParameterView kwargs:
                            if (wantParam)
                                throw new SyntaxErrorException("at least one parameter must follow bare '*'");

                            section = ParamSection.AfterKeywordVariadic;
                            break;

                        case PositionalVariadicParameterView:
                        case KeywordOnlyMarkerView:
                            throw new SyntaxErrorException("starred parameter cannot be used twice");

                        case PositionalOnlyMarkerView:
                            throw new SyntaxErrorException("positional-only marker cannot be used twice");

                        default:
                            throw new UnreachableException();
                    }

                    wantParam = false;
                    break;
                }

                case ParamSection.AfterKeywordVariadic:
                {
                    throw new SyntaxErrorException("parameters cannot follow after variadic keyword parameter");
                }
            }
        }

        if (wantParam)
        {
            throw new SyntaxErrorException("at least one parameter must follow bare '*'");
        }
    }

    private enum ParamSection
    {
        Positional,
        Free,
        Keyword,
        AfterKeywordVariadic,
    }
}
