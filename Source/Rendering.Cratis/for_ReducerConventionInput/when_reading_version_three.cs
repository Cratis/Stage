// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ReducerConventionInput;

public class when_reading_version_three
{
    const string First = "sem1:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    const string Plain = "{\"version\":3,\"application\":\"Projects\",\"reducerConvention\":\"self\",\"projectionConvention\":\"model-bound\"}";

    [Fact]
    public void should_write_canonical_bytes()
    {
        var input = ReducerConventionInput.Read("Projects", "{ \"projectionConvention\": \"model-bound\", \"reducerConvention\": \"self\", \"application\": \"Projects\", \"version\": 3 }");
        Assert.Equal("3", input.Version);
        Assert.Equal(Plain + "\n", Text(input));
    }

    [Fact]
    public void should_keep_overrides_after_the_projection_convention()
    {
        var convention = ReducerConvention.Classic with
        {
            SelfDefault = true,
            ModelBoundProjections = true,
            Overrides = ImmutableDictionary<SemanticId, ReducerOverride>.Empty.Add(SemanticId.Parse(First), new("Projects.A.Fold", "A/Fold.cs"))
        };
        var input = ReducerConventionInput.Create("Projects", convention);
        Assert.Equal(
            $"{{\"version\":3,\"application\":\"Projects\",\"reducerConvention\":\"self\",\"projectionConvention\":\"model-bound\",\"readModels\":[{{\"id\":\"{First}\",\"convention\":\"self\",\"observerId\":\"Projects.A.Fold\",\"previousPath\":\"A/Fold.cs\"}}]}}\n",
            Text(input));
        Assert.Equal(Text(input), Text(ReducerConventionInput.Read("Projects", Text(input))));
    }

    [Theory]
    [InlineData("{\"version\":3,\"application\":\"Projects\",\"reducerConvention\":\"self\",\"projectionConvention\":\"fluent\"}")]
    [InlineData("{\"version\":3,\"application\":\"Projects\",\"reducerConvention\":\"self\"}")]
    [InlineData("{\"version\":3,\"application\":\"Projects\",\"reducerConvention\":\"self\",\"projectionConvention\":\"model-bound\",\"readModels\":[]}")]
    public void should_refuse_other_projection_conventions(string json) =>
        Assert.Throws<InvalidArtifactRenderContract>(() => ReducerConventionInput.Read("Projects", json));

    [Fact]
    public void should_read_versions_one_and_two_as_fluent()
    {
        Assert.True(ReducerConventionInput.TryParse("{\"version\":1,\"application\":\"Projects\",\"reducerConvention\":\"self\"}", "Projects", out var convention));
        Assert.False(convention.ModelBoundProjections);
    }

    [Fact]
    public void should_select_model_bound_projections_only_for_new_applications()
    {
        var options = new CratisRenderingOptions("Projects", "Projects");
        Assert.True(ReducerConventionInput.TryRead(CratisRendering.CreateNewApplicationProfile("Projects", options), "Projects", out var created));
        Assert.True(created.ModelBoundProjections);
        Assert.True(ReducerConventionInput.TryRead(CratisRendering.CreateProfile("Projects", options), "Projects", out var classic));
        Assert.False(classic.ModelBoundProjections);
    }

    static string Text(ArtifactRenderInput input) => Encoding.UTF8.GetString(input.Bytes.AsSpan());
}
#endif
