// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Host.for_SemanticSpecificationRuns.given;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticSpecificationRuns;

public class when_deduplicating_scopes : a_specification_endpoint
{
    private protected override ISpecificationRunProcess Process { get; } = Substitute.For<ISpecificationRunProcess>();
    string[] _arguments = [];

    void Establish() => Process.Run(Arg.Any<ProcessStartInfo>(), Arg.Any<CancellationToken>()).Returns(async call =>
    {
        _arguments = [.. call.Arg<ProcessStartInfo>().ArgumentList];
        var id = _plan.Specifications.Keys.Single().ToString();
        var report = new SemanticSpecificationRunReport("stage-spec-run/1", _plan.Model.Application.Id.ToString(), _plan.Revision.ToString(), [new(id, "RegisteringAProject", string.Empty, "StateChange", SemanticSpecificationOutcome.Passed, "Accepted", null, [], null)]);
        await SemanticSpecificationRunReportFile.WriteToFile(report, _arguments[Array.IndexOf(_arguments, "--output") + 1]);
        return new SpecificationProcessResult(0, string.Empty, string.Empty);
    });

    async Task Because() => await Request($$"""{"scopes":["{{_plan.Specifications.Keys.Single()}}","{{_plan.Specifications.Keys.Single()}}"]}""");

    [Fact] void should_forward_one_scope() => _arguments.Count(argument => argument == "--scope").ShouldEqual(1);
    [Fact] void should_return_one_result() => Report.Results.Count.ShouldEqual(1);
}
