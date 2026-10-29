// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_ThreeWayConformance;

public class when_refusing_generated_occurrence_specifications : Specification
{
    [Theory]
    [InlineData("command", "STAGE-ESM-011")]
    [InlineData("append", "STAGE-ESM-011")]
    [InlineData("clocked-append", "STAGE-ESM-024")]
    [InlineData("clocked-redelivery", "STAGE-ESM-024")]
    public void should_keep_backend_occurrence_support_separate_from_generated_scenario_support(string form, string code)
    {
        var clock = form == "command" || form.StartsWith("clocked-", StringComparison.Ordinal) ? "given clock \"2027-03-04T08:00:00.123Z\"\n" : string.Empty;
        var action = form switch
        {
            "command" => "when Begin\n  item = \"one\"",
            "clocked-redelivery" => "given Started\n  for \"one\"\nwhen redelivered Started to Worker",
            _ => "when append Started\n  for \"one\""
        };
        const string source = """
            module Work
              feature Items
                slice Automation Process
                  command Begin
                    item String identifier
                    produces Started
                      for item
                  event Started
                  event Noted
                    notedAt DateTime
                  reaction Worker
                    when Started
                      produces Noted
                        notedAt = $context.occurred
            """;
        var model = invoice_model.Compile(source + "\n      specification Exercising\n" + Indent(clock + action, 8) + "\n" + Indent("then Noted\n  for \"one\"\n  notedAt = \"2027-03-04T08:00:00.1230000Z\"", 8));
        var rendered = invoice_model.Plan(model);
        var specification = model.Application.Modules.Single().Features.Single().Slices.Single().Specifications.Single();
        rendered.Success.ShouldBeTrue();
        rendered.Outcome.ShouldEqual(ArtifactRenderOutcome.Complete);
        rendered.Refusals.Single().Artifact.ShouldEqual(specification.Id);
        rendered.Artifacts.SelectMany(artifact => artifact.Sources).ShouldNotContain(specification.Id);
        rendered.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == code && diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Warning);

        var backend = invoice_model.Compile(source);
        var backendRendering = invoice_model.Plan(backend);
        Assert.True(backendRendering.Success, string.Join(';', backendRendering.Diagnostics));
        backendRendering.Artifacts.ShouldNotBeEmpty();
    }

    static string Indent(string source, int spaces) => string.Join('\n', source.Split('\n').Select(line => new string(' ', spaces) + line));
}
#endif
