// Licensed under the Apache-2.0 License
// https://github.com/sator-imaging/ZeroSerializer

using System;
using System.Buffers.Binary;
using System.Runtime.InteropServices;
using ZeroSerializer;

#pragma warning disable CS1591  // Missing XML comment for publicly visible type or member
#pragma warning disable CEK001  // Collection expressions are not allowed
#pragma warning disable CEK003  // Collection expression text length must be 12 or fewer characters
#pragma warning disable CEK005  // Collection expressions must be empty

// This project compiles every successful public wire shape against netstandard2.1; diagnostic failures remain test-project cases because they intentionally prevent compilation.
FixedPacket fixedPacket = new FixedPacket
{
    BooleanValue = true,
    ByteValue = 0xAB,
    SignedByteValue = -12,
    CharacterValue = '界',
    Int16Value = -1234,
    UInt16Value = 54321,
    Int32Value = -12345678,
    UInt32Value = 3456789012,
    Int64Value = -123456789012345,
    UInt64Value = 12345678901234567890,
    SingleValue = 1.5f,
    DoubleValue = -123.5,
    State = PacketState.Ready,
    Position = new PackedPosition { X = 10, Y = 20 },
};
byte[] fixedBuffer = new byte[FixedPacketView.RequiredByteLength];
int fixedWrittenByteCount = fixedPacket.Serialize(fixedBuffer);
FixedPacketView fixedView = new FixedPacketView(fixedBuffer);

RequireCondition(
    fixedWrittenByteCount == FixedPacketView.RequiredByteLength
    && fixedView.BooleanValue == fixedPacket.BooleanValue
    && fixedView.ByteValue == fixedPacket.ByteValue
    && fixedView.SignedByteValue == fixedPacket.SignedByteValue
    && fixedView.CharacterValue == fixedPacket.CharacterValue
    && fixedView.Int16Value == fixedPacket.Int16Value
    && fixedView.UInt16Value == fixedPacket.UInt16Value
    && fixedView.Int32Value == fixedPacket.Int32Value
    && fixedView.UInt32Value == fixedPacket.UInt32Value
    && fixedView.Int64Value == fixedPacket.Int64Value
    && fixedView.UInt64Value == fixedPacket.UInt64Value
    && fixedView.SingleValue == fixedPacket.SingleValue
    && fixedView.DoubleValue == fixedPacket.DoubleValue
    && fixedView.State == fixedPacket.State
    && fixedView.Position.X == fixedPacket.Position.X
    && fixedView.Position.Y == fixedPacket.Position.Y,
    "Fixed-size primitive, enum, or nested Blittable value did not match its source.");

ReadOnlySpan<byte> fixedSerializedSpan = fixedView;
ReadOnlyMemory<byte> fixedSerializedMemory = fixedView;
RequireCondition(
    fixedSerializedSpan.Length == FixedPacketView.RequiredByteLength
    && fixedSerializedMemory.Length == FixedPacketView.RequiredByteLength,
    "Fixed-size View conversion did not expose its exact serialized region.");

PackedPacket packedPacket = new PackedPacket
{
    Identifier = 42,
    Position = new PackedPosition { X = -10, Y = 20 },
};
byte[] packedBuffer = new byte[PackedPacketView.RequiredByteLength];
int packedWrittenByteCount = packedPacket.Serialize(packedBuffer);
PackedPacketView packedView = new PackedPacketView(packedBuffer);
RequireCondition(
    packedWrittenByteCount == PackedPacketView.RequiredByteLength
    && packedView.Identifier == packedPacket.Identifier
    && packedView.Position.X == packedPacket.Position.X
    && packedView.Position.Y == packedPacket.Position.Y,
    "Root Blittable Struct did not match its source.");

VariablePacket variablePacket = new VariablePacket
{
    Name = "unity",
    EmptyName = string.Empty,
    MissingName = null,
    Values = [10, 20, 30],
    States = [PacketState.None, PacketState.Ready],
    Positions = [new PackedPosition { X = 1, Y = 2 }, new PackedPosition { X = 3, Y = 4 }],
    EmptyValues = Array.Empty<int>(),
    MissingValues = null,
    OptionalValue = 17,
    MissingOptionalValue = null,
    OptionalState = PacketState.Ready,
    MissingOptionalState = null,
    OptionalPosition = new PackedPosition { X = 30, Y = 40 },
    MissingOptionalPosition = null,
    Child = new ChildPacket { Identifier = 99 },
    MissingChild = null,
    StructChild = new StructChildPacket { Identifier = 100, Name = "struct" },
    OptionalStructChild = new StructChildPacket { Identifier = 101, Name = "optional struct" },
    MissingOptionalStructChild = null,
    FloatValues = [1.5f, 2.5f, 3.5f],
    DoubleValues = [1.25, 2.25, 3.25],
};
byte[] variableBuffer = new byte[1024];
int variableWrittenByteCount = variablePacket.Serialize(variableBuffer);
VariablePacketView variableView = new VariablePacketView(variableBuffer.AsMemory(0, variableWrittenByteCount));

RequireCondition(
    VariablePacketView.RequiredByteLength < 0
    && variableView.Name.SequenceEqual("unity".AsSpan())
    && variableView.EmptyName.IsEmpty
    && variableView.MissingName.IsEmpty
    && variableView.Values.Length == 3
    && variableView.Values[2] == 30
    && variableView.States.Length == 2
    && variableView.States[1] == PacketState.Ready
    && variableView.Positions.Length == 2
    && variableView.Positions[1].Y == 4
    && variableView.EmptyValues.IsEmpty
    && variableView.MissingValues.IsEmpty
    && variableView.OptionalValue == 17
    && variableView.MissingOptionalValue is null
    && variableView.OptionalState == PacketState.Ready
    && variableView.MissingOptionalState is null
    && variableView.OptionalPosition!.Value.X == 30
    && variableView.MissingOptionalPosition is null
    && variableView.Child?.Identifier == 99
    && variableView.StructChild.Identifier == 100
    && variableView.StructChild.Name.SequenceEqual("struct".AsSpan())
    && variableView.OptionalStructChild?.Identifier == 101
    && variableView.OptionalStructChild?.Name.SequenceEqual("optional struct".AsSpan()) == true
    && variableView.MissingOptionalStructChild is null
    && variableView.FloatValues.Length == 3
    && variableView.FloatValues[1] == 2.5f
    && variableView.DoubleValues.Length == 3
    && variableView.DoubleValues[2] == 3.25,
    "Runtime-sized, nullable, array, string, or nested View value did not match its source.");

ReadOnlySpan<byte> variableSerializedSpan = variableView;
ReadOnlyMemory<byte> variableSerializedMemory = variableView;
RequireCondition(
    variableSerializedSpan.Length == variableWrittenByteCount
    && variableSerializedMemory.Length == variableWrittenByteCount,
    "Runtime-sized View conversion did not retain its supplied serialized region.");

ReadOnlySpan<byte> variableSerializedData = variableView;
RequireCondition(
    BinaryPrimitives.ReadInt32LittleEndian(variableSerializedData.Slice(2 * sizeof(int), sizeof(int))) == 0
    && BinaryPrimitives.ReadInt32LittleEndian(variableSerializedData.Slice(7 * sizeof(int), sizeof(int))) == 0
    && BinaryPrimitives.ReadInt32LittleEndian(variableSerializedData.Slice(9 * sizeof(int), sizeof(int))) == 0
    && BinaryPrimitives.ReadInt32LittleEndian(variableSerializedData.Slice(11 * sizeof(int), sizeof(int))) == 0
    && BinaryPrimitives.ReadInt32LittleEndian(variableSerializedData.Slice(13 * sizeof(int), sizeof(int))) == 0
    && BinaryPrimitives.ReadInt32LittleEndian(variableSerializedData.Slice(15 * sizeof(int), sizeof(int))) == 0
    && BinaryPrimitives.ReadInt32LittleEndian(variableSerializedData.Slice(18 * sizeof(int), sizeof(int))) == 0,
    "Null payloads were not represented by zero property offsets.");

IgnoredMembersPacket ignoredMembersPacket = new IgnoredMembersPacket(7, 8, 9) { IgnoredField = 10 };
byte[] ignoredMembersBuffer = new byte[IgnoredMembersPacketView.RequiredByteLength];
int ignoredMembersWritten = ignoredMembersPacket.Serialize(ignoredMembersBuffer);
IgnoredMembersPacketView ignoredMembersView = new IgnoredMembersPacketView(ignoredMembersBuffer);
RequireCondition(
    ignoredMembersWritten == 16 && ignoredMembersView.Included == 7
    && ignoredMembersView.PrivateSetter == 8
    && typeof(IgnoredMembersPacketView).GetProperties().Length == 2
    && typeof(IgnoredMembersPacketView).GetProperty(nameof(IgnoredMembersPacket.Included)) is not null
    && typeof(IgnoredMembersPacketView).GetProperty(nameof(IgnoredMembersPacket.PrivateSetter)) is not null,
    "Ignored properties, indexers, setters, or non-public getters changed the generated View.");

UnityCompatibilityModels.NamespacedPacket namespacedPacket = new UnityCompatibilityModels.NamespacedPacket { Identifier = 11 };
byte[] namespacedBuffer = new byte[UnityCompatibilityModels.NamespacedPacketView.RequiredByteLength];
int namespacedWritten = namespacedPacket.Serialize(namespacedBuffer);
UnityCompatibilityModels.NamespacedPacketView namespacedView = new UnityCompatibilityModels.NamespacedPacketView(namespacedBuffer);
RequireCondition(namespacedWritten == 8 && namespacedView.Identifier == namespacedPacket.Identifier, "Namespace-local generation failed.");

Span<byte> emptyClassBuffer = Span<byte>.Empty;
Span<byte> emptyStructBuffer = Span<byte>.Empty;
RequireCondition(
    new EmptyClassPacket().Serialize(emptyClassBuffer) == 0
    && new EmptyStructPacket().Serialize(emptyStructBuffer) == 0
    && EmptyClassPacketView.RequiredByteLength == 0
    && EmptyStructPacketView.RequiredByteLength == 0,
    "Empty serializable types did not remain zero length.");

// Record class roundtrip verification
UnitySimpleCsharpRecord recordObj = new UnitySimpleCsharpRecord { IntValue = 55, DoubleValue = 99.9 };
byte[] recordBuffer = new byte[128];
int recordWritten = recordObj.Serialize(recordBuffer);
UnitySimpleCsharpRecordView recordView = new UnitySimpleCsharpRecordView(recordBuffer);
RequireCondition(
    recordWritten == 20
    && recordView.IntValue == 55
    && recordView.DoubleValue == 99.9
    && !UnitySimpleCsharpRecordView.IsBlittable,
    "UnitySimpleCsharpRecord did not match its source or IsBlittable was incorrect.");

// Record struct roundtrip verification
UnitySimpleRecordStruct recordStructObj = new UnitySimpleRecordStruct { IntValue = 66, DoubleValue = 88.8 };
byte[] recordStructBuffer = new byte[128];
int recordStructWritten = recordStructObj.Serialize(recordStructBuffer);
UnitySimpleRecordStructView recordStructView = new UnitySimpleRecordStructView(recordStructBuffer);
RequireCondition(
    recordStructWritten == 20
    && recordStructView.IntValue == 66
    && recordStructView.DoubleValue == 88.8
    && !UnitySimpleRecordStructView.IsBlittable,
    "UnitySimpleRecordStruct did not match its source or IsBlittable was incorrect.");

// Blittable record struct layout verification
UnitySimpleBlittableRecordStruct blittableRecordStructObj = new UnitySimpleBlittableRecordStruct { IntValue = 77, DoubleValue = 77.7 };
byte[] blittableRecordStructBuffer = new byte[UnitySimpleBlittableRecordStructView.RequiredByteLength];
int blittableRecordStructWritten = blittableRecordStructObj.Serialize(blittableRecordStructBuffer);
UnitySimpleBlittableRecordStructView blittableRecordStructView = new UnitySimpleBlittableRecordStructView(blittableRecordStructBuffer);
RequireCondition(
    blittableRecordStructView.IntValue == 77
    && blittableRecordStructView.DoubleValue == 77.7
    && UnitySimpleBlittableRecordStructView.IsBlittable
    && UnitySimpleBlittableRecordStructView.RequiredByteLength == 12
    && blittableRecordStructWritten == 12,
    "UnitySimpleBlittableRecordStruct did not match its source or IsBlittable was incorrect.");

// Nested blittable record struct container verification
UnitySimpleBlittableRecordStruct firstRecord = new UnitySimpleBlittableRecordStruct { IntValue = 88, DoubleValue = 88.88 };
UnitySimpleBlittableRecordStruct secondRecord = new UnitySimpleBlittableRecordStruct { IntValue = 99, DoubleValue = 99.99 };
UnityBlittableRecordStructContainer nestedBlittableRecordContainer = new UnityBlittableRecordStructContainer
{
    Value = firstRecord,
    OptionalValue = secondRecord,
    Values = [firstRecord, secondRecord],
};
byte[] nestedBlittableRecordBuffer = new byte[256];
int nestedBlittableRecordWritten = nestedBlittableRecordContainer.Serialize(nestedBlittableRecordBuffer);
UnityBlittableRecordStructContainerView nestedBlittableRecordView = new UnityBlittableRecordStructContainerView(nestedBlittableRecordBuffer.AsMemory(0, nestedBlittableRecordWritten));
RequireCondition(
    nestedBlittableRecordWritten == 64
    && nestedBlittableRecordView.Value.IntValue == 88
    && nestedBlittableRecordView.Value.DoubleValue == 88.88
    && nestedBlittableRecordView.OptionalValue!.Value.IntValue == 99
    && nestedBlittableRecordView.OptionalValue!.Value.DoubleValue == 99.99
    && nestedBlittableRecordView.Values.Length == 2
    && nestedBlittableRecordView.Values[0].IntValue == 88
    && nestedBlittableRecordView.Values[0].DoubleValue == 88.88
    && nestedBlittableRecordView.Values[1].IntValue == 99
    && nestedBlittableRecordView.Values[1].DoubleValue == 99.99
    && nestedBlittableRecordView.GetByteLength() == 64
    && nestedBlittableRecordView.GetByteLength() == nestedBlittableRecordWritten,
    "UnityBlittableRecordStructContainer non-null roundtrip failed.");

UnityBlittableRecordStructContainer nestedBlittableRecordNullsContainer = new UnityBlittableRecordStructContainer
{
    Value = firstRecord,
    OptionalValue = null,
    Values = null,
};
int nestedBlittableRecordNullsWritten = nestedBlittableRecordNullsContainer.Serialize(nestedBlittableRecordBuffer);
UnityBlittableRecordStructContainerView nestedBlittableRecordNullsView = new UnityBlittableRecordStructContainerView(nestedBlittableRecordBuffer.AsMemory(0, nestedBlittableRecordNullsWritten));
RequireCondition(
    nestedBlittableRecordNullsWritten == 24
    && nestedBlittableRecordNullsView.Value.IntValue == 88
    && nestedBlittableRecordNullsView.Value.DoubleValue == 88.88
    && nestedBlittableRecordNullsView.OptionalValue is null
    && nestedBlittableRecordNullsView.Values.IsEmpty
    && nestedBlittableRecordNullsView.GetByteLength() == 24
    && nestedBlittableRecordNullsView.GetByteLength() == nestedBlittableRecordNullsWritten,
    "UnityBlittableRecordStructContainer nulls roundtrip failed.");

Console.WriteLine("ZeroSerializer Unity compatibility tests passed.");

return 0;





static void RequireCondition(bool condition, string failureMessage)
{
    if (!condition)
    {
        throw new InvalidOperationException(failureMessage);
    }
}

[ZeroSerializer]
public struct FixedPacket
{
    public bool BooleanValue { get; init; }

    public byte ByteValue { get; init; }

    public sbyte SignedByteValue { get; init; }

    public char CharacterValue { get; init; }

    public short Int16Value { get; init; }

    public ushort UInt16Value { get; init; }

    public int Int32Value { get; init; }

    public uint UInt32Value { get; init; }

    public long Int64Value { get; init; }

    public ulong UInt64Value { get; init; }

    public float SingleValue { get; init; }

    public double DoubleValue { get; init; }

    public PacketState State { get; init; }

    public PackedPosition Position { get; init; }
}

public enum PacketState : sbyte  // Use sbyte to verify code generator emitting `unchecked` correctly
{
    None,
    Ready,
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
[ZeroSerializer]
public struct PackedPosition
{
    public int X { get; init; }
    public int Y { get; init; }
}

[ZeroSerializer]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PackedPacket
{
    public int Identifier { get; init; }

    public PackedPosition Position { get; init; }
}

[ZeroSerializer]
public sealed class VariablePacket
{
    public string? Name { get; init; }

    public string? EmptyName { get; init; }

    public string? MissingName { get; init; }

    public int[]? Values { get; init; }

    public PacketState[]? States { get; init; }

    public PackedPosition[]? Positions { get; init; }

    public int[]? EmptyValues { get; init; }

    public int[]? MissingValues { get; init; }

    public int? OptionalValue { get; init; }

    public int? MissingOptionalValue { get; init; }

    public PacketState? OptionalState { get; init; }

    public PacketState? MissingOptionalState { get; init; }

    public PackedPosition? OptionalPosition { get; init; }

    public PackedPosition? MissingOptionalPosition { get; init; }

    public ChildPacket? Child { get; init; }

    public ChildPacket? MissingChild { get; init; }

    public StructChildPacket StructChild { get; init; }

    public StructChildPacket? OptionalStructChild { get; init; }

    public StructChildPacket? MissingOptionalStructChild { get; init; }

    public float[]? FloatValues { get; init; }

    public double[]? DoubleValues { get; init; }
}

[ZeroSerializer]
public sealed class ChildPacket
{
    public int Identifier { get; init; }
}

[ZeroSerializer]
public struct StructChildPacket
{
    public int Identifier { get; init; }

    public string? Name { get; init; }
}

[ZeroSerializer]
public sealed class IgnoredMembersPacket
{
    public IgnoredMembersPacket(int included, int privateSetter, int privateGetter)
    {
        Included = included;
        PrivateSetter = privateSetter;
        PrivateGetter = privateGetter;
    }

    public int Included { get; }

    public int PrivateSetter { get; private set; }

    public int PrivateGetter { private get; set; }

    public int this[int index] => index;

    public int IgnoredField;
}

[ZeroSerializer(EmitShapeTag = true)]
public sealed class EmptyClassPacket
{
}

[ZeroSerializer(EmitShapeTag = true)]
public struct EmptyStructPacket
{
}

namespace UnityCompatibilityModels
{
    [ZeroSerializer]
    public sealed class NamespacedPacket
    {
        public int Identifier { get; init; }
    }
}

// Polyfill
namespace System.Runtime.CompilerServices
{
    struct IsExternalInit { }
}

[ZeroSerializer]
public record UnitySimpleCsharpRecord
{
    public int IntValue { get; init; }
    public double DoubleValue { get; init; }
}

[ZeroSerializer]
public sealed class UnityBlittableRecordStructContainer
{
    public UnitySimpleBlittableRecordStruct Value { get; init; }

    public UnitySimpleBlittableRecordStruct? OptionalValue { get; init; }

    public UnitySimpleBlittableRecordStruct[]? Values { get; init; }
}

[ZeroSerializer]
public record struct UnitySimpleRecordStruct
{
    public int IntValue { get; init; }
    public double DoubleValue { get; init; }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
[ZeroSerializer]
public record struct UnitySimpleBlittableRecordStruct
{
    public int IntValue { get; init; }
    public double DoubleValue { get; init; }
}

public static class UnityCompatibilityHelper
{
    public static void AlwaysFail()
    {
        throw new InvalidOperationException("Intentional failure for verifying CI workflow correctness.");
    }
}
