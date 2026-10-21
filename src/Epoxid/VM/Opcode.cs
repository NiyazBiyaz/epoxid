namespace Epoxid.VM;

internal enum Opcode : byte
{
    // Binary operations
    Add,
    Sub,
    Mul,
    TDiv,
    Mod,
    Eq,
    NEq,
    LsTh,
    GrTh,
    InAdd,
    InSub,
    InMul,
    InTDiv,
    InMod,

    // Special operations
    Call,
    Ret,
    BrcForw,
    BrcBack,
    BrPopTr,
    BrPopFl,
    BrPeekTr,
    BrPeekFl,
    BindFun,
    Pop,

    // Load/store operations
    LdConst,
    LdArg,
    LdVar,
    StVar,
    LdLocal,
    StLocal,
    LdCell,
    StCell,

    // Class operations
    GAttr,
    SAttr,

    // Extended arguments
    Ext1,
    Ext2,
    Ext3,
}

internal static class OpcodeExtensions
{
    extension(Opcode opcode)
    {
        public bool IsCanBeLast => opcode is Opcode.Ret;
    }
}
