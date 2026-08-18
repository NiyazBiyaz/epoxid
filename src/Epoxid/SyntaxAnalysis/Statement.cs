namespace Epoxid.SyntaxAnalysis;

public partial interface IStatementView
{
    /// <summary>
    /// What is code block: <seealso href="https://docs.python.org/3/reference/executionmodel.html#structure-of-a-program"/>
    /// </summary>
    public bool IsCodeBlock => this is FunctionDefView or ClassDefView;
}
