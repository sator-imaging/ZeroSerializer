// Licensed under the Apache-2.0 License
// https://github.com/sator-imaging/ZeroSerializer

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using System;
using System.Buffers.Binary;
using System.IO.Hashing;
using System.Runtime.InteropServices;
using ArmCrc32 = System.Runtime.Intrinsics.Arm.Crc32;
using X86Crc32 = System.Runtime.Intrinsics.X86.Sse42;

#pragma warning disable CS1591  // Missing XML comment for publicly visible type or member

BenchmarkRunner.Run<ZeroSerializer.Benchmarks.ZeroSerializerBenchmarks>();

namespace ZeroSerializer.Benchmarks
{
    // These enum values exercise distinct underlying wire types in both serialize and View benchmarks.
    public enum ByteBenchmarkStatus : byte
    {
        Ready = 1,
        Complete = 2,
    }

    public enum IntBenchmarkMode : int
    {
        Passive = -1,
        Active = 1,
    }

    [Flags]
    public enum UlongBenchmarkOptions : ulong
    {
        None = 0,
        First = 1UL << 40,
        Second = 1UL << 48,
        Audit = 1UL << 56,
    }

    // BenchmarkDotNet requires the benchmark type to remain inheritable and owns these class-level attributes.
    [MemoryDiagnoser]
    [InProcess]
    [MarkdownExporterAttribute.GitHub]
    public class ZeroSerializerBenchmarks
    {
        private const int CollectionLength = 1024;

        private BenchmarkPayload source = null!;
        private byte[] buffer = null!;
        private int writtenBytes;

        [GlobalSetup]
        public void Setup()
        {
            var random = new Random(0x51A7C0DE);
            var integers = new int[CollectionLength];
            var longs = new long[CollectionLength];
            random.NextBytes(MemoryMarshal.AsBytes(integers.AsSpan()));
            random.NextBytes(MemoryMarshal.AsBytes(longs.AsSpan()));

            var packedValues = new PackedBenchmarkValue[CollectionLength];
            for (int elementIndex = 0; elementIndex < CollectionLength; elementIndex++)
            {
                packedValues[elementIndex] = new PackedBenchmarkValue
                {
                    Number = integers[elementIndex],
                    Amount = longs[elementIndex],
                };
            }

            source = new BenchmarkPayload
            {
                Sequence = random.Next(),
                Timestamp = CreateRandomInt64(random),
                Enabled = random.Next(2) != 0,
                ByteStatus = random.Next(2) == 0 ? ByteBenchmarkStatus.Ready : ByteBenchmarkStatus.Complete,
                IntMode = random.Next(2) == 0 ? IntBenchmarkMode.Passive : IntBenchmarkMode.Active,
                UlongOptions = (random.Next(2) == 0 ? UlongBenchmarkOptions.First : UlongBenchmarkOptions.Second)
                | UlongBenchmarkOptions.Audit,
                FirstText = CreateRandomString(random, CollectionLength),
                SecondText = CreateRandomString(random, CollectionLength),
                Integers = integers,
                Longs = longs,
                PackedValues = packedValues,
                Nested = new NestedPayload
                {
                    Version = random.Next(),
                    Label = CreateRandomString(random, 128),
                    Summary = packedValues[0],
                },
                NestedStruct = new NestedStructPayload(
                    random.Next(),
                    CreateRandomInt64(random),
                    CreateRandomString(random, 128)),
            };
            buffer = new byte[128 * 1024];
            writtenBytes = source.Serialize(buffer);
        }

        [Benchmark(Baseline = true)]
        public int Serialize()
        {
            return source.Serialize(buffer);
        }

        [Benchmark]
        public BenchmarkPayloadView CreateView()
        {
            // Keep this benchmark constructor-only; property access belongs to DeserializeAllProperties.
            return new BenchmarkPayloadView(buffer.AsMemory(0, writtenBytes));
        }

        [Benchmark]
        public int DeserializeAllProperties()
        {
            var view = new BenchmarkPayloadView(buffer.AsMemory(0, writtenBytes));
            int sequence = view.Sequence;
            long timestamp = view.Timestamp;
            bool enabled = view.Enabled;
            ByteBenchmarkStatus byteStatus = view.ByteStatus;
            IntBenchmarkMode intMode = view.IntMode;
            UlongBenchmarkOptions ulongOptions = view.UlongOptions;
            ReadOnlySpan<char> firstText = view.FirstText;
            ReadOnlySpan<char> secondText = view.SecondText;
            ReadOnlySpan<int> integers = view.Integers;
            ReadOnlySpan<long> longs = view.Longs;
            ReadOnlySpan<PackedBenchmarkValue> packedValues = view.PackedValues;
            NestedPayloadView? nested = view.Nested;
            int nestedVersion = nested!.Value.Version;
            ReadOnlySpan<char> nestedLabel = nested!.Value.Label;
            PackedBenchmarkValueView nestedSummary = nested!.Value.Summary;
            NestedStructPayloadView nestedStruct = view.NestedStruct;
            int nestedStructCode = nestedStruct.Code;
            long nestedStructAmount = nestedStruct.Amount;
            ReadOnlySpan<char> nestedStructLabel = nestedStruct.Label;

            // Consume getter results without traversing or validating collection contents.
            unchecked
            {
                int consumedValue = sequence;
                consumedValue = (consumedValue * 397) ^ byteStatus.GetHashCode();
                consumedValue = (consumedValue * 397) ^ intMode.GetHashCode();
                consumedValue = (consumedValue * 397) ^ ulongOptions.GetHashCode();
                consumedValue = (consumedValue * 31) + timestamp.GetHashCode();
                consumedValue = (consumedValue * 31) + enabled.GetHashCode();
                consumedValue = (consumedValue * 31) + firstText.Length;
                consumedValue = (consumedValue * 31) + secondText.Length;
                consumedValue = (consumedValue * 31) + integers.Length;
                consumedValue = (consumedValue * 31) + longs.Length;
                consumedValue = (consumedValue * 31) + packedValues.Length;
                consumedValue = (consumedValue * 31) + nestedVersion;
                consumedValue = (consumedValue * 31) + nestedLabel.Length;
                consumedValue = (consumedValue * 31) + nestedSummary.Number;
                consumedValue = (consumedValue * 31) + nestedSummary.Amount.GetHashCode();
                consumedValue = (consumedValue * 31) + nestedStructCode;
                consumedValue = (consumedValue * 31) + nestedStructAmount.GetHashCode();
                consumedValue = (consumedValue * 31) + nestedStructLabel.Length;
                return consumedValue;
            }
        }

        [Benchmark]
        public uint CreateViewAndHashWithXxHash32()
        {
            var view = new BenchmarkPayloadView(buffer.AsMemory(0, writtenBytes));

            // Hash the View's complete borrowed byte region to emulate content validation without materializing the model.
            return XxHash32.HashToUInt32(view);
        }

        [Benchmark]
        public ulong CreateViewAndHashWithXxHash3()
        {
            var view = new BenchmarkPayloadView(buffer.AsMemory(0, writtenBytes));

            // Hash the View's complete borrowed byte region to emulate content validation without materializing the model.
            return XxHash3.HashToUInt64(view);
        }

        [Benchmark]
        public uint CreateViewAndHashWithCrc32()
        {
            var view = new BenchmarkPayloadView(buffer.AsMemory(0, writtenBytes));

            // Sse42.Crc32 computes CRC-32C despite its name. That distinction is irrelevant here because this benchmark only emulates whole-buffer corruption detection, so use the faster hardware instruction directly. Vectorized CRC-32C support in System.IO.Hashing starts in .NET 11 Preview 3.
            ReadOnlySpan<byte> serializedData = view;
            uint hash = uint.MaxValue;
            int processedByteCount = 0;

            if (X86Crc32.IsSupported)
            {
                if (X86Crc32.X64.IsSupported)
                {
                    while (processedByteCount <= serializedData.Length - sizeof(ulong))
                    {
                        hash = (uint)X86Crc32.X64.Crc32(hash, BinaryPrimitives.ReadUInt64LittleEndian(serializedData.Slice(processedByteCount)));
                        processedByteCount += sizeof(ulong);
                    }
                }

                while (processedByteCount <= serializedData.Length - sizeof(uint))
                {
                    hash = X86Crc32.Crc32(hash, BinaryPrimitives.ReadUInt32LittleEndian(serializedData.Slice(processedByteCount)));
                    processedByteCount += sizeof(uint);
                }

                while (processedByteCount < serializedData.Length)
                {
                    hash = X86Crc32.Crc32(hash, serializedData[processedByteCount]);
                    processedByteCount++;
                }

                return ~hash;
            }

            if (ArmCrc32.IsSupported)
            {
                if (ArmCrc32.Arm64.IsSupported)
                {
                    while (processedByteCount <= serializedData.Length - sizeof(ulong))
                    {
                        hash = ArmCrc32.Arm64.ComputeCrc32C(hash, BinaryPrimitives.ReadUInt64LittleEndian(serializedData.Slice(processedByteCount)));
                        processedByteCount += sizeof(ulong);
                    }
                }

                while (processedByteCount <= serializedData.Length - sizeof(uint))
                {
                    hash = ArmCrc32.ComputeCrc32C(hash, BinaryPrimitives.ReadUInt32LittleEndian(serializedData.Slice(processedByteCount)));
                    processedByteCount += sizeof(uint);
                }

                while (processedByteCount < serializedData.Length)
                {
                    hash = ArmCrc32.ComputeCrc32C(hash, serializedData[processedByteCount]);
                    processedByteCount++;
                }

                return ~hash;
            }

            return Crc32.HashToUInt32(serializedData);
        }

        private static string CreateRandomString(Random random, int length)
        {
            var characters = new char[length];
            for (int characterIndex = 0; characterIndex < characters.Length; characterIndex++)
            {
                characters[characterIndex] = (char)random.Next(char.MaxValue + 1);
            }

            return new string(characters);
        }

        private static long CreateRandomInt64(Random random)
        {
            // Random.NextInt64 is unavailable on .NET 5; setup data must remain identical across benchmark targets.
            var randomBytes = new byte[sizeof(long)];
            random.NextBytes(randomBytes);
            return BitConverter.ToInt64(randomBytes, 0);
        }
    }

    [ZeroSerializerAttribute]
    public sealed class BenchmarkPayload
    {
        public int Sequence { get; init; }

        public long Timestamp { get; init; }

        public bool Enabled { get; init; }

        public ByteBenchmarkStatus ByteStatus { get; init; }

        public IntBenchmarkMode IntMode { get; init; }

        public UlongBenchmarkOptions UlongOptions { get; init; }

        public string FirstText { get; init; } = string.Empty;

        public string SecondText { get; init; } = string.Empty;

        public int[] Integers { get; init; } = Array.Empty<int>();

        public long[] Longs { get; init; } = Array.Empty<long>();

        public PackedBenchmarkValue[] PackedValues { get; init; } = Array.Empty<PackedBenchmarkValue>();

        public NestedPayload Nested { get; init; } = new();

        public NestedStructPayload NestedStruct { get; init; }
    }

    [ZeroSerializerAttribute]
    public sealed class NestedPayload
    {
        public int Version { get; init; }

        public string Label { get; init; } = string.Empty;

        public PackedBenchmarkValue Summary { get; init; }
    }

    [ZeroSerializerAttribute]
    public readonly struct NestedStructPayload
    {
        public NestedStructPayload(int code, long amount, string label)
        {
            Code = code;
            Amount = amount;
            Label = label;
        }

        public int Code { get; }

        public long Amount { get; }

        public string Label { get; }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [ZeroSerializerAttribute]
    public struct PackedBenchmarkValue
    {
        public int Number { get; init; }

        public long Amount { get; init; }
    }
}
