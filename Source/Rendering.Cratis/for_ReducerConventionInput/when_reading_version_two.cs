// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ReducerConventionInput;

public class when_reading_version_two
{
    const string First = "sem1:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    const string Second = "sem1:bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";

    [Fact]
    public void should_keep_version_one_bytes_without_overrides()
    {
        var input = ReducerConventionInput.Read("Projects", "{ \"reducerConvention\": \"self\", \"application\": \"Projects\", \"version\": 1 }");
        Assert.Equal("1", input.Version);
        Assert.Equal("{\"version\":1,\"application\":\"Projects\",\"reducerConvention\":\"self\"}\n", Text(input));
    }

    [Fact]
    public void should_write_canonical_sorted_overrides()
    {
        var convention = ReducerConvention.Classic with
        {
            Overrides = ImmutableDictionary<SemanticId, ReducerOverride>.Empty
                .Add(SemanticId.Parse(Second), new("Projects.B.Fold", "B/Fold.cs"))
                .Add(SemanticId.Parse(First), new("Projects.A.Fold", "A/Fold.cs"))
        };
        var input = ReducerConventionInput.Create("Projects", convention);
        Assert.Equal("2", input.Version);
        Assert.Equal(Json(Entry(First, "Projects.A.Fold", "A/Fold.cs"), Entry(Second, "Projects.B.Fold", "B/Fold.cs")) + "\n", Text(input));
        Assert.Equal(Text(input), Text(ReducerConventionInput.Read("Projects", Text(input))));
        Assert.True(ReducerConventionInput.TryParse(Text(input), "Projects", out var parsed));
        Assert.Equal("A/Fold.cs", parsed.Overrides[SemanticId.Parse(First)].PreviousPath);
    }

    [Theory]
    [InlineData("duplicate")]
    [InlineData("unsorted")]
    [InlineData("unknown-key")]
    [InlineData("classic-override")]
    [InlineData("empty-observer")]
    [InlineData("unportable-path")]
    [InlineData("not-csharp")]
    [InlineData("bad-id")]
    public void should_refuse_invalid_overrides(string invalid)
    {
        var json = invalid switch
        {
            "duplicate" => Json(Entry(First, "A", "A/Fold.cs"), Entry(First, "A", "A/Fold.cs")),
            "unsorted" => Json(Entry(Second, "B", "B/Fold.cs"), Entry(First, "A", "A/Fold.cs")),
            "unknown-key" => Json(Entry(First, "A", "A/Fold.cs").Replace("}", ",\"extra\":1}", StringComparison.Ordinal)),
            "classic-override" => Json(Entry(First, "A", "A/Fold.cs").Replace("\"self\"", "\"classic\"", StringComparison.Ordinal)),
            "empty-observer" => Json(Entry(First, " ", "A/Fold.cs")),
            "unportable-path" => Json(Entry(First, "A", "../Fold.cs")),
            "not-csharp" => Json(Entry(First, "A", "A/Fold.txt")),
            _ => Json(Entry("not-an-id", "A", "A/Fold.cs"))
        };
        Assert.Throws<InvalidArtifactRenderContract>(() => ReducerConventionInput.Read("Projects", json));
    }

    static string Entry(string id, string observerId, string previousPath) =>
        $"{{\"id\":\"{id}\",\"convention\":\"self\",\"observerId\":\"{observerId}\",\"previousPath\":\"{previousPath}\"}}";

    static string Json(params string[] entries) =>
        $"{{\"version\":2,\"application\":\"Projects\",\"reducerConvention\":\"classic\",\"readModels\":[{string.Join(',', entries)}]}}";

    static string Text(ArtifactRenderInput input) => Encoding.UTF8.GetString(input.Bytes.AsSpan());
}
#endif
