// Licensed under the Apache-2.0 License
// https://github.com/sator-imaging/ZeroSerializer

using System;
using System.Text;

namespace ZeroSerializer;

/// <summary>
/// Provides a standalone, zero-dependency implementation of the xxHash32 algorithm.
/// </summary>
public static class XXHash32
{
    /// <summary>
    /// Computes the xxHash32 hash of the provided string as UTF-8 with seed 0.
    /// </summary>
    public static uint HashToUInt32(string text)
    {
        return HashToUInt32(Encoding.UTF8.GetBytes(text));
    }

    /// <summary>
    /// Computes the xxHash32 hash of the provided byte span with seed 0.
    /// </summary>
    public static uint HashToUInt32(ReadOnlySpan<byte> bytes)
    {
        unchecked
        {
            const uint PRIME32_1 = 0x9E3779B1U;
            const uint PRIME32_2 = 0x85EBCA77U;
            const uint PRIME32_3 = 0xC2B2AE3DU;
            const uint PRIME32_4 = 0x27D4EB2FU;
            const uint PRIME32_5 = 0x165667B1U;

            int len = bytes.Length;
            uint h32;
            int index = 0;

            if (len >= 16)
            {
                int limit = len - 16;
                uint v1 = 0 + PRIME32_1 + PRIME32_2;
                uint v2 = 0 + PRIME32_2;
                uint v3 = 0;
                uint v4 = 0 - PRIME32_1;

                while (index <= limit)
                {
                    v1 = Round(v1, Read32(bytes, index)); index += 4;
                    v2 = Round(v2, Read32(bytes, index)); index += 4;
                    v3 = Round(v3, Read32(bytes, index)); index += 4;
                    v4 = Round(v4, Read32(bytes, index)); index += 4;
                }

                h32 = RotateLeft(v1, 1) + RotateLeft(v2, 7) + RotateLeft(v3, 12) + RotateLeft(v4, 18);
            }
            else
            {
                h32 = 0 + PRIME32_5;
            }

            h32 += (uint)len;

            while (index <= len - 4)
            {
                h32 += Read32(bytes, index) * PRIME32_3;
                h32 = RotateLeft(h32, 17) * PRIME32_4;
                index += 4;
            }

            while (index < len)
            {
                h32 += bytes[index] * PRIME32_5;
                h32 = RotateLeft(h32, 11) * PRIME32_1;
                index++;
            }

            h32 ^= h32 >> 15;
            h32 *= PRIME32_2;
            h32 ^= h32 >> 13;
            h32 *= PRIME32_3;
            h32 ^= h32 >> 16;

            return h32;
        }
    }

    private static uint Round(uint acc, uint lane)
    {
        unchecked
        {
            acc += lane * 0x85EBCA77U; // PRIME32_2
            acc = RotateLeft(acc, 13);
            acc *= 0x9E3779B1U; // PRIME32_1
            return acc;
        }
    }

    private static uint RotateLeft(uint value, int count)
    {
        return (value << count) | (value >> (32 - count));
    }

    private static uint Read32(ReadOnlySpan<byte> bytes, int offset)
    {
        return bytes[offset] |
               ((uint)bytes[offset + 1] << 8) |
               ((uint)bytes[offset + 2] << 16) |
               ((uint)bytes[offset + 3] << 24);
    }
}
