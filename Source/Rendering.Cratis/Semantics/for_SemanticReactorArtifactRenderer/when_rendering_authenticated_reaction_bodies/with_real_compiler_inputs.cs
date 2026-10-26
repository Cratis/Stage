// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticReactorArtifactRenderer.when_rendering_authenticated_reaction_bodies;

public class with_real_compiler_inputs
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_compile_the_fully_spliced_single_property_handler(bool file)
    {
        var loaded = reaction_body_model.Load(file: file);
        var requirement = Assert.Single(loaded.ImplementationRequirements);
        Assert.Equal(SemanticImplementationRole.ReactionEffect, requirement.Role);
        var plan = opaque_policy_model.Plan(loaded);
        Assert.True(plan.Success, opaque_policy_model.Errors(plan));
        var source = Encoding.UTF8.GetString(Assert.Single(plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith("Notifier.cs", StringComparison.Ordinal))).Bytes.AsSpan());
        Assert.Contains("IEnumerable<object>? TicketOpened(", source, StringComparison.Ordinal);
        Assert.Contains("var Title = @event.Title;", source, StringComparison.Ordinal);
        Assert.Contains(reaction_body_model.Body, source, StringComparison.Ordinal);
        Assert.DoesNotContain("var effects =", source, StringComparison.Ordinal);
        Assert.Empty(RenderedOutput.Errors(opaque_policy_model.Files(plan)));
    }

    [Fact]
    public void should_compile_multiple_independent_property_bindings()
    {
        var loaded = reaction_body_model.Load("return Count > 0 ? new object[] { Title, @event.Count, context } : null;", extraProperty: "        count Int");
        var plan = opaque_policy_model.Plan(loaded);
        Assert.True(plan.Success, opaque_policy_model.Errors(plan));
        Assert.Empty(RenderedOutput.Errors(opaque_policy_model.Files(plan)));
    }

    [Fact]
    public void should_leave_reference_execution_unsupported_after_successful_rendering()
    {
        Assert.True(opaque_policy_model.Plan(reaction_body_model.Load()).Success);
        var loaded = reaction_body_model.LoadSource(reaction_body_model.Source(reaction_body_model.Body) + "\n" + """
                  specification Notifying
                    when append TicketOpened
                      title = "Broken"
                    then no events
            """);
        var specification = loaded.Model.Application.Modules.Single().Features.Single().Slices.Single().Specifications.Single();
        Assert.IsType<SemanticUnsupported>(new SemanticSpecificationRunner().Run(loaded.Plan, specification.Id).Execution);
    }

    [Theory]
    [InlineData("}")]
    [InlineData("#if NEVER\n}\n#endif\nreturn null;")]
    [InlineData("return @\"unterminated")]
    [InlineData("var Title = \"collision\"; return null;")]
    public void should_refuse_hostile_or_colliding_bodies(string body)
    {
        var plan = opaque_policy_model.Plan(reaction_body_model.Load(body));
        Assert.False(plan.Success);
        Assert.Empty(plan.Artifacts);
    }

    [Fact]
    public void should_compile_reflection_without_executing_native_code()
    {
        var plan = opaque_policy_model.Plan(reaction_body_model.Load("return typeof(object).Assembly.GetTypes();"));
        Assert.True(plan.Success, opaque_policy_model.Errors(plan));
        Assert.Empty(RenderedOutput.Errors(opaque_policy_model.Files(plan)));
    }

    [Fact]
    public void should_preserve_the_same_authenticated_text_for_inline_and_file_attachments()
    {
        var inline = reaction_body_model.Load();
        var file = reaction_body_model.Load(file: true);
        Assert.Equal(Assert.Single(inline.ImplementationContents).Value, Assert.Single(file.ImplementationContents).Value);
    }

    [Fact]
    public void should_refuse_a_missing_requirement()
    {
        var loaded = reaction_body_model.Load();
        var plan = opaque_policy_model.Plan(loaded with { ImplementationRequirements = [] });
        Assert.False(plan.Success);
        Assert.Empty(plan.Artifacts);
    }

    [Fact]
    public void should_refuse_a_missing_implementation_body()
    {
        var loaded = reaction_body_model.Load();
        var plan = opaque_policy_model.Plan(loaded with { ImplementationContents = loaded.ImplementationContents.Clear() });
        Assert.False(plan.Success);
        Assert.Empty(plan.Artifacts);
    }

    [Theory]
    [InlineData("role")]
    [InlineData("capability")]
    [InlineData("language")]
    [InlineData("owner")]
    [InlineData("hash")]
    public void should_refuse_an_invalid_envelope(string part)
    {
        var loaded = reaction_body_model.Load();
        var requirement = Assert.Single(loaded.ImplementationRequirements);
        requirement = part switch
        {
            "role" => requirement with { Role = SemanticImplementationRole.ValueGeneration },
            "capability" => requirement with { RequiredCapability = "other" },
            "language" => requirement with { Language = "typescript" },
            "owner" => requirement with { Source = requirement.Source with { SemanticId = loaded.Model.Application.Id } },
            _ => requirement with { ContentHash = new string('0', 64) }
        };
        var plan = opaque_policy_model.Plan(loaded with { ImplementationRequirements = [requirement] });
        Assert.False(plan.Success);
        Assert.Empty(plan.Artifacts);
    }
}
#endif
