// Licensed under the Apache-2.0 License
// https://github.com/sator-imaging/ZeroSerializer

using System;
using Xunit;
using ZeroSerializer.Generator;

#pragma warning disable CS1591  // Missing XML comment for publicly visible type or member

namespace ZeroSerializer.Tests;

// Test source: https://github.com/Cyan4973/xxHash/blob/v0.8.3/cli/xsum_sanity_check.c#L100-L110
public sealed class XXHash32Tests
{
    private const uint PRIME32 = 2654435761U;
    private const ulong PRIME64 = 11400714785074694797UL;

    [Fact]
    public void SanityCheck()
    {
        byte[] sanityBuffer = new byte[222];
        FillTestBuffer(sanityBuffer);

        // Test vectors for seed = 0
        Assert.Equal(0x02CC5D05U, XXHash32.HashToUInt32(ReadOnlySpan<byte>.Empty));
        Assert.Equal(0xCF65B03EU, XXHash32.HashToUInt32(sanityBuffer.AsSpan(0, 1)));
        Assert.Equal(0x1208E7E2U, XXHash32.HashToUInt32(sanityBuffer.AsSpan(0, 14)));
        Assert.Equal(0x5BD11DBDU, XXHash32.HashToUInt32(sanityBuffer.AsSpan(0, 222)));
    }

    private static void FillTestBuffer(byte[] buffer)
    {
        unchecked
        {
            ulong byteGen = PRIME32;
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = (byte)(byteGen >> 56);
                byteGen *= PRIME64;
            }
        }
    }
}
