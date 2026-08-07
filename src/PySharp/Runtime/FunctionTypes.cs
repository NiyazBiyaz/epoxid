using PySharp.Runtime.Objects;

namespace PySharp.Runtime;

public delegate PsObject UnaryFunction(PsObject self);

public delegate PsObject BinaryFunction(PsObject self, PsObject other);

public delegate PsObject TernaryFunction(PsObject self, PsObject args, PsObject kwargs);
