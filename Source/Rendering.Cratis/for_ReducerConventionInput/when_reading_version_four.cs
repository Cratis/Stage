// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ReducerConventionInput;

public class when_reading_version_four
{
    const string First = "sem1:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    const string Plain = /* lang=json */ "{\"version\":4,\"application\":\"Projects\",\"reducerConvention\":\"self\",\"projectionConvention\":\"model-bound\"}";

    [Fact]
    public void should_write_canonical_bytes()
    {
        var input = ReducerConventionInput.Read("Projects", /* lang=json */ "{ \"projectionConvention\": \"model-bound\", \"reducerConvention\": \"self\", \"application\": \"Projects\", \"version\": 4 }");
        Assert.Equal("4", input.Version);
        Assert.Equal(Plain + "\n", Text(input));
    }

    [Fact]
    public void should_keep_overrides_after_the_projection_convention()
    {
        var convention = ReducerConvention.Classic with
        {
            SelfDefault = true,
            ModelBoundProjections = true,
            ModelBoundOperations = true,
            Overrides = ImmutableDictionary<SemanticId, ReducerOverride>.Empty.Add(SemanticId.Parse(First), new("Projects.A.Fold", "A/Fold.cs"))
        };
        var input = ReducerConventionInput.Create("Projects", convention);
        Assert.Equal(
            /* lang=json */ $"{{\"version\":4,\"application\":\"Projects\",\"reducerConvention\":\"self\",\"projectionConvention\":\"model-bound\",\"readModels\":[{{\"id\":\"{First}\",\"convention\":\"self\",\"observerId\":\"Projects.A.Fold\",\"previousPath\":\"A/Fold.cs\"}}]}}\n",
            Text(input));
        Assert.Equal(Text(input), Text(ReducerConventionInput.Read("Projects", Text(input))));
    }

    [Theory]
    [InlineData(/* lang=json */ "{\"version\":4,\"application\":\"Projects\",\"reducerConvention\":\"self\",\"projectionConvention\":\"fluent\"}")]
    [InlineData(/* lang=json */ "{\"version\":4,\"application\":\"Projects\",\"reducerConvention\":\"self\"}")]
    [InlineData(/* lang=json */ "{\"version\":4,\"application\":\"Projects\",\"reducerConvention\":\"self\",\"projectionConvention\":\"model-bound\",\"readModels\":[]}")]
    [InlineData(/* lang=json */ "{\"version\":5,\"application\":\"Projects\",\"reducerConvention\":\"self\",\"projectionConvention\":\"model-bound\"}")]
    public void should_refuse_other_configurations(string json) =>
        Assert.Throws<InvalidArtifactRenderContract>(() => ReducerConventionInput.Read("Projects", json));

    [Theory]
    [InlineData(1, false, false)]
    [InlineData(3, true, false)]
    [InlineData(4, true, true)]
    public void should_select_operations_only_from_version_four(int version, bool projections, bool operations)
    {
        var json = version == 3 || version == 4
            ? $"{{\"version\":{version},\"application\":\"Projects\",\"reducerConvention\":\"self\",\"projectionConvention\":\"model-bound\"}}"
            : $"{{\"version\":{version},\"application\":\"Projects\",\"reducerConvention\":\"self\"}}";
        Assert.True(ReducerConventionInput.TryParse(json, "Projects", out var convention));
        Assert.Equal(projections, convention.ModelBoundProjections);
        Assert.Equal(operations, convention.ModelBoundOperations);
    }

    [Fact]
    public void should_be_selected_for_new_applications()
    {
        var profile = CratisRendering.CreateNewApplicationProfile("Projects", new("Projects", "Projects"));
        Assert.Equal("4", profile.Inputs.Single(input => input.Name == ReducerConventionInput.Name).Version);
        Assert.True(ReducerConventionInput.TryRead(profile, "Projects", out var created));
        Assert.True(created.ModelBoundOperations);
    }

    static string Text(ArtifactRenderInput input) => Encoding.UTF8.GetString(input.Bytes.AsSpan());
}
#endif
