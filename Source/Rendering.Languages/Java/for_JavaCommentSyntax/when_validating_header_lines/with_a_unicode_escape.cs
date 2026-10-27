// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaCommentSyntax.when_validating_header_lines;

public class with_a_unicode_escape : Specification
{
    bool[] _valid = [];
    readonly List<string?> _reasons = [];

    void Because() => _valid = [.. new[] { @"\u000a", @"\uuu0041", @"\\\u0041", @"\uZZZZ", @"\u" }.Select(line =>
    {
        var valid = JavaCommentSyntax.TryValidateHeaderLine(line, out var reason);
        _reasons.Add(reason);
        return valid;
    })];

    [Fact] void should_refuse_every_eligible_introducer_including_malformed_escapes() => _valid.ShouldEachConformTo(valid => !valid);
    [Fact] void should_return_a_reason_for_the_header_writer() => _reasons.ShouldEachConformTo(reason => reason == "A Java header line must not contain an eligible Unicode escape.");
}
