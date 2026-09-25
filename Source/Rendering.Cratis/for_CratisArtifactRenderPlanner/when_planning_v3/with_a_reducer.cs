// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_v3.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_v3;

public class with_a_reducer : Specification
{
    ArtifactRenderPlan _plan = null!;

    void Because()
    {
        var source = invoice_model.Source("String", invoice_model.TextSource, invoice_model.OtherTextSource) + "\n    slice StateView InvoiceView\n      readmodel InvoiceSnapshot\n        streamReference String\n        description String\n      query InvoiceById => InvoiceSnapshot?\n        by streamReference String\n";
        var readModel = invoice_model.Compile(source).Application.Modules.Single().Features.Single().Slices.Single(slice => slice.Kind == SemanticSliceKind.StateView).ReadModels.Single();
        _plan = a_v3_invoice.Plan(a_v3_invoice.Model(slice => slice with
        {
            ReadModels = [readModel],
            Reducers = [new("InvoiceReducer", readModel.Id, [new(slice.Events.Single().Id, "reducer-body")])]
        }));
    }

    [Fact] void should_reject_the_reducer_with_019() => _plan.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain("STAGE-ESM-019");
    [Fact] void should_name_the_missing_context_contract() => _plan.Diagnostics.Single(diagnostic => diagnostic.Code == "STAGE-ESM-019").Message.ShouldContain("per-transition typed context descriptor for State and Event (ContextVersion 1, ResultVersion 1)");
    [Fact] void should_emit_no_artifacts() => _plan.Artifacts.ShouldBeEmpty();
}
