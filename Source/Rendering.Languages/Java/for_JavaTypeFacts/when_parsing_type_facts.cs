// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaTypeFacts;

public class when_parsing_type_facts : Specification
{
    JavaTypeFacts _facts = null!;
    ImmutableHashSet<string> _names = null!;
    bool _known;
    bool _empty;

    void Because()
    {
        _facts = JavaTypeFacts.Parse("{\"types\":{\"example.Outer.Inner\":[\"N\\u0000ame\",\"Name\"],\"example.Empty\":[]}}");
        _known = _facts.TryGet("example.Outer.Inner", out _names);
        _empty = _facts.TryGet("example.Empty", out _);
    }

    [Fact] void should_find_the_canonical_nested_name() => _known.ShouldBeTrue();
    [Fact] void should_strip_ignorables_and_deduplicate_the_names() => _names.SetEquals(["Name"]).ShouldBeTrue();
    [Fact] void should_distinguish_known_empty_from_unknown() => _empty.ShouldBeTrue();
    [Fact] void should_not_guess_unknown_facts() => _facts.TryGet("example.Unknown", out _).ShouldBeFalse();
    [Fact] void should_use_ordinal_type_identity() => _facts.TryGet("example.outer.Inner", out _).ShouldBeFalse();
}
