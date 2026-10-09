// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_scoped_shared_declarations;

public class with_scoped_projection_event_dependencies : Specification
{
    const string Source = """
        concept RootMarker : String
        concept JoinMarker : String
        concept ChildMarker : String
        concept NestedMarker : String
        concept RemovalMarker : String
        concept ChildRemovalMarker : String
        concept ChildJoinRemovalMarker : String
        concept UnusedMarker : String
        type JoinDetails
          marker JoinMarker
        type ChildRow
          childId String
          value String
        type RootInfo
          name String
        module External
          feature Facts
            slice StateChange Create
              command CreateRoot
                rootId String identifier
                marker RootMarker
                produces RootCreated
                  for rootId
                  rootId = rootId
                  marker = marker
              event RootCreated
                rootId String
                marker RootMarker
              event RootNamed
                name String
                details JoinDetails
              event ChildAdded
                rootId String
                childId String
                value String
                marker ChildMarker
              event InfoChanged
                rootId String
                name String
                marker NestedMarker
              event RootRemoved
                rootId String
                marker RemovalMarker
              event ChildRemoved
                rootId String
                childId String
                marker ChildRemovalMarker
              event ChildRemovedViaJoin
                childId String
                marker ChildJoinRemovalMarker
        module Views
          feature Lookup
            slice StateView RootLookup
              readmodel RootSummary
                rootId String
                name String
                visits Decimal?
                children ChildRow[]
                info RootInfo?
              query RootById => RootSummary?
                by rootId String
              projection RootSummaryProjection => RootSummary
                from RootCreated key rootId
                  increment visits
                from InfoChanged key rootId
                  name = name
                join root on rootId
                  with RootNamed
                    no automap
                    name = name
                remove with RootRemoved key rootId
                nested info
                  from InfoChanged key rootId
                    name = name
                children children identified by childId
                  from ChildAdded key childId
                    parent rootId
                    value = value
                  remove with ChildRemoved key childId
                    parent rootId
                  remove via join on ChildRemovedViaJoin key childId
        """;

    static readonly string[] EventOnlyPaths =
    [
        "Common/RootMarker.cs", "Common/JoinDetails.cs", "Common/JoinMarker.cs", "Common/ChildMarker.cs",
        "Common/NestedMarker.cs", "Common/RemovalMarker.cs", "Common/ChildRemovalMarker.cs", "Common/ChildJoinRemovalMarker.cs"
    ];

    ExecutableSemanticModel _model = null!;
    SemanticSlice _view = null!;
    ArtifactRenderPlan _application = null!;
    ArtifactRenderPlan _scoped = null!;
    IReadOnlyList<string> _errors = [];
    IReadOnlyList<string> _warnings = [];

    void Establish()
    {
        _model = invoice_model.Compile(Source);
        _view = _model.Application.Modules.Single(module => module.Name == "Views").Features.Single().Slices.Single();
        _application = invoice_model.Plan(_model);
        Assert.True(_application.Success, string.Join(Environment.NewLine, _application.Diagnostics));
    }

    void Because()
    {
        _scoped = invoice_model.Plan(_model, new(ArtifactRenderScopeKind.Slice, _view.Id));
        Assert.True(_scoped.Success, string.Join(Environment.NewLine, _scoped.Diagnostics));

        // The foreign event contracts already exist in the scoped render's destination. Extract only
        // those declarations from application output, not its command or any Common dependencies.
        var existingEvents = ExistingEvents();
        var files = Files(_scoped).Append(existingEvents).ToArray();
        _errors = RenderedOutput.Errors(files);
        _warnings = RenderedOutput.Warnings(files);
    }

    [Fact] void should_exercise_a_scoped_projection() => _view.Projections.Single().Scope.ShouldNotBeNull();
    [Fact] void should_have_no_flat_transitions() => _view.Projections.Single().Transitions.ShouldBeEmpty();
    [Fact] void should_include_the_from_events_concept() => Paths().ShouldContain("Common/RootMarker.cs");
    [Fact] void should_include_the_join_events_composite() => Paths().ShouldContain("Common/JoinDetails.cs");
    [Fact] void should_include_the_join_composites_transitive_concept() => Paths().ShouldContain("Common/JoinMarker.cs");
    [Fact] void should_include_the_child_events_concept() => Paths().ShouldContain("Common/ChildMarker.cs");
    [Fact] void should_include_the_nested_events_concept() => Paths().ShouldContain("Common/NestedMarker.cs");
    [Fact] void should_include_the_root_removal_events_concept() => Paths().ShouldContain("Common/RemovalMarker.cs");
    [Fact] void should_include_the_child_removal_events_concept() => Paths().ShouldContain("Common/ChildRemovalMarker.cs");
    [Fact] void should_include_the_child_join_removal_events_concept() => Paths().ShouldContain("Common/ChildJoinRemovalMarker.cs");
    [Fact] void should_keep_event_only_artifact_bytes_identical() => EventOnlyPaths.All(path => _scoped.Artifacts.Single(artifact => artifact.RelativePath == path).Bytes.SequenceEqual(_application.Artifacts.Single(artifact => artifact.RelativePath == path).Bytes)).ShouldBeTrue();
    [Fact] void should_exclude_unreferenced_concepts() => Paths().ShouldNotContain("Common/UnusedMarker.cs");
    [Fact] void should_not_render_the_foreign_slice() => Paths().ShouldNotContain("External/Facts/Create/Create.cs");
    [Fact] void should_compile_with_existing_event_contracts() => _errors.ShouldBeEmpty();
    [Fact] void should_compile_without_warnings() => _warnings.ShouldBeEmpty();

    IEnumerable<string> Paths() => _scoped.Artifacts.Select(artifact => artifact.RelativePath);

    RenderedFile ExistingEvents()
    {
        var foreign = _model.Application.Modules.Single(module => module.Name == "External").Features.Single().Slices.Single();
        var source = _application.Artifacts.Single(artifact => artifact.Sources.Contains(foreign.Id));
        var root = CSharpSyntaxTree.ParseText(Encoding.UTF8.GetString(source.Bytes.AsSpan())).GetCompilationUnitRoot();
        var ns = root.Members.OfType<FileScopedNamespaceDeclarationSyntax>().Single();
        var names = foreign.Events.Select(@event => @event.Name).ToHashSet(StringComparer.Ordinal);
        var declarations = ns.Members.OfType<RecordDeclarationSyntax>().Where(record => names.Contains(record.Identifier.ValueText));
        return new("ExistingEvents.cs", root.ReplaceNode(ns, ns.WithMembers(SyntaxFactory.List<MemberDeclarationSyntax>(declarations))).ToFullString());
    }

    static IEnumerable<RenderedFile> Files(ArtifactRenderPlan plan) => plan.Artifacts
        .Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal))
        .Select(artifact => new RenderedFile(artifact.RelativePath, Encoding.UTF8.GetString(artifact.Bytes.AsSpan())));
}
