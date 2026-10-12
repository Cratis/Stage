// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

public class when_admitting_the_v13_golden_vector : Specification
{
    SemanticExecutionPlan _plan = null!;
    SemanticSpecificationRunReport _report = null!;

    void Establish()
    {
        // The renderer embeds the owned vector. This project copies the same bytes: an EmbeddedResource
        // here creates a cycle between Arc's embedded-Screenplay and Roslyn resource targets.
        _plan = SemanticExecutionPlan.Compile(SemanticModelSerializer.Deserialize(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "full-esm-v13.json"))), []).Plan!;
    }

    async Task Because() => _report = await new SemanticSpecificationExecutor().Run(_plan, new([]), new());

    [Fact] void should_read_the_full_v13_vector() => _plan.Model.SemanticVersion.ShouldEqual(SemanticVersion.V13);
    [Fact] void should_run_a_nonempty_specification_set() => _report.Results.ShouldNotBeEmpty();
    [Fact] void should_not_refuse_the_audited_version() => _report.Results.Any(result => result.Unsupported?.Details.StartsWith("STAGE-ESM-016:", StringComparison.Ordinal) == true).ShouldBeFalse();
    [Fact] void should_not_misclassify_valid_compliance() => _report.Results.Any(result => result.Unsupported?.Details.StartsWith("STAGE-ESM-040:", StringComparison.Ordinal) == true).ShouldBeFalse();
}
