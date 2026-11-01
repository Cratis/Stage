// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_body_carrying_command_handlers;

/// <summary>
/// The Screenplay compiler passes its own command-handler descriptor: the Command snapshot plus the fixed ambient
/// members. Stage admits exactly that closed shape; anything unknown or misshaped keeps STAGE-ESM-021.
/// </summary>
public class with_the_compiler_descriptor
{
    static readonly string[] _ambient = ["Tenant", "Identity", "CausedBy", "Causation", "Occurred"];

    [Fact]
    public void should_pass_the_compiler_descriptor_with_the_command_and_every_ambient_member()
    {
        var descriptor = handler_model.Request().TypedContextDescriptors.Single();
        Assert.Equal(_ambient.Append("Command").Order(StringComparer.Ordinal), descriptor.Members.Select(member => member.Name).Order(StringComparer.Ordinal));
    }

    [Fact]
    public void should_admit_the_compiler_descriptor()
    {
        var plan = handler_model.Plan(handler_model.Request());
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
        Assert.DoesNotContain(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-021");
    }

    [Fact]
    public void should_admit_a_command_only_descriptor()
    {
        var plan = handler_model.Plan(Rewrite(members => [.. members.Where(member => member.Name == "Command")]));
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
    }

    [Fact]
    public void should_admit_a_known_subset_of_ambient_members()
    {
        var plan = handler_model.Plan(Rewrite(members => [.. members.Where(member => member.Name == "Command" || member.Name == "Tenant")]));
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
    }

    [Theory]
    [InlineData("return new OrderPlaced(context.Identity.Id);")]
    [InlineData("return new OrderPlaced(context.Tenant.ToString());")]
    [InlineData("return new OrderPlaced(context.CausedBy.ToString());")]
    [InlineData("return new OrderPlaced(context.Causation.ToString());")]
    [InlineData("return new OrderPlaced(context.Occurred.ToString());")]
    public void should_refuse_ambient_access_on_the_compiler_descriptor(string body)
    {
        var plan = handler_model.Plan(handler_model.Request(body));
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-015" && diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error);
        Assert.Empty(plan.Artifacts);
    }

    [Fact]
    public void should_refuse_an_unknown_extra_member() =>
        AssertRefused(members => [.. members, Ambient("Extra", members.First(member => member.Name == "Tenant"))]);

    [Theory]
    [InlineData("Tenant")]
    [InlineData("Identity")]
    [InlineData("CausedBy")]
    [InlineData("Causation")]
    [InlineData("Occurred")]
    public void should_refuse_an_ambient_member_with_the_wrong_runtime_token(string name) =>
        AssertRefused(members => [.. members.Select(member => member.Name == name ? member with { Type = member.Type with { RuntimeToken = SemanticContextRuntimeTokens.Text } } : member)]);

    [Fact]
    public void should_refuse_a_derived_ambient_member() =>
        AssertRefused(members => [.. members.Select(member => member.Name == "Identity" ? member with { IsDerived = true } : member)]);

    [Fact]
    public void should_refuse_a_nullable_ambient_member() =>
        AssertRefused(members => [.. members.Select(member => member.Name == "Tenant" ? member with { IsNullable = true } : member)]);

    [Fact]
    public void should_refuse_an_ambient_member_with_a_foreign_source() =>
        AssertRefused(members => [.. members.Select(member => member.Name == "Causation" ? member with { Source = new(SemanticContextSourceKinds.Unavailable, null, "Causation") } : member)]);

    [Fact]
    public void should_refuse_an_ambient_runtime_type_with_shape_properties() =>
        AssertRefused(members => [.. members.Select(member => member.Name == "Tenant"
            ? member with { Type = member.Type with { Properties = members.Single(candidate => candidate.Name == "Command").Type.Properties } }
            : member)]);

    [Fact]
    public void should_refuse_a_null_ambient_name_without_throwing() =>
        AssertRefused(members => [.. members.Select(member => member.Name == "Tenant" ? member with { Name = null! } : member)]);

    [Fact]
    public void should_refuse_a_duplicate_ambient_member() =>
        AssertRefused(members => [.. members, members.First(member => member.Name == "Tenant")]);

    [Fact]
    public void should_refuse_a_missing_command_member() =>
        AssertRefused(members => [.. members.Where(member => member.Name != "Command")]);

    [Fact]
    public void should_refuse_a_duplicate_command_member() =>
        AssertRefused(members => [.. members, members.First(member => member.Name == "Command")]);

    static SemanticTypedContextMember Ambient(string name, SemanticTypedContextMember template) =>
        template with { Name = name, Source = new(template.Source.Kind, template.Source.SemanticId, name) };

    static ArtifactRenderRequest Rewrite(Func<ImmutableArray<SemanticTypedContextMember>, ImmutableArray<SemanticTypedContextMember>> change)
    {
        var request = handler_model.Request();
        return request with { TypedContextDescriptors = [.. request.TypedContextDescriptors.Select(descriptor => descriptor with { Members = change(descriptor.Members) })] };
    }

    static void AssertRefused(Func<ImmutableArray<SemanticTypedContextMember>, ImmutableArray<SemanticTypedContextMember>> change)
    {
        var plan = handler_model.Plan(Rewrite(change));
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-021" && diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error);
        Assert.Empty(plan.Artifacts);
    }
}
