// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_newer_semantic_constructs;

public class with_an_unrepresentable_validation_message : given.an_invoice_model
{
    void Because() => Plan(Invoices.Replace(
        "        produces InvoiceIssued\n",
        "        validate\n          description not empty message \"{PropertyName} must be present\"\n        produces InvoiceIssued\n",
        StringComparison.Ordinal));

    [Fact] void should_not_plan_any_artifacts() => _plan.Artifacts.ShouldBeEmpty();
    [Fact] void should_reject_fluent_validation_message_substitution() => ErrorCodes.ShouldContainOnly(["STAGE-ESM-005"]);
}
