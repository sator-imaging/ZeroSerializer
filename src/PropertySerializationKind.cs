// Licensed under the Apache-2.0 License
// https://github.com/sator-imaging/ZeroSerializer

#pragma warning disable CS1591  // Missing XML comment for publicly visible type or member

namespace ZeroSerializer.Generator;

internal enum PropertySerializationKind
{
    Primitive,
    String,
    BlittableStruct,
    Array,
    Nested,
    InvalidArray,
}
