// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Templates.for_TemplateCodes;

public partial class when_listing : Specification
{
    string[] _codes = [];

    void Because() => _codes = [.. typeof(TemplateCodes).GetFields(BindingFlags.Public | BindingFlags.Static).Select(field => (string)field.GetRawConstantValue()!)];

    [Fact] void should_have_unique_codes() => _codes.Distinct(StringComparer.Ordinal).Count().ShouldEqual(_codes.Length);
    [Fact] void should_have_stable_code_shapes() => _codes.ShouldEachConformTo(CodePattern.IsMatch);
    [Fact] void should_cover_the_profile_rules() => _codes.Length.ShouldEqual(24);

    [System.Text.RegularExpressions.GeneratedRegex(@"^STAGE-TPL-\d{3}$", System.Text.RegularExpressions.RegexOptions.CultureInvariant, 1000)]
    private static partial System.Text.RegularExpressions.Regex CodePattern { get; }
}
