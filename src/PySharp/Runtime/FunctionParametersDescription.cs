using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using PySharp.Runtime.Objects;

namespace PySharp.Runtime;

// TODO: add tests somehow
public record FunctionParametersDescription
{
    /// <summary>
    /// Parameters that can be set only using positional arguments.
    /// In Py# this is a parameters declared before slash <c>/</c> symbol in function signature.
    /// </summary>
    public ImmutableArray<FunctionParameter> PositionalOnlyParams { get; init; } = ImmutableArray<FunctionParameter>.Empty;

    /// <summary>
    /// Parameters that can be set using both positional and keyword arguments.
    /// In Py# this is a parameters declared before <c>*</c> symbol or variadic argument and, if there are,
    /// after slash symbol in function signature.
    /// </summary>
    public ImmutableArray<FunctionParameter> FreeParams { get; init; } = ImmutableArray<FunctionParameter>.Empty;

    /// <summary>
    /// Parameter that accepts all non-specified in signature positional arguments.
    /// In Py# this is a parameter declared with star <c>*</c> symbol.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> if there are no variadic parameter.
    /// </remarks>
    public FunctionParameter? VariadicPositionalParam { get; init; }

    /// <summary>
    /// Parameters that can be set only using keywords arguments.
    /// In Py# this is a parameters declared after star <c>*</c> symbol or variadic argument.
    /// </summary>
    public ImmutableArray<FunctionParameter> KeywordOnlyParams { get; init; } = ImmutableArray<FunctionParameter>.Empty;

    /// <summary>
    /// Parameter that accepts all non-specified in signature keyword arguments.
    /// In Py# this is a parameter declared with double star <c>**</c> symbol.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> if there are no variadic keyword parameter.
    /// </remarks>
    public FunctionParameter? VariadicKeywordParam { get; init; }

    public bool HasPositionalVariadic => VariadicPositionalParam != null;

    public bool HasKeywordVariadic => VariadicKeywordParam != null;

    public bool IsArgumentsAreValid(PsTuple args, PsDict kwargs, [NotNullWhen(false)] out string? message)
    {
        Span<bool> allUsedParams = stackalloc bool[allParamsCount];
        allUsedParams.Clear();
        Span<bool> usedPosParams = allUsedParams[..PositionalOnlyParams.Length];
        Span<bool> usedFreeParams = allUsedParams[PositionalOnlyParams.Length..FreeParams.Length];
        Span<bool> usedKwParams = allUsedParams[FreeParams.Length..];

        int minPositionalArgs = PositionalOnlyParams.Count(p => !p.Required);
        int maxPositionalArgs = PositionalOnlyParams.Length + FreeParams.Length;

        // Check positional arguments
        if (args.Count < minPositionalArgs || args.Count > maxPositionalArgs && !HasPositionalVariadic)
        {
            string rangeString = getRange(minPositionalArgs, minPositionalArgs);
            message = $"Function {{0}} takes {rangeString}, but {args.Count} were given";
            return false;
        }

        // Mark used as positional
        for (int argsCounter = minPositionalArgs; argsCounter < args.Count; argsCounter++)
        {
            if (argsCounter < usedPosParams.Length)
            {
                usedPosParams[argsCounter] = true;
                continue;
            }
            else if (argsCounter < usedFreeParams.Length + usedPosParams.Length)
            {
                usedFreeParams[argsCounter - usedPosParams.Length] = true;
                continue;
            }

            Debug.Assert(HasPositionalVariadic);
        }

        // Mark used as keywords
        foreach (var key in kwargs.Keys)
        {
            string keyStr = ((PsString)key).Value;
            int index = FreeParams.IndexOf(new FunctionParameter(keyStr, false), new ParameterNameEqualityComparer());
            if (index == -1)
            {
                index = KeywordOnlyParams.IndexOf(new FunctionParameter(keyStr, false), new ParameterNameEqualityComparer());

                if (index == -1 && !HasKeywordVariadic)
                {
                    message = $"Function {{0}} does not take keyword argument '{keyStr}'";
                    return false;
                }

                if (usedKwParams[index])
                {
                    message = getUsedTwice(keyStr);
                    return false;
                }

                usedKwParams[index] = true;
            }
            else
            {
                if (usedFreeParams[index])
                {
                    message = getUsedTwice(keyStr);
                    return false;
                }

                usedFreeParams[index] = true;
            }
        }

        // Collect all unset required parameters
        int unusedRequiredCount = 0;
        Span<ParameterDescriptor> unusedRequiredParameters = stackalloc ParameterDescriptor[allUsedParams.Length];
        for (int paramIndex = 0; paramIndex < allUsedParams.Length; paramIndex++)
        {
            if (!allUsedParams[paramIndex])
            {
                FunctionParameter param;
                ParameterKind kind;
                int localIndex;
                if (paramIndex < usedPosParams.Length)
                {
                    localIndex = paramIndex;
                    kind = ParameterKind.Positional;
                    param = PositionalOnlyParams[localIndex];
                }
                else if (paramIndex < usedPosParams.Length + usedFreeParams.Length)
                {
                    localIndex = paramIndex - usedPosParams.Length;
                    kind = ParameterKind.Free;
                    param = FreeParams[localIndex];
                }
                else
                {
                    localIndex = paramIndex - usedPosParams.Length - usedFreeParams.Length;
                    kind = ParameterKind.Keyword;
                    param = KeywordOnlyParams[localIndex];
                }

                if (param.Required)
                {
                    unusedRequiredParameters[unusedRequiredCount++] = new ParameterDescriptor(localIndex, kind);
                }
            }
        }

        // Render unset required params
        switch (unusedRequiredCount)
        {
            case 0:
                message = null;
                return true;
            case 1:
                string paramName = unusedRequiredParameters[0].GetParamName(this);
                message = $"Function {{0}} requires argument '{paramName}'";
                return false;

            case 2:
                string param1Name = unusedRequiredParameters[0].GetParamName(this);
                string param2Name = unusedRequiredParameters[1].GetParamName(this);
                message = $"Function {{0}} requires arguments '{param1Name}' and '{param2Name}'";
                return false;

            default:
                var builder = new StringBuilder("Function {0} requires arguments ");
                for (int i = 0; i < unusedRequiredCount - 1; i++)
                {
                    string name = unusedRequiredParameters[i].GetParamName(this);
                    builder.Append('\'');
                    builder.Append(name);
                    builder.Append("', ");
                }
                string lastName = unusedRequiredParameters[unusedRequiredCount - 1].GetParamName(this);
                builder.Append('\'');
                builder.Append(lastName);
                builder.Append("', ");

                message = builder.ToString();
                return false;
        }

        static string getUsedTwice(ReadOnlySpan<char> name) => $"Argument '{name}' used twice in when called {{0}}";
        static string getRange(int min, int max)
            => min == max
            ? $"{max} positional arguments"
            : $"from {min} to {max} positional arguments";
    }

    private int allParamsCount => PositionalOnlyParams.Length + FreeParams.Length + KeywordOnlyParams.Length;
}

file readonly struct ParameterNameEqualityComparer : IEqualityComparer<FunctionParameter>
{
    public bool Equals(FunctionParameter x, FunctionParameter y) => x.Name == y.Name;

    public int GetHashCode([DisallowNull] FunctionParameter obj) => obj.Name.GetHashCode();
}

file readonly record struct ParameterDescriptor(int Index, ParameterKind Kind)
{
    public string GetParamName(FunctionParametersDescription parameters) => Kind switch
    {
        ParameterKind.Positional => parameters.PositionalOnlyParams[Index].Name,
        ParameterKind.Free => parameters.FreeParams[Index].Name,
        ParameterKind.Keyword => parameters.KeywordOnlyParams[Index].Name,
        _ => throw new InvalidOperationException(),
    };
}

file enum ParameterKind
{
    Positional,
    Free,
    Keyword,
}
