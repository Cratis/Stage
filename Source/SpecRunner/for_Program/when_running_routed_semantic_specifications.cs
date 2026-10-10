// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Xunit;

namespace Cratis.Stage.SpecRunner.for_Program;

[Collection("SpecRunner application boundary culture")]
public class when_running_routed_semantic_specifications : Specification
{
    readonly string _folder = Path.Combine(Path.GetTempPath(), $"stage routed semantic runner {Guid.NewGuid():N}");
    SemanticSpecificationRunReport _report = null!;
    int _exitCode;

    void Establish()
    {
        Directory.CreateDirectory(_folder);
        File.WriteAllText(Path.Combine(_folder, "Routed.play"), """
            eventsource Account
              identifier String
              stream Entries
                streamId String
            module Banking
              feature Deposits
                slice StateChange Deposit
                  command Deposit
                    accountId String identifier
                    period String
                    stream Account.Entries
                      streamId = period
                    produces Deposited
                      for accountId
                  event Deposited
                  specification Depositing
                    when Deposit
                      accountId = "acc-1"
                      period = "2026-10"
                    then Deposited
                      stream Account.Entries
                        streamId = "2026-10"
            """);
    }

    async Task Because()
    {
        var result = Path.Combine(_folder, "semantic.json");
        _exitCode = await Program.Run(["--engine", "semantic", "--model", _folder, "--output", result], TextWriter.Null, TextWriter.Null);
        if (File.Exists(result)) _report = await SemanticSpecificationRunReportFile.ReadFromFile(result);
    }

    [Fact] void should_complete_the_semantic_run() => _exitCode.ShouldEqual(0);
    [Fact] void should_execute_the_route_expectation() => _report.Results.Single().Outcome.ShouldEqual(SemanticSpecificationOutcome.Passed);

    void Destroy() => Directory.Delete(_folder, true);
}
#endif
