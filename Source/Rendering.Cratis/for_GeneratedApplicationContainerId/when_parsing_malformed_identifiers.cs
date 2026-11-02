// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_GeneratedApplicationContainerId;

public class when_parsing_malformed_identifiers : Specification
{
    static readonly string _full = new('a', 64);
    bool _empty;
    bool _short;
    bool _upper;
    bool _tooShort;
    bool _tooLong;
    bool _multiline;
    bool _mixed;
    bool _padded;

    void Because()
    {
        _empty = Accepts(string.Empty);
        _short = Accepts("abc123");
        _upper = Accepts(new string('A', 64));
        _tooShort = Accepts(new string('a', 63));
        _tooLong = Accepts(new string('a', 65));
        _multiline = Accepts($"{_full}\n{_full}");
        _mixed = Accepts($"Error response from daemon: {_full}");
        _padded = Accepts($"{_full}\n");
    }

    [Fact] void should_reject_an_empty_text() => _empty.ShouldBeFalse();
    [Fact] void should_reject_an_abbreviated_identifier() => _short.ShouldBeFalse();
    [Fact] void should_reject_uppercase_characters() => _upper.ShouldBeFalse();
    [Fact] void should_reject_63_characters() => _tooShort.ShouldBeFalse();
    [Fact] void should_reject_65_characters() => _tooLong.ShouldBeFalse();
    [Fact] void should_reject_multiple_lines() => _multiline.ShouldBeFalse();
    [Fact] void should_reject_an_identifier_mixed_with_other_text() => _mixed.ShouldBeFalse();
    [Fact] void should_reject_a_trailing_newline() => _padded.ShouldBeFalse();

    static bool Accepts(string text) => GeneratedApplicationContainerId.TryParse(text, out _);
}
