// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_body_carrying_command_handlers;

public class with_an_attachment_boundary
{
    [Fact]
    public void should_reject_a_stale_body_hash()
    {
        var request = handler_model.Request();
        request = request with { ImplementationContents = ImmutableDictionary<string, string>.Empty.Add(request.ImplementationRequirements.Single().RequirementId, "return 42;") };

        // ed0b46d7 separates body integrity failures from unresolved attachment envelopes.
        AssertBlocked(request, "STAGE-ESM-056");
    }

    [Theory]
    [InlineData(ImplementationGapMode.Refuse)]
    [InlineData(ImplementationGapMode.Stub)]
    public void should_require_the_compilation_descriptor(ImplementationGapMode mode)
    {
        var request = handler_model.Request() with { TypedContextDescriptors = [] };
        var plan = CratisRendering.Plan(request.Model, request.ExecutionPlan, request.Scope, new("HandlerApp", "Orders") { ImplementationGaps = mode }, request.ImplementationRequirements, request.ImplementationContents, [], request.TypedContextDescriptors);
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-021" && diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error);
        Assert.Equal(ArtifactRenderOutcome.Refused, plan.Outcome);
        Assert.Empty(plan.Artifacts);
        Assert.Empty(plan.ImplementationGaps);
    }

    [Fact]
    public void should_reject_a_descriptor_that_omits_a_command_input()
    {
        var request = handler_model.Request();
        var descriptor = request.TypedContextDescriptors.Single();
        var member = descriptor.Members.Single(member => member.Name == "Command");
        request = request with { TypedContextDescriptors = [descriptor with { Members = [member with { Type = member.Type with { Properties = [.. member.Type.Properties.Skip(1)] } }] }] };
        AssertBlocked(request, "STAGE-ESM-021");
    }

    [Theory]
    [InlineData("typescript", null)]
    [InlineData("csharp", "handler.ts")]
    public void should_keep_foreign_and_inconsistent_language_envelopes_refused(string language, string? file)
    {
        var request = handler_model.Request();
        request = request with { ImplementationRequirements = [request.ImplementationRequirements.Single() with { Language = language, File = file }] };
        AssertBlocked(request, "STAGE-IMPL-002");
        Assert.Contains(handler_model.Plan(request).Diagnostics, diagnostic => diagnostic.Code == "STAGE-IMPL-002" &&
            diagnostic.Message.Contains(language, StringComparison.Ordinal) && diagnostic.Message.Contains("only C# handler bodies", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(null, "handler.cs")]
    [InlineData("csharp", "handler.cs")]
    [InlineData("csharp", null)]
    public void should_admit_equivalent_csharp_envelopes(string? language, string? file)
    {
        var request = handler_model.Request();
        request = request with { ImplementationRequirements = [request.ImplementationRequirements.Single() with { Language = language, File = file }] };
        var plan = handler_model.Plan(request);
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
    }

    [Fact]
    public void should_keep_pending_handlers_refused()
    {
        var request = handler_model.Request();
        request = request with
        {
            ImplementationContents = [],
            ImplementationRequirements = [request.ImplementationRequirements.Single() with { AttachmentResolution = SemanticAttachmentResolution.Pending, ContentHash = string.Empty, Language = null }]
        };
        AssertBlocked(request, "STAGE-IMPL-002");
    }

    [Fact]
    public void should_render_a_positional_tuple_in_declared_event_order()
    {
        var plan = handler_model.Plan(handler_model.Request(handler_model.MultiBody, handler_model.MultiSource));
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
        var source = Source(plan);
        Assert.Contains(".Item1", source, StringComparison.Ordinal);
        Assert.Contains(".Item2", source, StringComparison.Ordinal);
        Assert.Contains(".Token", source, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("return (new OrderEchoed(context.Command.Name), new OrderPlaced(context.Command.Name));")]
    [InlineData("return new OrderPlaced(context.Command.Name);")]
    [InlineData("return (new OrderPlaced(context.Command.Name), new OrderEchoed(context.Command.Name), new OrderEchoed(context.Command.Name));")]
    public void should_reject_wrong_tuple_order_and_arity(string body) => AssertBlocked(handler_model.Request(body, handler_model.MultiSource), "STAGE-ESM-019");

    [Fact]
    public void should_return_a_bare_handler_result_without_a_pass_through_local()
    {
        var plan = handler_model.Plan(handler_model.Request());
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
        var source = Source(plan);
        Assert.DoesNotContain("handlerPayloads", source, StringComparison.Ordinal);
        Assert.Contains("return HandlePlaceOrder(", source, StringComparison.Ordinal);
    }

    [Fact]
    public void should_preserve_raw_literals_and_comments_and_normalize_crlf_outside_literals()
    {
        const string body = "// preserve this trivia\r\nvar text = \"\"\"\n    spaced\n      value\n    \"\"\";\r\nreturn new OrderPlaced(text);";
        var request = handler_model.Request(body);
        var plan = handler_model.Plan(request);
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
        var context = new SemanticApplicationContext(request, new("HandlerApp", "Orders"));
        var builder = new CSharpCodeBuilder();
        SemanticCommandHandlerRendering.Render(builder, context.Commands.Values.Single(), context);

        // Re-frozen for Cratis/Stage#848 and #846: re-indent the body while retaining LF inside literals and CRLF outside them.
        var rendered = builder.ToString();
        var indent = LeadingSpaces(rendered, "// preserve this trivia\r\n");
        Assert.NotEmpty(indent);
        Assert.Equal(0, indent.Length % 4);
        Assert.Contains($"{indent}// preserve this trivia\r\n{indent}var text = \"\"\"\n    spaced\n      value\n    \"\"\";\r\n{indent}return new OrderPlaced(text);", rendered, StringComparison.Ordinal);

        // PlannedArtifact.Text normalizes CRLF by contract; the authenticated splice does not.
        var planned = Source(plan);
        var plannedIndent = LeadingSpaces(planned, "// preserve this trivia\n");
        Assert.NotEmpty(plannedIndent);
        Assert.Contains($"{plannedIndent}// preserve this trivia\n{plannedIndent}var text = \"\"\"\n    spaced\n      value\n    \"\"\";\n{plannedIndent}return new OrderPlaced(text);", planned, StringComparison.Ordinal);
    }

    [Fact]
    public void should_refuse_crlf_inside_a_raw_literal()
    {
        const string body = "// preserve this trivia\r\nvar text = \"\"\"\r\n    spaced\r\n    \"\"\";\r\nreturn new OrderPlaced(text);";
        var plan = handler_model.Plan(handler_model.Request(body));
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-022" && diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error);
        Assert.Empty(plan.Artifacts);
    }

    [Fact]
    public void should_render_a_generated_specification_that_reaches_the_body_without_dropping_the_command()
    {
        const string specification = """

              specification Placing
                when PlaceOrder
                  orderId = "11111111-1111-1111-1111-111111111111"
                  name = "alice"
                then OrderPlaced
                  name = "ALICE"
        """;

        var request = handler_model.Request(source: handler_model.Source + specification);
        var slice = request.Model.Application.Modules.Single().Features.Single().Slices.Single();
        var specificationModel = slice.Specifications.Single();
        var command = slice.Commands.Single();
        var plan = handler_model.Plan(request);

        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
        Assert.Equal(ArtifactRenderOutcome.Complete, plan.Outcome);
        Assert.DoesNotContain(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-011");
        Assert.Empty(plan.Refusals);
        Assert.Contains(plan.Artifacts, artifact => artifact.Sources.Contains(specificationModel.Id));
        Assert.Contains(plan.Artifacts, artifact => artifact.Sources.Contains(command.Id));
        Assert.Contains("HandlePlaceOrder(", Source(plan), StringComparison.Ordinal);
    }

    static string Source(ArtifactRenderPlan plan) => string.Join('\n', plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal)).Select(artifact => Encoding.UTF8.GetString(artifact.Bytes.AsSpan())));

    static void AssertBlocked(ArtifactRenderRequest request, string code)
    {
        var plan = handler_model.Plan(request);
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == code && diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error);
        Assert.Empty(plan.Artifacts);
    }

    static string LeadingSpaces(string text, string marker)
    {
        var at = text.IndexOf(marker, StringComparison.Ordinal);
        var start = at;
        while (start > 0 && text[start - 1] == ' ') start--;
        return text[start..at];
    }
}
