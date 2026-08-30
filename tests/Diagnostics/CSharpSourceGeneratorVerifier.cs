// Licensed under the Apache-2.0 License
// https://github.com/sator-imaging/ZeroSerializer

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CS1591  // Missing XML comment for publicly visible type or member

namespace ZeroSerializer.Tests.Diagnostics;

public static class CSharpSourceGeneratorVerifier<TGenerator>
    where TGenerator : ISourceGenerator, new()
{
    public static async Task VerifySourceGeneratorAsync(string source, params DiagnosticResult[] expected)
    {
        Test test = new Test
        {
            TestState =
            {
                Sources = { source },
            },
            TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
        };

        test.ExpectedDiagnostics.AddRange(expected);
        await test.RunAsync(CancellationToken.None);
    }

    public class Test : CSharpSourceGeneratorTest<TGenerator, XUnitVerifier>
    {
        public Test()
        {
            TestState.AdditionalReferences.Add(MetadataReference.CreateFromFile(typeof(ZeroSerializerAttribute).Assembly.Location));
        }
    }
}
