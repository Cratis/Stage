// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Cratis.Stage.Rendering.Cratis.Semantics.Policies;

/// <summary>
/// Admits an opaque policy body through the same closed-compilation <c language="csharp">pure</c> gate as reducer transitions,
/// against the exact namespace, class and signature the application compiles it in.
/// </summary>
internal static class PurePolicyAdmission
{
    internal static PureTransitionAdmission.Verdict Analyze(
        string body,
        SemanticApplicationContext context,
        SemanticTypedContextDescriptor descriptor,
        SemanticImplementationRequirement requirement)
    {
        // A policy body has no generated type to test against; every type test is refused.
        var parsed = PureTransitionAdmission.ParseBody(body, new HashSet<string>(StringComparer.Ordinal), "policy");
        if (parsed.Rejection is not null) return parsed.Rejection;

        var declarations = "global using System; global using System.Collections.Generic; global using System.Linq; " +
            "global using System.IO; global using System.Net.Http; global using System.Threading; global using System.Threading.Tasks;\n" +
            SemanticPolicyContextRuntime.AnalysisDeclarations(context, descriptor);
        var code = SemanticPolicyContextRuntime.RenderBodies(context, [(descriptor, body)], evaluators: false).Content;
        var options = new CSharpParseOptions(Microsoft.CodeAnalysis.CSharp.LanguageVersion.Latest);
        var tree = CSharpSyntaxTree.ParseText(code, options);
        var compilation = CSharpCompilation.Create(
            "PurePolicyAnalysis",
            [CSharpSyntaxTree.ParseText(declarations, options), tree],
            PureTransitionAdmission.AnalysisReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
        var model = compilation.GetSemanticModel(tree);
        var method = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Single(declaration => declaration.Identifier.Text == SemanticPolicyContextRuntime.Body(descriptor));

        // Name refused reads before compile errors, so `context.Artifact.Amount` reports the read, not CS1061.
        var refused = RefusedRead(method.Body!, model, context.RootNamespace);
        if (refused is not null) return Reject("STAGE-ESM-015", refused);

        // Enclosing generated namespaces outrank the SDK's implicit global usings and could rebind a type the body names.
        var referenced = PureTransitionAdmission.MethodTypeReferences(tree, model, SemanticPolicyContextRuntime.BodyPrefix);
        var shadow = context.NamespacePaths.GroupBy(path => path.Split('.')[^1], StringComparer.Ordinal).FirstOrDefault(group =>
            PureTransitionAdmission.ShadowsAuditedName(group.Key) ||
            referenced.Any(reference => reference.Name == group.Key && (reference.Namespace is null || group.Any(path => path != reference.Namespace))))?.Key;
        if (shadow is not null)
        {
            return Reject("STAGE-ESM-022", $"Generated namespace '{shadow}' shadows a type referenced by the policy body.");
        }

        var errors = compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray();
        if (errors.Length > 0)
        {
            var error = errors[0];
            var offset = code.IndexOf(body, code.IndexOf($"static bool {SemanticPolicyContextRuntime.Body(descriptor)}(", StringComparison.Ordinal), StringComparison.Ordinal);
            var detail = $"{error.Id}: {error.GetMessage(System.Globalization.CultureInfo.InvariantCulture)}";
            if (!error.Location.IsInSource || error.Location.SourceTree != tree || error.Location.SourceSpan.Start < offset ||
                error.Location.SourceSpan.Start > offset + body.Length)
            {
                return Reject("STAGE-ESM-021", $"Internal policy analysis compilation error: {detail}");
            }

            return Reject("STAGE-ESM-019", $"Policy body {PureTransitionAdmission.SourceLocation(requirement, body, error.Location.SourceSpan.Start - offset)}: {detail}");
        }

        var verdict = PureTransitionAdmission.Walk(model, method.Body!, descriptor);
        if (verdict.Accepted && verdict.ContextReads.Any(read => !SemanticPolicyContextRuntime.SuppliedMembers.Contains(read, StringComparer.Ordinal)))
        {
            return Reject("STAGE-ESM-015", "The policy body reads a PolicyContext member Stage does not supply.");
        }

        return verdict;
    }

    static PureTransitionAdmission.Verdict Reject(string code, string reason) => new(code, reason, [], []);

    static string? RefusedRead(BlockSyntax body, SemanticModel model, string rootNamespace)
    {
        var contexts = $"{rootNamespace}.TypedContexts";
        foreach (var name in body.DescendantNodes().OfType<SimpleNameSyntax>().Where(name => name is not IdentifierNameSyntax { IsVar: true }))
        {
            var symbol = model.GetSymbolInfo(name).Symbol;
            var owner = symbol?.ContainingType;
            if (symbol is IPropertySymbol && owner?.ContainingNamespace.ToDisplayString() == contexts)
            {
                if (owner.Name.StartsWith("TypedContext_", StringComparison.Ordinal) && SemanticPolicyContextRuntime.RefusedMembers.Contains(symbol.Name, StringComparer.Ordinal))
                {
                    return symbol.Name == "Tenant"
                        ? "The policy body reads context.Tenant; Stage does not supply PolicyContext.Tenant at Arc's authorization boundary."
                        : "The policy body reads context.Artifact; Stage does not supply the dynamic PolicyContext.Artifact.";
                }

                if (owner.Name == SemanticPolicyContextRuntime.IdentityType && SemanticPolicyContextRuntime.UnmappedIdentityMembers.Contains(symbol.Name, StringComparer.Ordinal))
                {
                    return $"The policy body reads context.Identity.{symbol.Name}, which has no defined mapping from Arc's authorization principal.";
                }
            }

            if (name.Identifier.ValueText == "ArtifactAs")
            {
                return "The policy body reads context.ArtifactAs; Stage does not supply the dynamic PolicyContext.Artifact.";
            }

            // Bodies read the context; they never name, construct or default Stage's runtime context types.
            if (symbol is INamedTypeSymbol type && type.ContainingNamespace.ToDisplayString() == contexts)
            {
                return $"The policy body names Stage's runtime context type '{type.Name}'.";
            }
        }

        // A runtime type can be constructed or defaulted without ever naming it (target-typed new,
        // with, or a default literal). Inspect the bound operations as well as explicit type names.
        foreach (var node in body.DescendantNodes())
        {
            var operation = model.GetOperation(node);
            if (operation is IObjectCreationOperation or IWithOperation or IDefaultValueOperation &&
                operation.Type is INamedTypeSymbol type && type.ContainingNamespace.ToDisplayString() == contexts)
            {
                return $"The policy body constructs or defaults Stage's runtime context type '{type.Name}'.";
            }
        }

        return null;
    }
}
