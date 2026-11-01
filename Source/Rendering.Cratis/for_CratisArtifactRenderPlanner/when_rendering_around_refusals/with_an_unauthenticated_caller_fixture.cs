// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_around_refusals;

public class with_an_unauthenticated_caller_fixture : given.a_refused_specification
{
    void Establish()
    {
        const string source = when_rendering_portable_authorization.Source + "\n      specification ImpossibleGuest\n        given caller\n          role \"Staff\"\n        then query InvoiceById\n          arguments\n            invoiceId = \"invoice-one\"\n        then denied\n";
        var model = for_CratisArtifactRenderPlanner.given.invoice_model.Compile(source);
        _refused = model.Application.Modules.Single().Features.Single().Slices.SelectMany(slice => slice.Specifications).Single().Id;
        _request = OptIn(new(
            model,
            SemanticExecutionPlan.Compile(model).Plan!,
            CratisRendering.CreateProfile(model.Application.Name, new("InvoiceApp", "Invoices")),
            new(ArtifactRenderScopeKind.Application, model.Application.Id)));
    }

    void Because() => _plan = _planner.Plan(_request);

    [Fact] void should_render_a_complete_plan() => _plan.Outcome.ShouldEqual(ArtifactRenderOutcome.Complete);
    [Fact] void should_list_the_guest_fixture() => _plan.Refusals.Single().Artifact.ShouldEqual(_refused);
    [Fact] void should_omit_the_guest_fixture() => _plan.Artifacts.SelectMany(artifact => artifact.Sources).ShouldNotContain(_refused);
    [Fact] void should_keep_authorization_in_the_application() => _plan.Artifacts.ShouldContain(artifact => artifact.RelativePath.Contains("GeneratedPolicies", StringComparison.Ordinal));
}
