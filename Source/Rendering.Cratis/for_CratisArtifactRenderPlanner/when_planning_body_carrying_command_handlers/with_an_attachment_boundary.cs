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
        AssertBlocked(request, "STAGE-ESM-020");
    }

    [Fact]
    public void should_require_the_compilation_descriptor() => AssertBlocked(handler_model.Request() with { TypedContextDescriptors = [] }, "STAGE-ESM-021");

    [Fact]
    public void should_reject_a_descriptor_that_omits_a_command_input()
    {
        var request = handler_model.Request();
        var descriptor = request.TypedContextDescriptors.Single();
        var member = descriptor.Members.Single();
        request = request with { TypedContextDescriptors = [descriptor with { Members = [member with { Type = member.Type with { Properties = [.. member.Type.Properties.Skip(1)] } }] }] };
        AssertBlocked(request, "STAGE-ESM-021");
    }

    [Theory]
    [InlineData("typescript", null)]
    [InlineData("csharp", "handler.ts")]
    [InlineData("csharp", "handler.cs")]
    public void should_keep_foreign_and_inconsistent_language_envelopes_refused(string language, string? file)
    {
        var request = handler_model.Request();
        request = request with { ImplementationRequirements = [request.ImplementationRequirements.Single() with { Language = language, File = file }] };
        AssertBlocked(request, "STAGE-IMPL-002");
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
    public void should_preserve_raw_literals_comments_and_crlf()
    {
        const string body = "// preserve this trivia\r\nvar text = \"\"\"\r\n    spaced\r\n      value\r\n    \"\"\";\r\nreturn new OrderPlaced(text);";
        var request = handler_model.Request(body);
        var plan = handler_model.Plan(request);
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
        var context = new SemanticApplicationContext(request, new("HandlerApp", "Orders"));
        var builder = new CSharpCodeBuilder();
        SemanticCommandHandlerRendering.Render(builder, context.Commands.Values.Single(), context);
        Assert.Contains(body, builder.ToString(), StringComparison.Ordinal);

        // PlannedArtifact.Text normalizes CRLF by contract; the authenticated splice does not.
        Assert.Contains(body.Replace("\r\n", "\n", StringComparison.Ordinal), Source(plan), StringComparison.Ordinal);
    }

    [Fact]
    public void should_refuse_a_generated_specification_that_reaches_the_body()
    {
        const string specification = """

              specification Placing
                when PlaceOrder
                  orderId = "11111111-1111-1111-1111-111111111111"
                  name = "alice"
                then OrderPlaced
                  name = "ALICE"
        """;
        AssertBlocked(handler_model.Request(source: handler_model.Source + specification), "STAGE-ESM-011");
    }

    static string Source(ArtifactRenderPlan plan) => string.Join('\n', plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal)).Select(artifact => Encoding.UTF8.GetString(artifact.Bytes.AsSpan())));

    static void AssertBlocked(ArtifactRenderRequest request, string code)
    {
        var plan = handler_model.Plan(request);
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == code && diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error);
        Assert.Empty(plan.Artifacts);
    }
}
