// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_executing_command_clock_validations : a_generated_application
{
    const string Source = """
        concept Moment : DateTime
        module Scheduling
          feature Windows
            slice StateChange Keep
              command Keep
                id String identifier
                at DateTime
                optionalAt DateTime optional
                optionalMoment Moment optional
                validate
                  at >= now
                  optionalAt >= now
                  optionalMoment >= now
                produces Kept
                  for id
                  recordedAt = $context.occurred
              event Kept
                recordedAt DateTime
              specification AtTheReceiptWithAbsentOptionalInputs
                given clock "2026-01-02T00:00:00.0000000Z"
                when Keep
                  id = "window"
                  at = "2026-01-02T00:00:00.0000000Z"
                  optionalAt = null
                  optionalMoment = null
                then Kept
                  recordedAt = "2026-01-02T00:00:00.0000000Z"
              specification BeforeTheReceipt
                given clock "2026-01-02T00:00:00.0000000Z"
                when Keep
                  id = "window"
                  at = "2026-01-01T23:59:59.9990000Z"
                  optionalAt = null
                  optionalMoment = null
                then error "A value must be at least '2026-01-02T00:00:00.0000000Z'."
              specification BeforeTheReceiptInAnOptionalPrimitive
                given clock "2026-01-02T00:00:00.0000000Z"
                when Keep
                  id = "window"
                  at = "2026-01-02T00:00:00.0000000Z"
                  optionalAt = "2026-01-01T23:59:59.9990000Z"
                  optionalMoment = null
                then error "A value must be at least '2026-01-02T00:00:00.0000000Z'."
              specification BeforeTheReceiptInAnOptionalConcept
                given clock "2026-01-02T00:00:00.0000000Z"
                when Keep
                  id = "window"
                  at = "2026-01-02T00:00:00.0000000Z"
                  optionalAt = null
                  optionalMoment = "2026-01-01T23:59:59.9990000Z"
                then error "A value must be at least '2026-01-02T00:00:00.0000000Z'."
        """;

    string _debug = null!;
    string _tests = null!;

    protected override ArtifactRenderPlan CreatePlan()
    {
        var plan = invoice_model.Plan(invoice_model.Compile(Source));
        plan.Artifacts.Count(artifact => artifact.RelativePath.Contains("/when_", StringComparison.Ordinal)).ShouldEqual(4);
        return plan;
    }

    async Task Because()
    {
        _debug = await Run("clock-validation-debug.log", "build", "-c", "Debug", "-warnaserror");
        _tests = await Run("clock-validation-tests.log", "test", "-c", "Debug", "--no-build");
    }

    [Fact]
    void should_execute_the_clock_rules_and_optional_absence_with_the_receipt_instant()
    {
        BuildWarnings(_debug).ShouldEqual(string.Empty);
        _tests.ShouldContain("Passed!");
    }
}
#endif
