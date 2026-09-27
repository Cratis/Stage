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
    [InlineData("every-literal")]
    [InlineData("every-whole-number")]
    [InlineData("every-enumerated-concept")]
    [InlineData("event-source-with-property-key")]
    [InlineData("join-event-source")]
    [InlineData("expression-event-property")]
    [InlineData("derived-event-property")]
    [InlineData("flat-expression-event-property")]
    [InlineData("expression-read-model-property")]
    [InlineData("flat-expression-read-model-property")]
    [InlineData("all-literal")]
    [InlineData("all-events")]
    [InlineData("unsafe-text-literal")]
    [InlineData("unsafe-text-concept-literal")]
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

        if (variant == "unsafe-text-literal")
        {
            source = source.Replace("notes ProjectNote[]", "label String?\n        notes ProjectNote[]", StringComparison.Ordinal)
                .Replace("name = name\n          increment visits", "name = name\n          label = \"a)b\"\n          increment visits", StringComparison.Ordinal);
        }

        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("scopes"), "scopes", "Scopes.play", source);
        var compilation = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join("; ", compilation.Diagnostics.Select(diagnostic => diagnostic.Message)));
        var original = compilation.Value!.Model;
        var module = original.Application.Modules.Single();
        var feature = module.Features.Single();
        var view = feature.Slices.Single(_ => _.Kind == SemanticSliceKind.StateView);
        var projection = view.Projections.Single(_ => _.Name == (variant == "composite-key" || variant == "flat-expression-event-property" || variant == "flat-expression-read-model-property" ? "ProjectDetailsProjection" : "ProjectSummaryProjection"));
        var scope = projection.Scope!;
        if (variant == "composite-key")
        {
            Assert.Contains("key ProjectKey", source, StringComparison.Ordinal);
            Assert.NotNull(scope);
            Assert.IsType<SemanticProjectionCompositeKey>(scope.From[0].Key);
        }

        var nameTarget = view.ReadModels.Single(model => model.Name == "ProjectSummary").Properties.Single(property => property.Name == "name").Id;
        var unsafeLiteral = new SemanticProjectionLiteral(SemanticValue.Text("a)b"));
        var lastSeenTarget = view.ReadModels.Single(model => model.Name == "ProjectSummary").Properties.Single(property => property.Name == "lastSeen").Id;
        var visitsTarget = view.ReadModels.Single(model => model.Name == "ProjectSummary").Properties.Single(property => property.Name == "visits").Id;
        scope = variant switch
        {
            "composite-key" => scope,
            "all-events" => scope with
            {
                Children = [],
                Nested = [],
                Every = scope.Every! with { IncludeChildren = true, SubscribesToAllEvents = true }
            },
            "every-literal" or "all-literal" => scope with
            {
                Every = scope.Every! with
                {
                    SubscribesToAllEvents = variant == "all-literal",
                    IncludeChildren = variant == "all-literal",
                    Mappings = [new SemanticProjectionMapping([nameTarget], SemanticProjectionOperation.Set, new SemanticProjectionLiteral(SemanticValue.Text("fixed")))]
                }
            },
            "unsafe-text-literal" => scope,
            "unsafe-text-concept-literal" => scope with
            {
                From = [.. scope.From.Select((transition, index) => index == 0 ? transition with
            {
                Mappings = [.. transition.Mappings.Select(mapping => mapping.Target.Contains(nameTarget) ? mapping with { Source = unsafeLiteral } : mapping)]
            } : transition)]
            },

            "every-including-children" => scope with { Every = new(true, false, []) },
            "every-whole-number" => scope with
            {
                Every = scope.Every! with
                {
                    Mappings = [new SemanticProjectionMapping([visitsTarget], SemanticProjectionOperation.Set, SemanticProjectionValue.EventSourceIdentity)]
                }
            },
            "every-enumerated-concept" => scope,
            "event-source-with-property-key" => scope with
            {
                From = [.. scope.From.Select((transition, index) => index == 0 ? transition with
            {
                Mappings = [.. transition.Mappings, new SemanticProjectionMapping([lastSeenTarget], SemanticProjectionOperation.Set, SemanticProjectionValue.EventSourceIdentity)]
            } : transition)]
            },
            "join-event-source" => scope with
            {
                Joins = [.. scope.Joins.Select(join => join with
                {
                    Mappings = [.. join.Mappings, new SemanticProjectionMapping([lastSeenTarget], SemanticProjectionOperation.Set, SemanticProjectionValue.EventSourceIdentity)]
                })]
            },
            "expression-event-property" or "derived-event-property" or "flat-expression-event-property" or
                "expression-read-model-property" or "flat-expression-read-model-property" => scope,
            "root-join-removal" => scope with
            {
                JoinRemovals = [new(
                original.Application.Modules.Single().Features.Single().Slices.SelectMany(slice => slice.Events)
                    .Single(@event => @event.Name == "ProjectNoteRemovedViaJoin").Id,
                SemanticProjectionKey.EventSourceIdentity)]
            },
            "nested-join" => scope with
            {
                Nested = [.. scope.Nested.Select(nested => nested with
                {
                    Scope = nested.Scope with { Joins = [new(scope.Joins[0].EventContract, nested.Scope.From[0].Mappings[0].Target[0], [])] }
                })]
            },
            "nested-only-from" => scope with
            {
                Nested = [.. scope.Nested.Select(nested => nested with { Scope = nested.Scope with
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
            } })]
            },
            "nested-only-clear" => scope with
            {
                Nested = [.. scope.Nested.Select(nested => nested with { Scope = nested.Scope with
            {
                Removals = [new SemanticProjectionRemoval(
                    original.Application.Modules.Single().Features.Single().Slices.SelectMany(slice => slice.Events).Single(@event => @event.Name == "ProjectInfoCleared").Id,
                    SemanticProjectionKey.EventSourceIdentity,
                    null)]
            } })]
            },
            "root-from-join-overlap" => scope with { Joins = [.. scope.Joins, new SemanticProjectionJoin(scope.From[0].EventContract, scope.Joins[0].On, [])] },
            "join-removal-overlap" => scope with { Removals = [.. scope.Removals, scope.Removals[0] with { EventContract = scope.Joins[0].EventContract }] },
            "nested-mismatched-key" => scope with
            {
                Nested = [.. scope.Nested.Select(nested => nested with { Scope = nested.Scope with
            {
                From = [nested.Scope.From[0] with { Key = SemanticProjectionKey.EventSourceIdentity }]
            } })]
            },
            "nested-clear-with-root-from" => scope with
            {
                Nested = [.. scope.Nested.Select(nested => nested with { Scope = nested.Scope with
            {
                Removals = [new SemanticProjectionRemoval(scope.From[1].EventContract, scope.From[1].Key, null)]
            } })]
            },
            "child-join" => scope with
            {
                Children = [.. scope.Children.Select(children => children with
                {
                    Scope = children.Scope with { Joins = [new(scope.Joins[0].EventContract, children.IdentifiedBy, [])] }
                })]
            },
            _ => throw new UnknownScopeVariant(variant)
        };
        var eventContract = feature.Slices.SelectMany(slice => slice.Events).Single(@event => @event.Name == "ProjectRegistered");
        var targetModel = view.ReadModels.Single(model => model.Id == projection.ReadModel);
        var key = new SemanticResolvedExpression(
            SemanticExpressionRootKind.Event,
            SemanticExpressionSourceKind.Property,
            eventContract.Properties.Single(property => property.Name == "projectId").Id);
        var sourceName = new SemanticResolvedExpression(
            SemanticExpressionRootKind.Event,
            SemanticExpressionSourceKind.Property,
            eventContract.Properties.Single(property => property.Name == "name").Id);
        var flat = new SemanticProjectionTransition(
            eventContract.Id,
            new(AffectedInstanceCardinality.One, key),
            [new(targetModel.Properties.Single(property => property.Name == "projectId").Id, key),
             new(targetModel.Properties.Single(property => property.Name == "name").Id, sourceName)]);
        var changedProjection = variant == "flat-expression-event-property" || variant == "flat-expression-read-model-property" ? projection with { Scope = null, Transitions = [flat] } : projection with { Scope = scope };
        var changed = view with
        {
            ReadModels = [.. view.ReadModels.Select(model => model.Id == projection.ReadModel &&
                (variant == "every-whole-number" || variant == "every-enumerated-concept" || variant == "expression-read-model-property" || variant == "flat-expression-read-model-property") ? model with
            {
                Properties = [.. model.Properties.Select(property => ChangedReadModelProperty(
                    property,
                    variant,
                    visitsTarget,
                    lastSeenTarget,
                    original.Application.Concepts.Single(concept => concept.Name == "ProjectName").Id))]
            } : model)],
            Projections = [.. view.Projections.Select(value => value.Id == projection.Id ? changedProjection : value)]
        };
        var application = original.Application with
        {
            Concepts = [.. original.Application.Concepts.Select(concept => variant == "every-enumerated-concept" && concept.Name == "ProjectName" ? concept with { Values = ["Earlier", "Screenplay", "Pinned", "allowed"] } : concept)],
            Modules = [module with { Features = [feature with { Slices = [.. feature.Slices.Select(slice => slice.Id == view.Id ? changed : slice with
            {
                Events = variant == "expression-event-property" || variant == "flat-expression-event-property" || variant == "derived-event-property" ? [.. slice.Events.Select(@event => @event.Name == "ProjectRegistered" ? @event with
                {
                    Properties = [.. @event.Properties.Select(property => property.Name == "name" ? property with { Name = variant == "derived-event-property" ? "Week()" : "$value(0)" } : property)]
                } : @event)] : slice.Events
            })] }] }]
        };
        var model = ExecutableSemanticModel.Create(original.LanguageVersion, original.SemanticVersion, application);
        var execution = SemanticExecutionPlan.Compile(original).Plan!;
        var options = new CratisRenderingOptions("Projects", "Projects");
        var profile = CratisRendering.CreateProfile(model.Application.Name, options);
        var request = new ArtifactRenderRequest(model, execution, profile, new(ArtifactRenderScopeKind.Application, model.Application.Id));
        var context = new SemanticApplicationContext(request, options);
        var diagnostics = SemanticCratisAdmission.Evaluate(context, context.SelectedSlices());
        var diagnostic = Assert.Single(diagnostics, _ => _.Code == "STAGE-ESM-017" && _.Artifact == projection.Id);
        if (variant == "every-including-children" || variant == "nested-join" || variant == "root-join-removal")
        {
            Assert.Contains("Chronicle#4125", diagnostic.Message, StringComparison.Ordinal);
        }

        if (variant == "composite-key")
        {
            Assert.Contains("keyed lookup", diagnostic.Message, StringComparison.Ordinal);
        }

        if (variant == "join-event-source")
        {
            Assert.Contains("A join has an unsupported correlation key or mapping", diagnostic.Message, StringComparison.Ordinal);
        }

        if (variant == "nested-clear-with-root-from")
        {
            Assert.Contains("Chronicle#4166", diagnostic.Message, StringComparison.Ordinal);
        }

        if (variant == "composite-key" || variant == "every-whole-number" || variant == "every-enumerated-concept" ||
            variant == "event-source-with-property-key" || variant == "join-event-source" || variant == "expression-event-property" ||
            variant == "derived-event-property" || variant == "flat-expression-event-property" ||
            variant == "expression-read-model-property" || variant == "flat-expression-read-model-property")
        {
            var changedExecution = SemanticExecutionPlan.Compile(model);
            Assert.True(changedExecution.Success, string.Join(Environment.NewLine, changedExecution.Issues));
            var blocked = CratisRendering.Plan(model, changedExecution.Plan!, new(ArtifactRenderScopeKind.Application, model.Application.Id), options);
            Assert.False(blocked.Success);
            Assert.Empty(blocked.Artifacts);
            Assert.Contains(blocked.Diagnostics, item => item.Code == "STAGE-ESM-017");
        }
    }

    static SemanticProperty ChangedReadModelProperty(SemanticProperty property, string variant, SemanticId visits, SemanticId lastSeen, SemanticId concept)
    {
        if ((variant == "expression-read-model-property" || variant == "flat-expression-read-model-property") && property.Name == "name")
        {
            return property with { Name = "name.Length" };
        }

        if (variant == "every-whole-number" && property.Id == visits)
        {
            return property with { Type = SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.WholeNumber) };
        }

        if (variant == "every-enumerated-concept" && property.Id == lastSeen)
        {
            return property with { Type = SemanticTypeReference.ForConcept(concept) with { IsOptional = true } };
        }

        return property;
    }

    internal static void VerifyFromAllAdmission()
    {
        const string source = when_rendering_scoped_projections.ScopedSource + "\n" + """
                slice StateView AllLookup
                  readmodel AllSummary
                    projectId ProjectId
                    lastSeen ProjectId?
                  query AllById => AllSummary?
                    by projectId ProjectId
                  projection AllSummaryProjection => AllSummary
                    from ProjectRegistered key projectId
                    all
                      lastSeen = $eventSourceId
            """;
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("all"), "all", "All.play", source);
        var compilation = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join("; ", compilation.Diagnostics.Select(diagnostic => diagnostic.Message)));
        var model = compilation.Value!.Model;
        var execution = SemanticExecutionPlan.Compile(model);
        Assert.True(execution.Success, string.Join("; ", execution.Issues));
        var plan = CratisRendering.Plan(model, execution.Plan!, new(ArtifactRenderScopeKind.Application, model.Application.Id), new("Projects", "Projects"));
        Assert.False(plan.Success);
        Assert.Empty(plan.Artifacts);
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-017" && diagnostic.Message.Contains("MongoDB", StringComparison.Ordinal));
    }

    sealed class UnknownScopeVariant(string name) : Exception($"Unknown projection scope variant '{name}'.");
}
