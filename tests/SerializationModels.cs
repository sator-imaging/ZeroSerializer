// Licensed under the Apache-2.0 License
// https://github.com/sator-imaging/ZeroSerializer

using System;
using System.Runtime.InteropServices;

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

[ZeroSerializer]
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

[ZeroSerializer]
public sealed class EnumClass
{
    public ByteState ByteState { get; init; }

    public SignedState SignedState { get; init; }
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
[ZeroSerializer]
public struct PackedRecord
{
    public int Number { get; init; }

    public SignedState State { get; init; }
}

[ZeroSerializer]
public sealed class PackedContainer
{
    public PackedRecord Value { get; init; }

    public PackedRecord? OptionalValue { get; init; }

    public PackedRecord[]? Values { get; init; }
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
public struct Utf8Payload
{
    public Utf8Payload(byte[] utf8)
    {
        Utf8 = utf8;
    }

    public byte[] Utf8 { get; }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
[ZeroSerializer]
public struct StrictBlittableStruct
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
