// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

/// <summary>
/// Ensures unsupported scope behavior fails admission without producing artifacts.
/// </summary>
public class when_rejecting_unsupported_scoped_projections : Specification
{
    [Theory]
    [InlineData("literal")]
    [InlineData("every-including-children")]
    [InlineData("child-join")]
    [InlineData("nested-join")]
    [InlineData("all-events")]
    [InlineData("root-join-removal")]
    [InlineData("nested-only-from")]
    [InlineData("nested-only-clear")]
    [InlineData("root-from-join-overlap")]
    [InlineData("join-removal-overlap")]
    [InlineData("nested-mismatched-key")]
    [InlineData("nested-clear-with-root-from")]
    [InlineData("child-join-removal")]
    public void should_fail_closed_for_unsupported_blocks(string variant)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var source = when_rendering_scoped_projections.ScopedSource;
        if (variant == "literal")
        {
            source = source.Replace("notes ProjectNote[]", "notes ProjectNote[]\n        label String?", StringComparison.Ordinal)
                .Replace("join project on projectId", "    label = \"fixed\"\n        join project on projectId", StringComparison.Ordinal);
        }

        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("scopes"), "scopes", "Scopes.play", source);
        var compilation = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success);
        var original = compilation.Value!.Model;
        var module = original.Application.Modules.Single();
        var feature = module.Features.Single();
        var view = feature.Slices.Single(_ => _.Kind == SemanticSliceKind.StateView);
        var projection = view.Projections.Single(_ => _.Name == "ProjectSummaryProjection");
        var scope = projection.Scope!;
        scope = variant switch
        {
            "literal" => scope,
            "every-including-children" => scope with { Every = new(true, false, []) },
            "all-events" => scope with { Every = new(true, true, []) },
            "root-join-removal" => scope with { JoinRemovals = [new(
                original.Application.Modules.Single().Features.Single().Slices.SelectMany(slice => slice.Events)
                    .Single(@event => @event.Name == "ProjectNoteRemovedViaJoin").Id,
                SemanticProjectionKey.EventSourceIdentity)] },
            "nested-join" => scope with
            {
                Nested = [.. scope.Nested.Select(nested => nested with
                {
                    Scope = nested.Scope with { Joins = [new(scope.Joins[0].EventContract, nested.Scope.From[0].Mappings[0].Target[0], [])] }
                })]
            },
            "nested-only-from" => scope with { Nested = [.. scope.Nested.Select(nested => nested with { Scope = nested.Scope with
            {
                From = [nested.Scope.From[0] with
                {
                    EventContract = original.Application.Modules.Single().Features.Single().Slices.SelectMany(slice => slice.Events)
                        .Single(@event => @event.Name == "ProjectInfoChanged").Id,
                    Key = SemanticProjectionKey.EventSourceIdentity,
                    Mappings = [nested.Scope.From[0].Mappings[0] with { Source = new SemanticProjectionEventProperty(
                        [original.Application.Modules.Single().Features.Single().Slices.SelectMany(slice => slice.Events)
                            .Single(@event => @event.Name == "ProjectInfoChanged").Properties.Single().Id]) }]
                }]
            } })] },
            "nested-only-clear" => scope with { Nested = [.. scope.Nested.Select(nested => nested with { Scope = nested.Scope with
            {
                Removals = [new SemanticProjectionRemoval(
                    original.Application.Modules.Single().Features.Single().Slices.SelectMany(slice => slice.Events).Single(@event => @event.Name == "ProjectInfoCleared").Id,
                    SemanticProjectionKey.EventSourceIdentity,
                    null)]
            } })] },
            "root-from-join-overlap" => scope with { Joins = [.. scope.Joins, new SemanticProjectionJoin(scope.From[0].EventContract, scope.Joins[0].On, [])] },
            "join-removal-overlap" => scope with { Removals = [.. scope.Removals, scope.Removals[0] with { EventContract = scope.Joins[0].EventContract }] },
            "nested-mismatched-key" => scope with { Nested = [.. scope.Nested.Select(nested => nested with { Scope = nested.Scope with
            {
                From = [nested.Scope.From[0] with { Key = SemanticProjectionKey.EventSourceIdentity }]
            } })] },
            "nested-clear-with-root-from" => scope with { Nested = [.. scope.Nested.Select(nested => nested with { Scope = nested.Scope with
            {
                Removals = [new SemanticProjectionRemoval(scope.From[1].EventContract, scope.From[1].Key, null)]
            } })] },
            "child-join-removal" => scope with { Children = [.. scope.Children.Select(children => children with { Scope = children.Scope with
            {
                JoinRemovals = [new SemanticProjectionJoinRemoval(
                    original.Application.Modules.Single().Features.Single().Slices.SelectMany(slice => slice.Events)
                        .Single(@event => @event.Name == "ProjectNoteRemovedViaJoin").Id,
                    SemanticProjectionKey.EventSourceIdentity)]
            } })] },
            "child-join" => scope with
            {
                Children = [.. scope.Children.Select(children => children with
                {
                    Scope = children.Scope with { Joins = [new(scope.Joins[0].EventContract, children.IdentifiedBy, [])] }
                })]
            },
            _ => throw new UnknownScopeVariant(variant)
        };
        var changed = view with { Projections = [.. view.Projections.Select(value => value.Id == projection.Id ? value with { Scope = scope } : value)] };
        var model = ExecutableSemanticModel.Create(
            original.LanguageVersion,
            original.SemanticVersion,
            original.Application with { Modules = [module with { Features = [feature with { Slices = [.. feature.Slices.Select(slice => slice.Id == view.Id ? changed : slice)] }] }] });
        var execution = SemanticExecutionPlan.Compile(original).Plan!;
        var options = new CratisRenderingOptions("Projects", "Projects");
        var profile = CratisRendering.CreateProfile(model.Application.Name, options);
        var request = new ArtifactRenderRequest(model, execution, profile, new(ArtifactRenderScopeKind.Application, model.Application.Id));
        var context = new SemanticApplicationContext(request, options);
        var diagnostics = SemanticCratisAdmission.Evaluate(context, context.SelectedSlices());
        var diagnostic = Assert.Single(diagnostics, _ => _.Code == "STAGE-ESM-017");
        if (variant == "literal")
        {
            Assert.Contains("Chronicle#4124", diagnostic.Message, StringComparison.Ordinal);
        }

        if (variant == "every-including-children" || variant == "nested-join")
        {
            Assert.Contains("Chronicle#4125", diagnostic.Message, StringComparison.Ordinal);
        }
    }

    sealed class UnknownScopeVariant(string name) : Exception($"Unknown projection scope variant '{name}'.");
}
