// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_routed_specifications;

public class with_unrouted : a_routed_plan
{
    bool _unroutedPassed;
    bool _unroutedRouteAbsent;

    async Task Because()
    {
        var expected = _specification.ThenEvents[0] with { Route = null, Unrouted = true };
        var specification = _specification with { ThenEvents = [expected] };
        await Run(With(specification, _command with { Route = null }));
        AssertParity(true);
        _unroutedPassed = _reference.Passed;
        var report = new SemanticSpecificationRunReport("stage-spec-run/1", _plan.Model.Application.Id.ToString(), _plan.Revision.ToString(), [_result]);
        using var json = JsonDocument.Parse(SemanticSpecificationRunReportFile.Write(report));
        _unroutedRouteAbsent = !json.RootElement.GetProperty("results")[0].GetProperty("trace").GetProperty("facts")[0].TryGetProperty("route", out _);
        await Run(With(specification));
    }

    [Fact] void should_pass_for_an_unrouted_command() => _unroutedPassed.ShouldBeTrue();
    [Fact] void should_fail_for_a_routed_command_like_the_reference() => AssertParity(false);
    [Fact] void should_omit_the_route_from_an_unrouted_report() => _unroutedRouteAbsent.ShouldBeTrue();
    [Fact] void should_report_the_route_even_when_the_expectation_fails() => _result.Trace!.Facts.Single().Route.ShouldNotBeNull();
}
