// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Api.for_ReadModelArgumentFilter;

public class when_matching_instances : Specification
{
    DynamicReadModel _commentForA = null!;
    DynamicReadModel _commentForB = null!;
    DynamicReadModel _workItemB = null!;
    IReadOnlyList<object> _byProperty = null!;
    IReadOnlyList<object> _byKey = null!;
    IReadOnlyList<object> _missing = null!;
    IReadOnlyList<object> _empty = null!;

    void Establish()
    {
        _commentForA = Instance("c-1", "workItemId", "AAAAAAAA-0000-0000-0000-000000000000");
        _commentForB = Instance("c-2", "workItemId", "bbbbbbbb-0000-0000-0000-000000000000");
        _workItemB = Instance("bbbbbbbb-0000-0000-0000-000000000000", "title", "B");
    }

    void Because()
    {
        _byProperty = ReadModelArgumentFilter.Matching([_commentForA, _commentForB], "workItemId", "BBBBBBBB-0000-0000-0000-000000000000");
        _byKey = ReadModelArgumentFilter.Matching([_workItemB], "workItemId", "bbbbbbbb-0000-0000-0000-000000000000");
        _missing = ReadModelArgumentFilter.Matching([_commentForA, _commentForB], "workItemId", null);
        _empty = ReadModelArgumentFilter.Matching([_commentForA, _commentForB], "workItemId", " ");
    }

    [Fact] void should_match_the_property_regardless_of_case() => _byProperty.ShouldContainOnly([_commentForB]);
    [Fact] void should_match_the_instance_key_when_the_property_is_not_carried() => _byKey.ShouldContainOnly([_workItemB]);
    [Fact] void should_match_nothing_for_a_missing_argument() => _missing.ShouldBeEmpty();
    [Fact] void should_match_nothing_for_an_empty_argument() => _empty.ShouldBeEmpty();

    static DynamicReadModel Instance(string id, string property, string value) => new()
    {
        Id = id,
        Values = new Dictionary<string, JsonElement> { [property] = JsonSerializer.SerializeToElement(value) }
    };
}
