using PySharp.Runtime;

namespace PySharp.SyntaxAnalysis;

public partial class ParameterView
{
    public FunctionParameter AsFunctionParameter() => this switch
    {
        OrdinalParameterView ord => new FunctionParameter(ord.Param.Name.RawString, true),

        ParameterDefaultView def => new FunctionParameter(def.Param.Name.RawString, false),

        PositionalVariadicParameterView args => new FunctionParameter(args.Param.Name.RawString, false),

        KeywordVariadicParameterView kwargs => new FunctionParameter(kwargs.Param.Name.RawString, false),

        KeywordOnlyMarkerView => throw new InvalidOperationException("Cannot create description for marker-parameter"),
        PositionalOnlyMarkerView => throw new InvalidOperationException("Cannot create description for marker-parameter"),
        _ => throw new InvalidOperationException(),
    };
}
