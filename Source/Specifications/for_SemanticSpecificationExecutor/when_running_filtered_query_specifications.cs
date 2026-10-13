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
        _plan = SemanticExecutionPlan.Compile(ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, application)).Plan!;
    }

    async Task Because() => _report = await new SemanticSpecificationExecutor().Run(_plan, new([]), new());

    [Fact] void should_report_unsupported_instead_of_passing() => _report.Results.Select(result => result.Outcome).Distinct().ShouldContainOnly(SemanticSpecificationOutcome.Unsupported);
    [Fact] void should_name_the_filtered_query_gap() => _report.Results.All(result => result.Unsupported!.Capability == StageExecutionCapability.Query && result.Unsupported.Details.StartsWith("STAGE-ESM-010:", StringComparison.Ordinal)).ShouldBeTrue();
}
