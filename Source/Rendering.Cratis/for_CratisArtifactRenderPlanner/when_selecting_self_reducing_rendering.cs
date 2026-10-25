// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_selecting_self_reducing_rendering
{
    const string OriginalBody = "return new Total(Guid.Parse(\"00000000-0000-0000-0000-000000000001\"), context.Event.Amount);";
    const string Body = "return new Total(context.Event.Id, context.Event.Amount);";
    static string Source => when_rendering_a_pure_reducer.Source.Replace(OriginalBody, Body, StringComparison.Ordinal);

    [Fact]
    public async Task should_persist_reload_and_render_the_same_convention()
    {
        var profile = CratisRendering.CreateNewApplicationProfile("Projects", new("Projects", "Projects"));
        var scaffold = CratisRendering.PlanScaffold(new("Projects", "Projects", "Projects") { Profile = profile });
        Assert.True(scaffold.Success, string.Join(Environment.NewLine, scaffold.Diagnostics));
        var config = Assert.Single(scaffold.Artifacts, artifact => artifact.RelativePath == ReducerConventionInput.RelativePath);
        var reloaded = CratisRendering.WithReducerConvention(CratisRendering.CreateProfile("Projects", new("Projects", "Projects")), ReducerConventionInput.Read("Projects", Text(config)));
        Assert.Equal(profile.Inputs.Select(input => (input.Name, input.Version, input.Sha256)), reloaded.Inputs.Select(input => (input.Name, input.Version, input.Sha256)));
        var loaded = await when_rendering_a_pure_reducer.Load(Source);
        Assert.Equal(Plan(loaded, profile).Digest, Plan(loaded, reloaded).Digest);
    }

    [Theory]
    [InlineData("StateView")]
    [InlineData("Automation")]
    public async Task should_combine_reducer_members_sources_and_queries_in_the_slice(string kind)
    {
        var loaded = await when_rendering_a_pure_reducer.Load(Source.Replace("slice StateView Totals", $"slice {kind} Totals", StringComparison.Ordinal));
        var classic = when_rendering_a_pure_reducer.Plan(loaded);
        var self = when_rendering_a_pure_reducer.Plan(loaded, self: true);
        Assert.True(self.Success, string.Join(Environment.NewLine, self.Diagnostics));
        Assert.DoesNotContain(self.Artifacts, artifact => artifact.RelativePath.EndsWith("Fold.cs", StringComparison.Ordinal));
        var combined = self.Artifacts.Single(artifact => artifact.RelativePath.EndsWith("Totals.cs", StringComparison.Ordinal));
        var previous = classic.Artifacts.Where(artifact => artifact.RelativePath.EndsWith("Totals.cs", StringComparison.Ordinal) || artifact.RelativePath.EndsWith("Fold.cs", StringComparison.Ordinal));
        Assert.Equal(previous.SelectMany(artifact => artifact.Sources).Select(id => id.ToString()).Distinct().Order(StringComparer.Ordinal), combined.Sources.Select(id => id.ToString()).Distinct().Order(StringComparer.Ordinal));
        Assert.Contains("public static Total? Reduce(", Text(combined), StringComparison.Ordinal);
        var root = await CSharpSyntaxTree.ParseText(Text(combined)).GetRootAsync();
        var record = root.DescendantNodes().OfType<RecordDeclarationSyntax>().Single();
        Assert.DoesNotContain(record.Members.OfType<MethodDeclarationSyntax>(), method => method.Identifier.Text.StartsWith("Transition_", StringComparison.Ordinal));
        var helper = Assert.Single(record.Members.OfType<ClassDeclarationSyntax>());
        Assert.StartsWith("Transition_", helper.Identifier.Text, StringComparison.Ordinal);
        Assert.Contains($"var result = {helper.Identifier.Text}.Apply(", Text(combined), StringComparison.Ordinal);
        Assert.Equal("Apply", Assert.Single(helper.Members.OfType<MethodDeclarationSyntax>()).Identifier.Text);
        Assert.Contains(" ById(", Text(combined), StringComparison.Ordinal);
        Assert.DoesNotContain("IReducerFor", Text(combined), StringComparison.Ordinal);
        Assert.Empty(RenderedOutput.Errors(Files(self)));
    }

    [Fact]
    public async Task should_preserve_classic_bytes_and_ignore_an_unused_convention_in_scoped_digests()
    {
        var loaded = await when_rendering_a_pure_reducer.Load(Source);
        var original = CratisRendering.CreateProfile("Projects", new("Projects", "Projects"));
        var explicitClassic = CratisRendering.WithReducerConvention(original, ReducerConventionInput.Create("Projects", "classic"));
        var classic = Plan(loaded, original);
        var resolved = Plan(loaded, explicitClassic);
        Assert.True(resolved.Success, string.Join(Environment.NewLine, resolved.Diagnostics));
        Assert.Equal(classic.Artifacts.Select(artifact => (artifact.RelativePath, artifact.Sha256)), resolved.Artifacts.Where(artifact => artifact.RelativePath != ReducerConventionInput.RelativePath).Select(artifact => (artifact.RelativePath, artifact.Sha256)));
        var selection = new PlanSelection([PlanSelectionEntry.Slice("Orders", "Ordering", "PlaceOrder")]);
        var before = CratisRendering.PlanFrom(loaded, selection, new("Projects", "Projects", "Projects") { Profile = original });
        var after = CratisRendering.PlanFrom(loaded, selection, new("Projects", "Projects", "Projects") { Profile = CratisRendering.CreateNewApplicationProfile("Projects", new("Projects", "Projects")) });
        Assert.True(before.Success && after.Success);
        Assert.Equal(before.Digest, after.Digest);
    }

    [Theory]
    [InlineData("Reduce")]
    [InlineData("BeginReplay")]
    [InlineData("EndReplay")]
    [InlineData("BeginReplayPartition")]
    [InlineData("EndReplayPartition")]
    public async Task should_refuse_reserved_queries_only_in_self_mode(string name)
    {
        var loaded = await when_rendering_a_pure_reducer.Load(Source.Replace("query ById", $"query {name}", StringComparison.Ordinal));
        Assert.True(when_rendering_a_pure_reducer.Plan(loaded).Success);
        var self = when_rendering_a_pure_reducer.Plan(loaded, self: true);
        given.rendering_refusals.Rejected(self, "STAGE-ESM-019");
        Assert.Contains(self.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-019" && diagnostic.Message.Contains("reserved member", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("property")]
    [InlineData("type")]
    public async Task should_refuse_reduce_properties_and_model_type_names_only_in_self_mode(string collision)
    {
        var original = Source.Replace(Body, "return context.State;", StringComparison.Ordinal);
        var source = collision == "property"
            ? original.Replace("      readmodel Total\n        id Uuid\n        amount Decimal", "      readmodel Total\n        id Uuid\n        reduce Decimal", StringComparison.Ordinal)
            : original.Replace("Total", "Reduce", StringComparison.Ordinal);
        var loaded = await when_rendering_a_pure_reducer.Load(source);
        var classic = when_rendering_a_pure_reducer.Plan(loaded);
        Assert.True(classic.Success, string.Join(Environment.NewLine, classic.Diagnostics));
        var self = when_rendering_a_pure_reducer.Plan(loaded, self: true);
        given.rendering_refusals.Rejected(self, "STAGE-ESM-019");
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"version\":2,\"application\":\"Projects\",\"reducerConvention\":\"self\"}")]
    [InlineData("{\"version\":1,\"application\":\"Other\",\"reducerConvention\":\"self\"}")]
    [InlineData("{\"version\":1,\"application\":\"Projects\",\"reducerConvention\":\"unknown\"}")]
    [InlineData("{\"version\":1,\"application\":\"Projects\",\"reducerConvention\":\"classic\",\"reducerConvention\":\"self\"}")]
    [InlineData("{\"version\":1,\"application\":\"Projects\",\"reducerConvention\":\"self\",\"overrides\":{}}")]
    public void should_refuse_malformed_stale_or_unsupported_configuration(string json)
    {
        Assert.Throws<InvalidArtifactRenderContract>(() => ReducerConventionInput.Read("Projects", json));
        var profile = CratisRendering.WithReducerConvention(CratisRendering.CreateProfile("Projects", new("Projects", "Projects")), ArtifactRenderInput.Create(ReducerConventionInput.Name, "1", [.. Encoding.UTF8.GetBytes(json)]));
        var plan = CratisRendering.PlanScaffold(new("Projects", "Projects", "Projects") { Profile = profile });
        Assert.False(plan.Success);
        Assert.Empty(plan.Artifacts);
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-CRATIS-001");
    }

    [Fact]
    public void should_refuse_unknown_input_versions()
    {
        var input = ReducerConventionInput.Create("Projects", "self");
        var profile = CratisRendering.WithReducerConvention(CratisRendering.CreateProfile("Projects", new("Projects", "Projects")), ArtifactRenderInput.Create(input.Name, "2", input.Bytes));
        var plan = CratisRendering.PlanScaffold(new("Projects", "Projects", "Projects") { Profile = profile });
        Assert.False(plan.Success);
        Assert.Empty(plan.Artifacts);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Sales/Retail")]
    public async Task should_keep_scoped_and_full_model_bytes_equal_with_domain_placement(string domain)
    {
        var loaded = await when_rendering_a_pure_reducer.Load(Source);
        var profile = CratisRendering.CreateNewApplicationProfile("Projects", new("Projects", "Projects"));
        var full = Plan(loaded, DomainPlacementInput.WithDomain(profile, domain));
        Assert.True(full.Success, string.Join(Environment.NewLine, full.Diagnostics));
        var scoped = CratisRendering.PlanFrom(loaded, new([PlanSelectionEntry.Slice("Orders", "Ordering", "Totals")]), new("Projects", "Projects", "Projects") { Domain = domain, Profile = profile });
        Assert.True(scoped.Success, string.Join(Environment.NewLine, scoped.Diagnostics));
        var model = full.Artifacts.Single(artifact => artifact.RelativePath.EndsWith("Totals.cs", StringComparison.Ordinal));
        Assert.Equal(model.Sha256, scoped.Artifacts.Single(artifact => artifact.RelativePath == model.RelativePath).Sha256);
    }

    internal static ArtifactRenderPlan Plan(LoadedSemanticModel loaded, ArtifactRenderProfile profile) => new CratisArtifactRenderPlanner().Plan(new(loaded.Model, loaded.Plan, profile, new(ArtifactRenderScopeKind.Application, loaded.Model.Application.Id))
    {
        ImplementationRequirements = loaded.ImplementationRequirements,
        ImplementationContents = loaded.ImplementationContents,
        TypedContextDescriptors = loaded.TypedContextDescriptors
    });

    internal static RenderedFile[] Files(ArtifactRenderPlan plan) => [.. plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && artifact.RelativePath != "Program.cs").Select(artifact => new RenderedFile(artifact.RelativePath, Text(artifact)))];
    static string Text(PlannedArtifact artifact) => Encoding.UTF8.GetString(artifact.Bytes.AsSpan());
}
#endif
