// Licensed under the Apache-2.0 License
// https://github.com/sator-imaging/ZeroSerializer

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

#pragma warning disable CS1591  // Missing XML comment for publicly visible type or member

namespace ZeroSerializer.Generator;

internal sealed class SerializableTypeSyntaxReceiver : ISyntaxReceiver
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
