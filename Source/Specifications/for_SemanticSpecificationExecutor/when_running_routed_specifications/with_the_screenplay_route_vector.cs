// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_routed_specifications;

public class with_the_screenplay_route_vector : a_routed_plan
{
    SemanticSpecificationRunReport _report = null!;
    bool _referencePassed;

    void Establish()
    {
        // Vendored verbatim from Screenplay v4.114.0 Source/Screenplay/Compiler/Conformance.
        var path = Path.Combine(AppContext.BaseDirectory, "for_SemanticSpecificationExecutor", "when_running_routed_specifications", "Vectors", "specification-example-routes.play");

        // Its exact-numbers directive is syntax-only at this pin (#285). These integer fixtures are unchanged
        // by executing the route vector in the admitted numeric mode, in both Stage and the reference runner.
        _plan = Compile(File.ReadAllText(path).Replace("numbers exact\n", string.Empty, StringComparison.Ordinal));
    }

    async Task Because()
    {
        _referencePassed = _plan.Specifications.Values.All(specification => new SemanticSpecificationRunner().Run(_plan, specification.Id).Passed);
        _report = await new SemanticSpecificationExecutor().Run(_plan, new([]), new());
    }

    [Fact] void should_pass_both_reference_specifications() => _referencePassed.ShouldBeTrue();
    [Fact] void should_execute_both_vendored_specifications() => _report.Results.Count.ShouldEqual(2);
    [Fact] void should_pass_both_stage_specifications() => _report.Results.All(result => result.Outcome == SemanticSpecificationOutcome.Passed).ShouldBeTrue();
}
