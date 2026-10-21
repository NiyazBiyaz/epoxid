using System.Diagnostics;
using System.Text;
using Epoxid.VM;

namespace Epoxid.CodeGen;

[DebuggerDisplay("{getDebuggerDisplay(),nq}")]
internal record IntermediateInstruction
{
    public Opcode Opcode { get; init; }
    public bool IsLabel { get; init; }
    public IntermediateInstruction? JumpTarget { get; init; }
    public int ArgumentValue { get; init; }

    public int? Index { get; set; } = null;

    public int? JumpDelta => Opcode switch
    {
        Opcode.BrcBack => Index - JumpTarget?.Index,
        _ => JumpTarget?.Index - Index,
    };

    public int StackDelta => Opcode switch
    {
        Opcode.Add or
        Opcode.Sub or
        Opcode.Mul or
        Opcode.TDiv or
        Opcode.Mod or
        Opcode.Eq or
        Opcode.NEq or
        Opcode.LsTh or
        Opcode.GrTh or
        Opcode.InAdd or
        Opcode.InSub or
        Opcode.InMul or
        Opcode.InTDiv or
        Opcode.InMod => -1,

        // ArgumentValue = call arguments count
        // StackDelta = 1 (return) - 1 (function) - ArgCount = -ArgCount
        Opcode.Call => -ArgumentValue,

        Opcode.Ret => -1,

        Opcode.BrcForw or Opcode.BrcBack => 0,
        Opcode.BrPopTr or Opcode.BrPopFl => -1,
        Opcode.BrPeekTr or Opcode.BrPeekFl => 0,
        Opcode.BindFun => 0,
        Opcode.Pop => -ArgumentValue,

        Opcode.LdConst or
        Opcode.LdArg or
        Opcode.LdVar or
        Opcode.LdLocal or
        Opcode.LdCell => +1,

        Opcode.StVar or Opcode.StCell or Opcode.StLocal => -1,

        Opcode.GAttr => 0,
        Opcode.SAttr => -1,

        Opcode.Ext1 or Opcode.Ext2 or Opcode.Ext3 => 0,

        _ => throw new UnreachableException($"Unknown Opcode value: {Opcode}"),
    };

    private string getDebuggerDisplay()
    {
        var sb = new StringBuilder();
        if (Index is int index)
        {
            sb.Append(index);
            sb.Append(": ");
        }

        if (IsLabel)
        {
            sb.Append("Label");
        }
        else
        {
            sb.Append(Opcode.ToString());
            sb.Append('(');
            sb.Append(ArgumentValue);
            sb.Append(')');
        }

        return sb.ToString();
    }
}
