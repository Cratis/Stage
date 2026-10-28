// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticReactorArtifactRenderer.when_rendering_authenticated_reaction_bodies;

public class with_unsupported_shapes
{
    [Theory]
    [InlineData("if (@event is { Title: var Title }) return null; return null;")]
    [InlineData("foreach (var Title in new string[0]) { } return null;")]
    [InlineData("try { } catch (System.Exception Title) { } return null;")]
    [InlineData("System.Func<string, string> f = Title => Title; return null;")]
    [InlineData("void F(string Title) { } return null;")]
    [InlineData("var (Title, other) = (\"a\", \"b\"); return null;")]
    [InlineData("object value = null; if (value is string Title) { } return null;")]
    [InlineData("var context = new object(); return null;")]
    [InlineData("var @event = new object(); return null;")]
    public void should_refuse_declarations_that_collide_with_generated_bindings(string body)
    {
        var plan = opaque_policy_model.Plan(reaction_body_model.Load(body));
        Assert.False(plan.Success);
        Assert.Empty(plan.Artifacts);
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Message.Contains("collides with a generated handler binding", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void should_refuse_unknown_context_or_result_versions(bool context)
    {
        var loaded = reaction_body_model.Load();
        var requirement = Assert.Single(loaded.ImplementationRequirements);
        requirement = context ? requirement with { ContextVersion = 2 } : requirement with { ResultVersion = 2 };
        var plan = opaque_policy_model.Plan(loaded with { ImplementationRequirements = [requirement] });
        Assert.False(plan.Success);
        Assert.Empty(plan.Artifacts);
    }

    [Fact]
    public void should_preserve_the_compiler_refusal_of_multiple_same_event_body_triggers()
    {
        var source = reaction_body_model.Source("return null;") + "\n" + """
                    when TicketOpened
                      ```csharp
                      return null;
                      ```
            """;
        var failure = Assert.Throws<InvalidSemanticModel>(() => reaction_body_model.LoadSource(source));
        Assert.Contains(failure.Diagnostics, diagnostic => diagnostic.Contains("already declares 'when TicketOpened'", StringComparison.Ordinal));
    }

    [Fact]
    public void should_refuse_body_and_declarative_production()
    {
        var source = reaction_body_model.Source("return null;", extraTriggerContent: "          produces Notified\n            title = title")
            .Replace("      reaction Notifier", "      event Notified\n        title String\n      reaction Notifier", StringComparison.Ordinal);
        Refused(source);
    }

    [Fact]
    public void should_refuse_body_and_command_invocation()
    {
        var source = reaction_body_model.Source("return null;", extraTriggerContent: "          invokes Notify\n            title = title")
            .Replace("      reaction Notifier", "      event Notified\n        title String\n      command Notify\n        title String\n        produces Notified\n          title = title\n      reaction Notifier", StringComparison.Ordinal);
        Refused(source);
    }

    [Fact]
    public void should_preserve_the_compiler_refusal_of_unprotected_reads()
    {
        var source = reaction_body_model.Source("return null;", extraTriggerContent: "          reads Decision")
            .Replace("      reaction Notifier", "      readmodel Decision\n        title String\n      reaction Notifier", StringComparison.Ordinal);
        var failure = Assert.Throws<InvalidSemanticModel>(() => reaction_body_model.LoadSource(source));
        Assert.Contains(failure.Diagnostics, diagnostic => diagnostic.Contains("cannot protect that decision dependency", StringComparison.Ordinal));
    }

    [Fact]
    public void should_refuse_a_system_identity_with_an_opaque_body()
    {
        var source = reaction_body_model.Source("return null;").Replace("      reaction Notifier", "      reaction Notifier\n        runs as system", StringComparison.Ordinal);
        Refused(source);
    }

    [Fact]
    public void should_compile_a_guarded_body_without_rewriting_it()
    {
        var source = reaction_body_model.Source(reaction_body_model.Body, extraProperty: "        count Int")
            .Replace("      reaction Notifier", "      reaction Notifier\n        where count > 0", StringComparison.Ordinal);
        var plan = opaque_policy_model.Plan(reaction_body_model.LoadSource(source));
        Assert.True(plan.Success, opaque_policy_model.Errors(plan));
        var handler = Encoding.UTF8.GetString(Assert.Single(plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith("Notifier.cs", StringComparison.Ordinal))).Bytes.AsSpan());
        Assert.Contains("if (!(", handler, StringComparison.Ordinal);
        Assert.Contains(")) return null;", handler, StringComparison.Ordinal);
        Assert.Contains(reaction_body_model.Body, handler, StringComparison.Ordinal);
        Assert.Empty(RenderedOutput.Errors(opaque_policy_model.Files(plan)));
    }

    static void Refused(string source)
    {
        var plan = opaque_policy_model.Plan(reaction_body_model.LoadSource(source));
        Assert.False(plan.Success);
        Assert.Empty(plan.Artifacts);
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-024");
    }
}
#endif
