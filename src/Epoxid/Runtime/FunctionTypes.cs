using Epoxid.Runtime.Objects;

namespace Epoxid.Runtime;

public delegate EpObject UnaryFunction(EpObject self);

public delegate EpObject BinaryFunction(EpObject self, EpObject other);

public delegate EpObject TernaryFunction(EpObject self, EpObject args, EpObject kwargs);

public delegate EpObject FrameCallFunction(ReadOnlySpan<EpObject> args);

public delegate EpObject FrameKeywordCallFunction(ReadOnlySpan<EpObject> args, EpDict kwargs);
