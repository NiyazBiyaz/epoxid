using PySharp.Runtime.Objects;

namespace PySharp.VM;

internal enum Opcode : byte
{
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

    /// <summary>
    /// <b>Call</b>s the function that stored in the <see cref="Instruction.RegSrc1"/> with <see cref="Instruction.RegSrc2"/>
    /// number of positional arguments starting from register <see cref="Instruction.RegDest"/> and stores returned
    /// value in the <see cref="Instruction.RegDest"/>
    /// </summary>
    Call = 64,

    /// <summary>
    /// <b>Call</b>s the function that stored in the <see cref="Instruction.RegSrc1"/> with
    /// <see cref="Instruction.RegSrc2"/> number of positional arguments and <b>k</b>eyword arguments stored
    /// as <see cref="PsDict"/> in <see cref="Instruction.RegSrc2"/>+1 starting from register
    /// <see cref="Instruction.RegDest"/> and stores returned value in the <see cref="Instruction.RegDest"/>
    /// </summary>
    CallK,

    /// <summary>
    /// <b>Ret</b>urns from the current frame object stored in the <see cref="Instruction.RegSrc1"/>
    /// </summary>
    Ret,

    /// <summary>
    /// <b>Ret</b>urns from the current frame <b>c</b>onstant object stored in the <see cref="CodeObject.Constants"/>
    /// by index <see cref="Instruction.Immediate16"/>
    /// </summary>
    RetC,

    /// <summary>
    /// Unconditionally <b>jump</b>s to the instruction with zero-based index stored in
    /// <see cref="Instruction.Immediate24"/> relatively by <see cref="CodeObject.Instructions"/>
    /// </summary>
    Jump,

    /// <summary>
    /// <b>Move</b>s object from <see cref="Instruction.RegSrc1"/> to <see cref="Instruction.RegDest"/>
    /// </summary>
    Move,

    /// <summary>
    /// <b>L</b>oa<b>d</b>s <b>const</b>ant object from <see cref="CodeObject.Constants"/> with index stored in
    /// <see cref="Instruction.Immediate16"/> to register <see cref="Instruction.RegDest"/>
    /// </summary>
    LdConst,

    /// <summary>
    /// <b>L</b>oa<b>d</b>s <b>var</b>iable from the environment whose name is stored in <see cref="CodeObject.VarNames"/>
    /// with index stored in <see cref="Instruction.Immediate16"/>, to register <see cref="Instruction.RegDest"/>
    /// </summary>
    LdVar,

    /// <summary>
    /// <b>L</b>oa<b>d</b>s argument passed to the frame with index <see cref="Instruction.RegSrc1"/> and stores it
    /// in the <see cref="Instruction.RegDest"/>
    /// </summary>
    LdArg,
}

internal static class OpcodeExtensions
{
    extension(Opcode opcode)
    {
        public bool IsRegisterToRegister => opcode < Opcode.Call;
    }
}
