// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_newer_semantic_constructs;

/// <summary>
/// Only occurred can be captured by a model-bound handler; causation values still fail closed.
/// </summary>
public class with_an_unsupported_command_occurrence_mapping : given.an_invoice_model
{
    void Because() => Plan(Invoices.Replace("          description = description\n", "          description = $context.causedBy.name\n", StringComparison.Ordinal));

    [Fact] void should_report_the_unsupported_context() => ErrorCodes.ShouldContain("STAGE-ESM-013");
    [Fact] void should_not_generate_artifacts() => _plan.Artifacts.ShouldBeEmpty();
}
