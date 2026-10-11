// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticMatchAnchors;

public class when_porting_end_of_input : Specification
{
    [Theory]
    [InlineData("^[A-Z]{3}$", "ABC", true)]
    [InlineData("^[A-Z]{3}$", "ABC\n", false)]
    [InlineData("^[A-Z]{3}$", "ABC\r", false)]
    [InlineData("^[A-Z]{3}$", "ABC\u2028", false)]
    [InlineData(@"^\$$", "$", true)]
    [InlineData("^[$]$", "$", true)]
    [InlineData("^[^$]$", "A", true)]
    [InlineData(@"^\\$", "\\", true)]
    [InlineData(@"^\\$", "\\\n", false)]
    [InlineData(@"^[\]$]+$", "]$", true)]
    [InlineData("^[]$]+$", "]$", true)]
    [InlineData("^A[^]$", "AB\n", false)]
    [InlineData("B", "ABC", true)]
    [InlineData(@"^[^\s@]+@[^\s@.]+(?:\.[^\s@.]+)+$", "a@b.co\n", false)]
    public void should_preserve_literals_and_require_the_whole_input(string pattern, string input, bool expected) =>
        Regex.IsMatch(input, SemanticMatchAnchors.Portable(pattern), RegexOptions.ECMAScript | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1)).ShouldEqual(expected);

    [Theory]
    [InlineData("^[A-Z]{3}$", @"^[A-Z]{3}(?![\s\S])")]
    [InlineData("^[]$]+$", @"^[]$]+(?![\s\S])")]
    [InlineData("^A[^]$", @"^A[^](?![\s\S])")]
    [InlineData(@"^\$$", @"^\$(?![\s\S])")]
    [InlineData("^[$]$", @"^[$](?![\s\S])")]
    [InlineData("B", "B")]
    public void should_rewrite_only_anchors(string pattern, string expected) => SemanticMatchAnchors.Portable(pattern).ShouldEqual(expected);
}
