// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Contracts.Semantics.for_SemanticModelLoader;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Xunit;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

public class when_running_a_model_with_slice_local_plan_issues : Specification
{
    SemanticSpecificationRunReport _report = null!;
    LoadedSemanticModel _loaded = null!;

    void Establish() => _loaded = when_loading_a_model_with_slice_local_plan_issues.Create().Loaded!;

    async Task Because() => _report = await new SemanticSpecificationExecutor().Run(_loaded.Plan, new([_loaded.Model.Application.Id]), new());

    [Fact] void should_run_the_supported_sibling() => _report.Results.Single(result => result.Name == "Registering").Outcome.ShouldEqual(SemanticSpecificationOutcome.Passed);
    [Fact] void should_refuse_the_required_query_assertion() => _report.Results.Single(result => result.Name == "LookingUp").Outcome.ShouldEqual(SemanticSpecificationOutcome.Unsupported);
    [Fact] void should_identify_the_refused_query() => _report.Results.Single(result => result.Name == "LookingUp").Unsupported!.Construct.ShouldEqual(_loaded.Plan.Queries.Keys.Single().ToString());
    [Fact] void should_report_a_typed_query_refusal() => _report.Results.Single(result => result.Name == "LookingUp").Unsupported!.Details.ShouldContain("STAGE-PLAN-004:");
}
