// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCommandClockRendering;

public class when_rendering_clock_validations : Specification
{
    [Theory]
    [InlineData("==", "==")]
    [InlineData("!=", "!=")]
    [InlineData(">", ">")]
    [InlineData(">=", ">=")]
    [InlineData("<", "<")]
    [InlineData("<=", "<=")]
    public void should_render_instant_comparisons_and_resolved_default_messages(string operation, string symbol)
    {
        foreach (var concept in new[] { false, true })
        {
            var code = Render(operation, concept: concept);
            code.ShouldContain($"return value{(concept ? ".Value" : string.Empty)}.CompareTo(operand) {symbol} 0;");
            code.ShouldContain("ValidatedInstant(validation.RootContextData)");
            code.ShouldContain("operand.UtcDateTime.ToString(\"O\"");
            code.ShouldContain("{ClockOperand}");
            code.ShouldNotContain("UtcNow");
            code.ShouldNotContain("TimeProvider");
        }
    }

    [Theory]
    [InlineData("now + 1 minute", ".AddMinutes(1)")]
    [InlineData("now - 1 hour", ".AddHours(-1)")]
    [InlineData("now + 1 day", ".AddHours(24d * 1)")]
    public void should_render_existing_elapsed_offsets(string clock, string expected) =>
        Render(">", clock: clock).ShouldContain(expected);

    [Fact]
    public void should_preserve_authored_messages_and_severity()
    {
        var code = Render(">", suffix: "severity warning message \"Choose a future instant.\"");
        code.ShouldContain(".WithMessage(\"Choose a future instant.\")");
        code.ShouldContain("ValidationResultSeverity.Warning");
    }

    static string Render(string operation, bool concept = false, string clock = "now", string suffix = "")
    {
        var model = invoice_model.Compile($$"""
            concept Moment : DateTime
            module Scheduling
              feature Windows
                slice StateChange Keep
                  command Keep
                    id String identifier
                    at {{(concept ? "Moment" : "DateTime")}}
                    validate
                      at {{operation}} {{clock}} {{suffix}}
                    produces Kept
                      for id
                  event Kept
            """);
        var options = new CratisRenderingOptions("InvoiceApp", "Invoices");
        var request = new ArtifactRenderRequest(model, SemanticExecutionPlan.Compile(model).Plan, CratisRendering.CreateProfile("InvoiceApp", options), new(ArtifactRenderScopeKind.Application, model.Application.Id));
        var context = new SemanticApplicationContext(request, options);
        var command = context.Commands.Values.Single();
        var builder = new CSharpCodeBuilder();
        SemanticCommandClockValidationRendering.Render(builder, command.Validations.Single(), command.Properties.Single(property => property.Name == "at"), context);
        return builder.ToString();
    }
}
