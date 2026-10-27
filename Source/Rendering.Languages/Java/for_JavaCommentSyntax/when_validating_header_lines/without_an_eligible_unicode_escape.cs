// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaCommentSyntax.when_validating_header_lines;

public class without_an_eligible_unicode_escape : Specification
{
    bool[] _valid = [];
    readonly List<string?> _reasons = [];

    void Because() => _valid = [.. new[] { "", "Copyright © Cratis", @"\\u000a", @"\\\\uZZZZ", @"trailing\" }.Select(line =>
    {
        var valid = JavaCommentSyntax.TryValidateHeaderLine(line, out var reason);
        _reasons.Add(reason);
        return valid;
    })];

    [Fact] void should_accept_ordinary_text_and_ineligible_introducers() => _valid.ShouldEachConformTo(valid => valid);
    [Fact] void should_not_return_a_refusal_reason() => _reasons.ShouldEachConformTo(reason => reason == null);
    [Fact] void should_supply_java_line_comment_syntax() => JavaCommentSyntax.LinePrefix.ShouldEqual("//");
}
