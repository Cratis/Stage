// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_newer_semantic_constructs;

public class with_an_informational_requirement : given.an_invoice_model
{
    void Because() => Plan(Invoices.Replace(
        "        produces InvoiceIssued\n",
        "        validate\n          require description != \"draft\"\n            severity information\n        produces InvoiceIssued\n",
        StringComparison.Ordinal));

    [Fact] void should_plan_the_application() => _plan.Success.ShouldBeTrue();
    [Fact] void should_block_the_informational_failure() => Artifact("Issue.cs").ShouldContain("BlockOnValidationSeverity(ValidationResultSeverity.Information)");
    [Fact] void should_preserve_the_informational_severity() => Artifact("Issue.cs").ShouldContain("WithSeverity(ValidationResultSeverity.Information)");
}
