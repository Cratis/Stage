// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Stage.Rendering.Cratis.Execution;

/// <summary>Projects renderer declarations into a closed transition helper compilation.</summary>
internal static partial class BodyUnitSources
{
    internal static ReducerSources RenderReducer(SemanticApplicationContext context, SemanticTypedContextDescriptor descriptor, string body)
    {
        using var qualified = QualifiedSpelling.Begin();
        var reducer = context.Reducers.Single(reducer => reducer.Transitions.Any(transition => transition.RequirementId == descriptor.RequirementId));
        var model = context.ReadModels[reducer.ReadModel];
        var self = context.SelfReduces(model.Id);
        var located = context.DeclaringSlice(model.Id);
        var ns = SliceNaming.Namespace(context.RootNamespace, located.Path);
        var helper = SemanticReducerArtifactRenderer.HelperName(descriptor.RequirementId);
        var name = self ? Identifiers.ToPascalCase(model.Name) : Identifiers.ToPascalCase(reducer.Name);
        var path = $"worker/reducers/{name}/{helper}.cs";
        var builder = context.CodeBuilder().Namespace(ns);
        context.ReducerBindings(located, self ? null : reducer).Configure(builder);
        if (self)
        {
            builder.OpenBlock(SemanticStateViewArtifactRenderer.ModelDeclaration(model, new(context), located.Slice.Queries, true));
            RenderQueries(builder, model, located, context);
        }
        else
        {
            builder.OpenBlock($"public class {name}");
        }
        SemanticReducerArtifactRenderer.RenderTransitions(builder, reducer, context, descriptor.RequirementId, body);
        builder.EndBlock();
        var sources = new List<string> { WithoutBoundaryImports(builder.ToString()), SemanticBodyCompilation.ImplicitUsings };
        sources.AddRange(TransitionContexts(context, reducer));
        sources.AddRange(Declarations(context, qualified: true));
        sources.AddRange(PeerReadModels(context, self ? model.Id : null));
        sources.Add(TokenSource(context));
        return new([.. sources], path, self ? $"{ns}.{name}+{helper}" : $"{ns}.{name}", self ? SemanticReducerArtifactRenderer.SelfTransitionMethod : helper);
    }

    static IEnumerable<string> TransitionContexts(SemanticApplicationContext context, SemanticReducer reducer) =>
        reducer.Transitions.Select(transition => context.Request.TypedContextDescriptors.Single(candidate => candidate.RequirementId == transition.RequirementId && candidate.OperationId == reducer.ReadModel))
            .Select(candidate => SemanticTypedContextRenderer.Render(candidate, context).Content);

    static IEnumerable<string> Declarations(SemanticApplicationContext context, bool qualified) =>
        SemanticBodyDeclarations.RealConcepts(context)
            .Concat(SemanticBodyDeclarations.GeneratedEvents(context))
            .Concat(context.Application.Types.Select(type => SemanticCommonArtifactRenderer.Render(type, context, qualified).Content))
            .Concat(context.Commands.Values.Select(command => CommandDeclaration(context, command)));

    static IEnumerable<string> PeerReadModels(SemanticApplicationContext context, SemanticId? self)
    {
        foreach (var peer in context.ReadModels.Values.Where(peer => peer.Id != self))
        {
            var slice = context.DeclaringSlice(peer.Id);
            var peerBuilder = context.CodeBuilder().Namespace(SliceNaming.Namespace(context.RootNamespace, slice.Path));
            context.ReducerBindings(slice).Configure(peerBuilder);
            peerBuilder.OpenBlock(SemanticStateViewArtifactRenderer.ModelDeclaration(peer, new(context), slice.Slice.Queries, true));
            RenderQueries(peerBuilder, peer, slice, context);
            yield return WithoutBoundaryImports(peerBuilder.EndBlock().ToString());
        }
    }

    static string TokenSource(SemanticApplicationContext context)
    {
        var token = CSharpSyntaxTree.ParseText(SemanticReducerContextRuntime.Render(context).Content).GetCompilationUnitRoot();
        var boundary = token.DescendantNodes().OfType<ClassDeclarationSyntax>().Single();
        return token.RemoveNode(boundary, SyntaxRemoveOptions.KeepNoTrivia)!.ToFullString();
    }

    // Arc attributes are not part of the helper-only projection. Its two boundary-only imports
    // have no declarations in the pinned worker references; keep every model/query member intact.
    static string WithoutBoundaryImports(string source)
    {
        var root = CSharpSyntaxTree.ParseText(source).GetCompilationUnitRoot();
        return root.WithUsings([.. root.Usings.Where(directive => directive.Name?.ToString() is not ("Cratis.Arc.Authorization" or "Cratis.Arc.Queries.ModelBound"))]).ToFullString();
    }

    static void RenderQueries(CSharpCodeBuilder builder, SemanticReadModel model, LocatedSemanticSlice located, SemanticApplicationContext context)
    {
        foreach (var query in located.Slice.Queries.Where(query => query.ReadModel == model.Id))
        {
            var queryBuilder = context.CodeBuilder();
            SemanticStateViewArtifactRenderer.RenderQuery(queryBuilder, query, model, new(context), context);
            var syntax = CSharpSyntaxTree.ParseText($"class Query {{ {queryBuilder} }}").GetRoot();
            var method = syntax.DescendantNodes().OfType<MethodDeclarationSyntax>().Single();
            builder.RawVerbatim(method.WithAttributeLists(default).ToFullString());
        }
    }

    internal sealed record ReducerSources(string[] Sources, string Path, string EntryType, string EntryMethod);
}
