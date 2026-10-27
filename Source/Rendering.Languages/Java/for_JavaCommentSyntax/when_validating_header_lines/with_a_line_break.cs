// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaCommentSyntax.when_validating_header_lines;

public class with_a_line_break : Specification
{
    bool[] _valid = [];
    readonly List<string?> _reasons = [];

    void Because() => _valid = [.. new[] { "a\rb", "a\nb", "a\r\nb" }.Select(line =>
    {
        var valid = JavaCommentSyntax.TryValidateHeaderLine(line, out var reason);
        _reasons.Add(reason);
        return valid;
    })];

    [Fact] void should_refuse_cr_lf_and_crlf() => _valid.ShouldEachConformTo(valid => !valid);
    [Fact] void should_explain_each_refusal_without_a_header_diagnostic_code() => _reasons.ShouldEachConformTo(reason => reason == "A Java header line must not contain CR or LF.");
}
