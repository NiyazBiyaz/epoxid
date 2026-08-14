using System.Collections.Immutable;
using System.Diagnostics;
using PySharp.Runtime;

namespace PySharp.SyntaxAnalysis;

internal class FunctionValidator
{
    public readonly CodeBlockGenerator BlockValidator;

    private readonly List<string> variables = [];

    public FunctionValidator(FunctionDefRawView function)
    {
        // TODO: Other function stuff
        BlockValidator = new(function.Block.GetStatements())
        {
            Variables = variables, // Share variables with the block validator.
        };
        functionParameters = function.Parameters?.Parameters ?? [];
    }

    private readonly IEnumerable<ParameterView> functionParameters;

    private readonly List<ParameterView> positionalParams = [];
    private readonly List<ParameterView> freeParams = [];
    private readonly List<ParameterView> keywordParams = [];
    private PositionalVariadicParameterView? positionalVariadic;
    private KeywordVariadicParameterView? keywordVariadic;

    public ValidationResult ValidateParameters()
    {
        Debug.Assert(functionParameters != null, "Call only when code block is a function.");

        if (!functionParameters.Any())
            return ValidationResult.ResultSuccess;

        ParamSection section;
        if (functionParameters.Any(p => p is PositionalOnlyMarkerView))
            section = ParamSection.Positional;
        else
            section = ParamSection.Free;

        bool wasDefault = false;
        bool wantParam = section is ParamSection.Positional;

        foreach (var param in functionParameters)
        {
            switch (section)
            {
                case ParamSection.Positional:
                {
                    switch (param)
                    {
                        case OrdinalParameterView ordinal:
                            if (wasDefault)
                                return ValidationResult.ErrorDefaultOrder;

                            variables.Add(ordinal.Param.Name.RawString);
                            positionalParams.Add(ordinal);
                            break;

                        case ParameterDefaultView def:
                            wasDefault = true;
                            variables.Add(def.Param.Name.RawString);
                            positionalParams.Add(def);
                            break;

                        case PositionalOnlyMarkerView:
                            if (wantParam)
                                return new ValidationResult.Error("at least one parameter must precede positional-only marker");

                            section = ParamSection.Free;
                            break;

                        case KeywordOnlyMarkerView:
                        case KeywordVariadicParameterView:
                        case PositionalVariadicParameterView:
                            return new ValidationResult.Error("invalid positional-only parameter");

                        default:
                            throw new InvalidOperationException();
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
                                return ValidationResult.ErrorDefaultOrder;

                            variables.Add(ordinal.Param.Name.RawString);
                            freeParams.Add(ordinal);
                            break;

                        case ParameterDefaultView def:
                            wasDefault = true;
                            variables.Add(def.Param.Name.RawString);
                            freeParams.Add(def);
                            break;

                        case KeywordOnlyMarkerView:
                            wantParam = true;
                            section = ParamSection.Keyword;
                            break;

                        case PositionalVariadicParameterView args:
                            section = ParamSection.Keyword;
                            variables.Add(args.Param.Name.RawString);
                            positionalVariadic = args;
                            break;

                        case KeywordVariadicParameterView kwargs:
                            section = ParamSection.AfterKeywordVariadic;
                            variables.Add(kwargs.Param.Name.RawString);
                            keywordVariadic = kwargs;
                            break;

                        case PositionalOnlyMarkerView:
                            return ValidationResult.ErrorInvalidSlash;

                        default:
                            throw new InvalidOperationException();
                    }

                    break;
                }

                case ParamSection.Keyword:
                {
                    switch (param)
                    {
                        case OrdinalParameterView ordinal:
                            variables.Add(ordinal.Param.Name.RawString);
                            keywordParams.Add(ordinal);
                            break;

                        case ParameterDefaultView def:
                            variables.Add(def.Param.Name.RawString);
                            keywordParams.Add(def);
                            break;

                        case KeywordVariadicParameterView kwargs:
                            if (wantParam)
                                return ValidationResult.ErrorNeedParamAfterStar;
                            section = ParamSection.AfterKeywordVariadic;
                            variables.Add(kwargs.Param.Name.RawString);
                            break;

                        case PositionalVariadicParameterView:
                        case KeywordOnlyMarkerView:
                            return new ValidationResult.Error("starred parameter cannot be used twice");

                        case PositionalOnlyMarkerView:
                            return ValidationResult.ErrorInvalidSlash;

                        default:
                            throw new InvalidOperationException();
                    }

                    wantParam = false;
                    break;
                }

                case ParamSection.AfterKeywordVariadic:
                {
                    return new ValidationResult.Error("parameters cannot follow after variadic keyword");
                }
            }
        }

        if (wantParam)
        {
            return ValidationResult.ErrorNeedParamAfterStar;
        }

        return ValidationResult.ResultSuccess;
    }

    public FunctionParametersDescription GetParametersDescription() => new()
    {
        PositionalOnlyParams = positionalParams.Select(p => p.AsFunctionParameter()).ToImmutableArray(),

        FreeParams = freeParams.Select(p => p.AsFunctionParameter()).ToImmutableArray(),

        KeywordOnlyParams = keywordParams.Select(p => p.AsFunctionParameter()).ToImmutableArray(),

        VariadicPositionalParam = positionalVariadic?.AsFunctionParameter(),

        VariadicKeywordParam = keywordVariadic?.AsFunctionParameter(),
    };

    private enum ParamSection
    {
        Positional,
        Free,
        Keyword,
        AfterKeywordVariadic,
    }
}
