// Licensed under the Apache-2.0 License
// https://github.com/sator-imaging/ZeroSerializer

using Microsoft.CodeAnalysis;

#pragma warning disable CS1591  // Missing XML comment for publicly visible type or member

namespace ZeroSerializer.Generator;

internal sealed class PropertyGenerationModel
{
    internal PropertyGenerationModel(
        IPropertySymbol symbol,
        PropertySerializationKind kind,
        int elementByteCount,
        ITypeSymbol? arrayElementType,
        INamedTypeSymbol? nestedSerializableType,
        ITypeSymbol? nullableUnderlyingType = null,
        bool isNullableType = false)
    {
        Symbol = symbol;
        Kind = kind;
        ElementByteCount = elementByteCount;
        ArrayElementType = arrayElementType;
        NestedSerializableType = nestedSerializableType;
        NullableUnderlyingType = nullableUnderlyingType;
        IsNullableType = isNullableType;
    }

    internal IPropertySymbol Symbol { get; }

    internal PropertySerializationKind Kind { get; }

    internal int ElementByteCount { get; }

    internal int BlittableByteOffset { get; set; }

    internal ITypeSymbol? ArrayElementType { get; }

    internal INamedTypeSymbol? NestedSerializableType { get; }

    internal ITypeSymbol? NullableUnderlyingType { get; }

    internal bool IsNullableType { get; }
}
