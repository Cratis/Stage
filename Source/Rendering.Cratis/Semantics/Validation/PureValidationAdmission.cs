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
internal static partial class PureValidationAdmission
{
    enum AnalysisMode
    {
        Reads,
        Rendered
    }

    internal static PureTransitionAdmission.Verdict Analyze(string body, SemanticApplicationContext context, SemanticTypedContextDescriptor descriptor, SemanticImplementationRequirement requirement) =>
        new Analysis(context, descriptor, requirement).Analyze(body, AnalysisMode.Rendered);

    internal static PureTransitionAdmission.Verdict AnalyzeReads(string body, SemanticApplicationContext context, SemanticTypedContextDescriptor descriptor, SemanticImplementationRequirement requirement) =>
        new Analysis(context, descriptor, requirement).Analyze(body, AnalysisMode.Reads);

    static PureTransitionAdmission.Verdict Reject(string code, string reason) => new(code, reason, [], []);

    sealed class Analysis(SemanticApplicationContext context, SemanticTypedContextDescriptor descriptor, SemanticImplementationRequirement requirement)
    {
        internal PureTransitionAdmission.Verdict Analyze(string body, AnalysisMode mode)
        {
            var generated = context.Application.Concepts.Select(concept => Naming.Identifiers.ToPascalCase(concept.Name))
                .Concat(context.Application.Types.Select(type => Naming.Identifiers.ToPascalCase(type.Name)))
                .Concat(context.Commands.Values.Select(command => Naming.Identifiers.ToPascalCase(command.Name))).ToHashSet(StringComparer.Ordinal);
            var parsed = PureTransitionAdmission.ParseBody(body, generated, "validation", validationIterator: SemanticValidationContextRuntime.IsIterator(descriptor));
            if (parsed.Rejection is not null) return parsed.Rejection;
            MetadataReference[] references;
            try
            {
                references = PureTransitionAdmission.AnalysisReferences();
            }
            catch (InvalidOperationException exception)
            {
                return Reject("STAGE-ESM-021", exception.Message);
            }

            return AnalyzeCompilation(body, references, mode);
        }

        static Diagnostic? FirstError(CSharpCompilation compilation) => compilation.GetDiagnostics().FirstOrDefault(diagnostic =>
            diagnostic.Severity == DiagnosticSeverity.Error || (diagnostic.Severity == DiagnosticSeverity.Warning && !diagnostic.IsSuppressed));

        PureTransitionAdmission.Verdict AnalyzeCompilation(string body, MetadataReference[] references, AnalysisMode mode)
        {
            var code = SemanticValidationContextRuntime.RenderBodies(context, [(descriptor, body)], analysis: true).Content;
            var options = new CSharpParseOptions(Microsoft.CodeAnalysis.CSharp.LanguageVersion.Latest);
            var tree = CSharpSyntaxTree.ParseText(code, options);
            var declarations = "global using System; global using System.Collections.Generic; global using System.Linq; global using System.IO; global using System.Net.Http; global using System.Threading; global using System.Threading.Tasks;\n" + SemanticValidationContextRuntime.AnalysisDeclarations(context);
            var compilation = CSharpCompilation.Create(
                "PureValidationAnalysis",
                [CSharpSyntaxTree.ParseText(declarations, options), CSharpSyntaxTree.ParseText(SemanticValidationContextRuntime.RenderWrapper(context, descriptor, analysis: true).Content, options), tree],
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
            var model = compilation.GetSemanticModel(tree);
            var method = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single();
            var rejection = AmbientRejection(method, model) ?? TargetRejection(method, model, descriptor) ?? ShadowRejection(tree, model) ?? CompilationRejection(compilation, method, body);
            if (rejection is not null) return rejection;
            var verdict = PureTransitionAdmission.Walk(model, method.Body!, descriptor, context.RootNamespace);
            if (!RequiresRenderedVerification(verdict, mode)) return verdict;
            return VerifyRenderedBody(compilation, tree, body) ?? verdict;
        }

        bool RequiresRenderedVerification(PureTransitionAdmission.Verdict verdict, AnalysisMode mode) =>
            verdict.Accepted && mode == AnalysisMode.Rendered && SemanticValidationContextRuntime.IsIterator(descriptor);

        PureTransitionAdmission.Verdict? AmbientRejection(MethodDeclarationSyntax method, SemanticModel model)
        {
            var ambient = method.Body!.DescendantNodes().OfType<SimpleNameSyntax>()
                .Select(access => model.GetSymbolInfo(access).Symbol).OfType<IPropertySymbol>()
                .FirstOrDefault(property => property.ContainingType.Name == SemanticValidationContextRuntime.Wrapper(descriptor) &&
                    SemanticValidationContextRuntime.RefusedMembers.Contains(property.Name, StringComparer.Ordinal));
            return ambient is null ? null : Reject("STAGE-ESM-015", $"The validation body reads context.{ambient.Name}; dependency-free validators have no constructor-free ambient source for RuleContext.{ambient.Name} (Tenant and CausedBy remain refused).");
        }

        PureTransitionAdmission.Verdict? ShadowRejection(SyntaxTree tree, SemanticModel model)
        {
            var references = PureTransitionAdmission.MethodTypeReferences(tree, model, SemanticValidationContextRuntime.Method(descriptor));
            var peers = context.Application.Concepts.Select(concept => Naming.Identifiers.ToPascalCase(concept.Name))
                .Concat(context.Application.Types.Select(type => Naming.Identifiers.ToPascalCase(type.Name)));
            var shadow = peers.FirstOrDefault(name => references.Any(reference => reference.Name == name && PureTransitionAdmission.ShadowsAuditedName(name)));
            var commands = context.Commands.Values.Select(command => (Naming.Identifiers.ToPascalCase(command.Name), Naming.SliceNaming.Namespace(context.RootNamespace, context.DeclaringSlice(command.Id).Path))).ToHashSet();
            var method = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single();
            var commandTypes = method.Body!.DescendantNodes().OfType<SimpleNameSyntax>().Select(name => model.GetSymbolInfo(name).Symbol)
                .OfType<INamedTypeSymbol>().Where(type => commands.Contains((type.Name, type.ContainingNamespace.ToDisplayString()))).Select(type => type.Name).ToHashSet(StringComparer.Ordinal);
            var namespaceShadow = context.NamespacePaths.GroupBy(path => path.Split('.')[^1], StringComparer.Ordinal).FirstOrDefault(group =>
                references.Any(reference => !commandTypes.Contains(reference.Name) && reference.Name == group.Key &&
                    (reference.Namespace is null || group.Any(path => path != reference.Namespace))))?.Key;
            return shadow is null && namespaceShadow is null ? null : Reject("STAGE-ESM-022", $"Generated declaration '{shadow ?? namespaceShadow}' shadows a validation body type.");
        }

        PureTransitionAdmission.Verdict? CompilationRejection(CSharpCompilation compilation, MethodDeclarationSyntax method, string body)
        {
            var error = FirstError(compilation);
            if (error is null) return null;
            var code = method.SyntaxTree.GetText().ToString();
            var location = CSharpInlineIndentation.Locate(code, body, method.Body!.OpenBraceToken.Span.End);
            var detail = $"{error.Id}: {error.GetMessage(CultureInfo.InvariantCulture)}";
            if (location is null || error.Location.SourceTree != method.SyntaxTree || error.Location.SourceSpan.Start < location.Start || error.Location.SourceSpan.Start > location.Start + location.Length)
                return Reject("STAGE-ESM-021", $"Internal validation analysis compilation error: {detail}");
            return Reject("STAGE-ESM-019", $"Validation body {PureTransitionAdmission.SourceLocation(requirement, body, location.ToBody(error.Location.SourceSpan.Start - location.Start))}: {detail}");
        }

        // Keep original-body purity and source locations, then compile the exact invariant rewrite that will be emitted.
        PureTransitionAdmission.Verdict? VerifyRenderedBody(CSharpCompilation compilation, SyntaxTree original, string body)
        {
            string content;
            using (QualifiedSpelling.Begin())
            {
                content = SemanticValidationContextRuntime.RenderBodies(context, [(descriptor, body)]).Content;
            }

            var rendered = CSharpSyntaxTree.ParseText(content, (CSharpParseOptions)original.Options);
            var error = FirstError(compilation.ReplaceSyntaxTree(original, rendered));
            return error is null ? null : Reject("STAGE-ESM-019", $"Invariant validation rewrite {error.Id}: {error.GetMessage(CultureInfo.InvariantCulture)}");
        }
    }
}
