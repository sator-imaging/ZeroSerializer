// Licensed under the Apache-2.0 License
// https://github.com/sator-imaging/ZeroSerializer

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

#pragma warning disable CS1591  // Missing XML comment for publicly visible type or member

namespace ZeroSerializer.Generator;

[Generator]
public sealed class ZeroSerializerGenerator : ISourceGenerator
{
    // Serializer identity and generated API namespace are separate concepts even while their current values match.
    private const string SerializerName = "ZeroSerializer";
    private const string SerializerNamespace = "ZeroSerializer";
    private const string SerializerAttributeName = SerializerName + "Attribute";
    private const string SerializerExtensionsName = SerializerName + "Extensions";
    private const string SerializerHelperName = SerializerName + "Helper";
    private const string QualifiedSerializerHelperName = "global::" + SerializerNamespace + "." + SerializerHelperName;

    private static readonly DiagnosticDescriptor UnsupportedSerializableType = new(
        "ZEROS001",
        "Unsupported serializable type",
        "Type '{0}' must be a non-generic, top-level class or struct without a non-object base class",
        SerializerName,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor InaccessibleSerializableField = new(
        "ZEROS002",
        "Inaccessible serializable field",
        "Field '{0}' must not be private or private protected because generated serializers preserve every declared instance field",
        SerializerName,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor UnsupportedSerializableField = new(
        "ZEROS003",
        "Unsupported serializable field",
        "Field '{0}' has unsupported type '{1}'",
        SerializerName,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor InvalidBlittableArrayElement = new(
        "ZEROS004",
        "Invalid blittable array element",
        "Array field '{0}' requires a primitive, enum, or a struct recursively marked with StructLayout(LayoutKind.Sequential, Pack = 1)",
        SerializerName,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor InvalidSerializableDependency = new(
        "ZEROS005",
        "Invalid nested serializable type",
        "Field '{0}' refers to serializable type '{1}', but that type contains errors",
        SerializerName,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor BlittableCompatibleStructMissingLayout = new(
        "ZEROS006",
        "Struct can use Blittable serialization",
        "Struct '{0}' has a Blittable-compatible field shape; use StructLayout(LayoutKind.Sequential, Pack = 1) to enable raw payload serialization",
        SerializerName,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor BlittableCompatibleNestedStruct = new(
        "ZEROS007",
        "Nested struct can use faster Blittable serialization",
        "Nested struct '{0}' can use StructLayout(LayoutKind.Sequential, Pack = 1) to improve serialization performance with raw payload serialization",
        SerializerName,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    public void Initialize(GeneratorInitializationContext initializationContext)
    {
        // Unity is pinned to Roslyn 3.8, so discovery must stay on the classic syntax-receiver API.
        initializationContext.RegisterForSyntaxNotifications(static () => new SerializableTypeSyntaxReceiver());
    }

    public void Execute(GeneratorExecutionContext executionContext)
    {
        // Roslyn 3.8 has no post-initialization hook, so the internal marker attribute is injected here.
        var injectedAttributeSourceBuilder = new GeneratedSourceBuilder();
        AppendGeneratedFileHeader(injectedAttributeSourceBuilder);
        injectedAttributeSourceBuilder.AppendLine($"namespace {SerializerNamespace}");
        injectedAttributeSourceBuilder.OpenBlock();
        injectedAttributeSourceBuilder.AppendLine("[AttributeUsage(");
        injectedAttributeSourceBuilder.AppendLine("    AttributeTargets.Class | AttributeTargets.Struct,");
        injectedAttributeSourceBuilder.AppendLine("    AllowMultiple = false,");
        injectedAttributeSourceBuilder.AppendLine("    Inherited = false)]");
        injectedAttributeSourceBuilder.AppendLine($"internal sealed class {SerializerAttributeName} : Attribute");
        injectedAttributeSourceBuilder.OpenBlock();
        injectedAttributeSourceBuilder.CloseBlock();
        injectedAttributeSourceBuilder.CloseBlock();
        executionContext.AddSource(
            "- " + SerializerAttributeName + ".g.cs",
            SourceText.From(injectedAttributeSourceBuilder.ToString(), Encoding.UTF8));

        // Infrastructure stays in stable, separately injected files instead of being duplicated per serializable type.
        var injectedHelperSourceBuilder = new GeneratedSourceBuilder();
        AppendGeneratedFileHeader(injectedHelperSourceBuilder);
        EmitHelper(injectedHelperSourceBuilder);
        injectedHelperSourceBuilder.CloseBlock();
        executionContext.AddSource(
            "- " + SerializerHelperName + ".g.cs",
            SourceText.From(injectedHelperSourceBuilder.ToString(), Encoding.UTF8));

        if (executionContext.SyntaxReceiver is not SerializableTypeSyntaxReceiver syntaxReceiver)
        {
            return;
        }

        var collectedTypes = ImmutableArray.CreateBuilder<INamedTypeSymbol>();
        foreach (TypeDeclarationSyntax candidateDeclaration in syntaxReceiver.CandidateDeclarations)
        {
            SemanticModel semanticModel = executionContext.Compilation.GetSemanticModel(candidateDeclaration.SyntaxTree);
            if (semanticModel.GetDeclaredSymbol(candidateDeclaration) is not INamedTypeSymbol candidateType)
            {
                continue;
            }

            // The injected attribute is unavailable to this run's semantic model, so Roslyn 3.8 discovery is syntax-based.
            if (HasSerializableAttribute(candidateDeclaration))
            {
                collectedTypes.Add(candidateType);
            }
        }

        ExecuteCore(executionContext, collectedTypes.ToImmutable());
    }

    private static bool HasSerializableAttribute(TypeDeclarationSyntax candidateDeclaration)
    {
        foreach (AttributeListSyntax attributeList in candidateDeclaration.AttributeLists)
        {
            foreach (AttributeSyntax attribute in attributeList.Attributes)
            {
                string attributeName = attribute.Name.ToString();
                if (attributeName == SerializerName
                    || attributeName == SerializerAttributeName
                    || attributeName == SerializerNamespace + "." + SerializerName
                    || attributeName == SerializerNamespace + "." + SerializerAttributeName
                    || attributeName == "global::" + SerializerNamespace + "." + SerializerName
                    || attributeName == "global::" + SerializerNamespace + "." + SerializerAttributeName)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static void ExecuteCore(
        GeneratorExecutionContext executionContext,
        ImmutableArray<INamedTypeSymbol> collectedTypes)
    {
        var uniqueTypes = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
        foreach (INamedTypeSymbol collectedType in collectedTypes)
        {
            uniqueTypes.Add(collectedType);
        }

        var allSerializableTypes = new HashSet<INamedTypeSymbol>(uniqueTypes, SymbolEqualityComparer.Default);
        var generationModels = new Dictionary<INamedTypeSymbol, TypeGenerationModel>(SymbolEqualityComparer.Default);

        foreach (INamedTypeSymbol serializableType in uniqueTypes)
        {
            TypeGenerationModel generationModel = CreateGenerationModel(
                executionContext,
                serializableType,
                allSerializableTypes);
            generationModels.Add(serializableType, generationModel);
        }

        bool invalidDependencyFound;
        do
        {
            invalidDependencyFound = false;
            foreach (TypeGenerationModel generationModel in generationModels.Values)
            {
                if (!generationModel.IsValid)
                {
                    continue;
                }

                FieldGenerationModel? invalidNestedField = null;
                foreach (FieldGenerationModel nestedFieldCandidate in generationModel.Fields)
                {
                    if (nestedFieldCandidate.NestedSerializableType is not null
                        && generationModels.TryGetValue(
                            nestedFieldCandidate.NestedSerializableType,
                            out TypeGenerationModel? nestedModel)
                        && !nestedModel.IsValid)
                    {
                        invalidNestedField = nestedFieldCandidate;
                        break;
                    }
                }
                if (invalidNestedField is null)
                {
                    continue;
                }

                generationModel.IsValid = false;
                invalidDependencyFound = true;
                executionContext.ReportDiagnostic(Diagnostic.Create(
                    InvalidSerializableDependency,
                    invalidNestedField.Symbol.Locations.IsDefaultOrEmpty
                        ? null
                        : invalidNestedField.Symbol.Locations[0],
                    invalidNestedField.Symbol.Name,
                    invalidNestedField.NestedSerializableType!.ToDisplayString()));
            }
        }
        while (invalidDependencyFound);

        var validModels = new List<TypeGenerationModel>();
        foreach (TypeGenerationModel generationModel in generationModels.Values)
        {
            if (generationModel.IsValid)
            {
                validModels.Add(generationModel);
            }
        }
        if (validModels.Count == 0)
        {
            return;
        }

        ReportBlittableCompatibleNestedStructDiagnostics(
            executionContext,
            validModels,
            generationModels);

        var modelLookup = new Dictionary<INamedTypeSymbol, TypeGenerationModel>(SymbolEqualityComparer.Default);
        // Each type owns one generated file and contributes one method to its namespace-local partial extension class.
        foreach (TypeGenerationModel validModel in validModels)
        {
            modelLookup.Add(validModel.Symbol, validModel);
        }

        foreach (TypeGenerationModel validModel in validModels)
        {
            string generatedSource = EmitGeneratedSource(
                validModel,
                modelLookup);
            executionContext.AddSource(
                validModel.Symbol.ToDisplayString() + "." + SerializerName + ".g.cs",
                SourceText.From(generatedSource, Encoding.UTF8));
        }
    }

    private static TypeGenerationModel CreateGenerationModel(
        GeneratorExecutionContext executionContext,
        INamedTypeSymbol serializableType,
        HashSet<INamedTypeSymbol> allSerializableTypes)
    {
        string qualifiedSourceTypeName = serializableType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        int blittableStructByteCount = 0;
        bool isBlittableStruct = serializableType.TypeKind == TypeKind.Struct
            && TryGetFixedTypeByteCount(
                serializableType,
                new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default),
                out blittableStructByteCount);
        var generationModel = new TypeGenerationModel(
            serializableType,
            qualifiedSourceTypeName,
            serializableType.Name + "View",
            IsEffectivelyPublic(serializableType),
            isBlittableStruct,
            isBlittableStruct ? blittableStructByteCount : 0);

        bool hasUnsupportedShape = serializableType.Arity != 0
            || serializableType.ContainingType is not null
            || (serializableType.TypeKind != TypeKind.Class && serializableType.TypeKind != TypeKind.Struct)
            || (serializableType.TypeKind == TypeKind.Class
                && serializableType.BaseType is not null
                && serializableType.BaseType.SpecialType != SpecialType.System_Object);
        if (hasUnsupportedShape)
        {
            generationModel.IsValid = false;
            executionContext.ReportDiagnostic(Diagnostic.Create(
                UnsupportedSerializableType,
                serializableType.Locations.IsDefaultOrEmpty ? null : serializableType.Locations[0],
                serializableType.ToDisplayString()));
            return generationModel;
        }

        if (serializableType.TypeKind == TypeKind.Struct
            && !isBlittableStruct
            && HasBlittableCompatibleFieldShape(serializableType))
        {
            // Report on the declaration identifier so the layout optimization is actionable without highlighting the whole type.
            executionContext.ReportDiagnostic(Diagnostic.Create(
                BlittableCompatibleStructMissingLayout,
                GetTypeIdentifierLocation(serializableType),
                serializableType.Name));
        }

        // Roslyn's member order is the wire declaration order; never infer a different order from file paths or spans.
        foreach (ISymbol declaredMember in serializableType.GetMembers())
        {
            // Only public getter properties define the wire contract; fields, setters, and indexers must never leak into it.
            if (declaredMember is not IPropertySymbol serializableProperty
                || serializableProperty.IsStatic
                || serializableProperty.IsIndexer
                || serializableProperty.DeclaredAccessibility != Accessibility.Public
                || serializableProperty.GetMethod?.DeclaredAccessibility != Accessibility.Public)
            {
                continue;
            }

            FieldGenerationModel? propertyModel = CreatePropertyGenerationModel(serializableProperty, allSerializableTypes);
            if (propertyModel is null)
            {
                generationModel.IsValid = false;
                executionContext.ReportDiagnostic(Diagnostic.Create(
                    UnsupportedSerializableField,
                    serializableProperty.Locations.IsDefaultOrEmpty ? null : serializableProperty.Locations[0],
                    serializableProperty.Name,
                    serializableProperty.Type.ToDisplayString()));
                continue;
            }

            if (propertyModel.Kind == FieldSerializationKind.InvalidArray)
            {
                generationModel.IsValid = false;
                executionContext.ReportDiagnostic(Diagnostic.Create(
                    InvalidBlittableArrayElement,
                    serializableProperty.Locations.IsDefaultOrEmpty ? null : serializableProperty.Locations[0],
                    serializableProperty.Name));
                continue;
            }

            generationModel.Fields.Add(propertyModel);
        }

        return generationModel;
    }

    private static FieldGenerationModel? CreatePropertyGenerationModel(
        IPropertySymbol serializableProperty,
        HashSet<INamedTypeSymbol> allSerializableTypes)
    {
        if (serializableProperty.Type is INamedTypeSymbol nullableType
            && nullableType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
        {
            ITypeSymbol nullableUnderlyingType = nullableType.TypeArguments[0];
            if (TryGetPrimitiveByteCount(nullableUnderlyingType, out int nullablePrimitiveByteCount))
            {
                return new FieldGenerationModel(
                    serializableProperty,
                    FieldSerializationKind.Primitive,
                    nullablePrimitiveByteCount,
                    null,
                    null,
                    nullableUnderlyingType,
                    isNullableType: true);
            }

            if (nullableUnderlyingType.TypeKind == TypeKind.Enum
                && nullableUnderlyingType is INamedTypeSymbol nullableEnumType
                && nullableEnumType.EnumUnderlyingType is not null
                && TryGetPrimitiveByteCount(nullableEnumType.EnumUnderlyingType, out int nullableEnumByteCount))
            {
                return new FieldGenerationModel(
                    serializableProperty,
                    FieldSerializationKind.Primitive,
                    nullableEnumByteCount,
                    null,
                    null,
                    nullableUnderlyingType,
                    isNullableType: true);
            }

            if (TryGetFixedTypeByteCount(
                    nullableUnderlyingType,
                    new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default),
                    out int nullableStructByteCount))
            {
                // Blittable structs stay raw even when annotated, otherwise array Cast would see per-value metadata.
                return new FieldGenerationModel(
                    serializableProperty,
                    FieldSerializationKind.BlittableStruct,
                    nullableStructByteCount,
                    null,
                    null,
                    nullableUnderlyingType,
                    isNullableType: true);
            }

            if (nullableUnderlyingType is INamedTypeSymbol namedNullableUnderlyingType
                && allSerializableTypes.Contains(namedNullableUnderlyingType))
            {
                return new FieldGenerationModel(
                    serializableProperty,
                    FieldSerializationKind.Nested,
                    0,
                    null,
                    namedNullableUnderlyingType,
                    nullableUnderlyingType,
                    isNullableType: true);
            }

            return null;
        }

        if (serializableProperty.Type.SpecialType == SpecialType.System_String)
        {
            return new FieldGenerationModel(serializableProperty, FieldSerializationKind.String, 2, null, null);
        }

        if (serializableProperty.Type is IArrayTypeSymbol arrayType)
        {
            if (!arrayType.IsSZArray || arrayType.Rank != 1)
            {
                return new FieldGenerationModel(serializableProperty, FieldSerializationKind.InvalidArray, 0, null, null);
            }

            if (!TryGetFixedTypeByteCount(arrayType.ElementType, new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default), out int elementByteCount))
            {
                return new FieldGenerationModel(serializableProperty, FieldSerializationKind.InvalidArray, 0, null, null);
            }

            return new FieldGenerationModel(
                serializableProperty,
                FieldSerializationKind.Array,
                elementByteCount,
                arrayType.ElementType,
                null,
                null,
                isNullableType: true);
        }

        if (TryGetPrimitiveByteCount(serializableProperty.Type, out int primitiveByteCount))
        {
            return new FieldGenerationModel(serializableProperty, FieldSerializationKind.Primitive, primitiveByteCount, null, null);
        }

        if (serializableProperty.Type.TypeKind == TypeKind.Enum
            && serializableProperty.Type is INamedTypeSymbol enumType
            && enumType.EnumUnderlyingType is not null
            && TryGetPrimitiveByteCount(enumType.EnumUnderlyingType, out int enumByteCount))
        {
            return new FieldGenerationModel(serializableProperty, FieldSerializationKind.Primitive, enumByteCount, null, null);
        }

        if (TryGetFixedTypeByteCount(serializableProperty.Type, new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default), out int fixedStructByteCount))
        {
            // Blittable structs are copied as one contiguous value and never receive their own offset table.
            return new FieldGenerationModel(serializableProperty, FieldSerializationKind.BlittableStruct, fixedStructByteCount, null, null);
        }

        if (serializableProperty.Type is INamedTypeSymbol namedPropertyType && allSerializableTypes.Contains(namedPropertyType))
        {
            return new FieldGenerationModel(
                serializableProperty,
                FieldSerializationKind.Nested,
                0,
                null,
                namedPropertyType,
                null,
                isNullableType: namedPropertyType.IsReferenceType);
        }

        return null;
    }

    private static bool TryGetFixedTypeByteCount(
        ITypeSymbol candidateType,
        HashSet<ITypeSymbol> typesBeingInspected,
        out int byteCount)
    {
        if (TryGetPrimitiveByteCount(candidateType, out byteCount))
        {
            return true;
        }

        if (candidateType.TypeKind == TypeKind.Enum
            && candidateType is INamedTypeSymbol enumType
            && enumType.EnumUnderlyingType is not null)
        {
            // An enum has exactly the blittable size and wire representation of its underlying integral type.
            return TryGetPrimitiveByteCount(enumType.EnumUnderlyingType, out byteCount);
        }

        if (candidateType is not INamedTypeSymbol structType
            || structType.TypeKind != TypeKind.Struct
            || structType.IsRefLikeType
            || !HasSequentialPackOneLayout(structType)
            || !typesBeingInspected.Add(candidateType))
        {
            byteCount = 0;
            return false;
        }

        int accumulatedByteCount = 0;
        foreach (ISymbol declaredMember in structType.GetMembers())
        {
            if (declaredMember is not IFieldSymbol nestedField || nestedField.IsStatic)
            {
                continue;
            }

            if (!TryGetFixedTypeByteCount(nestedField.Type, typesBeingInspected, out int nestedFieldByteCount))
            {
                typesBeingInspected.Remove(candidateType);
                byteCount = 0;
                return false;
            }

            try
            {
                accumulatedByteCount = checked(accumulatedByteCount + nestedFieldByteCount);
            }
            // Ignore exception: an overflowing field total means the struct cannot have a supported fixed byte count.
            catch (OverflowException)
            {
                typesBeingInspected.Remove(candidateType);
                byteCount = 0;
                return false;
            }
        }

        typesBeingInspected.Remove(candidateType);
        byteCount = accumulatedByteCount;
        return true;
    }

    private static bool HasSequentialPackOneLayout(INamedTypeSymbol structType)
    {
        AttributeData? structLayoutAttribute = null;
        foreach (AttributeData candidateAttribute in structType.GetAttributes())
        {
            if (candidateAttribute.AttributeClass?.ToDisplayString()
                == "System.Runtime.InteropServices.StructLayoutAttribute")
            {
                structLayoutAttribute = candidateAttribute;
                break;
            }
        }
        if (structLayoutAttribute is null
            || structLayoutAttribute.ConstructorArguments.Length != 1
            || structLayoutAttribute.ConstructorArguments[0].Value is not int layoutKind
            || layoutKind != 0)
        {
            return false;
        }

        if (structLayoutAttribute.NamedArguments.Length != 1)
        {
            return false;
        }

        var namedArgument = structLayoutAttribute.NamedArguments[0];
        if (namedArgument.Key == "Pack"
            && namedArgument.Value.Value is int pack
            && pack == 1)
        {
            return true;
        }

        return false;
    }

    private static bool HasBlittableCompatibleFieldShape(INamedTypeSymbol candidateStruct)
    {
        if (candidateStruct.TypeKind != TypeKind.Struct || candidateStruct.IsRefLikeType)
        {
            return false;
        }

        // Ignore only the candidate's own layout; every nested struct must already be a valid Blittable Struct.
        var typesBeingInspected = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default)
        {
            candidateStruct,
        };
        foreach (ISymbol declaredMember in candidateStruct.GetMembers())
        {
            if (declaredMember is not IFieldSymbol instanceField || instanceField.IsStatic)
            {
                continue;
            }

            if (!TryGetFixedTypeByteCount(instanceField.Type, typesBeingInspected, out _))
            {
                return false;
            }
        }

        return true;
    }

    private static void ReportBlittableCompatibleNestedStructDiagnostics(
        GeneratorExecutionContext executionContext,
        IReadOnlyList<TypeGenerationModel> validGenerationModels,
        IReadOnlyDictionary<INamedTypeSymbol, TypeGenerationModel> generationModels)
    {
        var reportedNestedStructs = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
        foreach (TypeGenerationModel containingGenerationModel in validGenerationModels)
        {
            foreach (FieldGenerationModel nestedField in containingGenerationModel.Fields)
            {
                if (nestedField.Kind != FieldSerializationKind.Nested
                    || nestedField.NestedSerializableType is not INamedTypeSymbol nestedSerializableStruct
                    || nestedSerializableStruct.TypeKind != TypeKind.Struct
                    || !generationModels.TryGetValue(nestedSerializableStruct, out TypeGenerationModel? nestedGenerationModel)
                    || nestedGenerationModel.IsBlittableStruct
                    || !HasBlittableCompatibleFieldShape(nestedSerializableStruct)
                    || !reportedNestedStructs.Add(nestedSerializableStruct))
                {
                    continue;
                }

                // Report only after dependency validation so this performance advice never replaces ZEROS003 or another generation error.
                executionContext.ReportDiagnostic(Diagnostic.Create(
                    BlittableCompatibleNestedStruct,
                    GetTypeIdentifierLocation(nestedSerializableStruct),
                    nestedSerializableStruct.ToDisplayString()));
            }
        }
    }

    private static Location? GetTypeIdentifierLocation(INamedTypeSymbol declaredType)
    {
        foreach (SyntaxReference declaringSyntaxReference in declaredType.DeclaringSyntaxReferences)
        {
            if (declaringSyntaxReference.GetSyntax() is TypeDeclarationSyntax typeDeclaration)
            {
                return typeDeclaration.Identifier.GetLocation();
            }
        }

        return declaredType.Locations.IsDefaultOrEmpty ? null : declaredType.Locations[0];
    }

    private static bool TryGetPrimitiveByteCount(ITypeSymbol candidateType, out int byteCount)
    {
        switch (candidateType.SpecialType)
        {
            case SpecialType.System_Boolean:
            case SpecialType.System_Byte:
            case SpecialType.System_SByte:
                byteCount = 1;
                return true;
            case SpecialType.System_Char:
            case SpecialType.System_Int16:
            case SpecialType.System_UInt16:
                byteCount = 2;
                return true;
            case SpecialType.System_Int32:
            case SpecialType.System_UInt32:
            case SpecialType.System_Single:
                byteCount = 4;
                return true;
            case SpecialType.System_Int64:
            case SpecialType.System_UInt64:
            case SpecialType.System_Double:
                byteCount = 8;
                return true;
            default:
                byteCount = 0;
                return false;
        }
    }

    private static string EmitGeneratedSource(
        TypeGenerationModel generationModel,
        IReadOnlyDictionary<INamedTypeSymbol, TypeGenerationModel> modelLookup)
    {
        var sourceBuilder = new GeneratedSourceBuilder();
        AppendGeneratedFileHeader(sourceBuilder);
        // View declarations stay beside the source type; the global namespace falls back to ZeroSerializer.
        string generatedNamespaceName = generationModel.Symbol.ContainingNamespace.IsGlobalNamespace
            ? SerializerNamespace
            : generationModel.Symbol.ContainingNamespace.ToDisplayString();
        sourceBuilder.AppendLine($"namespace {generatedNamespaceName}");
        sourceBuilder.OpenBlock();
        EmitView(sourceBuilder, generationModel, modelLookup);
        sourceBuilder.CloseBlock();

        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine($"namespace {SerializerNamespace}");
        sourceBuilder.OpenBlock();
        EmitExtensionClass(
            sourceBuilder,
            generationModel,
            modelLookup);
        sourceBuilder.CloseBlock();

        return sourceBuilder.ToString();
    }

    private static void AppendGeneratedFileHeader(GeneratedSourceBuilder sourceBuilder)
    {
        sourceBuilder.AppendLine("// <auto-generated>");
        sourceBuilder.AppendLine($"//   Generated by {SerializerName}");
        sourceBuilder.AppendLine("// </auto-generated>");
        sourceBuilder.AppendLine();
        sourceBuilder.AppendDirective("#nullable enable");
        sourceBuilder.AppendDirective("#pragma warning disable");
        sourceBuilder.AppendLine();
        // Import common BCL APIs while retaining global qualification for generated and user-defined types.
        sourceBuilder.AppendLine("using System;");
        sourceBuilder.AppendLine("using System.Buffers.Binary;");
        sourceBuilder.AppendLine("using System.Runtime.InteropServices;");
        sourceBuilder.AppendLine();
    }

    private static void EmitHelper(GeneratedSourceBuilder sourceBuilder)
    {
        sourceBuilder.AppendLine($"namespace {SerializerNamespace}");
        sourceBuilder.OpenBlock();
        sourceBuilder.AppendLine($"internal static class {SerializerHelperName}");
        sourceBuilder.OpenBlock();
        sourceBuilder.AppendLine("internal static void ThrowEndianError() =>");
        sourceBuilder.AppendLine("    throw new PlatformNotSupportedException(\"ZeroSerializer requires a little-endian runtime.\");");
        sourceBuilder.CloseBlock();
        sourceBuilder.AppendLine();
    }

    private static void EmitWriteBody(
        GeneratedSourceBuilder sourceBuilder,
        TypeGenerationModel generationModel,
        IReadOnlyDictionary<INamedTypeSymbol, TypeGenerationModel> modelLookup,
        string sourceExpression,
        string localNamePrefix)
    {
        string serializedTypeStartOffsetName = CreateLocalName(localNamePrefix, "StartOffset");
        // The field-offset table begins at the serialized type start, so one local owns header writes and relative payload offsets.
        sourceBuilder.AppendLine($"int {serializedTypeStartOffsetName} = writtenBytes;");
        int propertyCount = generationModel.Fields.Count;
        sourceBuilder.AppendLine($"writtenBytes += {propertyCount * 4};");
        for (int propertyIndex = 0; propertyIndex < propertyCount; propertyIndex++)
        {
            FieldGenerationModel propertyModel = generationModel.Fields[propertyIndex];
            // Preserve a visible boundary for every property while keeping the generated writes linear.
            sourceBuilder.AppendLine();
            sourceBuilder.AppendLine($"// {propertyModel.Symbol.Name}");
            string propertyLocalNamePrefix = localNamePrefix + propertyModel.Symbol.Name;
            string propertyAccess = sourceExpression + "." + EscapeIdentifier(propertyModel.Symbol.Name);
            string propertyValueName = CreateLocalName(propertyLocalNamePrefix, "PropertyValue");
            sourceBuilder.AppendLine($"var {propertyValueName} = {propertyAccess};");
            if (IsNullRepresentedByZeroFieldOffset(propertyModel))
            {
                // Offset zero is reserved for null; non-null string and array offsets point to their length header.
                sourceBuilder.AppendLine($"if ({propertyValueName} is null)");
                sourceBuilder.OpenBlock();
                sourceBuilder.AppendLine($"BinaryPrimitives.WriteInt32LittleEndian(destination.Slice({serializedTypeStartOffsetName} + {propertyIndex * 4}, 4), 0);");
                sourceBuilder.CloseBlock();
                sourceBuilder.AppendLine("else");
                sourceBuilder.OpenBlock();
                sourceBuilder.AppendLine($"BinaryPrimitives.WriteInt32LittleEndian(destination.Slice({serializedTypeStartOffsetName} + {propertyIndex * 4}, 4), writtenBytes - {serializedTypeStartOffsetName});");
                EmitWriteField(
                    sourceBuilder,
                    propertyModel,
                    propertyValueName,
                    propertyLocalNamePrefix,
                    modelLookup);
                sourceBuilder.CloseBlock();
            }
            else
            {
                sourceBuilder.AppendLine($"BinaryPrimitives.WriteInt32LittleEndian(destination.Slice({serializedTypeStartOffsetName} + {propertyIndex * 4}, 4), writtenBytes - {serializedTypeStartOffsetName});");
                EmitWriteField(
                    sourceBuilder,
                    propertyModel,
                    propertyValueName,
                    propertyLocalNamePrefix,
                    modelLookup);
            }
        }
    }

    private static void EmitWriteField(
        GeneratedSourceBuilder sourceBuilder,
        FieldGenerationModel field,
        string propertyValueName,
        string localNamePrefix,
        IReadOnlyDictionary<INamedTypeSymbol, TypeGenerationModel> modelLookup)
    {
        string fieldAccess = propertyValueName;
        string serializationValueExpression = GetSerializationValueExpression(field, fieldAccess);
        ITypeSymbol serializedPropertyType = GetSerializedPropertyType(field);
        switch (field.Kind)
        {
            case FieldSerializationKind.Primitive:
                EmitPrimitiveWrite(
                    sourceBuilder,
                    serializedPropertyType,
                    serializationValueExpression,
                    "destination",
                    "writtenBytes");
                break;
            case FieldSerializationKind.BlittableStruct:
                string blittableValueName = CreateLocalName(localNamePrefix, "Value");
                sourceBuilder.AppendLine($"{serializedPropertyType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)} {blittableValueName} = {serializationValueExpression};");
                // MemoryMarshal.Write changed its value parameter from ref to in in .NET 8; generated source follows the target framework directly.
                sourceBuilder.AppendDirective("#if NET8_0_OR_GREATER");
                sourceBuilder.AppendLine($"MemoryMarshal.Write(destination.Slice(writtenBytes, {field.ElementByteCount}), {blittableValueName});");
                sourceBuilder.AppendDirective("#else");
                sourceBuilder.AppendLine($"MemoryMarshal.Write(destination.Slice(writtenBytes, {field.ElementByteCount}), ref {blittableValueName});");
                sourceBuilder.AppendDirective("#endif");
                sourceBuilder.AppendLine($"writtenBytes += {field.ElementByteCount};");
                break;
            case FieldSerializationKind.String:
                sourceBuilder.AppendLine($"ReadOnlySpan<byte> {CreateLocalName(localNamePrefix, "Payload")} = MemoryMarshal.AsBytes({fieldAccess}.AsSpan());");
                sourceBuilder.AppendLine($"int {CreateLocalName(localNamePrefix, "PayloadByteLength")} = {CreateLocalName(localNamePrefix, "Payload")}.Length;");
                sourceBuilder.AppendLine($"BinaryPrimitives.WriteInt32LittleEndian(destination.Slice(writtenBytes, 4), {CreateLocalName(localNamePrefix, "PayloadByteLength")});");
                sourceBuilder.AppendLine("writtenBytes += 4;");
                sourceBuilder.AppendLine($"{CreateLocalName(localNamePrefix, "Payload")}.CopyTo(destination.Slice(writtenBytes));");
                sourceBuilder.AppendLine($"writtenBytes += {CreateLocalName(localNamePrefix, "Payload")}.Length;");
                break;
            case FieldSerializationKind.Array:
                sourceBuilder.AppendLine($"ReadOnlySpan<byte> {CreateLocalName(localNamePrefix, "Payload")} = MemoryMarshal.AsBytes({serializationValueExpression}.AsSpan());");
                sourceBuilder.AppendLine($"int {CreateLocalName(localNamePrefix, "PayloadByteLength")} = {CreateLocalName(localNamePrefix, "Payload")}.Length;");
                sourceBuilder.AppendLine($"BinaryPrimitives.WriteInt32LittleEndian(destination.Slice(writtenBytes, 4), {CreateLocalName(localNamePrefix, "PayloadByteLength")});");
                sourceBuilder.AppendLine("writtenBytes += 4;");
                sourceBuilder.AppendLine($"{CreateLocalName(localNamePrefix, "Payload")}.CopyTo(destination.Slice(writtenBytes));");
                sourceBuilder.AppendLine($"writtenBytes += {CreateLocalName(localNamePrefix, "Payload")}.Length;");
                break;
            case FieldSerializationKind.Nested:
                string nestedSourceExpression;
                if (field.NestedSerializableType!.TypeKind == TypeKind.Struct
                    && field.NullableUnderlyingType is not null)
                {
                    string nullableStructValueName = CreateLocalName(localNamePrefix, "NullableStructValue");
                    sourceBuilder.AppendLine($"var {nullableStructValueName} = {serializationValueExpression};");
                    nestedSourceExpression = nullableStructValueName;
                }
                else
                {
                    nestedSourceExpression = serializationValueExpression;
                }
                EmitWriteBody(
                    sourceBuilder,
                    modelLookup[field.NestedSerializableType],
                    modelLookup,
                    nestedSourceExpression,
                    localNamePrefix);
                break;
        }

    }

    private static void EmitView(
        GeneratedSourceBuilder sourceBuilder,
        TypeGenerationModel generationModel,
        IReadOnlyDictionary<INamedTypeSymbol, TypeGenerationModel> modelLookup)
    {
        string viewAccessibility = generationModel.IsEffectivelyPublic ? "public" : "internal";
        sourceBuilder.AppendLine("/// <summary>");
        sourceBuilder.AppendLine($"/// Provides a deserialized view of <see cref=\"{generationModel.QualifiedSourceTypeName}\"/>.");
        sourceBuilder.AppendLine("/// </summary>");
        sourceBuilder.AppendLine($"{viewAccessibility} readonly struct {generationModel.ViewTypeName}");
        sourceBuilder.OpenBlock();
        sourceBuilder.AppendLine("/// <summary>");
        sourceBuilder.AppendLine($"/// The fixed byte count (including the offset table) plus one native pointer per runtime-sized property for <see cref=\"{generationModel.QualifiedSourceTypeName}\"/>; negative when variable data is present.");
        sourceBuilder.AppendLine("/// </summary>");
        int requiredByteLength = CalculateRequiredByteLength(generationModel, modelLookup);
        sourceBuilder.AppendLine($"public const int RequiredByteLength = {requiredByteLength};");
        sourceBuilder.AppendLine();
        // ReadOnlyMemory keeps the borrowed byte array reusable by ordinary and nested View structs without allocation.
        sourceBuilder.AppendLine("private readonly ReadOnlyMemory<byte> serializedMemory;");
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine($"public {generationModel.ViewTypeName}(ReadOnlyMemory<byte> containingSerializedMemory)");
        sourceBuilder.OpenBlock();
        EmitEndianGuard(sourceBuilder);
        // Never preflight the buffer: the generated Memory and Span operations own bounds validation for every layout.
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine("serializedMemory = containingSerializedMemory;");
        sourceBuilder.CloseBlock();
        sourceBuilder.AppendLine();
        // Fixed layouts trim unused containing-buffer bytes; variable layouts have no encoded total length and must retain the supplied region.
        string serializedMemoryExpression = requiredByteLength >= 0
            ? "serializedView.serializedMemory.Slice(0, RequiredByteLength)"
            : "serializedView.serializedMemory";
        sourceBuilder.AppendLine($"public static implicit operator ReadOnlySpan<byte>({generationModel.ViewTypeName} serializedView) => {serializedMemoryExpression}.Span;");
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine($"public static implicit operator ReadOnlyMemory<byte>({generationModel.ViewTypeName} serializedView) => {serializedMemoryExpression};");
        for (int fieldIndex = 0, fieldCount = generationModel.Fields.Count; fieldIndex < fieldCount; fieldIndex++)
        {
            sourceBuilder.AppendLine();
            EmitViewProperty(sourceBuilder, generationModel, generationModel.Fields[fieldIndex], fieldIndex, modelLookup);
        }
        sourceBuilder.CloseBlock();
    }

    private static int CalculateRequiredByteLength(
        TypeGenerationModel generationModel,
        IReadOnlyDictionary<INamedTypeSymbol, TypeGenerationModel> modelLookup)
    {
        if (generationModel.IsBlittableStruct)
        {
            return generationModel.BlittableStructByteCount;
        }

        bool hasOnlyPredictableData = TryCalculatePredictableRequiredByteLength(
            generationModel,
            modelLookup,
            new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default),
            out int predictableRequiredByteLength);
        return hasOnlyPredictableData ? predictableRequiredByteLength : -predictableRequiredByteLength;
    }

    private static bool TryCalculatePredictableRequiredByteLength(
        TypeGenerationModel generationModel,
        IReadOnlyDictionary<INamedTypeSymbol, TypeGenerationModel> modelLookup,
        HashSet<INamedTypeSymbol> typesBeingInspected,
        out int predictableRequiredByteLength)
    {
        if (!typesBeingInspected.Add(generationModel.Symbol))
        {
            predictableRequiredByteLength = 0;
            return false;
        }

        // Every type owns one 4-byte relative offset per serialized property before its payload area.
        int accumulatedBytesLength = generationModel.Fields.Count * 4;
        bool hasOnlyPredictableData = true;
        foreach (FieldGenerationModel propertyModel in generationModel.Fields)
        {
            int propertyBytesLength;
            if (propertyModel.IsNullableType
                || propertyModel.Kind == FieldSerializationKind.String
                || propertyModel.Kind == FieldSerializationKind.Array)
            {
                // Runtime-sized properties contribute one native pointer so RequiredByteLength retains a CPU-sized estimate.
                propertyBytesLength = IntPtr.Size;
                hasOnlyPredictableData = false;
            }
            else if (propertyModel.Kind == FieldSerializationKind.Primitive
                || propertyModel.Kind == FieldSerializationKind.BlittableStruct)
            {
                propertyBytesLength = propertyModel.ElementByteCount;
            }
            else if (propertyModel.Kind == FieldSerializationKind.Nested
                && propertyModel.NestedSerializableType is not null
                && modelLookup.TryGetValue(propertyModel.NestedSerializableType, out TypeGenerationModel? nestedGenerationModel))
            {
                bool isNestedSizePredictable = TryCalculatePredictableRequiredByteLength(
                    nestedGenerationModel,
                    modelLookup,
                    typesBeingInspected,
                    out int nestedBytesLength);
                if (isNestedSizePredictable)
                {
                    propertyBytesLength = nestedBytesLength;
                }
                else
                {
                    propertyBytesLength = IntPtr.Size;
                    hasOnlyPredictableData = false;
                }
            }
            else
            {
                propertyBytesLength = IntPtr.Size;
                hasOnlyPredictableData = false;
            }

            try
            {
                accumulatedBytesLength = checked(accumulatedBytesLength + propertyBytesLength);
            }
            // Ignore exception: an overflowing estimate is treated as an unpredictable RequiredByteLength.
            catch (OverflowException)
            {
                typesBeingInspected.Remove(generationModel.Symbol);
                predictableRequiredByteLength = 0;
                return false;
            }
        }

        typesBeingInspected.Remove(generationModel.Symbol);
        predictableRequiredByteLength = accumulatedBytesLength;
        return hasOnlyPredictableData;
    }

    private static void EmitViewProperty(
        GeneratedSourceBuilder sourceBuilder,
        TypeGenerationModel containingModel,
        FieldGenerationModel field,
        int fieldIndex,
        IReadOnlyDictionary<INamedTypeSymbol, TypeGenerationModel> modelLookup)
    {
        string propertyAccessibility = IsEffectivelyPublic(field.Symbol.Type) ? "public" : "internal";
        string propertyType;
        if (field.Kind == FieldSerializationKind.String)
        {
            propertyType = "ReadOnlySpan<char>";
        }
        else if (field.Kind == FieldSerializationKind.Array)
        {
            propertyType = $"ReadOnlySpan<{field.ArrayElementType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>";
        }
        else if (field.Kind == FieldSerializationKind.Nested)
        {
            propertyType = GetQualifiedViewName(modelLookup[field.NestedSerializableType!]);
        }
        else
        {
            propertyType = field.Symbol.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        }

        sourceBuilder.AppendLine($"{propertyAccessibility} {propertyType} {EscapeIdentifier(field.Symbol.Name)}");
        sourceBuilder.OpenBlock();
        sourceBuilder.AppendLine("get");
        sourceBuilder.OpenBlock();
        if (containingModel.IsBlittableStruct)
        {
            sourceBuilder.AppendLine($"{containingModel.QualifiedSourceTypeName} blittableSourceValue = MemoryMarshal.Read<{containingModel.QualifiedSourceTypeName}>(serializedMemory.Span);");
            sourceBuilder.AppendLine($"return blittableSourceValue.{EscapeIdentifier(field.Symbol.Name)};");
            sourceBuilder.CloseBlock();
            sourceBuilder.CloseBlock();
            return;
        }

        sourceBuilder.AppendLine("ReadOnlySpan<byte> serializedData = serializedMemory.Span;");
        sourceBuilder.AppendLine($"int fieldDataOffset = BinaryPrimitives.ReadInt32LittleEndian(serializedData.Slice({fieldIndex * 4}, 4));");
        if (IsNullRepresentedByZeroFieldOffset(field))
        {
            // Null is represented entirely by the offset table; no property payload marker is read.
            sourceBuilder.AppendLine("if (fieldDataOffset == 0)");
            sourceBuilder.OpenBlock();
            if (field.NullableUnderlyingType is not null && field.Kind != FieldSerializationKind.Nested)
            {
                sourceBuilder.AppendLine("return null;");
            }
            else
            {
                sourceBuilder.AppendLine("return default;");
            }
            sourceBuilder.CloseBlock();
        }

        switch (field.Kind)
        {
            case FieldSerializationKind.Primitive:
                EmitPrimitiveRead(
                    sourceBuilder,
                    GetSerializedPropertyType(field),
                    "serializedData",
                    "fieldDataOffset");
                break;
            case FieldSerializationKind.BlittableStruct:
                sourceBuilder.AppendLine($"return MemoryMarshal.Read<{GetSerializedPropertyType(field).ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>(serializedData.Slice(fieldDataOffset, {field.ElementByteCount}));");
                break;
            case FieldSerializationKind.String:
                EmitViewCollectionHeader(sourceBuilder, "serializedData", "fieldDataOffset");
                // Keep string payloads borrowed; constructing a string here would allocate on every View access.
                sourceBuilder.AppendLine("return MemoryMarshal.Cast<byte, char>(serializedData.Slice(fieldDataOffset + 4, fieldPayloadByteCount));");
                break;
            case FieldSerializationKind.Array:
                EmitViewCollectionHeader(sourceBuilder, "serializedData", "fieldDataOffset");
                if (field.ArrayElementType!.SpecialType == SpecialType.System_Byte)
                {
                    sourceBuilder.AppendLine("return serializedData.Slice(fieldDataOffset + 4, fieldPayloadByteCount);");
                }
                else
                {
                    sourceBuilder.AppendLine($"return MemoryMarshal.Cast<byte, {field.ArrayElementType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>(serializedData.Slice(fieldDataOffset + 4, fieldPayloadByteCount));");
                }
                break;
            case FieldSerializationKind.Nested:
                // Nested views receive an already-positioned memory region; their constructor has no offset semantics.
                sourceBuilder.AppendLine($"return new {GetQualifiedViewName(modelLookup[field.NestedSerializableType!])}(serializedMemory.Slice(fieldDataOffset));");
                break;
        }

        sourceBuilder.CloseBlock();
        sourceBuilder.CloseBlock();
    }

    private static void EmitViewCollectionHeader(
        GeneratedSourceBuilder sourceBuilder,
        string serializedDataName,
        string fieldDataOffsetName)
    {
        // Null is handled by the field offset before this point; invalid negative lengths must reach Span.Slice unchanged.
        sourceBuilder.AppendLine($"int fieldPayloadByteCount = BinaryPrimitives.ReadInt32LittleEndian({serializedDataName}.Slice({fieldDataOffsetName}, 4));");
    }

    private static void EmitExtensionClass(
        GeneratedSourceBuilder sourceBuilder,
        TypeGenerationModel generationModel,
        IReadOnlyDictionary<INamedTypeSymbol, TypeGenerationModel> modelLookup)
    {
        sourceBuilder.AppendLine($"public static partial class {SerializerExtensionsName}");
        sourceBuilder.OpenBlock();
        string methodAccessibility = generationModel.IsEffectivelyPublic ? "public" : "internal";
        sourceBuilder.AppendLine("/// <summary>");
        sourceBuilder.AppendLine($"/// Serializes <paramref name=\"source\"/> into the wire format (including the offset table) read by <see cref=\"{GetQualifiedViewName(generationModel)}\"/>.");
        sourceBuilder.AppendLine("/// </summary>");
        sourceBuilder.AppendLine("/// <returns>The number of bytes written to <paramref name=\"destination\"/> (including the offset table).</returns>");
        // The Span parameter is named destination so the emitted write body uses it directly without a conversion local.
        if (generationModel.Symbol.TypeKind == TypeKind.Struct
            && Math.Abs((long)CalculateRequiredByteLength(generationModel, modelLookup)) > 16)
        {
            // MemoryMarshal.Write changed from ref through .NET 7 to in in .NET 8, so only newer targets can keep a large receiver readonly.
            sourceBuilder.AppendDirective("#if NET8_0_OR_GREATER");
            sourceBuilder.AppendLine($"{methodAccessibility} static int Serialize(this in {generationModel.QualifiedSourceTypeName} source, Span<byte> destination)");
            sourceBuilder.AppendDirective("#else");
            sourceBuilder.AppendLine($"{methodAccessibility} static int Serialize(this {generationModel.QualifiedSourceTypeName} source, Span<byte> destination)");
            sourceBuilder.AppendDirective("#endif");
        }
        else
        {
            sourceBuilder.AppendLine($"{methodAccessibility} static int Serialize(this {generationModel.QualifiedSourceTypeName} source, Span<byte> destination)");
        }
        sourceBuilder.OpenBlock();
        EmitEndianGuard(sourceBuilder);
        sourceBuilder.AppendLine();
        // Never preflight the destination: emitted Span operations provide the native bounds failure at the write site.
        if (generationModel.IsBlittableStruct)
        {
            // Keep the Write argument aligned with the framework signature selected for the receiver above.
            sourceBuilder.AppendDirective("#if NET8_0_OR_GREATER");
            sourceBuilder.AppendLine("MemoryMarshal.Write(destination, source);");
            sourceBuilder.AppendDirective("#else");
            sourceBuilder.AppendLine("MemoryMarshal.Write(destination, ref source);");
            sourceBuilder.AppendDirective("#endif");
            sourceBuilder.AppendLine($"return {GetQualifiedViewName(generationModel)}.RequiredByteLength;");
        }
        else
        {
            sourceBuilder.AppendLine("int writtenBytes = 0;");
            EmitWriteBody(
                sourceBuilder,
                generationModel,
                modelLookup,
                "source",
                generationModel.Symbol.Name);
            sourceBuilder.AppendLine();
            sourceBuilder.AppendLine("return writtenBytes;");
        }
        sourceBuilder.CloseBlock();
        sourceBuilder.CloseBlock();
    }

    private static void EmitEndianGuard(GeneratedSourceBuilder sourceBuilder)
    {
        sourceBuilder.AppendLine("// String and blittable payloads use their native memory image, so accepting big-endian data would silently corrupt the wire format.");
        sourceBuilder.AppendLine("if (!BitConverter.IsLittleEndian)");
        sourceBuilder.OpenBlock();
        sourceBuilder.AppendLine($"{QualifiedSerializerHelperName}.ThrowEndianError();");
        sourceBuilder.CloseBlock();
    }

    private static ITypeSymbol GetSerializedPropertyType(FieldGenerationModel propertyModel)
    {
        return propertyModel.NullableUnderlyingType ?? propertyModel.Symbol.Type;
    }

    private static bool IsNullRepresentedByZeroFieldOffset(FieldGenerationModel propertyModel)
    {
        return propertyModel.IsNullableType || propertyModel.Kind == FieldSerializationKind.String;
    }

    private static string GetSerializationValueExpression(
        FieldGenerationModel propertyModel,
        string propertyAccess)
    {
        if (propertyModel.NullableUnderlyingType is not null)
        {
            // The surrounding non-null field-offset branch guarantees Value is accessed only for a non-null payload.
            return propertyAccess + ".Value";
        }

        return propertyAccess;
    }

    private static void EmitPrimitiveWrite(
        GeneratedSourceBuilder sourceBuilder,
        ITypeSymbol primitiveOrEnumType,
        string valueExpression,
        string destinationName,
        string destinationOffsetName)
    {
        ITypeSymbol primitiveType = primitiveOrEnumType;
        string convertedValueExpression = valueExpression;
        if (primitiveOrEnumType.TypeKind == TypeKind.Enum && primitiveOrEnumType is INamedTypeSymbol enumType)
        {
            primitiveType = enumType.EnumUnderlyingType!;
            convertedValueExpression = $"({primitiveType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}){valueExpression}";
        }

        string destinationSlice = $"{destinationName}.Slice({destinationOffsetName})";
        switch (primitiveType.SpecialType)
        {
            case SpecialType.System_Boolean:
                sourceBuilder.AppendLine($"{destinationName}[{destinationOffsetName}] = {convertedValueExpression} ? (byte)1 : (byte)0;");
                break;
            case SpecialType.System_Byte:
                sourceBuilder.AppendLine($"{destinationName}[{destinationOffsetName}] = {convertedValueExpression};");
                break;
            case SpecialType.System_SByte:
                sourceBuilder.AppendLine($"{destinationName}[{destinationOffsetName}] = unchecked((byte){convertedValueExpression});");
                break;
            case SpecialType.System_Char:
                sourceBuilder.AppendLine($"BinaryPrimitives.WriteUInt16LittleEndian({destinationSlice}, {convertedValueExpression});");
                break;
            case SpecialType.System_Int16:
                sourceBuilder.AppendLine($"BinaryPrimitives.WriteInt16LittleEndian({destinationSlice}, {convertedValueExpression});");
                break;
            case SpecialType.System_UInt16:
                sourceBuilder.AppendLine($"BinaryPrimitives.WriteUInt16LittleEndian({destinationSlice}, {convertedValueExpression});");
                break;
            case SpecialType.System_Int32:
                sourceBuilder.AppendLine($"BinaryPrimitives.WriteInt32LittleEndian({destinationSlice}, {convertedValueExpression});");
                break;
            case SpecialType.System_UInt32:
                sourceBuilder.AppendLine($"BinaryPrimitives.WriteUInt32LittleEndian({destinationSlice}, {convertedValueExpression});");
                break;
            case SpecialType.System_Int64:
                sourceBuilder.AppendLine($"BinaryPrimitives.WriteInt64LittleEndian({destinationSlice}, {convertedValueExpression});");
                break;
            case SpecialType.System_UInt64:
                sourceBuilder.AppendLine($"BinaryPrimitives.WriteUInt64LittleEndian({destinationSlice}, {convertedValueExpression});");
                break;
            case SpecialType.System_Single:
                // Floating-point BinaryPrimitives APIs were added in .NET 8; earlier targets preserve the same bits through integral APIs.
                sourceBuilder.AppendDirective("#if NET8_0_OR_GREATER");
                sourceBuilder.AppendLine($"BinaryPrimitives.WriteSingleLittleEndian({destinationSlice}, {convertedValueExpression});");
                sourceBuilder.AppendDirective("#else");
                sourceBuilder.AppendLine($"BinaryPrimitives.WriteInt32LittleEndian({destinationSlice}, BitConverter.SingleToInt32Bits({convertedValueExpression}));");
                sourceBuilder.AppendDirective("#endif");
                break;
            case SpecialType.System_Double:
                // Floating-point BinaryPrimitives APIs were added in .NET 8; earlier targets preserve the same bits through integral APIs.
                sourceBuilder.AppendDirective("#if NET8_0_OR_GREATER");
                sourceBuilder.AppendLine($"BinaryPrimitives.WriteDoubleLittleEndian({destinationSlice}, {convertedValueExpression});");
                sourceBuilder.AppendDirective("#else");
                sourceBuilder.AppendLine($"BinaryPrimitives.WriteInt64LittleEndian({destinationSlice}, BitConverter.DoubleToInt64Bits({convertedValueExpression}));");
                sourceBuilder.AppendDirective("#endif");
                break;
        }

        TryGetPrimitiveByteCount(primitiveType, out int primitiveByteCount);
        sourceBuilder.AppendLine($"{destinationOffsetName} += {primitiveByteCount};");
    }

    private static void EmitPrimitiveRead(
        GeneratedSourceBuilder sourceBuilder,
        ITypeSymbol primitiveOrEnumType,
        string serializedDataName,
        string serializedDataOffsetName)
    {
        ITypeSymbol primitiveType = primitiveOrEnumType;
        string returnConversionPrefix = string.Empty;
        if (primitiveOrEnumType.TypeKind == TypeKind.Enum && primitiveOrEnumType is INamedTypeSymbol enumType)
        {
            primitiveType = enumType.EnumUnderlyingType!;
            returnConversionPrefix = $"({primitiveOrEnumType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)})";
        }

        string serializedDataSlice = $"{serializedDataName}.Slice({serializedDataOffsetName})";
        string readExpression;
        switch (primitiveType.SpecialType)
        {
            case SpecialType.System_Boolean:
                readExpression = $"{serializedDataName}[{serializedDataOffsetName}] != 0";
                break;
            case SpecialType.System_Byte:
                readExpression = $"{serializedDataName}[{serializedDataOffsetName}]";
                break;
            case SpecialType.System_SByte:
                readExpression = $"unchecked((sbyte){serializedDataName}[{serializedDataOffsetName}])";
                break;
            case SpecialType.System_Char:
                readExpression = $"(char)BinaryPrimitives.ReadUInt16LittleEndian({serializedDataSlice})";
                break;
            case SpecialType.System_Int16:
                readExpression = $"BinaryPrimitives.ReadInt16LittleEndian({serializedDataSlice})";
                break;
            case SpecialType.System_UInt16:
                readExpression = $"BinaryPrimitives.ReadUInt16LittleEndian({serializedDataSlice})";
                break;
            case SpecialType.System_Int32:
                readExpression = $"BinaryPrimitives.ReadInt32LittleEndian({serializedDataSlice})";
                break;
            case SpecialType.System_UInt32:
                readExpression = $"BinaryPrimitives.ReadUInt32LittleEndian({serializedDataSlice})";
                break;
            case SpecialType.System_Int64:
                readExpression = $"BinaryPrimitives.ReadInt64LittleEndian({serializedDataSlice})";
                break;
            case SpecialType.System_UInt64:
                readExpression = $"BinaryPrimitives.ReadUInt64LittleEndian({serializedDataSlice})";
                break;
            case SpecialType.System_Single:
                // Floating-point BinaryPrimitives APIs were added in .NET 8; earlier targets reconstruct the same bits through integral APIs.
                sourceBuilder.AppendDirective("#if NET8_0_OR_GREATER");
                sourceBuilder.AppendLine($"return BinaryPrimitives.ReadSingleLittleEndian({serializedDataSlice});");
                sourceBuilder.AppendDirective("#else");
                sourceBuilder.AppendLine($"return BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian({serializedDataSlice}));");
                sourceBuilder.AppendDirective("#endif");
                return;
            case SpecialType.System_Double:
                // Floating-point BinaryPrimitives APIs were added in .NET 8; earlier targets reconstruct the same bits through integral APIs.
                sourceBuilder.AppendDirective("#if NET8_0_OR_GREATER");
                sourceBuilder.AppendLine($"return BinaryPrimitives.ReadDoubleLittleEndian({serializedDataSlice});");
                sourceBuilder.AppendDirective("#else");
                sourceBuilder.AppendLine($"return BitConverter.Int64BitsToDouble(BinaryPrimitives.ReadInt64LittleEndian({serializedDataSlice}));");
                sourceBuilder.AppendDirective("#endif");
                return;
            default:
                throw new InvalidOperationException("Unsupported primitive type reached source emission.");
        }

        sourceBuilder.AppendLine($"return {returnConversionPrefix}{readExpression};");
    }

    private static bool IsAccessibleFromGeneratedExtension(Accessibility accessibility)
    {
        return accessibility == Accessibility.Public
            || accessibility == Accessibility.Internal
            || accessibility == Accessibility.ProtectedOrInternal;
    }

    private static bool IsEffectivelyPublic(ITypeSymbol typeSymbol)
    {
        if (typeSymbol is IArrayTypeSymbol arrayType)
        {
            return IsEffectivelyPublic(arrayType.ElementType);
        }

        for (ISymbol? currentSymbol = typeSymbol; currentSymbol is not null; currentSymbol = currentSymbol.ContainingType)
        {
            if (currentSymbol.DeclaredAccessibility != Accessibility.Public)
            {
                return false;
            }
        }

        return true;
    }

    private static string GetQualifiedViewName(TypeGenerationModel generationModel)
    {
        string generatedNamespaceName = generationModel.Symbol.ContainingNamespace.IsGlobalNamespace
            ? SerializerNamespace
            : generationModel.Symbol.ContainingNamespace.ToDisplayString();
        return "global::" + generatedNamespaceName + "." + generationModel.ViewTypeName;
    }

    private static string CreateLocalName(string fieldName, string suffix)
    {
        if (fieldName.Length == 0)
        {
            return "serializedField" + suffix;
        }

        return char.ToLowerInvariant(fieldName[0]) + fieldName.Substring(1) + suffix;
    }

    private static string EscapeIdentifier(string identifier)
    {
        return SyntaxFacts.GetKeywordKind(identifier) != SyntaxKind.None ? "@" + identifier : identifier;
    }

    private static string EscapeString(string value)
    {
        return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    private enum FieldSerializationKind
    {
        Primitive,
        String,
        BlittableStruct,
        Array,
        Nested,
        InvalidArray,
    }

    private sealed class TypeGenerationModel
    {
        internal TypeGenerationModel(
            INamedTypeSymbol symbol,
            string qualifiedSourceTypeName,
            string viewTypeName,
            bool isEffectivelyPublic,
            bool isBlittableStruct,
            int blittableStructByteCount)
        {
            Symbol = symbol;
            QualifiedSourceTypeName = qualifiedSourceTypeName;
            ViewTypeName = viewTypeName;
            IsEffectivelyPublic = isEffectivelyPublic;
            IsBlittableStruct = isBlittableStruct;
            BlittableStructByteCount = blittableStructByteCount;
        }

        internal INamedTypeSymbol Symbol { get; }

        internal string QualifiedSourceTypeName { get; }

        internal string ViewTypeName { get; }

        internal bool IsEffectivelyPublic { get; }

        internal bool IsBlittableStruct { get; }

        internal int BlittableStructByteCount { get; }

        internal List<FieldGenerationModel> Fields { get; } = new();

        internal bool IsValid { get; set; } = true;
    }

    private sealed class FieldGenerationModel
    {
        internal FieldGenerationModel(
            IPropertySymbol symbol,
            FieldSerializationKind kind,
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

        internal FieldSerializationKind Kind { get; }

        internal int ElementByteCount { get; }

        internal ITypeSymbol? ArrayElementType { get; }

        internal INamedTypeSymbol? NestedSerializableType { get; }

        internal ITypeSymbol? NullableUnderlyingType { get; }

        internal bool IsNullableType { get; }
    }

    private sealed class GeneratedSourceBuilder
    {
        private readonly StringBuilder sourceBuilder = new();
        private int indentationLevel;

        internal void AppendLine(string line = "")
        {
            if (line.Length != 0)
            {
                sourceBuilder.Append(' ', indentationLevel * 4);
                sourceBuilder.Append(line);
            }

            sourceBuilder.AppendLine();
        }

        internal void AppendDirective(string line)
        {
            sourceBuilder.AppendLine(line);
        }

        internal void OpenBlock()
        {
            AppendLine("{");
            indentationLevel++;
        }

        internal void CloseBlock()
        {
            indentationLevel--;
            AppendLine("}");
        }

        public override string ToString()
        {
            return sourceBuilder.ToString();
        }
    }

    private sealed class SerializableTypeSyntaxReceiver : ISyntaxReceiver
    {
        internal List<TypeDeclarationSyntax> CandidateDeclarations { get; } = new();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is TypeDeclarationSyntax typeDeclaration
                && typeDeclaration.AttributeLists.Count != 0)
            {
                CandidateDeclarations.Add(typeDeclaration);
            }
        }
    }
}
