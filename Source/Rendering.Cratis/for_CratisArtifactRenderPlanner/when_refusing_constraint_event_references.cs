// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_refusing_constraint_event_references : Specification
{
    [Theory]
    [InlineData("target", "selected", "STAGE-ESM-031")]
    [InlineData("release", "selected", "STAGE-ESM-032")]
    [InlineData("target", "sibling", "STAGE-ESM-031")]
    [InlineData("release", "sibling", "STAGE-ESM-032")]
    public void should_refuse_every_rendered_constraint_dependency(string reference, string owner, string code)
    {
        var original = SemanticModelSerializer.Deserialize(PublicEventsCorpus.V9.EsmBytes.AsSpan());
        var module = original.Application.Modules.Single();
        var feature = module.Features.Single();
        var selected = feature.Slices.Single(slice => slice.Name == "PackOrder");
        var publisher = feature.Slices.Single(slice => slice.Direction == SemanticTranslationDirection.Outbound);
        var foreign = feature.Slices.SelectMany(slice => slice.Events).Single(@event => @event.Origin is not null);
        var target = reference == "target" ? publisher.Events.Single() : selected.Events.Single();
        var constraint = new SemanticConstraint("PublicBoundary", SemanticConstraintKind.UniquePropertyValue, SemanticConstraintScope.EventSequence, [new(target.Id, [target.Properties.Single(property => property.Name == "carrier").Id])], reference == "release" ? [foreign.Id] : [], false, null);
        if (reference == "target" && owner == "sibling")
        {
            var produced = selected.Events.Single();
            constraint = constraint with { Targets = [.. constraint.Targets, new(produced.Id, [produced.Properties.Single().Id])] };
        }
        var ownerId = owner == "selected" ? selected.Id : publisher.Id;
        var application = original.Application with
        {
            Modules = [module with { Features = [feature with
            {
                Slices = [.. feature.Slices.Select(slice => slice.Id == ownerId ? slice with { Constraints = [constraint] } : slice)]
            }] }]
        };

        // The ESM contract admits cross-slice constraints, including foreign releasing events that the source binder restricts.
        var model = ExecutableSemanticModel.Create(original.LanguageVersion, original.SemanticVersion, application);
        var plan = CratisRendering.Plan(model, SemanticExecutionPlan.Compile(model).Plan!, new(ArtifactRenderScopeKind.Slice, selected.Id), new("Contracts", "Contracts"));
        plan.Diagnostics.Select(diagnostic => diagnostic.Code).Distinct().ShouldContainOnly([code]);
        plan.Success.ShouldBeFalse();
        plan.Artifacts.ShouldBeEmpty();
    }
}
