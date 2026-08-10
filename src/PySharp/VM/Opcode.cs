namespace PySharp.VM;

internal enum Opcode : byte
{
    // Register-to-register opcodes

    /// <summary>
    /// <b>Add</b>s object from <see cref="Instruction.RegSrc1"/> to object from <see cref="Instruction.RegSrc2"/>
    /// and stores result in <see cref="Instruction.RegDest"/>
    /// </summary>
    Add,

    /// <summary>
    /// <b>Sub</b>tracts from object <see cref="Instruction.RegSrc1"/> object from <see cref="Instruction.RegSrc2"/>
    /// and stores result in <see cref="Instruction.RegDest"/>
    /// </summary>
    Sub,

    /// <summary>
    /// <b>Mul</b>tiplies object from <see cref="Instruction.RegSrc1"/> by object from <see cref="Instruction.RegSrc2"/>
    /// and stores result in <see cref="Instruction.RegDest"/>
    /// </summary>
    Mul,

    /// <summary>
    /// <b>T</b>rue-<b>div</b>ides object from <see cref="Instruction.RegSrc1"/> by object from <see cref="Instruction.RegSrc2"/>
    /// and stores result in <see cref="Instruction.RegDest"/>
    /// </summary>
    TDiv,

    /// <summary>
    /// <b>Eq</b>uates object from <see cref="Instruction.RegSrc1"/> to object from <see cref="Instruction.RegSrc2"/>
    /// and stores result in <see cref="Instruction.RegDest"/>
    /// </summary>
    Eq,

    /// <summary>
    /// <b>N</b>egatively <b>eq</b>uates object from <see cref="Instruction.RegSrc1"/> to object from <see cref="Instruction.RegSrc2"/>
    /// and stores result in <see cref="Instruction.RegDest"/>
    /// </summary>
    NEq,

    // Control flow opcodes
    Call = 64,
    Ret,
    LJmp,

    // Memory opcodes
    Move = 128,
    LdConst,
    LdVar,
    StVar,
}
