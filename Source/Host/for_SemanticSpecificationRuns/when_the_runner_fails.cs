// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Text.Json;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticSpecificationRuns.given;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticSpecificationRuns;

public class when_the_runner_fails : a_specification_endpoint
{
    protected virtual string? ReportContent => null;
    protected virtual int ExitCode => 1;
    private protected override ISpecificationRunProcess Process { get; } = Substitute.For<ISpecificationRunProcess>();
    string _output = null!;

    void Establish() => Process.Run(Arg.Any<ProcessStartInfo>(), Arg.Any<CancellationToken>()).Returns(async call =>
    {
        var arguments = call.Arg<ProcessStartInfo>().ArgumentList.ToArray();
        _output = arguments[Array.IndexOf(arguments, "--output") + 1];
        if (ReportContent is { } content) await File.WriteAllTextAsync(_output, content);
        return new SpecificationProcessResult(ExitCode, "runner output", "runner error");
    });

    async Task Because() => await Request("{}");

    [Fact] void should_return_a_gateway_failure() => _status.ShouldEqual(StatusCodes.Status502BadGateway);
    [Fact] void should_identify_the_failed_capability() => JsonDocument.Parse(_body).RootElement.GetProperty("capability").GetString().ShouldEqual("Specification");
    [Fact] void should_delete_the_run_directory() => Directory.Exists(Path.GetDirectoryName(_output)).ShouldBeFalse();
}
