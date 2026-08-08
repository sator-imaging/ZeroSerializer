// Licensed under the Apache-2.0 License
// https://github.com/sator-imaging/ZeroSerializer

using System;
using System.Collections.Generic;
using Xunit;

#pragma warning disable SMA8003  // Do not use debug-only `Assert` in public API surface

namespace ZeroSerializer.Tests;

internal static class TestAssert
{
    internal static void Equal<T>(T expected, T actual, string assertionName)
    {
        Assert.True(
            EqualityComparer<T>.Default.Equals(expected, actual),
            $"{assertionName}: expected '{expected}', actual '{actual}'.");
    }

    internal static void True(bool condition, string assertionName)
    {
        Assert.True(condition, assertionName);
    }

    internal static void SequenceEqual<T>(ReadOnlySpan<T> expected, ReadOnlySpan<T> actual, string assertionName)
        where T : IEquatable<T>
    {
        Assert.True(expected.SequenceEqual(actual), $"{assertionName}: sequences differ.");
    }

    internal static void Throws<TException>(Action action, string assertionName)
        where TException : Exception
    {
        _ = Assert.Throws<TException>(action);
    }

    internal static void ThrowsStandardBoundsException(Action action, string assertionName)
    {
        Exception? thrownException = Record.Exception(action);
        Assert.True(
            thrownException is ArgumentException || thrownException is IndexOutOfRangeException,
            $"{assertionName}: expected a standard bounds exception, actual '{thrownException?.GetType().FullName ?? "no exception"}'.");
    }
}
