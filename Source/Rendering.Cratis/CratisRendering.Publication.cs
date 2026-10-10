// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using Cratis.Stage.Contracts.Rendering;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Stage.Rendering.Cratis;

public static partial class CratisRendering
{
    static readonly string[] _aggregatePolicyPaths = ["GeneratedPolicies/Policies.cs", "GeneratedPolicies/PolicyBodies.cs", "TypedContexts/PolicyContext.cs"];

    /// <summary>
    /// Checks the shared policy files a plan would overwrite before the publisher writes any artifacts.
    /// Application scope is always compatible; scoped plans require application scope over an aggregate policy layout.
    /// </summary>
    /// <param name="plan">The destination-independent plan, including its requested scope and artifact placement.</param>
    /// <param name="readExistingFile">
    /// Reads an application-root-relative path. Return null only when the file is confirmed absent; let any read failure
    /// throw, so the check fails closed instead of treating an unreadable aggregate policy file as absent.
    /// </param>
    /// <returns>A compatible result or the incompatible paths and the reason application scope is required.</returns>
    /// <remarks>
    /// Identical content is compatible. Otherwise, declaration syntax distinguishes the split layout from aggregate
    /// policy classes, per-site bodies and typed wrappers. Unrecognized content requires application scope.
    /// This check does not replace the publisher's ownership, concurrency, recovery or write-failure checks.
    /// </remarks>
    public static CratisPublicationCheck CheckPublication(ArtifactRenderPlan plan, Func<string, string?> readExistingFile)
    {
        if (plan.Scope.Kind == ArtifactRenderScopeKind.Application)
        {
            return new CratisPublicationCheck.Compatible();
        }

        var paths = plan.Artifacts.Where(artifact => AggregatePolicyPath(artifact.RelativePath) is not null)
            .Where(artifact => readExistingFile(artifact.RelativePath) is { } existing &&
                NormalizePolicyText(existing) != Encoding.UTF8.GetString(artifact.Bytes.AsSpan()) &&
                !IsSplitPolicyLayout(AggregatePolicyPath(artifact.RelativePath)!, existing))
            .Select(artifact => artifact.RelativePath).ToImmutableArray();

        return paths.IsEmpty
            ? new CratisPublicationCheck.Compatible()
            : new CratisPublicationCheck.RequiresApplicationScope(
                paths,
                "The destination contains an aggregate or unrecognized policy layout. Render the entire application before publishing a scoped plan so unselected operations retain their policies.");
    }

    static string NormalizePolicyText(string text) => text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');

    static string? AggregatePolicyPath(string path) => _aggregatePolicyPaths.FirstOrDefault(candidate => path == candidate || path.EndsWith($"/{candidate}", StringComparison.Ordinal));

    static bool IsSplitPolicyLayout(string path, string text)
    {
        var syntax = CSharpSyntaxTree.ParseText(text).GetCompilationUnitRoot();
        if (syntax.ContainsDiagnostics) return false;
        var declarations = syntax.DescendantNodes().OfType<TypeDeclarationSyntax>().ToArray();

        return path switch
        {
            "GeneratedPolicies/Policies.cs" =>
                !declarations.Any(declaration => declaration.Identifier.ValueText.StartsWith("StagePolicy_", StringComparison.Ordinal)) &&
                declarations.OfType<ClassDeclarationSyntax>().Any(declaration => declaration.Identifier.ValueText == "Registration" &&
                    declaration.Members.OfType<FieldDeclarationSyntax>().Any(field => field.Declaration.Variables.Any(variable => variable.Identifier.ValueText == "_policies")) &&
                    declaration.Members.OfType<MethodDeclarationSyntax>().Any(method => method.Identifier.ValueText == "Add") &&
                    declaration.Members.OfType<MethodDeclarationSyntax>().Any(method => method.Identifier.ValueText == "RegisterGenerated")),
            "GeneratedPolicies/PolicyBodies.cs" => declarations is [ClassDeclarationSyntax bodies] &&
                bodies.Identifier.ValueText == "PolicyBodies" && bodies.Modifiers.Any(token => token.RawKind == (int)SyntaxKind.PartialKeyword) && bodies.Members.Count == 0,
            "TypedContexts/PolicyContext.cs" =>
                !declarations.Any(declaration => declaration.Identifier.ValueText.StartsWith("TypedContext_", StringComparison.Ordinal)) &&
                declarations.Any(declaration => declaration.Identifier.ValueText == "Claim") &&
                declarations.Any(declaration => declaration.Identifier.ValueText == "Identity") &&
                declarations.Any(declaration => declaration.Identifier.ValueText == "PolicyContextValues"),
            _ => false
        };
    }
}
