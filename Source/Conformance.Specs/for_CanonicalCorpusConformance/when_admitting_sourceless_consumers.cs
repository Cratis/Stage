// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSpecificationAdmission;
using Cratis.Stage.Specifications;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance;

public class when_admitting_sourceless_consumers : Specification
{
    [Fact]
    void should_refuse_a_source_observer_without_an_existing_projection_refusal()
    {
        var refusal = Admission(sourced: false, observed: true);
        Assert.NotNull(refusal);
        Assert.StartsWith("STAGE-ESM-051:", refusal.Details, StringComparison.Ordinal);
        Assert.Contains("Cratis/Stage#960", refusal.Details, StringComparison.Ordinal);
    }

    [Fact]
    void should_keep_an_explicitly_sourced_given_admitted() => Assert.Null(Admission(sourced: true, observed: true));

    [Fact]
    void should_keep_an_unobserved_sourceless_given_admitted() => Assert.Null(Admission(sourced: false, observed: false));

    static SemanticUnsupportedCapability? Admission(bool sourced, bool observed)
    {
        var model = invoice_model.Compile(when_checking_sourceless_event_consumers.Source);
        var module = model.Application.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single();
        var contract = slice.Events.Single();
        if (observed) slice = slice with { Constraints = [new("UniquePayment", SemanticConstraintKind.UniqueEventOccurrence, SemanticConstraintScope.EventSequence, [new(contract.Id, [])], [], false, null)] };
        if (sourced)
        {
            var projection = slice.Projections.Single();
            var contribution = projection.Scope.From.Single();
            contribution = contribution with { Key = SemanticProjectionKey.EventSourceIdentity, Mappings = [contribution.Mappings[0] with { Source = new SemanticProjectionEventSourceIdentity() }, .. contribution.Mappings.Skip(1)] };
            slice = slice with { Projections = [projection with { Scope = projection.Scope with { From = [contribution] } }] };
            var specification = slice.Specifications.Single();
            var given = specification.GivenEvents.Single();
            given = given with { EventSource = new(contract.Properties.Single(property => property.Name == "invoiceId").Type, SemanticValue.Text("INV-1")) };
            slice = slice with { Specifications = [specification with { GivenEvents = [given], WhenAppended = specification.WhenAppended with { EventSource = given.EventSource } }] };
        }
        var application = model.Application with { Modules = [module with { Features = [feature with { Slices = [slice] }] }] };
        var plan = SemanticExecutionPlan.Compile(ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, application)).Plan!;
        return SemanticSpecificationAdmission.Check(plan, plan.Specifications.Values.Single());
    }
}
#endif
