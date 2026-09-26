// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_newer_semantic_constructs;

public class with_an_error_validation : given.an_invoice_model
{
    void Because() => Plan(Invoices.Replace(
        "        produces InvoiceIssued\n",
        "        validate\n          description not empty severity error\n        produces InvoiceIssued\n",
        StringComparison.Ordinal));

    [Fact] void should_plan_the_application() => _plan.Success.ShouldBeTrue();
    [Fact] void should_keep_the_error_blocking_floor() => Artifact("Issue.cs").ShouldContain("BlockOnValidationSeverity(ValidationResultSeverity.Error)");
    [Fact] void should_use_the_default_error_rule_severity() => Artifact("Issue.cs").ShouldNotContain(".WithSeverity(ValidationResultSeverity.Error)");
}
