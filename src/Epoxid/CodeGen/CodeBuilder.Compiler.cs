using System.Collections.Immutable;
using System.Diagnostics;
using Epoxid.VM;

namespace Epoxid.CodeGen;

internal partial class CodeBuilder
{
    public CodeObject Compile()
    {
        fillIndexes();

        List<IntermediateInstruction> afterArgumentRepackingPass = [];
        foreach (var instr in instructions)
        {
            if (instr.IsLabel)
                continue;

            int argument = instr.JumpDelta ?? instr.ArgumentValue;

            byte mainArg = (byte)(argument & byte.MaxValue);
            byte ext1Arg = (byte)(argument >> 8 & byte.MaxValue);
            byte ext2Arg = (byte)(argument >> 16 & byte.MaxValue);
            byte ext3Arg = (byte)(argument >> 24 & byte.MaxValue);

            if (ext3Arg != 0)
            {
                afterArgumentRepackingPass.Add(new()
                {
                    Opcode = Opcode.Ext3,
                    ArgumentValue = ext3Arg,
                });
            }
            if (ext2Arg != 0)
            {
                afterArgumentRepackingPass.Add(new()
                {
                    Opcode = Opcode.Ext2,
                    ArgumentValue = ext2Arg,
                });
            }
            if (ext1Arg != 0)
            {
                afterArgumentRepackingPass.Add(new()
                {
                    Opcode = Opcode.Ext1,
                    ArgumentValue = ext1Arg,
                });
            }
            afterArgumentRepackingPass.Add(new()
            {
                Opcode = instr.Opcode,
                ArgumentValue = mainArg,
            });
        }

        int stackSize = 0;
        var instructionBuilder = ImmutableArray.CreateBuilder<Instruction>();
        foreach (var instr in afterArgumentRepackingPass)
        {
            if (instr.IsLabel)
                continue;

            stackSize += instr.StackDelta;

            Debug.Assert(stackSize >= 0);

            var result = new Instruction(instr.Opcode, (byte)instr.ArgumentValue);
            instructionBuilder.Add(result);
        }

        return new()
        {
            Constants = [.. constants],
            VarNames = [.. variableNames],
            Instructions = instructionBuilder.ToImmutable(),
        };
    }

    private void fillIndexes()
    {
        // This method of indexing jump labels introduces one thing that wasn't expected before:
        //     the actual jump address is one less than the instruction that should be
        //     executed immediately after the jump. In other words:
        //     0:    Add
        //     1:    BrTr (2) ----------------+----+
        //     2:    BindFun                  |    |
        //     3:    LdConst  <--jumps here---+    |
        //     3:      Actual label:               |
        //     4:    Sub      <--points here-------+
        //
        //      BrTr has index 1. Label has index 3, so jump delta is 2. So after adding this delta program
        //      counter points to the LdConst with the same as label index 3. But after auto-incrementing PC
        //      it will point to Sub with index 4 so everything is fine.
        //
        // This is somewhat strange in terms of what a jump actually represents, but
        // within the interpreter engine, it allows us to avoid handling the lack of
        // auto-increment after jumps. More precisely, we would need to add it for jump
        // instructions, but since all other instructions already have it, we're
        // essentially just eliminating the need to handle this exception.
        //
        // That's why this isn't handled in Engine.cs, so it can effectively be
        // considered part of the API now. So do not touch :P
        int indexer = 0;
        foreach (var instr in instructions)
        {
            if (!instr.IsLabel)
                indexer++;
            instr.Index = indexer;
        }
    }
}
