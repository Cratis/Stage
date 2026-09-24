// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_executing_primitive_invoice_specifications;

public class with_wrong_read_model_expectation : a_generated_invoice_application
{
    protected override string InvoiceSource => register_project_scenarios.WithPriorProjectAndSubsetExpectation.Replace(
        "then readmodel ProjectSummary\n          projectId = \"3fa85f64-5717-4562-b3fc-2c963f66afa6\"",
        "then readmodel ProjectSummary\n          projectId = \"3fa85f64-5717-4562-b3fc-2c963f66afa6\"\n          name = \"Not the projected name\"",
        StringComparison.Ordinal);

    Exception? _error;

    async Task Because() => _error = await Catch.Exception(VerifyGeneratedApplication);

    [Fact] void should_fail_the_generated_read_model_specification_not_the_build() => _error!.Message.ShouldContain("should_project_name");
    [Fact] void should_report_the_wrong_expectation_as_a_failed_fact() => _error!.Message.ShouldContain("Failed");
}
#endif
