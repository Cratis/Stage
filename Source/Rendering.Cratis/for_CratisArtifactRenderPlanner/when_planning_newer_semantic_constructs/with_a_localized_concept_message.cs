// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_newer_semantic_constructs;

public class with_a_localized_concept_message : given.an_invoice_model
{
    void Because() => Plan("concept Display : String\n  validate\n    not empty message $strings.display.required\n" + Invoices);

    [Fact] void should_not_plan_any_artifacts() => _plan.Artifacts.ShouldBeEmpty();
    [Fact] void should_reject_the_unresolved_string_key() => ErrorCodes.ShouldContainOnly(["STAGE-ESM-002"]);
}
