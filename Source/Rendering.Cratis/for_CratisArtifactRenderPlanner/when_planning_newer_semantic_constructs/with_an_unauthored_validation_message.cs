// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_newer_semantic_constructs;

public class with_an_unauthored_validation_message : given.an_invoice_model
{
    void Because() => Plan(Invoices.Replace(
        "        produces InvoiceIssued\n",
        "        validate\n          description max 20\n        produces InvoiceIssued\n",
        StringComparison.Ordinal));

    [Fact] void should_plan_the_application() => _plan.Success.ShouldBeTrue();
    [Fact] void should_render_the_reference_default_message() => Artifact("Issue.cs").ShouldContain(".WithMessage(\"A value must be at most 20 characters long.\")");
}
