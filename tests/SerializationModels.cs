// Licensed under the Apache-2.0 License
// https://github.com/sator-imaging/ZeroSerializer

using System;
using System.Runtime.InteropServices;
using ZeroSerializer.Tests.EnumTypingModels;

#pragma warning disable CS1591  // Missing XML comment for publicly visible type or member
#pragma warning disable SMA8003  // Do not use debug-only `Assert` in public API surface

namespace ZeroSerializer.Tests.Models;

public enum ByteState : byte
{
    None,
    Ready = 7,
}

public enum SignedState : short
{
    Negative = -3,
    Positive = 5,
}

public enum DefaultState
{
    None,
    Ready = 42,
}

public enum SByteEnum : sbyte
{
    Min = -128,
    Zero = 0,
    Max = 127,
}

[ZeroSerializer(EmitShapeTag = true)]
public sealed class PrimitiveRecord
{
    public bool Boolean { get; init; }

    public byte Byte { get; init; }

    public sbyte SignedByte { get; init; }

    public char Character { get; init; }

    public short Int16 { get; init; }

    public ushort UInt16 { get; init; }

    public int Int32 { get; init; }

    public uint UInt32 { get; init; }

    public long Int64 { get; init; }

    public ulong UInt64 { get; init; }

    public float Single { get; init; }

    public double Double { get; init; }
}

[ZeroSerializer(EmitShapeTag = true)]
public sealed class EnumClass
{
    public ByteState ByteState { get; init; }

    public SignedState SignedState { get; init; }

    public DefaultState DefaultState { get; init; }

    public DefaultState? NullableDefaultState { get; init; }
}

[ZeroSerializer]
public readonly struct EnumStruct
{
    public EnumStruct(ByteState byteState, SignedState signedState)
    {
        ByteState = byteState;
        SignedState = signedState;
    }

    public ByteState ByteState { get; }

    public SignedState SignedState { get; }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
[ZeroSerializer(EmitShapeTag = true)]
public struct PackedRecord
{
    public int Number { get; init; }

    public SignedState State { get; init; }
}

[ZeroSerializer(EmitShapeTag = true)]
public sealed class PackedContainer
{
    public PackedRecord Value { get; init; }

    public PackedRecord? OptionalValue { get; init; }

    public PackedRecord[]? Values { get; init; }
}

[ZeroSerializer]
public sealed class SByteTestRecord
{
    public sbyte SByteValue { get; init; }

    public sbyte? NullableSByteValue { get; init; }

    public SByteEnum SByteBackedEnum { get; init; }

    public SByteEnum? NullableSByteBackedEnum { get; init; }

    public sbyte[] SByteArray { get; init; } = Array.Empty<sbyte>();

    public SByteEnum[] SByteBackedEnumArray { get; init; } = Array.Empty<SByteEnum>();
}

[ZeroSerializer]
public sealed class LargeRandomRecord
{
    public string FirstText { get; init; } = string.Empty;

    public string SecondText { get; init; } = string.Empty;

    public byte[] Bytes { get; init; } = Array.Empty<byte>();

    public int[] Integers { get; init; } = Array.Empty<int>();

    public long[] Longs { get; init; } = Array.Empty<long>();

    public PackedRecord[] PackedRecords { get; init; } = Array.Empty<PackedRecord>();
}

[ZeroSerializer]
public sealed class FixedClass
{
    public int Identifier { get; init; }

    public ByteState State { get; init; }
}

[ZeroSerializer]
public readonly struct SmallFixedStruct
{
    public SmallFixedStruct(int value)
    {
        Value = value;
    }

    public int Value { get; }
}

[ZeroSerializer]
public readonly struct LargeFixedStruct
{
    public LargeFixedStruct(long value, ByteState state)
    {
        Value = value;
        State = state;
    }

    public long Value { get; }

    public ByteState State { get; }
}

[ZeroSerializer]
public sealed class VariableRecord
{
    public string? Text { get; init; }

    public int[]? Values { get; init; }

    public int? OptionalNumber { get; init; }

    public FixedClass? Child { get; init; }

    public int Tail { get; init; }
}

[ZeroSerializer]
public sealed class StringOnlyRecord
{
    public string? Text { get; init; }
}

[ZeroSerializer]
public sealed class EmptyClass
{
}

[ZeroSerializer]
public struct EmptyStruct
{
}

[ZeroSerializer]
public sealed class ZeroLengthNestedStructContainer
{
    public int Before { get; init; }

    public EmptyStruct Empty { get; init; }

    public int After { get; init; }
}

[ZeroSerializer]
public sealed class FieldsOnlyClass
{
    public int IgnoredField;
}

[ZeroSerializer]
public sealed class PropertyVariants
{
    public PropertyVariants(int privateSetterValue, int getterOnlyValue, int privateGetterValue)
    {
        PrivateSetter = privateSetterValue;
        GetterOnly = getterOnlyValue;
        PrivateGetter = privateGetterValue;
    }

    public int PrivateSetter { get; private set; }

    public int GetterOnly { get; }

    public int PrivateGetter { private get; set; }
}

[ZeroSerializer]
public sealed class ByteArrayRecord
{
    public byte[]? Payload { get; init; }
}

[ZeroSerializer]
public struct Utf8Payload
{
    public Utf8Payload(byte[] utf8)
    {
        Utf8 = utf8;
    }

    public byte[] Utf8 { get; }
}

[ZeroSerializer]
public sealed class ArrayRoundTripRecord
{
    public bool[] Booleans { get; init; } = Array.Empty<bool>();

    public byte[] Bytes { get; init; } = Array.Empty<byte>();

    public sbyte[] SignedBytes { get; init; } = Array.Empty<sbyte>();

    public char[] Characters { get; init; } = Array.Empty<char>();

    public short[] Int16s { get; init; } = Array.Empty<short>();

    public ushort[] UInt16s { get; init; } = Array.Empty<ushort>();

    public int[] Int32s { get; init; } = Array.Empty<int>();

    public uint[] UInt32s { get; init; } = Array.Empty<uint>();

    public long[] Int64s { get; init; } = Array.Empty<long>();

    public ulong[] UInt64s { get; init; } = Array.Empty<ulong>();

    public float[] Singles { get; init; } = Array.Empty<float>();

    public double[] Doubles { get; init; } = Array.Empty<double>();

    public ByteState[] ByteStates { get; init; } = Array.Empty<ByteState>();

    public SignedState[] SignedStates { get; init; } = Array.Empty<SignedState>();

    public PackedRecord[] PackedRecords { get; init; } = Array.Empty<PackedRecord>();
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
[ZeroSerializer]
public struct StrictBlittableStruct
{
    public int Value { get; init; }
}

[ZeroSerializer]
public record SimpleCsharpRecord
{
    public int IntValue { get; init; }
    public double DoubleValue { get; init; }
}

[ZeroSerializer]
public record struct SimpleRecordStruct
{
    public int IntValue { get; init; }
    public double DoubleValue { get; init; }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
[ZeroSerializer]
public record struct SimpleBlittableRecordStruct
{
    public int IntValue { get; init; }
    public double DoubleValue { get; init; }
}

[ZeroSerializer]
public sealed class VariableStructWithArrayAtEnd
{
    public int ID { get; init; }
    public int[]? Values { get; init; }
}

[ZeroSerializer]
public sealed class VariableStructWithStringAtEnd
{
    public int ID { get; init; }
    public string? Text { get; init; }
}

[ZeroSerializer]
public sealed class VariableStructWithBlittableStructAtEnd
{
    public string? Text { get; init; }
    public PackedRecord Blittable { get; init; }
}

[ZeroSerializer]
public sealed class VariableStructWithPrimitiveAtEnd
{
    public string? Text { get; init; }
    public int Value { get; init; }
}

[ZeroSerializer]
public sealed class VariableStructWithAllNullableFields
{
    public string? Text { get; init; }
    public int[]? Values { get; init; }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
[ZeroSerializer]
public struct EmptyBlittableStruct
{
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
[ZeroSerializer]
public sealed class SequentialPackOneClass
{
    public int Value { get; init; }
}

[ZeroSerializer(EmitShapeTag = true)]
public sealed class SchemaSignatureTestsModel
{
    // 1. blittable and non-blittable nested type combo
    public PackedRecord BlittableNested { get; init; }
    public FixedClass NonBlittableNested { get; init; }

    // 2. blittable struct array
    public PackedRecord[] BlittableStructArray { get; init; }

    // 3. empty class and empty blittable struct
    public EmptyClass EmptyClassValue { get; init; }
    public EmptyBlittableStruct EmptyBlittableStructValue { get; init; }

    // 4. non-blittable nullable struct
    public EnumStruct? NonBlittableNullableStruct { get; init; }

    // 5. enum array and flags enum array
    public ByteState[] EnumArray { get; init; }
    public UlongBackedOptions[] FlagsEnumArray { get; init; }

    // 6. nullable primitives
    public int? NullableInt { get; init; }
    public bool? NullableBool { get; init; }
}

[ZeroSerializer]
public sealed class AttributeSyntaxType1
{
    public int Value { get; init; }
}

[ZeroSerializerAttribute]
public sealed class AttributeSyntaxType2
{
    public int Value { get; init; }
}

[ZeroSerializer.ZeroSerializer]
public sealed class AttributeSyntaxType3
{
    public int Value { get; init; }
}

[ZeroSerializer.ZeroSerializerAttribute]
public sealed class AttributeSyntaxType4
{
    public int Value { get; init; }
}

[global::ZeroSerializer.ZeroSerializer]
public sealed class AttributeSyntaxType5
{
    public int Value { get; init; }
}

[global::ZeroSerializer.ZeroSerializerAttribute]
public sealed class AttributeSyntaxType6
{
    public int Value { get; init; }
}

[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
[ZeroSerializer]
public struct SequentialPackOneWithCharSetStruct
{
    public int Value { get; init; }
}

[StructLayout(LayoutKind.Sequential, Pack = 1, Size = 7)]
[ZeroSerializer]
public struct SequentialPackOneWithSizeStruct
{
    public int Value { get; init; }
}
