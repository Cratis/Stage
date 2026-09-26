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
    [InlineData("every-including-children")]
    [InlineData("child-join")]
    [InlineData("nested-join")]
    [InlineData("root-join-removal")]
    [InlineData("nested-only-from")]
    [InlineData("nested-only-clear")]
    [InlineData("root-from-join-overlap")]
    [InlineData("join-removal-overlap")]
    [InlineData("nested-mismatched-key")]
    [InlineData("nested-clear-with-root-from")]
    [InlineData("composite-key")]
    public void should_fail_closed_for_unsupported_blocks(string variant)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var source = when_rendering_scoped_projections.ScopedSource;
        if (variant == "composite-key")
        {
            // The seeded lookup states a scalar key, which a composite key cannot accept.
            var lookup = source.LastIndexOf('\n', source.IndexOf("specification LookingUpPinnedProject", StringComparison.Ordinal)) + 1;
            var end = source.LastIndexOf('\n', source.IndexOf("slice StateView ProjectLookup", StringComparison.Ordinal)) + 1;
            source = source.Remove(lookup, end - lookup).Replace("type ProjectInfo\n", "type ProjectKey\n  projectId ProjectId\ntype ProjectInfo\n", StringComparison.Ordinal)
                .Replace("readmodel ProjectDetails\n        projectId ProjectId", "readmodel ProjectDetails\n        projectId ProjectKey", StringComparison.Ordinal)
                .Replace("by projectId ProjectId\n      projection ProjectDetailsProjection", "by projectId ProjectKey\n      projection ProjectDetailsProjection", StringComparison.Ordinal)
                .Replace("from ProjectRegistered key projectId\n          name = name\n      projection ProjectSummaryProjection", "from ProjectRegistered\n          key ProjectKey\n            projectId = projectId\n          name = name\n        from ProjectRenamed\n          key ProjectKey\n            projectId = projectId\n          name = name\n      projection ProjectSummaryProjection", StringComparison.Ordinal);
        }

        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("scopes"), "scopes", "Scopes.play", source);
        var compilation = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success);
        var original = compilation.Value!.Model;
        var module = original.Application.Modules.Single();
        var feature = module.Features.Single();
        var view = feature.Slices.Single(_ => _.Kind == SemanticSliceKind.StateView);
        var projection = view.Projections.Single(_ => _.Name == (variant == "composite-key" ? "ProjectDetailsProjection" : "ProjectSummaryProjection"));
        var scope = projection.Scope!;
        if (variant == "composite-key")
        {
            Assert.Contains("key ProjectKey", source, StringComparison.Ordinal);
            Assert.NotNull(scope);
            Assert.IsType<SemanticProjectionCompositeKey>(scope.From[0].Key);
        }

        scope = variant switch
        {
            "composite-key" => scope,
            "every-including-children" => scope with { Every = new(true, false, []) },
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
        if (variant == "every-including-children" || variant == "nested-join" || variant == "root-join-removal")
        {
            Assert.Contains("Chronicle#4125", diagnostic.Message, StringComparison.Ordinal);
        }

        if (variant == "composite-key")
        {
            Assert.Contains("v19.8.1", diagnostic.Message, StringComparison.Ordinal);
        }

        if (variant == "nested-clear-with-root-from")
        {
            Assert.Contains("Chronicle#4166", diagnostic.Message, StringComparison.Ordinal);
        }

        if (variant == "composite-key")
        {
            var changedExecution = SemanticExecutionPlan.Compile(model);
            Assert.True(changedExecution.Success, string.Join(Environment.NewLine, changedExecution.Issues));
            var blocked = CratisRendering.Plan(model, changedExecution.Plan!, new(ArtifactRenderScopeKind.Application, model.Application.Id), options);
            Assert.False(blocked.Success);
            Assert.Empty(blocked.Artifacts);
            Assert.Contains(blocked.Diagnostics, item => item.Code == "STAGE-ESM-017");
        }
    }

    sealed class UnknownScopeVariant(string name) : Exception($"Unknown projection scope variant '{name}'.");
}
