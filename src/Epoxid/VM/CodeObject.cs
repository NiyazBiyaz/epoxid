using System.Collections.Immutable;
using System.Text;
using Epoxid.Runtime.Objects;

namespace Epoxid.VM;

internal class CodeObject
{
    public required ImmutableArray<Instruction> Instructions { get; init; }
    public required ImmutableArray<EpObject> Constants { get; init; }
    public required ImmutableArray<string> VarNames { get; init; }
    public required int StackSize { get; init; }

    public override string ToString()
    {
        const char indent = '\t';
        const char new_line = '\n';
        const char delimiter = ' ';
        const string label_prefix = "L";

        var labels = new Dictionary<int, string>();
        var branches = Instructions
            .Index()
            .Where(instr => instr.Item.Opcode.IsBranch);

        int labelsCount = 1;
        foreach (var branch in branches)
        {
            int targetInstructionIndex = branch.Index + branch.Item.Immediate16;
            if (labels.TryAdd(targetInstructionIndex, label_prefix + labelsCount))
            {
                labelsCount++;
            }
        }

        var sb = new StringBuilder();

        foreach (var (instructionIndex, instr) in Instructions.Index())
        {
            if (labels.TryGetValue(instructionIndex, out string? labelName))
            {
                sb.Append(labelName);
                sb.Append(':');
            }
            sb.Append(indent);

            sb.Append(instr.Opcode.ToString());
            sb.Append(indent);
            sb.Append(indent);

            switch (instr.Opcode)
            {
                case Opcode.Brc:
                    sb.Append(instr.Immediate16);
                    sb.Append(indent);
                    sb.Append(indent);
                    sb.Append(labels[instructionIndex + instr.Immediate16]);
                    break;

                case Opcode.BrTr or Opcode.BrFl:
                    sb.Append(formatRegister(instr.RegDest));
                    sb.Append(delimiter);
                    sb.Append(instr.Immediate16);
                    sb.Append(indent);
                    sb.Append(indent);
                    sb.Append(labels[instructionIndex + instr.Immediate16]);
                    break;

                case Opcode.LdConst:
                    sb.Append(formatRegister(instr.RegDest));
                    sb.Append(indent);
                    sb.Append(indent);
                    sb.Append('`');
                    sb.Append(Constants[instr.Immediate16]);
                    sb.Append('`');
                    break;

                case Opcode.LdVar:
                    sb.Append(formatRegister(instr.RegDest));
                    sb.Append(indent);
                    sb.Append(indent);
                    sb.Append('"');
                    sb.Append(VarNames[instr.Immediate16]);
                    sb.Append('"');
                    break;

                case Opcode.Move:
                    sb.Append(formatRegister(instr.RegDest));
                    sb.Append(delimiter);
                    sb.Append(formatRegister(instr.RegSrc1));
                    break;

                case Opcode.Call or Opcode.CallK:
                    sb.Append(formatRegister(instr.RegSrc1));
                    sb.Append(delimiter);
                    sb.Append(formatRegister(instr.RegDest));
                    sb.Append(delimiter);
                    sb.Append(instr.RegSrc2);

                    sb.Append(indent);
                    sb.Append(indent);
                    sb.Append("Arg count: ");
                    sb.Append(instr.RegSrc2 + (instr.Opcode == Opcode.CallK ? 1 : 0));
                    break;

                case Opcode.Ret:
                    sb.Append(formatRegister(instr.RegSrc1));
                    break;

                case Opcode.RetC:
                    sb.Append(instr.Immediate16);
                    sb.Append(indent);
                    sb.Append(indent);
                    sb.Append(Constants[instr.Immediate16]);
                    break;

                default:
                    if (instr.Opcode.IsRegisterToRegister)
                    {
                        sb.Append(formatRegister(instr.RegDest));
                        sb.Append(delimiter);
                        sb.Append(formatRegister(instr.RegSrc1));
                        sb.Append(delimiter);
                        sb.Append(formatRegister(instr.RegSrc2));
                    }
                    break;
            }

            sb.Append(new_line);
        }

        return sb.ToString();

        static string formatRegister(int register) => $"r{register}";
    }
}
