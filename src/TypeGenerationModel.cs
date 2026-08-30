// Licensed under the Apache-2.0 License
// https://github.com/sator-imaging/ZeroSerializer

using Microsoft.CodeAnalysis;
using System.Collections.Generic;

#pragma warning disable CS1591  // Missing XML comment for publicly visible type or member

namespace ZeroSerializer.Generator;

internal sealed class TypeGenerationModel
{
    internal TypeGenerationModel(
        INamedTypeSymbol symbol,
        string qualifiedSourceTypeName,
        string viewTypeName,
        bool isEffectivelyPublic,
        bool emitShapeTag,
        bool isBlittableStruct,
        int blittableStructByteCount)
    {
        Symbol = symbol;
        QualifiedSourceTypeName = qualifiedSourceTypeName;
        ViewTypeName = viewTypeName;
        IsEffectivelyPublic = isEffectivelyPublic;
        EmitShapeTag = emitShapeTag;
        IsBlittableStruct = isBlittableStruct;
        BlittableStructByteCount = blittableStructByteCount;
    }

    internal INamedTypeSymbol Symbol { get; }

    internal string QualifiedSourceTypeName { get; }

    internal string ViewTypeName { get; }

    internal bool IsEffectivelyPublic { get; }

    internal bool EmitShapeTag { get; }

    internal bool IsBlittableStruct { get; }

    internal int BlittableStructByteCount { get; }

    internal List<PropertyGenerationModel> Properties { get; } = new();

    internal bool IsValid { get; set; } = true;
}
