// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Semantics;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_admitting_v3;

public class with_a_reducer_fed_by_a_refused_action : given.a_body_reach_plan
{
    SemanticSpecification _specification = null!;
    SemanticUnsupportedCapability? _refusal;

    void Establish() => _specification = _plan.Specifications.Values.Single(specification => specification.Name == "Feeding") with
    {
        ThenEvents = [],
        ThenDenied = true
    };

    void Because() => _refusal = SemanticRunAdmission.Check(_plan, _specification);

    [Fact] void should_refuse_the_specification() => _refusal.ShouldNotBeNull();
    [Fact] void should_start_the_refusal_details_with_the_implementation_body_code() => _refusal!.Details.StartsWith("STAGE-IMPL-003:", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_identify_the_reducer_body() => _refusal!.Details.ShouldContain("Reducer 'Fold' transition");
}
