// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Cratis.Stage.Contracts;
using Cratis.Stage.Contracts.Screenplay;
using Xunit;

namespace Cratis.Stage.SpecRunner.for_Program;

[Collection("SpecRunner application boundary culture")]
public class when_refusing_an_unknown_validation_rule : Specification
{
    readonly string _folder = Path.Combine(Path.GetTempPath(), $"stage unknown rule runner {Guid.NewGuid():N}");
    readonly StringWriter _error = new();
    int _exitCode;

    void Establish() => Directory.CreateDirectory(_folder);

    async Task Because() => _exitCode = await Program.Run(["--model", "model.play", "--output", Path.Combine(_folder, "results.json")], TextWriter.Null, _error, _ =>
    {
        var rule = new ValidationRuleSyntax("reference", (ValidationRuleKind)999, null, null, SourceLocation.Start);
        ValidationRuleConverter.Convert([new DeclarativeValidateSyntax([rule], SourceLocation.Start)]);
        return Task.FromResult<EventModel>(null!);
    });

    [Fact] void should_exit_with_a_refusal() => _exitCode.ShouldEqual(1);
    [Fact] void should_not_write_results() => File.Exists(Path.Combine(_folder, "results.json")).ShouldBeFalse();
    [Fact] void should_report_the_code() => _error.ToString().ShouldContain("STAGE-ESM-005");
    [Fact] void should_report_the_kind() => _error.ToString().ShouldContain("999");
    [Fact] void should_report_the_property() => _error.ToString().ShouldContain("reference");

    void Destroy()
    {
        _error.Dispose();
        Directory.Delete(_folder, true);
    }
}
#endif
