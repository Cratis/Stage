// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ReducerConventionInput;

public class when_reading_version_five : Specification
{
    const string First = "sem1:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    const string Plain = /* lang=json */ "{\"version\":5,\"application\":\"Projects\",\"reducerConvention\":\"self\",\"projectionConvention\":\"model-bound\"}";

    [Fact]
    void should_canonicalize_and_select_all_flat_capabilities()
    {
        var input = ReducerConventionInput.Read("Projects", /* lang=json */ "{ \"projectionConvention\": \"model-bound\", \"reducerConvention\": \"self\", \"application\": \"Projects\", \"version\": 5 }");
        Assert.Equal("5", input.Version);
        Assert.Equal(Plain + "\n", Text(input));
        var profile = CratisRendering.WithReducerConvention(CratisRendering.CreateProfile("Projects", new("Projects", "Projects")), input);
        Assert.True(ReducerConventionInput.TryRead(profile, "Projects", out var convention));
        Assert.True(convention.ModelBoundProjections && convention.ModelBoundOperations && convention.ModelBoundFlatScopes);
        Assert.Equal(Text(input), Text(ReducerConventionInput.Create("Projects", convention)));
    }

    [Fact]
    void should_round_trip_overrides_without_changing_their_identity()
    {
        var convention = ReducerConvention.Classic with
        {
            SelfDefault = true,
            ModelBoundProjections = true,
            ModelBoundOperations = true,
            ModelBoundFlatScopes = true,
            Overrides = ImmutableDictionary<SemanticId, ReducerOverride>.Empty.Add(SemanticId.Parse(First), new("Projects.A.Fold", "A/Fold.cs"))
        };
        var input = ReducerConventionInput.Create("Projects", convention);
        Assert.Equal(Plain[..^1] + $",\"readModels\":[{{\"id\":\"{First}\",\"convention\":\"self\",\"observerId\":\"Projects.A.Fold\",\"previousPath\":\"A/Fold.cs\"}}]}}\n", Text(input));
        Assert.Equal(Text(input), Text(ReducerConventionInput.Read("Projects", Text(input))));
    }

    [Fact]
    void should_select_version_five_only_for_new_applications()
    {
        var profile = CratisRendering.CreateNewApplicationProfile("Projects", new("Projects", "Projects"));
        Assert.Equal("5", profile.Inputs.Single(input => input.Name == ReducerConventionInput.Name).Version);
        Assert.True(ReducerConventionInput.TryRead(profile, "Projects", out var created));
        Assert.True(created.ModelBoundFlatScopes);
        Assert.Empty(CratisRendering.CreateProfile("Projects", new("Projects", "Projects")).Inputs.Where(input => input.Name == ReducerConventionInput.Name));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    void should_leave_retained_versions_on_their_original_capability_path(int version)
    {
        var overrides = version == 2 ? $",\"readModels\":[{{\"id\":\"{First}\",\"convention\":\"self\",\"observerId\":\"Projects.A.Fold\",\"previousPath\":\"A/Fold.cs\"}}]" : string.Empty;
        var projection = version >= 3 ? ",\"projectionConvention\":\"model-bound\"" : string.Empty;
        Assert.True(ReducerConventionInput.TryParse($"{{\"version\":{version},\"application\":\"Projects\",\"reducerConvention\":\"self\"{projection}{overrides}}}", "Projects", out var convention));
        Assert.False(convention.ModelBoundFlatScopes);
    }

    [Theory]
    [InlineData("\"projectionConvention\":\"fluent\"", "Projects")]
    [InlineData("\"projectionConvention\":\"model-bound\",\"extra\":true", "Projects")]
    [InlineData("\"projectionConvention\":\"model-bound\",\"readModels\":[]", "Projects")]
    [InlineData("\"projectionConvention\":\"model-bound\"", "Other")]
    void should_refuse_malformed_or_mismatched_version_five(string extra, string application) =>
        Assert.Throws<InvalidArtifactRenderContract>(() => ReducerConventionInput.Read(application, $"{{\"version\":5,\"application\":\"Projects\",\"reducerConvention\":\"self\",{extra}}}"));

    static string Text(ArtifactRenderInput input) => Encoding.UTF8.GetString(input.Bytes.AsSpan());
}
#endif
