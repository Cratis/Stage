// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Stage.Rendering.Cratis.Semantics.Validation;

/// <summary>
/// Admits validation bodies against the same app-local context and body declarations that are emitted.
/// </summary>
internal static class PureValidationAdmission
{
    internal static PureTransitionAdmission.Verdict Analyze(string body, SemanticApplicationContext context, SemanticTypedContextDescriptor descriptor, SemanticImplementationRequirement requirement)
    {
        var generated = context.Application.Concepts.Select(concept => Naming.Identifiers.ToPascalCase(concept.Name))
            .Concat(context.Application.Types.Select(type => Naming.Identifiers.ToPascalCase(type.Name))).ToHashSet(StringComparer.Ordinal);
        var parsed = PureTransitionAdmission.ParseBody(body, generated, "validation", validationIterator: SemanticValidationContextRuntime.IsIterator(descriptor));
        if (parsed.Rejection is not null) return parsed.Rejection;
        var code = SemanticValidationContextRuntime.RenderBodies(context, [(descriptor, body)], analysis: true).Content;
        var options = new CSharpParseOptions(Microsoft.CodeAnalysis.CSharp.LanguageVersion.Latest);
        var tree = CSharpSyntaxTree.ParseText(code, options);
        var declarations = "global using System; global using System.Collections.Generic; global using System.Linq; global using System.IO; global using System.Net.Http; global using System.Threading; global using System.Threading.Tasks;\n" + SemanticValidationContextRuntime.AnalysisDeclarations(context);
        MetadataReference[] metadataReferences;
        try
        {
            metadataReferences = PureTransitionAdmission.AnalysisReferences();
        }
        catch (InvalidOperationException exception)
        {
            return Reject("STAGE-ESM-021", exception.Message);
        }

        var compilation = CSharpCompilation.Create(
            "PureValidationAnalysis",
            [CSharpSyntaxTree.ParseText(declarations, options), CSharpSyntaxTree.ParseText(SemanticValidationContextRuntime.RenderWrapper(context, descriptor, analysis: true).Content, options), tree],
            metadataReferences,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
        var model = compilation.GetSemanticModel(tree);
        var method = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single();
        foreach (var access in method.Body!.DescendantNodes().OfType<SimpleNameSyntax>())
        {
            if (model.GetSymbolInfo(access).Symbol is IPropertySymbol property && property.ContainingType.Name == SemanticValidationContextRuntime.Wrapper(descriptor) &&
                SemanticValidationContextRuntime.RefusedMembers.Contains(property.Name, StringComparer.Ordinal))
            {
                return Reject("STAGE-ESM-015", $"The validation body reads context.{property.Name}; dependency-free validators do not supply RuleContext.{property.Name}.");
            }
        }
        var references = PureTransitionAdmission.MethodTypeReferences(tree, model, SemanticValidationContextRuntime.Method(descriptor));
        var peers = context.Application.Concepts.Select(concept => Naming.Identifiers.ToPascalCase(concept.Name))
            .Concat(context.Application.Types.Select(type => Naming.Identifiers.ToPascalCase(type.Name)));
        var shadow = peers.FirstOrDefault(name => references.Any(reference => reference.Name == name && PureTransitionAdmission.ShadowsAuditedName(name)));
        var namespaceShadow = context.NamespacePaths.GroupBy(path => path.Split('.')[^1], StringComparer.Ordinal).FirstOrDefault(group =>
            references.Any(reference => reference.Name == group.Key && (reference.Namespace is null || group.Any(path => path != reference.Namespace))))?.Key;
        if (shadow is not null || namespaceShadow is not null) return Reject("STAGE-ESM-022", $"Generated declaration '{shadow ?? namespaceShadow}' shadows a validation body type.");
        var error = compilation.GetDiagnostics().FirstOrDefault(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error || (diagnostic.Severity == DiagnosticSeverity.Warning && !diagnostic.IsSuppressed));
        if (error is not null)
        {
            var location = CSharpInlineIndentation.Locate(code, body, method.Body.OpenBraceToken.Span.End);
            var detail = $"{error.Id}: {error.GetMessage(CultureInfo.InvariantCulture)}";
            if (location is null || error.Location.SourceTree != tree || error.Location.SourceSpan.Start < location.Start || error.Location.SourceSpan.Start > location.Start + location.Length)
                return Reject("STAGE-ESM-021", $"Internal validation analysis compilation error: {detail}");
            return Reject("STAGE-ESM-019", $"Validation body {PureTransitionAdmission.SourceLocation(requirement, body, location.ToBody(error.Location.SourceSpan.Start - location.Start))}: {detail}");
        }
        var verdict = PureTransitionAdmission.Walk(model, method.Body, descriptor, context.RootNamespace);
        if (!verdict.Accepted || !SemanticValidationContextRuntime.IsIterator(descriptor)) return verdict;
        return VerifyRenderedBody(compilation, tree, context, descriptor, body) ?? verdict;
    }

    // Keep original-body purity and source locations, then compile the exact invariant rewrite that will be emitted.
    static PureTransitionAdmission.Verdict? VerifyRenderedBody(CSharpCompilation compilation, SyntaxTree original, SemanticApplicationContext context, SemanticTypedContextDescriptor descriptor, string body)
    {
        // The analysis compilation has none of the application's global usings, so it compiles the qualified spelling.
        string content;
        using (QualifiedSpelling.Begin())
        {
            content = SemanticValidationContextRuntime.RenderBodies(context, [(descriptor, body)]).Content;
        }

        var rendered = CSharpSyntaxTree.ParseText(content, (CSharpParseOptions)original.Options);
        var error = compilation.ReplaceSyntaxTree(original, rendered).GetDiagnostics().FirstOrDefault(diagnostic =>
            diagnostic.Severity == DiagnosticSeverity.Error || (diagnostic.Severity == DiagnosticSeverity.Warning && !diagnostic.IsSuppressed));
        return error is null ? null : Reject("STAGE-ESM-019", $"Rendered validation body does not compile after invariant interpolation: {error.Id}: {error.GetMessage(CultureInfo.InvariantCulture)}");
    }

    static PureTransitionAdmission.Verdict Reject(string code, string reason) => new(code, reason, [], []);
}
