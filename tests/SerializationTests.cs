// Licensed under the Apache-2.0 License
// https://github.com/sator-imaging/ZeroSerializer

using System;
using System.Buffers.Binary;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Xunit;
using ZeroSerializer.Tests.Models;
using ZeroSerializer;

#pragma warning disable CS1591  // Missing XML comment for publicly visible type or member
#pragma warning disable SMA8003  // Do not use debug-only `Assert` in public API surface

namespace ZeroSerializer.Tests;

public sealed class SerializationTests
{
    [Fact]
    public void PrimitiveRoundTrip()
    {
        var source = new PrimitiveRecord
        {
            Boolean = true,
            Byte = 0xAB,
            SignedByte = -12,
            Character = '界',
            Int16 = -1234,
            UInt16 = 54321,
            Int32 = -12345678,
            UInt32 = 3456789012,
            Int64 = -123456789012345,
            UInt64 = 12345678901234567890,
            Single = 1.25f,
            Double = -123.5,
        };
        var buffer = new byte[PrimitiveRecordView.RequiredByteLength];

        int writtenBytes = source.Serialize(buffer);
        var view = new PrimitiveRecordView(buffer);

        TestAssert.Equal(PrimitiveRecordView.RequiredByteLength, writtenBytes, nameof(writtenBytes));
        TestAssert.Equal(source.Boolean, view.Boolean, nameof(view.Boolean));
        TestAssert.Equal(source.Byte, view.Byte, nameof(view.Byte));
        TestAssert.Equal(source.SignedByte, view.SignedByte, nameof(view.SignedByte));
        TestAssert.Equal(source.Character, view.Character, nameof(view.Character));
        TestAssert.Equal(source.Int16, view.Int16, nameof(view.Int16));
        TestAssert.Equal(source.UInt16, view.UInt16, nameof(view.UInt16));
        TestAssert.Equal(source.Int32, view.Int32, nameof(view.Int32));
        TestAssert.Equal(source.UInt32, view.UInt32, nameof(view.UInt32));
        TestAssert.Equal(source.Int64, view.Int64, nameof(view.Int64));
        TestAssert.Equal(source.UInt64, view.UInt64, nameof(view.UInt64));
        TestAssert.Equal(source.Single, view.Single, nameof(view.Single));
        TestAssert.Equal(source.Double, view.Double, nameof(view.Double));
    }

    [Fact]
    public void EnumClassAndStructRoundTrip()
    {
        var classSource = new EnumClass { ByteState = ByteState.Ready, SignedState = SignedState.Negative };
        var classBuffer = new byte[EnumClassView.RequiredByteLength];
        classSource.Serialize(classBuffer);
        var classView = new EnumClassView(classBuffer);

        TestAssert.Equal(ByteState.Ready, classView.ByteState, nameof(classView.ByteState));
        TestAssert.Equal(SignedState.Negative, classView.SignedState, nameof(classView.SignedState));

        var structSource = new EnumStruct(ByteState.Ready, SignedState.Positive);
        var structBuffer = new byte[EnumStructView.RequiredByteLength];
        structSource.Serialize(structBuffer);
        var structView = new EnumStructView(structBuffer);

        TestAssert.Equal(ByteState.Ready, structView.ByteState, nameof(structView.ByteState));
        TestAssert.Equal(SignedState.Positive, structView.SignedState, nameof(structView.SignedState));
    }

    [Fact]
    public void BlittableStructRoundTrip()
    {
        var source = new PackedRecord { Number = 123456, State = SignedState.Negative };
        var buffer = new byte[PackedRecordView.RequiredByteLength];

        int writtenBytes = source.Serialize(buffer);
        var view = new PackedRecordView(buffer);

        TestAssert.Equal(6, PackedRecordView.RequiredByteLength, nameof(PackedRecordView.RequiredByteLength));
        TestAssert.Equal(PackedRecordView.RequiredByteLength, writtenBytes, nameof(writtenBytes));
        TestAssert.Equal(source.Number, view.Number, nameof(view.Number));
        TestAssert.Equal(source.State, view.State, nameof(view.State));
    }

    [Fact]
    public void BlittableNestedNullableAndArrayRoundTrip()
    {
        var first = new PackedRecord { Number = 10, State = SignedState.Negative };
        var second = new PackedRecord { Number = 20, State = SignedState.Positive };
        var source = new PackedContainer
        {
            Value = first,
            OptionalValue = second,
            Values = new[] { first, second },
        };
        var buffer = new byte[128];

        int writtenBytes = source.Serialize(buffer);
        var view = new PackedContainerView(buffer.AsMemory(0, writtenBytes));

        TestAssert.Equal(first.Number, view.Value.Number, nameof(view.Value));
        TestAssert.Equal(second.Number, view.OptionalValue!.Value.Number, nameof(view.OptionalValue));
        TestAssert.Equal(2, view.Values.Length, nameof(view.Values.Length));
        TestAssert.Equal(first.Number, view.Values[0].Number, "Values[0]");
        TestAssert.Equal(second.State, view.Values[1].State, "Values[1]");
    }

    [Fact]
    public void RandomLength1013BlittableArraysAndStringsRoundTrip()
    {
        // Avoid power-of-two values, which **might** accidentally satisfy test conditions.
        const int elementCount = 1013;

        // A fixed seed keeps failures reproducible while every serialized element still receives a random value.
        var random = new Random(0x51A7C0DE);
        var firstCharacters = new char[elementCount];
        var secondCharacters = new char[elementCount];
        for (int elementIndex = 0; elementIndex < elementCount; elementIndex++)
        {
            firstCharacters[elementIndex] = (char)random.Next(char.MaxValue + 1);
            secondCharacters[elementIndex] = (char)random.Next(char.MaxValue + 1);
        }

        var bytes = new byte[elementCount];
        var integers = new int[elementCount];
        var longs = new long[elementCount];
        random.NextBytes(bytes);
        random.NextBytes(MemoryMarshal.AsBytes(integers.AsSpan()));
        random.NextBytes(MemoryMarshal.AsBytes(longs.AsSpan()));

        var packedRecords = new PackedRecord[elementCount];
        for (int elementIndex = 0; elementIndex < elementCount; elementIndex++)
        {
            packedRecords[elementIndex] = new PackedRecord
            {
                Number = integers[elementIndex],
                State = (SignedState)(short)random.Next(short.MinValue, short.MaxValue + 1),
            };
        }

        var source = new LargeRandomRecord
        {
            FirstText = new string(firstCharacters),
            SecondText = new string(secondCharacters),
            Bytes = bytes,
            Integers = integers,
            Longs = longs,
            PackedRecords = packedRecords,
        };
        var buffer = new byte[64 * 1024];

        int writtenBytes = source.Serialize(buffer);
        var view = new LargeRandomRecordView(buffer.AsMemory(0, writtenBytes));

        int expectedWrittenBytes = (6 * sizeof(int))
            + (2 * (sizeof(int) + (elementCount * sizeof(char))))
            + sizeof(int) + (elementCount * sizeof(byte))
            + sizeof(int) + (elementCount * sizeof(int))
            + sizeof(int) + (elementCount * sizeof(long))
            + sizeof(int) + (elementCount * PackedRecordView.RequiredByteLength);
        TestAssert.Equal(expectedWrittenBytes, writtenBytes, nameof(writtenBytes));
        TestAssert.Equal(-((6 * sizeof(int)) + (6 * IntPtr.Size)), LargeRandomRecordView.RequiredByteLength, nameof(LargeRandomRecordView.RequiredByteLength));
        TestAssert.Equal(elementCount, view.FirstText.Length, nameof(view.FirstText.Length));
        TestAssert.Equal(elementCount, view.SecondText.Length, nameof(view.SecondText.Length));
        TestAssert.Equal(elementCount, view.Bytes.Length, nameof(view.Bytes.Length));
        TestAssert.Equal(elementCount, view.Integers.Length, nameof(view.Integers.Length));
        TestAssert.Equal(elementCount, view.Longs.Length, nameof(view.Longs.Length));
        TestAssert.Equal(elementCount, view.PackedRecords.Length, nameof(view.PackedRecords.Length));
        TestAssert.SequenceEqual<char>(source.FirstText.AsSpan(), view.FirstText, nameof(view.FirstText));
        TestAssert.SequenceEqual<char>(source.SecondText.AsSpan(), view.SecondText, nameof(view.SecondText));
        TestAssert.SequenceEqual<byte>(source.Bytes, view.Bytes, nameof(view.Bytes));
        TestAssert.SequenceEqual<int>(source.Integers, view.Integers, nameof(view.Integers));
        TestAssert.SequenceEqual<long>(source.Longs, view.Longs, nameof(view.Longs));
        for (int elementIndex = 0; elementIndex < elementCount; elementIndex++)
        {
            TestAssert.Equal(source.PackedRecords[elementIndex].Number, view.PackedRecords[elementIndex].Number, $"PackedRecords[{elementIndex}].Number");
            TestAssert.Equal(source.PackedRecords[elementIndex].State, view.PackedRecords[elementIndex].State, $"PackedRecords[{elementIndex}].State");
        }
    }

    [Fact]
    public void FixedOffsetTableLayout()
    {
        var source = new FixedClass { Identifier = 0x10203040, State = ByteState.Ready };
        var buffer = new byte[FixedClassView.RequiredByteLength];

        int writtenBytes = source.Serialize(buffer);

        TestAssert.Equal(13, FixedClassView.RequiredByteLength, nameof(FixedClassView.RequiredByteLength));
        TestAssert.Equal(13, writtenBytes, nameof(writtenBytes));
        TestAssert.Equal(8, BinaryPrimitives.ReadInt32LittleEndian(buffer.AsSpan(0, 4)), "Identifier offset");
        TestAssert.Equal(12, BinaryPrimitives.ReadInt32LittleEndian(buffer.AsSpan(4, 4)), "State offset");
        TestAssert.Equal(source.Identifier, BinaryPrimitives.ReadInt32LittleEndian(buffer.AsSpan(8, 4)), "Identifier payload");
        TestAssert.Equal((byte)source.State, buffer[12], "State payload");
    }

    [Fact]
    public void VariableDataRoundTrip()
    {
        var source = new VariableRecord
        {
            Text = "日本語",
            Values = new[] { 1, -2, 3 },
            OptionalNumber = 42,
            Child = new FixedClass { Identifier = 99, State = ByteState.Ready },
            Tail = -7,
        };
        var buffer = new byte[256];

        int writtenBytes = source.Serialize(buffer);
        var view = new VariableRecordView(buffer.AsMemory(0, writtenBytes));

        int expectedRequiredByteLength = -(24 + (4 * IntPtr.Size));
        TestAssert.Equal(expectedRequiredByteLength, VariableRecordView.RequiredByteLength, nameof(VariableRecordView.RequiredByteLength));
        TestAssert.Equal(source.Text, view.Text.ToString(), nameof(view.Text));
        TestAssert.SequenceEqual<int>(source.Values, view.Values, nameof(view.Values));
        TestAssert.Equal(source.OptionalNumber, view.OptionalNumber, nameof(view.OptionalNumber));
        TestAssert.Equal(source.Child.Identifier, view.Child.Identifier, nameof(view.Child.Identifier));
        TestAssert.Equal(source.Child.State, view.Child.State, nameof(view.Child.State));
        TestAssert.Equal(source.Tail, view.Tail, nameof(view.Tail));
        int textFieldOffset = BinaryPrimitives.ReadInt32LittleEndian(buffer.AsSpan(0, 4));
        int valuesFieldOffset = BinaryPrimitives.ReadInt32LittleEndian(buffer.AsSpan(4, 4));
        int optionalNumberFieldOffset = BinaryPrimitives.ReadInt32LittleEndian(buffer.AsSpan(8, 4));
        int childFieldOffset = BinaryPrimitives.ReadInt32LittleEndian(buffer.AsSpan(12, 4));
        TestAssert.Equal(source.Text.Length * sizeof(char), BinaryPrimitives.ReadInt32LittleEndian(buffer.AsSpan(textFieldOffset, 4)), "String payload byte length");
        TestAssert.Equal(source.Values.Length * sizeof(int), BinaryPrimitives.ReadInt32LittleEndian(buffer.AsSpan(valuesFieldOffset, 4)), "Array payload byte length");
        TestAssert.Equal(source.OptionalNumber.Value, BinaryPrimitives.ReadInt32LittleEndian(buffer.AsSpan(optionalNumberFieldOffset, 4)), "Nullable payload without marker");
        TestAssert.Equal(8, BinaryPrimitives.ReadInt32LittleEndian(buffer.AsSpan(childFieldOffset, 4)), "Reference payload without marker");
    }

    [Fact]
    public void VariableViewOnlyRequiresCorrectSerializedStart()
    {
        const int serializedDataStartOffset = 17;
        var source = new VariableRecord
        {
            Text = "trailing buffer",
            Values = new[] { 1, -2, 3 },
            OptionalNumber = 42,
            Child = new FixedClass { Identifier = 99, State = ByteState.Ready },
            Tail = -7,
        };
        var containingBuffer = new byte[256];

        int writtenBytes = source.Serialize(containingBuffer.AsSpan(serializedDataStartOffset));
        ReadOnlyMemory<byte> serializedMemoryWithTrailingBytes = containingBuffer.AsMemory(serializedDataStartOffset);
        var view = new VariableRecordView(serializedMemoryWithTrailingBytes);

        // Variable layouts use relative offsets, so the correct start and sufficient backing bytes are enough for View access.
        TestAssert.Equal(source.Text, view.Text.ToString(), nameof(view.Text));
        TestAssert.SequenceEqual<int>(source.Values, view.Values, nameof(view.Values));
        TestAssert.Equal(source.OptionalNumber, view.OptionalNumber, nameof(view.OptionalNumber));
        TestAssert.Equal(source.Child.Identifier, view.Child.Identifier, nameof(view.Child.Identifier));
        TestAssert.Equal(source.Child.State, view.Child.State, nameof(view.Child.State));
        TestAssert.Equal(source.Tail, view.Tail, nameof(view.Tail));

        ReadOnlyMemory<byte> borrowedSerializedMemory = view;
        TestAssert.True(borrowedSerializedMemory.Length > writtenBytes, "Variable View retains trailing bytes");
        TestAssert.Equal(serializedMemoryWithTrailingBytes.Length, borrowedSerializedMemory.Length, "Variable View borrowed memory length");
    }

    [Fact]
    public void NullValuesRoundTrip()
    {
        var source = new VariableRecord
        {
            Text = null,
            Values = null,
            OptionalNumber = null,
            Child = null,
            Tail = 5,
        };
        var buffer = new byte[64];

        int writtenBytes = source.Serialize(buffer);
        var view = new VariableRecordView(buffer.AsMemory(0, writtenBytes));

        TestAssert.Equal(0, view.Text.Length, nameof(view.Text.Length));
        TestAssert.Equal(0, view.Values.Length, nameof(view.Values.Length));
        TestAssert.Equal<int?>(null, view.OptionalNumber, nameof(view.OptionalNumber));
        TestAssert.Equal(0, BinaryPrimitives.ReadInt32LittleEndian(buffer.AsSpan(0, 4)), "Null string offset");
        TestAssert.Equal(0, BinaryPrimitives.ReadInt32LittleEndian(buffer.AsSpan(4, 4)), "Null array offset");
        TestAssert.Equal(0, BinaryPrimitives.ReadInt32LittleEndian(buffer.AsSpan(8, 4)), "Null nullable offset");
        TestAssert.Equal(0, BinaryPrimitives.ReadInt32LittleEndian(buffer.AsSpan(12, 4)), "Null reference offset");
        TestAssert.Equal(20, BinaryPrimitives.ReadInt32LittleEndian(buffer.AsSpan(16, 4)), "Tail offset");
        TestAssert.Equal(24, writtenBytes, nameof(writtenBytes));
        TestAssert.Equal(5, view.Tail, nameof(view.Tail));
    }

    [Fact]
    public void NegativeStringLengthThrowsStandardRangeException()
    {
        var buffer = new byte[8];
        BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(0, 4), 4);
        BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(4, 4), -100);

        var view = new StringOnlyRecordView(buffer);

        TestAssert.Equal(-(4 + IntPtr.Size), StringOnlyRecordView.RequiredByteLength, nameof(StringOnlyRecordView.RequiredByteLength));
        // Negative payload lengths are corrupt data and must reach Span.Slice without normalization.
        TestAssert.Throws<ArgumentOutOfRangeException>(() => _ = view.Text.Length, "Negative string payload byte length");
    }

    [Fact]
    public void EmptyClassAndStruct()
    {
        var emptyClassBuffer = Span<byte>.Empty;
        var emptyStructBuffer = Span<byte>.Empty;

        int emptyClassWrittenBytes = new EmptyClass().Serialize(emptyClassBuffer);
        int emptyStructWrittenBytes = new EmptyStruct().Serialize(emptyStructBuffer);
        _ = new EmptyClassView(ReadOnlyMemory<byte>.Empty);
        _ = new EmptyStructView(ReadOnlyMemory<byte>.Empty);

        TestAssert.Equal(0, EmptyClassView.RequiredByteLength, nameof(EmptyClassView.RequiredByteLength));
        TestAssert.Equal(0, EmptyStructView.RequiredByteLength, nameof(EmptyStructView.RequiredByteLength));
        TestAssert.Equal(0, emptyClassWrittenBytes, nameof(emptyClassWrittenBytes));
        TestAssert.Equal(0, emptyStructWrittenBytes, nameof(emptyStructWrittenBytes));
    }

    [Fact]
    public void ZeroLengthNestedStructBetweenProperties()
    {
        var source = new ZeroLengthNestedStructContainer
        {
            Before = 123,
            Empty = new EmptyStruct(),
            After = 456,
        };
        var buffer = new byte[ZeroLengthNestedStructContainerView.RequiredByteLength];

        int writtenBytes = source.Serialize(buffer);
        var view = new ZeroLengthNestedStructContainerView(buffer);
        EmptyStructView emptyView = view.Empty;

        // A zero-byte nested payload and its following property intentionally share one offset.
        TestAssert.Equal(20, ZeroLengthNestedStructContainerView.RequiredByteLength, nameof(ZeroLengthNestedStructContainerView.RequiredByteLength));
        TestAssert.Equal(20, writtenBytes, nameof(writtenBytes));
        TestAssert.Equal(12, BinaryPrimitives.ReadInt32LittleEndian(buffer.AsSpan(0, 4)), "Before offset");
        TestAssert.Equal(16, BinaryPrimitives.ReadInt32LittleEndian(buffer.AsSpan(4, 4)), "Empty offset");
        TestAssert.Equal(16, BinaryPrimitives.ReadInt32LittleEndian(buffer.AsSpan(8, 4)), "After offset");
        TestAssert.Equal(0, EmptyStructView.RequiredByteLength, nameof(EmptyStructView.RequiredByteLength));
        TestAssert.Equal(source.Before, view.Before, nameof(view.Before));
        TestAssert.Equal(source.After, view.After, nameof(view.After));
        _ = emptyView;
    }

    [Fact]
    public void PublicFieldsAreIgnored()
    {
        var source = new FieldsOnlyClass { IgnoredField = 123 };

        int writtenBytes = source.Serialize(Span<byte>.Empty);

        TestAssert.Equal(0, FieldsOnlyClassView.RequiredByteLength, nameof(FieldsOnlyClassView.RequiredByteLength));
        TestAssert.Equal(0, writtenBytes, nameof(writtenBytes));
        TestAssert.Equal(0, typeof(FieldsOnlyClassView).GetProperties().Length, "Generated property count");
    }

    [Fact]
    public void PropertyGetterVariants()
    {
        var source = new PropertyVariants(11, 22, 33);
        var buffer = new byte[PropertyVariantsView.RequiredByteLength];

        int writtenBytes = source.Serialize(buffer);
        var view = new PropertyVariantsView(buffer);

        TestAssert.Equal(16, PropertyVariantsView.RequiredByteLength, nameof(PropertyVariantsView.RequiredByteLength));
        TestAssert.Equal(16, writtenBytes, nameof(writtenBytes));
        TestAssert.Equal(11, view.PrivateSetter, nameof(view.PrivateSetter));
        TestAssert.Equal(22, view.GetterOnly, nameof(view.GetterOnly));
        TestAssert.True(typeof(PropertyVariantsView).GetProperty("PrivateGetter") is null, "Private getter ignored");
    }

    [Fact]
    public void EveryInsufficientWriteBufferThrowsStandardBoundsException()
    {
        var fixedSource = new FixedClass { Identifier = 1, State = ByteState.Ready };
        AssertEveryInsufficientWriteBufferThrows(
            FixedClassView.RequiredByteLength,
            destination => _ = fixedSource.Serialize(destination),
            nameof(FixedClass));

        var packedValue = new PackedRecord { Number = 10, State = SignedState.Positive };
        AssertEveryInsufficientWriteBufferThrows(
            PackedRecordView.RequiredByteLength,
            destination => _ = packedValue.Serialize(destination),
            nameof(PackedRecord));

        var packedContainerSource = new PackedContainer
        {
            Value = packedValue,
            OptionalValue = packedValue,
            Values = new[] { packedValue, packedValue },
        };
        var packedContainerBuffer = new byte[128];
        int packedContainerSerializedByteLength = packedContainerSource.Serialize(packedContainerBuffer);
        AssertEveryInsufficientWriteBufferThrows(
            packedContainerSerializedByteLength,
            destination => _ = packedContainerSource.Serialize(destination),
            nameof(PackedContainer));

        var variableSource = new VariableRecord
        {
            Text = "buffer coverage",
            Values = new[] { 1, 2, 3 },
            OptionalNumber = 42,
            Child = fixedSource,
            Tail = -7,
        };
        var variableBuffer = new byte[256];
        int variableSerializedByteLength = variableSource.Serialize(variableBuffer);
        AssertEveryInsufficientWriteBufferThrows(
            variableSerializedByteLength,
            destination => _ = variableSource.Serialize(destination),
            nameof(VariableRecord));
    }

    [Fact]
    public void EveryTruncatedSerializedBufferThrowsStandardBoundsExceptionWhenRead()
    {
        var fixedSource = new FixedClass { Identifier = 1, State = ByteState.Ready };
        var fixedBuffer = new byte[FixedClassView.RequiredByteLength];
        int fixedSerializedByteLength = fixedSource.Serialize(fixedBuffer);
        AssertEveryTruncatedReadBufferThrows(
            fixedBuffer,
            fixedSerializedByteLength,
            serializedMemory =>
            {
                var view = new FixedClassView(serializedMemory);
                _ = view.Identifier;
                _ = view.State;
            },
            nameof(FixedClass));

        var packedValue = new PackedRecord { Number = 10, State = SignedState.Positive };
        var packedValueBuffer = new byte[PackedRecordView.RequiredByteLength];
        int packedValueSerializedByteLength = packedValue.Serialize(packedValueBuffer);
        AssertEveryTruncatedReadBufferThrows(
            packedValueBuffer,
            packedValueSerializedByteLength,
            serializedMemory =>
            {
                var view = new PackedRecordView(serializedMemory);
                _ = view.Number;
                _ = view.State;
            },
            nameof(PackedRecord));

        var packedContainerSource = new PackedContainer
        {
            Value = packedValue,
            OptionalValue = packedValue,
            Values = new[] { packedValue, packedValue },
        };
        var packedContainerBuffer = new byte[128];
        int packedContainerSerializedByteLength = packedContainerSource.Serialize(packedContainerBuffer);
        AssertEveryTruncatedReadBufferThrows(
            packedContainerBuffer,
            packedContainerSerializedByteLength,
            serializedMemory =>
            {
                var view = new PackedContainerView(serializedMemory);
                _ = view.Value.Number;
                _ = view.OptionalValue!.Value.Number;
                _ = view.Values.Length;
            },
            nameof(PackedContainer));

        var variableSource = new VariableRecord
        {
            Text = "buffer coverage",
            Values = new[] { 1, 2, 3 },
            OptionalNumber = 42,
            Child = fixedSource,
            Tail = -7,
        };
        var variableBuffer = new byte[256];
        int variableSerializedByteLength = variableSource.Serialize(variableBuffer);
        AssertEveryTruncatedReadBufferThrows(
            variableBuffer,
            variableSerializedByteLength,
            serializedMemory =>
            {
                var view = new VariableRecordView(serializedMemory);
                _ = view.Text.Length;
                _ = view.Values.Length;
                _ = view.OptionalNumber;
                FixedClassView childView = view.Child;
                _ = childView.Identifier;
                _ = childView.State;
                _ = view.Tail;
            },
            nameof(VariableRecord));
    }

    [Fact]
    public void FixedViewConversionsRejectInsufficientMemory()
    {
        ReadOnlyMemory<byte> insufficientMemory = new byte[FixedClassView.RequiredByteLength - 1];
        var view = new FixedClassView(insufficientMemory);

        // Fixed View conversions alone know the complete required region and therefore Slice it at conversion time.
        TestAssert.ThrowsStandardBoundsException(
            () =>
            {
                ReadOnlySpan<byte> serializedSpan = view;
                _ = serializedSpan.Length;
            },
            "ReadOnlySpan conversion");
        TestAssert.ThrowsStandardBoundsException(
            () =>
            {
                ReadOnlyMemory<byte> serializedMemory = view;
                _ = serializedMemory.Length;
            },
            "ReadOnlyMemory conversion");
    }

    private static void AssertEveryInsufficientWriteBufferThrows(
        int serializedByteLength,
        Action<byte[]> serialize,
        string modelName)
    {
        // A new destination for every length isolates buffer capacity from partial writes made before the native failure.
        for (int availableByteLength = 0; availableByteLength < serializedByteLength; availableByteLength++)
        {
            var insufficientBuffer = new byte[availableByteLength];
            TestAssert.ThrowsStandardBoundsException(
                () => serialize(insufficientBuffer),
                $"{modelName} write with {availableByteLength} of {serializedByteLength} bytes");
        }
    }

    private static void AssertEveryTruncatedReadBufferThrows(
        byte[] completeSerializedBuffer,
        int serializedByteLength,
        Action<ReadOnlyMemory<byte>> readAllProperties,
        string modelName)
    {
        // Every input is a prefix of serializer-produced data, so truncation is the only invalid condition under test.
        for (int availableByteLength = 0; availableByteLength < serializedByteLength; availableByteLength++)
        {
            ReadOnlyMemory<byte> truncatedSerializedMemory = completeSerializedBuffer.AsMemory(0, availableByteLength);
            TestAssert.ThrowsStandardBoundsException(
                () => readAllProperties(truncatedSerializedMemory),
                $"{modelName} read with {availableByteLength} of {serializedByteLength} bytes");
        }
    }

    [Fact]
    public void UnpredictableViewConstructionIsNotPrechecked()
    {
        _ = new VariableRecordView(ReadOnlyMemory<byte>.Empty);
    }

    [Fact]
    public void SerializeSignatureAndReceiverModifiers()
    {
        MethodInfo classSerialize = GetSerializeMethod(typeof(FixedClass));
        MethodInfo smallStructSerialize = GetSerializeMethod(typeof(SmallFixedStruct));
        MethodInfo largeStructSerialize = GetSerializeMethod(typeof(LargeFixedStruct));

        TestAssert.Equal(typeof(Span<byte>), classSerialize.GetParameters()[1].ParameterType, "Serialize buffer type");
        TestAssert.True(!classSerialize.GetParameters()[0].ParameterType.IsByRef, "Class receiver by value");
        TestAssert.True(!smallStructSerialize.GetParameters()[0].ParameterType.IsByRef, "Small struct receiver by value");
#if NET8_0_OR_GREATER
        // MemoryMarshal.Write accepts a readonly input from .NET 8, allowing large struct receivers to remain readonly references.
        TestAssert.True(largeStructSerialize.GetParameters()[0].ParameterType.IsByRef, "Large struct receiver by readonly reference");
#else
        // MemoryMarshal.Write requires a writable ref through .NET 7, so the generated method must receive the struct by value.
        TestAssert.True(!largeStructSerialize.GetParameters()[0].ParameterType.IsByRef, "Large struct receiver by value");
#endif
    }

    [Fact]
    public void ByteArrayRoundTrip()
    {
        var source = new ByteArrayRecord
        {
            Payload = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }
        };
        var buffer = new byte[256];
        int writtenBytes = source.Serialize(buffer);
        var view = new ByteArrayRecordView(buffer.AsMemory(0, writtenBytes));

        TestAssert.Equal(source.Payload.Length, view.Payload.Length, "Payload length");
        TestAssert.SequenceEqual<byte>(source.Payload, view.Payload, "Payload content");
    }

    [Fact]
    public void ViewConstructorHasNoOffset()
    {
        ConstructorInfo constructor = typeof(FixedClassView).GetConstructors().Single();
        ParameterInfo[] parameters = constructor.GetParameters();

        TestAssert.Equal(1, parameters.Length, "View constructor parameter count");
        TestAssert.Equal(typeof(ReadOnlyMemory<byte>), parameters[0].ParameterType, "View constructor memory type");
    }

    [Fact]
    public void Utf8PayloadRoundTrip()
    {
        // 1. Create string
        string originalString = "Hello, UTF-8 World! 世界";

        // 2. Use Utf8Encoding to make it byte array
        byte[] utf8Bytes = System.Text.Encoding.UTF8.GetBytes(originalString);

        // 3. Set byte array to payload
        var source = new Utf8Payload(utf8Bytes);

        // 4. Serialize
        var buffer = new byte[256];
        int writtenBytes = source.Serialize(buffer);

        // 5. Deserialize
        var view = new Utf8PayloadView(buffer.AsMemory(0, writtenBytes));

        // 6. Decode deserialized byte[] as string
        ReadOnlySpan<byte> decodedBytes = view.Utf8;
        string decodedString = System.Text.Encoding.UTF8.GetString(decodedBytes);

        // 7. Verify the result
        TestAssert.Equal(originalString, decodedString, "Decoded UTF-8 string matches original");
    }

    [Fact]
    public void ArrayRoundTripTest()
    {
        // Avoid computer-loving number for the size that **might** accidentally satisfy the test condition.
        const int length = 31;
        var random = new Random(42);

        var booleans = new bool[length];
        var bytes = new byte[length];
        var signedBytes = new sbyte[length];
        var characters = new char[length];
        var int16s = new short[length];
        var uint16s = new ushort[length];
        var int32s = new int[length];
        var uint32s = new uint[length];
        var int64s = new long[length];
        var uint64s = new ulong[length];
        var singles = new float[length];
        var doubles = new double[length];
        var byteStates = new ByteState[length];
        var signedStates = new SignedState[length];
        var packedRecords = new PackedRecord[length];

        for (int i = 0; i < length; i++)
        {
            booleans[i] = random.Next(2) == 1;
            bytes[i] = (byte)random.Next(256);
            signedBytes[i] = (sbyte)random.Next(sbyte.MinValue, sbyte.MaxValue + 1);
            characters[i] = (char)random.Next(char.MinValue, char.MaxValue + 1);
            int16s[i] = (short)random.Next(short.MinValue, short.MaxValue + 1);
            uint16s[i] = (ushort)random.Next(ushort.MinValue, ushort.MaxValue + 1);
            int32s[i] = random.Next();
            uint32s[i] = (uint)random.Next();
            int64s[i] = ((long)random.Next() << 32) | (uint)random.Next();
            uint64s[i] = (ulong)(((long)random.Next() << 32) | (uint)random.Next());
            singles[i] = (float)random.NextDouble();
            doubles[i] = random.NextDouble();
            byteStates[i] = random.Next(2) == 0 ? ByteState.None : ByteState.Ready;
            signedStates[i] = random.Next(2) == 0 ? SignedState.Negative : SignedState.Positive;
            packedRecords[i] = new PackedRecord
            {
                Number = random.Next(),
                State = random.Next(2) == 0 ? SignedState.Negative : SignedState.Positive
            };
        }

        var source = new ArrayRoundTripRecord
        {
            Booleans = booleans,
            Bytes = bytes,
            SignedBytes = signedBytes,
            Characters = characters,
            Int16s = int16s,
            UInt16s = uint16s,
            Int32s = int32s,
            UInt32s = uint32s,
            Int64s = int64s,
            UInt64s = uint64s,
            Singles = singles,
            Doubles = doubles,
            ByteStates = byteStates,
            SignedStates = signedStates,
            PackedRecords = packedRecords,
        };

        var buffer = new byte[100 * 1024];
        int writtenBytes = source.Serialize(buffer);

        var view = new ArrayRoundTripRecordView(buffer.AsMemory(0, writtenBytes));

        TestAssert.SequenceEqual<bool>(source.Booleans, view.Booleans, nameof(view.Booleans));
        TestAssert.SequenceEqual<byte>(source.Bytes, view.Bytes, nameof(view.Bytes));
        TestAssert.SequenceEqual<sbyte>(source.SignedBytes, view.SignedBytes, nameof(view.SignedBytes));
        TestAssert.SequenceEqual<char>(source.Characters, view.Characters, nameof(view.Characters));
        TestAssert.SequenceEqual<short>(source.Int16s, view.Int16s, nameof(view.Int16s));
        TestAssert.SequenceEqual<ushort>(source.UInt16s, view.UInt16s, nameof(view.UInt16s));
        TestAssert.SequenceEqual<int>(source.Int32s, view.Int32s, nameof(view.Int32s));
        TestAssert.SequenceEqual<uint>(source.UInt32s, view.UInt32s, nameof(view.UInt32s));
        TestAssert.SequenceEqual<long>(source.Int64s, view.Int64s, nameof(view.Int64s));
        TestAssert.SequenceEqual<ulong>(source.UInt64s, view.UInt64s, nameof(view.UInt64s));
        TestAssert.SequenceEqual<float>(source.Singles, view.Singles, nameof(view.Singles));
        TestAssert.SequenceEqual<double>(source.Doubles, view.Doubles, nameof(view.Doubles));
        TestAssert.Equal(length, view.ByteStates.Length, nameof(view.ByteStates.Length));
        for (int i = 0; i < length; i++)
        {
            TestAssert.Equal(source.ByteStates[i], view.ByteStates[i], $"ByteStates[{i}]");
        }

        TestAssert.Equal(length, view.SignedStates.Length, nameof(view.SignedStates.Length));
        for (int i = 0; i < length; i++)
        {
            TestAssert.Equal(source.SignedStates[i], view.SignedStates[i], $"SignedStates[{i}]");
        }

        TestAssert.Equal(length, view.PackedRecords.Length, nameof(view.PackedRecords.Length));
        for (int i = 0; i < length; i++)
        {
            TestAssert.Equal(source.PackedRecords[i].Number, view.PackedRecords[i].Number, $"PackedRecords[{i}].Number");
            TestAssert.Equal(source.PackedRecords[i].State, view.PackedRecords[i].State, $"PackedRecords[{i}].State");
        }
    }

    [Fact]
    public void NestedTypesReturnViewsTest()
    {
        // 1. Assert that PackedContainerView properties return View types instead of the raw struct
        PropertyInfo? valueProperty = typeof(PackedContainerView).GetProperty(nameof(PackedContainerView.Value));
        Assert.NotNull(valueProperty);
        Assert.Equal(typeof(PackedRecordView), valueProperty!.PropertyType);

        PropertyInfo? optionalValueProperty = typeof(PackedContainerView).GetProperty(nameof(PackedContainerView.OptionalValue));
        Assert.NotNull(optionalValueProperty);
        Assert.Equal(typeof(PackedRecordView?), optionalValueProperty!.PropertyType);

        // 2. Also assert that a non-blittable nested reference type returns its View type
        PropertyInfo? childProperty = typeof(VariableRecordView).GetProperty(nameof(VariableRecordView.Child));
        Assert.NotNull(childProperty);
        Assert.Equal(typeof(FixedClassView), childProperty!.PropertyType);
    }

    public void StrictBlittableStructTests()
    {
        // StrictBlittableStruct has Sequential, Pack=1 and nothing else.
        // It must have NO offset table (is serialized directly as raw bytes, checking the serialized size).
        var strictObj = new StrictBlittableStruct { Value = 42 };
        var strictBuffer = new byte[16];
        int strictWrittenBytes = strictObj.Serialize(strictBuffer);
        // Size should be exactly 4 bytes (sizeof(int)) because it has no offset table.
        TestAssert.Equal(4, strictWrittenBytes, nameof(strictWrittenBytes));
        TestAssert.Equal(42, BinaryPrimitives.ReadInt32LittleEndian(strictBuffer.AsSpan(0, 4)), "Value field at offset 0");

        // SequentialPackOneWithCharSetStruct has Sequential, Pack=1, AND CharSet=CharSet.Ansi.
        // It must have an offset table (is serialized with property offset table, checking that its serialized size is larger).
        var charSetObj = new SequentialPackOneWithCharSetStruct { Value = 42 };
        var charSetBuffer = new byte[16];
        int charSetWrittenBytes = charSetObj.Serialize(charSetBuffer);
        // Size should be 8 bytes (4 bytes offset table + 4 bytes payload).
        TestAssert.Equal(8, charSetWrittenBytes, nameof(charSetWrittenBytes));
        TestAssert.Equal(4, BinaryPrimitives.ReadInt32LittleEndian(charSetBuffer.AsSpan(0, 4)), "Offset table at offset 0 points to 4");
        TestAssert.Equal(42, BinaryPrimitives.ReadInt32LittleEndian(charSetBuffer.AsSpan(4, 4)), "Value field at offset 4");

        // SequentialPackOneWithSizeStruct has Sequential, Pack=1, AND Size=7.
        // It must have an offset table.
        var sizeObj = new SequentialPackOneWithSizeStruct { Value = 42 };
        var sizeBuffer = new byte[16];
        int sizeWrittenBytes = sizeObj.Serialize(sizeBuffer);
        // Size should be 8 bytes (4 bytes offset table + 4 bytes payload).
        TestAssert.Equal(8, sizeWrittenBytes, nameof(sizeWrittenBytes));
        TestAssert.Equal(4, BinaryPrimitives.ReadInt32LittleEndian(sizeBuffer.AsSpan(0, 4)), "Offset table at offset 0 points to 4");
        TestAssert.Equal(42, BinaryPrimitives.ReadInt32LittleEndian(sizeBuffer.AsSpan(4, 4)), "Value field at offset 4");
    }

    private static MethodInfo GetSerializeMethod(Type sourceType)
    {
        return typeof(ZeroSerializerExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(method =>
            {
                if (method.Name != "Serialize")
                {
                    return false;
                }

                Type receiverType = method.GetParameters()[0].ParameterType;
                return receiverType == sourceType
                    || receiverType.IsByRef && receiverType.GetElementType() == sourceType;
            });
    }
}
