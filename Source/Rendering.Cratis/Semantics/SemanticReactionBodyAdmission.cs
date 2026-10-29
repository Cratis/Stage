// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Stage.Rendering.Cratis.Naming.Imports;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Compiles the exact reactor file the renderer emits against the model's events in their slice namespaces, the generated
/// concepts and the real Chronicle declarations, so an authored reaction body that cannot compile is refused at render time.
/// </summary>
internal static class SemanticReactionBodyAdmission
{
    const int MaximumReported = 5;
    static readonly CSharpParseOptions _parseOptions = new(LanguageVersion.Latest, preprocessorSymbols: ["DEBUG"]);

    /// <summary>Compiles a rendered reactor and reports why it does not compile, or null when it does.</summary>
    /// <param name="context">The application being rendered.</param>
    /// <param name="renderedReactor">The full text of the rendered reactor file.</param>
    /// <returns>The first compiler diagnostics as "CSxxxx: message", or null on success.</returns>
    internal static string? Failure(SemanticApplicationContext context, string renderedReactor)
    {
        try
        {
            MetadataReference[] references =
            [
                .. PureTransitionAdmission.AnalysisReferences(),
                SemanticGenerationBodyAdmission.FundamentalsReference,
                SemanticGenerationBodyAdmission.ChronicleReference
            ];
            var reactor = CSharpSyntaxTree.ParseText(renderedReactor, _parseOptions);
            var stripped = reactor.WithRootAndOptions(new TypeAttributeRemover().Visit(reactor.GetRoot()), _parseOptions);
            var supporting = Supporting(context);
            var probe = SemanticBodyCompilation.Create("ReactionBodyProbe", [stripped, .. supporting], references);
            var unbound = UnboundUsings(stripped, probe.GetSemanticModel(stripped));
            if (unbound.Count > 0)
            {
                stripped = stripped.WithRootAndOptions(new UsingRemover(unbound).Visit(stripped.GetRoot()), _parseOptions);
                probe = SemanticBodyCompilation.Create("ReactionBodyProbe", [stripped, .. supporting], references);
            }

            var failures = probe.GetDiagnostics()
                .Where(diagnostic => SemanticBodyCompilation.IsFailure(diagnostic) && diagnostic.Location.SourceTree == stripped)
                .Take(MaximumReported)
                .Select(diagnostic => $"{diagnostic.Id}: {diagnostic.GetMessage(CultureInfo.InvariantCulture)}")
                .ToArray();
            return failures.Length == 0 ? null : string.Join("; ", failures);
        }
        catch (InvalidOperationException exception)
        {
            return $"analysis references unavailable: {exception.Message}";
        }
        catch (Contracts.Rendering.InvalidArtifactRenderContract exception)
        {
            return $"analysis references unavailable: {exception.Message}";
        }
    }

    static IEnumerable<SyntaxTree> Supporting(SemanticApplicationContext context)
    {
        var declarations = string.Join('\n', SemanticBodyDeclarations.RealConcepts(context)
            .Append($"namespace {context.CommonNamespace} {{ {string.Join('\n', SemanticBodyDeclarations.CompositeTypes(context))} }}")
            .Append(SemanticBodyDeclarations.Commands(context)));

        // The scaffold turns the Chronicle, Arc and implicit namespaces into global usings, so the renderer writes short names.
        var globals = FrameworkNameCatalog.Current.GlobalUsings(GeneratedProjects.Application).Select(@namespace => $"global using {@namespace};");
        yield return CSharpSyntaxTree.ParseText(SemanticBodyCompilation.ImplicitUsings + string.Join('\n', globals), _parseOptions);
        yield return CSharpSyntaxTree.ParseText(declarations, _parseOptions);

        // The events are the generated ones, not property-only stubs: members the real event carries stay available to the body.
        foreach (var @event in SemanticBodyDeclarations.GeneratedEvents(context))
        {
            yield return CSharpSyntaxTree.ParseText(@event, _parseOptions);
        }
    }

    static HashSet<UsingDirectiveSyntax> UnboundUsings(SyntaxTree tree, SemanticModel model) =>
        [.. tree.GetRoot().DescendantNodes().OfType<UsingDirectiveSyntax>()
            .Where(directive => directive.Name is { } name && model.GetSymbolInfo(name).Symbol is null)];

    sealed class TypeAttributeRemover : CSharpSyntaxRewriter
    {
        public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node) =>
            base.VisitClassDeclaration(node.WithAttributeLists(default));
    }

    sealed class UsingRemover(HashSet<UsingDirectiveSyntax> unbound) : CSharpSyntaxRewriter
    {
        public override SyntaxNode? VisitUsingDirective(UsingDirectiveSyntax node) =>
            unbound.Contains(node) ? null : base.VisitUsingDirective(node);
    }
}
