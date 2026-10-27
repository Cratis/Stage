// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Stage.Rendering.Cratis.CodeGeneration;

/// <summary>
/// Finds which generated bindings an authored body actually uses.
/// </summary>
internal static class CSharpBodyBindings
{
    /// <summary>
    /// Selects the candidate names a body references as simple names. An escaped identifier such as @class matches its unescaped name, and a
    /// member accessed through another object such as other.Title is not a reference.
    /// </summary>
    /// <param name="body">The authored statements.</param>
    /// <param name="candidates">The binding names the generated code could declare, already normalized to the emitted local names.</param>
    /// <returns>The referenced candidates, in candidate order, without duplicates.</returns>
    internal static IReadOnlyList<string> Referenced(string body, IEnumerable<string> candidates)
    {
        var names = candidates.Distinct(StringComparer.Ordinal).ToArray();
        if (names.Length == 0) return [];
        var used = SyntaxFactory.ParseStatement("{\n" + body + "\n}").DescendantNodes().OfType<IdentifierNameSyntax>()
            .Where(IsSimpleReference)
            .Select(name => name.Identifier.ValueText)
            .ToHashSet(StringComparer.Ordinal);
        return [.. names.Where(used.Contains)];
    }

    static bool IsSimpleReference(IdentifierNameSyntax name) => name.Parent switch
    {
        MemberAccessExpressionSyntax access => access.Name != name,
        MemberBindingExpressionSyntax => false,
        NameColonSyntax => false,
        NameEqualsSyntax => false,
        AssignmentExpressionSyntax { Parent: InitializerExpressionSyntax } assignment => assignment.Left != name,
        _ => true
    };
}
