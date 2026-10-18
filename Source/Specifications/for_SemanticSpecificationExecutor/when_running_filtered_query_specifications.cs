// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

public class when_running_filtered_query_specifications : Specification
{
    SemanticExecutionPlan _plan = null!;
    SemanticSpecificationRunReport _report = null!;
    SemanticExecutionPlan _unseeded = null!;
    SemanticSpecificationRunReport _unseededReport = null!;

    void Establish()
    {
        var model = SemanticModelSerializer.Deserialize(QueryFiltersCorpus.V14.EsmBytes.AsSpan());
        var module = model.Application.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single();
        var application = model.Application with
        {
            Modules = [module with { Features = [feature with { Slices = [slice with { Specifications = [.. slice.Specifications.Select(specification => specification with { GivenReadModels = [] })] }] }] }]
        };
        _unseeded = SemanticExecutionPlan.Compile(ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, application)).Plan!;
        _plan = SemanticExecutionPlan.Compile(model).Plan!;
    }

    async Task Because()
    {
        var executor = new SemanticSpecificationExecutor();
        _report = await executor.Run(_plan, new([]), new());
        _unseededReport = await executor.Run(_unseeded, new([]), new());
    }

    [Fact] void should_run_both_canonical_filtered_specifications() => _report.Results.Select(result => result.Outcome).ShouldContainOnly(SemanticSpecificationOutcome.Passed, SemanticSpecificationOutcome.Passed);
    [Fact] void should_match_the_reference() => _plan.Specifications.Values.ShouldEachConformTo(specification => new SemanticSpecificationRunner().Run(_plan, specification.Id).Passed);
    [Fact] void should_refuse_missing_query_state_instead_of_passing() => _unseededReport.Results.Select(result => result.Outcome).Distinct().ShouldContainOnly(SemanticSpecificationOutcome.Unsupported);
    [Fact] void should_report_a_typed_query_state_refusal() => _unseededReport.Results.All(result => result.Unsupported!.Capability == StageExecutionCapability.Query).ShouldBeTrue();
}
