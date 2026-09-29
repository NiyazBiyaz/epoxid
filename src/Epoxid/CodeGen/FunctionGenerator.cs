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

    public CodeObject GenerateCodeObject(FunctionDefView functionDef, Dictionary<string, Variable> variables)
    {
        var allVariableUsages = functionDef.Block
            .GetStatements()
            .GetTopLevelStatements()
            .SelectMany(stmt => stmt.Children())
            .OfType<NameAtomView>();

        var functionParameters = functionDef.Parameters;

        if (functionParameters != null)
        {
            fillParameters(functionParameters.Parameters, variables);
        }

        var generator = new BlockGenerator(functionDef.Block, variables, true);
        var builder = new CodeBuilder();

        generator.GenerateFunction(builder);

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

    private void fillParameters(ImmutableArray<ParameterView> parameters, Dictionary<string, Variable> variables)
    {
        ParamSection section;
        if (parameters.Any(p => p is PositionalOnlyMarkerView))
            section = ParamSection.Positional;
        else
            section = ParamSection.Free;

        int paramsCount = 0;

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

                            addParam(variables, ref paramsCount, ordinal.Param.Name.RawString);
                            positionalParams.Add(ordinal);
                            break;

                        case ParameterDefaultView def:
                            wasDefault = true;
                            addParam(variables, ref paramsCount, def.Param.Name.RawString);
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

                            addParam(variables, ref paramsCount, ordinal.Param.Name.RawString);
                            freeParams.Add(ordinal);
                            break;

                        case ParameterDefaultView def:
                            wasDefault = true;
                            addParam(variables, ref paramsCount, def.Param.Name.RawString);
                            freeParams.Add(def);
                            break;

                        case KeywordOnlyMarkerView:
                            wantParam = true;
                            section = ParamSection.Keyword;
                            break;

                        case PositionalVariadicParameterView args:
                            section = ParamSection.Keyword;
                            addParam(variables, ref paramsCount, args.Param.Name.RawString);
                            positionalVariadic = args;
                            break;

                        case KeywordVariadicParameterView kwargs:
                            section = ParamSection.AfterKeywordVariadic;
                            addParam(variables, ref paramsCount, kwargs.Param.Name.RawString);
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
                            addParam(variables, ref paramsCount, ordinal.Param.Name.RawString);
                            keywordParams.Add(ordinal);
                            break;

                        case ParameterDefaultView def:
                            addParam(variables, ref paramsCount, def.Param.Name.RawString);
                            keywordParams.Add(def);
                            break;

                        case KeywordVariadicParameterView kwargs:
                            if (wantParam)
                                throw new SyntaxErrorException("at least one parameter must follow bare '*'");

                            section = ParamSection.AfterKeywordVariadic;
                            addParam(variables, ref paramsCount, kwargs.Param.Name.RawString);
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

        static void addParam(Dictionary<string, Variable> variables, ref int paramsCount, string name)
        {
            var variable = new Variable()
            {
                Name = name,
                Kind = VariableKind.Local,
                ParameterPosition = paramsCount++,
            };
            variables[name] = variable;
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
