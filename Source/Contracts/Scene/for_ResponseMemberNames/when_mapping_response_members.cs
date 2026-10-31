// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_ResponseMemberNames;

public class when_mapping_response_members : Specification
{
    JsonNode _document = null!;
    readonly List<string> _names = [];

    void Because()
    {
        _document = JsonNode.Parse("""
            {"kind":"commandResponse","path":"Parent","children":[
              null,42,[{"kind":"commandResponse","path":"Child"}],
              {"kind":7,"path":"NotAResponse"},
              {"kind":"commandResponse","path":7},
              {"kind":"commandResponse","path":""},
              {"kind":"dataContext","path":"Ordinary"},
              {"path":"MissingKind"},{"kind":"commandResponse"}
            ]}
            """)!;
        ResponseMemberNames.Apply(_document, name =>
        {
            _names.Add(name);
            return name.ToLowerInvariant();
        });
    }

    [Fact]
    void should_map_parent_before_nested_array_children_and_skip_rejected_leaves() =>
        Assert.Equal(new[] { "Parent", "Child" }, _names);

    [Fact]
    void should_rewrite_only_nonempty_string_response_paths() => Assert.True(JsonNode.DeepEquals(_document, JsonNode.Parse("""
        {"kind":"commandResponse","path":"parent","children":[
          null,42,[{"kind":"commandResponse","path":"child"}],
          {"kind":7,"path":"NotAResponse"},
          {"kind":"commandResponse","path":7},
          {"kind":"commandResponse","path":""},
          {"kind":"dataContext","path":"Ordinary"},
          {"path":"MissingKind"},{"kind":"commandResponse"}
        ]}
        """)));
}
