// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Screenplay.Semantics;
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
        var parsed = PureTransitionAdmission.ParseBody(body, generated, "validation");
        if (parsed.Rejection is not null) return parsed.Rejection;
        var code = SemanticValidationContextRuntime.RenderBodies(context, [(descriptor, body)]).Content;
        var options = new CSharpParseOptions(Microsoft.CodeAnalysis.CSharp.LanguageVersion.Latest);
        var tree = CSharpSyntaxTree.ParseText(code, options);
        var declarations = "global using System; global using System.Collections.Generic; global using System.Linq; global using System.IO; global using System.Net.Http; global using System.Threading; global using System.Threading.Tasks;\n" + SemanticValidationContextRuntime.AnalysisDeclarations(context);
        var compilation = CSharpCompilation.Create(
            "PureValidationAnalysis",
            [CSharpSyntaxTree.ParseText(declarations, options), CSharpSyntaxTree.ParseText(SemanticValidationContextRuntime.RenderWrapper(context, descriptor, analysis: true).Content, options), tree],
            PureTransitionAdmission.AnalysisReferences(),
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
            var offset = code.IndexOf(body, method.Body.OpenBraceToken.Span.End, StringComparison.Ordinal);
            var detail = $"{error.Id}: {error.GetMessage(CultureInfo.InvariantCulture)}";
            if (error.Location.SourceTree != tree || error.Location.SourceSpan.Start < offset || error.Location.SourceSpan.Start > offset + body.Length)
                return Reject("STAGE-ESM-021", $"Internal validation analysis compilation error: {detail}");
            return Reject("STAGE-ESM-019", $"Validation body {PureTransitionAdmission.SourceLocation(requirement, body, error.Location.SourceSpan.Start - offset)}: {detail}");
        }
        return PureTransitionAdmission.Walk(model, method.Body, descriptor, context.RootNamespace);
    }

    static PureTransitionAdmission.Verdict Reject(string code, string reason) => new(code, reason, [], []);
}
