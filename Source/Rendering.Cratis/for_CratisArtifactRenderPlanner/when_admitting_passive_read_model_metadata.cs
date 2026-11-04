// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using System.Text.Json.Nodes;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;
using static Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_passive_read_models;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_admitting_passive_read_model_metadata : Specification
{
    [Theory]
    [InlineData("malformed")]
    [InlineData("unknown")]
    [InlineData("duplicate-identity")]
    [InlineData("duplicate-input")]
    [InlineData("empty")]
    [InlineData("query-target")]
    [InlineData("wrong-version")]
    [InlineData("noncanonical")]
    public void should_fail_closed_without_artifacts(string fault)
    {
        var compilation = Compile(Source);
        var profile = CratisRendering.WithPassiveReadModels(Profile(compilation), compilation);
        var input = profile.Inputs.Single(candidate => candidate.Name == PassiveReadModelsInput.Name);
        var json = JsonNode.Parse(input.Bytes.AsSpan())!;
        var target = compilation.Model.Application.Modules.Single().Features.Single().Slices.Single(slice => slice.Name == "Listing").ReadModels.Single().Id;
        json = Mutate(json, fault, target.ToString());
        var bytes = fault == "malformed" ? "{"u8.ToArray() : Encoding.UTF8.GetBytes(json.ToJsonString(new() { WriteIndented = false }));
        if (fault == "noncanonical") bytes = [32, .. input.Bytes];
        var replacement = ArtifactRenderInput.Create(input.Name, fault == "wrong-version" ? "2" : input.Version, [.. bytes]);
        var inputs = profile.Inputs.Where(candidate => candidate.Name != input.Name).Append(replacement);
        if (fault == "duplicate-input") inputs = inputs.Append(ArtifactRenderInput.Create(input.Name, "2.0.0", input.Bytes));
        var plan = Plan(compilation, ReplaceInputs(profile, inputs));
        plan.Success.ShouldBeFalse();
        plan.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain("STAGE-CRATIS-001");
        plan.Artifacts.ShouldBeEmpty();
    }

    [Fact]
    void should_hash_the_ordinal_identity_set_not_declaration_names()
    {
        var source = Source.Replace("      readmodel ListingBalance\n", "      readmodel ListingBalance\n        passive\n", StringComparison.Ordinal)
            .Replace("      query AllBalances => ListingBalance[]", string.Empty, StringComparison.Ordinal);
        var compilation = Compile(source);
        compilation.PassiveReadModels.Count.ShouldEqual(2);
        var profile = CratisRendering.WithPassiveReadModels(Profile(compilation), compilation);
        var input = profile.Inputs.Single(candidate => candidate.Name == PassiveReadModelsInput.Name);
        input.Version.ShouldEqual("1");
        Assert.Equal(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(compilation.PassiveReadModels.Select(id => id.ToString()).Order(StringComparer.Ordinal).ToArray()), input.Bytes);
        Assert.True(PassiveReadModelsInput.TryRead(profile, compilation.Model, out var identities));
        Assert.True(identities.SetEquals(compilation.PassiveReadModels));
    }

    [Fact]
    void should_not_spread_passivity_to_the_same_name_in_another_module()
    {
        // Screenplay refuses duplicate declaration names in source. Exercise the portable model's
        // qualified identities directly, without asking the source binder to accept that ambiguity.
        var compilation = Compile(Source.Replace("    slice StateView Listing", "module Sales\n  feature Views\n    slice StateView Listing", StringComparison.Ordinal));
        var application = compilation.Model.Application with
        {
            Modules = [.. compilation.Model.Application.Modules.Select(module => module with
            {
                Features = [.. module.Features.Select(feature => feature with
                {
                    Slices = [.. feature.Slices.Select(slice => slice with
                    {
                        ReadModels = [.. slice.ReadModels.Select(readModel => readModel with { Name = "Balance" })]
                    })]
                })]
            })]
        };
        var model = global::Cratis.Screenplay.Semantics.ExecutableSemanticModel.Create(compilation.Model.LanguageVersion, compilation.Model.SemanticVersion, application);
        var profile = CratisRendering.WithPassiveReadModels(Profile(compilation), compilation);
        Assert.True(PassiveReadModelsInput.TryRead(profile, model, out var passive));
        Assert.True(passive.SetEquals(compilation.PassiveReadModels));
        passive.Count.ShouldEqual(1);
    }

    [Fact]
    void should_not_infer_passivity_from_missing_queries_in_portable_semantics()
    {
        var compilation = Compile(Source);
        var plan = Plan(compilation, Profile(compilation));
        Assert.True(plan.Success, string.Join("; ", plan.Diagnostics));
        Text(plan, "Adjust.cs").ShouldNotContain("Passive");
    }

    [Fact]
    async Task should_admit_passive_reducers_with_the_same_attribute()
    {
        var source = when_rendering_a_pure_reducer.Source.Replace("      readmodel Total\n        id Uuid", "      readmodel Total\n        passive\n        id Uuid key", StringComparison.Ordinal)
            .Replace("      query ById => Total?\n        by id Uuid\n", string.Empty, StringComparison.Ordinal);
        var plan = await PlanSources(source);
        Assert.True(plan.Success, string.Join("; ", plan.Diagnostics));
        Text(plan, "Totals.cs").ShouldContain("[Passive]");
        var baseline = await PlanSources(source.Replace("        passive\n", string.Empty, StringComparison.Ordinal)
            .Replace("      reducer Fold => Total", "      query ById => Total?\n        by id Uuid\n      reducer Fold => Total", StringComparison.Ordinal));
        Assert.Equal(Text(baseline, "Fold.cs"), Text(plan, "Fold.cs"));
    }

    static JsonNode Mutate(JsonNode json, string fault, string queryTarget)
    {
        switch (fault)
        {
            case "unknown": json[0] = "unknown"; break;
            case "duplicate-identity": json.AsArray().Add(json[0]!.GetValue<string>()); break;
            case "empty": return new JsonArray();
            case "query-target": json[0] = queryTarget; break;
        }
        return json;
    }

    static async Task<ArtifactRenderPlan> PlanSources(string source)
    {
        var loaded = await when_rendering_a_pure_reducer.Load(source);
        var result = CratisRendering.PlanFrom(loaded, new([PlanSelectionEntry.Module("Orders")]), new("Projects", "Projects", "Projects"));
        Assert.True(result.Success, string.Join("; ", result.Diagnostics));
        return result.Plan!;
    }
}
#endif
