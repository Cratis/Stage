// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_validation_bodies;

public class with_non_error_severities : Specification
{
    [Theory]
    [InlineData("warning", FluentValidation.Severity.Warning)]
    [InlineData("information", FluentValidation.Severity.Info)]
    void should_preserve_the_failure_severity_and_blocking_floor(string severity, FluentValidation.Severity expected)
    {
        var source = validation_body_model.Command("return false;", message: $" severity {severity} message \"Rejected\"", generated: true);
        var plan = opaque_policy_model.Plan(validation_body_model.Load(source));
        Assert.True(plan.Success, opaque_policy_model.Errors(plan));
        var assembly = opaque_policy_model.Compile(plan);
        validation_body_model.Validate(assembly, "PlaceOrder", 1m).Errors.Single().Severity.ShouldEqual(expected);
        plan.Artifacts.ShouldContain(artifact => System.Text.Encoding.UTF8.GetString(artifact.Bytes.AsSpan()).Contains("BlockOnValidationSeverity(ValidationResultSeverity.Information)", StringComparison.Ordinal));
    }
}
#endif
